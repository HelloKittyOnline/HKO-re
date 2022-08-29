using System.Diagnostics;
using System.Text;
using ICSharpCode.SharpZipLib.Tar;

namespace Autoupdater;

public partial class Form1 : Form {
    static HttpClient client = new();

    private string currentVersion;
    private CancellationTokenSource cancellationToken = new();
    private bool running;

    private const string url = "http://hko.evidentfla.me:8080/single/leading.txt";

    public Form1() {
        InitializeComponent();

        if(!File.Exists("Leading.ini")) {
            Directory.SetCurrentDirectory("C:\\Program Files (x86)\\SanrioTown\\Hello Kitty Online");
        }

        if(!File.Exists("Leading.ini")) {
            MessageBox.Show("Could not find HKO installation", "Error", MessageBoxButtons.OK);
            Close();
            return;
        }

        File.WriteAllText("Leading.ini", url);
        var str = SplitLines(File.ReadAllText("ver/version_pc.txt"));
        currentVersion = str[^1];

        ProcessUpdates();
    }

    static string[] SplitLines(string text) {
        return text.Split("\n").Select(x => x.Trim('\r')).Where(x => x.Length != 0).ToArray();
    }

    public async void ProcessUpdates() {
        var body = await client.GetStringAsync(url);
        var links = SplitLines(body);

        var ver = links[0];
        var core = links[1];
        var file = links[2];

        var patches = new List<(string, string)>();

        // download list of patches
        var version = currentVersion;
        while(true) {
            var test = await client.GetAsync($"{ver}/{version}_pc.txt");
            if(!test.IsSuccessStatusCode)
                break;

            var lines = SplitLines(await test.Content.ReadAsStringAsync());
            version = lines[0];

            patches.Add((lines[0], lines[1]));
        }

        if(patches.Count == 0) {
            Finish();
            return;
        }

        progressBar1.Maximum = patches.Count;
        progressBar2.Maximum = 10000;

        running = true;

        foreach(var patch in patches) {
            progressBar2.Value = 0;

            // download patch to temp file
            var tempFile = Path.GetTempFileName();
            var stream = File.Open(tempFile, FileMode.Open, FileAccess.ReadWrite);

            try {
                // Get the http headers first to examine the content length
                using var response = await client.GetAsync($"{core}/{patch.Item2}", HttpCompletionOption.ResponseHeadersRead);
                var contentLength = response.Content.Headers.ContentLength;

                await using var download = await response.Content.ReadAsStreamAsync();

                // Ignore progress reporting when the content length is unknown
                if(!contentLength.HasValue) {
                    await download.CopyToAsync(stream);
                } else {
                    await download.CopyToAsync(stream, 81920, new Progress<long>(totalBytes => {
                        var prog = (float)totalBytes / contentLength.Value;

                        progressBar2.Value = (int)(prog * 10000);
                        progressBar2.ManualText = $"{totalBytes / 1000000.0:F2}/{contentLength.Value / 1000000.0:F2}MB - {prog * 100:F0}%";
                    }), cancellationToken.Token);

                    progressBar2.Value = 10000;
                }
            } catch(TaskCanceledException) {
                Close();
                break;
            }

            stream.Seek(0, SeekOrigin.Begin);
            using var tarArchive = TarArchive.CreateInputTarArchive(stream, Encoding.UTF8);

            progressBar2.ManualText = "Extracting...";
            await Task.Run(() => {
                tarArchive.ExtractContents("./");
            });

            stream.Close();
            File.Delete(tempFile);
            File.AppendAllText("ver/version_pc.txt", $"{patch.Item1}\r\n");

            progressBar1.Value++;
        }

        progressBar2.ManualText = "Done";
        running = false;
        button1.Enabled = true;
    }

    private void Finish() {
        Process.Start("./hko.exe", "execute_by_leading");
        Close();
    }

    private void button1_Click(object sender, EventArgs e) {
        Finish();
    }

    private void button2_Click(object sender, EventArgs e) {
        if(running) {
            cancellationToken.Cancel();
        } else {
            Close();
        }
    }
}
