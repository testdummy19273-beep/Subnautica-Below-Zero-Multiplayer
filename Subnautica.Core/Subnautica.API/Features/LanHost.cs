namespace Subnautica.API.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    using Subnautica.API.Extensions;

    /**
     *
     * Direct (LAN / same network) hosting helpers. No lobby server or VPN is involved:
     * the host runs the server inside the game and friends connect to the host's IP address.
     *
     */
    public static class LanHost
    {
        /**
         *
         * Firewall kuralı bu oturumda denendi mi?
         *
         */
        private static bool IsFirewallChecked { get; set; } = false;

        /**
         *
         * Bu bilgisayarın yerel ağ (LAN) IPv4 adreslerini döner.
         *
         */
        public static List<string> GetLocalIpAddresses()
        {
            var addresses = new List<string>();

            try
            {
                foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (networkInterface.OperationalStatus != OperationalStatus.Up)
                    {
                        continue;
                    }

                    if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                    {
                        continue;
                    }

                    foreach (var unicast in networkInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (unicast.Address.AddressFamily != AddressFamily.InterNetwork)
                        {
                            continue;
                        }

                        var ip = unicast.Address.ToString();

                        // 169.254.x.x = link-local (no DHCP), not reachable by friends.
                        if (ip.StartsWith("169.254.") || addresses.Contains(ip))
                        {
                            continue;
                        }

                        addresses.Add(ip);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"LanHost.GetLocalIpAddresses Exception: {ex}");
            }

            return addresses;
        }

        /**
         *
         * Arkadaşların girmesi gereken adresleri "ip:port" formatında döner.
         *
         */
        public static string GetJoinAddressText(int port)
        {
            var addresses = GetLocalIpAddresses();
            if (addresses.Count <= 0)
            {
                return ZeroLanguage.Get("GAME_LAN_NO_IP_FOUND", "No network connection found");
            }

            return string.Join("\n", addresses.Select(q => string.Format("{0}:{1}", q, port)).ToArray());
        }

        /**
         *
         * Kullanıcının girdiği "ip" ya da "ip:port" metnini ayrıştırır.
         *
         */
        public static bool TryParseAddress(string text, int defaultPort, out string host, out int port)
        {
            host = null;
            port = defaultPort;

            if (text.IsNull())
            {
                return false;
            }

            text = text.Trim();

            var parts = text.Split(':');
            if (parts.Length > 2)
            {
                return false;
            }

            if (parts.Length == 2)
            {
                if (!int.TryParse(parts[1], out port) || port < 1 || port > 65535)
                {
                    return false;
                }
            }

            host = parts[0].Trim();
            return host.Length > 0 && (IPAddress.TryParse(host, out _) || Uri.CheckHostName(host) == UriHostNameType.Dns);
        }

        /**
         *
         * Oyun exe'si için gelen bağlantıları (UDP/TCP) izin veren güvenlik duvarı kuralının
         * olduğundan emin olur. Kural yoksa bir kez yönetici izni (UAC) ister.
         *
         */
        public static void EnsureFirewallRule()
        {
            if (IsFirewallChecked || !Settings.ModConfig.ConfigureFirewall.GetBool(true))
            {
                return;
            }

            IsFirewallChecked = true;

            var exePath = Paths.GetGameExePath();
            if (exePath.IsNull())
            {
                Log.Info("LanHost.EnsureFirewallRule: Game exe path not found, skipping. Set GameExePath in Config.json.");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    if (!FirewallApi.IsSubnauticaFirewallOk(exePath))
                    {
                        FirewallApi.SetupFirewallElevated(exePath);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"LanHost.EnsureFirewallRule Exception: {ex}");
                }
            });
        }
    }
}
