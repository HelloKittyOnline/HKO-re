namespace Launcher;

internal static class Program {
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main() {
        if(!File.Exists("./hko.exe")) { // restart as admin if not in portable mode
            AdminRelauncher.RelaunchIfNotAdmin();
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Form1());
    }
}
