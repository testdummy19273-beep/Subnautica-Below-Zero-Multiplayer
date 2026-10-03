namespace Subnautica.Client.Core
{
    using LiteNetLib;
    using Subnautica.API.Enums;
    using Subnautica.API.Features;
    using Subnautica.Client.MonoBehaviours.General;
    using Subnautica.Network.Extensions;
    using Subnautica.Network.Models.Core;
    using Subnautica.Network.Models.Server;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class NetworkClient
    {
        public static string IpAddress { get; set; }

        public static int PortNumber { get; set; }

        public static bool IsConnectingToServer { get; set; } = false;

        public static bool IsConnectedToServer { get; set; } = false;

        public static bool IsSafeDisconnecting { get; set; } = false;

        public static NetManager Client { get; set; }

        public static Queue<ConnectionSignal> ConnectionSignalDataQueues { get; set; } = new Queue<ConnectionSignal>();

        public static void Connect(string ipAddress, int port, bool officialServerConnect = true, bool retryConnect = true)
        {
            if (IsConnectedToServer || IsConnectingToServer)
            {
                return;
            }
            Log.Info($"Connecting on {ipAddress} {port} ");
            if (!officialServerConnect && retryConnect)
            {
                ErrorMessage.AddMessage($"Connecting to {ipAddress}:{port}...");
            }
            NetworkClient.IpAddress = ipAddress;
            NetworkClient.PortNumber = port;

            try
            {
                NetworkClient.IsConnectingToServer = true;
                NetworkClient.IsConnectedToServer = false;
                NetworkClient.IsSafeDisconnecting = false;
                NetworkClient.ClearSignals();

                if (Client == null)
                {
                    Client = new NetManager(new ClientListener())
                    {
                        UpdateTime = 1,
                        AutoRecycle = true,
                        UnsyncedReceiveEvent = true,
                        UnsyncedDeliveryEvent = true,
                        UnsyncedEvents = true,
                        ChannelsCount = Network.GetChannelCount(),
                        IPv6Enabled = false,
                    };
                }

                if (Client.Start())
                {
                    Client.Connect(ipAddress, port, Tools.GetLauncherVersion());
                }
                else
                {
                    Log.Error($"NetworkClient.Connect.Start: NOT POSSIBLE");
                }
            }
            catch (TimeoutException e)
            {
                Log.Error($"NetworkClient.Connect.TimeoutException: {e}");
            }
            catch (Exception e)
            {
                Log.Error($"NetworkClient.Connect.Exception: {e}");
            }

            if (retryConnect)
            {
                NetworkClient.CheckConnectionAndJoinServer(officialServerConnect);
            }
        }

        public static void CheckConnectionAndJoinServer(bool officialServerConnect)
        {
            UWE.CoroutineHost.StartCoroutine(SubCheckConnectionAndJoinServer(officialServerConnect));
        }

        private static IEnumerator SubCheckConnectionAndJoinServer(bool officialServerConnect)
        {
            yield return new WaitForSecondsRealtime(0.1f);

            // LiteNetLib keeps trying for about 5 s; over a VPN (ZeroTier...) the first packets are slow, so wait that long and more.
            int maxWait = officialServerConnect ? 50 : 100;
            int maxRetry = officialServerConnect ? 3 : 1;

            for (int j = 0; j < maxRetry; j++)
            {
                Log.Info($"Connection Trying ({j + 1}) ");

                for (int i = 0; i < maxWait; i++)
                {
                    yield return new WaitForSecondsRealtime(0.1f);

                    ConnectionSignals.ConsumeQueue();

                    Log.Info($"WAIT ({i}): " + NetworkClient.IsConnected());

                    if (NetworkClient.IsConnected())
                    {
                        NetworkClient.JoinServer(Tools.GetLoggedInName(), Tools.GetLoggedId());
                        break;
                    }
                    else if (NetworkClient.IsDisconnected())
                    {
                        NetworkClient.ClearSignals();
                        break;
                    }
                }

                if (NetworkClient.IsConnected() || NetworkClient.IsDisconnected() || maxRetry == j + 1)
                {
                    break;
                }

                if (officialServerConnect)
                {
                    NetworkClient.IsSafeDisconnecting = true;
                    NetworkClient.Disconnect();

                    yield return new WaitForSecondsRealtime(1f);

                    NetworkClient.IsSafeDisconnecting = false;
                    NetworkClient.Connect(NetworkClient.IpAddress, NetworkClient.PortNumber, officialServerConnect, false);
                }
            }

            if (!NetworkClient.IsConnectedToServer && NetworkClient.IsConnectingToServer)
            {
                Log.Error($"Connection to {NetworkClient.IpAddress}:{NetworkClient.PortNumber} failed.");
                ErrorMessage.AddMessage(API.Features.ZeroLanguage.Get("GAME_SERVER_CONNECTING_ERROR") + $" ({NetworkClient.IpAddress}:{NetworkClient.PortNumber})");
                NetworkClient.Disconnect();

                ZeroGame.StopLoadingScreen();
            }
        }

        public static bool IsConnected()
        {
            if (NetworkClient.Client == null || NetworkClient.IsConnectingToServer)
            {
                return false;
            }

            return NetworkClient.IsConnectedToServer;
        }

        public static bool IsDisconnected()
        {
            return NetworkClient.Client == null;
        }

        public static bool Disconnect(bool isEndGame = false)
        {
            NetworkServer.AbortServer(isEndGame);

            IsConnectingToServer = false;
            IsConnectedToServer = false;

            if (Client == null)
            {
                return false;
            }

            Client.DisconnectAll();
            Client.Stop();
            Client = null;
            return true;
        }

        public static bool SendPacket(NetworkPacket packet)
        {
            if (IsConnected() && !EventBlocker.IsEventBlocked(packet.Type))
            {
                NetworkClient.Client.SendToAll(packet.Serialize(), packet.ChannelId, packet.DeliveryMethod);
                return true;
            }

            return false;
        }

        public static void JoinServer(string username, string userId)
        {
            try
            {
                MultiplayerChannelProcessor.AddToPlayerMultiplayerProcessors();

                JoiningServerArgs packet = new JoiningServerArgs()
                {
                    UserName = username,
                    UserId = userId
                };

                NetworkClient.SendPacket(packet);
            }
            catch (Exception ex)
            {
                Log.Error($"NetworkClient.JoinServer - Exception: {ex}");
            }
        }

        public static void ClearSignals()
        {
            ConnectionSignalDataQueues.Clear();
        }
    }
}
