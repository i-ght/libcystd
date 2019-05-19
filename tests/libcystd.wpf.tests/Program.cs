using System;
using System.IO;
using System.Windows;

namespace LibCyStd.Wpf.Tests
{
    internal static class Program
    {
        [STAThread]
        private static int Main()
        {
            var app = new Application();
            var mWin = new MainWindow();
            return app.Run(mWin);
        }
    }
}
