namespace Subnautica.Client.Synchronizations.Processors.General
{
    using Subnautica.API.Features;
    using Subnautica.Client.Abstracts;
    using Subnautica.Client.Core;
    using Subnautica.Client.Synchronizations.InitialSync;
    using Subnautica.Events.EventArgs;
    using Subnautica.Network.Models.Core;
    using System.Collections;
    using System.Linq;
    using UnityEngine;
    using UWE;
    using ServerModel = Subnautica.Network.Models.Server;

    public class IntroProcessor : NormalProcessor
    {
        public override bool OnDataReceived(NetworkPacket networkPacket)
        {
            var packet = networkPacket.GetPacket<ServerModel.IntroStartArgs>();
            if (packet == null)
            {
                return true;
            }

            if (packet.IsFinished)
            {
                Network.Session.Current.SupplyDrops.RemoveAll(q => q.Key == packet.SupplyDrop.Key);
                Network.Session.Current.SupplyDrops.Add(packet.SupplyDrop);

                WorldProcessor.SetDayNightCycle(packet.ServerTime);
                LifepodProcessor.ForceSupplyDrop(packet.SupplyDrop.Key);
            }
            else
            {
                Network.Session.Current.IsFirstLogin = false;
            }

            return true;
        }

        public static bool IsIntroActive { get; set; }

        public static void OnIntroChecking(IntroCheckingEventArgs ev)
        {
            var isMultiplayerIntro = Network.Session.Current.IsFirstLogin && GameModeManager.GetOption<bool>(GameOption.Story);

            if (ev.IsPlaying)
            {
                if (isMultiplayerIntro)
                {
                    ev.IsAllowed = false;

                    ev.Gui.coroutine = ev.Gui.StartCoroutine(IntroProcessor.InitalizeIntroAsync(ev.IntroManager, ev.Gui, ev.OnIntroDone));
                    ManagedUpdate.Subscribe(ManagedUpdate.Queue.UpdateAfterInput, ev.Gui.OnUpdate);
                    InputHandlerStack.main.Push(ev.Gui);
                }

                return;
            }

            if (isMultiplayerIntro)
            {
                ev.IsAllowed = false;
            }
            else
            {
                if (!Network.Session.Current.SupplyDrops.Any(q => q.Key == API.Constants.SupplyDrop.Lifepod))
                {
                    IntroProcessor.SendPacketToServer(true);
                }

                global::Utils.SetContinueMode(true);
            }
        }

        private static IEnumerator InitalizeIntroAsync(ExpansionIntroManager introManager, uGUI_SceneIntro gui, System.Action onIntroDone)
        {
            IntroProcessor.IsIntroActive = true;

            if (FPSInputModule.current)
            {
                FPSInputModule.current.lockPauseMenu = true;
            }

            gui.fader.SetState(true);
            global::Player.main.playerController.inputEnabled = false;
            gui.PauseGameTime();

            yield return new WaitForSecondsRealtime(0.5f);

            MainMenuMusic.Stop();
            introManager.TriggerStartScreenAudio();
            gui.mainText.SetText("");

            yield return new WaitForSecondsRealtime(2f);

            while (!LargeWorldStreamer.main || !LargeWorldStreamer.main.IsReady() || !LargeWorldStreamer.main.IsWorldSettled())
            {
                yield return new WaitForSecondsRealtime(1f);
            }

            VRLoadingOverlay.Hide();

            if (Network.IsHost)
            {
                gui.mainText.SetState(true);

                while (!GameInput.GetButtonDown(GameInput.Button.UICancel))
                {
                    gui.mainText.SetText(ZeroLanguage.Get("GAME_INTRO_PLAYERS_CONNECTED").Replace("{playerCount}", ZeroPlayer.GetAllPlayers().Count.ToString()) + "\n" + ZeroLanguage.Get("GAME_INTRO_SERVER_START_DESCRIPTION").Replace("{key}", "ESC") + "\n\n" + ZeroLanguage.Get("GAME_SERVER_IP", "Server IP") + "\n" + LanHost.GetJoinAddressText(NetworkServer.DefaultPort));
                    yield return null;
                }

                IntroProcessor.SendPacketToServer();

                while (Network.Session.Current.IsFirstLogin)
                {
                    yield return CoroutineUtils.waitForNextFrame;
                }
            }
            else
            {
                if (Network.Session.Current.IsFirstLogin)
                {
                    gui.mainText.SetState(true);

                    while (Network.Session.Current.IsFirstLogin)
                    {
                        gui.mainText.SetText(ZeroLanguage.Get("GAME_INTRO_PLAYERS_CONNECTED").Replace("{playerCount}", ZeroPlayer.GetAllPlayers().Count.ToString()) + "\n" + ZeroLanguage.Get("GAME_INTRO_SERVER_OWNER_WAITING"));
                        yield return new WaitForSecondsRealtime(0.25f);
                    }
                }
            }

            if (FPSInputModule.current)
            {
                FPSInputModule.current.lockPauseMenu = false;
            }

            yield return introManager.Play(global::Player.main, gui);

            IntroProcessor.IsIntroActive = false;

            gui.ResumeGameTime();
            gui.StopCoroutine(gui.coroutine);
            gui.coroutine = null;
            gui.StartCoroutine(gui.ControlsHints());
            gui.Stop(false);

            onIntroDone?.Invoke();

            IntroProcessor.SendPacketToServer(true);
        }

        public static void SendPacketToServer(bool isFinished = false)
        {
            ServerModel.IntroStartArgs request = new ServerModel.IntroStartArgs()
            {
                IsFinished = isFinished,
            };

            NetworkClient.SendPacket(request);
        }
    }
}