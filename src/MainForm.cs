// SPDX-License-Identifier: GPL-2.0-or-later
// Copyright (C) 2026 Swn555666
// Authors: Swn555666, Claude (Anthropic)

using System.Diagnostics;
using System.Text;

namespace BBKeybinds;

public sealed class MainForm : Form
{
    private const int HotkeyId = 0xB8B;
    private static readonly uint ShowMessage = Native.RegisterWindowMessage("BBKeybinds.Show");

    private readonly AppSettings _settings;
    private readonly ToolTip _toolTip = new() { AutoPopDelay = 8000, InitialDelay = 350, ReshowDelay = 100 };
    private readonly System.Windows.Forms.Timer _gameTimer = new() { Interval = 2000 };
    private readonly NotifyIcon _tray;

    private readonly Dictionary<string, BindingSet> _bindings = new(StringComparer.Ordinal);
    private MouseSettings _mouse = new();
    private InputDocument? _doc;
    private string? _path;
    private string _toggleKey = "f7";
    private string _reloadKey = "f8";
    private bool _dirty;
    private bool _exiting;
    private bool _loadingUi;
    private DateTime _stampTime;
    private long _stampLength;

    private HotkeySpec _hotkey;
    private bool _hotkeyRegistered;
    private IntPtr _gameHwnd;

    private BindingChip? _capturingChip;
    private BindingChip? _hotkeyChip;
    private readonly List<string> _captureModsHeld = new();
    private readonly List<string> _captureModsSeen = new();

    private readonly List<BindingChip> _actionChips = new();

    // main UI
    private readonly TableLayoutPanel _root = new();
    private readonly ListBox _nav = new();
    private readonly Panel _contentPanel = new();
    private readonly TableLayoutPanel _contentTable = new();
    private readonly Label _gameBadge = new();
    private readonly Label _warningLabel = new();
    private readonly Label _statusLabel = new();
    private readonly Label _titleLabel = new();
    private readonly TextBox _pathBox = new();
    private readonly Button _applyButton;

    private static readonly string[] CategoryKeys = { "move", "combat", "items", "mouse", "advanced" };
    private static readonly string[] CategoryNames = { "移动与镜头", "战斗", "物品与菜单", "鼠标视角", "高级" };

    // mouse page controls
    private CheckBox _mouseEnabled = new();
    private ComboBox _mouseStick = new();
    private ComboBox _mouseCamera = new();
    private CheckBox _invertX = new();
    private CheckBox _invertY = new();
    private NumericUpDown _sensNum = new();
    private NumericUpDown _camNum = new();
    private TrackBar _sensTrack = new();
    private TrackBar _camTrack = new();
    private NumericUpDown _dzNum = new();
    private NumericUpDown _speedNum = new();
    private NumericUpDown _speedOffsetNum = new();
    private CheckBox _showAdvanced = new();
    private Panel _advancedPanel = new();

    private Label _gameDetailLabel = new();

    public MainForm()
    {
        _settings = AppSettings.Load();
        _hotkey = _settings.Hotkey;
        if (_hotkey.Vk == 0)
            _hotkey = HotkeySpec.Default;

        Text = "按键设置";
        Font = Theme.BaseFont;
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(880, 640);
        Size = new Size(Math.Max(880, _settings.WindowWidth), Math.Max(640, _settings.WindowHeight));
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        DoubleBuffered = true;

        try
        {
            var icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (icon != null) Icon = icon;
        }
        catch { }

        _applyButton = Theme.MakeButton("应用", primary: true);

        BuildUi();

        _tray = new NotifyIcon
        {
            Icon = Icon ?? SystemIcons.Application,
            Text = "BB 按键设置",
            Visible = true,
            ContextMenuStrip = BuildTrayMenu()
        };
        _tray.DoubleClick += (_, _) => ShowOverlay();

        _gameTimer.Tick += (_, _) => RefreshGameStatus();
        _gameTimer.Start();

        Theme.Apply(this);
        Native.SetDarkTitleBar(Handle, Theme.IsDark);

        LoadInitialInput();
        TryRegisterHotkey(_hotkey, report: true);
        RefreshGameStatus();
    }

    // ------------------------------------------------------------------ UI

    private void BuildUi()
    {
        _root.Dock = DockStyle.Fill;
        _root.ColumnCount = 1;
        _root.RowCount = 5;
        _root.Padding = new Padding(0);
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(_root);

        _root.Controls.Add(BuildHeader(), 0, 0);
        _root.Controls.Add(BuildConfigBar(), 0, 1);
        _root.Controls.Add(BuildWarningBar(), 0, 2);

        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 196));
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _nav.Dock = DockStyle.Fill;
        _nav.BorderStyle = BorderStyle.None;
        _nav.DrawMode = DrawMode.OwnerDrawFixed;
        _nav.ItemHeight = 46;
        _nav.IntegralHeight = false;
        _nav.BackColor = Theme.Bg;
        _nav.ForeColor = Theme.Text;
        _nav.Font = Theme.BaseFont;
        foreach (var name in CategoryNames)
            _nav.Items.Add(name);
        int idx = Array.IndexOf(CategoryKeys, _settings.LastCategory);
        _nav.SelectedIndex = idx >= 0 ? idx : 0;
        _nav.DrawItem += NavOnDrawItem;
        _nav.SelectedIndexChanged += (_, _) =>
        {
            if (_nav.SelectedIndex >= 0 && _nav.SelectedIndex < CategoryKeys.Length)
            {
                _settings.LastCategory = CategoryKeys[_nav.SelectedIndex];
                RefreshCategory();
            }
        };
        main.Controls.Add(_nav, 0, 0);

        _contentPanel.Dock = DockStyle.Fill;
        _contentPanel.AutoScroll = true;
        _contentPanel.Padding = new Padding(14, 8, 14, 14);
        _contentPanel.BackColor = Theme.Bg;

        _contentTable.Dock = DockStyle.Top;
        _contentTable.AutoSize = true;
        _contentTable.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _contentTable.ColumnCount = 1;
        _contentTable.RowCount = 0;
        _contentTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _contentTable.BackColor = Theme.Bg;
        _contentPanel.Controls.Add(_contentTable);
        main.Controls.Add(_contentPanel, 1, 0);

        _root.Controls.Add(main, 0, 3);
        _root.Controls.Add(BuildFooter(), 0, 4);
    }

    private Control BuildHeader()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 78,
            Padding = new Padding(20, 14, 20, 8),
            BackColor = Theme.Bg
        };

        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Bg
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _titleLabel.Text = "按键设置";
        _titleLabel.Font = Theme.TitleFont;
        _titleLabel.AutoSize = true;
        _titleLabel.ForeColor = Theme.Text;
        stack.Controls.Add(_titleLabel, 0, 0);

        var sub = new Label
        {
            Text = "Bloodborne · 血源诅咒键鼠无缝联机版 · 只修改 input.ini",
            Font = Theme.SmallFont,
            ForeColor = Theme.Muted,
            AutoSize = true,
            Margin = new Padding(1, 2, 0, 0)
        };
        stack.Controls.Add(sub, 0, 1);
        header.Controls.Add(stack);

        _gameBadge.Text = "○ 游戏未运行";
        _gameBadge.Font = Theme.BoldFont;
        _gameBadge.ForeColor = Theme.Muted;
        _gameBadge.AutoSize = true;
        _gameBadge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _gameBadge.Location = new Point(header.Width - 150, 20);
        header.Controls.Add(_gameBadge);
        header.Resize += (_, _) =>
        {
            _gameBadge.Left = Math.Max(20, header.ClientSize.Width - _gameBadge.Width - 20);
            _gameBadge.Top = 22;
        };

        return header;
    }

    private Control BuildConfigBar()
    {
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 4,
            RowCount = 1,
            Padding = new Padding(20, 0, 20, 8),
            BackColor = Theme.Bg
        };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var label = new Label
        {
            Text = "配置文件",
            AutoSize = true,
            ForeColor = Theme.Muted,
            Font = Theme.SmallFont,
            Margin = new Padding(0, 8, 10, 0)
        };
        bar.Controls.Add(label, 0, 0);

        _pathBox.Dock = DockStyle.Fill;
        _pathBox.ReadOnly = true;
        _pathBox.TabStop = false;
        _pathBox.BorderStyle = BorderStyle.FixedSingle;
        _pathBox.Font = Theme.SmallFont;
        _pathBox.BackColor = Theme.Card;
        _pathBox.ForeColor = Theme.Text;
        _pathBox.Margin = new Padding(0, 4, 8, 0);
        bar.Controls.Add(_pathBox, 1, 0);

        var browse = Theme.MakeButton("浏览…");
        browse.Margin = new Padding(0, 0, 6, 0);
        browse.Click += (_, _) => BrowseInputIni();
        bar.Controls.Add(browse, 2, 0);

        var reload = Theme.MakeButton("重新读取");
        reload.Click += (_, _) =>
        {
            if (_dirty && MessageBox.Show(this, "重新读取会放弃未保存的更改，继续吗？", "重新读取",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            LoadFromDisk();
        };
        bar.Controls.Add(reload, 3, 0);
        return bar;
    }

    private Control BuildWarningBar()
    {
        _warningLabel.Dock = DockStyle.Top;
        _warningLabel.AutoSize = true;
        _warningLabel.Padding = new Padding(20, 8, 20, 8);
        _warningLabel.BackColor = Theme.WarningBg;
        _warningLabel.ForeColor = Theme.WarningText;
        _warningLabel.Font = Theme.SmallFont;
        _warningLabel.Text = "游戏不会暂停，请在篝火或安全位置调整。按 Esc 关闭面板；点「应用」后会自动回到游戏并重载按键。";
        return _warningLabel;
    }

    private Control BuildFooter()
    {
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(20, 8, 20, 12),
            BackColor = Theme.Bg
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _statusLabel.AutoSize = true;
        _statusLabel.ForeColor = Theme.Muted;
        _statusLabel.Font = Theme.SmallFont;
        _statusLabel.Text = "就绪";
        _statusLabel.Anchor = AnchorStyles.Left;
        _statusLabel.Margin = new Padding(0, 10, 10, 0);
        footer.Controls.Add(_statusLabel, 0, 0);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Anchor = AnchorStyles.Right,
            BackColor = Theme.Bg,
            Margin = new Padding(0)
        };

        var close = Theme.MakeButton("关闭");
        close.Click += (_, _) => HideToTray();
        buttons.Controls.Add(close);

        _applyButton.Click += async (_, _) => await ApplyAndReturnToGameAsync();
        buttons.Controls.Add(_applyButton);

        var ds3 = Theme.MakeButton("Dark Souls III 布局");
        ds3.Click += (_, _) => LoadLayout(Layouts.Ds3, "Dark Souls III");
        buttons.Controls.Add(ds3);

        var reset = Theme.MakeButton("恢复默认");
        reset.Click += (_, _) => LoadLayout(Layouts.EffectiveDefault, "默认");
        buttons.Controls.Add(reset);

        var saveDefault = Theme.MakeButton("设为默认");
        saveDefault.Click += (_, _) => SaveAsDefault();
        buttons.Controls.Add(saveDefault);

        footer.Controls.Add(buttons, 1, 0);
        return footer;
    }

    private ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip { Font = Theme.BaseFont };
        menu.Items.Add("打开按键设置 (" + _hotkey.Display + ")", null, (_, _) => ShowOverlay());
        menu.Items.Add("打开 input.ini", null, (_, _) => OpenInputFile());
        menu.Items.Add("打开所在文件夹", null, (_, _) => OpenInputFolder());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("重新检测游戏", null, (_, _) => DetectAndSwitch());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitApp());
        return menu;
    }

    private void NavOnDrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        bool selected = (e.State & DrawItemState.Selected) != 0;
        using (var bg = new SolidBrush(selected ? Theme.Selection : Theme.Bg))
            e.Graphics.FillRectangle(bg, e.Bounds);
        if (selected)
        {
            using var accent = new SolidBrush(Theme.Accent);
            e.Graphics.FillRectangle(accent, e.Bounds.X + 4, e.Bounds.Y + 10, 3, e.Bounds.Height - 20);
        }
        var text = _nav.Items[e.Index]?.ToString() ?? "";
        TextRenderer.DrawText(e.Graphics, text, selected ? Theme.BoldFont : Theme.BaseFont,
            new Rectangle(e.Bounds.X + 18, e.Bounds.Y, e.Bounds.Width - 24, e.Bounds.Height),
            selected ? Theme.Accent : Theme.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix);
    }

    // ------------------------------------------------------------------ content pages

    private void RefreshCategory()
    {
        if (_contentTable.IsDisposed) return;
        _contentTable.SuspendLayout();
        while (_contentTable.Controls.Count > 0)
        {
            var c = _contentTable.Controls[0];
            _contentTable.Controls.RemoveAt(0);
            c.Dispose();
        }
        _contentTable.RowStyles.Clear();
        _contentTable.RowCount = 0;
        _actionChips.Clear();
        _hotkeyChip = null;

        if (_path == null)
        {
            AddToContent(BuildNotFoundCard());
            _contentTable.ResumeLayout();
            return;
        }

        if (_nav.SelectedIndex < 0 || _nav.SelectedIndex >= CategoryKeys.Length)
        {
            _contentTable.ResumeLayout();
            return;
        }

        var key = CategoryKeys[_nav.SelectedIndex];
        if (key == "mouse")
        {
            BuildMousePage();
        }
        else if (key == "advanced")
        {
            BuildAdvancedPage();
        }
        else
        {
            BuildBindingsPage(key);
        }

        UpdateConflicts();
        _contentTable.ResumeLayout();
        try { _contentPanel.CreateControl(); } catch { }
        Theme.Apply(this);
        _contentPanel.AutoScrollPosition = new Point(0, 0);
    }

    private void AddToContent(Control control)
    {
        int row = _contentTable.RowCount;
        _contentTable.RowCount = row + 1;
        _contentTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        control.Dock = DockStyle.Top;
        _contentTable.Controls.Add(control, 0, row);
    }

    private void AddPageHeader(string title, string description)
    {
        var panel = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(2, 4, 2, 10),
            BackColor = Theme.Bg
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var t = new Label { Text = title, Font = new Font(Theme.FontFamilyName, 12.5f, FontStyle.Bold), AutoSize = true, ForeColor = Theme.Text };
        var d = new Label { Text = description, Font = Theme.SmallFont, AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(1, 3, 0, 0) };
        panel.Controls.Add(t, 0, 0);
        panel.Controls.Add(d, 0, 1);
        AddToContent(panel);
    }

    private void BuildBindingsPage(string groupKey)
    {
        var group = Spec.Groups.First(g => g.Key == groupKey);
        AddPageHeader(group.Title, groupKey switch
        {
            "move" => "移动、镜头和角色动作。每个动作最多 3 个绑定，每个绑定最多 3 个按键（例如 Shift + 鼠标左键）。",
            "combat" => "攻击和武器动作。",
            _ => "互动、道具和菜单。"
        });

        if (groupKey == "move" && !_settings.FirstRunTipShown)
            AddToContent(BuildFirstRunCard());

        foreach (var output in group.Outputs)
            AddToContent(BuildActionRow(output));
    }

    private Control BuildFirstRunCard()
    {
        var card = new CardPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(16, 12, 16, 12),
            Margin = new Padding(0, 0, 0, 10),
            FillColor = Theme.AccentSoft,
            BorderColor = Theme.Accent
        };
        var table = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.AccentSoft
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var title = new Label
        {
            Text = "首次使用",
            Font = Theme.BoldFont,
            ForeColor = Theme.Accent,
            AutoSize = true
        };
        var text = new Label
        {
            Text = "1. 正常启动游戏（PLAY BBPORT + BBSC 或 Play Bloodborne.exe）。\r\n" +
                   "2. 游戏中按 " + _hotkey.Display + " 打开本面板。\r\n" +
                   "3. 改键后点「应用」，工具会自动回到游戏并发送重载键，立即生效。",
            Font = Theme.SmallFont,
            ForeColor = Theme.Text,
            AutoSize = true,
            MaximumSize = new Size(760, 0),
            Margin = new Padding(0, 6, 0, 8)
        };
        var dismiss = Theme.MakeButton("知道了");
        dismiss.Click += (_, _) =>
        {
            _settings.FirstRunTipShown = true;
            _settings.Save();
            RefreshCategory();
        };
        table.Controls.Add(title, 0, 0);
        table.Controls.Add(text, 0, 1);
        table.Controls.Add(dismiss, 0, 2);
        card.Controls.Add(table);
        return card;
    }

    private Control BuildActionRow(string output)
    {
        if (!_bindings.TryGetValue(output, out var set))
        {
            set = new BindingSet();
            _bindings[output] = set;
        }

        var card = new CardPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(14, 10, 14, 10),
            Margin = new Padding(0, 0, 0, 6)
        };

        var table = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 1,
            Dock = DockStyle.Top,
            BackColor = Theme.Card
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var labels = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            BackColor = Theme.Card
        };
        labels.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        labels.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        labels.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var title = new Label
        {
            Text = Spec.OutputTitles.TryGetValue(output, out var t) ? t : output,
            Font = Theme.BoldFont,
            ForeColor = Theme.Text,
            AutoSize = true
        };
        var defaultText = string.Join(" / ", Layouts.EffectiveDefault.Bindings.TryGetValue(output, out var def)
            ? def.All.Select(Spec.DisplayBinding)
            : Enumerable.Empty<string>());
        var hint = new Label
        {
            Text = "默认：" + (string.IsNullOrEmpty(defaultText) ? "无" : defaultText),
            Font = Theme.SmallFont,
            ForeColor = Theme.Muted,
            AutoSize = true,
            Margin = new Padding(0, 3, 0, 0)
        };
        labels.Controls.Add(title, 0, 0);
        labels.Controls.Add(hint, 0, 1);
        table.Controls.Add(labels, 0, 0);

        var flow = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Top,
            Margin = new Padding(0),
            BackColor = Theme.Card
        };

        for (int i = 0; i < Spec.Slots; i++)
        {
            var chip = new BindingChip
            {
                OutputName = output,
                SlotIndex = i,
                Binding = set.Slots[i],
                Margin = new Padding(0, 0, 5, 6)
            };
            chip.CaptureRequested += (s, _) => BeginCapture((BindingChip)s!);
            chip.MouseCaptured += (s, parts) => CompleteCapture(parts);
            chip.KeyMessage += (_, msg, vk, lp) =>
                HandleCaptureVk(vk, lp, msg is Native.WmKeyDown or Native.WmSysKeyDown);
            chip.ClearRequested += (s, _) => ClearChip((BindingChip)s!);
            chip.ContextMenuStrip = BuildChipMenu(chip, output);
            _actionChips.Add(chip);
            flow.Controls.Add(chip);
        }

        table.Controls.Add(flow, 1, 0);
        card.Controls.Add(table);
        return card;
    }

    private ContextMenuStrip BuildChipMenu(BindingChip chip, string output)
    {
        var menu = new ContextMenuStrip { Font = Theme.SmallFont };
        menu.Items.Add("清除此绑定", null, (_, _) => ClearChip(chip));
        menu.Items.Add("手动输入按键…", null, (_, _) => ManualEntry(chip, output));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("恢复此动作默认", null, (_, _) => RestoreActionDefault(chip, output));
        menu.Items.Add("恢复全部默认", null, (_, _) => LoadLayout(Layouts.EffectiveDefault, "默认"));
        return menu;
    }

    private void BuildMousePage()
    {
        AddPageHeader("鼠标视角", "开启后可以用鼠标转动镜头。修改后点「应用」写入 input.ini；游戏内按 F8 也会重载。");

        var card = new CardPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(18, 14, 18, 14),
            Margin = new Padding(0, 0, 0, 10)
        };
        var table = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 0,
            Dock = DockStyle.Top,
            BackColor = Theme.Card
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int row = 0;
        void AddRow(string label, Control control)
        {
            if (!string.IsNullOrEmpty(label))
            {
                var l = new Label
                {
                    Text = label,
                    AutoSize = true,
                    ForeColor = Theme.Text,
                    Font = Theme.BaseFont,
                    Margin = new Padding(0, 8, 14, 0),
                    Anchor = AnchorStyles.Left
                };
                table.Controls.Add(l, 0, row);
            }
            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(0, 5, 0, 5);
            table.Controls.Add(control, 1, row);
            if (string.IsNullOrEmpty(label))
                table.SetColumnSpan(control, 2);
            table.RowCount = ++row;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        _mouseEnabled = new CheckBox
        {
            Text = "启用鼠标视角（用鼠标转动镜头）",
            AutoSize = true,
            Font = Theme.BoldFont,
            ForeColor = Theme.Text
        };
        _mouseEnabled.CheckedChanged += (_, _) =>
        {
            if (_loadingUi) return;
            _mouse.Enabled = _mouseEnabled.Checked;
            UpdateMouseEnabledState();
            MarkDirty();
        };
        AddRow("", _mouseEnabled);

        _mouseStick = Theme.MakeCombo();
        _mouseStick.Width = 260;
        _mouseStick.Items.AddRange(new object[] { "右摇杆（镜头，推荐）", "左摇杆（移动）" });
        _mouseStick.SelectedIndexChanged += (_, _) =>
        {
            if (_loadingUi) return;
            _mouse.Stick = _mouseStick.SelectedIndex == 1 ? "left" : "right";
            MarkDirty();
        };
        AddRow("鼠标控制", _mouseStick);

        _mouseCamera = Theme.MakeCombo();
        _mouseCamera.Width = 260;
        _mouseCamera.Items.AddRange(new object[] { "直连 1:1（推荐）", "摇杆模拟" });
        _mouseCamera.SelectedIndexChanged += (_, _) =>
        {
            if (_loadingUi) return;
            _mouse.Camera = _mouseCamera.SelectedIndex == 1 ? "stick" : "direct";
            MarkDirty();
        };
        AddRow("视角模式", _mouseCamera);

        _invertX = new CheckBox { Text = "反转水平（X）", AutoSize = true, ForeColor = Theme.Text };
        _invertY = new CheckBox { Text = "反转垂直（Y）", AutoSize = true, ForeColor = Theme.Text };
        _invertX.CheckedChanged += (_, _) => { if (!_loadingUi) { _mouse.InvertX = _invertX.Checked; MarkDirty(); } };
        _invertY.CheckedChanged += (_, _) => { if (!_loadingUi) { _mouse.InvertY = _invertY.Checked; MarkDirty(); } };
        var invertFlow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        invertFlow.Controls.Add(_invertX);
        invertFlow.Controls.Add(_invertY);
        AddRow("反转方向", invertFlow);

        _sensNum = new NumericUpDown
        {
            Minimum = 0.10m, Maximum = 10.00m, DecimalPlaces = 2, Increment = 0.05m,
            Width = 90, Font = Theme.BaseFont
        };
        _sensTrack = new TrackBar { Minimum = 10, Maximum = 1000, TickStyle = TickStyle.None, Width = 240, Height = 32 };
        _sensNum.ValueChanged += (_, _) =>
        {
            if (_loadingUi) return;
            _sensTrack.Value = Math.Clamp((int)Math.Round(_sensNum.Value * 100), _sensTrack.Minimum, _sensTrack.Maximum);
            _mouse.SensitivityY = (double)_sensNum.Value;
            MarkDirty();
        };
        _sensTrack.ValueChanged += (_, _) =>
        {
            if (_loadingUi) return;
            _sensNum.Value = Math.Clamp(_sensTrack.Value / 100m, _sensNum.Minimum, _sensNum.Maximum);
        };
        AddRow("鼠标灵敏度", Combine(_sensNum, _sensTrack));

        _camNum = new NumericUpDown
        {
            Minimum = 0.05m, Maximum = 20.00m, DecimalPlaces = 2, Increment = 0.05m,
            Width = 90, Font = Theme.BaseFont
        };
        _camTrack = new TrackBar { Minimum = 5, Maximum = 2000, TickStyle = TickStyle.None, Width = 240, Height = 32 };
        _camNum.ValueChanged += (_, _) =>
        {
            if (_loadingUi) return;
            _camTrack.Value = Math.Clamp((int)Math.Round(_camNum.Value * 100), _camTrack.Minimum, _camTrack.Maximum);
            _mouse.CameraSensitivity = (double)_camNum.Value;
            MarkDirty();
        };
        _camTrack.ValueChanged += (_, _) =>
        {
            if (_loadingUi) return;
            _camNum.Value = Math.Clamp(_camTrack.Value / 100m, _camNum.Minimum, _camNum.Maximum);
        };
        AddRow("镜头灵敏度", Combine(_camNum, _camTrack));

        var hint = new Label
        {
            Text = "提示：开启鼠标视角后，游戏内按 F7 可以释放/重新捕获鼠标。",
            Font = Theme.SmallFont,
            ForeColor = Theme.Muted,
            AutoSize = true,
            MaximumSize = new Size(720, 0),
            Margin = new Padding(0, 8, 0, 0)
        };
        AddRow("", hint);

        _showAdvanced = new CheckBox
        {
            Text = "显示高级参数（一般不需要修改）",
            AutoSize = true,
            ForeColor = Theme.Text,
            Margin = new Padding(0, 12, 0, 4)
        };
        AddRow("", _showAdvanced);

        _advancedPanel = new Panel { AutoSize = true, Visible = false, Dock = DockStyle.Fill, BackColor = Theme.Card };
        var advTable = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 0,
            Dock = DockStyle.Top,
            BackColor = Theme.Card
        };
        advTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        advTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        int ar = 0;
        void AddAdv(string label, NumericUpDown num)
        {
            var l = new Label { Text = label, AutoSize = true, ForeColor = Theme.Muted, Font = Theme.SmallFont, Margin = new Padding(0, 8, 14, 0) };
            num.Width = 110;
            num.DecimalPlaces = 2;
            num.Font = Theme.BaseFont;
            num.Margin = new Padding(0, 4, 0, 4);
            advTable.Controls.Add(l, 0, ar);
            advTable.Controls.Add(num, 1, ar);
            advTable.RowCount = ++ar;
            advTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        _dzNum = new NumericUpDown { Minimum = 0m, Maximum = 1m, Increment = 0.01m };
        _speedNum = new NumericUpDown { Minimum = 0.01m, Maximum = 20m, Increment = 0.01m };
        _speedOffsetNum = new NumericUpDown { Minimum = 0m, Maximum = 1m, Increment = 0.01m };
        _dzNum.ValueChanged += (_, _) => { if (!_loadingUi) { _mouse.DeadzoneOffset = (double)_dzNum.Value; MarkDirty(); } };
        _speedNum.ValueChanged += (_, _) => { if (!_loadingUi) { _mouse.Speed = (double)_speedNum.Value; MarkDirty(); } };
        _speedOffsetNum.ValueChanged += (_, _) => { if (!_loadingUi) { _mouse.SpeedOffset = (double)_speedOffsetNum.Value; MarkDirty(); } };
        AddAdv("死区偏移 (0–1)", _dzNum);
        AddAdv("速度 (0.01–20)", _speedNum);
        AddAdv("速度偏移 (0–1)", _speedOffsetNum);

        var restoreMouse = Theme.MakeButton("恢复鼠标默认");
        restoreMouse.Margin = new Padding(0, 10, 0, 4);
        restoreMouse.Click += (_, _) =>
        {
            _mouse = Layouts.EffectiveDefault.Mouse.Clone();
            PopulateMouseUi();
            MarkDirty();
        };
        advTable.Controls.Add(restoreMouse, 1, ar);
        advTable.RowCount = ++ar;

        _advancedPanel.Controls.Add(advTable);
        AddRow("", _advancedPanel);

        _showAdvanced.CheckedChanged += (_, _) =>
        {
            _advancedPanel.Visible = _showAdvanced.Checked;
            _advancedPanel.Parent?.PerformLayout();
        };

        card.Controls.Add(table);
        AddToContent(card);
        PopulateMouseUi();
    }

    private static Control Combine(params Control[] controls)
    {
        var flow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        foreach (var c in controls)
        {
            c.Margin = new Padding(0, 2, 12, 2);
            flow.Controls.Add(c);
        }
        return flow;
    }

    private void PopulateMouseUi()
    {
        _loadingUi = true;
        try
        {
            _mouseEnabled.Checked = _mouse.Enabled;
            _mouseStick.SelectedIndex = _mouse.Stick == "left" ? 1 : 0;
            _mouseCamera.SelectedIndex = _mouse.Camera == "stick" ? 1 : 0;
            _invertX.Checked = _mouse.InvertX;
            _invertY.Checked = _mouse.InvertY;
            _sensNum.Value = Math.Clamp((decimal)_mouse.SensitivityY, _sensNum.Minimum, _sensNum.Maximum);
            _sensTrack.Value = Math.Clamp((int)Math.Round(_mouse.SensitivityY * 100), _sensTrack.Minimum, _sensTrack.Maximum);
            _camNum.Value = Math.Clamp((decimal)_mouse.CameraSensitivity, _camNum.Minimum, _camNum.Maximum);
            _camTrack.Value = Math.Clamp((int)Math.Round(_mouse.CameraSensitivity * 100), _camTrack.Minimum, _camTrack.Maximum);
            _dzNum.Value = Math.Clamp((decimal)_mouse.DeadzoneOffset, _dzNum.Minimum, _dzNum.Maximum);
            _speedNum.Value = Math.Clamp((decimal)_mouse.Speed, _speedNum.Minimum, _speedNum.Maximum);
            _speedOffsetNum.Value = Math.Clamp((decimal)_mouse.SpeedOffset, _speedOffsetNum.Minimum, _speedOffsetNum.Maximum);
        }
        finally
        {
            _loadingUi = false;
        }
        UpdateMouseEnabledState();
    }

    private void UpdateMouseEnabledState()
    {
        bool on = _mouseEnabled.Checked;
        foreach (var c in new Control[] { _mouseStick, _mouseCamera, _invertX, _invertY, _sensNum, _sensTrack, _camNum, _camTrack, _showAdvanced })
            c.Enabled = on;
        _advancedPanel.Enabled = on;
        if (!on)
            _advancedPanel.Visible = false;
        else
            _advancedPanel.Visible = _showAdvanced.Checked;
    }

    private void BuildAdvancedPage()
    {
        AddPageHeader("高级", "面板快捷键、文件位置和兼容性说明。");

        var hotkeyCard = NewCard();
        AddCardTitle(hotkeyCard, "面板快捷键");
        hotkeyCard.Add(new Label
        {
            Text = "在游戏里按下这个键打开本面板。默认 F10；不能使用游戏保留的 Insert、F9 和重载键。",
            Font = Theme.SmallFont,
            ForeColor = Theme.Muted,
            AutoSize = true,
            MaximumSize = new Size(720, 0)
        });

        _hotkeyChip = new BindingChip
        {
            OutputName = "hotkey",
            AllowClear = false,
            Width = 190,
            Binding = HotkeyToBinding(_hotkey)
        };
        _hotkeyChip.CaptureRequested += (s, _) => BeginCapture((BindingChip)s!);
        _hotkeyChip.MouseCaptured += (_, _) => ShowStatus("呼出快捷键只能用键盘设置。", Theme.Danger);
        _hotkeyChip.KeyMessage += (_, msg, vk, lp) =>
            HandleCaptureVk(vk, lp, msg is Native.WmKeyDown or Native.WmSysKeyDown);
        _hotkeyChip.ContextMenuStrip = BuildHotkeyMenu();
        hotkeyCard.Add(_hotkeyChip);
        AddToContent(hotkeyCard);

        var fileCard = NewCard();
        AddCardTitle(fileCard, "文件");
        fileCard.Add(new Label
        {
            Text = _path ?? "（尚未选择 input.ini）",
            Font = Theme.SmallFont,
            ForeColor = Theme.Muted,
            AutoSize = true,
            MaximumSize = new Size(760, 0)
        });

        var fileFlow = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Margin = new Padding(0) };
        var openIni = Theme.MakeButton("打开 input.ini");
        openIni.Click += (_, _) => OpenInputFile();
        var openFolder = Theme.MakeButton("打开所在文件夹");
        openFolder.Click += (_, _) => OpenInputFolder();
        var openBackups = Theme.MakeButton("查看备份");
        openBackups.Click += (_, _) => OpenInputFolder();
        fileFlow.Controls.Add(openIni);
        fileFlow.Controls.Add(openFolder);
        fileFlow.Controls.Add(openBackups);

        var clearDefault = Theme.MakeButton("清除自定义默认");
        clearDefault.Click += (_, _) =>
        {
            if (!Layouts.HasUserDefault)
            {
                ShowStatus("当前没有自定义默认布局，「恢复默认」使用出厂布局。", Theme.Muted);
                return;
            }
            if (MessageBox.Show(this,
                    "清除自定义默认布局？以后「恢复默认」会使用出厂布局。",
                    "清除默认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            Layouts.ClearUserDefault();
            RefreshCategory();
            ShowStatus("已清除自定义默认布局。", Theme.Muted);
        };
        fileFlow.Controls.Add(clearDefault);
        fileCard.Add(fileFlow);
        AddToContent(fileCard);

        var gameCard = NewCard();
        AddCardTitle(gameCard, "游戏");
        _gameDetailLabel = new Label
        {
            Text = GameDetailText(),
            Font = Theme.SmallFont,
            ForeColor = Theme.Muted,
            AutoSize = true,
            MaximumSize = new Size(760, 0)
        };
        gameCard.Add(_gameDetailLabel);
        var detect = Theme.MakeButton("重新检测游戏窗口");
        detect.Click += (_, _) => { DetectAndSwitch(); _gameDetailLabel.Text = GameDetailText(); };
        gameCard.Add(detect);
        AddToContent(gameCard);

        var infoCard = NewCard();
        AddCardTitle(infoCard, "兼容性说明");
        var info = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.None,
            Font = Theme.SmallFont,
            BackColor = Theme.Card,
            ForeColor = Theme.Muted,
            Width = 760,
            Height = 150,
            Text = "本工具只读写 bbport-windows\\input.ini，不修改游戏、模拟器、联机配置或存档。\r\n" +
                   "手柄绑定、注释行和其他设置会原样保留。\r\n" +
                   "每次写入前会备份为 input.ini.bak-时间戳，保留最近 10 份。\r\n" +
                   "应用后会自动切回游戏并发送重载键（默认 F8），不需要重启游戏。\r\n" +
                   "写入格式与 bbport 启动器的 Controls 页完全一致，两边可以混用。\r\n" +
                   "游戏保留键：Insert（游戏内菜单）、F9；热键：F7（鼠标捕获）、F8（重载）。"
        };
        infoCard.Add(info);
        AddToContent(infoCard);

        var aboutCard = NewCard();
        aboutCard.Add(new Label
        {
            Text = "BB 按键设置 1.0.0 · 适用于 bbport 1.7.2 / Bloodborne CUSA03173 1.09",
            Font = Theme.SmallFont,
            ForeColor = Theme.Muted,
            AutoSize = true
        });
        AddToContent(aboutCard);
    }

    private SettingsCard NewCard() => new();

    private static void AddCardTitle(SettingsCard card, string text)
    {
        card.Add(new Label
        {
            Text = text,
            Font = Theme.BoldFont,
            ForeColor = Theme.Text,
            AutoSize = true
        });
    }

    private ContextMenuStrip BuildHotkeyMenu()
    {
        var menu = new ContextMenuStrip { Font = Theme.SmallFont };
        menu.Items.Add("恢复默认 (F10)", null, (_, _) => ChangeHotkey(HotkeySpec.Default));
        return menu;
    }

    private Control BuildNotFoundCard()
    {
        var card = NewCard();
        card.Add(new Label
        {
            Text = "未找到 input.ini",
            Font = new Font(Theme.FontFamilyName, 12.5f, FontStyle.Bold),
            ForeColor = Theme.Text,
            AutoSize = true
        });
        card.Add(new Label
        {
            Text = "请先运行一次游戏，或点下面的按钮手动选择 bbport-windows 文件夹里的 input.ini。\r\n" +
                   "自动查找位置：本程序上级目录\\bbport-windows\\input.ini、正在运行的 bb-probe.exe 所在目录。",
            Font = Theme.SmallFont,
            ForeColor = Theme.Muted,
            AutoSize = true,
            MaximumSize = new Size(720, 0)
        });
        var browse = Theme.MakeButton("选择 input.ini…", primary: true);
        browse.Click += (_, _) => BrowseInputIni();
        card.Add(browse);
        return card;
    }

    // ------------------------------------------------------------------ input file

    private void LoadInitialInput()
    {
        _path = FindInputIni(_settings.InputIniPath);
        if (_path != null)
        {
            _settings.InputIniPath = _path;
            _settings.Save();
            LoadFromDisk();
        }
        else
        {
            _pathBox.Text = "（未找到 input.ini，请点「浏览…」）";
            _applyButton.Enabled = false;
            RefreshCategory();
            ShowStatus("未找到 input.ini，请先运行一次游戏或手动选择文件。", Theme.Danger);
        }
    }

    private static string? FindRunningGameInput()
    {
        try
        {
            foreach (var p in Process.GetProcessesByName("bb-probe"))
            {
                try
                {
                    var exe = p.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(exe))
                    {
                        var candidate = Path.Combine(Path.GetDirectoryName(exe)!, "input.ini");
                        if (File.Exists(candidate))
                            return candidate;
                    }
                }
                catch { }
                finally { p.Dispose(); }
            }
        }
        catch { }
        return null;
    }

    private static string? FindInputIni(string? saved)
    {
        // If the game is running, its own input.ini wins: that is the file the
        // player is actually loading, even when several bbport copies exist.
        var running = FindRunningGameInput();
        if (running != null)
            return running;

        if (!string.IsNullOrWhiteSpace(saved) && File.Exists(saved))
            return saved;

        var env = Environment.GetEnvironmentVariable("BB_INPUT_CONFIG");
        if (!string.IsNullOrWhiteSpace(env) && File.Exists(env))
            return env;

        var baseDir = AppContext.BaseDirectory;
        var candidates = new List<string>
        {
            Path.Combine(baseDir, "input.ini"),
            Path.Combine(baseDir, "..", "input.ini"),
            Path.Combine(baseDir, "..", "bbport-windows", "input.ini"),
            Path.Combine(baseDir, "..", "..", "bbport-windows", "input.ini")
        };
        foreach (var c in candidates)
        {
            try
            {
                var full = Path.GetFullPath(c);
                if (File.Exists(full))
                    return full;
            }
            catch { }
        }

        // Sibling search: <parent>\*\bbport-windows\input.ini and <parent>\*\input.ini
        try
        {
            var parent = Directory.GetParent(baseDir.TrimEnd(Path.DirectorySeparatorChar))?.Parent;
            if (parent != null && parent.Exists)
            {
                var found = new List<string>();
                foreach (var dir in parent.GetDirectories())
                {
                    var direct = Path.Combine(dir.FullName, "input.ini");
                    if (File.Exists(direct) && File.Exists(Path.Combine(dir.FullName, "Bloodborne.exe")))
                        found.Add(direct);
                    var nested = Path.Combine(dir.FullName, "bbport-windows", "input.ini");
                    if (File.Exists(nested))
                        found.Add(nested);
                }
                if (found.Count > 0)
                    return found.OrderByDescending(File.GetLastWriteTimeUtc).First();
            }
        }
        catch { }

        return null;
    }

    private void LoadFromDisk()
    {
        if (string.IsNullOrEmpty(_path))
            return;
        try
        {
            var doc = InputDocument.Load(_path);
            _doc = doc;
            _bindings.Clear();
            foreach (var (k, v) in doc.Bindings)
                _bindings[k] = v.Clone();
            _mouse = doc.Mouse.Clone();
            _toggleKey = doc.ToggleKey;
            _reloadKey = doc.ReloadKey;
            _pathBox.Text = _path;
            _dirty = false;
            UpdateDirtyUi();
            var fi = new FileInfo(_path);
            _stampTime = fi.LastWriteTimeUtc;
            _stampLength = fi.Length;
            _applyButton.Enabled = true;
            RefreshCategory();
            ShowStatus("已读取 " + Path.GetFileName(_path) + " · 重载键 " + Spec.DisplayName(_reloadKey), Theme.Muted);
        }
        catch (Exception ex)
        {
            AppLog.Write("读取 input.ini 失败: " + ex);
            MessageBox.Show(this, "读取 input.ini 失败：\n" + ex.Message, "按键设置",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _doc = null;
            _applyButton.Enabled = false;
        }
    }

    private void BrowseInputIni()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "选择 bbport 的 input.ini",
            Filter = "input.ini|input.ini|INI 文件 (*.ini)|*.ini|所有文件 (*.*)|*.*",
            CheckFileExists = true,
            InitialDirectory = _path != null ? Path.GetDirectoryName(_path) : AppContext.BaseDirectory
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;
        _path = dlg.FileName;
        _settings.InputIniPath = _path;
        _settings.Save();
        LoadFromDisk();
    }

    private bool StampChanged()
    {
        if (_path == null || !File.Exists(_path))
            return false;
        var fi = new FileInfo(_path);
        return fi.LastWriteTimeUtc != _stampTime || fi.Length != _stampLength;
    }

    private bool ValidateBindings(out string error)
    {
        foreach (var (output, set) in _bindings)
        {
            foreach (var value in set.All)
            {
                if (!Spec.IsBindable(output, value))
                {
                    error = "「" + OutputTitle(output) + "」的绑定「" + Spec.DisplayBinding(value) + "」无效。";
                    return false;
                }
            }
        }
        error = "";
        return true;
    }

    private static string OutputTitle(string output) =>
        Spec.OutputTitles.TryGetValue(output, out var t) ? t : output;

    /// <summary>Writes the editor state to input.ini. Returns false if the user cancelled.</summary>
    private bool SaveToDisk(bool makeBackup)
    {
        if (_path == null || _doc == null)
        {
            MessageBox.Show(this, "请先选择 input.ini。", "按键设置", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        if (!ValidateBindings(out var error))
        {
            MessageBox.Show(this, error + "\n\n请修正后再应用。", "无法应用", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (StampChanged())
        {
            var r = MessageBox.Show(this,
                "input.ini 已被其他程序修改（例如启动器的 Controls 页）。\n\n" +
                "是：重新读取文件（放弃当前修改）\n" +
                "否：用当前设置覆盖文件\n" +
                "取消：不保存",
                "文件已变化", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (r == DialogResult.Cancel)
                return false;
            if (r == DialogResult.Yes)
            {
                LoadFromDisk();
                return false;
            }
        }

        try
        {
            var dir = Path.GetDirectoryName(_path)!;
            if (makeBackup && File.Exists(_path))
            {
                var baseName = Path.GetFileName(_path) + ".bak-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                var backup = Path.Combine(dir, baseName);
                int n = 1;
                while (File.Exists(backup))
                    backup = Path.Combine(dir, baseName + "-" + n++);
                File.Copy(_path, backup, true);
                PruneBackups(dir, Path.GetFileName(_path), 10);
            }

            var lines = InputDocument.BuildOutputLines(_doc.Lines, _bindings, _mouse);
            var text = string.Join("\n", lines) + "\n";
            File.WriteAllText(_path, text, new UTF8Encoding(false));

            _doc.Lines = lines;
            _doc.Bindings = _bindings.ToDictionary(kv => kv.Key, kv => kv.Value.Clone(), StringComparer.Ordinal);
            _doc.Mouse = _mouse.Clone();
            _doc.ToggleKey = _toggleKey;
            _doc.ReloadKey = _reloadKey;

            var fi = new FileInfo(_path);
            _stampTime = fi.LastWriteTimeUtc;
            _stampLength = fi.Length;
            _dirty = false;
            UpdateDirtyUi();
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Write("写入 input.ini 失败: " + ex);
            MessageBox.Show(this, "写入 input.ini 失败：\n" + ex.Message, "按键设置",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private static void PruneBackups(string dir, string fileName, int keep)
    {
        try
        {
            var files = Directory.GetFiles(dir, fileName + ".bak-*")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ToList();
            foreach (var old in files.Skip(keep))
                File.Delete(old);
        }
        catch { }
    }

    private async Task ApplyAndReturnToGameAsync()
    {
        if (_path == null)
        {
            BrowseInputIni();
            if (_path == null) return;
        }

        if (!SaveToDisk(makeBackup: true))
            return;

        var game = Native.FindGameWindow();
        if (game == null || Native.NameToVk(_reloadKey) == 0)
        {
            ShowStatus("已应用 · 游戏未运行，下次启动生效", Theme.Muted);
            return;
        }

        _gameHwnd = game.Handle;
        ShowStatus("已应用 · 正在切回游戏并重载…", Theme.Accent);
        HideToTray(showBalloon: false);
        await Task.Delay(120);
        bool focused = Native.ForceForeground(game.Handle);
        await Task.Delay(220);
        Native.SendKey(_reloadKey);
        if (focused)
            ShowStatus("已应用 · 已发送 " + Spec.DisplayName(_reloadKey) + " 重载", Theme.Muted);
        else
            ShowStatus("已应用 · 未能切回游戏，请回游戏按 " + Spec.DisplayName(_reloadKey), Theme.Danger);

        _tray.ShowBalloonTip(2500, "按键设置",
            "已写入 input.ini，游戏内按键已更新。按 " + _hotkey.Display + " 再次打开面板。",
            ToolTipIcon.Info);
    }

    // ------------------------------------------------------------------ binding capture

    private void BeginCapture(BindingChip chip)
    {
        if (_capturingChip != null && _capturingChip != chip)
            EndCapture();

        _capturingChip = chip;
        _captureModsHeld.Clear();
        _captureModsSeen.Clear();
        chip.SetCapturing(true);
        ActiveControl = null;
        Focus();
        ShowStatus("请按下按键或鼠标键…（Esc 取消，最多 3 个键）", Theme.Accent);
    }

    private void EndCapture()
    {
        _capturingChip?.SetCapturing(false);
        _capturingChip = null;
        _captureModsHeld.Clear();
        _captureModsSeen.Clear();
    }

    private void CancelCapture(string reason)
    {
        EndCapture();
        ShowStatus(reason, Theme.Muted);
    }

    private void CompleteCapture(List<string> parts)
    {
        if (_capturingChip == null)
            return;
        var chip = _capturingChip;

        // modifiers first, then the main key, preserving order inside each group.
        var ordered = parts
            .Select((p, i) => (p, i))
            .OrderBy(x => Spec.ModifierNamesSet.Contains(x.p) || x.p is "lwin" or "rwin" ? 0 : 1)
            .ThenBy(x => x.i)
            .Select(x => x.p)
            .ToList();

        if (ordered.Count == 0 || ordered.Count > Spec.MaxCombo)
        {
            ShowStatus("最多 3 个按键，请重新按下。", Theme.Danger);
            return;
        }
        if (ordered.Distinct(StringComparer.Ordinal).Count() != ordered.Count)
        {
            ShowStatus("同一个按键不能重复。", Theme.Danger);
            return;
        }

        if (chip == _hotkeyChip)
        {
            CompleteHotkeyCapture(ordered);
            return;
        }

        if (!Spec.IsBindable(chip.OutputName, string.Join(", ", ordered)))
        {
            var msg = Spec.StickOutputsSet.Contains(chip.OutputName)
                ? "摇杆方向只能绑定键盘按键，不能绑定鼠标键。"
                : "这个按键组合不可用，请换一个。";
            ShowStatus(msg + "（Esc 取消）", Theme.Danger);
            return;
        }

        var reserved = ReservedForBindings();
        foreach (var p in ordered)
        {
            if (reserved.Contains(p))
            {
                ShowStatus(Spec.DisplayName(p) + " 被游戏或面板占用，不能绑定。", Theme.Danger);
                return;
            }
        }
        if (SameAsHotkey(ordered))
        {
            ShowStatus("这个组合是面板呼出快捷键，不能绑定。", Theme.Danger);
            return;
        }

        var value = string.Join(", ", ordered);
        var set = _bindings[chip.OutputName];
        set.Slots[chip.SlotIndex] = value;
        chip.SetBinding(value);
        EndCapture();
        MarkDirty();
        UpdateConflicts();
        ShowStatus("已设置：" + Spec.DisplayBinding(value) + "（未保存）", Theme.Muted);
    }

    private HashSet<string> ReservedForBindings()
    {
        var set = new HashSet<string>(Spec.AlwaysReserved, StringComparer.Ordinal) { _toggleKey, _reloadKey };
        if (_hotkey.Modifiers == 0)
            set.Add(_hotkey.KeyName);
        return set;
    }

    private bool SameAsHotkey(List<string> parts)
    {
        var hotkeyParts = BindingToParts(HotkeyToBinding(_hotkey));
        return new HashSet<string>(parts, StringComparer.Ordinal)
            .SetEquals(new HashSet<string>(hotkeyParts, StringComparer.Ordinal));
    }

    private void CompleteHotkeyCapture(List<string> ordered)
    {
        var last = ordered[^1];
        if (Spec.ModifierNamesSet.Contains(last) || last is "lwin" or "rwin")
        {
            ShowStatus("呼出快捷键必须包含一个普通按键（例如 F10 或 Ctrl+Alt+K）。", Theme.Danger);
            return;
        }
        if (ordered.Any(p => Spec.MouseButtonNamesSet.Contains(p)))
        {
            ShowStatus("呼出快捷键只能用键盘。", Theme.Danger);
            return;
        }
        if (Spec.AlwaysReserved.Contains(last) || last == _toggleKey || last == _reloadKey)
        {
            ShowStatus(Spec.DisplayName(last) + " 被游戏占用，不能作为呼出快捷键。", Theme.Danger);
            return;
        }
        if (ordered.Count > 3)
        {
            ShowStatus("呼出快捷键最多 3 个键（例如 Ctrl+Alt+K）。", Theme.Danger);
            return;
        }

        uint mods = 0;
        foreach (var p in ordered.Take(ordered.Count - 1))
        {
            mods |= p switch
            {
                "lshift" or "rshift" => HotkeySpec.ModShift,
                "lctrl" or "rctrl" => HotkeySpec.ModControl,
                "lalt" or "ralt" => HotkeySpec.ModAlt,
                "lwin" or "rwin" => HotkeySpec.ModWin,
                _ => 0
            };
        }
        var spec = new HotkeySpec(mods, last);
        if (spec.Vk == 0)
        {
            ShowStatus("这个按键不能作为呼出快捷键。", Theme.Danger);
            return;
        }
        ChangeHotkey(spec);
    }

    private void ChangeHotkey(HotkeySpec spec)
    {
        var old = _hotkey;
        if (!TryRegisterHotkey(spec, report: false))
        {
            TryRegisterHotkey(old, report: false);
            ShowStatus(spec.Display + " 已被其他程序占用，未修改。", Theme.Danger);
            EndCapture();
            return;
        }

        _hotkey = spec;
        _settings.HotkeyModifiers = spec.Modifiers;
        _settings.HotkeyKey = spec.KeyName;
        _settings.Save();
        if (_hotkeyChip != null)
            _hotkeyChip.SetBinding(HotkeyToBinding(spec));
        _tray.Text = "BB 按键设置（" + spec.Display + "）";
        var oldMenu = _tray.ContextMenuStrip;
        _tray.ContextMenuStrip = BuildTrayMenu();
        oldMenu?.Dispose();
        EndCapture();
        ShowStatus("面板快捷键已改为 " + spec.Display, Theme.Muted);
    }

    private static string HotkeyToBinding(HotkeySpec spec)
    {
        var parts = new List<string>();
        if ((spec.Modifiers & HotkeySpec.ModControl) != 0) parts.Add("lctrl");
        if ((spec.Modifiers & HotkeySpec.ModShift) != 0) parts.Add("lshift");
        if ((spec.Modifiers & HotkeySpec.ModAlt) != 0) parts.Add("lalt");
        if ((spec.Modifiers & HotkeySpec.ModWin) != 0) parts.Add("lwin");
        parts.Add(spec.KeyName);
        return string.Join(", ", parts);
    }

    private static List<string> BindingToParts(string binding) => Spec.BindingParts(binding);

    private void HandleCaptureVk(int vk, long lParam, bool down)
    {
        var name = Native.VkToName(vk, lParam);
        if (name == null)
            return;

        if (down && vk == 0x1B && _captureModsHeld.Count == 0 && _captureModsSeen.Count == 0)
        {
            CancelCapture("已取消。");
            return;
        }

        bool isModifier = Spec.ModifierNamesSet.Contains(name) || name is "lwin" or "rwin";

        if (down)
        {
            if (isModifier)
            {
                if (!_captureModsHeld.Contains(name))
                    _captureModsHeld.Add(name);
                if (!_captureModsSeen.Contains(name))
                    _captureModsSeen.Add(name);
                ShowCaptureState();
                return;
            }

            if (_captureModsHeld.Contains(name))
                return;

            var parts = new List<string>(_captureModsHeld) { name };
            if (parts.Count > Spec.MaxCombo)
            {
                ShowStatus("最多 3 个按键（例如 Shift + Ctrl + K）。", Theme.Danger);
                return;
            }
            CompleteCapture(parts);
        }
        else
        {
            if (!isModifier)
                return;
            _captureModsHeld.Remove(name);
            if (_captureModsHeld.Count == 0 && _captureModsSeen.Count > 0)
            {
                // a modifier-only binding: complete when the last modifier is released
                CompleteCapture(new List<string>(_captureModsSeen));
            }
            else
            {
                ShowCaptureState();
            }
        }
    }

    private void ShowCaptureState()
    {
        var held = _captureModsHeld.Count > 0
            ? string.Join(" + ", _captureModsHeld.Select(Spec.DisplayName)) + " + …"
            : "";
        ShowStatus("请按下按键… " + held + "（Esc 取消，最多 3 个键）", Theme.Accent);
    }

    private void ClearChip(BindingChip chip)
    {
        if (chip == _hotkeyChip)
        {
            ChangeHotkey(HotkeySpec.Default);
            return;
        }
        var set = _bindings[chip.OutputName];
        set.Slots[chip.SlotIndex] = null;
        chip.SetBinding(null);
        MarkDirty();
        UpdateConflicts();
        ShowStatus("已清除「" + OutputTitle(chip.OutputName) + "」的一个绑定（未保存）", Theme.Muted);
    }

    private void ManualEntry(BindingChip chip, string output)
    {
        using var dlg = new ManualBindingDialog(output, chip.Binding ?? "");
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;
        var value = dlg.Value;
        if (!Spec.IsBindable(output, value))
        {
            MessageBox.Show(this, "格式无效。示例：space、lshift, leftbutton、mousewheelup", "按键设置",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var parts = Spec.BindingParts(value);
        var reserved = ReservedForBindings();
        var reservedPart = parts.FirstOrDefault(reserved.Contains);
        if (reservedPart != null)
        {
            MessageBox.Show(this, Spec.DisplayName(reservedPart) + " 被游戏或面板占用，不能绑定。", "按键设置",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var set = _bindings[output];
        set.Slots[chip.SlotIndex] = string.Join(", ", parts);
        chip.SetBinding(string.Join(", ", parts));
        MarkDirty();
        UpdateConflicts();
    }

    private void RestoreActionDefault(BindingChip chip, string output)
    {
        var defaults = Layouts.EffectiveDefault.Bindings.TryGetValue(output, out var d) ? d : new BindingSet();
        var set = _bindings[output];
        for (int i = 0; i < Spec.Slots; i++)
            set.Slots[i] = i < defaults.Slots.Length ? defaults.Slots[i] : null;
        set.Extra.Clear();
        set.Extra.AddRange(defaults.Extra);
        RefreshCategory();
        MarkDirty();
        ShowStatus("已恢复「" + OutputTitle(output) + "」默认绑定（未保存）", Theme.Muted);
    }

    private void SaveAsDefault()
    {
        if (MessageBox.Show(this,
                "把当前编辑器的按键和鼠标设置保存为「恢复默认」使用的布局？\n\n" +
                "以后点「恢复默认」会恢复这套键位；可以在「高级」页清除。",
                "设为默认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        Layouts.SaveUserDefault(_bindings, _mouse);
        RefreshCategory();
        ShowStatus("已保存为默认布局 · 以后「恢复默认」会用这套键位", Theme.Accent);
    }

    private void LoadLayout(InputDocument layout, string name)
    {
        if (MessageBox.Show(this,
                "将载入" + name + "布局到编辑器（不会立即写入）。\n\n" +
                "手柄绑定、注释和未知设置会保持不变。继续吗？",
                "载入布局", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        _bindings.Clear();
        foreach (var (k, v) in layout.Bindings)
            _bindings[k] = v.Clone();
        _mouse = layout.Mouse.Clone();
        RefreshCategory();
        MarkDirty();
        ShowStatus("已载入" + name + "布局，点「应用」写入 input.ini", Theme.Accent);
    }

    private void UpdateConflicts()
    {
        var conflicts = Spec.SlotConflicts(_bindings);
        foreach (var chip in _actionChips)
        {
            if (conflicts.TryGetValue(chip.OutputName, out var slots) &&
                slots.TryGetValue(chip.SlotIndex, out var others) && others.Count > 0)
            {
                chip.HasConflict = true;
                var names = others.Select(OutputTitle);
                var text = "与这些动作冲突：" + string.Join("、", names);
                chip.ConflictText = text;
                _toolTip.SetToolTip(chip, text);
            }
            else
            {
                chip.HasConflict = false;
                chip.ConflictText = null;
                _toolTip.SetToolTip(chip, string.IsNullOrWhiteSpace(chip.Binding)
                    ? "点击设置按键"
                    : Spec.DisplayBinding(chip.Binding) + "\n点击重新设置；× 清除");
            }
            chip.Invalidate();
        }
    }

    // ------------------------------------------------------------------ state / status

    private void MarkDirty()
    {
        _dirty = true;
        UpdateDirtyUi();
    }

    private void UpdateDirtyUi()
    {
        _titleLabel.Text = "按键设置" + (_dirty ? "  *" : "");
        _applyButton.Text = _dirty ? "应用 *" : "应用";
        _applyButton.Enabled = _path != null;
    }

    private void ShowStatus(string text, Color color)
    {
        _statusLabel.Text = text;
        _statusLabel.ForeColor = color;
    }

    private void RefreshGameStatus()
    {
        var game = Native.FindGameWindow();
        _gameHwnd = game?.Handle ?? IntPtr.Zero;
        if (game == null)
        {
            _gameBadge.Text = "○ 游戏未运行";
            _gameBadge.ForeColor = Theme.Muted;
            _warningLabel.Text = "游戏未运行：可以先改键，点「应用」保存；下次启动游戏生效。";
        }
        else
        {
            _gameBadge.Text = "● 游戏运行中";
            _gameBadge.ForeColor = Theme.IsDark ? Color.FromArgb(108, 203, 95) : Color.FromArgb(16, 124, 16);
            _warningLabel.Text = game.CoversScreen
                ? "游戏不会暂停，请在篝火或安全位置调整。全屏模式下若面板被遮挡，请按 Alt+Tab 或把游戏改为窗口模式。按 Esc 关闭。"
                : "游戏不会暂停，请在篝火或安全位置调整。按 Esc 关闭面板；点「应用」后会自动回到游戏并重载按键。";
        }

        if (_gameDetailLabel != null && !_gameDetailLabel.IsDisposed)
            _gameDetailLabel.Text = GameDetailText();
    }

    private string GameDetailText()
    {
        var game = Native.FindGameWindow();
        return game == null
            ? "未检测到游戏窗口（进程 bb-probe.exe）。请先启动游戏。"
            : $"已检测到：{game.Process} · 窗口「{game.Title}」" + (game.CoversScreen ? " · 全屏" : "");
    }

    /// <summary>
    /// If a different bbport copy is running, offer to follow its input.ini.
    /// Useful when several installations exist on the same PC.
    /// </summary>
    private void DetectAndSwitch()
    {
        var running = FindRunningGameInput();
        if (running == null)
        {
            RefreshGameStatus();
            ShowStatus("未检测到正在运行的游戏（bb-probe.exe）。", Theme.Muted);
            return;
        }

        string current = "";
        if (_path != null)
        {
            try { current = Path.GetFullPath(_path); } catch { current = _path; }
        }
        string candidate;
        try { candidate = Path.GetFullPath(running); } catch { candidate = running; }

        if (string.Equals(candidate, current, StringComparison.OrdinalIgnoreCase))
        {
            RefreshGameStatus();
            ShowStatus("已连接到正在运行的游戏配置。", Theme.Muted);
            return;
        }

        if (_dirty)
        {
            var r = MessageBox.Show(this,
                "检测到正在运行的游戏使用：\n" + candidate + "\n\n" +
                "当前面板有未保存的更改。切换会放弃这些更改，继续吗？",
                "切换配置", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes)
                return;
        }

        _path = candidate;
        _settings.InputIniPath = candidate;
        _settings.Save();
        LoadFromDisk();
        RefreshGameStatus();
        ShowStatus("已切换到正在运行的游戏配置。", Theme.Accent);
    }

    // ------------------------------------------------------------------ window / tray

    private void ShowOverlay()
    {
        if (_exiting) return;
        if (WindowState == FormWindowState.Minimized)
            WindowState = FormWindowState.Normal;

        var game = Native.FindGameWindow();
        _gameHwnd = game?.Handle ?? IntPtr.Zero;
        TopMost = _gameHwnd != IntPtr.Zero;

        var bounds = _gameHwnd != IntPtr.Zero
            ? Native.GetMonitorBounds(_gameHwnd)
            : Screen.FromPoint(Cursor.Position).WorkingArea;
        var w = Math.Min(Width, Math.Max(640, bounds.Width - 40));
        var h = Math.Min(Height, Math.Max(480, bounds.Height - 40));
        if (w != Width || h != Height)
            Size = new Size(w, h);
        Location = new Point(bounds.Left + (bounds.Width - w) / 2, bounds.Top + (bounds.Height - h) / 2);

        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
        Native.ForceForeground(Handle);
        RefreshGameStatus();
        if (_path == null)
            RefreshCategory();
    }

    private void HideToTray(bool showBalloon = true)
    {
        if (_capturingChip != null)
            CancelCapture("已取消。");
        Hide();
        if (showBalloon && !_settings.FirstRunTipShown)
        {
            _settings.FirstRunTipShown = true;
            _settings.Save();
            _tray.ShowBalloonTip(4000, "按键设置仍在后台运行",
                "按 " + _hotkey.Display + " 可在游戏中随时重新打开。右键托盘图标可以退出。",
                ToolTipIcon.Info);
        }
    }

    private void ExitApp()
    {
        if (_dirty)
        {
            var r = MessageBox.Show(this, "有未应用的按键更改。退出前保存吗？", "按键设置",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (r == DialogResult.Cancel)
                return;
            if (r == DialogResult.Yes && !SaveToDisk(makeBackup: true))
                return;
        }
        _exiting = true;
        Close();
    }

    private void OpenInputFile()
    {
        if (_path == null || !File.Exists(_path))
        {
            BrowseInputIni();
            return;
        }
        try { Process.Start(new ProcessStartInfo(_path) { UseShellExecute = true }); }
        catch (Exception ex) { ShowStatus("无法打开文件：" + ex.Message, Theme.Danger); }
    }

    private void OpenInputFolder()
    {
        var dir = _path != null ? Path.GetDirectoryName(_path) : AppContext.BaseDirectory;
        try { Process.Start(new ProcessStartInfo("explorer.exe", "\"" + dir + "\"") { UseShellExecute = true }); }
        catch (Exception ex) { ShowStatus("无法打开文件夹：" + ex.Message, Theme.Danger); }
    }

    // ------------------------------------------------------------------ hotkey

    private bool TryRegisterHotkey(HotkeySpec spec, bool report)
    {
        if (spec.Vk == 0)
            return false;
        if (_hotkeyRegistered)
        {
            Native.UnregisterHotKey(Handle, HotkeyId);
            _hotkeyRegistered = false;
        }
        bool ok = Native.RegisterHotKey(Handle, HotkeyId, spec.Modifiers | Native.ModNoRepeat, spec.Vk);
        if (ok)
            _hotkeyRegistered = true;
        else if (report)
        {
            MessageBox.Show(this,
                "无法注册面板快捷键 " + spec.Display + "（可能被其他程序占用）。\n\n" +
                "你仍然可以从托盘图标打开面板，或在「高级」页换一个快捷键。",
                "按键设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        return ok;
    }

    // ------------------------------------------------------------------ message loop

    protected override void WndProc(ref Message m)
    {
        if (ShowMessage != 0 && m.Msg == (int)ShowMessage)
        {
            ShowOverlay();
            return;
        }
        if (m.Msg == Native.WmHotkey && m.WParam.ToInt32() == HotkeyId)
        {
            ShowOverlay();
            return;
        }

        if (_capturingChip != null)
        {
            if (m.Msg is Native.WmKeyDown or Native.WmKeyUp or Native.WmSysKeyDown or Native.WmSysKeyUp)
            {
                int vk = m.WParam.ToInt32();
                bool down = m.Msg is Native.WmKeyDown or Native.WmSysKeyDown;
                HandleCaptureVk(vk, m.LParam.ToInt64(), down);
                return;
            }
            if (m.Msg == 0x020A) // WM_MOUSEWHEEL
            {
                int delta = (short)((long)m.WParam >> 16);
                _capturingChip.HandleWheel(delta);
                return;
            }
        }

        base.WndProc(ref m);
    }

    protected override bool ProcessKeyPreview(ref Message m)
    {
        if (_capturingChip != null &&
            (m.Msg is Native.WmKeyDown or Native.WmKeyUp or Native.WmSysKeyDown or Native.WmSysKeyUp))
        {
            int vk = m.WParam.ToInt32();
            bool down = m.Msg is Native.WmKeyDown or Native.WmSysKeyDown;
            HandleCaptureVk(vk, m.LParam.ToInt64(), down);
            return true;
        }
        return base.ProcessKeyPreview(ref m);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (_capturingChip != null)
        {
            // Key downs are pre-translated here before they reach the focused control.
            int vk = msg.WParam.ToInt32();
            if (vk != 0)
                HandleCaptureVk(vk, msg.LParam.ToInt64(), true);
            return true;
        }
        if (keyData == (Keys.Control | Keys.S))
        {
            _ = ApplyAndReturnToGameAsync();
            return true;
        }
        if (keyData == Keys.Escape)
        {
            HideToTray();
            return true;
        }
        if (keyData == Keys.F1)
        {
            OpenInputFile();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _nav.Focus();
        Theme.Apply(this);
        _contentPanel.AutoScrollPosition = new Point(0, 0);
    }

    protected override void OnDeactivate(EventArgs e)
    {
        if (_capturingChip != null)
            CancelCapture("已取消（窗口失去焦点）。");
        base.OnDeactivate(e);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_exiting && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideToTray();
            base.OnFormClosing(e);
            return;
        }

        _exiting = true;
        _gameTimer.Stop();
        if (_hotkeyRegistered)
            Native.UnregisterHotKey(Handle, HotkeyId);
        _tray.Visible = false;
        _tray.Dispose();

        _settings.InputIniPath = _path;
        _settings.HotkeyModifiers = _hotkey.Modifiers;
        _settings.HotkeyKey = _hotkey.KeyName;
        if (WindowState == FormWindowState.Normal)
        {
            _settings.WindowWidth = Width;
            _settings.WindowHeight = Height;
        }
        _settings.Save();
        base.OnFormClosing(e);
    }
}
