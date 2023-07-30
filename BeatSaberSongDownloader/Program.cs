using System.Runtime.InteropServices;
using static BeatSaberSongDownloader.BSaberSearchFilterEnums;

namespace BeatSaberSongDownloader
{
    public class Program
    {
        private static readonly Time _time = Time.All;
        private static readonly Genre _genre = Genre.Vocaloid;

        public static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

        private static async Task MainAsync(string[] args)
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var bsCustomMapFolder = new BeatSaberCustomMapFolderWindows();
                if(bsCustomMapFolder.CustomMapFolder is null)
                {
                    // Get folder first from User
                }
                var downloader = new SongDownloader(bsCustomMapFolder);
                await downloader.DownloadAllSongsAsync(_time, _genre, false);

                await CustomLogger.InfoWriteAsync("Done!");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("This application only works on Windows OS.\nPress Any Key to exit...");
                Console.ReadKey();
                Environment.Exit(1);
            }
        }
    }
}