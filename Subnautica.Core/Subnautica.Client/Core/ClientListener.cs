namespace Subnautica.Client.Core
{
    using LiteNetLib;
    using Subnautica.API.Enums;
    using Subnautica.API.Features;
    using Subnautica.Client.MonoBehaviours.General;
    using Subnautica.Network.Extensions;
    using Subnautica.Network.Models.Client;
    using System;
    using System.Net;
    using System.Net.Sockets;

    public class ClientListener : INetEventListener
    {
        public void OnPeerConnected(NetPeer peer)
        {
            NetworkClient.IsConnectedToServer = true;
            NetworkClient.IsConnectingToServer = false;

            NetworkClient.ConnectionSignalDataQueues.Enqueue(ConnectionSignal.Connected);
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {

        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            NetworkClient.IsConnectedToServer = false;
            NetworkClient.IsConnectingToServer = false;

            Log.Info($"Client disconnected from {NetworkClient.IpAddress}:{NetworkClient.PortNumber}, reason: {disconnectInfo.Reason}");

            var rejectType = this.GetRejectType(disconnectInfo);
            if (rejectType == ConnectionSignal.ServerFull)
            {
                NetworkClient.ConnectionSignalDataQueues.Enqueue(ConnectionSignal.ServerFull);
            }
            else if (rejectType == ConnectionSignal.VersionMismatch)
            {
                NetworkClient.ConnectionSignalDataQueues.Enqueue(ConnectionSignal.VersionMismatch);
            }
            else
            {
                if (disconnectInfo.Reason == DisconnectReason.ConnectionRejected)
                {
                    NetworkClient.ConnectionSignalDataQueues.Enqueue(ConnectionSignal.Rejected);
                }
                else if (!NetworkClient.IsSafeDisconnecting)
                {
                    Log.Info("OnPeerDisconnected - Reason: " + disconnectInfo.Reason);
                    NetworkClient.ConnectionSignalDataQueues.Enqueue(ConnectionSignal.Disconnected);
                }
            }
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            try
            {
                var packet = reader.GetPacket();

                if (MultiplayerChannelProcessor.Processors.TryGetValue(packet.ChannelType, out var processor))
                {
                    processor.AddPacket(packet);
                }
                else
                {
                    Log.Error(string.Format("[OnClientDataReceived.NetworkReceiveEvent] Channel Not Found: {0}", packet.ChannelType));
                }
            }
            catch (Exception e)
            {
                Log.Error($"[OnClientDataReceived.NetworkReceiveEvent] Exception: {e}");
            }
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {

        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {

        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {

        }

        private ConnectionSignal GetRejectType(DisconnectInfo disconnectInfo)
        {
            try
            {
                if (disconnectInfo.AdditionalData.AvailableBytes > 0)
                {
                    var packet = disconnectInfo.AdditionalData.GetPacket()?.GetPacket<ConnectionRejectArgs>();
                    if (packet != null)
                    {
                        return packet.RejectType;
                    }
                }
            }
            catch (Exception)
            {

            }

            return ConnectionSignal.Unknown;
        }
    }
}
