using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace BZLauncher
{
    /// <summary>JSON helpers. Documents are loaded as dictionaries so keys we do not know about are preserved.</summary>
    static class Json
    {
        static readonly JavaScriptSerializer S = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        public static Dictionary<string, object> ReadObject(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var d = S.DeserializeObject(File.ReadAllText(path)) as Dictionary<string, object>;
                    if (d != null) return d;
                }
            }
            catch { }
            return new Dictionary<string, object>();
        }

        public static object ReadAny(string path)
        {
            try { if (File.Exists(path)) return S.DeserializeObject(File.ReadAllText(path)); } catch { }
            return null;
        }

        public static void Write(string path, object value, bool pretty)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string text;
            if (pretty)
            {
                var sb = new StringBuilder();
                Pretty(sb, value, 0);
                text = sb.ToString();
            }
            else text = S.Serialize(value);
            File.WriteAllText(path, text, new UTF8Encoding(false));
        }

        static void Pretty(StringBuilder sb, object v, int indent)
        {
            var d = v as IDictionary<string, object>;
            if (d != null)
            {
                if (d.Count == 0) { sb.Append("{}"); return; }
                sb.Append("{\r\n");
                int i = 0;
                foreach (var kv in d)
                {
                    sb.Append(' ', (indent + 1) * 2).Append(S.Serialize(kv.Key)).Append(": ");
                    Pretty(sb, kv.Value, indent + 1);
                    if (++i < d.Count) sb.Append(',');
                    sb.Append("\r\n");
                }
                sb.Append(' ', indent * 2).Append('}');
                return;
            }
            var list = v as IList;
            if (list != null && !(v is string))
            {
                sb.Append('[');
                for (int i = 0; i < list.Count; i++) { if (i > 0) sb.Append(", "); Pretty(sb, list[i], indent); }
                sb.Append(']');
                return;
            }
            sb.Append(S.Serialize(v));
        }

        public static int ToInt(object v, int fallback)
        {
            try { return Convert.ToInt32(v); } catch { return fallback; }
        }
    }

    /// <summary>The launcher's own settings, stored next to the exe so the app stays portable.</summary>
    static class LauncherSettings
    {
        static string FilePath { get { return Path.Combine(Paths.Base, "launcher.json"); } }

        public static string GamePath
        {
            get { object v; return Json.ReadObject(FilePath).TryGetValue("GamePath", out v) ? v as string : null; }
            set { var d = Json.ReadObject(FilePath); d["GamePath"] = value; Json.Write(FilePath, d, true); }
        }
    }

    static class Paths
    {
        public static readonly string Base = AppDomain.CurrentDomain.BaseDirectory;
        public static string PackageMultiplayer { get { return Path.Combine(Base, "Multiplayer"); } }
        public static string PatcherDir { get { return Path.Combine(Base, "Patcher"); } }
        public static string GameTool { get { return Path.Combine(PatcherDir, "gametool.exe"); } }
        public static string Bootstrap { get { return Path.Combine(PatcherDir, "SubnauticaBootstrap.dll"); } }

        public const string GameExeName = "SubnauticaZero.exe";
        public static string GameExe(string game) { return Path.Combine(game, GameExeName); }
        public static string Managed(string game) { return Path.Combine(game, "SubnauticaZero_Data", "Managed"); }
        public static string Assembly(string game) { return Path.Combine(Managed(game), "Assembly-CSharp.dll"); }
        public static string ModRoot(string game) { return Path.Combine(game, "Multiplayer", "Game"); }
        public static string ConfigFile(string game) { return Path.Combine(ModRoot(game), "Core", "Config.json"); }
        public static string ServersFile(string game) { return Path.Combine(ModRoot(game), "servers.json"); }
        public static string WorldsDir(string game) { return Path.Combine(ModRoot(game), "Saves", "Server"); }
        public static string LogsDir(string game) { return Path.Combine(ModRoot(game), "Logs"); }
    }

    static class GameLocator
    {
        /// <summary>Accepts either SubnauticaZero.exe or its folder. Returns the game folder, or null.</summary>
        public static string Resolve(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            path = path.Trim().Trim('"');
            try
            {
                if (File.Exists(path) && string.Equals(Path.GetFileName(path), Paths.GameExeName, StringComparison.OrdinalIgnoreCase))
                    return Path.GetDirectoryName(Path.GetFullPath(path));
                if (Directory.Exists(path) && File.Exists(Paths.GameExe(path)))
                    return Path.GetFullPath(path).TrimEnd('\\');
            }
            catch { }
            return null;
        }

        public static string AutoDetect()
        {
            // Launcher placed inside the game folder.
            var here = Resolve(Paths.Base);
            if (here != null) return here;

            foreach (var library in SteamLibraries())
            {
                var found = Resolve(Path.Combine(library, "steamapps", "common", "SubnauticaZero"));
                if (found != null) return found;
            }
            return null;
        }

        static IEnumerable<string> SteamLibraries()
        {
            var roots = new List<string>();
            foreach (var key in new[] { @"HKEY_CURRENT_USER\Software\Valve\Steam", @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam" })
            {
                foreach (var name in new[] { "SteamPath", "InstallPath" })
                {
                    try
                    {
                        var v = Registry.GetValue(key, name, null) as string;
                        if (!string.IsNullOrEmpty(v)) roots.Add(v.Replace('/', '\\'));
                    }
                    catch { }
                }
            }
            roots.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\Steam");
            roots.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Steam");

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in roots.ToList())
            {
                if (seen.Add(root)) yield return root;

                var vdf = Path.Combine(root, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(vdf)) continue;

                string text;
                try { text = File.ReadAllText(vdf); } catch { continue; }

                foreach (Match m in Regex.Matches(text, "\"path\"\\s+\"([^\"]+)\""))
                {
                    var lib = m.Groups[1].Value.Replace("\\\\", "\\");
                    if (seen.Add(lib)) yield return lib;
                }
            }
        }
    }

    enum InstallState { NoGame, NotInstalled, NeedsUpdate, Installed }

    static class Installer
    {
        public static bool IsGameRunning()
        {
            try { return Process.GetProcessesByName("SubnauticaZero").Length > 0; } catch { return false; }
        }

        public static bool PackageAvailable()
        {
            return File.Exists(Paths.GameTool) && File.Exists(Paths.Bootstrap) && Directory.Exists(Paths.PackageMultiplayer);
        }

        public static bool IsPatched(string asmPath)
        {
            if (!File.Exists(asmPath)) return false;
            var needle = Encoding.ASCII.GetBytes("SubnauticaBootstrap");
            var data = File.ReadAllBytes(asmPath);
            for (int i = 0; i <= data.Length - needle.Length; i++)
            {
                if (data[i] != needle[0]) continue;
                int j = 1;
                while (j < needle.Length && data[i + j] == needle[j]) j++;
                if (j == needle.Length) return true;
            }
            return false;
        }

        public static InstallState GetState(string game)
        {
            if (game == null) return InstallState.NoGame;
            var asm = Paths.Assembly(game);
            bool patched = IsPatched(asm);
            bool loader = File.Exists(Path.Combine(ModDir(game), "Subnautica.Loader.dll"));
            if (!patched) return InstallState.NotInstalled;
            if (!loader) return InstallState.NeedsUpdate;

            // Installed, but is it the version that ships with this launcher?
            var shipped = Path.Combine(Paths.PackageMultiplayer, "Game", "Dependencies", "Subnautica.Core.dll");
            var current = Path.Combine(ModDir(game), "Dependencies", "Subnautica.Core.dll");
            if (File.Exists(shipped) && !SameFile(shipped, current)) return InstallState.NeedsUpdate;
            return InstallState.Installed;
        }

        static string ModDir(string game) { return Paths.ModRoot(game); }

        static bool SameFile(string a, string b)
        {
            try
            {
                if (!File.Exists(a) || !File.Exists(b)) return false;
                var fa = new FileInfo(a); var fb = new FileInfo(b);
                if (fa.Length != fb.Length) return false;
                return File.ReadAllBytes(a).SequenceEqual(File.ReadAllBytes(b));
            }
            catch { return false; }
        }

        /// <summary>Same steps as Install.bat. Throws on failure with a readable message.</summary>
        public static void Install(string game, Action<string> log)
        {
            if (!PackageAvailable()) throw new InvalidOperationException("The Patcher and Multiplayer folders were not found next to the launcher. Run it from the unzipped folder.");
            if (IsGameRunning()) throw new InvalidOperationException("Close Subnautica: Below Zero first.");

            var asm = Paths.Assembly(game);
            var original = asm + ".original";

            if (!IsPatched(asm))
            {
                File.Copy(asm, original, true);
                log("Backed up Assembly-CSharp.dll to Assembly-CSharp.dll.original");
            }
            else if (!File.Exists(original))
            {
                throw new InvalidOperationException("Assembly-CSharp.dll is already patched but no backup exists. Verify game files in Steam first, then install again.");
            }

            var patched = asm + ".patched";
            if (File.Exists(patched)) File.Delete(patched);

            var args = string.Format("inject \"{0}\" \"{1}\" \"{2}\" \"{3}\"", original, Paths.Bootstrap, patched, Paths.Managed(game));
            int code = Run(Paths.GameTool, args, Paths.PatcherDir, log);
            if (code != 0 || !File.Exists(patched)) throw new InvalidOperationException("The patch tool failed (exit code " + code + ").");

            File.Delete(asm);
            File.Move(patched, asm);
            log("Patched Assembly-CSharp.dll");

            var destination = Path.Combine(game, "Multiplayer");
            if (!string.Equals(Path.GetFullPath(Paths.PackageMultiplayer).TrimEnd('\\'), Path.GetFullPath(destination).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            {
                CopyDirectory(Paths.PackageMultiplayer, destination);
                log("Copied the Multiplayer folder");
            }

            foreach (var sub in new[] { "Logs", "Plugins", "Saves", "Core" })
                Directory.CreateDirectory(Path.Combine(ModDir(game), sub));

            log("Install finished.");
        }

        public static void Uninstall(string game, Action<string> log)
        {
            if (IsGameRunning()) throw new InvalidOperationException("Close Subnautica: Below Zero first.");

            var asm = Paths.Assembly(game);
            var original = asm + ".original";
            if (!File.Exists(original))
                throw new InvalidOperationException("No backup found. Use \"Verify integrity of game files\" in Steam to restore the original.");

            File.Copy(original, asm, true);
            File.Delete(original);
            log("Original Assembly-CSharp.dll restored. The Multiplayer folder (with your saves) was left in place.");
        }

        static int Run(string exe, string args, string workDir, Action<string> log)
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                WorkingDirectory = workDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using (var p = Process.Start(psi))
            {
                p.OutputDataReceived += (s, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) log(e.Data); };
                p.ErrorDataReceived += (s, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) log(e.Data); };
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
                return p.ExitCode;
            }
        }

        static void CopyDirectory(string from, string to)
        {
            Directory.CreateDirectory(to);
            foreach (var file in Directory.GetFiles(from))
                File.Copy(file, Path.Combine(to, Path.GetFileName(file)), true);
            foreach (var dir in Directory.GetDirectories(from))
                CopyDirectory(dir, Path.Combine(to, Path.GetFileName(dir)));
        }
    }

    /// <summary>Multiplayer\Game\Core\Config.json: every option is {"Description": ..., "Value": ...}.</summary>
    static class ConfigStore
    {
        static readonly string[][] Defaults =
        {
            new[] { "ConnectionTimeout", "Connection timeout period. (Type: Number/Second, Default: 120, Min: 60, Max: 300)" },
            new[] { "PlayerName", "Your player name in multiplayer. Leave empty to use your Steam name. 2-24 characters: letters, digits, space, _ - . (Every player on a server needs a different name.)" },
            new[] { "GameExePath", "Full path to SubnauticaZero.exe (or its folder). Leave empty to auto-detect. Example: C:/Program Files (x86)/Steam/steamapps/common/SubnauticaZero/SubnauticaZero.exe" },
            new[] { "ConfigureFirewall", "Ask once for admin permission to allow the game through the Windows Firewall when hosting. (true/false)" },
            new[] { "HostOnPort", "Port to host the game on." },
            new[] { "MaxPlayer", "How many player should max join." },
            new[] { "DefaultJoinPort", "Port to host the game on." },
        };

        static object DefaultValue(string key)
        {
            switch (key)
            {
                case "ConnectionTimeout": return 120;
                case "GameExePath": return "";
                case "PlayerName": return "";
                case "ConfigureFirewall": return true;
                case "HostOnPort": return 7777;
                case "MaxPlayer": return 8;
                default: return 7777;
            }
        }

        public static Dictionary<string, object> Load(string game)
        {
            var doc = Json.ReadObject(Paths.ConfigFile(game));
            foreach (var d in Defaults)
            {
                var item = doc.ContainsKey(d[0]) ? doc[d[0]] as Dictionary<string, object> : null;
                if (item == null)
                {
                    item = new Dictionary<string, object> { { "Description", d[1] }, { "Value", DefaultValue(d[0]) } };
                    doc[d[0]] = item;
                }
                else if (!item.ContainsKey("Value")) item["Value"] = DefaultValue(d[0]);
            }
            return doc;
        }

        public static object Get(Dictionary<string, object> doc, string key)
        {
            var item = doc[key] as Dictionary<string, object>;
            return item["Value"];
        }

        public static void Set(Dictionary<string, object> doc, string key, object value)
        {
            var item = doc[key] as Dictionary<string, object>;
            item["Value"] = value;
        }

        public static void Save(string game, Dictionary<string, object> doc)
        {
            Json.Write(Paths.ConfigFile(game), doc, true);
        }
    }

    class World
    {
        public string Id;
        public int GameMode;
        public DateTime Created;
        public DateTime LastPlayed;
        public long Bytes;

        public string ModeName { get { return GameModes.Name(GameMode); } }
    }

    static class GameModes
    {
        public static readonly string[] Creatable = { "Survival", "Freedom", "Hardcore", "Creative" };

        public static string Name(int mode)
        {
            switch (mode)
            {
                case 0: return "Survival";
                case 1: return "Freedom";
                case 2: return "Hardcore";
                case 3: return "Creative";
                case 100: return "Custom";
                default: return "Mode " + mode;
            }
        }
    }

    static class WorldStore
    {
        public static List<World> Load(string game)
        {
            var list = new List<World>();
            var root = Paths.WorldsDir(game);
            if (!Directory.Exists(root)) return list;

            foreach (var dir in Directory.GetDirectories(root))
            {
                var cfg = Path.Combine(dir, "config.json");
                if (!File.Exists(cfg)) continue;
                var d = Json.ReadObject(cfg);
                object v;
                list.Add(new World
                {
                    Id = Path.GetFileName(dir),
                    GameMode = d.TryGetValue("GameMode", out v) ? Json.ToInt(v, 0) : 0,
                    Created = FromUnix(d.TryGetValue("CreationDate", out v) ? Json.ToInt(v, 0) : 0),
                    LastPlayed = FromUnix(d.TryGetValue("LastPlayedDate", out v) ? Json.ToInt(v, 0) : 0),
                    Bytes = FolderSize(dir),
                });
            }
            return list.OrderByDescending(w => w.LastPlayed).ToList();
        }

        public static void Delete(string game, string id)
        {
            if (string.IsNullOrEmpty(id) || id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return;
            var dir = Path.Combine(Paths.WorldsDir(game), id);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }

        static DateTime FromUnix(int seconds)
        {
            return seconds <= 0 ? DateTime.MinValue : new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(seconds).ToLocalTime();
        }

        static long FolderSize(string dir)
        {
            try { return new DirectoryInfo(dir).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length); } catch { return 0; }
        }

        public static string FormatSize(long bytes)
        {
            if (bytes < 1024 * 1024) return Math.Max(1, bytes / 1024) + " KB";
            return (bytes / 1048576.0).ToString("0.#") + " MB";
        }
    }

    class SavedServer
    {
        public string Id;
        public string Name;
        public string Ip;
        public int Port;

        public string Address { get { return Ip + ":" + Port; } }
    }

    /// <summary>Multiplayer\Game\servers.json: the same list the in-game Join menu uses.</summary>
    static class ServerStore
    {
        public static List<SavedServer> Load(string game)
        {
            var result = new List<SavedServer>();
            var list = Json.ReadAny(Paths.ServersFile(game)) as IList;
            if (list == null) return result;

            foreach (var item in list)
            {
                var d = item as Dictionary<string, object>;
                if (d == null) continue;
                object v;
                result.Add(new SavedServer
                {
                    Id = d.TryGetValue("Id", out v) ? Convert.ToString(v) : Guid.NewGuid().ToString(),
                    Name = d.TryGetValue("Name", out v) ? Convert.ToString(v) : "",
                    Ip = d.TryGetValue("IpAddress", out v) ? Convert.ToString(v) : "",
                    Port = d.TryGetValue("Port", out v) ? Json.ToInt(v, 7777) : 7777,
                });
            }
            return result;
        }

        public static void Save(string game, List<SavedServer> servers)
        {
            var list = servers.Select(s => (object)new Dictionary<string, object>
            {
                { "Id", s.Id }, { "Name", s.Name }, { "IpAddress", s.Ip }, { "Port", s.Port },
            }).ToList();
            Json.Write(Paths.ServersFile(game), list, false);
        }

        /// <summary>Accepts "ip" or "ip:port" (hostnames too).</summary>
        public static bool TryParse(string text, int defaultPort, out string host, out int port)
        {
            host = null; port = defaultPort;
            if (string.IsNullOrWhiteSpace(text)) return false;
            text = text.Trim();

            int colon = text.LastIndexOf(':');
            if (colon > 0 && text.IndexOf(':') == colon)
            {
                if (!int.TryParse(text.Substring(colon + 1), out port) || port < 1 || port > 65535) return false;
                text = text.Substring(0, colon);
            }
            if (!Regex.IsMatch(text, @"^[A-Za-z0-9]([A-Za-z0-9\.\-]*[A-Za-z0-9])?$")) return false;
            host = text;
            return true;
        }
    }

    static class LocalIps
    {
        /// <summary>IPv4 addresses friends can use, with the adapter name. Same filtering as the mod's LanHost.</summary>
        public static List<KeyValuePair<string, string>> Get()
        {
            var list = new List<KeyValuePair<string, string>>();
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up) continue;
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback || nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                    foreach (var u in nic.GetIPProperties().UnicastAddresses)
                    {
                        if (u.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        var ip = u.Address.ToString();
                        if (ip.StartsWith("169.254.") || list.Any(q => q.Key == ip)) continue;
                        list.Add(new KeyValuePair<string, string>(ip, nic.Name));
                    }
                }
            }
            catch { }
            return list;
        }
    }
}
