using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Media_Downloader
{
    public partial class MainWindow : Window
    {
        private string YoutubedlPath;

        public MainWindow()
        {
            YoutubedlPath = AppDomain.CurrentDomain.BaseDirectory + @"Youtube-dl\";
            InitializeComponent();
        }

        private void Btn_Descargar(object sender, RoutedEventArgs e)
        {
            Process p = new Process();
            p.StartInfo.FileName = YoutubedlPath + "Youtube-dl.exe";
            p.StartInfo.Arguments = "-U";
            p.Start();
        }
    }
}
