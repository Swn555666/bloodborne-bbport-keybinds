// SPDX-License-Identifier: GPL-2.0-or-later
// Copyright (C) 2026 Swn555666
// Authors: Swn555666, Claude (Anthropic)

using System.Globalization;
using System.Text;

namespace BBKeybinds;

/// <summary>
/// The exact input.ini vocabulary and rules used by bbport's launcher
/// (bbport_input_config.py). Keep this file in sync with that module: it is the
/// single source of truth for what we read and write.
/// </summary>
public static class Spec
{
    public const int Slots = 3;
    public const int MaxCombo = 3;

    public static readonly string[] ButtonOutputs =
    {
        "cross", "circle", "square", "triangle", "l1", "r1", "l2", "r2", "l3", "r3",
        "options", "pad_up", "pad_down", "pad_left", "pad_right",
        "touchpad_left", "touchpad_right"
    };

    public static readonly string[] StickOutputs =
    {
        "axis_left_x_minus", "axis_left_x_plus", "axis_left_y_minus", "axis_left_y_plus",
        "axis_right_x_minus", "axis_right_x_plus", "axis_right_y_minus", "axis_right_y_plus"
    };

    public static readonly string[] HoldOutputs = { "leftjoystick_halfmode" };

    public static readonly string[] KeyboardOutputs =
        ButtonOutputs.Concat(StickOutputs).Concat(HoldOutputs).ToArray();

    public static readonly string[] KeyNames =
    {
        "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m",
        "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z",
        "0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
        "f1", "f2", "f3", "f4", "f5", "f6", "f7", "f8", "f9", "f10", "f11", "f12",
        "escape", "backspace", "delete", "insert", "home", "end", "pgup", "pgdown",
        "tab", "capslock", "enter",
        "lshift", "rshift", "lctrl", "rctrl", "lalt", "ralt",
        "lmeta", "rmeta", "lwin", "rwin", "space",
        "up", "down", "left", "right",
        "kp0", "kp1", "kp2", "kp3", "kp4", "kp5", "kp6", "kp7", "kp8", "kp9",
        "kpperiod", "kpcomma", "kpslash", "kpasterisk", "kpminus", "kpplus", "kpequals", "kpenter",
        "grave", "minus", "equals", "lbracket", "rbracket", "backslash",
        "semicolon", "apostrophe", "comma", "period", "slash",
        "printscreen", "scrolllock", "pausebreak"
    };

    public static readonly string[] MouseButtonNames =
    {
        "leftbutton", "middlebutton", "rightbutton", "sidebuttonback", "sidebuttonforward",
        "mousewheelup", "mousewheeldown", "mousewheelleft", "mousewheelright"
    };

    public static readonly string[] ModifierNames =
    {
        "lshift", "rshift", "lctrl", "rctrl", "lalt", "ralt"
    };

    public static readonly HashSet<string> KeyNamesSet = new(KeyNames, StringComparer.Ordinal);
    public static readonly HashSet<string> MouseButtonNamesSet = new(MouseButtonNames, StringComparer.Ordinal);
    public static readonly HashSet<string> StickOutputsSet = new(StickOutputs, StringComparer.Ordinal);
    public static readonly HashSet<string> ModifierNamesSet = new(ModifierNames, StringComparer.Ordinal);
    public static readonly HashSet<string> AlwaysReserved = new(StringComparer.Ordinal) { "insert", "f9" };

    public static readonly string[] LayoutSettings =
    {
        "leftjoystick_halfmode", "rightjoystick_halfmode", "mouse_to_joystick", "mouse_camera"
    };

    /// <summary>Order matters: it is the order the launcher writes these lines in.</summary>
    public static readonly (string Key, string Line)[] MouseSimpleLines =
    {
        ("invert_x", "mouse_invert_x"),
        ("invert_y", "mouse_invert_y"),
        ("sensitivity_y", "mouse_sensitivity_y"),
        ("camera", "mouse_camera"),
        ("camera_sensitivity", "mouse_camera_sensitivity")
    };

    public static readonly Dictionary<string, string> OutputTitles = new()
    {
        ["cross"] = "互动 / 菜单确认 (×)",
        ["circle"] = "闪避 / 冲刺 (○)",
        ["square"] = "使用快捷道具 (□)",
        ["triangle"] = "血瓶 (△)",
        ["l1"] = "变形武器 (L1)",
        ["r1"] = "轻攻击 (R1)",
        ["l2"] = "开火 (L2)",
        ["r2"] = "重攻击 (R2)",
        ["l3"] = "冲刺中跳跃 (L3)",
        ["r3"] = "锁定 / 重置镜头 (R3)",
        ["options"] = "游戏菜单 (Options)",
        ["pad_up"] = "十字键上",
        ["pad_down"] = "切换快捷道具 (十字键下)",
        ["pad_left"] = "切换左手武器 (十字键左)",
        ["pad_right"] = "切换右手武器 (十字键右)",
        ["touchpad_left"] = "手势 (触摸板左侧)",
        ["touchpad_right"] = "触摸板右侧 (调试菜单)",
        ["axis_left_y_minus"] = "向前移动",
        ["axis_left_y_plus"] = "向后移动",
        ["axis_left_x_minus"] = "向左移动",
        ["axis_left_x_plus"] = "向右移动",
        ["leftjoystick_halfmode"] = "慢走（按住）",
        ["axis_right_y_minus"] = "镜头上移",
        ["axis_right_y_plus"] = "镜头下移",
        ["axis_right_x_minus"] = "镜头左移",
        ["axis_right_x_plus"] = "镜头右移"
    };

    public static readonly (string Key, string Title, string[] Outputs)[] Groups =
    {
        ("move", "移动与镜头", new[]
        {
            "axis_left_y_minus", "axis_left_y_plus", "axis_left_x_minus", "axis_left_x_plus",
            "leftjoystick_halfmode", "circle", "l3", "r3",
            "axis_right_y_minus", "axis_right_y_plus", "axis_right_x_minus", "axis_right_x_plus"
        }),
        ("combat", "战斗", new[] { "r1", "r2", "l1", "l2" }),
        ("items", "物品与菜单", new[]
        {
            "cross", "square", "triangle", "pad_up", "pad_down", "pad_left", "pad_right",
            "options", "touchpad_left", "touchpad_right"
        })
    };

    public static readonly Dictionary<string, string> DisplayNames = new(StringComparer.Ordinal)
    {
        ["lshift"] = "Shift",
        ["rshift"] = "右 Shift",
        ["lctrl"] = "Ctrl",
        ["rctrl"] = "右 Ctrl",
        ["lalt"] = "Alt",
        ["ralt"] = "右 Alt",
        ["lwin"] = "Win",
        ["rwin"] = "右 Win",
        ["lmeta"] = "Meta",
        ["rmeta"] = "右 Meta",
        ["escape"] = "Esc",
        ["backspace"] = "Backspace",
        ["delete"] = "Delete",
        ["insert"] = "Insert",
        ["home"] = "Home",
        ["end"] = "End",
        ["pgup"] = "Page Up",
        ["pgdown"] = "Page Down",
        ["tab"] = "Tab",
        ["capslock"] = "Caps Lock",
        ["enter"] = "Enter",
        ["space"] = "Space",
        ["up"] = "↑",
        ["down"] = "↓",
        ["left"] = "←",
        ["right"] = "→",
        ["kpperiod"] = "小键盘 .",
        ["kpcomma"] = "小键盘 ,",
        ["kpslash"] = "小键盘 /",
        ["kpasterisk"] = "小键盘 *",
        ["kpminus"] = "小键盘 -",
        ["kpplus"] = "小键盘 +",
        ["kpequals"] = "小键盘 =",
        ["kpenter"] = "小键盘 Enter",
        ["grave"] = "`",
        ["minus"] = "-",
        ["equals"] = "=",
        ["lbracket"] = "[",
        ["rbracket"] = "]",
        ["backslash"] = "\\",
        ["semicolon"] = ";",
        ["apostrophe"] = "'",
        ["comma"] = ",",
        ["period"] = ".",
        ["slash"] = "/",
        ["printscreen"] = "Print Screen",
        ["scrolllock"] = "Scroll Lock",
        ["pausebreak"] = "Pause",
        ["leftbutton"] = "鼠标左键",
        ["middlebutton"] = "鼠标中键",
        ["rightbutton"] = "鼠标右键",
        ["sidebuttonback"] = "鼠标侧键（后退）",
        ["sidebuttonforward"] = "鼠标侧键（前进）",
        ["mousewheelup"] = "滚轮上",
        ["mousewheeldown"] = "滚轮下",
        ["mousewheelleft"] = "滚轮左",
        ["mousewheelright"] = "滚轮右"
    };

    public static string DisplayName(string part)
    {
        if (DisplayNames.TryGetValue(part, out var name))
            return name;
        if (part.Length == 2 && part[0] == 'k' && char.IsDigit(part[1]))
            return "小键盘 " + part[1];
        if (part.Length == 1)
            return part.ToUpperInvariant();
        if (part.StartsWith('f') && int.TryParse(part.AsSpan(1), out var fn) && fn >= 1 && fn <= 12)
            return "F" + fn;
        return part.ToUpperInvariant();
    }

    public static string DisplayBinding(string value) =>
        string.Join(" + ", BindingParts(value).Select(DisplayName));

    public static List<string> BindingParts(string value) =>
        value.Split(',').Select(p => p.Trim()).ToList();

    public static string FormatBinding(string value) =>
        string.Join(", ", BindingParts(value));

    public static bool IsBindable(string output, string value)
    {
        var parts = BindingParts(value);
        if (parts.Count < 1 || parts.Count > MaxCombo)
            return false;
        if (parts.Distinct(StringComparer.Ordinal).Count() != parts.Count)
            return false;
        var allowed = StickOutputsSet.Contains(output)
            ? KeyNamesSet
            : new HashSet<string>(KeyNamesSet.Concat(MouseButtonNamesSet), StringComparer.Ordinal);
        return parts.All(allowed.Contains);
    }

    /// <summary>
    /// True when two bindings would fire together: identical (in any order), or a
    /// single modifier that is also part of the other combination.
    /// </summary>
    public static bool Clash(List<string> pa, List<string> pb)
    {
        bool same = pa.SequenceEqual(pb) ||
                    new HashSet<string>(pa, StringComparer.Ordinal)
                        .SetEquals(new HashSet<string>(pb, StringComparer.Ordinal));
        bool inside =
            (pa.Count == 1 && ModifierNamesSet.Contains(pa[0]) && pb.Contains(pa[0])) ||
            (pb.Count == 1 && ModifierNamesSet.Contains(pb[0]) && pa.Contains(pb[0]));
        return same || inside;
    }

    public static HashSet<string> ReservedKeys(string toggleKey, string reloadKey, IEnumerable<string> panelHotkey)
    {
        var set = new HashSet<string>(AlwaysReserved, StringComparer.Ordinal) { toggleKey, reloadKey };
        foreach (var p in panelHotkey)
            set.Add(p);
        return set;
    }

    /// <summary>output -> slot -> other outputs it clashes with.</summary>
    public static Dictionary<string, Dictionary<int, HashSet<string>>> SlotConflicts(
        Dictionary<string, BindingSet> bindings)
    {
        var items = new List<(string Output, int Slot, List<string> Parts)>();
        foreach (var (output, set) in bindings)
        {
            for (int i = 0; i < Spec.Slots; i++)
            {
                var value = set.Slots[i];
                if (!string.IsNullOrWhiteSpace(value))
                    items.Add((output, i, BindingParts(value!)));
            }
            for (int j = 0; j < set.Extra.Count; j++)
                items.Add((output, Spec.Slots + j, BindingParts(set.Extra[j])));
        }

        var conflicts = new Dictionary<string, Dictionary<int, HashSet<string>>>(StringComparer.Ordinal);
        for (int n = 0; n < items.Count; n++)
        {
            var (a, ia, pa) = items[n];
            for (int m = n + 1; m < items.Count; m++)
            {
                var (b, ib, pb) = items[m];
                if (a == b)
                    continue;
                if (!Clash(pa, pb))
                    continue;
                if (!conflicts.TryGetValue(a, out var sa))
                    conflicts[a] = sa = new Dictionary<int, HashSet<string>>();
                if (!sa.TryGetValue(ia, out var oa))
                    sa[ia] = oa = new HashSet<string>(StringComparer.Ordinal);
                oa.Add(b);
                if (!conflicts.TryGetValue(b, out var sb))
                    conflicts[b] = sb = new Dictionary<int, HashSet<string>>();
                if (!sb.TryGetValue(ib, out var ob))
                    sb[ib] = ob = new HashSet<string>(StringComparer.Ordinal);
                ob.Add(a);
            }
        }
        return conflicts;
    }
}

public sealed class MouseSettings : IEquatable<MouseSettings>
{
    public bool Enabled = false;
    public string Stick = "right";
    public bool InvertX = false;
    public bool InvertY = false;
    public double SensitivityY = 1.0;
    public string Camera = "stick";
    public double CameraSensitivity = 1.0;
    public double DeadzoneOffset = 0.5;
    public double Speed = 1.0;
    public double SpeedOffset = 0.125;

    public MouseSettings Clone() => (MouseSettings)MemberwiseClone();

    public bool Equals(MouseSettings? other)
    {
        if (other is null) return false;
        return Enabled == other.Enabled && Stick == other.Stick && InvertX == other.InvertX &&
               InvertY == other.InvertY && SensitivityY.Equals(other.SensitivityY) &&
               Camera == other.Camera && CameraSensitivity.Equals(other.CameraSensitivity) &&
               DeadzoneOffset.Equals(other.DeadzoneOffset) && Speed.Equals(other.Speed) &&
               SpeedOffset.Equals(other.SpeedOffset);
    }

    public override bool Equals(object? obj) => Equals(obj as MouseSettings);

    public override int GetHashCode() => HashCode.Combine(Enabled, Stick, InvertX, InvertY,
        SensitivityY, Camera, CameraSensitivity, HashCode.Combine(DeadzoneOffset, Speed, SpeedOffset));
}

public sealed class BindingSet
{
    public string?[] Slots { get; } = new string?[Spec.Slots];
    public List<string> Extra { get; } = new();

    public IEnumerable<string> All => Slots.Where(s => !string.IsNullOrWhiteSpace(s))
                                           .Select(s => s!)
                                           .Concat(Extra);

    public int Count => All.Count();

    public bool IsEmpty => !All.Any();

    public void Add(string formatted)
    {
        for (int i = 0; i < Slots.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(Slots[i]))
            {
                Slots[i] = formatted;
                return;
            }
        }
        Extra.Add(formatted);
    }

    public BindingSet Clone()
    {
        var copy = new BindingSet();
        for (int i = 0; i < Slots.Length; i++)
            copy.Slots[i] = Slots[i];
        copy.Extra.AddRange(Extra);
        return copy;
    }
}

/// <summary>One input.ini file, parsed and ready to be written back.</summary>
public sealed class InputDocument
{
    public List<string> Lines { get; set; } = new();
    public Dictionary<string, BindingSet> Bindings { get; set; } = new(StringComparer.Ordinal);
    public MouseSettings Mouse { get; set; } = new();
    public string ToggleKey { get; set; } = "f7";
    public string ReloadKey { get; set; } = "f8";

    public static InputDocument Load(string path) =>
        Parse(File.ReadAllText(path, Encoding.UTF8));

    public static InputDocument Parse(string text)
    {
        var doc = new InputDocument
        {
            Lines = SplitLines(text),
            Bindings = Spec.KeyboardOutputs.ToDictionary(o => o, _ => new BindingSet(), StringComparer.Ordinal)
        };

        foreach (var raw in doc.Lines)
        {
            var line = NoWs(raw);
            int hash = line.IndexOf('#');
            string body = hash >= 0 ? line[..hash] : line;
            string commentedBody = hash >= 0 ? line[(hash + 1)..] : "";

            if (commentedBody.Contains('='))
            {
                var (co, cv) = SplitOnce(commentedBody, '=');
                var co0 = co.Split(':')[0].Trim();
                var cv0 = cv.Split(':')[0].Trim();
                if (co0 == "mouse_to_joystick" && (cv0 == "left" || cv0 == "right"))
                    doc.Mouse.Stick = cv0;
            }

            if (!body.Contains('='))
                continue;

            var (output, value) = SplitOnce(body, '=');
            output = output.Split(':')[0].Trim();
            value = value.Split(':')[0].Trim();

            if (output == "hotkey_toggle_mouse_to_joystick" && Spec.KeyNamesSet.Contains(value))
            {
                doc.ToggleKey = value;
                continue;
            }
            if (output == "hotkey_reload_inputs" && Spec.KeyNamesSet.Contains(value))
            {
                doc.ReloadKey = value;
                continue;
            }
            if (ParseMouseLine(output, value, doc.Mouse))
                continue;
            if (!doc.Bindings.TryGetValue(output, out var set))
                continue;
            if (!Spec.IsBindable(output, value))
                continue;
            set.Add(Spec.FormatBinding(value));
        }
        return doc;
    }

    private static bool ParseMouseLine(string output, string value, MouseSettings s)
    {
        if (output == "mouse_to_joystick" && (value == "left" || value == "right"))
        {
            s.Enabled = true;
            s.Stick = value;
            return true;
        }
        if ((output == "mouse_invert_x" || output == "mouse_invert_y") && (value == "on" || value == "off"))
        {
            bool on = value == "on";
            if (output == "mouse_invert_x") s.InvertX = on; else s.InvertY = on;
            return true;
        }
        if (output == "mouse_camera" && (value == "direct" || value == "stick"))
        {
            s.Camera = value;
            return true;
        }
        if (output == "mouse_sensitivity_y" || output == "mouse_camera_sensitivity")
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                return false;
            if (output == "mouse_sensitivity_y")
            {
                if (v < 0.1 || v > 10.0) return false;
                s.SensitivityY = v;
            }
            else
            {
                if (v < 0.05 || v > 20.0) return false;
                s.CameraSensitivity = v;
            }
            return true;
        }
        if (output == "mouse_movement_params")
        {
            var parts = value.Split(',');
            if (parts.Length != 3)
                return false;
            if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var dz) ||
                !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var sp) ||
                !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var so))
                return false;
            if (dz < 0.0 || dz > 1.0 || sp < 0.01 || sp > 20.0 || so < 0.0 || so > 1.0)
                return false;
            s.DeadzoneOffset = dz;
            s.Speed = sp;
            s.SpeedOffset = so;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Applies keyboard/mouse bindings and mouse settings to the original lines,
    /// preserving comments, controller lines and unknown lines byte-for-byte.
    /// </summary>
    public static List<string> BuildOutputLines(
        List<string> baseLines,
        Dictionary<string, BindingSet> bindings,
        MouseSettings mouse)
    {
        var afterBindings = ApplyBindings(baseLines, bindings);
        return ApplyMouseSettings(afterBindings, mouse);
    }

    private static List<string> ApplyBindings(List<string> lines, Dictionary<string, BindingSet> bindings)
    {
        var written = new HashSet<string>(StringComparer.Ordinal);
        var outLines = new List<string>();

        foreach (var raw in lines)
        {
            var line = NoWs(raw);
            int hash = line.IndexOf('#');
            var body = hash >= 0 ? line[..hash] : line;
            if (body.Contains('='))
            {
                var (co, cv) = SplitOnce(body, '=');
                var output = co.Split(':')[0].Trim();
                var value = cv.Split(':')[0].Trim();
                if (bindings.TryGetValue(output, out var set) && Spec.IsBindable(output, value))
                {
                    if (!written.Contains(output))
                    {
                        written.Add(output);
                        foreach (var b in set.All)
                            outLines.Add(output + " = " + Spec.FormatBinding(b));
                    }
                    continue;
                }
            }
            outLines.Add(raw);
        }

        foreach (var output in Spec.KeyboardOutputs)
        {
            if (written.Contains(output))
                continue;
            if (!bindings.TryGetValue(output, out var set) || set.IsEmpty)
                continue;
            foreach (var b in set.All)
                outLines.Add(output + " = " + Spec.FormatBinding(b));
        }
        return outLines;
    }

    private static List<string> ApplyMouseSettings(List<string> lines, MouseSettings s)
    {
        bool writtenToggle = false;
        bool writtenParams = false;
        var writtenSimple = new HashSet<string>(StringComparer.Ordinal);
        var outLines = new List<string>();

        foreach (var raw in lines)
        {
            var line = NoWs(raw);
            int hash = line.IndexOf('#');
            var body = hash >= 0 ? line[..hash] : line;
            var commentedBody = hash >= 0 ? line[(hash + 1)..] : "";

            bool isToggleLine =
                (body.Contains('=') && SplitOnce(body, '=').Lhs.Contains("mouse_to_joystick")) ||
                (commentedBody.Contains('=') && SplitOnce(commentedBody, '=').Lhs.Contains("mouse_to_joystick"));

            bool isParamsLine = body.Contains('=') &&
                                SplitOnce(body, '=').Lhs.Split(':')[0].Trim() == "mouse_movement_params";

            if (isToggleLine && !writtenToggle)
            {
                writtenToggle = true;
                var newLine = "mouse_to_joystick = " + s.Stick;
                outLines.Add(s.Enabled ? newLine : "# " + newLine);
                continue;
            }

            string? simpleKey = null;
            if (body.Contains('='))
            {
                var lhs = SplitOnce(body, '=').Lhs.Split(':')[0].Trim();
                foreach (var (key, lineName) in Spec.MouseSimpleLines)
                {
                    if (lhs == lineName) { simpleKey = key; break; }
                }
            }
            if (simpleKey != null && writtenSimple.Add(simpleKey))
            {
                outLines.Add(MouseSimpleLine(simpleKey, s));
                continue;
            }

            if (isParamsLine && !writtenParams)
            {
                writtenParams = true;
                outLines.Add(FormatMovementParams(s));
                continue;
            }

            outLines.Add(raw);
        }

        if (!writtenToggle)
        {
            var newLine = "mouse_to_joystick = " + s.Stick;
            outLines.Add(s.Enabled ? newLine : "# " + newLine);
        }
        if (!writtenParams)
            outLines.Add(FormatMovementParams(s));

        foreach (var (key, _) in Spec.MouseSimpleLines)
        {
            if (writtenSimple.Contains(key))
                continue;
            if (!IsDefaultSimple(s, key))
                outLines.Add(MouseSimpleLine(key, s));
        }
        return outLines;
    }

    private static string FormatMovementParams(MouseSettings s) =>
        string.Format(CultureInfo.InvariantCulture, "mouse_movement_params = {0:G3}, {1:G3}, {2:G3}",
            s.DeadzoneOffset, s.Speed, s.SpeedOffset);

    private static string MouseSimpleLine(string key, MouseSettings s)
    {
        var name = Spec.MouseSimpleLines.First(x => x.Key == key).Line;
        object value = key switch
        {
            "invert_x" => s.InvertX,
            "invert_y" => s.InvertY,
            "sensitivity_y" => s.SensitivityY,
            "camera" => s.Camera,
            "camera_sensitivity" => s.CameraSensitivity,
            _ => ""
        };
        if (value is bool b)
            return name + " = " + (b ? "on" : "off");
        if (value is string str)
            return name + " = " + str;
        return name + " = " + ((double)value).ToString("G3", CultureInfo.InvariantCulture);
    }

    private static bool IsDefaultSimple(MouseSettings s, string key) => key switch
    {
        "invert_x" => !s.InvertX,
        "invert_y" => !s.InvertY,
        "sensitivity_y" => Math.Abs(s.SensitivityY - 1.0) < 1e-9,
        "camera" => s.Camera == "stick",
        "camera_sensitivity" => Math.Abs(s.CameraSensitivity - 1.0) < 1e-9,
        _ => true
    };

    /// <summary>Serializes the editor state as a reusable layout (no controller lines).</summary>
    public static string ToLayoutText(Dictionary<string, BindingSet> bindings, MouseSettings mouse)
    {
        var baseLines = new List<string>
        {
            "# 用户默认布局（由 BB 按键设置保存）",
            "# 恢复默认时会载入这些键鼠绑定和鼠标设置",
            ""
        };
        var lines = BuildOutputLines(baseLines, bindings, mouse);
        return string.Join("\n", lines) + "\n";
    }

    public void Save(string path)
    {
        var text = string.Join("\n", Lines) + "\n";
        File.WriteAllText(path, text, new UTF8Encoding(false));
    }

    // ---- small helpers shared with the UI ----

    public static List<string> SplitLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return new List<string>();
        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        var parts = normalized.Split('\n').ToList();
        if (parts.Count > 0 && parts[^1].Length == 0)
            parts.RemoveAt(parts.Count - 1);
        return parts;
    }

    public static string NoWs(string s) =>
        new(s.Where(c => !char.IsWhiteSpace(c)).ToArray());

    public static (string Lhs, string Rhs) SplitOnce(string s, char c)
    {
        int i = s.IndexOf(c);
        return i < 0 ? (s, "") : (s[..i], s[(i + 1)..]);
    }
}

public static class Layouts
{
    public const string DefaultInputIni = """
# bbport input (shadPS4 syntax: output = input, one input per line).
# A shadPS4 input config (user/input_config/CUSA03173.ini) can be copied over this file.
# F7 toggles the mouse, F8 reloads this file.

# Keyboard
cross = space
circle = lshift
square = e
triangle = v
l1 = 1
r1 = 3
l2 = r
r2 = f
l3 = z
r3 = q
r3 = c
options = enter
pad_up = i
pad_down = k
pad_left = j
pad_right = l
touchpad_left = tab
touchpad_right = backspace

axis_left_x_minus = a
axis_left_x_plus = d
axis_left_y_minus = w
axis_left_y_plus = s
axis_right_x_minus = left
axis_right_x_plus = right
axis_right_y_minus = up
axis_right_y_plus = down

# Hold to halve the left stick (walk): uncomment and pick a key
# leftjoystick_halfmode = lalt

# Mouse (uncomment mouse_to_joystick to turn it on; buttons work only while captured)
# mouse_to_joystick = right
mouse_movement_params = 0.5, 1, 0.125
r1 = leftbutton
r2 = rightbutton
circle = sidebuttonback
square = sidebuttonforward

# Controller
cross = cross
circle = circle
square = square
triangle = triangle
l1 = l1
r1 = r1
l2 = l2
r2 = r2
l3 = l3
r3 = r3
options = options
touchpad_left = back
pad_up = pad_up
pad_down = pad_down
pad_left = pad_left
pad_right = pad_right
axis_left_x = axis_left_x
axis_left_y = axis_left_y
axis_right_x = axis_right_x
axis_right_y = axis_right_y

# Hotkeys
hotkey_toggle_mouse_to_joystick = f7
hotkey_reload_inputs = f8
""";

    public const string Ds3Layout = """
# Keyboard and mouse: Dark Souls III layout (launcher, Controls page)
axis_left_x_minus = a
axis_left_x_plus = d
axis_left_y_minus = w
axis_left_y_plus = s
leftjoystick_halfmode = lalt
axis_right_x_minus = j
axis_right_x_plus = l
axis_right_y_minus = i
axis_right_y_plus = k
mouse_to_joystick = right
mouse_camera = direct
circle = space
l3 = c
r3 = q
r3 = middlebutton
r1 = leftbutton
r2 = lshift, leftbutton
l1 = rightbutton
l2 = lshift, rightbutton
l2 = lctrl
cross = e
cross = enter
square = r
triangle = f
pad_up = up
pad_up = mousewheelup
pad_down = down
pad_down = mousewheeldown
pad_left = left
pad_left = lshift, mousewheelup
pad_right = right
pad_right = lshift, mousewheeldown
options = tab
options = escape
touchpad_left = g
touchpad_right = backspace
""";

    private static readonly Lazy<InputDocument> DefaultDoc = new(() => InputDocument.Parse(DefaultInputIni));
    private static readonly Lazy<InputDocument> Ds3Doc = new(() => InputDocument.Parse(Ds3Layout));

    public static InputDocument Default => DefaultDoc.Value;
    public static InputDocument Ds3 => Ds3Doc.Value;

    /// <summary>The layout "恢复默认" uses: the player's saved layout, else the factory one.</summary>
    public static string UserDefaultPath => Path.Combine(AppSettings.Dir, "default-layout.ini");
    public static bool HasUserDefault => File.Exists(UserDefaultPath);

    public static InputDocument? TryLoadUserDefault()
    {
        try
        {
            if (File.Exists(UserDefaultPath))
                return InputDocument.Load(UserDefaultPath);
        }
        catch { }
        return null;
    }

    private static InputDocument? _userDefaultCache;
    private static DateTime _userDefaultStamp;

    public static InputDocument EffectiveDefault
    {
        get
        {
            try
            {
                var fi = new FileInfo(UserDefaultPath);
                if (fi.Exists)
                {
                    if (_userDefaultCache == null || fi.LastWriteTimeUtc != _userDefaultStamp)
                    {
                        _userDefaultCache = InputDocument.Load(UserDefaultPath);
                        _userDefaultStamp = fi.LastWriteTimeUtc;
                    }
                    return _userDefaultCache;
                }
            }
            catch { }
            return Default;
        }
    }

    public static void SaveUserDefault(Dictionary<string, BindingSet> bindings, MouseSettings mouse)
    {
        Directory.CreateDirectory(AppSettings.Dir);
        File.WriteAllText(UserDefaultPath,
            InputDocument.ToLayoutText(bindings, mouse), new UTF8Encoding(false));
        _userDefaultCache = null;
    }

    public static void ClearUserDefault()
    {
        try { if (File.Exists(UserDefaultPath)) File.Delete(UserDefaultPath); } catch { }
        _userDefaultCache = null;
    }

    /// <summary>Replaces keyboard/mouse bindings and layout settings, keeps controller lines.</summary>
    public static List<string> ApplyLayout(List<string> lines, string layout)
    {
        var outLines = new List<string>();
        var layoutLines = InputDocument.SplitLines(layout);
        var header = layoutLines.Count > 0 ? layoutLines[0] : "";
        var layoutSettings = new HashSet<string>(Spec.LayoutSettings, StringComparer.Ordinal);

        foreach (var raw in lines)
        {
            if (raw.Trim() == header)
                continue;
            var line = InputDocument.NoWs(raw);
            bool commented = line.StartsWith('#');
            var body = line.TrimStart('#').Split('#')[0];
            if (body.Contains('='))
            {
                var (co, cv) = InputDocument.SplitOnce(body, '=');
                var output = co.Split(':')[0].Trim();
                var value = cv.Split(':')[0].Trim();
                if (layoutSettings.Contains(output))
                    continue;
                if (!commented && Spec.KeyboardOutputs.Contains(output) && Spec.IsBindable(output, value))
                    continue;
            }
            outLines.Add(raw);
        }

        while (outLines.Count > 0 && string.IsNullOrWhiteSpace(outLines[^1]))
            outLines.RemoveAt(outLines.Count - 1);

        outLines.Add("");
        outLines.AddRange(layoutLines);
        return outLines;
    }
}
