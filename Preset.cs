using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media_Downloader
{
    [Serializable]
    public class Preset
    {
        private string _name;
        private bool _isPlaylist;
        private bool _startsAt;
        private bool _endsAt;
        private int _startsAtInt;
        private int _endsAtInt;
        private bool _quiet;
        private bool _download_thumbnails;
        private bool _download_subs;
        private bool _embed_thumb;
        private bool _embed_subs;
        private string _extension;
        private int _media_type;
        private string _download_path;
        private bool _devMode;

        public string Name { get => _name; set => _name = value; }
        public bool IsPlaylist { get => _isPlaylist; set => _isPlaylist = value; }
        public bool StartsAt { get => _startsAt; set => _startsAt = value; }
        public bool EndsAt { get => _endsAt; set => _endsAt = value; }
        public int StartsAtInt { get => _startsAtInt; set => _startsAtInt = value; }
        public int EndsAtInt { get => _endsAtInt; set => _endsAtInt = value; }
        public bool Quiet { get => _quiet; set => _quiet = value; }
        public bool Download_thumbnails { get => _download_thumbnails; set => _download_thumbnails = value; }
        public bool Download_subs { get => _download_subs; set => _download_subs = value; }
        public bool Embed_thumb { get => _embed_thumb; set => _embed_thumb = value; }
        public bool Embed_subs { get => _embed_subs; set => _embed_subs = value; }
        public string Extension { get => _extension; set => _extension = value; }
        public int Media_type { get => _media_type; set => _media_type = value; }
        public string DownloadPath { get => _download_path; set => _download_path = value; }
        public bool DevMode { get => _devMode; set => _devMode = value; }

        public Preset(string name ,bool isPlaylist, bool startsAt, bool endsAt, int startsAtInt, int endsAtInt, bool quiet, bool download_thumbnails, bool download_subs, bool embed_thumb, bool embed_subs, string extension,string download_path, int media_type, bool devMode)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            IsPlaylist = isPlaylist;
            StartsAt = startsAt;
            EndsAt = endsAt;
            StartsAtInt = startsAtInt;
            EndsAtInt = endsAtInt;
            Quiet = quiet;
            Download_thumbnails = download_thumbnails;
            Download_subs = download_subs;
            Embed_thumb = embed_thumb;
            Embed_subs = embed_subs;
            Extension = extension ?? throw new ArgumentNullException(nameof(extension));
            Media_type = media_type;
            DownloadPath = download_path;
            DevMode = devMode;
        }
        /// <summary>
        /// Creates a default Preset
        /// </summary>
        public Preset()
        {
            Name = "Default";
            IsPlaylist = false;
            StartsAt = false;
            EndsAt = false;
            StartsAtInt = 0;
            EndsAtInt = 0;
            Quiet = false;
            Download_thumbnails = false;
            Download_subs = false;
            Embed_thumb = false;
            Embed_subs = false;
            Extension = "mp4";
            DownloadPath = AppDomain.CurrentDomain.BaseDirectory + @"Descargas";
            Media_type = (int)media_types.video;
            DevMode = false;
        }
    }
    public enum media_types : int
    {
        video, audio
    };
}
