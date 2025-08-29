using Sellorio.YouTubeMusicGrabber.Helpers;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Sellorio.YouTubeMusicGrabber.Services
{
    internal class PrerequisiteService(HttpClient httpClient) : IPrerequisiteService
    {
        // These flags will prevent you from downloading exe's twice when using the update
        // command when the exe's weren't already downloaded
        private bool _recentlyInstalledYouTubeDlp;
        private bool _recentlyInstalledFfmpeg;

        public async Task EnsureYouTubeDlpAsync(bool force = false)
        {
            if (!_recentlyInstalledYouTubeDlp && (force || !File.Exists("yt-dlp.exe")))
            {
                return;
            }

            _recentlyInstalledYouTubeDlp = true;

            ConsoleHelper.WriteLine("Downloading yt-dlp.exe\r\nThis won't take long...", ConsoleColor.DarkGray);

            using var fileStream = new FileStream("yt-dlp.exe", FileMode.Create, FileAccess.Write);
            using var downloadStream = await httpClient.GetStreamAsync("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe");
            await downloadStream.CopyToAsync(fileStream);
        }

        public async Task EnsureFfmpegAsync(bool force = false)
        {
            if (!_recentlyInstalledFfmpeg && (force || !File.Exists("ffmpeg.exe") || !File.Exists("ffprobe.exe")))
            {
                return;
            }

            _recentlyInstalledFfmpeg = true;

            ConsoleHelper.WriteLine("Downloading ffmpeg.exe and ffprobe.exe\r\nThis will take some time...", ConsoleColor.DarkGray);

            using var downloadStream = await httpClient.GetStreamAsync("https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-lgpl.zip");
            using var zip = new ZipArchive(downloadStream, ZipArchiveMode.Read, true);
            var entriesToExtract = zip.Entries.Where(x => x.Name is "ffmpeg.exe" or "ffprobe.exe").ToArray();

            foreach (var entry in entriesToExtract)
            {
                entry.ExtractToFile(entry.Name);
            }
        }
    }
}
