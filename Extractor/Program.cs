using System;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Tar;

namespace Extractor;

class Program {
    static byte[] PatchSdb(string path) {
        var data = SeanArchive.Extract(path);

        foreach(var item in data) {
            if(item.Name != "lobby_info.txt")
                continue;

            var lobbys = SeanDatabase.Load<LobbyInfo>(item.Contents);
            lobbys[1].Address = "ip:127.0.0.1";
            item.Contents = SeanDatabase.Save(lobbys);
        }

        return SeanArchive.Create(data);
    }

    static void CreateTar(string root, string outP) {
        {
            var file1 = new DirectoryInfo($"{root}\\data").GetFiles("*.*", SearchOption.AllDirectories);
            var file2 = new DirectoryInfo($"{root}\\flash").GetFiles("*.*", SearchOption.AllDirectories);

            var files = file1.Concat(file2).ToArray();

            int pos = 0;
            for(int i = 0; pos < files.Length; i++) {
                using var stream = File.OpenWrite($"{outP}\\data_{i}.tar");
                using var tar = TarArchive.CreateOutputTarArchive(stream);
                tar.RootPath = root;

                long size = 0;
                while(size < (1 << 30) && pos < files.Length) {
                    var item = files[pos++];

                    Console.WriteLine($"Adding {item.FullName}");
                    var tarEntry = TarEntry.CreateEntryFromFile(item.FullName);
                    tar.WriteEntry(tarEntry, false);

                    size += item.Length;
                }
            }
        }

        {
            var tables = new DirectoryInfo($"{root}\\tables").GetFiles("*.*", SearchOption.AllDirectories);

            using var stream = File.OpenWrite($"{outP}\\tables.tar");
            using var tar = new TarOutputStream(stream, Encoding.UTF8);

            foreach(var item in tables) {
                if(item.FullName.EndsWith(".old"))
                    continue;

                Console.WriteLine($"Adding {item.FullName}");

                var tarEntry = TarEntry.CreateTarEntry(Path.GetRelativePath(root, item.FullName));

                if(item.Extension == ".sdb") {
                    var data = PatchSdb(item.FullName);
                    tarEntry.Size = data.Length;

                    tar.PutNextEntry(tarEntry);
                    tar.Write(data);
                } else {
                    tarEntry.Size = item.Length;

                    tar.PutNextEntry(tarEntry);
                    using var inFile = item.OpenRead();
                    inFile.CopyTo(tar);
                }

                tar.CloseEntry();
            }
        }
    }

    static void Main(string[] args) {
        // Enter local paths here
        var hkoPath = ""; // should be the path to the client we got from reddit
        var outPath = "";

        if(hkoPath == "" || outPath == "") {
            Console.WriteLine("Please enter valid data paths");
            return;
        }

        CreateTar(hkoPath, outPath);
    }
}
