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
        private List<String> listaVideo = new List<String> { ".mp4", ".avi", ".mkv", ".webm" };
        private List<String> listaAudio = new List<String> { ".mp3", ".flac", ".aac",".m4a",".wav" };

        public MainWindow()
        {
            YoutubedlPath = AppDomain.CurrentDomain.BaseDirectory + @"Youtube-dl\";
            InitializeComponent();
            this.DataContext = this;
            cb_Formats.ItemsSource = listaVideo;
            cb_Formats.SelectedItem = listaVideo[0];
        }

        private void Btn_Descargar(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("relaja...");
        }

        #region GUI RESPONSIVENESS

        private void Seleccion_click(object sender, RoutedEventArgs e)
        {
            if (rb_Video.IsChecked == true)
            {
                cb_Formats.ItemsSource = listaVideo;
                cb_Formats.SelectedItem = listaVideo[0];
            } else
            {
                cb_Formats.ItemsSource = listaAudio;
                cb_Formats.SelectedItem = listaAudio[0];
            }
        }

        private void Chk_startsAt_Click(object sender, RoutedEventArgs e)
        {
            if(chk_startsAt.IsChecked == true)
            {
                txt_StartsAt.IsEnabled = true;
            } else
            {
                txt_StartsAt.IsEnabled = false;
                txt_StartsAt.Text = ""; 
            }
        }

        private void Chk_endsAt_Click(object sender, RoutedEventArgs e)
        {
            if (chk_endsAt.IsChecked == true)
            {
                chk_endsAt.IsEnabled = true;
            }
            else
            {
                txt_endsAt.IsEnabled = false;
                txt_endsAt.Text = "";
            }
        }
        #endregion

        private void Btn_Actualizar(object sender, RoutedEventArgs e)
        {
            Process p = new Process();
            p.StartInfo.FileName = YoutubedlPath + "Youtube-dl.exe";
            p.StartInfo.Arguments = "-U";
            p.Start();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Txt_StartsAt_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

        }

    

        private void Chk_Addsubsfile_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
