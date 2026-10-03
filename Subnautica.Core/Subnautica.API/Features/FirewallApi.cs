namespace Subnautica.API.Features
{
    using System;
    using System.Diagnostics;
    using System.IO;

    public class FirewallApi
    {
        private const string RuleName = "subnauticazero";

        private const string RuleDescription = "Subnautica BZ Multiplayer by BOT Benson";

        /**
         *
         * Is there already an allow rule for this exe? Plain netsh (no admin needed, no COM, no `dynamic`:
         * the game's Mono has no Microsoft.CSharp.dll). The rule's program path is language independent.
         *
         */
        public static bool IsSubnauticaFirewallOk(string path)
        {
            path = path.Replace("/", "\\");

            try
            {
                var process = new Process();
                process.StartInfo.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "netsh.exe");
                process.StartInfo.Arguments = string.Format("advfirewall firewall show rule name=\"{0}\" verbose", RuleName);
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.Start();

                var output = process.StandardOutput.ReadToEnd();
                process.StandardError.ReadToEnd();
                process.WaitForExit(5000);

                return process.ExitCode == 0 && output.IndexOf(path, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch (Exception ex)
            {
                Log.Error($"FirewallApi.IsSubnauticaFirewallOk: {ex.Message}");
                return false;
            }
        }

        /**
         *
         * Adds the allow rule, asking for administrator permission (UAC) once.
         *
         */
        public static void SetupFirewallElevated(string filePath)
        {
            filePath = filePath.Replace("/", "\\");

            // Delete the old rule and add the new one in a single elevated cmd, so there is only one UAC prompt.
            var command = string.Format(
                @"/c netsh advfirewall firewall delete rule name=""{0}"" & netsh advfirewall firewall add rule name=""{0}"" description=""{1}"" dir=in action=allow program=""{2}"" enable=yes",
                RuleName,
                RuleDescription,
                filePath
            );

            var process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = command;
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.Verb = "runas";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.Start();
            process.WaitForExit(15000);
        }
    }
}
