namespace Subnautica.Events.Patches.Events.Game
{
    using HarmonyLib;
    using Subnautica.API.Features;
    using Subnautica.Events.EventArgs;
    using System;

    /*
     * The intro is decided inside MainGameController.StartGame in current game builds.
     * ShouldPlayIntro is asked first (continue-mode / first login handling), then uGUI_SceneIntro.Play runs the intro.
     */
    [HarmonyPatch(typeof(global::MainGameController), nameof(global::MainGameController.ShouldPlayIntro))]
    public static class IntroChecking
    {
        private static bool Prefix(ref bool __result)
        {
            if (!Network.IsMultiplayerActive)
            {
                return true;
            }

            IntroCheckingEventArgs args = new IntroCheckingEventArgs();

            try
            {
                Handlers.Game.OnIntroChecking(args);
            }
            catch (Exception e)
            {
                Log.Error($"IntroChecking.Prefix: {e}\n{e.StackTrace}");
            }

            if (args.IsAllowed)
            {
                return true;
            }

            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(global::uGUI_SceneIntro), nameof(global::uGUI_SceneIntro.Play))]
    public static class IntroPlaying
    {
        private static bool Prefix(global::uGUI_SceneIntro __instance, global::ExpansionIntroManager introManager, Action onIntroDone)
        {
            if (!Network.IsMultiplayerActive)
            {
                return true;
            }

            IntroCheckingEventArgs args = new IntroCheckingEventArgs()
            {
                IsPlaying    = true,
                IntroManager = introManager,
                OnIntroDone  = onIntroDone,
                Gui          = __instance,
            };

            try
            {
                Handlers.Game.OnIntroChecking(args);
            }
            catch (Exception e)
            {
                Log.Error($"IntroPlaying.Prefix: {e}\n{e.StackTrace}");
            }

            return args.IsAllowed;
        }
    }
}