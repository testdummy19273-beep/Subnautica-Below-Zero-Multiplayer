namespace Subnautica.Client.Modules
{
    using Subnautica.API.Extensions;
    using Subnautica.API.Features;
    using Subnautica.API.Features.Helper;
    using Subnautica.Client.Core;
    using Subnautica.Client.Modules.MultiplayerMainMenuModule;
    using Subnautica.Events.EventArgs;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using UnityEngine;
    using UWE;

    public class MultiplayerMainMenu
    {
        public static void OnSceneLoaded(SceneLoadedEventArgs ev)
        {
            IsClicked = false;

            if (ev.Scene.name == "XMenu")
            {
                if (Settings.IsBepinexInstalled)
                {
                    CoroutineHost.StartCoroutine(SendAutoBepinexWarn());
                }
                else
                {
                    InitializeMultiplayerMenu();
                }
            }
        }

        public static IEnumerator SendAutoBepinexWarn()
        {
            yield return new WaitForSecondsRealtime(2f);

            uGUI.main.confirmation.Show(ZeroLanguage.Get("GAME_BEPINEX_DETECTED"), null, null);
        }

        public static void InitializeMultiplayerMenu()
        {
            CacheSinglePlayerSaveGames();

            UserInterfaceElements.SinglePlayerButtonAddEvent(OnSinglePlayerButtonClick);
            UserInterfaceElements.CreateSidebarButton(ZeroLanguage.Get("GAME_MULTIPLAYER"), OnSidebarMultiplayerButtonClick);

            var baseGroup = UserInterfaceElements.CreateMultiplayerBaseContent(MULTIPLAYER_BASE_GROUP_NAME, ZeroLanguage.Get("GAME_MULTIPLAYER"), ZeroLanguage.Get("GAME_MULTIPLAYER_HOST_GAME"), ZeroLanguage.Get("GAME_MULTIPLAYER_JOIN_GAME"), OnHostGameButtonClick, OnJoinGameButtonClick);
            var hostGameGroup = UserInterfaceElements.CreateHostBaseContent(MULTIPLAYER_HOST_GROUP_NAME, ZeroLanguage.Get("GAME_MULTIPLAYER_HOST_GAME"), OnHostCreateServerButtonClick);
            var joinGameGroup = UserInterfaceElements.CreateJoinBaseContent(MULTIPLAYER_JOIN_GROUP_NAME, ZeroLanguage.Get("GAME_MULTIPLAYER_JOIN_GAME"), ZeroLanguage.Get("GAME_ADD_SERVER"), OnAddServerButtonClick);
            var addServerGroup = UserInterfaceElements.CreateAddServerGroup(MULTIPLAYER_JOIN_ADD_SERVER_GROUP_NAME, OnAddServerSaveButtonClick);
            var createServerGroup = UserInterfaceElements.CreateServerHostGroup(MULTIPLAYER_HOST_CREATE_SERVER_GROUP_NAME, OnCreateServerHostClick);

            MainMenuRightSide.main.groups.Add(baseGroup.GetComponent<MainMenuGroup>());
            MainMenuRightSide.main.groups.Add(hostGameGroup.GetComponent<MainMenuGroup>());
            MainMenuRightSide.main.groups.Add(joinGameGroup.GetComponent<MainMenuGroup>());
            MainMenuRightSide.main.groups.Add(addServerGroup.GetComponent<MainMenuGroup>());
            MainMenuRightSide.main.groups.Add(createServerGroup.GetComponent<MainMenuGroup>());

            var launchRequest = LaunchRequest.Consume();
            if (launchRequest != null)
            {
                CoroutineHost.StartCoroutine(RunLaunchRequest(launchRequest));
            }

            CoroutineHost.StartCoroutine(PollJoinRequestFile());
        }

        /// <summary>
        /// While the main menu is open, picks up "join-request.txt" (written by the launcher's Join button when the game is already running).
        /// A request older than 15 seconds is stale (the game was inside a world when it was written) and is dropped.
        /// </summary>
        private static IEnumerator PollJoinRequestFile()
        {
            var path = Paths.GetLauncherGamePath() + "join-request.txt";

            while (MainMenuRightSide.main != null)
            {
                yield return new WaitForSecondsRealtime(1f);

                try
                {
                    if (MainMenuRightSide.main == null || !File.Exists(path))
                    {
                        continue;
                    }

                    var address = File.ReadAllText(path).Trim();
                    var isFresh = (DateTime.UtcNow - File.GetLastWriteTimeUtc(path)).TotalSeconds < 15;
                    File.Delete(path);

                    if (!isFresh || address.Length == 0)
                    {
                        continue;
                    }

                    if (IsClicked || NetworkClient.IsConnectingToServer || NetworkClient.IsConnectedToServer || NetworkServer.IsConnecting() || NetworkServer.IsConnected())
                    {
                        continue;
                    }

                    Log.Info($"Launcher request: join {address} (running game)");
                    JoinAddress(address);
                }
                catch (Exception e)
                {
                    Log.Error($"PollJoinRequestFile: {e}");
                }
            }
        }

        private static void JoinAddress(string address)
        {
            if (LanHost.TryParseAddress(address, Settings.ModConfig.DefaultJoinPort.GetInt(7777), out var hostAddress, out var hostPort))
            {
                NetworkClient.Connect(hostAddress, hostPort, false);
            }
            else
            {
                Log.Error($"Launcher request: invalid server address '{address}'.");
            }
        }

        /// <summary>
        /// Executes a host/join request the launcher passed on the command line (see LaunchRequest).
        /// </summary>
        public static IEnumerator RunLaunchRequest(LaunchRequest request)
        {
            // Give the main menu a moment to finish building before taking over.
            yield return new WaitForSecondsRealtime(1.5f);

            try
            {
                if (request.IsJoin)
                {
                    Log.Info($"Launcher request: join {request.Value}");

                    JoinAddress(request.Value);

                    yield break;
                }

                Log.Info($"Launcher request: host {request.Value}");

                string serverId = request.Value;
                if (request.IsNewWorld)
                {
                    if (!TryParseGameMode(request.NewWorldGameMode, out var gameMode))
                    {
                        Log.Error($"Launcher request: unknown game mode '{request.NewWorldGameMode}'.");
                        yield break;
                    }

                    serverId = NetworkServer.CreateNewServer(gameMode);
                }

                StartHostedServer(serverId);
            }
            catch (Exception e)
            {
                Log.Error($"Launcher request failed: {e}");
                ZeroGame.StopLoadingScreen();
                IsClicked = false;
            }
        }

        private static bool TryParseGameMode(string text, out GameModePresetId gameMode)
        {
            if (Enum.TryParse(text, true, out gameMode) && Enum.IsDefined(typeof(GameModePresetId), gameMode))
            {
                return gameMode != GameModePresetId.Custom;
            }

            gameMode = GameModePresetId.Survival;
            return false;
        }

        private static void StartHostedServer(string serverId)
        {
            if (IsClicked || NetworkServer.IsConnecting() || NetworkServer.IsConnected())
            {
                return;
            }

            if (!NetworkServer.GetHostServerList().Any(q => q.Id == serverId))
            {
                Log.Error($"Launcher request: world '{serverId}' was not found.");
                ErrorMessage.AddMessage(ZeroLanguage.Get("GAME_NOT_FOUND_SERVER"));
                return;
            }

            IsClicked = true;

            ShowHostLoadingScreen();

            if (NetworkServer.StartServer(serverId, Tools.GetLoggedId()))
            {
                NetworkClient.Connect(NetworkServer.DefaultLocalIpAddress, NetworkServer.DefaultPort);
            }
            else
            {
                ZeroGame.StopLoadingScreen();
                IsClicked = false;
            }
        }

        public static void OnSinglePlayerButtonClick()
        {
            SaveLoadManager.main.gameInfoCache.Clear();

            foreach (var item in SinglePlayerGameSaves)
            {
                SaveLoadManager.main.gameInfoCache[item.Key] = item.Value;
            }

            MainMenuRightSide.main.OpenGroup("SavedGames");
        }

        public static void OnSidebarMultiplayerButtonClick()
        {
            MainMenuRightSide.main.OpenGroup(MULTIPLAYER_BASE_GROUP_NAME);
        }

        public static void OnHostGameButtonClick()
        {
            SaveLoadManager.main.gameInfoCache.Clear();

            foreach (var item in NetworkServer.GetHostServerList())
            {
                SaveLoadManager.GameInfo info = new SaveLoadManager.GameInfo();
                info.Initialize(0, 0, SaveLoadManager.defaultStoryVersion, item.Id, null, item.GetGameMode(), null);

                SaveLoadManager.main.gameInfoCache[item.Id] = info;
            }

            MainMenuRightSide.main.OpenGroup(MULTIPLAYER_HOST_GROUP_NAME);
        }

        public static void OnAddServerButtonClick()
        {
            var serverAddress = UserInterfaceElements.GetInputText("GAME_INVITE_CODE").Trim();
            if (serverAddress.IsNull())
            {
                UserInterfaceElements.SetInputErrorMessage("GAME_INVITE_CODE", ZeroLanguage.Get("GAME_INVITE_CODE_EMPTY_ERROR"));
                return;
            }

            var defaultPort = Settings.ModConfig.DefaultJoinPort.GetInt(7777);
            if (!LanHost.TryParseAddress(serverAddress, defaultPort, out var hostAddress, out var hostPort))
            {
                UserInterfaceElements.SetInputErrorMessage("GAME_INVITE_CODE", ZeroLanguage.Get("GAME_SERVER_IP_INVALID_ERROR", "Invalid IP address. Example: 192.168.1.20"));
                return;
            }

            NetworkClient.Connect(hostAddress, hostPort, false);
        }

        public static void OnHostCreateServerButtonClick()
        {
            MainMenuRightSide.main.OpenGroup(MULTIPLAYER_HOST_CREATE_SERVER_GROUP_NAME);
        }

        public static void OnJoinGameButtonClick()
        {
            SaveLoadManager.main.gameInfoCache.Clear();

            foreach (var item in NetworkServer.GetLocalServerList())
            {
                SaveLoadManager.GameInfo info = new SaveLoadManager.GameInfo();
                info.Initialize(0, 0, SaveLoadManager.defaultStoryVersion, item.Id, null, GameModePresetId.Survival, null);

                SaveLoadManager.main.gameInfoCache[item.Id] = info;
            };

            MainMenuRightSide.main.OpenGroup(MULTIPLAYER_JOIN_GROUP_NAME);
        }

        public static void CacheSinglePlayerSaveGames()
        {
            SinglePlayerGameSaves = new Dictionary<string, SaveLoadManager.GameInfo>(SaveLoadManager.main.gameInfoCache);
        }

        public static void OnMenuSaveCancelDeleteButtonClicking(MenuSaveCancelDeleteButtonClickingEventArgs ev)
        {
            ev.IsAllowed = CancelDeleteSave();

            if (!ev.IsAllowed)
            {
                ev.IsRunAnimation = true;
            }
        }

        public static void OnMenuSaveDeleteButtonClicking(MenuSaveDeleteButtonClickingEventArgs ev)
        {
            ev.IsAllowed = DeleteSave(ev.SessionId);

            if (!ev.IsAllowed)
            {
                ev.IsRunAnimation = true;
            }
        }

        public static void OnMenuSaveUpdateLoadedButtonState(MenuSaveUpdateLoadedButtonStateEventArgs ev)
        {
            UpdateLoadSaveButtonState(ev.Button);
        }

        public static void OnMenuSaveLoadButtonClicking(MenuSaveLoadButtonClickingEventArgs ev)
        {
            ev.IsAllowed = LoadSave(ev.SessionId);
        }

        public static bool LoadSave(string sessionId)
        {
            if (UserInterfaceElements.IsSinglePlayerMenuActive)
            {
                return true;
            }

            if (UserInterfaceElements.IsHostGroupActive)
            {
                var server = NetworkServer.GetHostServerList().Where(q => q.Id == sessionId).FirstOrDefault();
                if (server == null)
                {
                    ErrorMessage.AddMessage(ZeroLanguage.Get("GAME_NOT_FOUND_SERVER"));
                    return false;
                }

                if (NetworkServer.IsConnecting())
                {
                    ErrorMessage.AddMessage(ZeroLanguage.Get("GAME_SERVER_ALREADY_CONNECTING"));
                    return false;
                }

                if (NetworkServer.IsConnected())
                {
                    ErrorMessage.AddMessage(ZeroLanguage.Get("GAME_SERVER_ALREADY_CONNECTED"));
                    return false;
                }

                if (IsClicked)
                {
                    return false;
                }

                IsClicked = true;

                ShowHostLoadingScreen();

                if (NetworkServer.StartServer(server.Id, Tools.GetLoggedId()))
                {
                    NetworkClient.Connect(NetworkServer.DefaultLocalIpAddress, NetworkServer.DefaultPort);
                }
                else
                {
                    ZeroGame.StopLoadingScreen();
                    IsClicked = false;
                }
            }

            return false;
        }

        public static bool CancelDeleteSave()
        {
            if (UserInterfaceElements.IsSinglePlayerMenuActive)
            {
                return true;
            }

            return false;
        }

        public static bool DeleteSave(string sessionId)
        {
            if (UserInterfaceElements.IsSinglePlayerMenuActive)
            {
                return true;
            }

            if (UserInterfaceElements.IsHostGroupActive)
            {
                var server = NetworkServer.GetHostServerList().Where(q => q.Id == sessionId).FirstOrDefault();
                if (server != null)
                {
                    string serverPath = Paths.GetMultiplayerServerSavePath(server.Id);
                    if (Directory.Exists(serverPath))
                    {
                        Directory.Delete(serverPath, true);
                    }
                }
            }
            else
            {
                var serverList = NetworkServer.GetLocalServerList();
                if (serverList.Count <= 0)
                {
                    return false;
                }

                var server = serverList.Where(q => q.Id == sessionId).FirstOrDefault();
                if (server != null)
                {
                    serverList.Remove(server);
                    NetworkServer.SaveLocalServerList(serverList);
                }
            }

            return false;
        }

        public static void UpdateLoadSaveButtonState(MainMenuLoadButton lb)
        {
            if (UserInterfaceElements.IsHostGroupActive)
            {
                var server = NetworkServer.GetHostServerList().Where(q => q.Id == lb.sessionId).FirstOrDefault();
                if (server != null)
                {
                    lb.saveGameLengthText.text = Tools.GetSizeByTextFormat(Tools.GetFolderSize(Paths.GetMultiplayerServerSavePath(server.Id)));
                    lb.saveGameTimeText.text = Tools.GetDateByTextFormat(server.CreationDate);
                }
            }
            else
            {
                var server = NetworkServer.GetLocalServerList().Where(q => q.Id == lb.sessionId).FirstOrDefault();
                if (server != null)
                {
                    lb.saveGameLengthText.text = String.Format("{0}:{1}", server.IpAddress, server.Port);
                    lb.saveGameTimeText.text = server.Name;
                    lb.saveGameModeText.text = "";
                }
            }
        }

        public static void OnAddServerSaveButtonClick()
        {
            var serverName = UserInterfaceElements.GetInputText("GAME_SERVER_NAME").Trim();
            var serverIp = UserInterfaceElements.GetInputText("GAME_SERVER_IP").Trim();

            if (string.IsNullOrEmpty(serverName))
            {
                UserInterfaceElements.SetInputErrorMessage("GAME_SERVER_IP", ZeroLanguage.Get("GAME_SERVER_NAME_EMPTY_ERROR"));
                return;
            }

            if (serverName.Length < 3 || serverName.Length > 64)
            {
                UserInterfaceElements.SetInputErrorMessage("GAME_SERVER_IP", ZeroLanguage.Get("GAME_SERVER_NAME_LENGTH_ERROR"));
                return;
            }

            if (string.IsNullOrEmpty(serverIp) || !Regex.Match(serverIp, @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b").Success)
            {
                UserInterfaceElements.SetInputErrorMessage("GAME_SERVER_IP", ZeroLanguage.Get("GAME_SERVER_IP_INVALID_ERROR"));
                return;
            }

            var serverList = NetworkServer.GetLocalServerList();
            if (serverList.Where(q => q.IpAddress == serverIp).Any())
            {
                UserInterfaceElements.SetInputErrorMessage("GAME_SERVER_IP", ZeroLanguage.Get("GAME_SERVER_EXIST"));
                return;
            }

            serverList.Add(new LocalServerItem()
            {
                Id = Guid.NewGuid().ToString(),
                IpAddress = serverIp,
                Port = NetworkServer.DefaultPort,
                Name = serverName,
            });

            NetworkServer.SaveLocalServerList(serverList);

            UserInterfaceElements.ClearInputText("GAME_SERVER_NAME");
            UserInterfaceElements.ClearInputText("GAME_SERVER_IP");

            OnJoinGameButtonClick();
        }

        public static void OnCreateServerHostClick(GameModePresetId gameModeId)
        {
            if (IsClicked == false)
            {
                IsClicked = true;

                ShowHostLoadingScreen();

                var serverId = NetworkServer.CreateNewServer(gameModeId);

                if (NetworkServer.StartServer(serverId, Tools.GetLoggedId()))
                {
                    NetworkClient.Connect(NetworkServer.DefaultLocalIpAddress, NetworkServer.DefaultPort);
                }
                else
                {
                    ZeroGame.StopLoadingScreen();
                    IsClicked = false;
                    OnHostGameButtonClick();
                }
            }
        }

        private static void ShowHostLoadingScreen()
        {
            ZeroGame.ShowLoadingScreen();
            LanHost.EnsureFirewallRule();
        }

        public const string MULTIPLAYER_BASE_GROUP_NAME = "MultiplayerBase";
        public const string MULTIPLAYER_HOST_GROUP_NAME = "MultiplayerHostBase";
        public const string MULTIPLAYER_JOIN_GROUP_NAME = "MultiplayerJoinBase";
        public const string MULTIPLAYER_JOIN_ADD_SERVER_GROUP_NAME = "MultiplayerJoinAddServerBase";
        public const string MULTIPLAYER_HOST_CREATE_SERVER_GROUP_NAME = "MultiplayerHostCreateServerBase";

        public static Dictionary<string, SaveLoadManager.GameInfo> SinglePlayerGameSaves { get; set; }

        public static bool IsClicked { get; set; } = false;
    }
}