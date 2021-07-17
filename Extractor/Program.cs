using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extractor {
    class Program {
        static int PatternAt(byte[] source, byte[] pattern) {
            // TODO: make this more efficient
            for (int i = 0; i < source.Length; i++) {
                if (source.Skip(i).Take(pattern.Length).SequenceEqual(pattern)) {
                    return i;
                }
            }

            return -1;
        }

        static void PatchSdb() {
            var pattern = Encoding.ASCII.GetBytes("ip:");
            var replace = Encoding.ASCII.GetBytes("127.0.0.1");

            foreach (var file in Directory.GetFiles("C:/Program Files (x86)/SanrioTown/Hello Kitty Online/tables", "*.sdb")) {
                var data = Sdb.Extract(file);

                bool patched = false;

                foreach(var item in data) {
                    var pos = PatternAt(item.Contents, pattern);
                    if (pos == -1) continue;
                    
                    var dat = item.Contents.ToList();

                    pos += 3;
                    dat.RemoveRange(pos, dat.Count - pos);
                    dat.AddRange(replace);
                    dat.Add(0);

                    while (dat.Count % 4 != 0) {
                        dat.Add(0);
                    }

                    item.Contents = dat.ToArray();
                    
                    patched = true;
                }

                if (!patched) continue;

                var path = "C:/Program Files (x86)/SanrioTown/Hello Kitty Online/tables/" + Path.GetFileName(file);
                var rep = Sdb.Create(data);

                if (!File.Exists(path + ".old")) {
                    File.Move(path, path + ".old");
                }
                File.WriteAllBytes(path, rep);
                Console.WriteLine($"Patched {path}");
            }
        }
        
        static void ExtractData(string dir) {
            var files = Directory.GetFiles("C:/Program Files (x86)/SanrioTown/Hello Kitty Online/data/", "*.*", SearchOption.AllDirectories);

            Parallel.ForEach(files, file => {
                var outPath = Path.Combine(dir, file.Split(Path.DirectorySeparatorChar)[^2], Path.GetFileNameWithoutExtension(file));
                Directory.CreateDirectory(outPath);
                
                switch (Path.GetExtension(file).ToLower()) {
                    case ".man": Man.Extract(file, outPath); break;
                    case ".ani": Ani.Extract(file, outPath); break;
                    case ".map": Map.Extract(file, outPath); break;
                    case ".ogg": // normal audio file
                    case ".jpg": // normal jpg image
                    case ".fnt": // TODO: custom font file
                        break;
                    default:
                        Console.WriteLine($"Unknown extension {Path.GetFileName(file)}");
                        break;
                }
            });
        }

        static void Main(string[] args) {
            if(args.Length == 0) {
                Console.WriteLine("Usage options:");
                Console.WriteLine("  patch           // rewrites HKO config to connect to localhost");
                Console.WriteLine("  extract [path]  // extracts all images from the data folder");
                return;
            }

            switch(args[0]) {
                case "patch":
                    PatchSdb();
                    break;
                case "extract":
                    if(args.Length == 1) {
                        Console.WriteLine("Missing output path");
                        return;
                    }
                    ExtractData(args[1]);
                    break;
            }

            // PatchSdb();
            // ExtractData("D:/Daten/Desktop/extract/");
        }
    }
}
