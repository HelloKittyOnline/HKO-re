using System.Diagnostics;
using System.Text;
using ICSharpCode.SharpZipLib.Tar;
using Microsoft.Win32;

namespace Launcher;

public partial class Form1 : Form {
    static HttpClient client = new();

    private string currentVersion;
    private CancellationTokenSource cancellationToken = new();
    private bool running;

#if DEBUG
    private const string url = "http://127.0.0.1:8080/single/leading.txt";
#else
    private const string url = "http://hko.evidentfla.me:8080/single/leading.txt";
#endif

    private string? HKOPath;

    public Form1() {
        InitializeComponent();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.0.0 Safari/537.36");

        var ver = Version.GetCurrent();
        Text = $"HKO Launcher v{ver.Major}.{ver.Minor}.{ver.Patch}";

        RunStuff();
    }

    private async Task<bool> DownloadFile(string url, Stream stream) {
        try {
            // Get the http headers first to examine the content length
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            var contentLength = response.Content.Headers.ContentLength;

            await using var download = await response.Content.ReadAsStreamAsync();

            // Ignore progress reporting when the content length is unknown
            if(!contentLength.HasValue) {
                await download.CopyToAsync(stream);

                progressBar2.ManualText = "??? / ???";
            } else {
                progressBar2.Maximum = 10000;

                await download.CopyToAsync(stream, 81920, new Progress<long>(totalBytes => {
                    var prog = (float)totalBytes / contentLength.Value;

                    progressBar2.Value = (int)(prog * 10000);
                    progressBar2.ManualText = $"{totalBytes / 1000000.0:F2}/{contentLength.Value / 1000000.0:F2}MB - {prog * 100:F0}%";
                }), cancellationToken.Token);

                progressBar2.Value = 10000;
            }

            return true;
        } catch(TaskCanceledException) {
            Close();
        } catch(Exception ex) {
            MessageBox.Show($"Error while downloading file:\n{ex}", "Error", MessageBoxButtons.OK);
            Close();
        }

        return false;
    }

    static string[] SplitLines(string text) {
        return text.Split("\n").Select(x => x.Trim('\r')).Where(x => x.Length != 0).ToArray();
    }

    private async Task CheckFlash() {
        var type = Type.GetTypeFromProgID("ShockwaveFlash.ShockwaveFlash");
        if(type != null) {
            return;
        }

        var res = MessageBox.Show("No flash installation found.\nHKO requires the flash plugin for internet explorer to run correctly.\nInstall Clean Flash Player 34.0.0.277?", "Warning", MessageBoxButtons.YesNo);

        if(res != DialogResult.Yes)
            return;

        var fileName = "./cleanflash3400277installer1.exe";
        if(!File.Exists(fileName)) {
            var stream = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite);
            await DownloadFile("http://hko.evidentfla.me:8080/cleanflash3400277installer3.exe", stream);
            stream.Close();
        }

        Enabled = false;
        var proc = Process.Start(fileName);
        await proc.WaitForExitAsync();
        Enabled = true;

#if !DEBUG
        File.Delete(fileName);
#endif
    }

    private async Task<bool> DoInstall() {
        if(!File.Exists("./POD-19902_setup.exe")) {
            progressBar1.ManualText = "Downloading HKO installer...";

            var stream = File.Open("./POD-19902_setup.exe", FileMode.Create, FileAccess.ReadWrite);
            if(!await DownloadFile("http://hko.evidentfla.me:8080/POD-19902_setup.exe", stream)) {
                return false;
            }
            stream.Close();
        }

        progressBar1.ManualText = "Installing HKO...";
        // https://jrsoftware.org/ishelp/index.php?topic=setupcmdline
        var proc = Process.Start("./POD-19902_setup.exe", "/silent /TASKS=\"\"");

        Enabled = false;
        await proc.WaitForExitAsync();
        if(proc.ExitCode != 0) {
            return false;
        }
        Enabled = true;
#if !DEBUG
        File.Delete("./POD-19902_setup.exe");
#endif
        return true;
    }

    // searches through registry to find Hello Kitty install location
    static string? FindInstallation() {
        if(File.Exists("./hko.exe")) { // current folder
            return ".";
        }
        if(File.Exists("C:\\Program Files (x86)\\SanrioTown\\Hello Kitty Online\\hko.exe")) { // standard path
            return "C:\\Program Files (x86)\\SanrioTown\\Hello Kitty Online";
        }

        try {
            var local = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            var key = local.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall");
            var sub = key.GetSubKeyNames();

            foreach(var name in sub) {
                var s = key.OpenSubKey(name);
                var n = (string)s.GetValue("DisplayName");

                if(n == "Hello Kitty Online POD Installer") {
                    return (string)s.GetValue("InstallLocation");
                }
            }
        } catch { }

        return null;
    }

    private async void RunStuff() {
        var latest = await Version.GetLatest(client);
        var current = Version.GetCurrent();
        if(latest != null) {
            if(latest > current) {
                var res = MessageBox.Show("New Launcher Version detected.\nOpen in browser?", "New Version Avalible", MessageBoxButtons.YesNo);
                if(res == DialogResult.Yes) {
                    Process.Start("explorer", latest.Url);
                    Close();
                    return;
                }
            }
        }

        HKOPath = FindInstallation();
        if(HKOPath == null) {
            var res = MessageBox.Show("Could not find HKO installation.\nInstall automatically.", "Error", MessageBoxButtons.YesNo);
            if(res != DialogResult.Yes) {
                Close();
                return;
            }

            if(!await DoInstall()) {
                Close();
                return;
            }
            HKOPath = FindInstallation();
        }

        await CheckFlash();

        File.WriteAllText($"{HKOPath}/Leading.ini", url);
        var str = SplitLines(File.ReadAllText($"{HKOPath}/ver/version_pc.txt"));
        currentVersion = str[^1];

        var body = await client.GetStringAsync(url);
        var links = SplitLines(body);

        var ver = links[0];
        var core = links[1];
        var file = links[2];

        var patches = new List<(string version, string path)>();

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

        running = true;

        for(var i = 0; i < patches.Count; i++) {
            var patch = patches[i];

            progressBar1.ManualText = $"Installing update - {i + 1}/{patches.Count}";
            progressBar1.Value = i + 1;
            progressBar2.Value = 0;

            var tempFile = Path.GetTempFileName();
            var stream = File.Open(tempFile, FileMode.Open, FileAccess.ReadWrite);

            if(!await DownloadFile($"{core}/{patch.path}", stream)) {
                stream.Close();
                File.Delete(tempFile);

                return;
            }

            stream.Seek(0, SeekOrigin.Begin);
            using var tarArchive = TarArchive.CreateInputTarArchive(stream, Encoding.UTF8);

            progressBar2.ManualText = "Extracting...";
            await Task.Run(() => { tarArchive.ExtractContents(HKOPath); });

            stream.Close();
            File.Delete(tempFile);
            File.AppendAllText($"{HKOPath}/ver/version_pc.txt", $"{patch.version}\r\n");
        }

        progressBar1.ManualText = "";
        progressBar2.ManualText = "Done";
        running = false;
        button1.Enabled = true;
    }

    private void Finish() {
        var info = new ProcessStartInfo {
            FileName = $"{HKOPath}/hko.exe",
            Arguments = "execute_by_leading",
            WorkingDirectory = HKOPath
        };
        Process.Start(info);

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
