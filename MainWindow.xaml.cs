using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace Media_Downloader
{
    /* Video con subtitulos https://www.youtube.com/watch?v=YU4-LFAK7t0 (Introduction to the Overwatch WorkShop)
    * Video sin subtitulos https://www.youtube.com/watch?v=-8rTfTm6JN0 (BAKURETSU BAKURESTU)
    * TODO:
    *   Big things:
    *       Localizacion. (No tengo NPI como lo voy a hacer)
    *       Pruebas unitarias? Vendrian muy bien pero es mucho curro.
    *       Mover todo Youtube-dl a una clase/proceso (Pasandole una preset / argumentos) 
    *       y que trabaje en su propio hilo. (Ya sabes, para eso que no se joda toda la Interfaz cuando estas haciendo algo)
    *       Esto se puede usar para que el hilo principal de ejecucion de la GUI y
    *       el de Youtube-dl trabajen independientemente (y poder dar avisos al usuario sobre el tema)
    *       
    *   Smol things:
    *       hacer un poco de cast en los setters de StartsAtInt y EndsAtInt (Aunque el binding se ocupe de ello)
    */

    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        #region Arguments, Fields and other pseudostatic thingies
        //Version
        private readonly String CurrentVersion = "0.7.0a";

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
            set
            {
                _verbose = value; OnPropertyChanged("Verbose");
            }
        }
        private bool _verbose = false;

        public bool Log
        {
            get => _log;
            set
            {
                _log = value; OnPropertyChanged("Log");
                if (Log) MessageBox.Show(Properties.Strings.LogEnabledWarning + YoutubedlPath + "\\log.txt"
                      + Properties.Strings.Bonico);
            }
        }
        private bool _log = false;

        public bool IsPlaylist
        {
            get => _isPlaylist; set
            {
                _isPlaylist = value; OnPropertyChanged("IsPlaylist");
                if (value)
                {
                    chk_startsAt.IsEnabled = true;
                    chk_endsAt.IsEnabled = true;
                }
                else
                {
                    chk_startsAt.IsEnabled = false;
                    chk_startsAt.IsChecked = false;

                    txt_startsAt.IsEnabled = false;
                    txt_startsAt.Text = "";

                    chk_endsAt.IsEnabled = false;
                    chk_endsAt.IsChecked = false;

                    txt_endsAt.IsEnabled = false;
                    txt_endsAt.Text = "";
                }
            }
        }
        private bool _isPlaylist;

        public bool StartsAt
        {
            get => _startsAt; set
            {
                _startsAt = value; OnPropertyChanged("StartsAt");
                if (value)
                {
                    txt_startsAt.IsEnabled = true;
                    txt_startsAt.Text = "";
                }
                else
                {
                    txt_startsAt.IsEnabled = false;
                    StartsAtInt = 0;
                }

            }
        }
        private bool _startsAt;

        public bool EndsAt
        {
            get => _endsAt; set
            {
                _endsAt = value; OnPropertyChanged("EndsAt");
                if (value)
                {
                    txt_endsAt.IsEnabled = true;
                    txt_endsAt.Text = "";
                }
                else
                {
                    txt_endsAt.IsEnabled = false;
                    EndsAtInt = 0;
                }

            }
        }
        private bool _endsAt;

        public int StartsAtInt { get => _startsAtInt; set { _startsAtInt = value; OnPropertyChanged("StartsAtInt"); } }
        private int _startsAtInt;

        public int EndsAtInt { get => _endsAtInt; set { _endsAtInt = value; OnPropertyChanged("EndsAtInt"); } }
        private int _endsAtInt;

        public bool Quiet { get => _quiet; set { _quiet = value; OnPropertyChanged("Quiet"); } }
        private bool _quiet;

        public bool Download_thumbnails { get => _download_thumbnails; set { _download_thumbnails = value; OnPropertyChanged("Download_thumbnails"); } }
        private bool _download_thumbnails;

        public bool Download_subs { get => _download_subs; set { _download_subs = value; OnPropertyChanged("Download_subs"); } }
        private bool _download_subs;

        public bool Embed_thumbnails
        {
            get => _embed_thumbnails; set
            {
                _embed_thumbnails = value; OnPropertyChanged("Embed_thumbnails");
                Download_thumbnails = value;
            }
        }
        private bool _embed_thumbnails;

        public bool Embed_subs { get => _embed_subs; set { _embed_subs = value; OnPropertyChanged("Embed_subs"); } }
        private bool _embed_subs;

        public bool IsVideo { get => _isVideo; set { _isVideo = value; OnPropertyChanged("IsVideo"); Seleccion_click(); } }
        private bool _isVideo;

        public bool IsAudio { get => _isAudio; set { _isAudio = value; OnPropertyChanged("IsAudio"); Seleccion_click(); } }
        private bool _isAudio;



        //Rutas
        private string YoutubedlPath = "";
        private string DownloadPath = "";
        private string MainPath = "";
        private string PresetsFilePath = "";

        //Extensiones
        private String _ExtensionSeleccionada = "";
        public string ExtensionSeleccionada
        {
            get
            {
                return _ExtensionSeleccionada;
            }
            set
            {
                _ExtensionSeleccionada = value;
                OnPropertyChanged("ExtensionSeleccionada");
            }
        }
        //Presets
        private List<Preset> _presets = new List<Preset>();
        public List<Preset> Presets
        {
            get
            {
                return _presets;
            }
            set
            {
                _presets = value;
                SavePresetsToFile(PresetsFilePath, value);
                LoadPresetMenu(value);
            }
        }


        //Otros
        private String formatoDescarga = @"\%(title)s.%(ext)s";

        //KKode
        private int KonamiStatus = 0;
        Key[] KonamiKode = new Key[] { Key.Up, Key.Up, Key.Down, Key.Down, Key.Left, Key.Right, Key.Left, Key.Right, Key.B, Key.A }; //START!


        //Boilerplate
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));

            //El metodo RefrescarComando escribe sobre la Propiedad Argumentos. Que tiene un OnPropertyChanged, y vuelta a empezar. (Genera un StackOverflow)
            if (!propertyName.Equals("Argumentos"))
            {
                RefrescarComando();
            }
        }
        #endregion

        #region big chungus of Logic
        public MainWindow()
        {
            System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo(Properties.Settings.Default.Idioma);
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;

            InitializeComponent();
            this.DataContext = this;
            this.Dispatcher.UnhandledException += Dispatcher_UnhandledException;
            this.Closing += MainWindow_Closing;
            Start();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(Properties.Strings.ExceptionMsg, Properties.Strings.ExceptionTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            String LogFile = MainPath + "Log.txt";
            File.AppendAllText(LogFile, e.Exception.Message);
            File.AppendAllText(LogFile, e.Exception.StackTrace);
            Environment.Exit(1);
        }

        private void Btn_Descargar_Click(object sender, RoutedEventArgs e)
        {
            Youtube_dl.StartInfo.FileName = YoutubedlPath + "Youtube-dl.exe";
            Refescar_STARTINFO();
            //añadimos la carpeta de salida al programa
            Youtube_dl.StartInfo.Arguments = Argumentos;

            GetDownloadNames(MainPath + "Filenames.tmp", txt_URL.Text);

            Dispatcher.BeginInvoke((Action)delegate
            {
                Btn_Descargar.IsEnabled = false;
                StatusBar.Text = Properties.Strings.Descargando_archivos;
            }, DispatcherPriority.Background);


            Youtube_dl.Start();
            Youtube_dl.WaitForExit();

            if (Youtube_dl.ExitCode > 0 && DevMode != false) //Habemus fallo (Y al estar DevMode Activado. Pensamos que no podria ser normal)
            {
                Dispatcher.BeginInvoke((Action)delegate
                {
                    StatusBar.Text = Properties.Strings.Fallo_en_la_descarga;
                }, DispatcherPriority.Background);
                MessageBoxResult resultado = MessageBox.Show(Properties.Strings.DownloadErrorMsg,
                Properties.Strings.DownloadErrorTitle,
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
                Dispatcher.BeginInvoke((Action)delegate
                {
                    StatusBar.Text = Properties.Strings.Generando_log;
                }, DispatcherPriority.Background);
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

                File.WriteAllText(LogFile, SalidaLog);
            }
            Dispatcher.BeginInvoke((Action)delegate
            {
                StatusBar.Text = Properties.Strings.Convirtiendo_Archivos;
            }, DispatcherPriority.Background);
            FFMPEG conversor = new FFMPEG(YoutubedlPath + "ffmpeg.exe");
            conversor.Convert(MainPath + "Filenames.tmp", ExtensionSeleccionada);

            while (File.Exists(MainPath + "Filenames.tmp"))
            {
                Thread.Sleep(200);
            }

            Dispatcher.BeginInvoke((Action)delegate
            {
                Btn_Descargar.IsEnabled = true;
                StatusBar.Text = "";
            }, DispatcherPriority.Background);
        }

        private async void GetDownloadNames(String file, String url)
        {

            String args = "";

            if (IsPlaylist)
            {
                args += " --yes-playlist";

                if (StartsAt) args += " --playlist-start " + StartsAtInt;
                if (EndsAt) args += " --playlist-end " + EndsAtInt;
            }
            else
            {
                args += " --no-playlist";
            }


            ThreadStart threadStart = new ThreadStart(() =>
            {
                Process pr = new Process();
                pr.StartInfo = new ProcessStartInfo
                {
                    FileName = YoutubedlPath + "Youtube-dl.exe",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Arguments = " --get-filename" + args + " -o \"" + DownloadPath + formatoDescarga + "\" --restrict-filenames \"" + url + "\""
                };
                Console.Write(pr.StartInfo.Arguments);
                pr.Start();
                StreamReader strOutStream = pr.StandardOutput;
                String salida = "";
                while (!strOutStream.EndOfStream)
                {
                    salida += strOutStream.ReadLine() + "\n";
                }
                salida.TrimEnd();
                File.WriteAllText(file, salida);
            });
            Thread th = new Thread(threadStart);
            th.Start();
        }

        private void Refescar_STARTINFO()
        {
            if (Log || Quiet)
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
            //FIXME when the storm is all over

            String TempCmd = "";
            //BEHOLD THE IF WALL

            //Listas (o no)
            if (IsPlaylist)
            {
                TempCmd += " --yes-playlist";

                if (StartsAt) TempCmd += " --playlist-start " + StartsAtInt;
                if (EndsAt) TempCmd += " --playlist-end " + EndsAtInt;
            }
            else
            {
                TempCmd += " --no-playlist";
            }

            //TODO Remove when it works
            //Formato

            //TODO hacer que esta asignacion se haga en otro sitio
            //ExtensionSeleccionada = cb_Formats.Text.TrimStart('.');

            if (IsAudio)
            {

                TempCmd += " -x";
                TempCmd += " --audio-format " + ExtensionSeleccionada;
            }


            //embed - thumbs - subs                                                                                     >dubs
            if (Download_subs) TempCmd += " --all-subs";

            if (Download_thumbnails) TempCmd += " --write-thumbnail";

            if (Embed_subs) TempCmd += " --embed-subs";

            if (Embed_thumbnails) TempCmd += " --embed-thumbnail";

            //extra


            if (Verbose) TempCmd += " --verbose";

            Argumentos = TempCmd + " -o " + "\"" + DownloadPath + formatoDescarga + "\" --restrict-filenames \"" + txt_URL.Text + "\"";

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
            salida += "Version Youtube-dl: " + GetLineFromProcess(YoutubedlPath + "Youtube-dl.exe", "--version") + "\n\n";
            //FFMPEG version
            salida += GetLineFromProcess(YoutubedlPath + "ffmpeg.exe", "-version") + "\n\n";
            MessageBox.Show(salida, "Versiones", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void MenuItem_ListarComandos(object sender, RoutedEventArgs e) //TODO
        {
            String salida = "";
            Process p = new Process();
            StreamReader strOutStream;


            p.StartInfo = new ProcessStartInfo
            {
                FileName = YoutubedlPath + "Youtube-dl.exe",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = "--help"
            };

            p.Start();
            strOutStream = p.StandardOutput;
            while (!strOutStream.EndOfStream)
            {
                salida += strOutStream.ReadLine() + "\n";
            }
            strOutStream.Dispose();
            p.Dispose();

            MessageBox.Show(salida, Properties.Strings.Lista_Completa_de_opciones);
        }

        //Este metodo me parece tan especifico que necesita su propia documentacion
        /// <summary>
        /// Inicia un proceso con la ruta y los argumentos proporcionados y devuelve la primera linea
        /// </summary>
        /// <param name="Path">Ruta del proceso</param>
        /// <param name="Arguments">Argumentos pasados a dicho proceso</param>
        /// <param name="Line">La linea en Base 0 que devuelve</param>
        /// <returns>La primera linea que devuelve el proceso</returns>
        private String GetLineFromProcess(String Path, String Arguments, int Line = 0)
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
        private void Btn_AbrirCarpeta_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(DownloadPath);
        }

        private void cb_Formats_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /*ComboBoxItem typeItem = IsVideo ? (ComboBoxItem)cb_Formats_Video.SelectedItem : (ComboBoxItem)cb_Formats_Audio.SelectedItem;
            ExtensionSeleccionada = typeItem.Content.ToString();*/
            if ((sender as ComboBox).SelectedItem != null)
            {
                String exttemp = ((sender as ComboBox).SelectedItem as ComboBoxItem).Content.ToString();
                ExtensionSeleccionada = exttemp;
                txt_URL.Text = exttemp;
            }

        }

        #endregion

        #region Presets
        //Mira, son las 6 de la mañana, voy a dejar esto como me gustaria ahora y luego ya lo hago. Quizas no es la mejor manera, pero sientente libre de borrar
        //todo esto

        //Y SI, VOY A USAR LOS NOMBRES DE LAS PRESETS COMO IDS Y ME DA IGUAL LO QUE PIENSES


        /// <summary>
        /// Carga una lista de presets desde un archivo XML
        /// </summary>
        /// <param name="fullPath">La ruta completa del archivo XML</param>
        /// <returns>Una Lista con todos los Presets</returns>
        private List<Preset> LoadPresetsFromFile(String fullPath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<Preset>));
            TextReader textReader = File.OpenText(PresetsFilePath);
            List<Preset> retorno;
            try
            {
                retorno = (List<Preset>)serializer.Deserialize(textReader);
            }
            //Si el XML es invalido
            //Borra el fichero de presets y lo vuelve a guardar con las presets actuales.
            catch (InvalidOperationException)
            {
                textReader.Close();
                File.Delete(PresetsFilePath);

                SavePresetsToFile(PresetsFilePath, Presets);

                textReader = File.OpenText(PresetsFilePath);
                retorno = (List<Preset>)serializer.Deserialize(textReader);
            }
            textReader.Close();
            return retorno;
        }


        /// <summary>
        /// Guarda una lista de presets en un archivo XML
        /// </summary>
        /// <param name="fullPath">La ruta completa donde se va a guardar</param>
        /// <param name="presets">La lista con las presets</param>
        private void SavePresetsToFile(String fullPath, List<Preset> presets)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<Preset>));
            TextWriter textWriter = File.CreateText(PresetsFilePath);
            serializer.Serialize(textWriter, presets);
            textWriter.Close();

        }


        /// <summary>
        /// Carga el menu de la interfaz grafica.
        /// </summary>
        /// <param name="presets">La lista de presets a cargar</param>
        private void LoadPresetMenu(List<Preset> presets)
        {
            //Dis gon be gud - Y si. He hecho unas carpetas y he hecho un cmd>tree para obtener la lista de abajo

            //  PresetName
            //  ├───Usar
            //  ├───Usar como predeterminada
            //  └───Borrar (Necesita confirmacion)
            this.Menu_presets.Items.Clear();

            MenuItem anyadir = new MenuItem();
            anyadir.Header = Properties.Strings.Añadir_Preset;
            anyadir.Click += AddPreset;

            Menu_presets.Items.Add(anyadir);

            List<MenuItem> ListaMenus = new List<MenuItem>();
            for (int i = 0; i < presets.Count; i++)
            {
                //Create main menu item
                MenuItem PresetMenuItem = new MenuItem();
                PresetMenuItem.Header = (String)presets[i].Name;

                //Create Usar MenuItem
                MenuItem UsarMenuItem = new MenuItem();
                UsarMenuItem.Header = Properties.Strings.Usar;
                UsarMenuItem.Click += PresetMenuItem_Click;

                //Create Usar como predeterminada MenuItem
                MenuItem PrederminadoMenuItem = new MenuItem();
                PrederminadoMenuItem.Header = Properties.Strings.Usar_como_predeterminada;
                PrederminadoMenuItem.Click += PrederminadoMenuItem_Click;

                //Create Borrar MenuItem
                MenuItem BorrarMenuItem = new MenuItem();
                BorrarMenuItem.Header = Properties.Strings.Borrar;
                BorrarMenuItem.Click += BorrarMenuItem_Click;

                //Añado los menus correspondientes
                Menu_presets.Items.Add(PresetMenuItem);

                PresetMenuItem.Items.Add(UsarMenuItem);
                PresetMenuItem.Items.Add(PrederminadoMenuItem);
                PresetMenuItem.Items.Add(BorrarMenuItem);

            }
            this.Menu_presets.UpdateLayout();
        }

        private void BorrarMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            MenuItem father = (MenuItem)menuItem.Parent;
            string NombrePreset = (String)father.Header;

            //confirmacion
            MessageBoxResult result = MessageBox.Show(Properties.Strings.Desea_borrar_la_preset_ + NombrePreset, Properties.Strings.Borrar_Preset, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result.Equals(MessageBoxResult.Yes))
            {
                for (int i = 0; i < Presets.Count; i++)
                {
                    if (Presets[i].Name.Equals(NombrePreset))
                    {
                        Presets.RemoveAt(i);
                        i--;
                    }
                }
                SavePresetsToFile(PresetsFilePath, Presets);
                LoadPresetMenu(Presets);
            }
        }

        private void PrederminadoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            MenuItem father = (MenuItem)menuItem.Parent;
            string NombrePreset = (String)father.Header;
            for (int i = 0; i < Presets.Count; i++)
            {
                if (Presets[i].Name.Equals(NombrePreset))
                {
                    Preset p = Presets[i];
                    Presets.RemoveAt(i);
                    Presets.Insert(0, p);
                }
            }
            SavePresetsToFile(PresetsFilePath, Presets);
            LoadPresetMenu(Presets);
        }

        private void PresetMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            MenuItem father = (MenuItem)menuItem.Parent;
            string NombrePreset = (String)father.Header;
            foreach (Preset preset in Presets)
            {
                if (preset.Name.Equals(NombrePreset)) LoadPreset(preset);
            }
        }

        /// <summary>
        /// Carga una preset en la interfaz grafica
        /// </summary>
        /// <param name="preset">Preset a cargar</param>
        public void LoadPreset(Preset preset)
        {
            IsPlaylist = preset.IsPlaylist;
            StartsAt = preset.StartsAt;
            EndsAt = preset.EndsAt;
            StartsAtInt = preset.StartsAtInt;
            EndsAtInt = preset.EndsAtInt;
            Quiet = preset.Quiet;
            Download_thumbnails = preset.Download_thumbnails;
            Download_subs = preset.Download_subs;
            Embed_thumbnails = preset.Embed_thumb;
            Embed_subs = preset.Embed_subs;
            ExtensionSeleccionada = preset.Extension;
            bool isVideo = preset.Media_type == (int)media_types.video;
            if (isVideo)
            {
                IsVideo = isVideo; IsAudio = !isVideo;
                cb_Formats_Video.Visibility = isVideo ? Visibility.Visible : Visibility.Collapsed;
                cb_Formats_Video.SelectedItem = ExtensionSeleccionada;
            }
            else
            {
                IsVideo = isVideo; IsAudio = !isVideo;
                cb_Formats_Audio.Visibility = !isVideo ? Visibility.Visible : Visibility.Collapsed;
                cb_Formats_Audio.SelectedItem = ExtensionSeleccionada;
            } //Seguro que hay una forma mas complicada _y mejor_ de hacer esto, pero esto tambien tira

            DownloadPath = preset.DownloadPath;
            //si no existe la carpeta de descargas la crea
            if (!Directory.Exists(DownloadPath)) Directory.CreateDirectory(DownloadPath);
            DevMode = preset.DevMode;
        }

        private void AddPreset(object sender, RoutedEventArgs e)
        {
            //Flags
            bool repetido = false,
                 canceled = false;
            //ruta y nombre de la  preset
            string path = "",
                   name = "";

            //preguntamos por el nombre ¡Y! control de presets repetidas
            name = Microsoft.VisualBasic.Interaction.InputBox(Properties.Strings.Introduce_el_nombre_del_preset, Properties.Strings.MeBeingLazy);
            canceled = string.IsNullOrWhiteSpace(name);
            if (!canceled)
            {
                do
                {
                    repetido = false;
                    foreach (Preset preset in Presets)
                    {
                        if (preset.Name.Equals(name)) repetido = true;
                    }
                    if (repetido)
                    {
                        MessageBox.Show(Properties.Strings.PresetExists, Properties.Strings.Nombre_Repetido, MessageBoxButton.OK, MessageBoxImage.Error);
                        name = Microsoft.VisualBasic.Interaction.InputBox(Properties.Strings.Introduce_el_nombre_del_preset, Properties.Strings.MeBeingLazy);
                        canceled = string.IsNullOrWhiteSpace(name);
                    }
                } while (repetido);
            }



            //Control de rutas
            if (!canceled)
            {
                using (System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    SelectedPath = MainPath,
                    ShowNewFolderButton = true,
                    Description = Properties.Strings.SelectDownloadFolder,
                })
                {
                    System.Windows.Forms.DialogResult result = folderBrowserDialog.ShowDialog();
                    folderBrowserDialog.SelectedPath = MainPath + Properties.Strings.Descargas;
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        path = folderBrowserDialog.SelectedPath;
                    }
                    else
                    {
                        canceled = true;
                    }
                    DownloadPath = path;
                }
            }

            //Si esta todo bien guardamos la preset
            if (!canceled && !repetido)
            {
                Presets.Add(SavePreset(name)); SavePresetsToFile(PresetsFilePath, Presets); LoadPresetMenu(Presets);
            }
            //he preguntado tres veces por la misma condicion? Si. Se te ocurre una forma mejor? haz un pull request
        }

        /// <summary>
        /// Guarda las opciones actuales dentro de una preset
        /// </summary>
        /// <param name="nombre">El nombre de la preset</param>
        /// <returns>Preset con las opciones actuales</returns>
        public Preset SavePreset(String nombre)
        {
            Preset preset = new Preset(
                nombre,
                IsPlaylist,
                StartsAt,
                EndsAt,
                StartsAtInt,
                EndsAtInt,
                Quiet,
                Download_thumbnails,
                Download_subs,
                Embed_thumbnails,
                Embed_subs,
                ExtensionSeleccionada, //Puede que esto falle
                DownloadPath,
                IsVideo ? (int)media_types.video : (int)media_types.audio,
                DevMode
                );
            return preset;
        }

        #endregion

        #region GUI RESPONSIVENESS
        private void Seleccion_click()
        { //Seguro que hay formas mejores de hacer esto. SEGURISIMO
           if (IsVideo)
            {
               if(cb_Formats_Video.SelectedIndex == -1)
                {
                    cb_Formats_Video.SelectedIndex = 0;
                }
                cb_Formats_Video.Visibility = Visibility.Visible;
                cb_Formats_Audio.Visibility = Visibility.Collapsed;
                ExtensionSeleccionada = cb_Formats_Video.Text;
                chk_Embedsubs.IsEnabled = true;
            }
            else
            {
                if (cb_Formats_Audio.SelectedIndex == -1)
                {
                    cb_Formats_Audio.SelectedIndex = 0;
                }
                cb_Formats_Audio.Visibility = Visibility.Visible;
                cb_Formats_Video.Visibility = Visibility.Collapsed;
                ExtensionSeleccionada = cb_Formats_Audio.Text;
                chk_Embedsubs.IsEnabled = false;
                chk_Embedsubs.IsChecked = false;
            }
            RefrescarComando();
        }



        private void Cb_Formats_DropDownClosed(object sender, EventArgs e)
        {
            try
            {
                if (rb_Video.IsChecked == true)
                {
                    if (ExtensionSeleccionada.Equals(".avi"))
                    {
                        chk_Embedsubs.IsChecked = false;
                        chk_Embedsubs.IsEnabled = false;
                    }
                    else
                    {
                        chk_Embedsubs.IsEnabled = true;
                    }

                    if (ExtensionSeleccionada.Equals(".mp4"))
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
                    if (ExtensionSeleccionada.Equals(".mp3"))
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
            YoutubedlPath = AppDomain.CurrentDomain.BaseDirectory + @"Youtube-dl\";
            MainPath = AppDomain.CurrentDomain.BaseDirectory;
            PresetsFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Presets.XML";



            //Si no existe la carpeta de Youtube.dl da un aviso y cierra el programa
            if (!Directory.Exists(YoutubedlPath))
            {
                MessageBox.Show(Properties.Strings.NoYTDLExmsg, Properties.Strings.NoYTDLExtitle, MessageBoxButton.OK, MessageBoxImage.Error);
                MainPanel.IsEnabled = false;
                Environment.Exit(1);
            }



            //Si no encuentra el archivo con presets, lo crea y añade uno por defecto y lo vuelve a guardar
            if (!File.Exists(PresetsFilePath))
            {
                FileStream fs = File.Create(PresetsFilePath);
                fs.Close(); //Esto es necesario
                List<Preset> TempPreset = new List<Preset>();
                TempPreset.Add(new Preset());
                SavePresetsToFile(PresetsFilePath, TempPreset);
            }

            if (File.Exists(MainPath + "Filenames.tmp"))
            {
                File.Delete(MainPath + "Filenames.tmp");
            }

            Presets = LoadPresetsFromFile(PresetsFilePath);

            //En caso de que no haya ninguna preset cargada (Es decir, que el usuario haya decidido borrar todas las presets en el XML) Genera una por defecto
            try
            {
                LoadPreset(Presets[0]);
            }
            catch (ArgumentOutOfRangeException)
            {
                Presets.Add(new Preset());
                SavePresetsToFile(PresetsFilePath, Presets);
                LoadPreset(Presets[0]);
            }
            LoadPresetMenu(Presets);
            //TODO mirar si esto es necesario
            Seleccion_click();
            RefrescarComando();
        }
        private void Txt_URL_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            txt_URL.Text = Clipboard.GetText();
            RefrescarComando();
        }

        /*private void CultureChange(string cultureID)
        {

            System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo(cultureID);

            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;

            var oldwindow = Application.Current.MainWindow;

            Application.Current.MainWindow = new MainWindow(this.DataContext);
            Application.Current.MainWindow.Show();

            oldwindow.Close();

        }*/
        #endregion

        #region El rincon oscuro del dev
        private void GUI_DEV_TEST(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("J̶͎̞͔̙̘͍̱͒̾̾͗̄̇̆̓̚͡E̸̙̼͌̐͑͆͡J̶̴̘̦͔͕̼̲̱͂ͮ̐̉Ỏ̭̤̗̥̩̼̻̣̥͋ͧJ̱͌ͮͨ̏Ě̷̛͎̝̭̭̺̤̅̅̒ͨ̚͝J̠͉̱͇̣̍̀͊ͪ͆ͮ̚͢O̵̵̘̣͇ͣͮ͑̃̓ͪ͜", @"º̶̈́̓҉̱̮̥͖̝̯͈͖͙ª̝̥̫̘̪ͭͤ̈̈́́̅ͭ·̡̟̫̙͉̬̼͎̻͋ͪ̉́͜&̼̺̰̥̘͋̓̓ͥ̃̂͗̃͝%̛͍̲̝̬̒ͤ̀͟$̧͈̼͚̭̝͈̱̤ͨ̒̇͒͊ͤ̓͡^͔̘̤̳̼̬̅̉͋͆͝¨̩͚̺̐̾̀*̫̘ͤ̃̏̑̏̂́Çͬ͛̊͜͜͏̫̬̻͕̳̙̜;̨͖͇̫͈̤ͦ_̡͖̭̜̲͔͎̳̆͛̿ͯͮͬ̆̕+ͨ̏̄̑͆̍̅̀͏͙̪̦͚͈-ͤ̔̿ͥ҉̟͍̖̙̘̥̳̪́¡̴̙̳̻̩̣̼̠̳̫ͯͬͣ̌̊ͩͭ́'̸͕͚̗̓̒̔͝·̢̗̙̯̱͍͕̱̦̬̍̊̽͟%̖͎͉̯͔̦̹͙ͬ̀ͧ̏̌̓ͮ́$̸̬̼͚̜̰̭͖̥̊̒̏̇͛͠·͕͙̿͌͂̀͊ͬ͠^̩̪̈ͪ͘*͈̱̱̫̘̩͙̞̊ͬ͗̒ͨ͠ͅ¨̼̟͑̓ͥͦͧ̑̌͘͡͞;̴̡͓͖͈̥̮̼ͧͪ͛͛̄̑ͬ͜+̩͉̟̭ͬͭ͗̇̍̄͌ͭ?̸̷̠̖͎̦̭͆̂̅̑ͬ͗̃ͣ͠'͇̪͈͚̳̦͊ͮ͛ͤ͛̇ͭ͟¡̧̣̯̜̩̔̾ͤͩ", MessageBoxButton.YesNoCancel, MessageBoxImage.Error);
        }

        private void GUI_DEV_LoadPresetsFromFile(object sender, RoutedEventArgs e) => Presets = LoadPresetsFromFile(PresetsFilePath);

        private void GUI_DEV_SavePresetsToFile(object sender, RoutedEventArgs e) => SavePresetsToFile(PresetsFilePath, Presets);

        private void GUI_DEV_ReloadPresetsMenu(object sender, RoutedEventArgs e) => LoadPresetMenu(Presets);

        private void GUI_DEV_HideDevMode(object sender, RoutedEventArgs e) => DevMode = false;

        private void GUI_DEV_LimpiarCache(object sender, RoutedEventArgs e)
        {
            if (Youtube_dl.HasExited)
            {
                Youtube_dl.StartInfo = new ProcessStartInfo
                {
                    FileName = YoutubedlPath + "Youtube-dl.exe",
                    RedirectStandardOutput = false,
                    UseShellExecute = true,
                    Arguments = "--rm-cache-dir"
                }; //dis shit is useful bro
                Youtube_dl.Start();
                Youtube_dl.WaitForExit();
                MessageBox.Show("Cache Limpiada", "No mas errores 403!", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Activar_Log_Click(object sender, RoutedEventArgs e)
        {
            Log = !Log;
        }
        private void Activar_Verbose_Click(object sender, RoutedEventArgs e)
        {
            Verbose = !Verbose;
        }

        #endregion

        private void CambiarAEspanyol(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Idioma = "es-ES";
            MessageBox.Show("Por favor reinicia la aplicacion para aplicar los cambios","Reinicio Necesario", MessageBoxButton.OK,MessageBoxImage.Information);
        }

        private void CambiarAIngles(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Idioma = "en-US";
            MessageBox.Show("Please restart the application to apply the changes", "Restart", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}