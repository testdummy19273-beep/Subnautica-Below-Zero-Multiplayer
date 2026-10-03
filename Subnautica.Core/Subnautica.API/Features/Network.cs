namespace Subnautica.API.Features
{
    using Subnautica.API.Features.Creatures;
    using Subnautica.API.Features.NetworkUtility;

    using System;
    using System.Collections.Generic;

    using UnityEngine;

    public class Network
    {
        public static bool IsHost
        {
            get
            {
                return Server.Core.Server.Instance != null && Server.Core.Server.Instance.IsConnected;
            }
        }

        public static bool IsMultiplayerActive { get; set; }

        public static BaseFacePiece BaseFacePiece { get; private set; } = new BaseFacePiece();

        public static DynamicEntity DynamicEntity { get; private set; } = new DynamicEntity();

        public static StaticEntity StaticEntity { get; private set; } = new StaticEntity();

        public static Identifier Identifier { get; private set; } = new Identifier();

        public static Session Session { get; private set; } = new Session();

        public static WorldStreamer WorldStreamer { get; private set; } = new WorldStreamer();

        public static Story Story { get; private set; } = new Story();

        public static Storage Storage { get; private set; } = new Storage();

        public static HandTarget HandTarget { get; private set; } = new HandTarget();

        public static CellManager CellManager { get; private set; } = new CellManager();

        public static DataStorage DataStorage { get; private set; } = new();

        public static EntityDatabase EntityDatabase { get; private set; } = new EntityDatabase();

        public static MultiplayerCreatureManager Creatures { get; private set; } = new MultiplayerCreatureManager();

        public static void Dispose()
        {
            try
            {
                World.SetLoaded(false);

                Network.IsMultiplayerActive = false;
                Network.BaseFacePiece.Dispose();
                Network.DynamicEntity.Dispose();
                Network.StaticEntity.Dispose();
                Network.Identifier.Dispose();
                Network.Session.Dispose();
                Network.WorldStreamer.Dispose();
                Network.Story.Dispose();
                Network.Storage.Dispose();
                Network.HandTarget.Dispose();
                Network.CellManager.Dispose();
                Network.DataStorage.Dispose();
                Network.EntityDatabase.Dispose();
                Network.Creatures.Dispose();

                Entity.Dispose();
                Interact.Dispose();
                WaitingScreen.Dispose();
                World.Dispose();
                ZeroPlayer.DisposeAll();

                Network.PersistentVirtualEntities.Clear();
            }
            catch (Exception ex)
            {
                Log.Error($"Network.Dispose -> Exception: {ex}");
            }
        }

        public static byte GetChannelCount()
        {
            return 2;
        }

        public static bool IsExistsConstructionInServer(string unqiueId)
        {
            if (!Network.IsHost)
            {
                return false;
            }

            return Subnautica.Server.Core.Server.Instance.Storages.Construction.Storage.Constructions.ContainsKey(unqiueId);
        }


        public static string GetWorldEntityId(Vector3 position)
        {
            if (PersistentVirtualEntities.TryGetValue(position, out string uniqueId))
            {
                return uniqueId;
            }

            PersistentVirtualEntities[position] = position.GetHashCode().ToString();
            return PersistentVirtualEntities[position];
        }

        public static Dictionary<Vector3, string> PersistentVirtualEntities { get; private set; } = new Dictionary<Vector3, string>();

    }
}
