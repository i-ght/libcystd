using LibCyStd.Seq;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xaml;

namespace LibCyStd.Wpf.Tests
{
    public class MainWindow : Window
    {
        private readonly IniCfg _iniCfg;
        private readonly ConfigDataGrid _cfgDataGrid;
        public Grid GrdCfgContent => (Grid)FindName("GrdCfgContent");

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _iniCfg.Save();
        }

        public MainWindow()
        {
            WpfModule.InjectXaml(this, "mainwindow.xaml");
            var items = ReadOnlyCollectionModule.OfSeq(new[]
            {
                new ConfigDataGridItem("int0", "int", 0),
                new ConfigDataGridItem("str0", "string", "hello world"),
                new ConfigDataGridItem("bool0", "bool", true),
                new ConfigDataGridItem("file0", "sequence", ConfigSequence.Empty)
            });

            _iniCfg = new IniCfg("settings.ini");
            _cfgDataGrid = new ConfigDataGrid(items, GrdCfgContent, _iniCfg);

            //Content = xamlObjWriter.Result;
            Closing += MainWindow_Closing;
        }


    }
}
