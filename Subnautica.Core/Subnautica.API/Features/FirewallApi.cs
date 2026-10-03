namespace Subnautica.API.Features
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Subnautica.API.Extensions;
    using Subnautica.API.Features.Helper;
    
    public class FirewallApi
    {
        /**
         *
         * IsInitialized değerini barındırır.
         *
         * @author Ismail <ismaiil_0234@hotmail.com>
         *
         */
        private static bool IsInitialized { get; set; } = true;

        /**
         *
         * Son çıktıyı barındırır.
         *
         * @author Ismail <ismaiil_0234@hotmail.com>
         *
         */
        public static string LastOutput { get; private set; } = "";

        /**
         *
         * Son hatayı barındırır.
         *
         * @author Ismail <ismaiil_0234@hotmail.com>
         *
         */
        public static string LastError { get; private set; } = "";

        /**
         *
         * SubnauticaBelowZeroDescription değerini barındırır.
         *
         * @author Ismail <ismaiil_0234@hotmail.com>
         *
         */
        private static string SubnauticaBelowZeroDescription { get; set; } = "Subnautica BZ Multiplayer by BOT Benson";

        /**
         *
         * SubnauticaBelowZeroId değerini barındırır.
         *
         * @author Ismail <ismaiil_0234@hotmail.com>
         *
         */
        private static string SubnauticaBelowZeroId { get; set; } = "subnauticazero";

        /**
         *
         * Subnautica Firewall Kurulumunu yapar.
         *
         * @author Ismail <ismaiil_0234@hotmail.com>
         *
         */
        public static void SetupFirewallWithAdminPerms(string filePath)
        {
            FirewallApi.ExecuteCommand($@"Subnautica.Firewall.exe ""{filePath}""", true, false);
        }

        /**
         *
         * Subnautica Firewall Kurulumunu yapar.
         *
         * @author Ismail <ismaiil_0234@hotmail.com>
         *
         */
        public static void SetupSubnauticaFirewall(string filePath)
        {
            filePath = filePath.Replace("/", "\\");

            FirewallApi.ExecuteCommand($@"advfirewall firewall delete rule name=""{FirewallApi.SubnauticaBelowZeroId}""");
            FirewallApi.ExecuteCommand($@"advfirewall firewall add rule name=""{FirewallApi.SubnauticaBelowZeroId}"" description=""{FirewallApi.SubnauticaBelowZeroDescription}"" dir=in action=allow program=""{filePath}"" enable=yes");
        }

        /**
         *
         * Yönetici izni (UAC) isteyerek güvenlik duvarı kuralını ekler.
         * Oyun yönetici olarak çalışmadığında kullanılır.
         *
         */
        public static void SetupFirewallElevated(string filePath)
        {
            filePath = filePath.Replace("/", "\\");

            var arguments = string.Format(
                @"advfirewall firewall add rule name=""{0}"" description=""{1}"" dir=in action=allow program=""{2}"" enable=yes",
                FirewallApi.SubnauticaBelowZeroId,
                FirewallApi.SubnauticaBelowZeroDescription,
                filePath
            );

            // Önce eski kural silinir, sonra yenisi eklenir. İkisi tek UAC isteğiyle çalışsın diye cmd kullanılır.
            var command = string.Format(
                @"/c netsh advfirewall firewall delete rule name=""{0}"" & netsh {1}",
                FirewallApi.SubnauticaBelowZeroId,
                arguments
            );

            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName        = "cmd.exe";
            process.StartInfo.Arguments       = command;
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.Verb            = "runas";
            process.StartInfo.WindowStyle     = System.Diagnostics.ProcessWindowStyle.Hidden;
            process.Start();
            process.WaitForExit(15000);
        }

        /**
         *
         * Kurallar doğru yapılandırılmış mı?
         *
         * @author Ismail <ismaiil_0234@hotmail.com>
         *
         */
        public static bool IsSubnauticaFirewallOk(string path)
        {
            path = path.Replace("/", "\\");

            var rules = FirewallApi.GetSubnauticaRules();
            if (rules.Count != 1)
            {
                return false;
            }

            return rules.Any(q => q.IsEnabled && q.IsUdp && q.IsTcp && q.IsPublicProfile && q.IsPrivateProfile && q.IsDomainProfile && q.IsAllow && q.Path == path && q.Description == FirewallApi.SubnauticaBelowZeroDescription);
        }

        /**
         *
         * Subnautica Kuralları döner.
         *
         * @author Ismail <ismaiil_0234@hotmail.com>
         *
         */
        public static List<FirewallItemFormat> GetSubnauticaRules()
        {
            return GetRules(FirewallApi.SubnauticaBelowZeroId);
        }

        /**
         *
         * Kuralları döner.
         *
         * @author Ismail <ismaiil_0234@hotmail.com>
         *
         */
        public static List<FirewallItemFormat> GetRules(string name)
        {
            var items = new List<FirewallItemFormat>();

            try
            {
                // Late binding (IDispatch): no NetFwTypeLib interop assembly has to be shipped.
                // Values: Action Allow = 1, Protocol Any = 256 / TCP = 6 / UDP = 17,
                // Profiles Domain = 1 / Private = 2 / Public = 4 / All = 0x7FFFFFFF.
                var tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
                dynamic fwPolicy2 = Activator.CreateInstance(tNetFwPolicy2);

                foreach (dynamic rule in fwPolicy2.Rules)
                {
                    if ((string) rule.Name == name)
                    {
                        int profile  = (int) rule.Profiles;
                        int protocol = (int) rule.Protocol;
                        bool isAll   = (profile & 0x7FFFFFFF) == 0x7FFFFFFF;

                        items.Add(new FirewallItemFormat() {
                            Name        = (string) rule.Name,
                            Description = (string) rule.Description,
                            Path        = (string) rule.ApplicationName,
                            IsEnabled   = (bool) rule.Enabled,
                            IsAllow     = (int) rule.Action == 1,
                            IsTcp       = protocol == 256 || protocol == 6,
                            IsUdp       = protocol == 256 || protocol == 17,

                            IsPrivateProfile = isAll || (profile & 2) != 0,
                            IsPublicProfile  = isAll || (profile & 4) != 0,
                            IsDomainProfile  = isAll || (profile & 1) != 0,
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Is Firewall Ok?: {ex}");
            }

            return items;
        }

        /**
         *
         * komut çalıştırır.
         *
         * @author Ismail <ismaiil_0234@hotmail.com>
         *
         */
        private static void ExecuteCommand(string command, bool useCorePath = false, bool isNetShCommand = true, bool silence = true)
        {
            try
            {
                var process = new System.Diagnostics.Process();

                if (isNetShCommand)
                {
                    process.StartInfo.FileName  = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "netsh.exe");
                    process.StartInfo.Arguments = command;
                }
                else
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = string.Format("/c {0}", command);
                }

                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError  = true;
                process.StartInfo.UseShellExecute = false;

                if (silence)
                {
                    process.StartInfo.WindowStyle    = System.Diagnostics.ProcessWindowStyle.Hidden;
                    process.StartInfo.CreateNoWindow = true;
                }

                if (useCorePath)
                {
                    process.StartInfo.WorkingDirectory = Paths.GetLauncherGameCorePath();
                }

                process.Start();

                var strOutput = process.StandardOutput.ReadToEnd();
                var strError  = process.StandardError.ReadToEnd();

                process.WaitForExit(5000);

                LastOutput = strOutput?.Trim();
                LastError  = strError?.Trim();
            }
            catch (Exception ex)
            {
                LastOutput = "";
                LastError = $"System.Exception: {ex.Message}:{ex.InnerException}";
            }

            if (LastError.IsNotNull())
            {
                Log.Error($"FirewallApi Exception: {LastError}, SC: " + silence);
            }
        }
    }
}
