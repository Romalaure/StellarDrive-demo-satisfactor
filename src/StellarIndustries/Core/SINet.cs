using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Serializing;
using FishNet.Transporting;

namespace StellarIndustries.Core
{
    /// <summary>What a Stellar Industries network message is about.</summary>
    internal enum SIMessageKind : byte
    {
        /// <summary>Client to server: set machine A/B's recipe to Text ("" = automatic).</summary>
        SetRecipe = 1,
        /// <summary>Client to server: what is machine A/B doing?</summary>
        StatusRequest = 2,
        /// <summary>Server to one client: machine A/B's state, Text from <see cref="Production.MachineStatusText"/>.</summary>
        Status = 3,
        /// <summary>Client to server: send me the factory overview.</summary>
        OverviewRequest = 4,
        /// <summary>Server to one client: the factory overview, Text is the panel.</summary>
        Overview = 5,
        /// <summary>Server to every client: the working machines, Text is "ship:part;ship:part...".</summary>
        Active = 6,
        /// <summary>Client to server: fit the module item Text into my scanner.</summary>
        ScannerInstall = 7,
        /// <summary>Client to server: take the module of slot Text out of my scanner.</summary>
        ScannerRemove = 8,
        /// <summary>Client to server: send me my scanner's modules.</summary>
        ScannerRequest = 9,
        /// <summary>Server to one client: its scanner's modules, Text is "id,id,..." (A: 1 if the last change was refused).</summary>
        ScannerModules = 10,
        /// <summary>Client to server: I mined an inexhaustible deposit, give me item A times B.</summary>
        NodeMined = 11,
        /// <summary>Client to server: extractor A/B has a rock within reach on my game, Text from <see cref="Production.DepositReports.Encode"/>.</summary>
        DepositSeen = 12
    }

    /// <summary>
    /// One small message type sent as a FishNet broadcast (the game's network library). The host
    /// decides everything; clients only ask. Every player needs the mod.
    /// </summary>
    internal static class SINet
    {
        private struct SIMessage : IBroadcast
        {
            public byte Kind;
            public uint A;
            public ushort B;
            public string Text;
        }

        public delegate void ServerHandler(NetworkConnection sender, uint a, ushort b, string text);
        public delegate void ClientHandler(uint a, ushort b, string text);

        private static readonly Dictionary<SIMessageKind, ServerHandler> ServerHandlers = new Dictionary<SIMessageKind, ServerHandler>();
        private static readonly Dictionary<SIMessageKind, ClientHandler> ClientHandlers = new Dictionary<SIMessageKind, ClientHandler>();
        private static NetworkManager _network;

        public static void InstallSerializers()
        {
            GenericWriter<SIMessage>.SetWrite((writer, m) =>
            {
                writer.WriteUInt8Unpacked(m.Kind);
                writer.WriteUInt32(m.A);
                writer.WriteUInt16(m.B);
                writer.WriteString(m.Text ?? "");
            });
            GenericReader<SIMessage>.SetRead(reader => new SIMessage
            {
                Kind = reader.ReadUInt8Unpacked(),
                A = reader.ReadUInt32(),
                B = reader.ReadUInt16(),
                Text = reader.ReadStringAllocated()
            });
        }

        public static void OnServer(SIMessageKind kind, ServerHandler handler) => ServerHandlers[kind] = handler;

        public static void OnClient(SIMessageKind kind, ClientHandler handler) => ClientHandlers[kind] = handler;

        public static bool IsServer => InstanceFinder.IsServerStarted;

        public static void Update() => EnsureHandlers();

        public static void SendToServer(SIMessageKind kind, uint a, ushort b, string text)
        {
            NetworkManager network = _network;
            if (network == null)
                return;
            var message = new SIMessage { Kind = (byte)kind, A = a, B = b, Text = text ?? "" };
            // On the host, handle our own requests directly instead of looping through the transport
            if (network.IsServerStarted)
            {
                NetworkConnection local = null;
                NetworkConnection clientSide = network.ClientManager.Connection;
                if (clientSide != null)
                    network.ServerManager.Clients.TryGetValue(clientSide.ClientId, out local);
                OnServerMessage(local, message, Channel.Reliable);
            }
            else if (network.IsClientStarted)
            {
                network.ClientManager.Broadcast(message);
            }
        }

        /// <summary>Server only: tells every remote client (not the host's own).</summary>
        public static void SendToClients(SIMessageKind kind, uint a, ushort b, string text)
        {
            if (_network == null || !_network.IsServerStarted)
                return;
            var message = new SIMessage { Kind = (byte)kind, A = a, B = b, Text = text ?? "" };
            foreach (NetworkConnection connection in _network.ServerManager.Clients.Values)
            {
                if (connection != null && !connection.IsLocalClient && connection.IsAuthenticated)
                    _network.ServerManager.Broadcast(connection, message);
            }
        }

        /// <summary>Server only: answers one client.</summary>
        public static void SendTo(NetworkConnection connection, SIMessageKind kind, uint a, ushort b, string text)
        {
            if (_network == null || !_network.IsServerStarted || connection == null)
                return;
            _network.ServerManager.Broadcast(connection, new SIMessage { Kind = (byte)kind, A = a, B = b, Text = text ?? "" });
        }

        private static void EnsureHandlers()
        {
            NetworkManager network = InstanceFinder.NetworkManager;
            if (network == _network)
                return;
            if (_network != null)
            {
                try
                {
                    _network.ServerManager.UnregisterBroadcast<SIMessage>(OnServerMessage);
                    _network.ClientManager.UnregisterBroadcast<SIMessage>(OnClientMessage);
                }
                catch (Exception)
                {
                    // the old manager is being torn down
                }
            }
            _network = network;
            if (network == null)
                return;
            network.ServerManager.RegisterBroadcast<SIMessage>(OnServerMessage, true);
            network.ClientManager.RegisterBroadcast<SIMessage>(OnClientMessage);
        }

        private static void OnServerMessage(NetworkConnection sender, SIMessage message, Channel channel)
        {
            var kind = (SIMessageKind)message.Kind;
            try
            {
                if (ServerHandlers.TryGetValue(kind, out ServerHandler handler))
                    handler(sender, message.A, message.B, message.Text ?? "");
            }
            catch (Exception e)
            {
                ErrorLog.Report("network " + kind, e);
            }
        }

        private static void OnClientMessage(SIMessage message, Channel channel)
        {
            var kind = (SIMessageKind)message.Kind;
            try
            {
                if (ClientHandlers.TryGetValue(kind, out ClientHandler handler))
                    handler(message.A, message.B, message.Text ?? "");
            }
            catch (Exception e)
            {
                ErrorLog.Report("network " + kind, e);
            }
        }
    }
}
