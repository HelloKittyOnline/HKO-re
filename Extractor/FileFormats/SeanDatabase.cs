using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Extractor;

[AttributeUsage(AttributeTargets.Property)]
class SeanField : Attribute {
    public int Id { get; }
    public int Size { get; }

    public SeanField(int id, int size = 1) {
        Id = id;
        Size = size;
    }
}

[AttributeUsage(AttributeTargets.Struct)]
class SeanItem : Attribute {
    public int Size { get; }

    public SeanItem(int size) {
        Size = size;
    }
}

public class SeanDatabase {
    public int ItemSize;
    public int ItemCount;
    public int[,] Items;
    public Dictionary<int, string> Strings = new();

    public SeanDatabase(byte[] data) : this(new MemoryStream(data)) { }

    public SeanDatabase(Stream stream) {
        var reader = new BinaryReader(stream);

        var head = reader.ReadBytes(4); // "SD01"
        if(head[0] != 'S' || head[1] != 'D' || head[2] != '0' || head[3] != '1') {
            throw new Exception("Invalid Sean Database");
        }

        ItemSize = reader.ReadInt32();
        ItemCount = reader.ReadInt32();

        var stringByteSize = reader.ReadInt32();

        Items = new int[ItemCount, ItemSize];
        for(int i = 0; i < ItemCount; i++) {
            for(int j = 0; j < ItemSize; j++) {
                Items[i, j] = reader.ReadInt32();
            }
        }

        if(stream.Position == stream.Length)
            return; // no text?

        Debug.Assert(stringByteSize == (stream.Length - stream.Position - 4));

        var root = stream.Position;
        var stringCount = reader.ReadInt32();
        for(int i = 0; i < stringCount; i++) {
            var pos = (int)(stream.Position - root);
            Strings[pos] = Helper.ReadCString(reader);
        }
    }

    public static byte[] Save<T>(T[] items) {
        var itemAtt = typeof(T).GetCustomAttribute<SeanItem>();

        var ItemCount = items.Length;
        var props = new (Type type, Func<T, object> getter)[itemAtt.Size];

        int BuildPropArray(Type type, int offset, Func<T, object> pGetter) {
            int size = 0;

            foreach(var prop in type.GetProperties()) {
                var att = prop.GetCustomAttribute<SeanField>();
                if(att == null)
                    continue;

                if(!prop.PropertyType.IsArray) {
                    props[offset + att.Id] = (prop.PropertyType, x => prop.GetValue(pGetter(x)));
                    size++;
                } else {
                    var subType = prop.PropertyType.GetElementType()!;

                    var start = offset + att.Id;
                    for(int i = 0; i < att.Size; i++) {
                        var i1 = i;
                        start += BuildPropArray(subType, start, x => ((Array)prop.GetValue(pGetter(x))).GetValue(i1));
                    }
                }
            }

            return size;
        }
        BuildPropArray(typeof(T), 0, x => x);
        Debug.Assert(props.Count(x => x.getter != null) == itemAtt.Size);

        var ms = new MemoryStream();
        var w = new BinaryWriter(ms);

        w.Write((byte)'S');
        w.Write((byte)'D');
        w.Write((byte)'0');
        w.Write((byte)'1');

        w.Write(itemAtt.Size);
        w.Write(ItemCount);

        int stringPos = 0;
        var dict = new Dictionary<string, int>();

        // pre calculate string positions
        for(int i = 0; i < ItemCount; i++) {
            foreach(var prop in props) {
                if(prop.type == typeof(string)) {
                    var s = (string)prop.getter(items[i]);

                    if(s == null || dict.ContainsKey(s))
                        continue;

                    dict[s] = stringPos + 4;
                    stringPos += Encoding.UTF8.GetByteCount(s) + 1;
                }
            }
        }

        var padding = stringPos % 4 == 0 ? 0 : 4 - (stringPos % 4);
        w.Write(stringPos + padding); // stringByteSize

        // write integers and replace strings with their calculated positions
        for(int i = 0; i < ItemCount; i++) {
            foreach(var prop in props) {
                var val = prop.getter(items[i]);

                if(prop.type == typeof(string)) {
                    w.Write(val == null ? 0 : dict[(string)val]);
                } else if(prop.type.IsEnum || prop.type == typeof(int)) {
                    w.Write((int)val);
                } else {
                    w.Write((int)Convert.ChangeType(val, TypeCode.Int32));
                }
            }
        }

        if(dict.Count == 0) {
            return ms.ToArray();
        }

        w.Write(dict.Count); // string count

        // write actual strings
        for(int i = 0; i < ItemCount; i++) {
            foreach(var prop in props) {
                if(prop.type == typeof(string)) {
                    var val = (string)prop.getter(items[i]);

                    if(val != null && dict.ContainsKey(val)) {
                        w.WriteCString(val);
                        dict.Remove(val);
                    }
                }
            }
        }

        for(int i = 0; i < padding; i++) {
            w.Write((byte)0);
        }

        return ms.ToArray();
    }

    public static T[] Load<T>(byte[] data) where T : struct {
        var db = new SeanDatabase(data);

        var ret = new T[db.ItemCount];

#if DEBUG
        // used to detect unused strings. Helpful for reversing file format
        var usedStrings = new HashSet<int>();
#endif

        (object, int) WriteValues(Type type, int i, int offset) {
            var el = Activator.CreateInstance(type);

            int size = 0;
            foreach(var prop in type.GetProperties()) {
                var att = prop.GetCustomAttribute<SeanField>();
                if(att == null)
                    continue;

                int j = offset + att.Id;

                if(!prop.CanWrite) {
                    Debug.Assert(prop.GetValue(el).Equals(db.Items[i, j]));
                    size++;
                } else if(prop.PropertyType.IsArray) {
                    var subType = prop.PropertyType.GetElementType()!;
                    var arr = Array.CreateInstance(subType, att.Size);

                    for(int k = 0; k < att.Size; k++) {
                        var (v, s) = WriteValues(subType, i, j);
                        arr.SetValue(v, k);
                        j += s;
                        size += s;
                    }

                    prop.SetValue(el, arr);
                } else {
                    if(prop.PropertyType == typeof(string)) {
#if DEBUG
                        usedStrings.Add(db.Items[i, j]);
#endif
                        prop.SetValue(el, db.GetString(i, j));
                    } else if(prop.PropertyType.IsEnum || prop.PropertyType == typeof(int)) {
                        prop.SetValue(el, db.Items[i, j]);
                    } else {
                        prop.SetValue(el, Convert.ChangeType(db.Items[i, j], prop.PropertyType));
                    }

                    size++;
                }
            }

            return (el, size);
        }

        int size = 0;
        for(int i = 0; i < db.ItemCount; i++) {
            var (v, s) = WriteValues(typeof(T), i, 0);
            ret[i] = (T)v;
            size = s;
        }

        var itemAtt = typeof(T).GetCustomAttribute<SeanItem>();
        Debug.Assert(itemAtt != null && itemAtt.Size == db.ItemSize);
        Debug.Assert(size == db.ItemSize);

#if DEBUG
        foreach(var dbString in db.Strings) {
            if(!usedStrings.Contains(dbString.Key)) {
                Console.WriteLine($"Unused string {dbString}");
            }
        }
#endif

        return ret;
    }

    public string ToCsv() {
        string str = "";

        for(int i = 0; i < ItemCount; i++) {
            for(int j = 0; j < ItemSize; j++) {
                if(j != 0)
                    str += ",";
                str += Items[i, j];
            }

            str += "\n";
        }

        return str;
    }

    public string GetString(int i, int j) {
        Strings.TryGetValue(Items[i, j], out var val);
        return val;
    }
}
