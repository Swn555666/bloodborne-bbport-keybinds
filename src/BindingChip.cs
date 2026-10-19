// SPDX-License-Identifier: GPL-2.0-or-later
// Copyright (C) 2026 Swn555666
// Authors: Swn555666, Claude (Anthropic)

using System.Drawing.Drawing2D;

namespace BBKeybinds;

public delegate void MousePartsHandler(object? sender, List<string> parts);
public delegate void KeyMessageHandler(object? sender, int msg, int vk, long lParam);

/// <summary>
/// One binding slot. Draws the combination, starts capture on click, clears with the ×.
/// Keyboard capture is handled by the form (so keys such as Tab/Enter can be captured);
/// mouse buttons and the wheel are handled here.
/// </summary>
public sealed class BindingChip : Control
{
    private bool _hover;
    private bool _hoverClear;
    private bool _suppressContextMenu;
    private Rectangle _clearRect;

    public string OutputName { get; set; } = "";
    public int SlotIndex { get; set; }
    public string? Binding { get; set; }
    public bool Capturing { get; set; }
    public bool HasConflict { get; set; }
    public string? ConflictText { get; set; }
    public bool AllowClear { get; set; } = true;

    public event EventHandler? CaptureRequested;
    public event MousePartsHandler? MouseCaptured;
    public event KeyMessageHandler? KeyMessage;
    public event EventHandler? ClearRequested;

    public BindingChip()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.Selectable, true);
        Height = 36;
        Width = 130;
        TabStop = false;
        Cursor = Cursors.Hand;
        Font = new Font(Theme.FontFamilyName, 9f, FontStyle.Regular, GraphicsUnit.Point);
        BackColor = Theme.Card;
        DoubleBuffered = true;
    }

    public void SetBinding(string? value)
    {
        Binding = value;
        Invalidate();
    }

    public void SetCapturing(bool value)
    {
        Capturing = value;
        Invalidate();
    }

    public void HandleWheel(int delta)
    {
        if (!Capturing)
            return;
        var parts = Native.GetModifierParts();
        parts.Add(delta > 0 ? "mousewheelup" : "mousewheeldown");
        MouseCaptured?.Invoke(this, parts);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        var fill = Capturing ? Theme.AccentSoft
                 : HasConflict ? Theme.DangerSoft
                 : _hover ? Theme.CardHover
                 : Theme.Card;
        var border = Capturing ? Theme.Accent
                   : HasConflict ? Theme.Danger
                   : _hover ? ControlPaint.Dark(Theme.Border, 0.05f)
                   : Theme.Border;

        using (var path = Theme.RoundedRect(rect, 6))
        using (var brush = new SolidBrush(fill))
        using (var pen = new Pen(border, Capturing || HasConflict ? 1.6f : 1f))
        {
            g.FillPath(brush, path);
            g.DrawPath(pen, path);
        }

        int clearWidth = 0;
        if (AllowClear && !Capturing && !string.IsNullOrWhiteSpace(Binding))
            clearWidth = 24;

        var textRect = new Rectangle(10, 0, Width - 10 - 8 - clearWidth, Height);
        var text = Capturing
            ? "请按下按键…"
            : string.IsNullOrWhiteSpace(Binding) ? "—" : Spec.DisplayBinding(Binding);

        TextRenderer.DrawText(
            g, text, Font, textRect,
            Capturing ? Theme.Accent : string.IsNullOrWhiteSpace(Binding) ? Theme.Muted : Theme.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix);

        if (clearWidth > 0)
        {
            _clearRect = new Rectangle(Width - 26, (Height - 20) / 2, 20, 20);
            using var clearBrush = new SolidBrush(_hoverClear ? Theme.Danger : Theme.Muted);
            using var clearFont = new Font(Theme.FontFamilyName, 9f, FontStyle.Bold);
            TextRenderer.DrawText(g, "×", clearFont, _clearRect, clearBrush.Color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
        else
        {
            _clearRect = Rectangle.Empty;
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        _hoverClear = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        bool overClear = _clearRect.Contains(e.Location);
        if (overClear != _hoverClear)
        {
            _hoverClear = overClear;
            Invalidate();
        }
        Cursor = overClear ? Cursors.Default : Cursors.Hand;
        base.OnMouseMove(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (Capturing)
        {
            string? name = e.Button switch
            {
                MouseButtons.Left => "leftbutton",
                MouseButtons.Middle => "middlebutton",
                MouseButtons.Right => "rightbutton",
                MouseButtons.XButton1 => "sidebuttonback",
                MouseButtons.XButton2 => "sidebuttonforward",
                _ => null
            };
            if (name != null)
            {
                if (name == "rightbutton")
                    _suppressContextMenu = true;
                var parts = Native.GetModifierParts();
                parts.Add(name);
                MouseCaptured?.Invoke(this, parts);
            }
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            if (_clearRect.Contains(e.Location))
            {
                ClearRequested?.Invoke(this, EventArgs.Empty);
                return;
            }
            Focus();
            CaptureRequested?.Invoke(this, EventArgs.Empty);
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (Capturing)
        {
            HandleWheel(e.Delta);
            return;
        }
        base.OnMouseWheel(e);
    }

    protected override bool IsInputKey(Keys keyData) => true;

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!Capturing && (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space))
        {
            CaptureRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (_suppressContextMenu && (m.Msg is 0x0205 or 0x007B))
        {
            _suppressContextMenu = false;
            return;
        }
        if (Capturing && (m.Msg is 0x0100 or 0x0101 or 0x0104 or 0x0105))
        {
            KeyMessage?.Invoke(this, m.Msg, m.WParam.ToInt32(), m.LParam.ToInt64());
            return;
        }
        base.WndProc(ref m);
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        using var brush = new SolidBrush(BackColor);
        pevent.Graphics.FillRectangle(brush, ClientRectangle);
    }
}
