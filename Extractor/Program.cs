using System;
using System.IO;
using System.Threading.Tasks;

namespace Extractor {
    class Program {
        static void PatchSdb() {
            foreach(var file in Directory.GetFiles("C:/Program Files (x86)/SanrioTown/Hello Kitty Online/tables", "*.sdb")) {
                var data = SeanArchive.Extract(file);

                bool patched = false;

                foreach(var item in data) {
                    if(item.Name != "lobby_info.txt") continue;
                    var lobbys = SeanDatabase.Load<LobbyInfo>(item.Contents);
                    lobbys[1].Address = "ip:127.0.0.1";
                    item.Contents = SeanDatabase.Save(lobbys);
                    patched = true;
                }

                if(!patched) continue;

                var path = "C:/Program Files (x86)/SanrioTown/Hello Kitty Online/tables/" + Path.GetFileName(file);
                var rep = SeanArchive.Create(data);

                if(!File.Exists(path + ".old")) {
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

                switch(Path.GetExtension(file).ToLower()) {
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
        }
    }
}
