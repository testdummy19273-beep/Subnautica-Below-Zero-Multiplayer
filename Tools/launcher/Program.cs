using System;
using System.IO;
using System.Windows.Forms;

namespace BZLauncher
{
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            // Scripting mode: SubnauticaBZ-Launcher.exe --cli <status|install|uninstall> "<game folder>"
            // Output goes to launcher-cli.log next to the exe (the exe has no console).
            if (args.Length >= 3 && args[0] == "--cli")
            {
                return RunCli(args[1], args[2]);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(args.Length >= 2 && args[0] == "--tab" ? args[1] : "Play"));
            return 0;
        }

        static int RunCli(string command, string gamePath)
        {
            var logFile = Path.Combine(Paths.Base, "launcher-cli.log");
            Action<string> log = line => File.AppendAllText(logFile, line + Environment.NewLine);

            try
            {
                var game = GameLocator.Resolve(gamePath);
                if (game == null) { log("Game not found: " + gamePath); return 2; }

                switch (command)
                {
                    case "status":
                        log("state=" + Installer.GetState(game));
                        break;
                    case "install":
                        Installer.Install(game, log);
                        log("state=" + Installer.GetState(game));
                        break;
                    case "uninstall":
                        Installer.Uninstall(game, log);
                        log("state=" + Installer.GetState(game));
                        break;
                    default:
                        log("Unknown command: " + command);
                        return 2;
                }
                return 0;
            }
            catch (Exception ex)
            {
                log("ERROR: " + ex.Message);
                return 1;
            }
        }
    }
}
