namespace Subnautica.Server.Core
{
    using LiteNetLib;
    using Subnautica.API.Enums;
    using Subnautica.API.Extensions;
    using Subnautica.API.Features;
    using Subnautica.Events.Handlers;
    using Subnautica.Network.Core;
    using Subnautica.Network.Extensions;
    using Subnautica.Network.Models.Core;
    using Subnautica.Network.Structures;
    using Subnautica.Server.Abstracts;
    using Subnautica.Server.Events;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using EventHandlers = Subnautica.Events.Handlers;
    using ServerHandlers = Subnautica.Server.Events.Handlers;

    public class Server
    {
        public static bool DEBUG { get; set; } = true;

        public static Server Instance { get; set; }

        public int Port { get; set; }

        public int MaxPlayer { get; set; }

        public bool IsConnecting { get; set; }

        public bool IsConnected { get; set; }

        public string ServerId { get; set; }

        public string OwnerId { get; set; }

        public string SavePath { get; set; }

        public byte CurrentPlayerId { get; set; } = 0;

        public string Version { get; set; }

        public GameModePresetId GameMode { get; set; }

        public Dictionary<string, AuthorizationProfile> Players { get; set; }

        public Storages Storages { get; set; }

        private NetManager NetworkServer { get; set; }

        private GameObject ServerGameObject { get; set; }

        private bool IsRegisteredEvents { get; set; } = false;

        public Logices Logices { get; set; }

        public Server(string serverId, GameModePresetId gameModeId, int port, int maxPlayer, string ownerId, string version)
        {
            Instance = this;

            this.Dispose();

            this.Port = port;
            this.MaxPlayer = maxPlayer;
            this.ServerId = serverId;
            this.OwnerId = ownerId;
            this.Version = version;
            this.GameMode = gameModeId;
            this.SavePath = Paths.GetMultiplayerServerSavePath(this.ServerId);
            this.Players = new Dictionary<string, AuthorizationProfile>();

            this.ServerGameObject = new GameObject(serverId);

            this.Logices = this.ServerGameObject.AddComponent<Logices>();

            this.Storages = new Storages();
            this.Storages.Start(this.ServerId);

            this.StartProcessors();
            this.RegisterEvents();
        }

        public void Start()
        {
            try
            {
                this.IsConnecting = true;

                Log.Info("Starting Server...");
                Log.Info(String.Format("Server Port: {0}", this.Port));

                this.NetworkServer = new NetManager(new ServerListener())
                {
                    UpdateTime = 1,
                    AutoRecycle = true,
                    UnsyncedReceiveEvent = true,
                    UnsyncedDeliveryEvent = true,
                    UnsyncedEvents = true,
                    ChannelsCount = Network.GetChannelCount(),
                    IPv6Enabled = false,
                };

                this.NetworkServer.Start(this.Port);

                this.IsConnecting = false;
                this.IsConnected = true;

                Log.Info("Started Server.");
                Log.Info("Waiting for players");
            }
            catch (Exception e)
            {
                this.IsConnecting = false;
                this.IsConnected = false;
                Log.Error($"Server.Start Exception: {e}");
            }
        }

        public NetManager GetNetworkServer()
        {
            return this.NetworkServer;
        }

        public int GetConnectedPeerCount()
        {
            return this.NetworkServer.ConnectedPeersCount;
        }

        public byte GetNextPlayerId()
        {
            if (this.Players.Count > 250)
            {
                return 251;
            }

            while (true)
            {
                ++this.CurrentPlayerId;

                if (this.CurrentPlayerId > 250)
                {
                    this.CurrentPlayerId = 1;
                }

                if (this.HasPlayer(this.CurrentPlayerId))
                {
                    continue;
                }

                return this.CurrentPlayerId;
            }
        }

        public static void SendPacketToOtherClients(AuthorizationProfile profile, NetworkPacket packet, bool checkConnected = false)
        {
            if (Server.Instance.IsLogablePacket(packet.Type))
            {
                Log.Info($"PACKET SENDED: [Length: {NetworkTools.Serialize(packet).Length}] -> {packet.Type}");
            }

            foreach (var player in Server.Instance.Players.Values.Where(q => q.IpPortAddress != profile.IpPortAddress))
            {
                if (checkConnected && !player.IsFullConnected)
                {
                    continue;
                }

                SendPacket(player.IpPortAddress, packet);
            }
        }

        public static void SendPacketToAllClient(NetworkPacket packet, bool checkConnected = false)
        {
            if (Server.Instance.IsLogablePacket(packet.Type))
            {
                Log.Info($"PACKET SENDED: [Length: {NetworkTools.Serialize(packet).Length}] -> {packet.Type}");
            }

            foreach (var player in Server.Instance.Players)
            {
                if (checkConnected && !player.Value.IsFullConnected)
                {
                    continue;
                }

                SendPacket(player.Value.IpPortAddress, packet);
            }
        }

        public static void SendPacket(AuthorizationProfile profile, NetworkPacket packet)
        {
            SendPacket(profile.IpPortAddress, packet);
        }

        public static bool SendPacket(string ipPort, NetworkPacket packet)
        {
            if (Server.Instance.Players.TryGetValue(ipPort, out var profile))
            {
                if (profile.NetPeer != null && profile.NetPeer.ConnectionState == ConnectionState.Connected)
                {
                    profile.NetPeer.Send(packet.Serialize(), packet.ChannelId, packet.DeliveryMethod);

                    Server.Instance.NetworkServer.TriggerUpdate();
                    return true;
                }
            }

            return false;
        }

        public static bool DisconnectToClient(AuthorizationProfile authorization)
        {
            return DisconnectToClient(authorization.IpPortAddress);
        }

        public static bool DisconnectToClient(string ipPort)
        {
            if (Server.Instance.NetworkServer != null)
            {
                foreach (var peer in Server.Instance.NetworkServer.ConnectedPeerList)
                {
                    if (peer.ToString() == ipPort)
                    {
                        peer.Disconnect();
                        return true;
                    }
                }
            }

            return false;
        }

        public List<AuthorizationProfile> GetPlayers()
        {
            return this.Players.Values.ToList();
        }

        public byte GetPlayerCount()
        {
            return (byte)this.Players.Count;
        }

        public AuthorizationProfile GetServerOwner()
        {
            return this.GetPlayer(this.OwnerId);
        }

        public AuthorizationProfile GetPlayer(byte playerId)
        {
            return this.Players.FirstOrDefault(q => q.Value.PlayerId == playerId).Value;
        }

        public AuthorizationProfile GetPlayer(string uniqueId)
        {
            return this.Players.FirstOrDefault(q => q.Value.UniqueId == uniqueId).Value;
        }

        public bool HasPlayer(string uniqueId)
        {
            return this.Players.ContainsKey(uniqueId);
        }

        public bool HasPlayer(byte playerId)
        {
            return this.Players.Any(q => q.Value.PlayerId == playerId);
        }

        private void StartProcessors()
        {
            foreach (BaseProcessor baseProcessor in ProcessorShared.GetAllProcessors())
            {
                baseProcessor.OnStart();
            }
        }
        private void RegisterEvents()
        {
            if (!this.IsRegisteredEvents)
            {
                this.IsRegisteredEvents = true;

                EventHandlers.Game.PowerSourceAdding += this.Logices.PowerConsumer.OnPowerSourceAdding;
                EventHandlers.Game.PowerSourceRemoving += this.Logices.PowerConsumer.OnPowerSourceRemoving;
                EventHandlers.Game.EntityDistributionLoaded += this.Logices.WorldStreamer.OnEntityDistributionLoaded;

                EventHandlers.Building.BaseHullStrengthCrushing += this.Logices.BaseHullStrength.OnCrushing;

                Furnitures.PlanterStorageReseting += this.Logices.BaseWaterPark.OnPlanterStorageReseting;

                ServerHandlers.PlayerFullConnected += this.Logices.CreatureWatcher.OnPlayerFullConnected;
                ServerHandlers.PlayerFullConnected += this.Logices.BaseWaterPark.OnPlayerFullConnected;

                MainGameController.OnGameStarted.AddHandler(new Action(this.Logices.PowerConsumer.OnGameStart));
            }
        }

        private void UnRegisterEvents()
        {
            if (this.IsRegisteredEvents)
            {
                this.IsRegisteredEvents = false;

                EventHandlers.Game.PowerSourceAdding -= this.Logices.PowerConsumer.OnPowerSourceAdding;
                EventHandlers.Game.PowerSourceRemoving -= this.Logices.PowerConsumer.OnPowerSourceRemoving;
                EventHandlers.Game.EntityDistributionLoaded -= this.Logices.WorldStreamer.OnEntityDistributionLoaded;

                EventHandlers.Building.BaseHullStrengthCrushing -= this.Logices.BaseHullStrength.OnCrushing;

                Furnitures.PlanterStorageReseting -= this.Logices.BaseWaterPark.OnPlanterStorageReseting;

                ServerHandlers.PlayerFullConnected -= this.Logices.BaseWaterPark.OnPlayerFullConnected;
                ServerHandlers.PlayerFullConnected -= this.Logices.CreatureWatcher.OnPlayerFullConnected;

                MainGameController.OnGameStarted.RemoveHandler(new Action(this.Logices.PowerConsumer.OnGameStart));
            }
        }

        public bool IsLogablePacket(ProcessType type)
        {
            if (!Core.Server.DEBUG)
            {
                return false;
            }

            switch (type)
            {
                case ProcessType.PlayerUpdated:
                case ProcessType.PlayerStats:
                case ProcessType.WorldDynamicEntityPosition:
                case ProcessType.WorldCreaturePosition:
                case ProcessType.VehicleUpdated:
                case ProcessType.PlayerAnimationChanged:
                case ProcessType.EnergyTransmission:
                case ProcessType.VehicleEnergyTransmission:
                case ProcessType.Ping:
                case ProcessType.CreatureAnimation:
                    return false;
            }

            return true;
        }

        public void Dispose(bool isEndGame = false)
        {
            this.UnRegisterEvents();

            if (this.Logices != null)
            {
                this.Logices.AutoSave.SaveAll();
            }

            if (this.NetworkServer != null)
            {
                this.NetworkServer.DisconnectAll();
                this.NetworkServer.Stop();
                this.NetworkServer = null;
            }

            if (isEndGame)
            {
                this.Logices.StoryTrigger.ResetEndGame();
                this.Logices.AutoSave.SaveAll();

                foreach (var player in this.Storages.Player.GetAllPlayers())
                {
                    player.SetPosition(new ZeroVector3(-235.4f, 6.5f, 163.5f), new ZeroQuaternion(0.0f, -0.9f, 0.0f, 0.4f));
                    player.SaveToDisk();
                }
            }

            this.Port = 0;
            this.MaxPlayer = 0;
            this.IsConnecting = false;
            this.IsConnected = false;
            this.ServerId = null;
            this.OwnerId = null;
            this.SavePath = null;
            this.Players = null;

            if (this.Storages != null)
            {
                this.Storages.Dispose();
                this.Storages = null;
            }

            if (this.ServerGameObject != null)
            {
                this.ServerGameObject.Destroy();
                this.ServerGameObject = null;
            }
        }
    }
}
