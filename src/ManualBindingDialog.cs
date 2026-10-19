// SPDX-License-Identifier: GPL-2.0-or-later
// Copyright (C) 2026 Swn555666
// Authors: Swn555666, Claude (Anthropic)

namespace BBKeybinds;

public sealed class ManualBindingDialog : Form
{
    public string Value { get; private set; } = "";

    public ManualBindingDialog(string output, string current)
    {
        Text = "手动输入按键";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        Font = Theme.BaseFont;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        ClientSize = new Size(500, 300);
        Padding = new Padding(18);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Theme.Bg
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var title = new Label
        {
            Text = "「" + (Spec.OutputTitles.TryGetValue(output, out var t) ? t : output) + "」的手动绑定",
            Font = Theme.BoldFont,
            AutoSize = true,
            ForeColor = Theme.Text
        };
        root.Controls.Add(title, 0, 0);

        var hint = new Label
        {
            Text = "用英文键名输入，多个键用逗号分隔。\r\n" +
                   "示例：space　lshift, leftbutton　mousewheelup\r\n\r\n" +
                   "可用：a–z、0–9、f1–f12、escape、backspace、delete、home、end、\r\n" +
                   "pgup、pgdown、tab、enter、space、up/down/left/right、\r\n" +
                   "mousewheelup/down、leftbutton、rightbutton、middlebutton、\r\n" +
                   "sidebuttonback、sidebuttonforward。\r\n" +
                   "摇杆方向只能绑定键盘键。",
            Font = Theme.SmallFont,
            ForeColor = Theme.Muted,
            AutoSize = true,
            MaximumSize = new Size(460, 0),
            Margin = new Padding(0, 8, 0, 10)
        };
        root.Controls.Add(hint, 0, 1);

        var box = new TextBox
        {
            Text = current,
            Dock = DockStyle.Top,
            Font = new Font("Consolas", 10f),
            BackColor = Theme.Card,
            ForeColor = Theme.Text,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, 0, 12)
        };
        root.Controls.Add(box, 0, 2);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Fill,
            BackColor = Theme.Bg,
            Margin = new Padding(0)
        };
        var ok = Theme.MakeButton("确定", primary: true);
        ok.DialogResult = DialogResult.OK;
        var cancel = Theme.MakeButton("取消");
        cancel.DialogResult = DialogResult.Cancel;
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 3);

        Controls.Add(root);
        AcceptButton = ok;
        CancelButton = cancel;
        Theme.Apply(this);

        ok.Click += (_, _) =>
        {
            Value = Spec.FormatBinding(box.Text);
            if (string.IsNullOrWhiteSpace(Value))
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(this, "请输入至少一个按键。", "手动输入", MessageBoxButtons.OK, MessageBoxIcon.Information);
                box.Focus();
            }
        };
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Native.SetDarkTitleBar(Handle, Theme.IsDark);
    }
}
