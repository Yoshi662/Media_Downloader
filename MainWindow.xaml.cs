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
    * TODO:
    * Presets
    */

    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        #region Arguments, Fields and other pseudostatic thingies
        //Version
        private readonly String CurrentVersion = "0.4.1b";

        //Youtube-dl
        Process Youtube_dl = new Process();

        public string Argumentos
        {
            get => _argumentos;
            set { _argumentos = value; OnPropertyChanged("Argumentos"); }
        }
        private String _argumentos;
        
        public bool DevMode
        {
            get => _devMode;
            set
            {
                _devMode = value; OnPropertyChanged("DevMode");
                DevPanel.Visibility = value ? Visibility.Visible : Visibility.Hidden;

                /* Chicos. No hagais esto, es jodido de entender y no tiene sentido
                   // _ = value ? DevPanel.Visibility = Visibility.Visible : DevPanel.Visibility = Visibility.Hidden; Horrible
                   if (value) DevPanel.Visibility = Visibility.Visible; else DevPanel.Visibility = Visibility.Hidden; Funcional
                */


            }
        }
        private bool _devMode = false;

        public bool Verbose
        {
            get => _verbose;
            set { _verbose = value; OnPropertyChanged("Verbose"); }
        }
        private bool _verbose = false;

        public bool Log
        {
            get => _log;
            set { _log = value; OnPropertyChanged("Log"); }
        }
        private bool _log = false;

        //Rutas
        private string YoutubedlPath;
        private string DownloadPath;
        private string MainPath;

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
        #endregion

        #region big chunk of Logic
        public MainWindow()
        {
            YoutubedlPath = AppDomain.CurrentDomain.BaseDirectory + @"Youtube-dl\";
            DownloadPath = AppDomain.CurrentDomain.BaseDirectory + @"Descargas";
            MainPath = AppDomain.CurrentDomain.BaseDirectory;
            InitializeComponent();
            this.DataContext = this;
            Start();
        }

        private void Btn_Descargar_Click(object sender, RoutedEventArgs e)
        {
            Youtube_dl.StartInfo.FileName = YoutubedlPath + "Youtube-dl.exe";
            Refescar_STARTINFO();
            //añadimos la carpeta de salida al programa
            Youtube_dl.StartInfo.Arguments = Argumentos;


            Btn_Descargar.IsEnabled = false;
            Youtube_dl.Start();
            Youtube_dl.WaitForExit();
            Btn_Descargar.IsEnabled = true;

            if (Youtube_dl.ExitCode > 0 && DevMode != false) //Habemus fallo (Y al estar DevMode Activado. Pensamos que no podria ser normal)
            {
                MessageBoxResult resultado = MessageBox.Show("Ha habido un fallo en la ejecucion del programa." +
                "\n¿Quiere volver a intentar la descarga y guardar el resultado para poder revisarlo mas tarde?",
                "Error en la descarga.",
                MessageBoxButton.YesNo,
                MessageBoxImage.Error);

                if (resultado.Equals(MessageBoxResult.Yes))
                {
                    Log = true; DevMode = true; Verbose = true; //Habilitamos todas las opciones de debug/log
                    RefrescarComando(); Refescar_STARTINFO();
                    Youtube_dl.Start();
                }
            }

            if (Log)
            {
                String log = "", errlog = "";
                StreamReader strOutStream, strErrStream;
                String LogFile = MainPath + "Log.txt";
                //Podria encapsular esto en un metodo, pero para dos veces que se va a usar.



                //Get Standard Output
                strOutStream = Youtube_dl.StandardOutput;
                while (!strOutStream.EndOfStream)
                {
                    log += strOutStream.ReadLine() + "\n";
                }

                //Get ErrorOutput
                strErrStream = Youtube_dl.StandardError;
                while (!strErrStream.EndOfStream)
                {
                    errlog += strErrStream.ReadLine() + "\n";
                }
                File.WriteAllText(LogFile, errlog);

                //Write to File
                String SalidaLog = "------------------------------------------------------" +
                    "\n" + DateTime.Now.ToString() +
                    "\n\n" + errlog +
                    "\n" + log;

                MessageBox.Show(SalidaLog);
                File.WriteAllText(LogFile, SalidaLog);
            }
        }

        private void Refescar_STARTINFO()
        {
            if (DevMode)
            {
                Youtube_dl.StartInfo.RedirectStandardOutput = true;
                Youtube_dl.StartInfo.RedirectStandardError = true;
                Youtube_dl.StartInfo.UseShellExecute = false;
                Youtube_dl.StartInfo.CreateNoWindow = true;
            }
            else
            {
                Youtube_dl.StartInfo.RedirectStandardOutput = false;
                Youtube_dl.StartInfo.UseShellExecute = true;
                Youtube_dl.StartInfo.CreateNoWindow = false;
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
                KonamiStatus = 0;
                DevMode = true;
            }

        }
        private void dev_addVerbose(object sender, RoutedEventArgs e)
        {
            Verbose = !Verbose;
            RefrescarComando();
        }
        private void dev_addLog(object sender, RoutedEventArgs e)
        {
            Log = !Log;

            RefrescarComando();
            if (Log) MessageBox.Show("Ahora al descargar videos, no aparecera la ventana.\nPara ver dicha informacion, " +
                  "mire el fichero de log ubicado en\n" + YoutubedlPath + "\\log.txt"
                  + "\n\nAlso tengo que hacer esto mas bonico");
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

            String salida = $"Media_Downloader v{CurrentVersion}" + "\n\n";

            //Youtube-dl GetLine
            salida += "Version Youtube-dl: " + GetLine(YoutubedlPath + "Youtube-dl.exe", "--version") + "\n\n";
            //FFMPEG version
            salida += GetLine(YoutubedlPath + "ffmpeg.exe", "-version") + "\n\n";
            //AVCONV version
            salida += GetLine(YoutubedlPath + "avconv.exe", "-version");

            MessageBox.Show(salida, "Versiones", MessageBoxButton.OK, MessageBoxImage.Information);
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
                Environment.Exit(1);
            }

            if (!Directory.Exists(DownloadPath)) Directory.CreateDirectory(DownloadPath);



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