namespace Subnautica.API.Features
{
    using Subnautica.API.Enums;
    using Subnautica.API.Extensions;
    using Subnautica.API.Features.Helper;
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using UnityEngine;
    using UnityEngine.Playables;
    using UWE;
    using SystemVersion = System.Version;

    public class Tools
    {
        public static System.Random Random { get; set; } = new System.Random();

        private static string[] LauncherVersionData { get; set; }

        public static byte[] SerializeGameObject(UnityEngine.GameObject gameObject)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (PooledObject<ProtobufSerializer> proxy = ProtobufSerializerPool.GetProxy())
                {
                    proxy.Value.SerializeObjectTree(memoryStream, gameObject);
                    return memoryStream.ToArray();
                }
            }
        }

        public static VersionType CheckCreditsPage()
        {
            return VersionType.EmptyContent;
        }

        public static VersionType CheckLauncherVersion(bool save = false)
        {
            return VersionType.NewVersionNotFound;
        }

        /**
         *
         * Player name: the "PlayerName" option (Config.json or -bzmp-name) if set, otherwise the platform (Steam) name.
         *
         */
        public static string GetLoggedInName()
        {
            var customName = GetCustomPlayerName();
            if (customName != null)
            {
                return customName;
            }

            return global::PlatformUtils.main.GetLoggedInUserName();
        }

        /**
         *
         * Player id. With a custom player name the id is derived from the name, so it does not depend on Steam
         * and stays the same between sessions. Otherwise it is the platform (Steam) id.
         *
         */
        public static string GetLoggedId()
        {
            var customName = GetCustomPlayerName();
            if (customName != null)
            {
                return "bzmp-" + customName.ToLowerInvariant();
            }

            var userId = global::PlatformUtils.main.GetCurrentUserId();
            if (userId == "0" || userId.IsNull())
            {
                return null;
            }

            return userId;
        }

        /**
         *
         * Cleaned custom player name or null if none is set. Letters, digits, space, "_", "-" and "." only, 2-24 characters.
         *
         */
        public static string GetCustomPlayerName()
        {
            string name = null;

            try
            {
                var args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (string.Equals(args[i], "-bzmp-name", StringComparison.OrdinalIgnoreCase))
                    {
                        name = args[i + 1];
                        break;
                    }
                }
            }
            catch (Exception)
            {
            }

            if (name.IsNull())
            {
                name = Settings.ModConfig.PlayerName.GetString("");
            }

            if (name.IsNull())
            {
                return null;
            }

            var clean = new System.Text.StringBuilder();
            foreach (var c in name.Trim())
            {
                if (char.IsLetterOrDigit(c) || c == ' ' || c == '_' || c == '-' || c == '.')
                {
                    clean.Append(c);
                }
            }

            var result = clean.ToString().Trim();
            if (result.Length > 24)
            {
                result = result.Substring(0, 24).Trim();
            }

            return result.Length >= 2 ? result : null;
        }

        public static bool IsInStackTrace(string text)
        {
            var stackTrace = new StackTrace();
            for (int i = 0; i < stackTrace.FrameCount; i++)
            {
                var methodBase = stackTrace.GetFrame(i).GetMethod();
                if (methodBase.Name.Contains(text))
                {
                    return true;
                }
            }

            return false;
        }

        public static int GetRandomInt(int min, int max)
        {
            return Random.Next(min, max + 1);
        }

        public static string GetLauncherAuthor(bool addAuthorString = false)
        {
            if (!addAuthorString)
            {
                return Settings.AuthorName;
            }

            return String.Format("{0}: {1}", ZeroLanguage.Get("APP_AUTHOR"), Settings.AuthorName);
        }

        public static bool CheckVersion(string localVersionNumber, string remoteVersionNumber)
        {
            var versions1 = localVersionNumber.Split('.');
            var versions2 = remoteVersionNumber.Split('.');
            int maxLength = versions1.Length > versions2.Length ? versions1.Length : versions2.Length;

            for (int i = 0; i < maxLength; i++)
            {
                if (versions1.Length < i)
                {
                    versions1[i] = "0";
                }

                if (versions2.Length < i)
                {
                    versions2[i] = "0";
                }
            }

            SystemVersion localVersion = new SystemVersion(string.Join(".", versions1));
            SystemVersion remoteVersion = new SystemVersion(string.Join(".", versions2));

            if (localVersion.CompareTo(remoteVersion) < 0)
            {
                return true;
            }

            return false;
        }

        public static string GetLauncherVersion(bool addVersionString = false, bool addImplodeDot = true)
        {
            if (LauncherVersionData == null)
            {
                string[] data = Assembly.GetExecutingAssembly().GetName().Version.ToString().Split('.');
                if (!string.IsNullOrEmpty(Settings.LauncherVersion))
                {
                    data = Settings.LauncherVersion.Split('.');
                }

                Array.Resize(ref data, data.Length - 1);
                LauncherVersionData = data;
            }

            string launcherVersion = "";
            if (addVersionString)
            {
                launcherVersion = "v";
            }

            if (addImplodeDot)
            {
                return String.Format("{0}{1}", launcherVersion, String.Join(".", LauncherVersionData));
            }

            return String.Format("{0}{1}", launcherVersion, String.Join("", LauncherVersionData));
        }

        public static string Base64Encode(string plainText, int limit = 1)
        {
            if (plainText.IsNull())
            {
                return null;
            }

            for (int i = 0; i < limit; i++)
            {
                plainText = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plainText));
            }

            return plainText;
        }

        public static Vector3 GetCameraForward(bool yReset = false, bool isNormalize = false)
        {
            var forward = new Vector3()
            {
                x = MainCamera.camera.transform.forward.x,
                z = MainCamera.camera.transform.forward.z
            };

            if (!yReset)
            {
                forward.y = MainCamera.camera.transform.forward.y;
            }

            if (isNormalize)
            {
                forward.Normalize();
            }

            return forward;
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public static double CalculatePercent(long one, long two, int max = 100)
        {
            return ((double)one / (double)two) * max;
        }

        public static double CalculateThousandToPercent(int thousand)
        {
            return ((double)thousand / 10.0);
        }

        public static void OpenFolder(string folderPath)
        {
            try
            {
                Directory.CreateDirectory(folderPath);
                Process.Start(folderPath);
            }
            catch (Exception e)
            {
                Log.Error($"Tools.OpenFolder: {e}");
            }
        }

        public static void CreateSubFolders(string localFile)
        {
            var paths = localFile.Split('\\');
            if (paths.Length > 1)
            {
                paths = paths.Take(paths.Length - 1).ToArray();
                if (paths.Length > 1)
                {
                    string folderPath = String.Join("\\", paths);
                    if (folderPath.IsNotNull())
                    {
                        Directory.CreateDirectory(folderPath);
                    }
                }
            }
        }

        public static string CreateMD5(string input)
        {
            if (input.IsNull())
            {
                return null;
            }

            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }

                return sb.ToString();
            }
        }

        public static string GetShortUniqueId()
        {
            var historicalDate = new DateTime(1970, 1, 1, 0, 0, 0);
            var spanTillNow = DateTime.UtcNow.Subtract(historicalDate);

            string shortId = String.Format("{0:0}", spanTillNow.TotalMilliseconds);

            return long.Parse(shortId).ToString("X");
        }

        public static string GetSizeByTextFormat(long size)
        {
            if (size < 1024)
            {
                return string.Format("{0} {1}", size, ZeroLanguage.Get("GAME_SIZE_B"));
            }

            if (size < 1024 * 1024)
            {
                return string.Format("{0} {1}", System.Math.Round((double)size / 1024, 2), ZeroLanguage.Get("GAME_SIZE_KB"));
            }

            if (size < 1024 * 1024 * 1024)
            {
                return string.Format("{0} {1}", System.Math.Round((double)size / 1024 / 1024, 2), ZeroLanguage.Get("GAME_SIZE_MB"));
            }

            return string.Format("{0} {1}", System.Math.Round((double)size / 1024 / 1024 / 1024, 2), ZeroLanguage.Get("GAME_SIZE_GB"));
        }

        public static string GetDateByTextFormat(int unixTimeStamp)
        {
            DateTime dateTime = UnixTimeStampToDateTime(unixTimeStamp);

            string hourText = dateTime.Hour > 10 ? dateTime.Hour.ToString() : "0" + dateTime.Hour;
            string minuteText = dateTime.Minute > 10 ? dateTime.Minute.ToString() : "0" + dateTime.Minute;

            return string.Format("{0} {1} {2} {3}:{4}", dateTime.Day, ZeroLanguage.Get("GAME_MONTH_" + dateTime.Month), dateTime.Year, hourText, minuteText);
        }

        public static DateTime UnixTimeStampToDateTime(int unixTimeStamp)
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds((double)unixTimeStamp).ToLocalTime();
            return dateTime;
        }

        public static long GetFolderSize(string folderPath)
        {
            DirectoryInfo di = new DirectoryInfo(folderPath);
            return di.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
        }

        public static bool IsBepinexInstalled()
        {
            string gamePath = null;

            foreach (var item in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (item.Location.Contains("Managed\\Assembly-CSharp.dll"))
                {
                    gamePath = item.Location.Replace("SubnauticaZero_Data\\Managed\\Assembly-CSharp.dll", "");
                    break;
                }
            }

            if (gamePath.IsNull())
            {
                Log.Error("IsBepinexInstalled: Null Problem");
                return true;
            }
            else
            {
                if (File.Exists(string.Format("{0}doorstop_config.ini", gamePath)))
                {
                    return true;
                }

                if (File.Exists(string.Format("{0}winhttp.dll", gamePath)))
                {
                    return true;
                }

                if (Directory.Exists(string.Format("{0}BepInEx", gamePath)))
                {
                    return true;
                }
            }

            return false;
        }

        public static int GetUnixTime()
        {
            return (int)DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        public static string GetLanguage()
        {
            string language = CultureInfo.CurrentCulture.ToString();
            switch (language)
            {
                case "az-AZ":
                case "bg-BG":
                case "cs-CZ":
                case "de-DE":
                case "en-US":
                case "es-ES":
                case "fi-FI":
                case "fr-FR":
                case "hr-HR":
                case "is-IS":
                case "it-IT":
                case "ja-JP":
                case "ko-KR":
                case "nl-NL":
                case "pl-PL":
                case "pt-BR":
                case "ro-RO":
                case "ru-RU":
                case "sr-CS":
                case "sv-SE":
                case "tr-TR":
                case "uk-UA":
                case "zh-CN":
                    return language;
            }

            Log.Info($"Language Not Found: {language}");
            return "en-US";
        }

        public static string GetComputerLanguage()
        {
            return CultureInfo.CurrentCulture.ToString();
        }
    }
}
