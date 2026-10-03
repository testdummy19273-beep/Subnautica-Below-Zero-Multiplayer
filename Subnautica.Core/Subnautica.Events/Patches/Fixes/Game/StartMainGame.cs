namespace Subnautica.Events.Patches.Fixes.Game
{
    using HarmonyLib;

    using Subnautica.API.Features;
    using Subnautica.Events.EventArgs;

    using System;
    using System.Collections;

    using UnityEngine;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEngine.ResourceManagement.ResourceProviders;
    using UnityEngine.SceneManagement;

    using UWE;

    [HarmonyPatch(typeof(global::MainGameController), nameof(global::MainGameController.StartGame))]
    public static class StartMainGame
    {
        private static IEnumerator Postfix(IEnumerator values, global::MainGameController __instance)
        {
            if (Network.IsMultiplayerActive)
            {
                WaitScreen.ManualWaitItem waitItem = WaitScreen.Add("Root");

                yield return global::Utils.EnsureLootCubeCreated();

                __instance.skipHeartbeat = true;

                Physics.autoSyncTransforms = false;
                Physics2D.autoSimulation = false;

                __instance.detailedMemoryLog = Environment.GetEnvironmentVariable("SN_DETAILED_MEMLOG") == "1";

                if (__instance.detailedMemoryLog && !Debug.isDebugBuild)
                {
                    Debug.LogWarning("SN_DETAILED_MEMLOG was set, but this is not a debug/dev build. So the detailed mem readings will all be 0.");
                }

                float num = 60f;
                string environmentVariable = Environment.GetEnvironmentVariable("SN_HEARTBEAT_PERIOD_S");
                if (!string.IsNullOrEmpty(environmentVariable))
                {
                    num = float.Parse(environmentVariable);
                }

                __instance.InvokeRepeating("DoHeartbeat", 0.0f, num);
                waitItem.SetProgress(0.1f);

                for (int i = 0; i < __instance.additionalScenes.Length; ++i)
                {
                    string additionalScene = __instance.additionalScenes[i];
                    AsyncOperationHandle<SceneInstance> asyncOperationHandle = AddressablesUtility.LoadSceneAsync(additionalScene, LoadSceneMode.Additive);
                    WaitScreen.AsyncOperationItem sceneWaitItem = WaitScreen.Add("Scene" + additionalScene, asyncOperationHandle);
                    yield return asyncOperationHandle;
                    WaitScreen.Remove(sceneWaitItem);
                    sceneWaitItem = null;
                }

                waitItem.SetProgress(0.2f);

                while (LightmappedPrefabs.main.IsWaitingOnLoads())
                {
                    yield return CoroutineUtils.waitForNextFrame;
                }

                __instance.SetInitialPlayerPosition();

                waitItem.SetProgress(0.4f);

                PAXTerrainController main2 = PAXTerrainController.main;
                if (main2 != null)
                {
                    yield return main2.Initialize();
                }

                while (!LargeWorldStreamer.main || !LargeWorldStreamer.main.IsWorldSettled())
                {
                    yield return CoroutineUtils.waitForNextFrame;
                }

                waitItem.SetProgress(0.8f);

                __instance.PerformGarbageAndAssetCollection();

                waitItem.SetProgress(0.9f);

                yield return WorldLoadedEvent();

                waitItem.SetProgress(1f);

                WaitScreen.Remove(waitItem);

                Application.backgroundLoadingPriority = ThreadPriority.Normal;
                __instance.UpdateFixedTimestep();

                DevConsole.RegisterConsoleCommand(__instance, "collect");
                DevConsole.RegisterConsoleCommand(__instance, "endsession");


                MainGameController.OnGameStarted?.Invoke();

                World.SetLoaded(true);

                // The host's "CreateServer" loading item (ZeroGame.ShowLoadingScreen) is only removed on failure elsewhere.
                ZeroGame.StopLoadingScreen();

                yield return FinishGameStart(__instance);
            }
            else
            {
                yield return values;
            }
        }

        /*
         * Current game builds run the intro / creative start at the end of MainGameController.StartGame.
         * Because StartGame is replaced in multiplayer, that part is repeated here. ShouldPlayIntro and
         * uGUI_SceneIntro.Play are patched (see IntroChecking.cs) so the multiplayer lobby intro can take over.
         */
        private static IEnumerator FinishGameStart(global::MainGameController controller)
        {
            var player = global::Player.main;
            var data = player.GetGameData(SaveLoadManager.main.storyVersion);
            var playIntro = global::MainGameController.ShouldPlayIntro();

            if (!playIntro)
            {
                WaitScreen.ManualWaitItem waitWorldSettle = WaitScreen.Add("WorldSettle");
                waitWorldSettle?.SetProgress(0.5f);

                var newCreativeMode = !GameModeManager.GetOption<bool>(GameOption.Story) && !global::Utils.GetContinueMode();
                if (newCreativeMode)
                {
                    player.SetPosition(data.creativeStartLocation.position, Quaternion.Euler(data.creativeStartLocation.rotation));
                    player.playerController.SetEnabled(false);

                    yield return null;
                }

                while (!LargeWorldStreamer.main || !LargeWorldStreamer.main.IsReady() || !LargeWorldStreamer.main.IsWorldSettled())
                {
                    yield return new WaitForSecondsRealtime(1f);
                }

                if (newCreativeMode)
                {
                    player.playerController.SetEnabled(true);

                    global::Story.StoryGoal.Execute(data.introManagerPrefab.gameStartGoal, global::Story.GoalType.Story, true, true);
                    global::Story.StoryGoal.Execute("CreativeMode", global::Story.GoalType.Story, true, true);

                    if (DayNightCycle.main)
                    {
                        DayNightCycle.main.SetDayNightTime(data.creativeStartTimeOfDay / 24f);
                    }
                }

                controller.OnIntroDone();

                MainMenuMusic.Stop();
                VRLoadingOverlay.Hide();

                if (waitWorldSettle != null)
                {
                    WaitScreen.Remove(waitWorldSettle);
                }
            }
            else
            {
                player.SetPosition(data.storyStartLocation.position, Quaternion.Euler(data.storyStartLocation.rotation));

                var introManager = UnityEngine.Object.Instantiate(data.introManagerPrefab);
                uGUI.main.intro.Play(introManager, controller.OnIntroDone);

                controller.OnIntroDone();
            }
        }

        private static IEnumerator WorldLoadedEvent()
        {
            WorldLoadedEventArgs args = new WorldLoadedEventArgs();

            Handlers.Game.OnWorldLoaded(args);

            if (args.WaitingMethods != null)
            {
                foreach (var waitingMethod in args.WaitingMethods)
                {
                    yield return waitingMethod;
                }

                args.WaitingMethods = null;
            }
        }
    }
}



