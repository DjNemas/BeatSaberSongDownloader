using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberSongDownloader
{
    internal interface IBeatSaberCustomMapFolder
    {
        abstract DirectoryInfo? CustomMapFolder { get; }

        public void SetCutomMapFolder(DirectoryInfo dir);
    }
}
