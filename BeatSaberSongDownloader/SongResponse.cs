using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberSongDownloader
{
    internal class SongResponse
    {
        public string DirectoryName { get; set; } = string.Empty;
        public byte[] SongData { get; set; } = new byte[0];
    }
}
