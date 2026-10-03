namespace Subnautica.Events.Patches.Fixes.Furnitures;

using HarmonyLib;
using Subnautica.API.Extensions;
using Subnautica.API.Features;
using Subnautica.API.MonoBehaviours;
using UnityEngine;

[HarmonyPatch]
public class BaseWaterPark
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(WaterParkCreature), "ManagedUpdate")]
    private static bool WaterParkCreature_ManagedUpdate(WaterParkCreature __instance)
    {
        return !Network.IsMultiplayerActive;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(WaterParkCreature), "SetMatureTime")]
    private static bool WaterParkCreature_SetMatureTime(WaterParkCreature __instance)
    {
        return !Network.IsMultiplayerActive;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(WaterParkCreature), "OnAddToWP")]
    private static bool WaterParkCreature_OnAddToWP(WaterParkCreature __instance)
    {
        return !Network.IsMultiplayerActive;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CreatureEgg), "UpdateHatchingTime")]
    private static bool CreatureEgg_UpdateHatchingTime(CreatureEgg __instance)
    {
        return !Network.IsMultiplayerActive;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CreatureEgg), "Hatch")]
    private static bool CreatureEgg_Hatch(CreatureEgg __instance)
    {
        return !Network.IsMultiplayerActive;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CreatureEgg), "UpdateProgress")]
    private static bool CreatureEgg_UpdateProgress(CreatureEgg __instance)
    {
        if (!Network.IsMultiplayerActive)
        {
            return true;
        }
        else
        {
            __instance.progress = Mathf.InverseLerp(__instance.timeStartHatching, __instance.timeStartHatching + 1200f, (float)Network.Session.GetWorldTime());
            foreach (Animator animator in __instance.animators)
            {
                animator.SetFloat(AnimatorHashID.progress, __instance.progress);
            }
            FMOD_CustomLoopingEmitter idleSound = __instance.idleSound;
            if (idleSound != null)
            {
                idleSound.SetParameterValue("close_to_hatch", __instance.progress);
            }
            return false;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(LargeRoomWaterPark), "OnDeconstructionStart")]
    private static bool LargeRoomWaterPark_OnDeconstructionStart(LargeRoomWaterPark __instance)
    {
        if (!Network.IsMultiplayerActive)
        {
            return true;
        }
        else
        {
            if (__instance.size == 1)
            {
                return false;
            }
            else
            {
                LargeRoomWaterPark root = __instance.GetRoot();
                for (int i = root.items.Count - 1; i >= 0; i--)
                {
                    WaterParkItem waterParkItem = root.items[i];
                    bool flag4 = waterParkItem.GetWaterPark() == __instance;
                    if (flag4)
                    {
                        waterParkItem.SetWaterPark(null);
                        waterParkItem.transform.localScale = Vector3.zero;
                    }
                }
                __instance.size = 1;
                __instance.segments.Clear();
                __instance.segments.Add(__instance);
                __instance._rootWaterPark = __instance;
                return false;
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(WaterPark), "Rebuild")]
    private static void WaterPark_Rebuild(WaterPark __instance)
    {
        if (Network.IsMultiplayerActive && __instance.rootWaterPark && __instance.rootWaterPark.IsPointInside(ZeroPlayer.CurrentPlayer.Main.transform.position))
        {
            ZeroPlayer.CurrentPlayer.Main.currentWaterPark = __instance.rootWaterPark;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(WaterParkCreature), "OnRemoveFromWP")]
    private static bool WaterParkCreature_OnRemoveFromWP(WaterParkCreature __instance)
    {
        if (!Network.IsMultiplayerActive)
        {
            return true;
        }
        else
        {
            __instance.timeNextBreed = -1f;
            __instance.CancelInvoke();
            BehaviourUpdateUtils.Deregister(__instance);
            return false;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GrowingPlant), "ManagedUpdate")]
    private static bool GrowingPlant_ManagedUpdate(GrowingPlant __instance)
    {
        if (!Network.IsMultiplayerActive)
        {
            return true;
        }
        else
        {
            if (__instance.enabled)
            {
                float progress = __instance.GetProgress();
                __instance.SetScale(__instance.growingTransform, progress);
                __instance.SetPosition(__instance.growingTransform);
                if (progress == 1f)
                {
                    __instance.SpawnGrownModel();
                }
            }
            return false;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Pickupable), "OnHandHover")]
    private static void Pickupable_OnHandHover(Pickupable __instance, GUIHand hand)
    {
        if (Network.IsMultiplayerActive && hand.IsFreeToInteract())
        {
            CreatureEgg creatureEgg;
            if (__instance.GetTechType().IsCreatureEgg() && __instance.TryGetComponent<CreatureEgg>(out creatureEgg) && creatureEgg.insideWaterPark)
            {
                HandReticle.main.SetProgress(creatureEgg.progress);
                HandReticle.main.SetIcon(HandReticle.IconType.Progress, 1.5f);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GrowingPlant), "SetProgress")]
    private static void GrowingPlant_SetProgress(GrowingPlant __instance)
    {
        if (Network.IsMultiplayerActive && __instance.maxProgress == 1f && __instance.seed)
        {
            PlanterItemComponent planterItemComponent;
            if (__instance.seed.TryGetComponent<PlanterItemComponent>(out planterItemComponent))
            {
                __instance.timeStartGrowth = planterItemComponent.TimeStartGrowth;
            }
        }
    }
}
