using System;
using System.IO;
using System.Windows;

namespace LibCyStd.Wpf.Tests
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            var app = new Application();
            var win = new MainWindow();
            app.Run(win);
        }
    }
}
