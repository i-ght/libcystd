using LibCyStd.Seq;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xaml;

namespace LibCyStd.Wpf
{
    public static class WpfModule
    {
        public static Stream WpfResrcStream(string resrcFileName)
        {
            var uri = new Uri(resrcFileName, UriKind.Relative);
            var resrc = Application.GetResourceStream(uri);
            return resrc.Stream;
        }

        public static ReadOnlyCollection<string> ReadResrcAsCollection(string resrcFileName)
        {
            using var strem = WpfResrcStream(resrcFileName);
            using var sr = new StreamReader(strem);
            var lst = new List<string>(20);
            while (!sr.EndOfStream)
                lst.Add(sr.ReadLine());
            return ReadOnlyCollectionModule.OfSeq(lst);
        }

        public static void InjectXaml(object root, string xamlResrcFileName)
        {
            var uri = new Uri(xamlResrcFileName, UriKind.Relative);
            var resrc = Application.GetResourceStream(uri);
            using var sr = new StreamReader(resrc.Stream);
            using var xamlXmlReader = new XamlXmlReader(
                sr,
                System.Windows.Markup.XamlReader.GetWpfSchemaContext()
            );
            using var xamlObjWriter = new XamlObjectWriter(
                xamlXmlReader.SchemaContext,
                new XamlObjectWriterSettings { RootObjectInstance = root }
            );
            while (xamlXmlReader.Read())
                xamlObjWriter.WriteNode(xamlXmlReader);
        }

        public static Option<FileInfo> SelectFile(
            string title = "Select file",
            string filter = "all|*")
        {
            var ofd = new OpenFileDialog
            {
                Title = title,
                Filter = filter
            };

            var result = ofd.ShowDialog();
            return
                result == true
                ? Option.Some(new FileInfo(ofd.FileName))
                : Option.None;
        }
    }
}
