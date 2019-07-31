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
     /* Video con subtitulos https://www.youtube.com/watch?v=YU4-LFAK7t0 (Introduction to the Overwatch WorkShop)
     * Video sin subtitulos https://www.youtube.com/watch?v=-8rTfTm6JN0 (BAKURETSU BAKURESTU)
     * 
     * Añadir
     * Presets
     * Sitios soportados
     */



    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        //Youtube-dl
        Process Youtube_dl = new Process();
        private String _argumentos;
        public string Argumentos
        {
            get => _argumentos;
            set { _argumentos = value; OnPropertyChanged("Argumentos"); }
        }

        private bool DevMode = false;
        private bool Verbose = false;
        private bool Log = false;


        //Rutas
        private string YoutubedlPath;
        private string DownloadPath;

        //Extensiones
        private List<String> listaVideo = new List<String> { ".mp4", ".avi", ".mkv", ".webm" };
        private List<String> listaAudio = new List<String> { ".mp3", ".flac", ".aac", ".m4a", ".wav" };
        private String extensionSeleccionada = "";



        //KKode
        private int KonamiStatus = 0;
        Key[] KonamiKode = new Key[] { Key.Up, Key.Up, Key.Down, Key.Down, Key.Left, Key.Right, Key.Left, Key.Right, Key.B, Key.A }; //START!


        //Boilerplate
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        //MAIN
        public MainWindow()
        {
            YoutubedlPath = AppDomain.CurrentDomain.BaseDirectory + @"Youtube-dl\";
            DownloadPath = AppDomain.CurrentDomain.BaseDirectory + @"Descargas";
            InitializeComponent();
            this.DataContext = this;
            Start();
        }

        #region big chunk of Logic

        private void Btn_Descargar(object sender, RoutedEventArgs e)
        {
            Youtube_dl.StartInfo.FileName = YoutubedlPath + "Youtube-dl.exe";
            StreamReader strOutStream;

            if (DevMode)
            {
                Youtube_dl.StartInfo.RedirectStandardOutput = true;
                Youtube_dl.StartInfo.UseShellExecute = false;
            }
            else
            {
                Youtube_dl.StartInfo.RedirectStandardOutput = false;
                Youtube_dl.StartInfo.UseShellExecute = true;
                Youtube_dl.StartInfo.CreateNoWindow = true;
            }



            //añadimos la carpeta de salida al programa
            Youtube_dl.StartInfo.Arguments = Argumentos;

            String log = "";

            Youtube_dl.Start();


            if (Log)
            {
                strOutStream = Youtube_dl.StandardOutput;
                while (!strOutStream.EndOfStream)
                {
                    log += strOutStream.ReadLine() + "\n";
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
            else
            {
                TempCmd += " --no-playlist";
            }

            //Formato
            extensionSeleccionada = cb_Formats.Text.TrimStart('.');
            if (rb_Audio.IsChecked == true)
            {

                TempCmd += " -x";
                TempCmd += " --audio-format " + extensionSeleccionada;
            }
            else
            {
                TempCmd += " --recode-video " + extensionSeleccionada;
            }


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
                DevMode = true;
            }

        }
        private void dev_addVerbose(object sender, RoutedEventArgs e)
        {
            chk_Verbose.IsChecked = !chk_Verbose.IsChecked;
            Verbose = chk_Verbose.IsChecked;
            RefrescarComando();
        }
        private void dev_addLog(object sender, RoutedEventArgs e)
        {
            chk_Log.IsChecked = !chk_Log.IsChecked;
            Log = chk_Log.IsChecked;

            RefrescarComando();
           if(Log) MessageBox.Show("Ahora al descargar videos, no aparecera la ventana.\nPara ver dicha informacion, " +
                "mire el fichero de log ubicado en\n" + YoutubedlPath + "\\log.txt"
                +"\n\nAlso tengo que hacer esto mas bonico");
        }
        private void MenuItem_ActualizarYTdl(object sender, RoutedEventArgs e)
        {
            Youtube_dl.StartInfo = new ProcessStartInfo
            {
                FileName = YoutubedlPath + "Youtube-dl.exe",
                RedirectStandardOutput = false,
                UseShellExecute = true,
                Arguments = "-U"
            }; //dis shit is useful bro


            Youtube_dl.Start();
        }
        private void MenuItem_CheckVersion(object sender, RoutedEventArgs e)
        {

            String salida = "Media_Downloader v0.3b" + "\n\n";

            //Youtube-dl GetLine
            salida += "Version Youtube-dl: " + GetLine(YoutubedlPath + "Youtube-dl.exe", "--version") + "\n\n";
            //FFMPEG version
            salida += GetLine(YoutubedlPath + "ffmpeg.exe", "-version") + "\n\n";
            //AVCONV version
            salida += GetLine(YoutubedlPath + "avconv.exe", "-version");

            MessageBox.Show(salida, "Versiones",MessageBoxButton.OK,MessageBoxImage.Information);
        }
        private void MenuItem_ListarComandos(object sender, RoutedEventArgs e) //TODO
        {
            String salida = "";
            Process p = new Process();
            StreamReader strOutStream;


            p.StartInfo = new ProcessStartInfo
            {
                FileName = YoutubedlPath + "Youtube - dl.exe",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = ""
            };



            p.Start();
            strOutStream = p.StandardOutput;
            while (!strOutStream.EndOfStream)
            {
                salida += strOutStream.ReadLine() + "\n";
            }
            strOutStream.Dispose();
            p.Dispose();

            MessageBox.Show(salida, "Lista Completa de opciones");
        }

        //Este metodo me parece tan especifico que necesita su propia documentacion
        /// <summary>
        /// Inicia un proceso con la ruta y los argumentos proporcionados y devuelve la primera linea
        /// </summary>
        /// <param name="Path">Ruta del proceso</param>
        /// <param name="Arguments">Argumentos pasados a dicho proceso</param>
        /// <param name="Line">La linea en Base 0 que devuelve</param>
        /// <returns>La primera linea que devuelve el proceso</returns>
        private String GetLine(String Path, String Arguments, int Line = 0)
        {
            String salida = "";
            Process p = new Process();
            StreamReader strOutStream;

            p.StartInfo = new ProcessStartInfo
            {
                FileName = Path,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = Arguments
            };

            p.Start();
            strOutStream = p.StandardOutput;
            while (!strOutStream.EndOfStream)
            {
                salida += strOutStream.ReadLine() + "\n";
            }
            strOutStream.Dispose();
            p.Dispose();

            return salida.Split('\n')[Line]; //kinda proud of dis
        }
        private void MenuItem_SitiosSoportados(object sender, RoutedEventArgs e)
        {
            Process.Start("https://ytdl-org.github.io/youtube-dl/supportedsites.html");
        }
        #endregion

        #region GUI RESPONSIVENESS
        private void Seleccion_click(object sender, RoutedEventArgs e)
        { //Seguro que hay formas mejores de hacer esto.
            if (rb_Video.IsChecked == true)
            {
                cb_Formats.ItemsSource = listaVideo;
                cb_Formats.SelectedItem = listaVideo[0];
                chk_Embedsubs.IsEnabled = true;
                //chk_EmbedThumb.IsEnabled = true;
            }
            else
            {
                cb_Formats.ItemsSource = listaAudio;
                cb_Formats.SelectedItem = listaAudio[0];
                chk_Embedsubs.IsEnabled = false;
                //chk_EmbedThumb.IsEnabled = false;
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
        private void Cb_Formats_DropDownClosed(object sender, EventArgs e)
        {
            try
            {
                if (rb_Video.IsChecked == true)
                {
                    if (cb_Formats.SelectedItem.Equals(".avi"))
                    {
                        chk_Embedsubs.IsChecked = false;
                        chk_Embedsubs.IsEnabled = false;
                    }
                    else
                    {
                        chk_Embedsubs.IsEnabled = true;
                    }

                    if (cb_Formats.SelectedItem.Equals(".mp4"))
                    {
                        chk_EmbedThumb.IsEnabled = true;
                    }
                    else
                    {
                        chk_EmbedThumb.IsEnabled = false;
                        chk_EmbedThumb.IsChecked = false;
                    }
                }
                else //Se selecciona audio
                {
                    if (cb_Formats.SelectedItem.Equals(".mp3"))
                    {
                        chk_EmbedThumb.IsEnabled = true;
                    }
                    else
                    {
                        chk_EmbedThumb.IsEnabled = false;
                        chk_EmbedThumb.IsChecked = false;
                    }
                }


            }
            catch (NullReferenceException) { }
            //Esto es horrible, pero pega un error completamente normal cuando se cambia los items de este combobox
            //asi que a cascarla
            RefrescarComando();
        }
        private void GUI_RefrescarComando_Click(object sender, RoutedEventArgs e) => RefrescarComando();
        private void GUI_RefrescarComando_KeyUp(object sender, KeyEventArgs e) => RefrescarComando();
        private void Start()
        {
            if (!Directory.Exists(YoutubedlPath))
            {
                MessageBox.Show("El programa no puede encontrar la carpeta con todos los archivos necesarios para funcionar." +
                    "\nAsegurate que la carpeta \"youtube-dl\" esta en la mismo directorio que este programa.", "Faltan Archivos", MessageBoxButton.OK, MessageBoxImage.Error);
                MainPanel.IsEnabled = false;
            }

            if(!Directory.Exists(DownloadPath)) Directory.CreateDirectory(DownloadPath);



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
        private void Chk_Embedsubs_Click(object sender, RoutedEventArgs e)
        {
            chk_Addsubsfile.IsChecked = chk_EmbedThumb.IsChecked;
            RefrescarComando();
        }

        #endregion

        
    }
}

