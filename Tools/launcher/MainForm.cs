using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BZLauncher
{
    class MainForm : Form
    {
        static readonly Color Bg = Color.FromArgb(24, 28, 36);
        static readonly Color Surface = Color.FromArgb(34, 40, 52);
        static readonly Color Nav = Color.FromArgb(18, 21, 28);
        static readonly Color Fg = Color.FromArgb(232, 235, 240);
        static readonly Color Muted = Color.FromArgb(150, 158, 172);
        static readonly Color Accent = Color.FromArgb(40, 150, 214);
        static readonly Color Danger = Color.FromArgb(190, 64, 64);
        static readonly Color Good = Color.FromArgb(80, 190, 120);
        static readonly Color Warn = Color.FromArgb(230, 170, 60);

        string game;
        bool busy;

        readonly Dictionary<string, Panel> panels = new Dictionary<string, Panel>();
        readonly Dictionary<string, Button> navButtons = new Dictionary<string, Button>();
        TextBox output;

        // Play
        Label lblGame, lblStatus;
        Button btnInstall, btnUninstall, btnLaunch, btnBrowse, btnAuto;

        // Host
        ListView lvWorlds;
        Button btnHost, btnDeleteWorld, btnRefreshWorlds, btnNewHost, btnCopyIp;
        ComboBox cbMode;
        ListBox lbAddresses;
        Label lblHostHint;

        // Join
        ListView lvServers;
        TextBox txtAddress, txtName;
        Button btnJoinSaved, btnRemoveServer, btnJoinAddress, btnSaveServer;

        // Settings
        TextBox txtExe;
        CheckBox chkFirewall;
        NumericUpDown numHostPort, numJoinPort, numMaxPlayers, numTimeout;
        Button btnSaveSettings, btnBrowse2;

        // Log
        TextBox txtGameLog;
        Timer logTimer;

        public MainForm(string startTab = "Play")
        {
            Text = "Subnautica: Below Zero Multiplayer Launcher";
            ClientSize = new Size(960, 640);
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Bg;
            ForeColor = Fg;
            Font = new Font("Segoe UI", 9.5f);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            BuildLayout();
            BuildPlay();
            BuildHost();
            BuildJoin();
            BuildSettings();
            BuildLog();

            logTimer = new Timer { Interval = 1500 };
            logTimer.Tick += (s, e) => RefreshGameLog();

            var saved = GameLocator.Resolve(LauncherSettings.GamePath);
            if (saved == null)
            {
                saved = GameLocator.AutoDetect();
                if (saved != null) LauncherSettings.GamePath = saved;
            }
            game = saved;

            Print(game == null ? "Game not found. Use Browse on the Play tab to pick SubnauticaZero.exe." : "Game: " + game);
            if (!Installer.PackageAvailable()) Print("Note: the Patcher and Multiplayer folders are missing next to the launcher, so Install is unavailable.");

            RefreshAll();
            ShowPanel(panels.ContainsKey(startTab) ? startTab : "Play");
        }

        // ---------------------------------------------------------------- layout helpers

        void BuildLayout()
        {
            var nav = new Panel { Dock = DockStyle.Left, Width = 180, BackColor = Nav };
            var title = new Label { Text = "Subnautica\nBelow Zero", Font = new Font("Segoe UI Semibold", 15f), ForeColor = Fg, AutoSize = false, Height = 80, Dock = DockStyle.Top, Padding = new Padding(18, 22, 0, 0) };
            var sub = new Label { Text = "Multiplayer · LAN", ForeColor = Muted, Dock = DockStyle.Top, Height = 28, Padding = new Padding(18, 0, 0, 0) };

            var items = new[] { "Play", "Host", "Join", "Settings", "Game log" };
            for (int i = items.Length - 1; i >= 0; i--)
            {
                var name = items[i];
                var b = new Button { Text = "   " + name, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Top, Height = 44, FlatStyle = FlatStyle.Flat, ForeColor = Fg, BackColor = Nav, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 10.5f) };
                b.FlatAppearance.BorderSize = 0;
                b.FlatAppearance.MouseOverBackColor = Surface;
                b.Click += (s, e) => ShowPanel(name);
                navButtons[name] = b;
                nav.Controls.Add(b);
            }
            nav.Controls.Add(sub);
            nav.Controls.Add(title);

            output = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Bottom, Height = 110, BackColor = Nav, ForeColor = Muted, BorderStyle = BorderStyle.None, Font = new Font("Consolas", 9f) };

            var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(26, 22, 26, 10) };
            foreach (var n in items)
            {
                var p = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = Bg };
                panels[n] = p;
                content.Controls.Add(p);
            }

            Controls.Add(content);
            Controls.Add(output);
            Controls.Add(nav);
        }

        Label L(Control parent, string text, int x, int y, bool muted = false, float size = 0, bool bold = false)
        {
            var l = new Label { Text = text, Location = new Point(x, y), AutoSize = true, ForeColor = muted ? Muted : Fg };
            if (size > 0 || bold) l.Font = new Font(bold ? "Segoe UI Semibold" : "Segoe UI", size > 0 ? size : 9.5f);
            parent.Controls.Add(l);
            return l;
        }

        Button B(Control parent, string text, int x, int y, int w, EventHandler click, Color? color = null, int h = 36)
        {
            var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, h), FlatStyle = FlatStyle.Flat, BackColor = color ?? Surface, ForeColor = Color.White, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.Click += click;
            parent.Controls.Add(b);
            return b;
        }

        TextBox T(Control parent, int x, int y, int w)
        {
            var t = new TextBox { Location = new Point(x, y), Width = w, BackColor = Surface, ForeColor = Fg, BorderStyle = BorderStyle.FixedSingle };
            parent.Controls.Add(t);
            return t;
        }

        NumericUpDown N(Control parent, int x, int y, int min, int max, int value)
        {
            var n = new NumericUpDown { Location = new Point(x, y), Width = 110, Minimum = min, Maximum = max, Value = value, BackColor = Surface, ForeColor = Fg, BorderStyle = BorderStyle.FixedSingle };
            parent.Controls.Add(n);
            return n;
        }

        ListView LV(Control parent, int x, int y, int w, int h, params string[] columns)
        {
            var lv = new ListView { Location = new Point(x, y), Size = new Size(w, h), View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, BorderStyle = BorderStyle.None, BackColor = Surface, ForeColor = Fg, HeaderStyle = ColumnHeaderStyle.Nonclickable, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            int colWidth = (w - 4) / columns.Length;
            foreach (var c in columns) lv.Columns.Add(c, colWidth);
            parent.Controls.Add(lv);
            return lv;
        }

        void ShowPanel(string name)
        {
            foreach (var kv in panels) kv.Value.Visible = kv.Key == name;
            foreach (var kv in navButtons) kv.Value.BackColor = kv.Key == name ? Surface : Nav;
            logTimer.Enabled = name == "Game log";

            if (name == "Host") { RefreshWorlds(); RefreshAddresses(); }
            if (name == "Join") RefreshServers();
            if (name == "Settings") LoadSettings();
            if (name == "Game log") RefreshGameLog();
        }

        void Print(string line)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string>(Print), line); return; }
            output.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + line + "\r\n");
        }

        // ---------------------------------------------------------------- Play

        void BuildPlay()
        {
            var p = panels["Play"];
            L(p, "Play", 0, 0, false, 20, true);
            L(p, "Subnautica: Below Zero with the LAN multiplayer mod", 0, 36, true);

            L(p, "Game folder", 0, 86, true);
            lblGame = L(p, "", 0, 108);
            btnBrowse = B(p, "Browse...", 0, 140, 110, (s, e) => BrowseGame());
            btnAuto = B(p, "Auto-detect", 120, 140, 110, (s, e) => AutoDetectGame());

            L(p, "Multiplayer mod", 0, 206, true);
            lblStatus = L(p, "", 0, 228, false, 11);
            btnInstall = B(p, "Install", 0, 264, 150, async (s, e) => { await RunInstall(); }, Accent);
            btnUninstall = B(p, "Uninstall", 160, 264, 120, async (s, e) => await RunUninstall());

            btnLaunch = B(p, "Launch game", 0, 336, 280, (s, e) => LaunchGame(null), Good, 52);
            btnLaunch.Font = new Font("Segoe UI Semibold", 12f);
            L(p, "Opens the normal main menu. Use the Host and Join tabs to go straight into a game.", 0, 396, true);
        }

        void BrowseGame()
        {
            using (var dlg = new OpenFileDialog { Title = "Select SubnauticaZero.exe", Filter = "SubnauticaZero.exe|SubnauticaZero.exe", CheckFileExists = true })
            {
                if (game != null) dlg.InitialDirectory = game;
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                var folder = GameLocator.Resolve(dlg.FileName);
                if (folder == null) { MessageBox.Show(this, "That is not SubnauticaZero.exe.", Text); return; }
                SetGame(folder);
            }
        }

        void AutoDetectGame()
        {
            var folder = GameLocator.AutoDetect();
            if (folder == null) { MessageBox.Show(this, "Could not find Subnautica: Below Zero. Use Browse to pick SubnauticaZero.exe.", Text); return; }
            SetGame(folder);
        }

        void SetGame(string folder)
        {
            game = folder;
            LauncherSettings.GamePath = folder;
            Print("Game: " + folder);
            RefreshAll();
        }

        void RefreshAll()
        {
            lblGame.Text = game ?? "Not found";
            lblGame.ForeColor = game == null ? Warn : Fg;

            var state = Installer.GetState(game);
            switch (state)
            {
                case InstallState.NoGame: lblStatus.Text = "Game not found"; lblStatus.ForeColor = Warn; break;
                case InstallState.NotInstalled: lblStatus.Text = "Not installed"; lblStatus.ForeColor = Warn; break;
                case InstallState.NeedsUpdate: lblStatus.Text = "Needs install / update (the game or mod files changed)"; lblStatus.ForeColor = Warn; break;
                default: lblStatus.Text = "Installed and up to date"; lblStatus.ForeColor = Good; break;
            }

            btnInstall.Text = state == InstallState.NotInstalled ? "Install" : state == InstallState.NeedsUpdate ? "Update / Repair" : "Reinstall";
            btnInstall.Enabled = game != null && !busy;
            btnUninstall.Enabled = game != null && !busy && state != InstallState.NotInstalled;
            btnLaunch.Enabled = game != null && !busy;
            btnHost.Enabled = btnNewHost.Enabled = btnJoinSaved.Enabled = btnJoinAddress.Enabled = game != null && !busy;
            txtExe.Text = game == null ? "" : Paths.GameExe(game);
        }

        async Task<bool> RunInstall()
        {
            if (game == null || busy) return false;
            busy = true; RefreshAll();
            try
            {
                var g = game;
                await Task.Run(() => Installer.Install(g, Print));
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                Print("Access denied writing to the game folder.");
                AskElevate();
                return false;
            }
            catch (Exception ex)
            {
                Print("Install failed: " + ex.Message);
                MessageBox.Show(this, ex.Message, "Install failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally { busy = false; RefreshAll(); }
        }

        async Task RunUninstall()
        {
            if (game == null || busy) return;
            if (MessageBox.Show(this, "Restore the original game file? Your multiplayer saves are kept.", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            busy = true; RefreshAll();
            try
            {
                var g = game;
                await Task.Run(() => Installer.Uninstall(g, Print));
            }
            catch (UnauthorizedAccessException) { Print("Access denied writing to the game folder."); AskElevate(); }
            catch (Exception ex) { Print("Uninstall failed: " + ex.Message); MessageBox.Show(this, ex.Message, "Uninstall failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { busy = false; RefreshAll(); }
        }

        void AskElevate()
        {
            if (MessageBox.Show(this, "The game folder needs administrator permission. Restart the launcher as administrator?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                Process.Start(new ProcessStartInfo(Application.ExecutablePath) { Verb = "runas", UseShellExecute = true });
                Application.Exit();
            }
            catch (Exception ex) { Print("Could not restart as administrator: " + ex.Message); }
        }

        // ---------------------------------------------------------------- launching

        /// <summary>Starts the game. Host/join requests need the mod, so offer to install it first.</summary>
        async void LaunchGame(string arguments)
        {
            if (game == null) return;
            if (Installer.IsGameRunning())
            {
                MessageBox.Show(this, "Subnautica: Below Zero is already running. Close it first.", Text);
                return;
            }

            var state = Installer.GetState(game);
            if (state != InstallState.Installed && Installer.PackageAvailable())
            {
                var msg = arguments == null
                    ? "The multiplayer mod is not installed / up to date. Install it now?"
                    : "Hosting and joining need the multiplayer mod. Install / update it now?";
                if (MessageBox.Show(this, msg, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (!await RunInstall()) return;
                }
                else if (arguments != null) return;
            }
            else if (state != InstallState.Installed && arguments != null)
            {
                MessageBox.Show(this, "The multiplayer mod is not installed.", Text);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(Paths.GameExe(game), arguments ?? "") { WorkingDirectory = game, UseShellExecute = false });
                Print("Started the game" + (arguments == null ? "." : " (" + arguments + ")."));
            }
            catch (Exception ex)
            {
                Print("Could not start the game: " + ex.Message);
                MessageBox.Show(this, ex.Message, "Could not start the game", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ---------------------------------------------------------------- Host

        void BuildHost()
        {
            var p = panels["Host"];
            L(p, "Host", 0, 0, false, 20, true);
            lblHostHint = L(p, "The game starts and goes straight into hosting the world you pick. Keep it running while your friends play.", 0, 36, true);

            lvWorlds = LV(p, 0, 70, 700, 170, "World", "Mode", "Created", "Last played", "Size");
            lvWorlds.DoubleClick += (s, e) => HostSelected();

            btnHost = B(p, "Host selected world", 0, 250, 170, (s, e) => HostSelected(), Accent);
            btnDeleteWorld = B(p, "Delete", 180, 250, 90, (s, e) => DeleteSelectedWorld(), Danger);
            btnRefreshWorlds = B(p, "Refresh", 280, 250, 90, (s, e) => { RefreshWorlds(); RefreshAddresses(); });

            L(p, "New world", 0, 306, true);
            cbMode = new ComboBox { Location = new Point(0, 330), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Surface, ForeColor = Fg };
            cbMode.Items.AddRange(GameModes.Creatable);
            cbMode.SelectedIndex = 0;
            p.Controls.Add(cbMode);
            btnNewHost = B(p, "Create and host", 160, 326, 150, (s, e) => LaunchGame("-bzmp-host new:" + cbMode.SelectedItem), Accent);

            L(p, "Friends join with (copy one)", 380, 306, true);
            lbAddresses = new ListBox { Location = new Point(380, 330), Size = new Size(290, 76), BackColor = Surface, ForeColor = Fg, BorderStyle = BorderStyle.None };
            p.Controls.Add(lbAddresses);
            btnCopyIp = B(p, "Copy", 680, 330, 70, (s, e) => CopyAddress());
        }

        void RefreshWorlds()
        {
            lvWorlds.Items.Clear();
            if (game == null) return;
            foreach (var w in WorldStore.Load(game))
            {
                var item = new ListViewItem(new[]
                {
                    w.Id.Substring(0, Math.Min(8, w.Id.Length)),
                    w.ModeName,
                    w.Created == DateTime.MinValue ? "" : w.Created.ToString("g"),
                    w.LastPlayed == DateTime.MinValue ? "" : w.LastPlayed.ToString("g"),
                    WorldStore.FormatSize(w.Bytes),
                }) { Tag = w.Id };
                lvWorlds.Items.Add(item);
            }
            if (lvWorlds.Items.Count > 0) lvWorlds.Items[0].Selected = true;
        }

        void RefreshAddresses()
        {
            lbAddresses.Items.Clear();
            int port = 7777;
            if (game != null)
            {
                try { port = Json.ToInt(ConfigStore.Get(ConfigStore.Load(game), "HostOnPort"), 7777); } catch { }
            }

            var ips = LocalIps.Get();
            foreach (var ip in ips) lbAddresses.Items.Add(ip.Key + ":" + port + "   (" + ip.Value + ")");
            if (ips.Count == 0) lbAddresses.Items.Add("No network connection found");
            else lbAddresses.SelectedIndex = 0;
        }

        void CopyAddress()
        {
            var text = lbAddresses.SelectedItem as string;
            if (string.IsNullOrEmpty(text) || text.StartsWith("No network")) return;
            var address = text.Split(' ')[0];
            try { Clipboard.SetText(address); Print("Copied " + address); } catch (Exception ex) { Print("Could not copy: " + ex.Message); }
        }

        void HostSelected()
        {
            if (lvWorlds.SelectedItems.Count == 0) { MessageBox.Show(this, "Pick a world first, or create a new one.", Text); return; }
            LaunchGame("-bzmp-host " + lvWorlds.SelectedItems[0].Tag);
        }

        void DeleteSelectedWorld()
        {
            if (game == null || lvWorlds.SelectedItems.Count == 0) return;
            var id = (string)lvWorlds.SelectedItems[0].Tag;
            if (MessageBox.Show(this, "Delete this world permanently?\r\n" + id, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try { WorldStore.Delete(game, id); Print("Deleted world " + id); }
            catch (Exception ex) { Print("Could not delete: " + ex.Message); }
            RefreshWorlds();
        }

        // ---------------------------------------------------------------- Join

        void BuildJoin()
        {
            var p = panels["Join"];
            L(p, "Join", 0, 0, false, 20, true);
            L(p, "Type the host's address (ip or ip:port). ZeroTier / LAN addresses both work.", 0, 36, true);

            lvServers = LV(p, 0, 70, 700, 150, "Name", "Address");
            lvServers.DoubleClick += (s, e) => JoinSaved();
            lvServers.SelectedIndexChanged += (s, e) =>
            {
                if (lvServers.SelectedItems.Count > 0)
                {
                    var sv = (SavedServer)lvServers.SelectedItems[0].Tag;
                    txtAddress.Text = sv.Address; txtName.Text = sv.Name;
                }
            };

            btnJoinSaved = B(p, "Join selected", 0, 230, 140, (s, e) => JoinSaved(), Accent);
            btnRemoveServer = B(p, "Remove", 150, 230, 100, (s, e) => RemoveServer(), Danger);

            L(p, "Address", 0, 290, true);
            txtAddress = T(p, 0, 314, 300);
            L(p, "Name (to save)", 320, 290, true);
            txtName = T(p, 320, 314, 240);

            btnJoinAddress = B(p, "Join", 0, 352, 140, (s, e) => JoinTyped(), Accent);
            btnSaveServer = B(p, "Save to list", 150, 352, 120, (s, e) => SaveServer());
        }

        void RefreshServers()
        {
            lvServers.Items.Clear();
            if (game == null) return;
            foreach (var sv in ServerStore.Load(game))
                lvServers.Items.Add(new ListViewItem(new[] { sv.Name, sv.Address }) { Tag = sv });
        }

        int DefaultJoinPort()
        {
            try { return Json.ToInt(ConfigStore.Get(ConfigStore.Load(game), "DefaultJoinPort"), 7777); } catch { return 7777; }
        }

        void JoinSaved()
        {
            if (lvServers.SelectedItems.Count == 0) { MessageBox.Show(this, "Pick a saved server, or type an address below.", Text); return; }
            var sv = (SavedServer)lvServers.SelectedItems[0].Tag;
            LaunchGame("-bzmp-join " + sv.Address);
        }

        void JoinTyped()
        {
            string host; int port;
            if (!ServerStore.TryParse(txtAddress.Text, DefaultJoinPort(), out host, out port))
            {
                MessageBox.Show(this, "Enter the host address like 192.168.1.20 or 192.168.1.20:7777.", Text);
                return;
            }
            LaunchGame("-bzmp-join " + host + ":" + port);
        }

        void SaveServer()
        {
            if (game == null) return;
            string host; int port;
            if (!ServerStore.TryParse(txtAddress.Text, DefaultJoinPort(), out host, out port))
            {
                MessageBox.Show(this, "Enter the host address like 192.168.1.20 or 192.168.1.20:7777.", Text);
                return;
            }
            var name = txtName.Text.Trim();
            if (name.Length == 0) name = host;

            var list = ServerStore.Load(game);
            var existing = list.FirstOrDefault(s => s.Ip == host && s.Port == port);
            if (existing != null) existing.Name = name;
            else list.Add(new SavedServer { Id = Guid.NewGuid().ToString(), Name = name, Ip = host, Port = port });

            ServerStore.Save(game, list);
            Print("Saved server " + name + " (" + host + ":" + port + ")");
            RefreshServers();
        }

        void RemoveServer()
        {
            if (game == null || lvServers.SelectedItems.Count == 0) return;
            var sv = (SavedServer)lvServers.SelectedItems[0].Tag;
            var list = ServerStore.Load(game).Where(s => s.Id != sv.Id).ToList();
            ServerStore.Save(game, list);
            RefreshServers();
        }

        // ---------------------------------------------------------------- Settings

        void BuildSettings()
        {
            var p = panels["Settings"];
            L(p, "Settings", 0, 0, false, 20, true);
            L(p, "Saved to Multiplayer\\Game\\Core\\Config.json in the game folder. Applied the next time the game starts.", 0, 36, true);

            L(p, "SubnauticaZero.exe", 0, 80, true);
            txtExe = T(p, 0, 104, 460);
            txtExe.ReadOnly = true;
            txtExe.TabStop = false;
            btnBrowse2 = B(p, "Browse...", 470, 100, 100, (s, e) => BrowseGame(), null, 30);

            L(p, "Host port (UDP)", 0, 156, true);
            numHostPort = N(p, 0, 180, 1, 65535, 7777);
            L(p, "Default join port", 160, 156, true);
            numJoinPort = N(p, 160, 180, 1, 65535, 7777);
            L(p, "Max players", 320, 156, true);
            numMaxPlayers = N(p, 320, 180, 1, 64, 8);
            L(p, "Connection timeout (s)", 480, 156, true);
            numTimeout = N(p, 480, 180, 60, 300, 120);

            chkFirewall = new CheckBox { Text = "Ask once to open the Windows Firewall for the game when hosting (needs admin)", Location = new Point(0, 232), AutoSize = true, ForeColor = Fg };
            p.Controls.Add(chkFirewall);

            btnSaveSettings = B(p, "Save settings", 0, 280, 150, (s, e) => SaveSettings(), Accent);
            B(p, "Open saves folder", 160, 280, 150, (s, e) => OpenFolder(game == null ? null : Paths.WorldsDir(game)));
            B(p, "Open game folder", 320, 280, 150, (s, e) => OpenFolder(game));
        }

        void LoadSettings()
        {
            txtExe.Text = game == null ? "" : Paths.GameExe(game);
            if (game == null) return;
            try
            {
                var cfg = ConfigStore.Load(game);
                numHostPort.Value = Clamp(Json.ToInt(ConfigStore.Get(cfg, "HostOnPort"), 7777), numHostPort);
                numJoinPort.Value = Clamp(Json.ToInt(ConfigStore.Get(cfg, "DefaultJoinPort"), 7777), numJoinPort);
                numMaxPlayers.Value = Clamp(Json.ToInt(ConfigStore.Get(cfg, "MaxPlayer"), 8), numMaxPlayers);
                numTimeout.Value = Clamp(Json.ToInt(ConfigStore.Get(cfg, "ConnectionTimeout"), 120), numTimeout);
                chkFirewall.Checked = Convert.ToBoolean(ConfigStore.Get(cfg, "ConfigureFirewall"));
            }
            catch (Exception ex) { Print("Could not read Config.json: " + ex.Message); }
        }

        static decimal Clamp(int value, NumericUpDown n)
        {
            return Math.Max(n.Minimum, Math.Min(n.Maximum, value));
        }

        void SaveSettings()
        {
            if (game == null) { MessageBox.Show(this, "Pick the game folder first (Play tab).", Text); return; }
            try
            {
                var cfg = ConfigStore.Load(game);
                ConfigStore.Set(cfg, "GameExePath", Paths.GameExe(game).Replace('\\', '/'));
                ConfigStore.Set(cfg, "HostOnPort", (int)numHostPort.Value);
                ConfigStore.Set(cfg, "DefaultJoinPort", (int)numJoinPort.Value);
                ConfigStore.Set(cfg, "MaxPlayer", (int)numMaxPlayers.Value);
                ConfigStore.Set(cfg, "ConnectionTimeout", (int)numTimeout.Value);
                ConfigStore.Set(cfg, "ConfigureFirewall", chkFirewall.Checked);
                ConfigStore.Save(game, cfg);
                Print("Settings saved.");
            }
            catch (UnauthorizedAccessException) { Print("Access denied writing Config.json."); AskElevate(); }
            catch (Exception ex) { Print("Could not save settings: " + ex.Message); MessageBox.Show(this, ex.Message, "Could not save settings", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        void OpenFolder(string path)
        {
            try
            {
                if (path == null) return;
                Directory.CreateDirectory(path);
                Process.Start("explorer.exe", "\"" + path + "\"");
            }
            catch (Exception ex) { Print("Could not open folder: " + ex.Message); }
        }

        // ---------------------------------------------------------------- Game log

        void BuildLog()
        {
            var p = panels["Game log"];
            L(p, "Game log", 0, 0, false, 20, true);
            L(p, "Latest Multiplayer\\Game\\Logs file. Useful when something does not connect.", 0, 36, true);
            txtGameLog = new TextBox { Location = new Point(0, 70), Size = new Size(700, 360), Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, BackColor = Surface, ForeColor = Fg, BorderStyle = BorderStyle.None, Font = new Font("Consolas", 9f), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };
            p.Controls.Add(txtGameLog);
        }

        void RefreshGameLog()
        {
            if (game == null) { txtGameLog.Text = "Game not found."; return; }
            try
            {
                var dir = Paths.LogsDir(game);
                var file = Directory.Exists(dir) ? new DirectoryInfo(dir).GetFiles("*.log").OrderByDescending(f => f.LastWriteTime).FirstOrDefault() : null;
                if (file == null) { txtGameLog.Text = "No log yet. Start the game once."; return; }

                string text;
                using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var sr = new StreamReader(fs, Encoding.UTF8))
                    text = sr.ReadToEnd();

                var lines = text.Split('\n');
                var tail = string.Join("\r\n", lines.Skip(Math.Max(0, lines.Length - 300)).Select(l => l.TrimEnd('\r')));
                if (tail == txtGameLog.Text) return;

                txtGameLog.Text = tail;
                txtGameLog.SelectionStart = txtGameLog.TextLength;
                txtGameLog.ScrollToCaret();
            }
            catch (Exception ex) { txtGameLog.Text = "Could not read the log: " + ex.Message; }
        }
    }
}
