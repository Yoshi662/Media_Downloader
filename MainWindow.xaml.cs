﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;
using System.Windows.Controls;

namespace Media_Downloader
{
    /* Video con subtitulos https://www.youtube.com/watch?v=YU4-LFAK7t0 (Introduction to the Overwatch WorkShop)
    * Video sin subtitulos https://www.youtube.com/watch?v=-8rTfTm6JN0 (BAKURETSU BAKURESTU)
    * TODO:
    * Presets
    * 
    * Hacer que la lista de Presets sea una propiedad para que el getter, siempre las cargue de un archivo, y el setter siempre las guarde en el archivo. (Y ambas recarguen el menu)
    * Aunque siendo una lista. No se como voy a hacerlo. pero lo hare.
    * 
    * Tengo que crear todo el codigo para cargar la lista de presets desde un archivo XML
    * Y luego el siguiente que carge el menu con las opciones de los presets 
    * 
    * 
    * hacer un poco de cast en los setters de StartsAtInt y EndsAtInt
    * 
    * 
    */

    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        #region Arguments, Fields and other pseudostatic thingies
        //Version
        private readonly String CurrentVersion = "0.5.0b";

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
                if (Log) MessageBox.Show("Ahora al descargar videos, no aparecera la ventana.\nPara ver dicha informacion, " +
                      "mire el fichero de log ubicado en\n" + YoutubedlPath + "\\log.txt"
                      + "\n\nAlso tengo que hacer esto mas bonico");
            }
        }
        private bool _log = false;

        public bool IsPlaylist
        {
            get => isPlaylist; set
            {
                isPlaylist = value; OnPropertyChanged("IsPlaylist");
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
        private bool isPlaylist;

        public bool StartsAt
        {
            get => startsAt; set
            {
                startsAt = value; OnPropertyChanged("StartsAt");
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
        private bool startsAt;

        public bool EndsAt
        {
            get => endsAt; set
            {
                endsAt = value; OnPropertyChanged("EndsAt");
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
        private bool endsAt;

        public int StartsAtInt { get => startsAtInt; set { startsAtInt = value; OnPropertyChanged("StartsAtInt"); } }
        private int startsAtInt;

        public int EndsAtInt { get => endsAtInt; set { endsAtInt = value; OnPropertyChanged("EndsAtInt"); } }
        private int endsAtInt;

        public bool Quiet { get => quiet; set { quiet = value; OnPropertyChanged("Quiet"); } }
        private bool quiet;

        public bool Download_thumbnails { get => download_thumbnails; set { download_thumbnails = value; OnPropertyChanged("Download_thumbnails"); } }
        private bool download_thumbnails;

        public bool Download_subs { get => download_subs; set { download_subs = value; OnPropertyChanged("Download_subs"); } }
        private bool download_subs;

        public bool Embed_thumbnails
        {
            get => embed_thumbnails; set
            {
                embed_thumbnails = value; OnPropertyChanged("Embed_thumbnails");
                Download_thumbnails = value;
            }
        }
        private bool embed_thumbnails;

        public bool Embed_subs { get => embed_subs; set { embed_subs = value; OnPropertyChanged("Embed_subs"); } }
        private bool embed_subs;

        public bool IsVideo { get => isVideo; set { isVideo = value; OnPropertyChanged("IsVideo"); Seleccion_click(); } }
        private bool isVideo;

        public bool IsAudio { get => isAudio; set { isAudio = value; OnPropertyChanged("IsAudio"); Seleccion_click(); } }
        private bool isAudio;



        //Rutas
        private string YoutubedlPath;
        private string DownloadPath;
        private string MainPath;
        private string PresetsFilePath;

        //Extensiones
        private List<String> listaVideo = new List<String> { ".mp4", ".avi", ".mkv", ".webm" };
        private List<String> listaAudio = new List<String> { ".mp3", ".flac", ".aac", ".m4a", ".wav" };
        private String extensionSeleccionada = "";

        //Presets
        public List<Preset> Presets = new List<Preset>();

        //KKode
        private int KonamiStatus = 0;
        Key[] KonamiKode = new Key[] { Key.Up, Key.Up, Key.Down, Key.Down, Key.Left, Key.Right, Key.Left, Key.Right, Key.B, Key.A }; //START!


        //Boilerplate
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));

            //El metodo RefrescarComando escribe sobre la Propiedad Argumentos. Que tiene un OnPropertyChanged, y vuelta a empezar.
            if (!propertyName.Equals("Argumentos"))
            {
                RefrescarComando();
            }
        }
        #endregion

        #region big chungus of Logic
        public MainWindow()
        {
            YoutubedlPath = AppDomain.CurrentDomain.BaseDirectory + @"Youtube-dl\";
            DownloadPath = AppDomain.CurrentDomain.BaseDirectory + @"Descargas";
            MainPath = AppDomain.CurrentDomain.BaseDirectory;
            PresetsFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Presets.XML";
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

            //Formato
            extensionSeleccionada = cb_Formats.Text.TrimStart('.');
            if (IsAudio)
            {

                TempCmd += " -x";
                TempCmd += " --audio-format " + extensionSeleccionada;
            }
            else
            {
                TempCmd += " --recode-video " + extensionSeleccionada;
            }


            //embed - thumbs - subs                                                                                     >dubs
            if (Download_subs) TempCmd += " --all-subs";

            if (Download_thumbnails) TempCmd += " --write-thumbnail";

            if (Embed_subs) TempCmd += " --embed-subs";

            if (Embed_thumbnails) TempCmd += " --embed-thumbnail";

            //extra

            if (Quiet) TempCmd += " --quiet";

            if (Verbose) TempCmd += " --verbose";

            Argumentos = TempCmd + " -o " + "\"" + DownloadPath + @"\%(title)s.%(ext)s" + "\" \"" + txt_URL.Text + "\"";

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
            List<Preset> retorno = (List<Preset>)serializer.Deserialize(textReader);
            textReader.Close();
            return retorno;


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
            anyadir.Header = "Añadir preset";
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
                UsarMenuItem.Header = "Usar";
                UsarMenuItem.Click += PresetMenuItem_Click;

                //Create Usar como predeterminada MenuItem
                MenuItem PrederminadoMenuItem = new MenuItem();
                PrederminadoMenuItem.Header = "Usar como predeterminado";
                PrederminadoMenuItem.Click += PrederminadoMenuItem_Click;

                //Create Borrar MenuItem
                MenuItem BorrarMenuItem = new MenuItem();
                BorrarMenuItem.Header = "Borrar";
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
        /// Carga una preset en la interfaz grafica
        /// </summary>
        /// <param name="preset">Preset a cargar</param>
        private void LoadPreset(Preset preset)
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
            extensionSeleccionada = preset.Extension;
            if (preset.Media_type == (int)media_types.video)
            {
                IsVideo = true; IsAudio = false;
            }
            else
            {
                IsVideo = false; IsAudio = true;
            } //Seguro que hay una forma mas complicada _y mejor_ de hacer esto, pero esto tambien tira
        }

        private void AddPreset(object sender, RoutedEventArgs e)
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox("Introduce el nombre del preset", "No he podido hacerlo mejor, y soy demasiado vago y quiero algo funcional");
            bool Repetido = false;
            foreach (Preset preset in Presets)
            {
                if (preset.Name.Equals(name)) Repetido = true;
            }
            if (Repetido)
            {
                MessageBox.Show("Ya existe un preset con ese nombre", "Nombre repetido", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                if (!String.IsNullOrWhiteSpace(name)) { Presets.Add(SavePreset(name)); SavePresetsToFile(PresetsFilePath, Presets); LoadPresetMenu(Presets); }
            }
        }

        /// <summary>
        /// Guarda las opciones actuales dentro de una preset
        /// </summary>
        /// <param name="nombre">El nombre de la preset</param>
        /// <returns>Preset con las opciones actuales</returns>
        private Preset SavePreset(String nombre)
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
                extensionSeleccionada, //Puede que esto falle
                IsVideo ? (int)media_types.video : (int)media_types.audio
                );
            return preset;
        }

        #endregion

        #region GUI RESPONSIVENESS
        private void Seleccion_click()
        { //Seguro que hay formas mejores de hacer esto. SEGURISIMO
            if (isVideo)
            {
                cb_Formats.ItemsSource = listaVideo;
                cb_Formats.SelectedItem = listaVideo[0];
                chk_Embedsubs.IsEnabled = true;
            }
            else
            {
                cb_Formats.ItemsSource = listaAudio;
                cb_Formats.SelectedItem = listaAudio[0];
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
            //Si no existe la carpeta de Youtube.dl da un aviso y cierra el programa
            if (!Directory.Exists(YoutubedlPath))
            {
                MessageBox.Show("El programa no puede encontrar la carpeta con todos los archivos necesarios para funcionar." +
                    "\nAsegurate que la carpeta \"youtube-dl\" esta en la mismo directorio que este programa.", "Faltan Archivos", MessageBoxButton.OK, MessageBoxImage.Error);
                MainPanel.IsEnabled = false;
                Environment.Exit(1);
            }


            if (!Directory.Exists(DownloadPath)) Directory.CreateDirectory(DownloadPath);

            //Si no encuentra el archivo con presets, lo crea y añade uno por defecto
            if (!File.Exists(PresetsFilePath))
            {
                FileStream fs = File.Create(PresetsFilePath);
                fs.Close();
                List<Preset> TempPreset = new List<Preset>();
                TempPreset.Add(new Preset());
                SavePresetsToFile(PresetsFilePath, TempPreset);
            }

            Presets = LoadPresetsFromFile(PresetsFilePath);

            //TODO, hacer que una nueva preset cargue como determinada
            LoadPreset(Presets[0]);
            LoadPresetMenu(Presets);

            DevMode = true;
            RefrescarComando();
        }
        private void Txt_URL_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            txt_URL.Text = Clipboard.GetText();
            RefrescarComando();
        }
        #endregion

        private void GUI_DEV_TEST(object sender, RoutedEventArgs e)
        {
            Start();
            //Presets = LoadPresetsFromFile(PresetsFilePath);
            //LoadPresetMenu(Presets);
            //this.Menu_presets.Items.Clear();
        }
    }
}