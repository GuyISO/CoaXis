using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CoaXis.Ui;

internal static class ScrollBarTheme
{
    internal static void Apply(Control control, Theme theme)
    {
        control.HandleCreated -= OnHandleCreated;
        control.HandleCreated += OnHandleCreated;
        if (control.IsHandleCreated)
        {
            ApplyToHandle(control, theme);
        }
    }

    private static void OnHandleCreated(object sender, EventArgs e)
    {
        if (sender is Control control)
        {
            ApplyToHandle(control, ThemeManager.CurrentTheme);
        }
    }

    private static void ApplyToHandle(Control control, Theme theme)
    {
        int result = SetWindowTheme(control.Handle, theme is DarkTheme ? "DarkMode_Explorer" : null, null);
        if (result < 0)
        {
            Trace.TraceWarning("スクロールバーのテーマ適用に失敗しました: 0x{0:X8}", result);
        }
    }

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int SetWindowTheme(IntPtr window, string subAppName, string subIdList);
}
