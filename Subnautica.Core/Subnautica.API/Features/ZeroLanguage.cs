namespace Subnautica.API.Features
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class ZeroLanguage
    {
        private static IDictionary<string, string> LanguageData { get; set; } = new Dictionary<string, string>()
        {
            { "GAME_MULTIPLAYER", "Multiplayer" },
            { "GAME_NOT_FOUND_SERVER", "This server does not exist." },
            { "GAME_SERVER_ALREADY_CONNECTING", "You are already connecting to a server. Please wait." },
            { "GAME_SERVER_ALREADY_CONNECTED", "You are already connected to a server" },
            { "GAME_SERVER_CONNECTING_ERROR", "Connection to server failed." },
            { "GAME_PLAYER_CONNECTED", "{playername} is connected." },
            { "GAME_SERVER_PLAYER_CONNECTED", "You are connected to the server. Loading game world." },
            { "GAME_ITEM_USED_ANOTHER_PLAYER", "It is being used by another player." },
            { "GAME_CONNECTION_REJECTED", "Connection to the server failed. Please update by restarting the launcher." },
            { "GAME_MULTIPLAYER_HOST_GAME", "Host Game" },
            { "GAME_MULTIPLAYER_JOIN_GAME", "Join Server" },
            { "GAME_MULTIPLAYER_CREATE_SERVER", "Create Server" },
            { "GAME_SIZE_B", "Byte" },
            { "GAME_SIZE_KB", "KB" },
            { "GAME_SIZE_MB", "MB" },
            { "GAME_SIZE_GB", "GB" },
            { "GAME_MONTH_1", "January" },
            { "GAME_MONTH_2", "February" },
            { "GAME_MONTH_3", "March" },
            { "GAME_MONTH_4", "April" },
            { "GAME_MONTH_5", "May" },
            { "GAME_MONTH_6", "June" },
            { "GAME_MONTH_7", "July" },
            { "GAME_MONTH_8", "August" },
            { "GAME_MONTH_9", "September" },
            { "GAME_MONTH_10", "October" },
            { "GAME_MONTH_11", "November" },
            { "GAME_MONTH_12", "December" },
            { "GAME_BED_PLAYERS_SLEEPINIG", "{playerCount} Players Sleeping" },
            { "GAME_BED_TIME", "Time: {time}" },
            { "GAME_INTRO_PLAYERS_CONNECTED", "{playerCount} players connected." },
            { "GAME_INTRO_SERVER_OWNER_WAITING", "The server owner is expected to start the game." },
            { "GAME_INTRO_SERVER_START_DESCRIPTION", "Please press {key} to start the game." },
            { "GAME_INTRO_MULTIPLAYER_BY_BOTBENSON", "Subnautica Below Zero Multiplayer Mod" },
            { "GAME_STORY_WAITING_PLAYERS", "Waiting for other players ({playerCount}/{maxPlayer})" },
            { "GAME_BEPINEX_DETECTED", "Bepinex Detected. Multiplayer mod has been disabled. Please remove all mod files belonging to Bepinex." },
            { "LOBBY_INVALID_JOIN_CODE", "Invalid Invite Code" },
            { "LOBBY_ALL_SERVER_BUSY", "Failed to Create Server. There are no available servers." },
            { "LOBBY_SERVER_CREATE_FAILED", "Server creation failed." },
            { "LOBBY_SERVER_CREATE_FAILED_EX", "Server creation failed. (0x99)" },
            { "LOBBY_NOT_NETWORK_CONNECTED", "You are not connected to the Network." },
            { "LOBBY_SERVER_FULL", "Connection failed. Server Full" },
            { "API_WEB_SERVER_RETURN_NULL", "There was no response from the server." },
            { "API_SERIALIZE_ERROR", "There is a problem in the parsing of data." },
            { "API_CLIENT_TO_HOST_JOIN_FAILED", "Connecting to the server failed. Try again." },
            { "GAME_INVITE_CODE_PLACEHOLDER", "Enter the host IP address (e.g. 192.168.1.20)" },
            { "GAME_INVITE_CODE_EMPTY_ERROR", "Please enter the host IP address." },
            { "GAME_INVITE_CODE_OR_IP", "Host IP Address (LAN)" },
            { "GAME_INVITE_CODE", "Invite Code" },
            { "GAME_CONNECTION_SERVER_FULL", "Server Full" },
            { "GAME_CONNECTION_SERVER_VERSION_MISMATCH", "The server version and your version do not match." },
            { "GAME_CONNECTION_ERROR_POPUP_TITLE", "Connection Error" },
            { "GAME_CONNECTION_FIX_ERROR", "If your friend cannot connect to your server or you cannot connect to your friend's server. There is no other solution other than these 3 steps.\n\n1. Steam Offline Mode (Do not use Steam in offline mode)\n2. Any VPN Program (AvastVPN, NordVPN, etc. / Close your VPN Program and try again)\n3. Antivirus Problem (Disable your antivirus program and try it)\n\nImportant Note: Both parties need to check these steps." },
            { "GAME_SHOW_INVITE_CODE", "Show Server IP" },
            { "GAME_SHOW_SERVER_IP", "Show Server IP" },
            { "GAME_SERVER_IP_INVALID_ERROR", "Invalid IP address. Example: 192.168.1.20" }
        };

        public static bool LoadLanguage(string language, bool forceDownload = false)
        {
            return false;
        }

        public static string Get(string languageKey)
        {
            if (LanguageData == null)
            {
                return languageKey;
            }

            if (LanguageData.TryGetValue(languageKey, out string text))
            {
                return text;
            }

            return languageKey;
        }

        public static string Get(string languageKey, string defaultText)
        {
            var text = Get(languageKey);
            return text == languageKey ? defaultText : text;
        }

        public static string GetStoryWaitingPlayers(byte playerCount, byte maxPlayer)
        {
            return Get("GAME_STORY_WAITING_PLAYERS").Replace("{playerCount}", playerCount.ToString()).Replace("{maxPlayer}", maxPlayer.ToString());
        }
    }
}