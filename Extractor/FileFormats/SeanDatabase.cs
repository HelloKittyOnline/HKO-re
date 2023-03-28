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

    public SeanField(int id) {
        Id = id;
    }
}

[AttributeUsage(AttributeTargets.Property)]
class SeanArray : Attribute {
    public int Start, Count;

    public SeanArray(int start, int count) {
        Start = start;
        Count = count;
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
    public Dictionary<int, string> Strings = new Dictionary<int, string>();

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

        var root = stream.Position;
        var stringCount = reader.ReadInt32();
        for(int i = 0; i < stringCount; i++) {
            var pos = (int)(stream.Position - root);
            Strings[pos] = Helper.ReadCString(reader);
        }
    }

    public static byte[] Save<T>(T[] items) {
        var ms = new MemoryStream();
        var w = new BinaryWriter(ms);

        w.Write((byte)'S');
        w.Write((byte)'D');
        w.Write((byte)'0');
        w.Write((byte)'1');

        var itemAtt = typeof(T).GetCustomAttribute<SeanItem>();

        var ItemCount = items.Length;
        var ItemSize = itemAtt.Size;

        var props = new PropertyInfo[ItemSize];

        foreach(var prop in typeof(T).GetProperties()) {
            var att = prop.GetCustomAttribute<SeanField>();
            if(att == null)
                continue;

            props[att.Id] = prop;
        }

        w.Write(ItemSize);
        w.Write(ItemCount);

        int stringPos = 0;
        var dict = new Dictionary<string, int>();

        // pre calculate string positions
        for(int i = 0; i < ItemCount; i++) {
            for(int j = 0; j < ItemSize; j++) {
                var prop = props[j];

                if(prop != null && prop.PropertyType == typeof(string)) {
                    var s = (string)prop.GetValue(items[i]);

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
            for(int j = 0; j < ItemSize; j++) {
                var prop = props[j];

                if(prop == null) {
                    w.Write(0);
                    continue;
                }

                var val = prop.GetValue(items[i]);

                if(prop.PropertyType == typeof(string)) {
                    w.Write(val == null ? 0 : dict[(string)val]);
                } else if(prop.PropertyType.IsEnum || prop.PropertyType == typeof(int)) {
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
            for(int j = 0; j < ItemSize; j++) {
                var prop = props[j];

                if(prop != null && prop.PropertyType == typeof(string)) {
                    var val = (string)prop.GetValue(items[i]);

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

        (PropertyInfo, SeanField)[] GetFields(Type t) {
            return t.GetProperties().Select(x => (x, x.GetCustomAttribute<SeanField>())).Where(x => x.Item2 != null).ToArray();
        }

        var fields = GetFields(typeof(T));
        var arrays = new List<(PropertyInfo, SeanArray, Type, (PropertyInfo, SeanField)[])>();
        foreach(var prop in typeof(T).GetProperties()) {
            var att = prop.GetCustomAttribute<SeanArray>();
            if(att == null)
                continue;

            var type = prop.PropertyType.GetElementType();
            var f = GetFields(type);

            arrays.Add((prop, att, type, f));
        }

#if DEBUG
        var itemAtt = typeof(T).GetCustomAttribute<SeanItem>();
        if(itemAtt == null || itemAtt.Size != db.ItemSize) {
            Console.WriteLine($"{typeof(T).Name}: invalid Item size");
            Debugger.Break();
        }
        var arrCount = arrays.Sum(x => x.Item2.Count * x.Item4.Length);
        if(fields.Length + arrCount != db.ItemSize) {
            Console.WriteLine($"{typeof(T).Name}: missing some elements");
        }

        // used to detect unused strings. Helpful for reversing file format
        var usedStrings = new HashSet<int>();
#endif

        for(int i = 0; i < db.ItemCount; i++) {
            object item = new T(); // cast to object for struct support

            foreach(var (prop, att) in fields) {
                if(!prop.CanWrite) { // assume constant value
                    Debug.Assert((int)prop.GetValue(item) == db.Items[i, att.Id]);
                } else if(prop.PropertyType == typeof(string)) {
#if DEBUG
                    usedStrings.Add(db.Items[i, att.Id]);
#endif
                    prop.SetValue(item, db.GetString(i, att.Id));
                } else if(prop.PropertyType.IsEnum || prop.PropertyType == typeof(int)) {
                    prop.SetValue(item, db.Items[i, att.Id]);
                } else {
                    prop.SetValue(item, Convert.ChangeType(db.Items[i, att.Id], prop.PropertyType));
                }
            }

            foreach(var (prop, att, type, f) in arrays) {
                var arr = Array.CreateInstance(type, att.Count);

                for(int j = 0; j < att.Count; j++) {
                    var el = Activator.CreateInstance(type);
                    var s = f.Length;

                    foreach(var (prop1, att1) in f) {
                        var pos = att.Start + s * j + att1.Id;
                        prop1.SetValue(el, Convert.ChangeType(db.Items[i, pos], prop1.PropertyType));
                    }

                    arr.SetValue(el, j);
                }

                prop.SetValue(item, arr);
            }

            ret[i] = (T)item;
        }
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
