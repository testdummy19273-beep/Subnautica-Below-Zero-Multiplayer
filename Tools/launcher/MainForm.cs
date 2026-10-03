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
        const string ModVersion = "1.4.1";
        const string ProjectUrl = "https://github.com/testdummy19273-beep/Subnautica-Below-Zero-Multiplayer";

        string game;
        bool busy;
        bool loading;

        readonly Dictionary<string, Panel> pages = new Dictionary<string, Panel>();
        readonly Dictionary<string, NavItem> navItems = new Dictionary<string, NavItem>();
        string currentPage = "Play game";

        TextBox output;
        Label statusLabel;
        Panel statusStrip;

        // the game process we started for hosting
        Process hosted;
        string hostedId;
        Timer statusTimer;

        // Play game
        HeroPanel hero;
        RoundPanel playCard;
        RoundButton btnPlay, btnMod, btnBrowse, btnAuto, btnUninstall;
        Label lblDesc, lblGameCaption, lblGame;

        // Servers
        Panel serverList;
        RoundButton btnCreate;
        readonly Dictionary<string, KeyValuePair<Label, Label>> serverStatus = new Dictionary<string, KeyValuePair<Label, Label>>();
        readonly Dictionary<string, RoundButton> serverButtons = new Dictionary<string, RoundButton>();
        bool stopping;

        // Manage
        World cur, orig;
        bool origFirewall;
        Label lblManageName, lblManageDot, lblManageStatus;
        Field fName;
        NumField nLimit, nAuto, nPort;
        Chip[] modeChips;
        Toggle tBackup, tFirewall;
        RoundButton btnUndo, btnSave, btnStart;
        Panel addrPanel;
        Label lblStartHint;

        // Join
        Panel joinList;
        Field fAddress, fSaveName;
        RoundButton btnJoin, btnSaveServer;

        // Options
        Field fPlayer, fExe;
        NumField nJoinPort, nTimeout;

        // Game log
        TextBox txtGameLog;
        Timer logTimer;

        public MainForm(string startTab = "Play game")
        {
            Text = "Subnautica: Below Zero Multiplayer";
            ClientSize = new Size(Ui.D(1180), Ui.D(760));
            MinimumSize = new Size(Ui.D(1020), Ui.D(680));
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Ui.Bg;
            ForeColor = Ui.Fg;
            Font = Ui.Font(9.5f);
            Ui.DarkTitle(this);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            BuildLayout();
            BuildPlay();
            BuildServers();
            BuildManage();
            BuildJoin();
            BuildLog();
            BuildInfo();
            BuildOptions();

            logTimer = new Timer { Interval = 1500 };
            logTimer.Tick += (s, e) => RefreshGameLog();
            statusTimer = new Timer { Interval = 2000 };
            statusTimer.Tick += (s, e) => UpdateHostedStatus();
            statusTimer.Start();

            var saved = GameLocator.Resolve(LauncherSettings.GamePath);
            if (saved == null)
            {
                saved = GameLocator.AutoDetect();
                if (saved != null) LauncherSettings.GamePath = saved;
            }
            game = saved;

            Print(game == null ? "Game not found. Use Browse on the Play game page to pick SubnauticaZero.exe." : "Game: " + game);
            if (!Installer.PackageAvailable()) Print("Note: the Patcher and Multiplayer folders are missing next to the launcher, so Install is unavailable.");

            RefreshAll();
            ShowPage(MapTab(startTab));

            // --tab manage opens the most recent server's Manage page (handy for screenshots and tests).
            if (game != null && string.Equals(startTab, "manage", StringComparison.OrdinalIgnoreCase))
            {
                var first = WorldStore.Load(game).FirstOrDefault();
                if (first != null) OpenManage(first.Id);
            }
        }

        /// <summary>Accepts the older tab names too (used by --tab).</summary>
        static string MapTab(string name)
        {
            switch ((name ?? "").ToLowerInvariant())
            {
                case "host": case "servers": return "Servers";
                case "join": return "Join";
                case "settings": case "options": return "Options";
                case "game log": case "log": return "Game log";
                case "info": return "Info";
                default: return "Play game";
            }
        }

        // ---------------------------------------------------------------- layout

        void BuildLayout()
        {
            var nav = new Panel { Dock = DockStyle.Left, Width = Ui.D(250), BackColor = Ui.Nav };

            var title = new Label { Text = "BELOW ZERO", Font = new Font("Segoe UI Black", 19f, FontStyle.Italic), ForeColor = Color.White, BackColor = Ui.Nav, AutoSize = true, Location = new Point(Ui.D(26), Ui.D(26)) };
            var sub = new Label { Text = "MULTIPLAYER  ·  LAN", Font = Ui.Font(8f, true), ForeColor = Ui.Muted, BackColor = Ui.Nav, AutoSize = true, Location = new Point(Ui.D(28), Ui.D(66)) };
            nav.Controls.Add(title);
            nav.Controls.Add(sub);

            int y = Ui.D(112);
            y = NavSection(nav, "PLAY", y);
            y = NavEntry(nav, "Play game", "", y);
            y = NavEntry(nav, "Servers", "", y);
            y = NavEntry(nav, "Join", "", y);
            y += Ui.D(10);
            y = NavSection(nav, "EXPLORE", y);
            y = NavEntry(nav, "Game log", "", y);
            y = NavEntry(nav, "Info", "", y);

            var options = new NavItem("Options", "") { Dock = DockStyle.Bottom };
            options.Click += (s, e) => ShowPage("Options");
            navItems["Options"] = options;
            nav.Controls.Add(options);

            output = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Bottom, Height = Ui.D(140), BackColor = Ui.Nav, ForeColor = Ui.Muted, BorderStyle = BorderStyle.None, Font = new Font("Consolas", 9f), Visible = false };
            Ui.DarkScroll(output);

            statusStrip = new Panel { Dock = DockStyle.Bottom, Height = Ui.D(28), BackColor = Ui.Nav, Cursor = Cursors.Hand };
            statusLabel = new Label { Text = "Ready", AutoSize = false, Dock = DockStyle.Fill, ForeColor = Ui.Muted, BackColor = Ui.Nav, Padding = new Padding(Ui.D(14), 0, 0, 0), TextAlign = ContentAlignment.MiddleLeft, Font = Ui.Font(8.5f) };
            statusStrip.Controls.Add(statusLabel);
            EventHandler toggleLog = (s, e) => output.Visible = !output.Visible;
            statusStrip.Click += toggleLog;
            statusLabel.Click += toggleLog;

            var content = new Panel { Dock = DockStyle.Fill, BackColor = Ui.Bg };
            foreach (var name in new[] { "Play game", "Servers", "Manage", "Join", "Game log", "Info", "Options" })
            {
                var p = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = Ui.Bg };
                pages[name] = p;
                content.Controls.Add(p);
            }

            Controls.Add(content);
            Controls.Add(output);
            Controls.Add(statusStrip);
            Controls.Add(nav);
        }

        int NavSection(Panel nav, string text, int y)
        {
            var l = new Label { Text = text, Font = Ui.Font(8f, true), ForeColor = Color.FromArgb(120, 120, 120), BackColor = Ui.Nav, AutoSize = true, Location = new Point(Ui.D(28), y) };
            nav.Controls.Add(l);
            return y + Ui.D(26);
        }

        int NavEntry(Panel nav, string name, string glyph, int y)
        {
            var item = new NavItem(name, glyph) { Location = new Point(0, y), Width = nav.Width };
            item.Click += (s, e) => ShowPage(name);
            navItems[name] = item;
            nav.Controls.Add(item);
            return y + item.Height;
        }

        void ShowPage(string name)
        {
            currentPage = name;
            foreach (var kv in pages) kv.Value.Visible = kv.Key == name;
            var navName = name == "Manage" ? "Servers" : name;
            foreach (var kv in navItems) { kv.Value.Selected = kv.Key == navName; kv.Value.Invalidate(); }
            logTimer.Enabled = name == "Game log";

            pages[name].PerformLayout();
            if (name == "Play game") LayoutPlay();
            if (name == "Servers") RefreshServers();
            if (name == "Join") RefreshJoinList();
            if (name == "Options") LoadOptions();
            if (name == "Game log") RefreshGameLog();
        }

        void Print(string line)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string>(Print), line); return; }
            output.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + line + "\r\n");
            statusLabel.Text = line;
        }

        // ---------------------------------------------------------------- builders

        Label Lbl(Control parent, string text, float size = 9.5f, bool bold = false, Color? color = null, int x = 0, int y = 0)
        {
            var l = new Label { Text = text, AutoSize = true, Font = Ui.Font(size, bold), ForeColor = color ?? Ui.Fg, BackColor = parent.BackColor, Location = new Point(x, y) };
            parent.Controls.Add(l);
            return l;
        }

        /// <summary>Title and subtitle at the top of a page.</summary>
        Panel Header(Panel page, string title, string subtitle)
        {
            var h = new Panel { Dock = DockStyle.Top, Height = Ui.D(104), BackColor = Ui.Bg };
            Lbl(h, title, 24f, true, Color.White, Ui.D(36), Ui.D(30));
            var sub = Lbl(h, subtitle, 9.5f, false, Color.FromArgb(215, 215, 215), Ui.D(38), Ui.D(78));
            sub.MaximumSize = new Size(Ui.D(900), 0);
            return h;
        }

        Panel ScrollPanel()
        {
            var p = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Ui.Bg, Padding = new Padding(Ui.D(36), 0, Ui.D(36), 0) };
            Ui.DarkScroll(p);
            return p;
        }

        Control Labeled(string label, Control field, int height = 80)
        {
            var p = new Panel { Height = Ui.D(height), Dock = DockStyle.Fill, Margin = new Padding(0, 0, Ui.D(16), Ui.D(8)), BackColor = Ui.Bg };
            Lbl(p, label, 7.5f, true, Color.FromArgb(150, 150, 150), 0, 0);
            field.Location = new Point(0, Ui.D(24));
            field.Width = p.Width;
            field.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            p.Controls.Add(field);
            return p;
        }

        TableLayoutPanel Form2()
        {
            var t = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, BackColor = Ui.Bg, Padding = new Padding(0, 0, 0, Ui.D(30)) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            return t;
        }

        void AddRow(TableLayoutPanel t, Control a, Control b = null)
        {
            int row = t.RowCount++;
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            if (b == null)
            {
                t.Controls.Add(a, 0, row);
                t.SetColumnSpan(a, 2);
            }
            else
            {
                t.Controls.Add(a, 0, row);
                t.Controls.Add(b, 1, row);
            }
        }

        Control Heading(string text)
        {
            var p = new Panel { Height = Ui.D(40), Dock = DockStyle.Fill, Margin = new Padding(0, Ui.D(14), 0, 0), BackColor = Ui.Bg };
            Lbl(p, text, 7.5f, true, Color.FromArgb(150, 150, 150), 0, Ui.D(16));
            return p;
        }

        Control OptionRow(string text, string hint, Toggle toggle)
        {
            var p = new Panel { Height = Ui.D(hint == null ? 52 : 66), Dock = DockStyle.Fill, Margin = new Padding(0, 0, Ui.D(16), 0), BackColor = Ui.Bg };
            Lbl(p, text, 10.5f, false, Ui.Fg, 0, Ui.D(hint == null ? 14 : 10));
            if (hint != null) Lbl(p, hint, 8.5f, false, Ui.Muted, 0, Ui.D(34));
            toggle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            toggle.Location = new Point(p.Width - toggle.Width, Ui.D(hint == null ? 12 : 16));
            p.Controls.Add(toggle);
            return p;
        }

        RoundButton LinkRow(string text, string glyph, Action click, bool danger = false)
        {
            var b = new RoundButton(text) { LeftAlign = true, Glyph = glyph, Fill = Ui.Bg, HotFill = Ui.Card, Height = Ui.D(44), Dock = DockStyle.Fill, Margin = new Padding(0, 0, Ui.D(16), 0), TextColor = danger ? Ui.Danger : Ui.Fg };
            b.Font = Ui.Font(10f);
            b.Click += (s, e) => click();
            return b;
        }

        // ---------------------------------------------------------------- Play game

        void BuildPlay()
        {
            var p = pages["Play game"];
            hero = new HeroPanel();
            p.Controls.Add(hero);

            lblDesc = new Label { AutoSize = false, ForeColor = Color.FromArgb(220, 220, 220), BackColor = Ui.Bg, Text = "Play Subnautica: Below Zero together with your friends on the same network (or over a VPN such as ZeroTier). The world runs inside the host's game, so there is nothing else to install or sign up for. Open Servers to host a world, or Join to connect to a friend." };
            p.Controls.Add(lblDesc);

            lblGameCaption = Lbl(p, "GAME FOLDER", 7.5f, true, Color.FromArgb(150, 150, 150));
            lblGame = Lbl(p, "", 9.5f);
            btnBrowse = new RoundButton("Browse...") { Height = Ui.D(34), Width = Ui.D(110) };
            btnBrowse.Click += (s, e) => BrowseGame();
            btnAuto = new RoundButton("Auto-detect") { Height = Ui.D(34), Width = Ui.D(110) };
            btnAuto.Click += (s, e) => AutoDetectGame();
            btnUninstall = new RoundButton("Uninstall mod") { Height = Ui.D(34), Width = Ui.D(130) };
            btnUninstall.Click += async (s, e) => await RunUninstall();
            p.Controls.Add(btnBrowse);
            p.Controls.Add(btnAuto);
            p.Controls.Add(btnUninstall);

            playCard = new RoundPanel { Size = new Size(Ui.D(390), Ui.D(214)), BackColor = Color.FromArgb(22, 22, 22), Radius = Ui.D(14) };
            Lbl(playCard, "BZ MULTIPLAYER", 7.5f, true, Color.FromArgb(150, 150, 150), Ui.D(22), Ui.D(18));
            Lbl(playCard, "LAN edition " + ModVersion, 9f, true, Color.White, Ui.D(22), Ui.D(34));

            btnPlay = RoundButton.Primary("PLAY");
            btnPlay.SubText = "MULTIPLAYER";
            btnPlay.Font = Ui.Font(11f, true);
            btnPlay.SetBounds(Ui.D(20), Ui.D(70), playCard.Width - Ui.D(40), Ui.D(64));
            btnPlay.Click += async (s, e) => await LaunchGame(null);
            playCard.Controls.Add(btnPlay);

            btnMod = new RoundButton("INSTALL MOD");
            btnMod.Font = Ui.Font(10f, true);
            btnMod.SetBounds(Ui.D(20), Ui.D(144), playCard.Width - Ui.D(40), Ui.D(52));
            btnMod.Click += async (s, e) => { await RunInstall(); };
            playCard.Controls.Add(btnMod);

            p.Controls.Add(playCard);
            p.Resize += (s, e) => LayoutPlay();
        }

        void LayoutPlay()
        {
            var p = pages["Play game"];
            if (p.Width < 50) return;
            int h = (int)(p.Height * 0.5);
            hero.SetBounds(0, 0, p.Width, h);
            playCard.Location = new Point(p.Width - playCard.Width - Ui.D(32), h - Ui.D(76));
            playCard.BringToFront();

            int left = Ui.D(38);
            lblDesc.SetBounds(left, h + Ui.D(24), Math.Max(Ui.D(200), p.Width - playCard.Width - Ui.D(110)), Ui.D(84));
            lblGameCaption.Location = new Point(left, lblDesc.Bottom + Ui.D(14));
            lblGame.Location = new Point(left, lblGameCaption.Bottom + Ui.D(4));
            lblGame.MaximumSize = new Size(lblDesc.Width, 0);
            btnBrowse.Location = new Point(left, lblGame.Bottom + Ui.D(14));
            btnAuto.Location = new Point(btnBrowse.Right + Ui.D(10), btnBrowse.Top);
            btnUninstall.Location = new Point(btnAuto.Right + Ui.D(10), btnBrowse.Top);
        }

        void BrowseGame()
        {
            using (var dlg = new OpenFileDialog { Title = "Select SubnauticaZero.exe", Filter = "SubnauticaZero.exe|SubnauticaZero.exe", CheckFileExists = true })
            {
                if (game != null) dlg.InitialDirectory = game;
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                var folder = GameLocator.Resolve(dlg.FileName);
                if (folder == null) { DarkBox.Show(this, Text, "That is not SubnauticaZero.exe."); return; }
                SetGame(folder);
            }
        }

        void AutoDetectGame()
        {
            var folder = GameLocator.AutoDetect();
            if (folder == null) { DarkBox.Show(this, Text, "Could not find Subnautica: Below Zero. Use Browse to pick SubnauticaZero.exe."); return; }
            SetGame(folder);
        }

        void SetGame(string folder)
        {
            game = folder;
            LauncherSettings.GamePath = folder;
            Print("Game: " + folder);
            RefreshAll();
            if (currentPage == "Servers") RefreshServers();
            if (currentPage == "Join") RefreshJoinList();
            if (currentPage == "Options") LoadOptions();
        }

        void RefreshAll()
        {
            lblGame.Text = game ?? "Not found";
            lblGame.ForeColor = game == null ? Ui.Warn : Ui.Fg;

            var state = Installer.GetState(game);
            string status;
            switch (state)
            {
                case InstallState.NoGame: status = "Game not found"; break;
                case InstallState.NotInstalled: status = "Mod not installed"; break;
                case InstallState.NeedsUpdate: status = "Mod needs an update"; break;
                default: status = "Mod installed and up to date"; break;
            }
            btnMod.Text = state == InstallState.NotInstalled ? "INSTALL MOD" : state == InstallState.NeedsUpdate ? "UPDATE / REPAIR MOD" : "REINSTALL MOD";
            btnMod.SubText = status.ToUpperInvariant();
            btnMod.Enabled = game != null && !busy;
            btnPlay.Enabled = game != null && !busy;
            btnUninstall.Enabled = game != null && !busy && state != InstallState.NotInstalled;
            btnCreate.Enabled = game != null && !busy;
            LayoutPlay();
            btnMod.Invalidate();
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
                DarkBox.Show(this, "Install failed", ex.Message);
                return false;
            }
            finally { busy = false; RefreshAll(); }
        }

        async Task RunUninstall()
        {
            if (game == null || busy) return;
            if (!DarkBox.Show(this, Text, "Restore the original game file?\r\nYour multiplayer saves are kept.", "Uninstall", "Cancel", true)) return;

            busy = true; RefreshAll();
            try
            {
                var g = game;
                await Task.Run(() => Installer.Uninstall(g, Print));
            }
            catch (UnauthorizedAccessException) { Print("Access denied writing to the game folder."); AskElevate(); }
            catch (Exception ex) { Print("Uninstall failed: " + ex.Message); DarkBox.Show(this, "Uninstall failed", ex.Message); }
            finally { busy = false; RefreshAll(); }
        }

        void AskElevate()
        {
            if (!DarkBox.Show(this, Text, "The game folder needs administrator permission.\r\nRestart the launcher as administrator?", "Restart", "Not now")) return;
            try
            {
                Process.Start(new ProcessStartInfo(Application.ExecutablePath) { Verb = "runas", UseShellExecute = true });
                Application.Exit();
            }
            catch (Exception ex) { Print("Could not restart as administrator: " + ex.Message); }
        }

        // ---------------------------------------------------------------- launching

        /// <summary>Starts the game. Host/join requests need the mod, so offer to install it first.</summary>
        async Task LaunchGame(string arguments, string hostWorldId = null)
        {
            if (game == null) return;
            if (Installer.IsGameRunning())
            {
                DarkBox.Show(this, Text, "Subnautica: Below Zero is already running. Close it first.");
                return;
            }

            var state = Installer.GetState(game);
            if (state != InstallState.Installed && Installer.PackageAvailable())
            {
                var msg = arguments == null
                    ? "The multiplayer mod is not installed / up to date.\r\nInstall it now?"
                    : "Hosting and joining need the multiplayer mod.\r\nInstall / update it now?";
                if (DarkBox.Show(this, Text, msg, "Install", "Not now"))
                {
                    if (!await RunInstall()) return;
                }
                else if (arguments != null) return;
            }
            else if (state != InstallState.Installed && arguments != null)
            {
                DarkBox.Show(this, Text, "The multiplayer mod is not installed.");
                return;
            }

            try
            {
                var p = Process.Start(new ProcessStartInfo(Paths.GameExe(game), arguments ?? "") { WorkingDirectory = game, UseShellExecute = false });
                hosted = hostWorldId != null ? p : null;
                hostedId = hostWorldId;
                Print("Started the game" + (arguments == null ? "." : " (" + arguments + ")."));
                UpdateHostedStatus();
            }
            catch (Exception ex)
            {
                Print("Could not start the game: " + ex.Message);
                DarkBox.Show(this, "Could not start the game", ex.Message);
            }
        }

        // ---------------------------------------------------------------- Servers

        bool IsHosting(string id)
        {
            return hosted != null && hostedId == id && !SafeExited(hosted);
        }

        static bool SafeExited(Process p) { try { return p.HasExited; } catch { return true; } }

        void UpdateHostedStatus()
        {
            if (hosted != null && SafeExited(hosted)) { hosted = null; hostedId = null; }

            foreach (var kv in serverStatus)
            {
                bool on = IsHosting(kv.Key);
                kv.Value.Key.ForeColor = on ? Ui.Good : Ui.Danger;
                kv.Value.Value.Text = on ? "Online" : "Offline";

                RoundButton button;
                if (serverButtons.TryGetValue(kv.Key, out button)) StyleStartButton(button, on);
            }

            if (cur != null && lblManageDot != null)
            {
                bool on = IsHosting(cur.Id);
                lblManageDot.ForeColor = on ? Ui.Good : Ui.Danger;
                lblManageStatus.Text = (on ? "Online   " : "Offline   ") + cur.ModeName;
                btnStart.Text = on ? "STOP SERVER" : "START SERVER";
                btnStart.Fill = on ? Ui.Danger : Ui.Accent;
                btnStart.HotFill = on ? Color.FromArgb(255, 70, 80) : Ui.AccentHot;
                btnStart.Enabled = on ? !stopping : game != null && !busy;
                btnStart.Invalidate();
            }
        }

        /// <summary>Start (blue) while the server is offline, Stop (red) while it runs.</summary>
        void StyleStartButton(RoundButton button, bool online)
        {
            button.Text = online ? "Stop" : "Start";
            button.Fill = online ? Ui.Danger : Ui.Accent;
            button.HotFill = online ? Color.FromArgb(255, 70, 80) : Ui.AccentHot;
            button.Enabled = !stopping || !online;
            button.Invalidate();
        }

        /// <summary>Asks the game to close like the window's X button does; the mod saves and stops the server on quit.</summary>
        async Task StopWorld()
        {
            var p = hosted;
            if (p == null || SafeExited(p) || stopping) return;
            if (!DarkBox.Show(this, Text, "Stop the server? Everyone connected is disconnected. The world is saved first.", "Stop", "Cancel", true)) return;

            stopping = true;
            UpdateHostedStatus();
            Print("Stopping the server (saving the world)...");
            try
            {
                p.Refresh();
                p.CloseMainWindow();
                for (int i = 0; i < 40 && !SafeExited(p); i++) await Task.Delay(500);

                if (!SafeExited(p))
                {
                    if (DarkBox.Show(this, Text, "The game did not close within 20 seconds. Force close it? Progress since the last auto save is lost.", "Force close", "Keep waiting", true))
                    {
                        try { p.Kill(); } catch { }
                    }
                }
            }
            catch (Exception ex) { Print("Could not stop the game: " + ex.Message); }
            finally
            {
                stopping = false;
                UpdateHostedStatus();
            }

            if (SafeExited(p)) Print("Server stopped.");
        }

        void BuildServers()
        {
            var p = pages["Servers"];
            serverList = ScrollPanel();
            p.Controls.Add(serverList);
            serverList.Resize += (s, e) => LayoutCards(serverList);

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = Ui.D(120), BackColor = Ui.Bg, Padding = new Padding(Ui.D(36), Ui.D(12), Ui.D(36), Ui.D(24)) };
            var bar = new RoundPanel { Dock = DockStyle.Fill, BackColor = Ui.Card };
            btnCreate = RoundButton.Primary("CREATE NEW SERVER");
            btnCreate.Font = Ui.Font(10f, true);
            btnCreate.Size = new Size(Ui.D(300), Ui.D(48));
            btnCreate.Click += (s, e) => CreateServer();
            bar.Controls.Add(btnCreate);
            bar.Resize += (s, e) => btnCreate.Location = new Point((bar.Width - btnCreate.Width) / 2, (bar.Height - btnCreate.Height) / 2);
            bottom.Controls.Add(bar);
            p.Controls.Add(bottom);

            p.Controls.Add(Header(p, "Servers", "Host a world from your own computer. Press Start: the game opens and loads the world, then your friends join with your address."));
        }

        void LayoutCards(Panel list)
        {
            int y = Ui.D(4);
            int w = Math.Max(Ui.D(200), list.ClientSize.Width - list.Padding.Horizontal);
            foreach (Control c in list.Controls)
            {
                c.SetBounds(list.Padding.Left, y + list.AutoScrollPosition.Y, w, c.Height);
                y += c.Height + Ui.D(14);
            }
            list.AutoScrollMinSize = new Size(0, y);
        }

        RoundPanel Card(Panel list, int height)
        {
            var c = new RoundPanel { Height = Ui.D(height), Width = Math.Max(Ui.D(200), list.ClientSize.Width - list.Padding.Horizontal), BackColor = Ui.Card };
            list.Controls.Add(c);
            return c;
        }

        RoundButton CardButton(RoundPanel card, string text, int width, int rightOffset, Action click, bool primary = false)
        {
            var b = primary ? RoundButton.Primary(text) : new RoundButton(text);
            b.Size = new Size(Ui.D(width), Ui.D(42));
            b.Location = new Point(card.Width - rightOffset - b.Width, (card.Height - b.Height) / 2);
            b.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            b.Click += (s, e) => click();
            card.Controls.Add(b);
            return b;
        }

        void RefreshServers()
        {
            foreach (Control c in serverList.Controls.Cast<Control>().ToList()) { serverList.Controls.Remove(c); c.Dispose(); }
            serverStatus.Clear();
            serverButtons.Clear();

            if (game == null)
            {
                var c = Card(serverList, 90);
                Lbl(c, "Game not found. Pick SubnauticaZero.exe on the Play game page first.", 10f, false, Ui.Warn, Ui.D(24), Ui.D(34));
                LayoutCards(serverList);
                return;
            }

            var worlds = WorldStore.Load(game);
            if (worlds.Count == 0)
            {
                var c = Card(serverList, 90);
                Lbl(c, "No servers yet. Press CREATE NEW SERVER below to make your first world.", 10f, false, Ui.Muted, Ui.D(24), Ui.D(34));
            }

            foreach (var w in worlds)
            {
                var world = w;
                var card = Card(serverList, 96);
                var icon = new ServerIcon { Location = new Point(Ui.D(20), (card.Height - Ui.D(56)) / 2) };
                card.Controls.Add(icon);

                Lbl(card, world.DisplayName, 13f, true, Color.White, Ui.D(94), Ui.D(22));
                var dot = Lbl(card, "●", 9f, false, Ui.Danger, Ui.D(94), Ui.D(54));
                var state = Lbl(card, "Offline", 9.5f, false, Ui.Muted, Ui.D(112), Ui.D(54));
                int maxPlayers = world.MaxPlayer > 0 ? world.MaxPlayer : GlobalInt("MaxPlayer", 8);
                Lbl(card, world.ModeName + "    " + maxPlayers + " players max    " + WorldStore.FormatSize(world.Bytes), 9.5f, false, Ui.Muted, Ui.D(176), Ui.D(54));
                serverStatus[world.Id] = new KeyValuePair<Label, Label>(dot, state);

                var startButton = CardButton(card, "Start", 100, Ui.D(20), () => { var t = IsHosting(world.Id) ? StopWorld() : StartWorld(world); }, true);
                serverButtons[world.Id] = startButton;
                CardButton(card, "Manage", 110, Ui.D(130), () => OpenManage(world.Id));
                CardButton(card, "Open world folder", 170, Ui.D(250), () => OpenFolder(Path.Combine(Paths.WorldsDir(game), world.Id)));
            }

            LayoutCards(serverList);
            UpdateHostedStatus();
        }

        int GlobalInt(string key, int fallback)
        {
            try { return Json.ToInt(ConfigStore.Get(ConfigStore.Load(game), key), fallback); } catch { return fallback; }
        }

        void CreateServer()
        {
            if (game == null) return;
            using (var f = new Form())
            {
                f.Text = "Create new server";
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.StartPosition = FormStartPosition.CenterParent;
                f.MaximizeBox = f.MinimizeBox = false;
                f.ShowInTaskbar = false;
                f.BackColor = Ui.Card;
                f.ForeColor = Ui.Fg;
                f.Font = Ui.Font(9.5f);
                f.ClientSize = new Size(Ui.D(520), Ui.D(290));
                Ui.DarkTitle(f);

                Lbl(f, "SERVER NAME", 7.5f, true, Color.FromArgb(150, 150, 150), Ui.D(28), Ui.D(24));
                var name = new Field { Location = new Point(Ui.D(28), Ui.D(48)), Width = Ui.D(464), Text = "My world" };
                name.Box.MaxLength = 40;
                f.Controls.Add(name);

                Lbl(f, "GAMEMODE", 7.5f, true, Color.FromArgb(150, 150, 150), Ui.D(28), Ui.D(112));
                var chips = new List<Chip>();
                int x = Ui.D(28);
                foreach (var mode in GameModes.Creatable)
                {
                    var chip = new Chip(mode) { Location = new Point(x, Ui.D(138)), Selected = mode == "Survival", BackColor = Ui.Card };
                    chip.Click += (s, e) => { foreach (var c in chips) { c.Selected = c == chip; c.Invalidate(); } };
                    chips.Add(chip);
                    f.Controls.Add(chip);
                    x += chip.Width + Ui.D(10);
                }

                int selected = -1;
                bool ok = false;
                var create = RoundButton.Primary("Create");
                create.SetBounds(f.ClientSize.Width - Ui.D(28) - Ui.D(130), Ui.D(222), Ui.D(130), Ui.D(42));
                create.Click += (s, e) =>
                {
                    if (name.Text.Trim().Length == 0) { name.Focus(); return; }
                    selected = chips.FindIndex(c => c.Selected);
                    ok = true;
                    f.Close();
                };
                var cancel = new RoundButton("Cancel");
                cancel.SetBounds(create.Left - Ui.D(10) - Ui.D(110), Ui.D(222), Ui.D(110), Ui.D(42));
                cancel.Click += (s, e) => f.Close();
                f.Controls.Add(create);
                f.Controls.Add(cancel);

                f.ShowDialog(this);
                if (!ok || selected < 0) return;

                try
                {
                    var id = WorldStore.Create(game, name.Text.Trim(), selected);
                    Print("Created server \"" + name.Text.Trim() + "\" (" + GameModes.Name(selected) + ")");
                    OpenManage(id);
                }
                catch (UnauthorizedAccessException) { Print("Access denied writing to the game folder."); AskElevate(); }
                catch (Exception ex) { Print("Could not create the server: " + ex.Message); DarkBox.Show(this, "Could not create the server", ex.Message); }
            }
        }

        async Task StartWorld(World w)
        {
            if (game == null) return;
            if (Installer.IsGameRunning())
            {
                DarkBox.Show(this, Text, "Subnautica: Below Zero is already running. Close it first.");
                return;
            }

            try
            {
                var cfg = ConfigStore.Load(game);
                ConfigStore.Set(cfg, "HostOnPort", w.Port > 0 ? w.Port : Json.ToInt(ConfigStore.Get(cfg, "HostOnPort"), 7777));
                ConfigStore.Set(cfg, "MaxPlayer", w.MaxPlayer > 0 ? w.MaxPlayer : Json.ToInt(ConfigStore.Get(cfg, "MaxPlayer"), 8));
                ConfigStore.Set(cfg, "AutoSaveInterval", w.AutoSave > 0 ? w.AutoSave : Json.ToInt(ConfigStore.Get(cfg, "AutoSaveInterval"), 5));
                ConfigStore.Save(game, cfg);
            }
            catch (UnauthorizedAccessException) { Print("Access denied writing Config.json."); AskElevate(); return; }
            catch (Exception ex) { Print("Could not apply the server settings: " + ex.Message); DarkBox.Show(this, "Could not apply the server settings", ex.Message); return; }

            if (w.BackupOnStart)
            {
                var g = game; var id = w.Id;
                try
                {
                    await Task.Run(() => { WorldStore.Backup(g, id, "auto"); WorldStore.PruneAutoBackups(g, id, 5); });
                    Print("Backed up \"" + w.DisplayName + "\" before starting.");
                }
                catch (Exception ex) { Print("Backup failed (starting anyway): " + ex.Message); }
            }

            await LaunchGame("-bzmp-host " + w.Id, w.Id);
        }

        // ---------------------------------------------------------------- Manage

        void BuildManage()
        {
            var p = pages["Manage"];

            var scroll = ScrollPanel();
            p.Controls.Add(scroll);

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = Ui.D(112), BackColor = Ui.Bg, Padding = new Padding(Ui.D(36), Ui.D(8), Ui.D(36), Ui.D(20)) };
            var bar = new RoundPanel { Dock = DockStyle.Fill, BackColor = Ui.Card };
            lblStartHint = Lbl(bar, "Keep the game running while your friends play.", 9.5f, false, Ui.Muted, Ui.D(24), Ui.D(26));
            btnStart = RoundButton.Primary("START SERVER");
            btnStart.Font = Ui.Font(11f, true);
            btnStart.Size = new Size(Ui.D(340), Ui.D(52));
            btnStart.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnStart.Click += async (s, e) =>
            {
                if (cur == null) return;
                if (IsHosting(cur.Id)) { await StopWorld(); return; }
                if (Dirty()) SaveManage();
                await StartWorld(cur);
            };
            bar.Controls.Add(btnStart);
            bar.Resize += (s, e) => btnStart.Location = new Point(bar.Width - btnStart.Width - Ui.D(16), (bar.Height - btnStart.Height) / 2);
            bottom.Controls.Add(bar);
            p.Controls.Add(bottom);

            var table = Form2();
            scroll.Controls.Add(table);

            // header row
            var head = new Panel { Height = Ui.D(130), Dock = DockStyle.Fill, Margin = new Padding(0, 0, Ui.D(16), 0), BackColor = Ui.Bg };
            var back = new RoundButton("") { Fill = Ui.Bg, HotFill = Ui.Card, Size = new Size(Ui.D(44), Ui.D(44)), Location = new Point(Ui.D(0), Ui.D(10)) };
            back.Font = new Font("Segoe MDL2 Assets", 14f);
            back.Click += (s, e) => ShowPage("Servers");
            head.Controls.Add(back);
            head.Controls.Add(new ServerIcon { Location = new Point(Ui.D(8), Ui.D(64)), Size = new Size(Ui.D(56), Ui.D(56)) });
            lblManageDot = Lbl(head, "●", 9f, false, Ui.Danger, Ui.D(80), Ui.D(68));
            lblManageStatus = Lbl(head, "Offline", 9.5f, false, Ui.Muted, Ui.D(98), Ui.D(68));
            lblManageName = Lbl(head, "", 20f, true, Color.White, Ui.D(78), Ui.D(86));

            btnSave = RoundButton.Primary("SAVE");
            btnSave.Size = new Size(Ui.D(100), Ui.D(40));
            btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSave.Click += (s, e) => SaveManage();
            btnUndo = new RoundButton("UNDO") { Fill = Color.FromArgb(150, 30, 40), HotFill = Color.FromArgb(190, 40, 52) };
            btnUndo.Size = new Size(Ui.D(100), Ui.D(40));
            btnUndo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnUndo.Click += (s, e) => { if (orig != null) FillManage(Clone(orig)); };
            head.Controls.Add(btnSave);
            head.Controls.Add(btnUndo);
            head.Resize += (s, e) =>
            {
                btnSave.Location = new Point(head.Width - btnSave.Width, Ui.D(70));
                btnUndo.Location = new Point(btnSave.Left - Ui.D(10) - btnUndo.Width, Ui.D(70));
            };
            AddRow(table, head);

            fName = new Field();
            fName.Box.MaxLength = 40;
            nLimit = new NumField { Min = 1, Max = 64 };
            AddRow(table, Labeled("SERVER NAME", fName), Labeled("PLAYER LIMIT", nLimit));

            var modePanel = new Panel { Height = Ui.D(86), Dock = DockStyle.Fill, Margin = new Padding(0, 0, Ui.D(16), Ui.D(8)), BackColor = Ui.Bg };
            Lbl(modePanel, "GAMEMODE  (chosen when the world is created)", 7.5f, true, Color.FromArgb(150, 150, 150), 0, 0);
            modeChips = new Chip[GameModes.Creatable.Length];
            int x = 0;
            for (int i = 0; i < modeChips.Length; i++)
            {
                modeChips[i] = new Chip(GameModes.Creatable[i]) { Location = new Point(x, Ui.D(26)), Locked = true, Cursor = Cursors.Default, BackColor = Ui.Bg };
                modePanel.Controls.Add(modeChips[i]);
                x += modeChips[i].Width + Ui.D(10);
            }
            AddRow(table, modePanel);

            nAuto = new NumField { Min = 1, Max = 600 };
            nPort = new NumField { Min = 1024, Max = 65535 };
            AddRow(table, Labeled("AUTO SAVE INTERVAL (S)", nAuto), Labeled("SERVER PORT (UDP)", nPort));

            AddRow(table, Heading("OPTIONS"));
            tBackup = new Toggle();
            AddRow(table, OptionRow("Back up this world every time it starts", "Keeps the last 5 automatic backups. Use Restore a backup below to go back.", tBackup));
            tFirewall = new Toggle();
            AddRow(table, OptionRow("Allow the game through the Windows Firewall", "Asks once for administrator permission when hosting. Applies to all servers.", tFirewall));

            AddRow(table, Heading("FRIENDS JOIN WITH"));
            addrPanel = new Panel { Dock = DockStyle.Fill, Height = Ui.D(52), Margin = new Padding(0, 0, Ui.D(16), 0), BackColor = Ui.Bg };
            AddRow(table, addrPanel);

            AddRow(table, Heading("ADVANCED"));
            AddRow(table, LinkRow("Open world folder", "", () => { if (cur != null) OpenFolder(Path.Combine(Paths.WorldsDir(game), cur.Id)); }));
            AddRow(table, LinkRow("Back up now", "", () => { var t = BackupNow(); }));
            AddRow(table, LinkRow("Restore a backup", "", RestoreBackup));
            AddRow(table, LinkRow("Delete server", "", DeleteServer, true));

            EventHandler changed = (s, e) => UpdateDirty();
            fName.TextValueChanged += changed;
            nLimit.TextValueChanged += changed;
            nAuto.TextValueChanged += changed;
            nPort.TextValueChanged += (s, e) => { UpdateDirty(); RefreshAddresses(); };
            tBackup.CheckedChanged += changed;
            tFirewall.CheckedChanged += changed;
        }

        static World Clone(World w)
        {
            return new World { Id = w.Id, GameMode = w.GameMode, Created = w.Created, LastPlayed = w.LastPlayed, Bytes = w.Bytes, Name = w.Name, Port = w.Port, MaxPlayer = w.MaxPlayer, AutoSave = w.AutoSave, BackupOnStart = w.BackupOnStart };
        }

        void OpenManage(string id)
        {
            if (game == null) return;
            var w = WorldStore.Load(game).FirstOrDefault(x => x.Id == id);
            if (w == null) { DarkBox.Show(this, Text, "That world no longer exists."); RefreshServers(); return; }

            var cfg = ConfigStore.Load(game);
            if (w.Port <= 0) w.Port = Json.ToInt(ConfigStore.Get(cfg, "HostOnPort"), 7777);
            if (w.MaxPlayer <= 0) w.MaxPlayer = Json.ToInt(ConfigStore.Get(cfg, "MaxPlayer"), 8);
            if (w.AutoSave <= 0) w.AutoSave = Json.ToInt(ConfigStore.Get(cfg, "AutoSaveInterval"), 5);
            origFirewall = SafeBool(ConfigStore.Get(cfg, "ConfigureFirewall"));

            orig = w;
            FillManage(Clone(w));
            ShowPage("Manage");
        }

        static bool SafeBool(object v) { try { return Convert.ToBoolean(v); } catch { return true; } }

        void FillManage(World w)
        {
            loading = true;
            cur = w;
            fName.Text = w.DisplayName;
            nLimit.Value = w.MaxPlayer;
            nAuto.Value = w.AutoSave;
            nPort.Value = w.Port;
            tBackup.Checked = w.BackupOnStart;
            tFirewall.Checked = origFirewall;
            for (int i = 0; i < modeChips.Length; i++) { modeChips[i].Selected = i == w.GameMode; modeChips[i].Invalidate(); }
            loading = false;

            lblManageName.Text = w.DisplayName;
            RefreshAddresses();
            UpdateDirty();
            UpdateHostedStatus();
        }

        bool Dirty()
        {
            if (cur == null || orig == null || loading) return false;
            return fName.Text.Trim() != orig.DisplayName
                || nLimit.Value != orig.MaxPlayer
                || nAuto.Value != orig.AutoSave
                || nPort.Value != orig.Port
                || tBackup.Checked != orig.BackupOnStart
                || tFirewall.Checked != origFirewall;
        }

        void UpdateDirty()
        {
            if (loading || btnSave == null) return;
            bool dirty = Dirty();
            btnSave.Enabled = dirty;
            btnUndo.Enabled = dirty;
        }

        void SaveManage()
        {
            if (cur == null || game == null) return;
            var name = fName.Text.Trim();
            if (name.Length == 0) { DarkBox.Show(this, Text, "Give the server a name."); return; }

            try
            {
                var w = Clone(orig);
                w.Name = name;
                w.MaxPlayer = nLimit.Value;
                w.AutoSave = nAuto.Value;
                w.Port = nPort.Value;
                w.BackupOnStart = tBackup.Checked;
                WorldStore.SaveSettings(game, w);

                if (tFirewall.Checked != origFirewall)
                {
                    var cfg = ConfigStore.Load(game);
                    ConfigStore.Set(cfg, "ConfigureFirewall", tFirewall.Checked);
                    ConfigStore.Save(game, cfg);
                    origFirewall = tFirewall.Checked;
                }

                orig = w;
                cur = Clone(w);
                lblManageName.Text = w.DisplayName;
                UpdateDirty();
                Print("Saved \"" + w.DisplayName + "\".");
            }
            catch (UnauthorizedAccessException) { Print("Access denied writing to the game folder."); AskElevate(); }
            catch (Exception ex) { Print("Could not save: " + ex.Message); DarkBox.Show(this, "Could not save", ex.Message); }
        }

        void RefreshAddresses()
        {
            if (addrPanel == null) return;
            foreach (Control c in addrPanel.Controls.Cast<Control>().ToList()) { addrPanel.Controls.Remove(c); c.Dispose(); }

            var ips = LocalIps.Get();
            int port = nPort.Value;
            if (ips.Count == 0)
            {
                Lbl(addrPanel, "No network connection found.", 10f, false, Ui.Warn, 0, Ui.D(14));
                addrPanel.Height = Ui.D(52);
                return;
            }

            int y = 0;
            foreach (var ip in ips)
            {
                var address = ip.Key + ":" + port;
                var row = new RoundPanel { Location = new Point(0, y), Height = Ui.D(46), Width = addrPanel.Width, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = Ui.Card, Radius = Ui.D(8) };
                Lbl(row, address, 10.5f, true, Color.White, Ui.D(16), Ui.D(12));
                Lbl(row, ip.Value, 9f, false, Ui.Muted, Ui.D(190), Ui.D(14));
                var copy = new RoundButton("Copy") { Size = new Size(Ui.D(84), Ui.D(32)), Anchor = AnchorStyles.Top | AnchorStyles.Right };
                copy.Location = new Point(row.Width - copy.Width - Ui.D(8), Ui.D(7));
                var text = address;
                copy.Click += (s, e) => { try { Clipboard.SetText(text); Print("Copied " + text); } catch (Exception ex) { Print("Could not copy: " + ex.Message); } };
                row.Controls.Add(copy);
                addrPanel.Controls.Add(row);
                y += Ui.D(52);
            }
            addrPanel.Height = y;
        }

        async Task BackupNow()
        {
            if (cur == null || game == null) return;
            var g = game; var id = cur.Id;
            try
            {
                var dest = await Task.Run(() => WorldStore.Backup(g, id, "manual"));
                Print("Backup created: " + dest);
                DarkBox.Show(this, Text, "Backup created.\r\n" + Path.GetFileName(dest));
            }
            catch (Exception ex) { Print("Backup failed: " + ex.Message); DarkBox.Show(this, "Backup failed", ex.Message); }
        }

        void RestoreBackup()
        {
            if (cur == null || game == null) return;
            if (Installer.IsGameRunning()) { DarkBox.Show(this, Text, "Close the game first. The world is in use while it runs."); return; }

            var backups = WorldStore.ListBackups(game, cur.Id);
            if (backups.Count == 0) { DarkBox.Show(this, Text, "There are no backups of this world yet."); return; }

            BackupInfo chosen = null;
            using (var f = new Form())
            {
                f.Text = "Restore a backup";
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.StartPosition = FormStartPosition.CenterParent;
                f.MaximizeBox = f.MinimizeBox = false;
                f.ShowInTaskbar = false;
                f.BackColor = Ui.Card;
                f.ForeColor = Ui.Fg;
                f.Font = Ui.Font(9.5f);
                f.ClientSize = new Size(Ui.D(560), Ui.D(380));
                Ui.DarkTitle(f);

                Lbl(f, "Pick a backup. The current world is saved as a \"before-restore\" backup first.", 9.5f, false, Ui.Fg, Ui.D(24), Ui.D(20));
                var list = new ListBox { Location = new Point(Ui.D(24), Ui.D(54)), Size = new Size(Ui.D(512), Ui.D(250)), BackColor = Ui.Field, ForeColor = Ui.Fg, BorderStyle = BorderStyle.None, Font = Ui.Font(10f), ItemHeight = Ui.D(26), DrawMode = DrawMode.OwnerDrawFixed };
                list.DrawItem += (s, e) =>
                {
                    if (e.Index < 0) return;
                    var b = backups[e.Index];
                    bool sel = (e.State & DrawItemState.Selected) != 0;
                    using (var br = new SolidBrush(sel ? Ui.Accent : Ui.Field)) e.Graphics.FillRectangle(br, e.Bounds);
                    TextRenderer.DrawText(e.Graphics, b.Time.ToString("g") + "   " + b.Label.Split('-')[0] + "   " + WorldStore.FormatSize(b.Bytes), e.Font, new Rectangle(e.Bounds.X + Ui.D(8), e.Bounds.Y, e.Bounds.Width, e.Bounds.Height), Color.White, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                };
                foreach (var b in backups) list.Items.Add(b.Label);
                list.SelectedIndex = 0;
                f.Controls.Add(list);

                var restore = RoundButton.Primary("Restore");
                restore.SetBounds(f.ClientSize.Width - Ui.D(24) - Ui.D(130), Ui.D(320), Ui.D(130), Ui.D(42));
                restore.Click += (s, e) => { if (list.SelectedIndex >= 0) chosen = backups[list.SelectedIndex]; f.Close(); };
                var cancel = new RoundButton("Cancel");
                cancel.SetBounds(restore.Left - Ui.D(10) - Ui.D(110), Ui.D(320), Ui.D(110), Ui.D(42));
                cancel.Click += (s, e) => f.Close();
                f.Controls.Add(restore);
                f.Controls.Add(cancel);
                f.ShowDialog(this);
            }

            if (chosen == null) return;
            if (!DarkBox.Show(this, Text, "Replace \"" + cur.DisplayName + "\" with the backup from " + chosen.Time.ToString("g") + "?", "Restore", "Cancel", true)) return;
            try
            {
                WorldStore.Restore(game, cur.Id, chosen);
                Print("Restored \"" + cur.DisplayName + "\" from " + chosen.Label);
                OpenManage(cur.Id);
            }
            catch (Exception ex) { Print("Restore failed: " + ex.Message); DarkBox.Show(this, "Restore failed", ex.Message); }
        }

        void DeleteServer()
        {
            if (cur == null || game == null) return;
            if (Installer.IsGameRunning() && IsHosting(cur.Id)) { DarkBox.Show(this, Text, "This server is running. Close the game first."); return; }
            if (!DarkBox.Show(this, Text, "Delete \"" + cur.DisplayName + "\" permanently?\r\nIts backups are kept in Multiplayer\\Game\\Backups.", "Delete", "Cancel", true)) return;

            try { WorldStore.Delete(game, cur.Id); Print("Deleted server \"" + cur.DisplayName + "\"."); }
            catch (Exception ex) { Print("Could not delete: " + ex.Message); DarkBox.Show(this, "Could not delete", ex.Message); return; }
            cur = null; orig = null;
            ShowPage("Servers");
        }

        // ---------------------------------------------------------------- Join

        void BuildJoin()
        {
            var p = pages["Join"];
            joinList = ScrollPanel();
            p.Controls.Add(joinList);
            joinList.Resize += (s, e) => LayoutCards(joinList);

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = Ui.D(190), BackColor = Ui.Bg, Padding = new Padding(Ui.D(36), Ui.D(12), Ui.D(36), Ui.D(24)) };
            var bar = new RoundPanel { Dock = DockStyle.Fill, BackColor = Ui.Card };
            Lbl(bar, "ADDRESS", 7.5f, true, Color.FromArgb(150, 150, 150), Ui.D(24), Ui.D(18));
            fAddress = new Field { Location = new Point(Ui.D(24), Ui.D(42)), Width = Ui.D(320) };
            fAddress.Placeholder = "192.168.1.20 or 192.168.1.20:7777";
            bar.Controls.Add(fAddress);
            Lbl(bar, "NAME (TO SAVE)", 7.5f, true, Color.FromArgb(150, 150, 150), Ui.D(364), Ui.D(18));
            fSaveName = new Field { Location = new Point(Ui.D(364), Ui.D(42)), Width = Ui.D(260) };
            fSaveName.Placeholder = "Optional";
            bar.Controls.Add(fSaveName);

            btnJoin = RoundButton.Primary("JOIN");
            btnJoin.Font = Ui.Font(10f, true);
            btnJoin.SetBounds(Ui.D(24), Ui.D(104), Ui.D(200), Ui.D(46));
            btnJoin.Click += async (s, e) => await JoinTyped();
            btnSaveServer = new RoundButton("Save to list") { Height = Ui.D(46), Width = Ui.D(150), Location = new Point(Ui.D(236), Ui.D(104)) };
            btnSaveServer.Click += (s, e) => SaveServer();
            bar.Controls.Add(btnJoin);
            bar.Controls.Add(btnSaveServer);
            bottom.Controls.Add(bar);
            p.Controls.Add(bottom);

            p.Controls.Add(Header(p, "Join", "Type the host's address (ip or ip:port). Save it to the list so you can join with one click next time. LAN and ZeroTier addresses both work."));
        }

        int DefaultJoinPort()
        {
            return game == null ? 7777 : GlobalInt("DefaultJoinPort", 7777);
        }

        void RefreshJoinList()
        {
            foreach (Control c in joinList.Controls.Cast<Control>().ToList()) { joinList.Controls.Remove(c); c.Dispose(); }

            var servers = game == null ? new List<SavedServer>() : ServerStore.Load(game);
            if (servers.Count == 0)
            {
                var c = Card(joinList, 90);
                Lbl(c, game == null ? "Game not found. Pick SubnauticaZero.exe on the Play game page first." : "No saved servers yet. Type an address below and press Save to list.", 10f, false, game == null ? Ui.Warn : Ui.Muted, Ui.D(24), Ui.D(34));
            }

            foreach (var sv in servers)
            {
                var server = sv;
                var card = Card(joinList, 86);
                var icon = new ServerIcon { Location = new Point(Ui.D(20), (card.Height - Ui.D(56)) / 2) };
                card.Controls.Add(icon);
                Lbl(card, server.Name, 13f, true, Color.White, Ui.D(94), Ui.D(18));
                Lbl(card, server.Address, 9.5f, false, Ui.Muted, Ui.D(94), Ui.D(48));

                CardButton(card, "Join", 100, Ui.D(20), () => { var t = LaunchGame("-bzmp-join " + server.Address); }, true);
                CardButton(card, "Edit", 90, Ui.D(130), () => { fAddress.Text = server.Address; fSaveName.Text = server.Name; });
                CardButton(card, "Remove", 100, Ui.D(230), () => RemoveServer(server));
            }
            LayoutCards(joinList);
            btnJoin.Enabled = btnSaveServer.Enabled = game != null;
        }

        async Task JoinTyped()
        {
            string host; int port;
            if (!ServerStore.TryParse(fAddress.Text, DefaultJoinPort(), out host, out port))
            {
                DarkBox.Show(this, Text, "Enter the host address like 192.168.1.20 or 192.168.1.20:7777.");
                return;
            }
            await LaunchGame("-bzmp-join " + host + ":" + port);
        }

        void SaveServer()
        {
            if (game == null) return;
            string host; int port;
            if (!ServerStore.TryParse(fAddress.Text, DefaultJoinPort(), out host, out port))
            {
                DarkBox.Show(this, Text, "Enter the host address like 192.168.1.20 or 192.168.1.20:7777.");
                return;
            }
            var name = fSaveName.Text.Trim();
            if (name.Length == 0) name = host;

            var list = ServerStore.Load(game);
            var existing = list.FirstOrDefault(s => s.Ip == host && s.Port == port);
            if (existing != null) existing.Name = name;
            else list.Add(new SavedServer { Id = Guid.NewGuid().ToString(), Name = name, Ip = host, Port = port });

            ServerStore.Save(game, list);
            Print("Saved server " + name + " (" + host + ":" + port + ")");
            RefreshJoinList();
        }

        void RemoveServer(SavedServer sv)
        {
            if (game == null) return;
            ServerStore.Save(game, ServerStore.Load(game).Where(s => s.Id != sv.Id).ToList());
            RefreshJoinList();
        }

        // ---------------------------------------------------------------- Options

        void BuildOptions()
        {
            var p = pages["Options"];
            var scroll = ScrollPanel();
            p.Controls.Add(scroll);

            var table = Form2();
            scroll.Controls.Add(table);

            fPlayer = new Field();
            fPlayer.Box.MaxLength = 24;
            fPlayer.Placeholder = "Empty = your Steam name";
            AddRow(table, Labeled("PLAYER NAME  (EVERY PLAYER ON A SERVER NEEDS A DIFFERENT NAME)", fPlayer));

            fExe = new Field { ReadOnly = true };
            var exeHolder = new Panel { Height = Ui.D(46), BackColor = Ui.Bg };
            var browse = new RoundButton("Browse...") { Size = new Size(Ui.D(110), Ui.D(44)), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            browse.Click += (s, e) => BrowseGame();
            exeHolder.Controls.Add(fExe);
            exeHolder.Controls.Add(browse);
            exeHolder.Resize += (s, e) =>
            {
                browse.Location = new Point(exeHolder.Width - browse.Width, 0);
                fExe.SetBounds(0, 0, exeHolder.Width - browse.Width - Ui.D(10), Ui.D(44));
            };
            exeHolder.Width = Ui.D(600);
            AddRow(table, Labeled("SUBNAUTICAZERO.EXE", exeHolder, 80));

            nJoinPort = new NumField { Min = 1, Max = 65535 };
            nTimeout = new NumField { Min = 60, Max = 300 };
            AddRow(table, Labeled("DEFAULT JOIN PORT", nJoinPort), Labeled("CONNECTION TIMEOUT (S)", nTimeout));

            var buttons = new Panel { Height = Ui.D(64), Dock = DockStyle.Fill, BackColor = Ui.Bg, Margin = new Padding(0, Ui.D(14), 0, 0) };
            var save = RoundButton.Primary("SAVE OPTIONS");
            save.SetBounds(0, Ui.D(6), Ui.D(170), Ui.D(46));
            save.Click += (s, e) => SaveOptions();
            var openGame = new RoundButton("Open game folder") { Location = new Point(Ui.D(182), Ui.D(6)), Size = new Size(Ui.D(170), Ui.D(46)) };
            openGame.Click += (s, e) => OpenFolder(game);
            var openSaves = new RoundButton("Open saves folder") { Location = new Point(Ui.D(364), Ui.D(6)), Size = new Size(Ui.D(170), Ui.D(46)) };
            openSaves.Click += (s, e) => OpenFolder(game == null ? null : Paths.WorldsDir(game));
            buttons.Controls.Add(save);
            buttons.Controls.Add(openGame);
            buttons.Controls.Add(openSaves);
            AddRow(table, buttons);

            AddRow(table, Labeled("", new Panel { Height = 1 }, 10));
            var note = new Label { AutoSize = false, Height = Ui.D(60), Dock = DockStyle.Fill, ForeColor = Ui.Muted, BackColor = Ui.Bg, Text = "Saved to Multiplayer\\Game\\Core\\Config.json in the game folder and applied the next time the game starts. Server settings (port, player limit, auto save) are per server: open Servers, then Manage." };
            AddRow(table, note);

            p.Controls.Add(Header(p, "Options", "Your player name and connection settings. Applied the next time the game starts."));
        }

        void LoadOptions()
        {
            fExe.Text = game == null ? "" : Paths.GameExe(game);
            if (game == null) return;
            try
            {
                var cfg = ConfigStore.Load(game);
                fPlayer.Text = Convert.ToString(ConfigStore.Get(cfg, "PlayerName")) ?? "";
                nJoinPort.Value = Json.ToInt(ConfigStore.Get(cfg, "DefaultJoinPort"), 7777);
                nTimeout.Value = Json.ToInt(ConfigStore.Get(cfg, "ConnectionTimeout"), 120);
            }
            catch (Exception ex) { Print("Could not read Config.json: " + ex.Message); }
        }

        void SaveOptions()
        {
            if (game == null) { DarkBox.Show(this, Text, "Pick the game folder first (Play game page)."); return; }
            try
            {
                string name = CleanPlayerName(fPlayer.Text);
                if (name == null)
                {
                    DarkBox.Show(this, Text, "The player name needs 2-24 characters: letters, digits, space, _ - . (or leave it empty to use your Steam name).");
                    return;
                }
                fPlayer.Text = name;

                var cfg = ConfigStore.Load(game);
                ConfigStore.Set(cfg, "GameExePath", Paths.GameExe(game).Replace('\\', '/'));
                ConfigStore.Set(cfg, "PlayerName", name);
                ConfigStore.Set(cfg, "DefaultJoinPort", nJoinPort.Value);
                ConfigStore.Set(cfg, "ConnectionTimeout", nTimeout.Value);
                ConfigStore.Save(game, cfg);
                Print("Options saved.");
            }
            catch (UnauthorizedAccessException) { Print("Access denied writing Config.json."); AskElevate(); }
            catch (Exception ex) { Print("Could not save options: " + ex.Message); DarkBox.Show(this, "Could not save options", ex.Message); }
        }

        /// <summary>Same rules as the mod: returns "" for empty, null if invalid.</summary>
        static string CleanPlayerName(string text)
        {
            text = (text ?? "").Trim();
            if (text.Length == 0) return "";
            if (text.Length < 2 || text.Length > 24) return null;
            foreach (var c in text)
                if (!(char.IsLetterOrDigit(c) || c == ' ' || c == '_' || c == '-' || c == '.')) return null;
            return text;
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
            var p = pages["Game log"];
            var holder = new Panel { Dock = DockStyle.Fill, BackColor = Ui.Bg, Padding = new Padding(Ui.D(36), 0, Ui.D(36), Ui.D(24)) };
            txtGameLog = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, BackColor = Ui.Card, ForeColor = Ui.Fg, BorderStyle = BorderStyle.None, Font = new Font("Consolas", 9f) };
            Ui.DarkScroll(txtGameLog);
            holder.Controls.Add(txtGameLog);
            p.Controls.Add(holder);
            p.Controls.Add(Header(p, "Game log", "The latest Multiplayer\\Game\\Logs file. Useful when something does not connect."));
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

        // ---------------------------------------------------------------- Info

        void BuildInfo()
        {
            var p = pages["Info"];
            var scroll = ScrollPanel();
            p.Controls.Add(scroll);

            var body = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(Ui.D(820), 0),
                Location = new Point(Ui.D(38), Ui.D(4)),
                ForeColor = Color.FromArgb(215, 215, 215),
                BackColor = Ui.Bg,
                Font = Ui.Font(10f),
                Text =
                    "HOW IT WORKS\r\n" +
                    "The multiplayer server runs inside the host's game. Press Start on a server and the game opens straight into that world. Keep it running while your friends play. Friends press Join, type your address and they are in.\r\n\r\n" +
                    "PLAYING OVER THE INTERNET\r\n" +
                    "On a LAN nothing is needed. Over the internet either forward the server port (UDP) on your router or use a VPN such as ZeroTier and give your friends the VPN address.\r\n\r\n" +
                    "IF SOMETHING BREAKS\r\n" +
                    "Joined players have a Resync (reconnect) button at the top of the in-game ESC menu. If the host's own game is the broken one, close it and press Start again; everyone rejoins. The world autosaves while it runs, and the launcher keeps backups you can restore on the server's Manage page.\r\n\r\n" +
                    "AFTER A STEAM UPDATE\r\n" +
                    "Press UPDATE / REPAIR MOD on the Play game page. Everyone needs the same game version.\r\n\r\n" +
                    "No game files are redistributed. Based on the Subnautica Below Zero Multiplayer mod by BOT Benson and the Detanup01 fork. This is an unofficial port: back up your saves.",
            };
            scroll.Controls.Add(body);

            var open = RoundButton.Primary("Open project page");
            open.Size = new Size(Ui.D(180), Ui.D(44));
            open.Click += (s, e) => { try { Process.Start(ProjectUrl); } catch (Exception ex) { Print("Could not open the browser: " + ex.Message); } };
            var readme = new RoundButton("Open README") { Size = new Size(Ui.D(160), Ui.D(44)) };
            readme.Click += (s, e) =>
            {
                var file = Path.Combine(Paths.Base, "README-LAN.txt");
                try { if (File.Exists(file)) Process.Start(file); else Print("README-LAN.txt is not next to the launcher."); } catch (Exception ex) { Print("Could not open the README: " + ex.Message); }
            };
            scroll.Controls.Add(open);
            scroll.Controls.Add(readme);
            int bodyHeight = body.GetPreferredSize(new Size(Ui.D(820), 0)).Height;
            open.Location = new Point(Ui.D(38), body.Top + bodyHeight + Ui.D(24));
            readme.Location = new Point(open.Right + Ui.D(10), open.Top);

            p.Controls.Add(Header(p, "Info", "LAN / direct IP edition " + ModVersion));
        }
    }
}
