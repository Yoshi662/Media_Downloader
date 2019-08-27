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
        private bool isPlaylist;
        private bool startsAt;
        private bool endsAt;
        private int startsAtInt;
        private int endsAtInt;
        private bool quiet;
        private bool download_thumbnails;
        private bool download_subs;
        private bool embed_thumb;
        private bool embed_subs;
        private string extension;
        private int media_type;

        public bool IsPlaylist { get => isPlaylist; set => isPlaylist = value; }
        public bool StartsAt { get => startsAt; set => startsAt = value; }
        public bool EndsAt { get => endsAt; set => endsAt = value; }
        public int StartsAtInt { get => startsAtInt; set => startsAtInt = value; }
        public int EndsAtInt { get => endsAtInt; set => endsAtInt = value; }
        public bool Quiet { get => quiet; set => quiet = value; }
        public bool Download_thumbnails { get => download_thumbnails; set => download_thumbnails = value; }
        public bool Download_subs { get => download_subs; set => download_subs = value; }
        public bool Embed_thumb { get => embed_thumb; set => embed_thumb = value; }
        public bool Embed_subs { get => embed_subs; set => embed_subs = value; }
        public string Extension { get => extension; set => extension = value; }
        public int Media_type { get => media_type; set => media_type = value; }

        public Preset(bool isPlaylist, bool startsAt, bool endsAt, int startsAtInt, int endsAtInt, bool quiet, bool download_thumbnails, bool download_subs, bool embed_thumb, bool embed_subs, string extension, int media_type)
        {
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
        }
    }
    enum media_types : int
    {
        video, audio
    };
}
