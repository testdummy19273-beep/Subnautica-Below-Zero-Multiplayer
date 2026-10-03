using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BZLauncher
{
    /// <summary>Colors, scaling and small drawing helpers for the dark UI.</summary>
    static class Ui
    {
        public static readonly float Scale = GetScale();

        public static readonly Color Bg = Color.FromArgb(8, 8, 8);
        public static readonly Color Nav = Color.FromArgb(18, 18, 18);
        public static readonly Color Card = Color.FromArgb(24, 24, 24);
        public static readonly Color Field = Color.FromArgb(34, 34, 34);
        public static readonly Color Btn = Color.FromArgb(62, 62, 62);
        public static readonly Color BtnHot = Color.FromArgb(82, 82, 82);
        public static readonly Color Accent = Color.FromArgb(10, 124, 255);
        public static readonly Color AccentHot = Color.FromArgb(48, 146, 255);
        public static readonly Color Danger = Color.FromArgb(235, 40, 50);
        public static readonly Color Good = Color.FromArgb(70, 200, 110);
        public static readonly Color Warn = Color.FromArgb(235, 175, 60);
        public static readonly Color Fg = Color.FromArgb(240, 240, 240);
        public static readonly Color Muted = Color.FromArgb(150, 150, 150);

        public static int D(int px) { return (int)Math.Round(px * Scale); }

        static float GetScale()
        {
            try { using (var g = Graphics.FromHwnd(IntPtr.Zero)) return g.DpiX / 96f; }
            catch { return 1f; }
        }

        public static Font Font(float size, bool bold = false, bool italic = false)
        {
            var style = (bold ? FontStyle.Bold : FontStyle.Regular) | (italic ? FontStyle.Italic : FontStyle.Regular);
            return new Font("Segoe UI", size, style);
        }

        public static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = Math.Max(1, radius * 2);
            if (d > r.Height) d = r.Height;
            if (d > r.Width) d = r.Width;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        static extern int SetWindowTheme(IntPtr hwnd, string subAppName, string subIdList);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, string lParam);

        /// <summary>Dark scroll bars where the OS supports it.</summary>
        public static void DarkScroll(Control c)
        {
            EventHandler apply = (s, e) => { try { SetWindowTheme(c.Handle, "DarkMode_Explorer", null); } catch { } };
            if (c.IsHandleCreated) apply(null, null); else c.HandleCreated += apply;
        }

        public static void SetCue(TextBox box, string text)
        {
            EventHandler apply = (s, e) => { try { SendMessage(box.Handle, 0x1501, (IntPtr)1, text); } catch { } };
            if (box.IsHandleCreated) apply(null, null); else box.HandleCreated += apply;
        }

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        public static void DarkTitle(Form f)
        {
            EventHandler apply = (s, e) => { int v = 1; try { DwmSetWindowAttribute(f.Handle, 20, ref v, 4); } catch { } };
            if (f.IsHandleCreated) apply(null, null); else f.HandleCreated += apply;
        }

        public static Color Mix(Color a, Color b, float t)
        {
            return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        }
    }

    /// <summary>Flat rounded button, optionally with a small second line.</summary>
    class RoundButton : Control
    {
        public Color Fill = Ui.Btn;
        public Color HotFill = Ui.BtnHot;
        public Color TextColor = Color.White;
        public int Radius = Ui.D(7);
        public string SubText;
        public string Glyph;
        public bool LeftAlign;
        bool hot, down;

        public RoundButton(string text)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            Text = text;
            Cursor = Cursors.Hand;
            Font = Ui.Font(9.5f);
            Size = new Size(Ui.D(120), Ui.D(40));
        }

        public static RoundButton Primary(string text) { return new RoundButton(text) { Fill = Ui.Accent, HotFill = Ui.AccentHot }; }

        protected override void OnMouseEnter(EventArgs e) { hot = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hot = down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
        protected override bool IsInputKey(Keys keyData) { return false; }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Ui.Bg);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var fill = !Enabled ? Ui.Mix(Fill, Ui.Bg, 0.55f) : down ? Ui.Mix(Fill, Color.Black, 0.2f) : hot ? HotFill : Fill;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Ui.RoundRect(rect, Radius))
            using (var brush = new SolidBrush(fill))
                g.FillPath(brush, path);

            var color = Enabled ? TextColor : Ui.Mix(TextColor, Ui.Bg, 0.5f);
            var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;
            if (LeftAlign)
            {
                int textX = Ui.D(16);
                if (!string.IsNullOrEmpty(Glyph))
                {
                    using (var icons = new Font("Segoe MDL2 Assets", 12f))
                        TextRenderer.DrawText(g, Glyph, icons, new Rectangle(Ui.D(16), 0, Ui.D(30), Height), color, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    textX = Ui.D(52);
                }
                TextRenderer.DrawText(g, Text, Font, new Rectangle(textX, 0, Width - textX, Height), color, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
            else if (string.IsNullOrEmpty(SubText))
            {
                TextRenderer.DrawText(g, Text, Font, rect, color, flags);
            }
            else
            {
                using (var small = Ui.Font(7f))
                {
                    var top = new Rectangle(0, 0, Width, Height / 2 + Ui.D(3));
                    var bottom = new Rectangle(0, Height / 2 + Ui.D(3), Width, Height / 2 - Ui.D(3));
                    TextRenderer.DrawText(g, Text, Font, top, color, TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom | TextFormatFlags.NoPadding);
                    TextRenderer.DrawText(g, SubText, small, bottom, Ui.Mix(color, fill, 0.25f), TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.NoPadding);
                }
            }
        }
    }

    /// <summary>Selectable outlined pill (game mode choice).</summary>
    class Chip : Control
    {
        public bool Selected { get; set; }
        public bool Locked { get; set; }

        public Chip(string text)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Text = text;
            Cursor = Cursors.Hand;
            Font = Ui.Font(9.5f);
            Size = new Size(Ui.D(96), Ui.D(46));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Ui.Bg);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(1, 1, Width - 3, Height - 3);
            var border = Selected ? Ui.Accent : Color.FromArgb(70, 70, 70);
            if (Locked && !Selected) border = Color.FromArgb(44, 44, 44);
            using (var path = Ui.RoundRect(rect, Ui.D(10)))
            using (var pen = new Pen(border, Selected ? 2.4f : 1.6f))
                g.DrawPath(pen, path);
            var color = Locked && !Selected ? Color.FromArgb(110, 110, 110) : Ui.Fg;
            TextRenderer.DrawText(g, Text, Font, rect, color, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    class Toggle : Control
    {
        bool isChecked;
        public event EventHandler CheckedChanged;

        public bool Checked
        {
            get { return isChecked; }
            set { if (isChecked == value) return; isChecked = value; Invalidate(); if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty); }
        }

        public Toggle()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            Size = new Size(Ui.D(52), Ui.D(28));
        }

        protected override void OnClick(EventArgs e) { Checked = !Checked; base.OnClick(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Ui.Bg);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var track = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Ui.RoundRect(track, Height / 2))
            using (var brush = new SolidBrush(Color.FromArgb(70, 70, 70)))
                g.FillPath(brush, path);

            int pad = Ui.D(4);
            int d = Height - pad * 2 - 1;
            int x = isChecked ? Width - 1 - pad - d : pad;
            using (var brush = new SolidBrush(isChecked ? Ui.Accent : Color.White))
                g.FillEllipse(brush, x, pad, d, d);
        }
    }

    /// <summary>Panel with rounded corners filled with its BackColor.</summary>
    class RoundPanel : Panel
    {
        public int Radius = Ui.D(10);

        public RoundPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint, true);
            BackColor = Ui.Card;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Ui.Bg);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = Ui.RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), Radius))
            using (var brush = new SolidBrush(BackColor))
                g.FillPath(brush, path);
        }
    }

    /// <summary>Dark rounded text box.</summary>
    class Field : RoundPanel
    {
        public readonly TextBox Box;
        public event EventHandler TextValueChanged;

        public Field()
        {
            BackColor = Ui.Field;
            Radius = Ui.D(8);
            Height = Ui.D(44);
            Box = new TextBox { BorderStyle = BorderStyle.None, BackColor = Ui.Field, ForeColor = Ui.Fg, Font = Ui.Font(10f) };
            Controls.Add(Box);
            Box.TextChanged += (s, e) => { if (TextValueChanged != null) TextValueChanged(this, EventArgs.Empty); };
            Click += (s, e) => Box.Focus();
            Box.KeyDown += (s, e) => { if (e.Control && e.KeyCode == Keys.A) { Box.SelectAll(); e.SuppressKeyPress = true; } };
        }

        public string Placeholder { set { Ui.SetCue(Box, value); } }
        public bool ReadOnly { get { return Box.ReadOnly; } set { Box.ReadOnly = value; Box.ForeColor = value ? Ui.Muted : Ui.Fg; } }

        public override string Text { get { return Box.Text; } set { Box.Text = value; } }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (Box == null) return;
            int pad = Ui.D(14);
            Box.Left = pad;
            Box.Width = Math.Max(10, Width - pad * 2);
            Box.Top = Math.Max(0, (Height - Box.Height) / 2);
        }
    }

    /// <summary>Number field limited to digits and a range.</summary>
    class NumField : Field
    {
        public int Min = 1, Max = 65535;

        public NumField()
        {
            Box.MaxLength = 6;
            Box.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            Box.Leave += (s, e) => Value = Value;
        }

        public int Value
        {
            get { int v; if (!int.TryParse(Box.Text, out v)) v = Min; return Math.Max(Min, Math.Min(Max, v)); }
            set { Box.Text = Math.Max(Min, Math.Min(Max, value)).ToString(); }
        }
    }

    class NavItem : Control
    {
        public string Glyph = "";
        public bool Selected { get; set; }
        bool hot;

        public NavItem(string text, string glyph)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Text = text;
            Glyph = glyph;
            Cursor = Cursors.Hand;
            Height = Ui.D(46);
            Font = Ui.Font(10.5f);
        }

        protected override void OnMouseEnter(EventArgs e) { hot = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hot = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Ui.Nav);
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            var color = Selected ? Color.White : hot ? Color.FromArgb(215, 215, 215) : Color.FromArgb(160, 160, 160);
            using (var icons = new Font("Segoe MDL2 Assets", 12.5f))
                TextRenderer.DrawText(g, Glyph, icons, new Rectangle(Ui.D(26), 0, Ui.D(30), Height), color, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            using (var f = Ui.Font(10.5f, Selected))
                TextRenderer.DrawText(g, Text, f, new Rectangle(Ui.D(66), 0, Width - Ui.D(70), Height), color, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    /// <summary>The server icon: a dark tile with a stylised drop.</summary>
    class ServerIcon : Control
    {
        public ServerIcon()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(Ui.D(56), Ui.D(56));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Ui.Bg);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Ui.RoundRect(rect, Ui.D(12)))
            using (var brush = new LinearGradientBrush(rect, Color.FromArgb(70, 76, 84), Color.FromArgb(28, 32, 38), 90f))
                g.FillPath(brush, path);

            float cx = Width / 2f, cy = Height / 2f, r = Width * 0.24f;
            using (var pen = new Pen(Color.FromArgb(225, 235, 245), Math.Max(2f, Width * 0.055f)))
            {
                g.DrawEllipse(pen, cx - r, cy - r, r * 2, r * 2);
                g.DrawArc(pen, cx - r * 0.55f, cy - r * 0.55f, r * 1.1f, r * 1.1f, 200, 140);
            }
            using (var brush = new SolidBrush(Color.FromArgb(10, 124, 255)))
                g.FillEllipse(brush, cx - r * 0.2f, cy - r * 0.2f, r * 0.4f, r * 0.4f);
        }
    }

    /// <summary>Procedural underwater banner (no game art is shipped).</summary>
    class HeroPanel : Control
    {
        Bitmap cache;

        public HeroPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            if (cache != null) { cache.Dispose(); cache = null; }
            base.OnSizeChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width < 10 || Height < 10) return;
            if (cache == null) cache = Render(Width, Height);
            e.Graphics.DrawImageUnscaled(cache, 0, 0);
        }

        static Bitmap Render(int w, int h)
        {
            var bmp = new Bitmap(w, h);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                var rnd = new Random(7);
                var rect = new Rectangle(0, 0, w, h);

                using (var sea = new LinearGradientBrush(rect, Color.FromArgb(3, 20, 40), Color.FromArgb(6, 70, 96), 90f))
                    g.FillRectangle(sea, rect);

                // light shafts from the surface
                for (int i = 0; i < 6; i++)
                {
                    float x = w * (0.1f + 0.16f * i) + rnd.Next(-30, 30);
                    float spread = w * (0.05f + 0.04f * (float)rnd.NextDouble());
                    var poly = new[] { new PointF(x, 0), new PointF(x + spread, 0), new PointF(x + spread * 4 - w * 0.12f, h), new PointF(x - spread * 2 - w * 0.12f, h) };
                    using (var shaft = new LinearGradientBrush(new RectangleF(0, 0, w, h), Color.FromArgb(34, 150, 220, 255), Color.FromArgb(0, 150, 220, 255), 90f))
                        g.FillPolygon(shaft, poly);
                }

                // glowing plants
                var glows = new[] { Color.FromArgb(220, 70, 230), Color.FromArgb(60, 210, 240), Color.FromArgb(120, 255, 150), Color.FromArgb(255, 120, 80) };
                for (int i = 0; i < 14; i++)
                {
                    float cx = (float)rnd.NextDouble() * w, cy = h * (0.35f + 0.55f * (float)rnd.NextDouble());
                    float r = Ui.D(18) + (float)rnd.NextDouble() * Ui.D(46);
                    var c = glows[rnd.Next(glows.Length)];
                    using (var path = new GraphicsPath())
                    {
                        path.AddEllipse(cx - r, cy - r, r * 2, r * 2);
                        using (var br = new PathGradientBrush(path) { CenterColor = Color.FromArgb(150, c), SurroundColors = new[] { Color.FromArgb(0, c) } })
                            g.FillPath(br, path);
                    }
                }

                // seabed silhouette
                var pts = new List<PointF> { new PointF(0, h) };
                for (int x = 0; x <= w; x += Math.Max(8, w / 60))
                    pts.Add(new PointF(x, h - Ui.D(26) - (float)(Math.Sin(x / 90.0) * Ui.D(14) + Math.Sin(x / 37.0) * Ui.D(6)) - (float)rnd.NextDouble() * Ui.D(5)));
                pts.Add(new PointF(w, h));
                using (var br = new SolidBrush(Color.FromArgb(5, 14, 20)))
                    g.FillPolygon(br, pts.ToArray());

                // bubbles
                for (int i = 0; i < 40; i++)
                {
                    float r = 1.5f + (float)rnd.NextDouble() * 5f;
                    using (var pen = new Pen(Color.FromArgb(60 + rnd.Next(80), 190, 235, 255), 1f))
                        g.DrawEllipse(pen, (float)rnd.NextDouble() * w, (float)rnd.NextDouble() * h * 0.9f, r * 2, r * 2);
                }

                // darken toward the bottom so the text below stays readable
                using (var fade = new LinearGradientBrush(new Rectangle(0, h - Ui.D(90), w, Ui.D(90) + 1), Color.FromArgb(0, Ui.Bg), Ui.Bg, 90f))
                    g.FillRectangle(fade, 0, h - Ui.D(90), w, Ui.D(90) + 1);

                // title
                using (var big = new Font("Segoe UI Black", 44f, FontStyle.Italic))
                using (var small = new Font("Segoe UI Semibold", 15f, FontStyle.Regular))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    var mid = new RectangleF(0, h * 0.28f, w, h * 0.3f);
                    using (var shadow = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
                        g.DrawString("BELOW ZERO", big, shadow, new RectangleF(mid.X + 3, mid.Y + 3, mid.Width, mid.Height), sf);
                    using (var fill = new LinearGradientBrush(mid, Color.FromArgb(255, 214, 120), Color.FromArgb(240, 120, 30), 90f))
                        g.DrawString("BELOW ZERO", big, fill, mid, sf);
                    using (var white = new SolidBrush(Color.FromArgb(235, 245, 255)))
                        g.DrawString("M U L T I P L A Y E R", small, white, new RectangleF(0, mid.Bottom - Ui.D(4), w, Ui.D(40)), sf);
                }
            }
            return bmp;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && cache != null) { cache.Dispose(); cache = null; }
            base.Dispose(disposing);
        }
    }

    /// <summary>Dark replacement for MessageBox.</summary>
    static class DarkBox
    {
        /// <summary>One button when noText is null, otherwise yes/no. Returns true for the first button.</summary>
        public static bool Show(IWin32Window owner, string title, string message, string yesText = "OK", string noText = null, bool danger = false)
        {
            using (var f = new Form())
            {
                f.Text = title;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.StartPosition = FormStartPosition.CenterParent;
                f.MaximizeBox = f.MinimizeBox = false;
                f.ShowInTaskbar = false;
                f.BackColor = Ui.Card;
                f.ForeColor = Ui.Fg;
                f.Font = Ui.Font(9.5f);
                f.ClientSize = new Size(Ui.D(460), Ui.D(170));
                Ui.DarkTitle(f);

                var label = new Label { Text = message, Location = new Point(Ui.D(24), Ui.D(22)), Size = new Size(Ui.D(412), Ui.D(90)), ForeColor = Ui.Fg, BackColor = Ui.Card };
                f.Controls.Add(label);

                bool result = false;
                var yes = new RoundButton(yesText) { Size = new Size(Ui.D(120), Ui.D(38)), Location = new Point(f.ClientSize.Width - Ui.D(24) - Ui.D(120), Ui.D(116)) };
                yes.Fill = danger ? Ui.Danger : Ui.Accent;
                yes.HotFill = danger ? Color.FromArgb(255, 70, 80) : Ui.AccentHot;
                yes.Click += (s, e) => { result = true; f.Close(); };
                f.Controls.Add(yes);

                if (noText != null)
                {
                    var no = new RoundButton(noText) { Size = new Size(Ui.D(110), Ui.D(38)), Location = new Point(yes.Left - Ui.D(10) - Ui.D(110), Ui.D(116)) };
                    no.Click += (s, e) => f.Close();
                    f.Controls.Add(no);
                }
                f.AcceptButton = null;
                f.ShowDialog(owner);
                return result;
            }
        }
    }
}
