namespace Subnautica.API.Features.Helper
{
    using Newtonsoft.Json;
    using System;
    using System.IO;

    public class ModConfigFormat
    {
        public ModConfigFormatItem ConnectionTimeout { get; set; } = new ModConfigFormatItem(120, "Connection timeout period. (Type: Number/Second, Default: 120, Min: 60, Max: 300)");

        public ModConfigFormatItem PlayerName { get; set; } = new ModConfigFormatItem("", "Your player name in multiplayer. Leave empty to use your Steam name. 2-24 characters: letters, digits, space, _ - . (Every player on a server needs a different name.)");
        public ModConfigFormatItem GameExePath { get; set; } = new ModConfigFormatItem("", "Full path to SubnauticaZero.exe (or its folder). Leave empty to auto-detect. Example: C:/Program Files (x86)/Steam/steamapps/common/SubnauticaZero/SubnauticaZero.exe");
        public ModConfigFormatItem ConfigureFirewall { get; set; } = new ModConfigFormatItem(true, "Ask once for admin permission to allow the game through the Windows Firewall when hosting. (true/false)");
        public ModConfigFormatItem HostOnPort { get; set; } = new(7777, "Port to host the game on.");
        public ModConfigFormatItem MaxPlayer { get; set; } = new(8, "How many player should max join.");
        public ModConfigFormatItem DefaultJoinPort { get; set; } = new(7777, "Port to host the game on.");

        public void Initialize()
        {
            var filePath = Paths.GetLauncherGameCorePath("Config.json");

            try
            {
                if (File.Exists(filePath))
                {
                    var config = JsonConvert.DeserializeObject<ModConfigFormat>(File.ReadAllText(filePath));
                    if (config != null)
                    {
                        if (config.ConnectionTimeout != null && config.ConnectionTimeout.GetInt() >= 60 && config.ConnectionTimeout.GetInt() <= 300)
                        {
                            this.ConnectionTimeout.SetValue(config.ConnectionTimeout.GetInt());
                        }

                        if (config.PlayerName != null) PlayerName.SetValue(config.PlayerName.Value);
                        if (config.GameExePath != null) GameExePath.SetValue(config.GameExePath.Value);
                        if (config.ConfigureFirewall != null) ConfigureFirewall.SetValue(config.ConfigureFirewall.Value);
                        if (config.HostOnPort != null) HostOnPort.SetValue(config.HostOnPort.Value);
                        if (config.MaxPlayer != null) MaxPlayer.SetValue(config.MaxPlayer.Value);
                        if (config.DefaultJoinPort != null) DefaultJoinPort.SetValue(config.DefaultJoinPort.Value);
                    }
                }

                // Always rewrite so options added in newer versions show up in the file.
                File.WriteAllText(filePath, JsonConvert.SerializeObject(this, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Log.Error($"ModConfigFormat.Initialize - Exception: {ex}");
            }
        }
    }

    public class ModConfigFormatItem
    {
        public string Description { get; set; }

        public object Value { get; set; }

        public ModConfigFormatItem(object value, string description)
        {
            this.Value = value;
            this.Description = description;
        }

        public void SetValue(object value)
        {
            this.Value = value;
        }

        public string GetString(string defaultValue = null)
        {
            return this.Value == null ? defaultValue : this.Value.ToString();
        }

        public bool GetBool(bool defaultValue = false)
        {
            try
            {
                return Convert.ToBoolean(this.Value);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public int GetInt(int defaultValue = -1)
        {
            try
            {
                return Convert.ToInt32(this.Value);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }
    }
}
