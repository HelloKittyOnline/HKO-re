using System.Diagnostics;
using System.Security.Principal;

namespace Launcher;

public static class AdminRelauncher {
    public static void RelaunchIfNotAdmin() {
        if(RunningAsAdmin())
            return;
        
        try {
            var proc = new ProcessStartInfo {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Environment.ProcessPath,
                Verb = "runas"
            };
            Process.Start(proc);
        } catch(Exception ex) {
            MessageBox.Show($"This program must be run as an administrator! \n\n{ex}");
        }
        Environment.Exit(0);
    }

    private static bool RunningAsAdmin() {
        try {
            var user = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(user);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        } catch {
            return false;
        }
    }
}
