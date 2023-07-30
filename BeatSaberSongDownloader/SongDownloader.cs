using HtmlAgilityPack;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static BeatSaberSongDownloader.BSaberSearchFilterEnums;

namespace BeatSaberSongDownloader
{
    internal class SongDownloader
    {
        private readonly Uri _bsaberBaseUri = new Uri("https://bsaber.com/");
        private IBeatSaberCustomMapFolder _customMapFolder;
        private DirectoryInfo[] _mapInstalled;
        private static object _blocker = new();


        public SongDownloader(IBeatSaberCustomMapFolder cmFolder)
        {
            this._customMapFolder = cmFolder;

            if (_customMapFolder.CustomMapFolder is null)
                throw new Exception($"CustomMapFolder has to be set first in {_customMapFolder.GetType().Name}\n" +
                    $"Please use SetCutomMapFolder() method of class {_customMapFolder.GetType().Name} first.");

            _mapInstalled = _customMapFolder.CustomMapFolder.GetDirectories();
        }

        public async Task DownloadAllSongsAsync(Time time, Genre genre, bool overwriteExistingMaps)
        {
            var articles = await GetArticleOfAllPagesAsync(time, genre);
            if(articles.Count > 0)
            {
                var songResponseList = await DownloadSongs(articles, overwriteExistingMaps);
                await ExtractSongs(songResponseList);
            }
        }

        private async Task ExtractSongs(List<SongResponse> songRespsoneList)
        {
            var options = new ParallelOptions();
            options.MaxDegreeOfParallelism = 10;

            uint position = 1;
            await Parallel.ForEachAsync(songRespsoneList, options, (song, token) =>
            {
                uint tempPosition;
                lock (_blocker)
                    tempPosition = position++;

                CustomLogger.InfoCustomLines(tempPosition, $"Start Extracting {song.DirectoryName}");

                string songFolder = string.Empty;
                if(_customMapFolder.CustomMapFolder is not null)
                    songFolder = Path.Combine(_customMapFolder.CustomMapFolder.FullName, song.DirectoryName);

                if (!Directory.Exists(songFolder))
                    Directory.CreateDirectory(songFolder);

                lock (_blocker)
                    using (var zip = ZipFile.Read(new MemoryStream(song.SongData)))
                    {
                        zip.ExtractAll(songFolder, ExtractExistingFileAction.OverwriteSilently);
                    }

                CustomLogger.InfoCustomLines(tempPosition, $"Done Extracting {song.DirectoryName}");
                return ValueTask.CompletedTask;
            });
            CustomLogger.ResetCustomLines();
        }

        private async Task<List<SongResponse>> DownloadSongs(List<HtmlNode> articles, bool overwriteExistingMaps)
        {
            var songResponseList = new List<SongResponse>();

            CustomLogger.ResetCustomLines();

            var options = new ParallelOptions();
            //options.MaxDegreeOfParallelism = 10;

            uint songNumber = 1;
            await Parallel.ForEachAsync(articles, options, async (article, token) =>
            {
                uint copySongNumber;
                lock (_blocker)
                {
                    copySongNumber = songNumber++;
                }
                CustomLogger.InfoCustomLines(copySongNumber, $"Downloading Song {copySongNumber}");

                var songAPIUri = GetSongDownloadUri(article);
                var songAPIID = GetSongAPIID(songAPIUri);

                // Check Song Already Downloaded
                lock (_blocker)
                    if (!overwriteExistingMaps && CheckMapAlreadyExist(songAPIID))
                    {
                        CustomLogger.InfoCustomLines(copySongNumber, $"Song {copySongNumber} already Downloaded. Skip Download.");
                        return;
                    }                        
                
                var songResponse = await GetSongData(songAPIUri);
                if(songResponse is not null)
                {
                    lock (_blocker)
                    {
                        songResponseList.Add(songResponse);
                    }
                    CustomLogger.InfoCustomLines(copySongNumber, $"Song {copySongNumber} Downloaded!");
                }
                else
                {
                    CustomLogger.ErrorCustomLines(copySongNumber, $"Song {copySongNumber} coult not be Downloaded!");
                }
            });
            CustomLogger.ResetCustomLines();
            return songResponseList;
        }

        private string GetSongAPIID(Uri songAPIUri)
        {
            var index = songAPIUri.AbsoluteUri.LastIndexOf("/") + 1;
            return songAPIUri.AbsoluteUri.Substring(index, songAPIUri.AbsoluteUri.Length - index);
        }

        private async Task<SongResponse?> GetSongData(Uri apiUri)
        {
            var songResponse = new SongResponse();

            using (var client = new HttpClient())
            {
                try
                {
                    var httpResponse = await client.GetAsync(apiUri);
                    songResponse.DirectoryName = GetSongDirectoryName(httpResponse);
                    if (songResponse.DirectoryName == "\"\"" || songResponse.DirectoryName == string.Empty)
                        return null;
                    songResponse.SongData = await httpResponse.Content.ReadAsByteArrayAsync();
                }
                catch (Exception ex)
                {
                    await CustomLogger.ErrorWriteLineAsync("[Error] Download Failed!\n" + ex);
                    return null;
                }
                return songResponse;
            }
        }

        private string GetSongDirectoryName(HttpResponseMessage response)
        {
            var fileName = response.Content.Headers.ContentDisposition?.FileName?.Replace(".zip", "");
            if (fileName is not null)
                return fileName.Substring(1, fileName.Count() - 2);
            else
                return "";
        }

        private Uri GetSongDownloadUri(HtmlNode article)
            => new Uri(article.SelectSingleNode(".//a[contains(@class, '-download-zip')]").GetAttributeValue("href", ""));

        public async Task<List<HtmlNode>> GetArticleOfAllPagesAsync(Time time, Genre genre)
        {
            await CustomLogger.InfoWriteLineAsync($"Start searching Songs.");

            var listArticles = new List<HtmlNode>();
            var listTasks = new List<Task>();

            uint page = 1;

            var domForPages = await GetDom(page, time, genre);            
            var pages = GetPageCount(domForPages);
            var songsFound = 0;
            await CustomLogger.InfoWriteLineAsync($"Pages found {pages}.");

            for (; page <= pages; page++)
            {
                var task = await Task.Factory.StartNew(async () =>
                {
                    var copyPage = page; // storepage one. this is needed to check if a song exist on page one

                    var dom = await GetDom(page, time, genre);
                    var pageArticles = GetArticlesFromPage(dom);

                    if(copyPage == 1 && pageArticles is null)
                        await CustomLogger.InfoWriteAsync("Songs Found: 0");

                    if (pageArticles is not null)
                    {
                        await Parallel.ForEachAsync(pageArticles, (article, token) =>
                        {
                            lock (_blocker)
                            {
                                songsFound += 1;
                                CustomLogger.InfoWriteAsync("Songs Found: " + songsFound.ToString(), 0).Wait();

                                listArticles.Add(article);
                            }
                            return ValueTask.CompletedTask;
                        });
                    }
                });
                listTasks.Add(task);
            }
            Task.WaitAll(listTasks.ToArray());

            await Console.Out.WriteLineAsync(); // Continue on Next Line
            return listArticles;
        }

        private uint GetPageCount(HtmlDocument dom)
        {
            var nodes = dom.DocumentNode.SelectNodes("//a[@class='page-numbers']");
            if (nodes is not null)
                return Convert.ToUInt32(nodes.Last().InnerHtml);
            else
                return 1;
        }

        private HtmlNodeCollection? GetArticlesFromPage(HtmlDocument dom) => dom.DocumentNode.SelectNodes("//article");

        private bool CheckMapAlreadyExist(string songApiID) => 
            _mapInstalled.FirstOrDefault(i => i.Name.Contains(songApiID)) != null;            

        private async Task<HtmlDocument> GetDom(uint page, Time time, Genre genre) =>
            await new HtmlWeb().LoadFromWebAsync(
                $"{_bsaberBaseUri.AbsoluteUri}songs/top/page/{page}/" +
                $"?time={time.GetAttribute<CustomNameAttribute>().Name}" +
                $"&genre={genre.GetAttribute<CustomNameAttribute>().Name}");


    }
}
