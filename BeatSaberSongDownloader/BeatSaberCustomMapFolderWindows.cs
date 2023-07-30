using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberSongDownloader
{
    [SupportedOSPlatform("windows")]
    internal class BeatSaberCustomMapFolderWindows : IBeatSaberCustomMapFolder
    {
        private readonly string steamBSRegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 620980";
        private readonly string steamBSRegistryValue = "InstallLocation";

        public DirectoryInfo? CustomMapFolder { get; private set; }

        public BeatSaberCustomMapFolderWindows()
        {
            string? installFolder = Registry.GetValue(steamBSRegistryPath, steamBSRegistryValue, RegistryValueKind.String) as string;
            if (installFolder is not null)
            {
                CustomMapFolder = new DirectoryInfo(Path.Combine(installFolder, "Beat Saber_Data", "CustomLevels"));
            }
            else
                CustomMapFolder = null;
        }

        public void SetCutomMapFolder(DirectoryInfo dir) => CustomMapFolder = dir;
    }
}
