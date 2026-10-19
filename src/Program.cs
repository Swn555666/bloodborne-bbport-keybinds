// SPDX-License-Identifier: GPL-2.0-or-later
// Copyright (C) 2026 Swn555666
// Authors: Swn555666, Claude (Anthropic)

namespace BBKeybinds;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length > 0 && args[0].Equals("--selftest", StringComparison.OrdinalIgnoreCase))
        {
            Environment.ExitCode = SelfTest.Run(args.Length > 1 ? args[1] : null);
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using var mutex = new Mutex(true, @"Local\BBKeybinds_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            var msg = Native.RegisterWindowMessage("BBKeybinds.Show");
            for (int i = 0; i < 3; i++)
            {
                if (msg != 0)
                    Native.PostMessage(Native.HwndBroadcast, msg, IntPtr.Zero, IntPtr.Zero);
                Thread.Sleep(120);
            }
            return;
        }

        Theme.Initialize();

        Application.ThreadException += (_, e) =>
        {
            AppLog.Write("未处理异常: " + e.Exception);
            MessageBox.Show(
                "发生错误：\n" + e.Exception.Message + "\n\n详细信息已写入：\n" + AppSettings.LogPath,
                "BB 按键设置", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            AppLog.Write("未处理异常: " + e.ExceptionObject);
        };

        Application.Run(new MainForm());
    }
}
