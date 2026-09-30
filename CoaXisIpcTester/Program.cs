using System;
using System.Windows.Forms;

namespace CoaXis.IpcTester;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}