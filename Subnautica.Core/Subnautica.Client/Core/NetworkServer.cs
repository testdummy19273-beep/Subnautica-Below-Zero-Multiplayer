namespace Subnautica.Client.Core
{
    using Subnautica.API.Features;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class NetworkServer
    {
        public static int DefaultPort => Settings.ModConfig.HostOnPort.GetInt() == -1 ? 7777 : Settings.ModConfig.HostOnPort.GetInt();

        public const string DefaultLocalIpAddress = "127.0.0.1";

        public static int DefaultMaxPlayer => Settings.ModConfig.MaxPlayer.GetInt() == -1 ? 8 : Settings.ModConfig.MaxPlayer.GetInt();

        public static bool IsConnecting()
        {
            return Server.Core.Server.Instance != null && Server.Core.Server.Instance.IsConnecting;
        }

        public static bool IsConnected()
        {
            return Server.Core.Server.Instance != null && Server.Core.Server.Instance.IsConnected;
        }

        public static string CreateServerId()
        {
            List<HostServerItem> servers = GetHostServerList();

            while (true)
            {
                string serverId = Guid.NewGuid().ToString();

                if (!servers.Where(q => q.Id == serverId).Any())
                {
                    return serverId;
                }
            }
        }

        public static string CreateNewServer(GameModePresetId gameModeId)
        {
            var serverId = CreateServerId();
            var serverPath = Paths.GetMultiplayerServerSavePath(serverId, "config.json");

            Directory.CreateDirectory(Path.GetDirectoryName(serverPath));

            HostServerItem serverItem = new HostServerItem()
            {
                GameMode = (int)gameModeId,
                CreationDate = Tools.GetUnixTime(),
                LastPlayedDate = Tools.GetUnixTime(),
            };

            File.WriteAllText(serverPath, Newtonsoft.Json.JsonConvert.SerializeObject(serverItem));
            return serverId;
        }

        public static bool StartServer(string serverId, string ownerId)
        {
            var data = GetHostServerList().FirstOrDefault(q => q.Id == serverId);
            if (data == null)
            {
                return false;
            }

            try
            {
                NetworkServer.AbortServer();

                Server.Core.Server server = new Server.Core.Server(data.Id, data.GetGameMode(), NetworkServer.DefaultPort, NetworkServer.DefaultMaxPlayer, Tools.CreateMD5(ownerId), Tools.GetLauncherVersion());
                server.Start();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

            return true;
        }

        public static void AbortServer(bool isEndGame = false)
        {
            Network.Session.Dispose();

            if (Server.Core.Server.Instance != null)
            {
                Server.Core.Server.Instance.Dispose(isEndGame);
                Server.Core.Server.Instance = null;
            }
        }

        public static void UpdateConstructionSync(byte[] constructionData)
        {
            if (NetworkServer.IsConnected())
            {
                Server.Core.Server.Instance.Storages.World.UpdateConstructions(constructionData);
            }
        }

        public static List<LocalServerItem> GetLocalServerList()
        {
            var serverListPath = Paths.GetGameServersPath();
            if (File.Exists(serverListPath))
            {
                var servers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<LocalServerItem>>(File.ReadAllText(serverListPath));
                if (servers == null)
                {
                    return new List<LocalServerItem>();
                }

                return servers;
            }

            SaveLocalServerList(new List<LocalServerItem>());
            return GetLocalServerList();
        }

        public static void SaveLocalServerList(List<LocalServerItem> serverList)
        {
            try
            {
                File.WriteAllText(Paths.GetGameServersPath(), Newtonsoft.Json.JsonConvert.SerializeObject(serverList));
            }
            catch (Exception e)
            {
                Log.Error($"NetworkServer.SaveLocalServerList Exception: {e}");
            }
        }

        public static List<HostServerItem> GetHostServerList()
        {
            string serverPath = Paths.GetMultiplayerServerSavePath();

            Directory.CreateDirectory(serverPath);

            List<HostServerItem> servers = new List<HostServerItem>();

            foreach (var path in Directory.GetDirectories(serverPath))
            {
                string serverId = Path.GetFileName(path);
                if (string.IsNullOrEmpty(serverId))
                {
                    continue;
                }

                try
                {
                    string serverConfigPath = Paths.GetMultiplayerServerSavePath(serverId, "config.json");

                    if (File.Exists(serverConfigPath))
                    {
                        HostServerItem server = Newtonsoft.Json.JsonConvert.DeserializeObject<HostServerItem>(File.ReadAllText(serverConfigPath));
                        server.Id = serverId;

                        servers.Add(server);
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"NetworkServer.GetHostServerList Exception: {e}");
                }
            }

            return servers;
        }
    }
}
