namespace Subnautica.Events.Patches.Events.Storage
{
    using HarmonyLib;

    using Subnautica.API.Extensions;
    using Subnautica.API.Features;
    using Subnautica.Events.EventArgs;

    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [HarmonyPatch]
    public static class Opening
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StorageContainer), "OnHandClick")]
        private static bool StorageContainer_OnHandClick(StorageContainer __instance)
        {
            if (!Network.IsMultiplayerActive)
                return true;
            if (!__instance.enabled || __instance.disableUseability)
                return false;
            KeyValuePair<string, TechType> storageDetail = Opening.GetStorageDetail(__instance);
            if (storageDetail.Key.IsNull())
                return false;
            try
            {
                StorageOpeningEventArgs ev = new StorageOpeningEventArgs(storageDetail.Key, storageDetail.Value);
                Handlers.Storage.OnOpening(ev);
                return ev.IsAllowed;
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("StorageContainer_OnHandClick: {0}\n{1}", (object)ex, (object)ex.StackTrace));
                return true;
            }
        }

        public static KeyValuePair<string, TechType> GetStorageDetail(StorageContainer storageContainer)
        {
            Constructable component = storageContainer.gameObject.GetComponent<Constructable>();
            if (component)
                return component.constructed ? new KeyValuePair<string, TechType>(component.gameObject.GetIdentityId(), component.techType) : new KeyValuePair<string, TechType>();
            if (storageContainer.GetComponentInParent<LifepodDrop>())
                return new KeyValuePair<string, TechType>(storageContainer.gameObject.GetIdentityId(), TechType.EscapePod);
            if (storageContainer.GetComponentInParent<SeaTruckSegment>())
                return new KeyValuePair<string, TechType>(storageContainer.gameObject.GetIdentityId(), storageContainer.transform.gameObject.GetTechType());
            MapRoomFunctionality componentInParent1 = storageContainer.GetComponentInParent<MapRoomFunctionality>();
            if (componentInParent1)
            {
                BaseDeconstructable baseDeconstructable = componentInParent1.GetBaseDeconstructable();
                string key;
                if (baseDeconstructable == null)
                {
                    key = null;
                }
                else
                {
                    GameObject gameObject = ((Component)baseDeconstructable).gameObject;
                    key = gameObject != null ? gameObject.GetIdentityId() : (string)null;
                }
                return new KeyValuePair<string, TechType>(key, TechType.BaseMapRoom);
            }
            WaterPark componentInParent2 = ((Component)storageContainer).GetComponentInParent<WaterPark>();
            if (componentInParent2)
            {
                BaseDeconstructable baseDeconstructable = componentInParent2.GetBaseDeconstructable();
                string key;
                if (baseDeconstructable == null)
                {
                    key = (string)null;
                }
                else
                {
                    GameObject gameObject = ((Component)baseDeconstructable).gameObject;
                    key = gameObject != null ? gameObject.GetIdentityId() : (string)null;
                }
                return new KeyValuePair<string, TechType>(key, TechType.BaseWaterPark);
            }
            LargeWorldEntity componentInParent3 = ((Component)storageContainer).GetComponentInParent<LargeWorldEntity>();
            return componentInParent3 ? new KeyValuePair<string, TechType>(componentInParent3.gameObject.GetIdentityId(), storageContainer.transform.gameObject.GetTechType()) : new KeyValuePair<string, TechType>();
        }
    }
}
