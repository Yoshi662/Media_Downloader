using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        //Relacionado con Youtube-dl
        Process Youtube_dl = new Process();
        private String _argumentos;
        public string Argumentos
        {
            get => _argumentos;
            set { _argumentos = value; OnPropertyChanged("Argumentos"); }
        }
        private bool Verbose = false;
        private bool Log = false;

        //Rutas
        private string YoutubedlPath;
        private string DownloadPath;

        //Extensiones
        private List<String> listaVideo = new List<String> { ".mp4", ".avi", ".mkv", ".webm" };
        private List<String> listaAudio = new List<String> { ".mp3", ".flac", ".aac", ".m4a", ".wav" };



        //KKode
        private int KonamiStatus = 0;
        Key[] KonamiKode = new Key[] { Key.Up, Key.Up, Key.Down, Key.Down, Key.Left, Key.Right, Key.Left, Key.Right, Key.B, Key.A };


        //Boilerplate
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow()
        {
            YoutubedlPath = AppDomain.CurrentDomain.BaseDirectory + @"Youtube-dl\";
            DownloadPath = AppDomain.CurrentDomain.BaseDirectory + @"Descargas";
            InitializeComponent();
            this.DataContext = this;
            StartGUI();
        }

        #region chunk of Logic

        private void Btn_Descargar(object sender, RoutedEventArgs e)
        {
            Youtube_dl.StartInfo.FileName = YoutubedlPath + "Youtube-dl.exe";
            Youtube_dl.StartInfo.RedirectStandardOutput = true;
            Youtube_dl.StartInfo.UseShellExecute = false;
            Youtube_dl.StartInfo.RedirectStandardInput = true;
            //Youtube_dl.StartInfo.CreateNoWindow = true;
            //añadimos la carpeta de salida al programa
            Youtube_dl.StartInfo.Arguments = Argumentos;

            String log = "";

            Youtube_dl.Start();

            StreamReader strOutStream = Youtube_dl.StandardOutput;

            if (Log)
            {
                while (!strOutStream.EndOfStream)
                {
                    log += strOutStream.ReadLine() + "\n";
                    Youtube_dl.StandardInput.WriteLine("Here we go");
                }
                MessageBox.Show(log);
                File.WriteAllText(YoutubedlPath + "Log.txt", log);
            }
        }


        private void RefrescarComando()
        {
            Argumentos = "";
            String TempCmd = "";
            //BEHOLD THE IF WALL

            //Listas (o no)
            if (chk_isPlayList.IsChecked == true)
            {
                TempCmd += " --yes-playlist";

                if (chk_startsAt.IsChecked == true) TempCmd += " --playlist-start " + txt_StartsAt.Text; //TODO Checkear nº
                if (chk_endsAt.IsChecked == true) TempCmd += " --playlist-end " + txt_endsAt.Text; //TODO Checkear nº
            }
            else { TempCmd += " --no-playlist"; }

            //Formato
            String extensionActual = cb_Formats.Text;
            extensionActual = extensionActual.TrimStart('.');
            if (rb_Audio.IsChecked == true)
            {


                TempCmd += " -x";
                TempCmd += " --audio-format " + extensionActual;
            }
            else
                TempCmd += " --recode-video " + extensionActual;


            //embed - thumbs - subs                                                                                     >dubs
            if (chk_Addsubsfile.IsChecked == true) TempCmd += " --all-subs";

            if (chk_AddThumbFile.IsChecked == true) TempCmd += " --write-thumbnail";

            if (chk_Embedsubs.IsChecked == true) TempCmd += " --embed-subs";

            if (chk_EmbedThumb.IsChecked == true) TempCmd += " --embed-thumbnail";

            //extra

            if (chk_Silencioso.IsChecked == true) TempCmd += " --quiet";

            if (Verbose) TempCmd += " --verbose";

            Argumentos = TempCmd + " -o " + "\"" + DownloadPath + @"\%(title)s.%(ext)s" + "\" \"" + txt_URL.Text + "\"";

        }

        private void Btn_Actualizar(object sender, RoutedEventArgs e)
        {
            Youtube_dl.StartInfo.FileName = YoutubedlPath + "Youtube-dl.exe";
            Youtube_dl.StartInfo.Arguments = "-U";
            Youtube_dl.Start();
        }

        private void AddPreset(object sender, RoutedEventArgs e)
        {
            //TODO BIG CHUNK OF CODE
        }

        private void FuKonami_Sequencer(object sender, KeyEventArgs e)
        {
            if (e.Key == KonamiKode[KonamiStatus])
                KonamiStatus++;
            else
                KonamiStatus = 0;

            if (KonamiStatus == KonamiKode.Length)
            {
                DevPanel.Visibility = Visibility.Visible;
                KonamiStatus = 0;
            }

        }

        private void dev_addVerbose(object sender, RoutedEventArgs e) => Verbose = true;

        private void dev_addLog(object sender, RoutedEventArgs e) => Log = true;
        #endregion

        #region GUI RESPONSIVENESS
        private void Seleccion_click(object sender, RoutedEventArgs e)
        { //Seguro que hay formas mejores de hacer esto.
            if (rb_Video.IsChecked == true)
            {
                cb_Formats.ItemsSource = listaVideo;
                cb_Formats.SelectedItem = listaVideo[0];
                chk_Embedsubs.IsEnabled = true;
                chk_EmbedThumb.IsEnabled = true;
            }
            else
            {
                cb_Formats.ItemsSource = listaAudio;
                cb_Formats.SelectedItem = listaAudio[0];
                chk_Embedsubs.IsEnabled = false;
                chk_EmbedThumb.IsEnabled = false;
                chk_Embedsubs.IsChecked = false;
                chk_EmbedThumb.IsChecked = false;
            }
            RefrescarComando();
        }
        private void Chk_startsAt_Click(object sender, RoutedEventArgs e)
        {
            if (chk_startsAt.IsChecked == true)
            {
                txt_StartsAt.IsEnabled = true;
            }
            else
            {
                txt_StartsAt.IsEnabled = false;
                txt_StartsAt.Text = "";
            }
            RefrescarComando();
        }
        private void Chk_endsAt_Click(object sender, RoutedEventArgs e)
        {
            if (chk_endsAt.IsChecked == true)
            {
                txt_endsAt.IsEnabled = true;
            }
            else
            {
                txt_endsAt.IsEnabled = false;
                txt_endsAt.Text = "";
            }
            RefrescarComando();
        }
        private void IsPlayList_Click(object sender, RoutedEventArgs e)
        {
            if (chk_isPlayList.IsChecked == true)
            {
                chk_startsAt.IsEnabled = true;
                chk_endsAt.IsEnabled = true;
            }
            else
            {
                chk_startsAt.IsEnabled = false;
                chk_startsAt.IsChecked = false;

                txt_StartsAt.IsEnabled = false;
                txt_StartsAt.Text = "";

                chk_endsAt.IsEnabled = false;
                chk_endsAt.IsChecked = false;

                txt_endsAt.IsEnabled = false;
                txt_endsAt.Text = "";
            }
            RefrescarComando();
        }
        private void Cb_Formats_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cb_Formats.SelectedItem.Equals(".avi") && rb_Video.IsChecked == true)
                {
                    chk_Embedsubs.IsChecked = false;
                    chk_Embedsubs.IsEnabled = false;
                }
                else
                {
                    chk_Embedsubs.IsEnabled = true;
                }
            }
            catch (NullReferenceException) { }
            //Esto es horrible, pero pega un error completamente normal cuando se cambia los items de este combobox
            //asi que a cascarla
            RefrescarComando();
        }
        private void GUI_RefrescarComando_Click(object sender, RoutedEventArgs e) => RefrescarComando();
        private void GUI_RefrescarComando_KeyUp(object sender, KeyEventArgs e) => RefrescarComando();
        private void StartGUI()
        {
            cb_Formats.ItemsSource = listaVideo;
            cb_Formats.SelectedItem = listaVideo[0];

            chk_startsAt.IsEnabled = false;
            txt_StartsAt.IsEnabled = false;
            chk_endsAt.IsEnabled = false;
            txt_endsAt.IsEnabled = false;
            RefrescarComando();
        }

        private void Txt_URL_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            txt_URL.Text = Clipboard.GetText();
            RefrescarComando();
        }




        #endregion


    }
}
