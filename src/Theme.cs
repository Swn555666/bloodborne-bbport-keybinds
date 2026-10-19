// SPDX-License-Identifier: GPL-2.0-or-later
// Copyright (C) 2026 Swn555666
// Authors: Swn555666, Claude (Anthropic)

using System.Drawing.Drawing2D;
using Microsoft.Win32;

namespace BBKeybinds;

public static class Theme
{
    public static bool IsDark { get; private set; }

    public static Color Bg { get; private set; }
    public static Color Card { get; private set; }
    public static Color CardHover { get; private set; }
    public static Color Text { get; private set; }
    public static Color Muted { get; private set; }
    public static Color Border { get; private set; }
    public static Color Accent { get; private set; }
    public static Color AccentText { get; private set; }
    public static Color AccentSoft { get; private set; }
    public static Color Danger { get; private set; }
    public static Color DangerSoft { get; private set; }
    public static Color Selection { get; private set; }
    public static Color WarningBg { get; private set; }
    public static Color WarningText { get; private set; }

    public static string FontFamilyName { get; private set; } = "Segoe UI";
    public static Font BaseFont { get; private set; } = SystemFonts.MessageBoxFont!;
    public static Font SmallFont { get; private set; } = SystemFonts.MessageBoxFont!;
    public static Font BoldFont { get; private set; } = SystemFonts.MessageBoxFont!;
    public static Font TitleFont { get; private set; } = SystemFonts.MessageBoxFont!;

    public static void Initialize()
    {
        IsDark = ReadDarkMode();

        if (IsDark)
        {
            Bg = Hex("#202020");
            Card = Hex("#2B2B2B");
            CardHover = Hex("#333333");
            Text = Hex("#FFFFFF");
            Muted = Hex("#A0A0A0");
            Border = Hex("#3A3A3A");
            Accent = Hex("#4CC2FF");
            AccentText = Hex("#003A5C");
            AccentSoft = Hex("#24343D");
            Danger = Hex("#FF99A4");
            DangerSoft = Hex("#3A2426");
            Selection = Hex("#2D3B45");
            WarningBg = Hex("#332B18");
            WarningText = Hex("#F2D08A");
        }
        else
        {
            Bg = Hex("#F3F3F3");
            Card = Hex("#FFFFFF");
            CardHover = Hex("#F7F7F7");
            Text = Hex("#1B1B1B");
            Muted = Hex("#6B6B6B");
            Border = Hex("#E1E1E1");
            Accent = Hex("#0067C0");
            AccentText = Hex("#FFFFFF");
            AccentSoft = Hex("#E5F1FB");
            Danger = Hex("#C42B1C");
            DangerSoft = Hex("#FDE7E9");
            Selection = Hex("#E5F1FB");
            WarningBg = Hex("#FFF4CE");
            WarningText = Hex("#6B5400");
        }

        FontFamilyName = PickFamily("Microsoft YaHei UI", "Microsoft YaHei", "Segoe UI", "Tahoma");
        BaseFont = new Font(FontFamilyName, 9.5f, FontStyle.Regular, GraphicsUnit.Point);
        SmallFont = new Font(FontFamilyName, 8.5f, FontStyle.Regular, GraphicsUnit.Point);
        BoldFont = new Font(FontFamilyName, 9.5f, FontStyle.Bold, GraphicsUnit.Point);
        TitleFont = new Font(FontFamilyName, 16f, FontStyle.Bold, GraphicsUnit.Point);
    }

    private static bool ReadDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var v = key?.GetValue("AppsUseLightTheme");
            if (v is int i)
                return i == 0;
        }
        catch { }
        return false;
    }

    public static Color Hex(string hex)
    {
        hex = hex.TrimStart('#');
        return Color.FromArgb(
            int.Parse(hex[..2], System.Globalization.NumberStyles.HexNumber),
            int.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber),
            int.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber));
    }

    private static string PickFamily(params string[] names)
    {
        try
        {
            var installed = FontFamily.Families
                .Select(f => f.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var name in names)
                if (installed.Contains(name))
                    return name;
        }
        catch { }
        return SystemFonts.MessageBoxFont?.FontFamily.Name ?? "Segoe UI";
    }

    public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        int d = Math.Max(1, radius * 2);
        var path = new GraphicsPath();
        if (bounds.Width <= d || bounds.Height <= d)
        {
            path.AddRectangle(bounds);
            return path;
        }
        path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void Apply(Control root)
    {
        ApplyOne(root);
        foreach (Control c in root.Controls)
            Apply(c);
    }

    private static void ApplyOne(Control c)
    {
        switch (c)
        {
            case Form f:
                f.BackColor = Bg;
                f.ForeColor = Text;
                break;
            case ListBox lb:
                lb.BackColor = Bg;
                lb.ForeColor = Text;
                break;
            case TextBox tb:
                tb.BackColor = Card;
                tb.ForeColor = Text;
                break;
            case ComboBox cb:
                cb.BackColor = Card;
                cb.ForeColor = Text;
                cb.FlatStyle = FlatStyle.Flat;
                if (IsDark && cb.IsHandleCreated)
                    Native.SetWindowTheme(cb.Handle, "", "");
                break;
            case NumericUpDown nu:
                nu.BackColor = Card;
                nu.ForeColor = Text;
                foreach (Control child in nu.Controls)
                {
                    child.BackColor = Card;
                    child.ForeColor = Text;
                }
                break;
            case CheckBox chk:
                if (IsDark)
                {
                    chk.FlatStyle = FlatStyle.Flat;
                    chk.BackColor = Card;
                }
                else
                {
                    chk.BackColor = Color.Transparent;
                }
                if (chk.ForeColor.IsEmpty || chk.ForeColor == SystemColors.ControlText)
                    chk.ForeColor = Text;
                break;
            case Label lbl:
                if (lbl.BackColor.IsEmpty || lbl.BackColor == SystemColors.Control)
                    lbl.BackColor = Color.Transparent;
                if (lbl.ForeColor.IsEmpty || lbl.ForeColor == SystemColors.ControlText)
                    lbl.ForeColor = Text;
                break;
            case Button btn:
                btn.FlatStyle = FlatStyle.Flat;
                if (btn.Tag as string == "primary")
                {
                    btn.BackColor = Accent;
                    btn.ForeColor = AccentText;
                    btn.FlatAppearance.BorderColor = Accent;
                    btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(Accent, 0.1f);
                }
                else
                {
                    btn.FlatAppearance.BorderColor = Border;
                    btn.FlatAppearance.MouseOverBackColor = CardHover;
                    if (btn.BackColor == Color.Empty || btn.BackColor == SystemColors.Control)
                        btn.BackColor = Card;
                    if (btn.ForeColor.IsEmpty || btn.ForeColor == SystemColors.ControlText)
                        btn.ForeColor = Text;
                }
                break;
            case TrackBar tb:
                tb.BackColor = Card;
                break;
            default:
                if (c.BackColor == SystemColors.Control)
                    c.BackColor = Bg;
                if (c.ForeColor == SystemColors.ControlText)
                    c.ForeColor = Text;
                break;
        }
        if (IsDark && c.IsHandleCreated &&
            c is not CardPanel and not BindingChip and not ComboBox and not NumericUpDown)
            Native.UseDarkControlTheme(c.Handle);
        c.Invalidate();
    }

    public static Button MakeButton(string text, bool primary = false)
    {
        var b = new Button
        {
            Text = text,
            AutoSize = false,
            Height = 34,
            MinimumSize = new Size(84, 34),
            Padding = new Padding(12, 0, 12, 0),
            FlatStyle = FlatStyle.Flat,
            Font = BaseFont,
            UseVisualStyleBackColor = false
        };
        b.FlatAppearance.BorderSize = 1;
        if (primary)
        {
            b.Tag = "primary";
            b.BackColor = Accent;
            b.ForeColor = AccentText;
            b.FlatAppearance.BorderColor = Accent;
            b.FlatAppearance.MouseOverBackColor = ControlPaint.Light(Accent, 0.1f);
        }
        else
        {
            b.BackColor = Card;
            b.ForeColor = Text;
            b.FlatAppearance.BorderColor = Border;
            b.FlatAppearance.MouseOverBackColor = CardHover;
        }
        b.AutoSize = true;
        return b;
    }

    public static ComboBox MakeCombo()
    {
        var cb = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = Card,
            ForeColor = Text,
            DrawMode = DrawMode.OwnerDrawFixed,
            Font = BaseFont
        };
        cb.DrawItem += (s, e) =>
        {
            var combo = (ComboBox)s!;
            var bg = (e.State & DrawItemState.Selected) != 0 ? Selection : Card;
            using (var brush = new SolidBrush(bg))
                e.Graphics.FillRectangle(brush, e.Bounds);
            if (e.Index >= 0)
                TextRenderer.DrawText(e.Graphics, combo.Items[e.Index]?.ToString() ?? "", combo.Font,
                    e.Bounds, Text,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        };
        return cb;
    }
}

/// <summary>A flat card panel with a 1px border and small corner radius.</summary>
public class CardPanel : Panel
{
    public int Radius { get; set; } = 8;
    public Color FillColor { get; set; } = Theme.Card;
    public Color BorderColor { get; set; } = Theme.Border;

    public CardPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Card;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateRegion();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        UpdateRegion();
    }

    private void UpdateRegion()
    {
        if (Width <= 0 || Height <= 0)
            return;
        var old = Region;
        using var path = Theme.RoundedRect(new Rectangle(0, 0, Width, Height), Radius);
        Region = new Region(path);
        old?.Dispose();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Theme.RoundedRect(rect, Radius);
        using var fill = new SolidBrush(FillColor);
        using var pen = new Pen(BorderColor);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(pen, path);
    }
}

/// <summary>A card with a vertical, auto-sizing body; use Add() for each row.</summary>
public sealed class SettingsCard : CardPanel
{
    public TableLayoutPanel Body { get; }

    public SettingsCard()
    {
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(18, 14, 18, 14);
        Margin = new Padding(0, 0, 0, 10);

        Body = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 0,
            Dock = DockStyle.Top,
            Margin = new Padding(0),
            BackColor = Theme.Card
        };
        Body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(Body);
    }

    public T Add<T>(T control) where T : Control
    {
        int row = Body.RowCount;
        Body.RowCount = row + 1;
        Body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        if (control is Button or BindingChip)
        {
            control.Dock = DockStyle.None;
            control.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        }
        else
        {
            control.Dock = DockStyle.Top;
        }
        control.Margin = new Padding(
            control.Margin.Left, control.Margin.Top, control.Margin.Right,
            Math.Max(control.Margin.Bottom, 8));
        Body.Controls.Add(control, 0, row);
        return control;
    }
}
