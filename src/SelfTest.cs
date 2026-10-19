// SPDX-License-Identifier: GPL-2.0-or-later
// Copyright (C) 2026 Swn555666
// Authors: Swn555666, Claude (Anthropic)

using System.Text;

namespace BBKeybinds;

/// <summary>
/// Headless checks for the input.ini format. Run with --selftest [report.txt].
/// </summary>
public static class SelfTest
{
    public static int Run(string? outPath)
    {
        var sb = new StringBuilder();
        int pass = 0, fail = 0;

        void Check(string name, bool ok, string? detail = null)
        {
            if (ok) { pass++; sb.AppendLine("PASS  " + name); }
            else { fail++; sb.AppendLine("FAIL  " + name + (detail != null ? "  — " + detail : "")); }
        }

        try
        {
            var lines = InputDocument.SplitLines(Layouts.DefaultInputIni);
            var def = InputDocument.Parse(Layouts.DefaultInputIni);

            Check("默认 cross = space", def.Bindings["cross"].All.SequenceEqual(new[] { "space" }),
                string.Join("|", def.Bindings["cross"].All));
            Check("默认 r1 = 3 + 鼠标左键", def.Bindings["r1"].All.SequenceEqual(new[] { "3", "leftbutton" }),
                string.Join("|", def.Bindings["r1"].All));
            Check("默认 r3 两个键", def.Bindings["r3"].All.SequenceEqual(new[] { "q", "c" }),
                string.Join("|", def.Bindings["r3"].All));
            Check("默认鼠标视角关闭", !def.Mouse.Enabled);
            Check("默认鼠标参数", Math.Abs(def.Mouse.DeadzoneOffset - 0.5) < 1e-9 &&
                                  Math.Abs(def.Mouse.Speed - 1.0) < 1e-9 &&
                                  Math.Abs(def.Mouse.SpeedOffset - 0.125) < 1e-9);
            Check("默认热键 f7/f8", def.ToggleKey == "f7" && def.ReloadKey == "f8");

            // unchanged round-trip: comments and the whole controller block must be identical
            var outLines = InputDocument.BuildOutputLines(lines, def.Bindings, def.Mouse);
            var origComments = lines.Where(l => l.TrimStart().StartsWith('#')).ToList();
            var newComments = outLines.Where(l => l.TrimStart().StartsWith('#')).ToList();
            Check("注释行原样保留", origComments.SequenceEqual(newComments));

            var origCtrl = ControllerBlock(lines);
            var newCtrl = ControllerBlock(outLines);
            Check("手柄段原样保留", origCtrl.Count > 10 && origCtrl.SequenceEqual(newCtrl),
                origCtrl.Count + " vs " + newCtrl.Count);
            Check("默认设置不追加多余鼠标行",
                !outLines.Any(l => l.StartsWith("mouse_invert_x") || l.StartsWith("mouse_invert_y") ||
                                   l.StartsWith("mouse_sensitivity_y") || l.StartsWith("mouse_camera_sensitivity")));

            // change one binding
            var mod = def.Bindings.ToDictionary(kv => kv.Key, kv => kv.Value.Clone(), StringComparer.Ordinal);
            mod["cross"].Slots[0] = "f";
            var out2 = InputDocument.BuildOutputLines(lines, mod, def.Mouse);
            var doc2 = InputDocument.Parse(string.Join("\n", out2));
            Check("修改 cross = f", doc2.Bindings["cross"].All.SequenceEqual(new[] { "f" }));
            Check("修改后手柄段不变", ControllerBlock(lines).SequenceEqual(ControllerBlock(out2)));
            Check("修改后注释不变",
                lines.Where(l => l.TrimStart().StartsWith('#')).SequenceEqual(
                    out2.Where(l => l.TrimStart().StartsWith('#'))));

            // combo
            mod["r2"].Slots[0] = "lshift, leftbutton";
            var out3 = InputDocument.BuildOutputLines(lines, mod, def.Mouse);
            var doc3 = InputDocument.Parse(string.Join("\n", out3));
            Check("组合键写回", doc3.Bindings["r2"].All.Contains("lshift, leftbutton"),
                string.Join("|", doc3.Bindings["r2"].All));

            // conflicts
            var clashDoc = InputDocument.Parse("cross = space\r\nr1 = space\r\n");
            var conf = Spec.SlotConflicts(clashDoc.Bindings);
            Check("冲突检测", conf.TryGetValue("cross", out var cs) && cs[0].Contains("r1"));
            var modClash = InputDocument.Parse("cross = lshift\r\nr1 = lshift, leftbutton\r\n");
            var conf2 = Spec.SlotConflicts(modClash.Bindings);
            Check("修饰键冲突", conf2.TryGetValue("cross", out var cs2) && cs2[0].Contains("r1"));

            var slotDoc = InputDocument.Parse("axis_left_y_minus = w\r\naxis_left_y_minus = k\r\npad_down = k\r\n");
            var slotConf = Spec.SlotConflicts(slotDoc.Bindings);
            Check("冲突槽位索引正确",
                slotConf.TryGetValue("axis_left_y_minus", out var sc) &&
                sc.ContainsKey(1) && sc[1].Contains("pad_down") && !sc.ContainsKey(0));

            // reserved / validation
            Check("保留键 Insert/F9", Spec.AlwaysReserved.Contains("insert") && Spec.AlwaysReserved.Contains("f9"));
            Check("摇杆方向拒绝鼠标键", !Spec.IsBindable("axis_left_x_minus", "leftbutton"));
            Check("最多 3 键", !Spec.IsBindable("cross", "a, b, c, d"));
            Check("重复键拒绝", !Spec.IsBindable("cross", "a, a"));
            Check("合法组合", Spec.IsBindable("cross", "lshift, leftbutton"));

            // DS3
            var ds3 = InputDocument.Parse(Layouts.Ds3Layout);
            Check("DS3 鼠标视角开启", ds3.Mouse.Enabled && ds3.Mouse.Stick == "right" && ds3.Mouse.Camera == "direct");
            Check("DS3 r2 组合键", ds3.Bindings["r2"].All.Contains("lshift, leftbutton"));
            Check("DS3 慢走", ds3.Bindings["leftjoystick_halfmode"].All.SequenceEqual(new[] { "lalt" }));

            var applied = Layouts.ApplyLayout(lines, Layouts.Ds3Layout);
            var adoc = InputDocument.Parse(string.Join("\n", applied));
            Check("应用 DS3 后鼠标视角开启", adoc.Mouse.Enabled && adoc.Mouse.Camera == "direct");
            Check("应用 DS3 后手柄段保留",
                ControllerBlock(lines).SequenceEqual(ControllerBlock(applied)));
            Check("应用 DS3 后无旧键盘行",
                !applied.Any(l => l.StartsWith("cross = space")));

            // actual file write/read
            var tmp = Path.Combine(Path.GetTempPath(), "bbkeys-selftest-" + Guid.NewGuid().ToString("N") + ".ini");
            File.WriteAllText(tmp, Layouts.DefaultInputIni, new UTF8Encoding(false));
            var loaded = InputDocument.Load(tmp);
            loaded.Bindings["cross"].Slots[0] = "f";
            var written = InputDocument.BuildOutputLines(loaded.Lines, loaded.Bindings, loaded.Mouse);
            File.WriteAllText(tmp, string.Join("\n", written) + "\n", new UTF8Encoding(false));
            var reread = InputDocument.Load(tmp);
            Check("文件写入/读取", reread.Bindings["cross"].All.SequenceEqual(new[] { "f" }));
            var head = File.ReadAllBytes(tmp).Take(3).ToArray();
            Check("写入后仍是 UTF-8 无 BOM",
                !(head.Length == 3 && head[0] == 0xEF && head[1] == 0xBB && head[2] == 0xBF));
            File.Delete(tmp);

            // mouse settings write
            var m2 = def.Mouse.Clone();
            m2.Enabled = true;
            m2.Stick = "right";
            m2.Camera = "direct";
            m2.InvertX = true;
            m2.SensitivityY = 2.5;
            var mout = InputDocument.BuildOutputLines(lines, def.Bindings, m2);
            var mdoc = InputDocument.Parse(string.Join("\n", mout));
            Check("鼠标设置写回", mdoc.Mouse.Enabled && mdoc.Mouse.Camera == "direct" &&
                                   mdoc.Mouse.InvertX && Math.Abs(mdoc.Mouse.SensitivityY - 2.5) < 1e-9);
            Check("关闭后写回注释行",
                InputDocument.BuildOutputLines(lines, def.Bindings, def.Mouse)
                    .Any(l => l.StartsWith("# mouse_to_joystick = right")));
        }
        catch (Exception ex)
        {
            fail++;
            sb.AppendLine("FAIL  异常: " + ex);
        }

        sb.AppendLine();
        sb.AppendLine(pass + " passed, " + fail + " failed");
        var path = outPath ?? Path.Combine(Path.GetTempPath(), "bbkeys-selftest.txt");
        try
        {
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
            Console.WriteLine(path);
        }
        catch { }

        return fail == 0 ? 0 : 1;
    }

    private static List<string> ControllerBlock(List<string> lines)
    {
        int start = lines.FindIndex(l => l.Trim() == "# Controller");
        int end = lines.FindIndex(l => l.Trim() == "# Hotkeys");
        if (start < 0 || end <= start)
            return new List<string>();
        return lines.Skip(start).Take(end - start).ToList();
    }
}
