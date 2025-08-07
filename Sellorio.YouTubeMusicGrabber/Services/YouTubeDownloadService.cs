using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;

namespace Sellorio.YouTubeMusicGrabber.Services;

internal class YouTubeDownloadService/*(YoutubeClient youtubeClient)*/ : IYouTubeDownloadService
{
    public async Task DownloadAsMp3Async(YouTubeMetadata metadata, string outputFilename)
    {
        //var audioFormat =
        //    metadata.Formats.First(x =>
        //        x.Note.Contains("High", StringComparison.OrdinalIgnoreCase) &&
        //        x.AudioExtension == "mp4" &&
        //        x.Resolution == "audio only");

        //var videoFormat = metadata.Formats.First(x => x.VideoExtension == "mp4");

        var downloadFilenameWithoutExtension = Path.GetFileNameWithoutExtension(metadata.Filename);

        var startInfo =
            new ProcessStartInfo(
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "yt-dlp.exe"),
                $"\"https://music.youtube.com/watch?v={metadata.Id}\"")
            {
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

        var process = new Process { StartInfo = startInfo };
        process.Start();

        try
        {
            await process!.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(await process.StandardError.ReadToEndAsync());
            }

            if (File.Exists(downloadFilenameWithoutExtension + ".mp4"))
            {
                await ConvertToMp3Async(downloadFilenameWithoutExtension + ".mp4", outputFilename);
            }

            if (File.Exists(downloadFilenameWithoutExtension + ".webm"))
            {
                await ConvertToMp3Async(downloadFilenameWithoutExtension + ".webm", outputFilename);
            }
        }
        finally
        {
            if (File.Exists(downloadFilenameWithoutExtension + ".mp4"))
            {
                File.Delete(downloadFilenameWithoutExtension + ".mp4");
            }

            if (File.Exists(downloadFilenameWithoutExtension + ".webm"))
            {
                File.Delete(downloadFilenameWithoutExtension + ".webm");
            }
        }

        //var streamManifest = await youtubeClient.Videos.Streams.GetManifestAsync(metadata.Id);

        //var success =
        //    await TryDownloadAudioStream(streamManifest, outputFilename) ||
        //    await TryDownloadVideoStreamAndConvertToMp3(streamManifest, outputFilename);

        //if (!success)
        //{
        //    throw new InvalidOperationException("Unable to find an audio stream.");
        //}
    }

    //private async Task<bool> TryDownloadAudioStream(StreamManifest streamManifest, string outputFilename)
    //{
    //    var streamInfo = streamManifest.GetAudioOnlyStreams().TryGetWithHighestBitrate();

    //    if (streamInfo == null)
    //    {
    //        return false;
    //    }

    //    if (streamInfo.Size.MegaBytes > 20)
    //    {
    //        throw new InvalidOperationException("Audio is too large to be accepted.");
    //    }

    //    var stream = await youtubeClient.Videos.Streams.GetAsync(streamInfo);

    //    using var fileStream = File.Create(outputFilename);
    //    await stream.CopyToAsync(fileStream);

    //    return true;
    //}

    //private async Task<bool> TryDownloadVideoStreamAndConvertToMp3(StreamManifest streamManifest, string outputFilename)
    //{
    //    var streamInfo = streamManifest.GetMuxedStreams().TryGetWithHighestBitrate();

    //    if (streamInfo == null)
    //    {
    //        return false;
    //    }

    //    var videoFilename = Path.GetTempFileName();

    //    try
    //    {
    //        if (streamInfo.Size.MegaBytes > 500)
    //        {
    //            throw new InvalidOperationException("Video is too large to be accepted.");
    //        }

    //        var stream = await youtubeClient.Videos.Streams.GetAsync(streamInfo);

    //        using (var fileStream = File.Create(videoFilename))
    //        {
    //            await stream.CopyToAsync(fileStream);
    //        }

    //        await ConvertMp4ToMp3Async(videoFilename, outputFilename);
    //    }
    //    finally
    //    {
    //        File.Delete(videoFilename);
    //    }

    //    return true;
    //}

    private async Task ConvertToMp3Async(string source, string destination)
    {
        //FFmpeg.SetExecutablesPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        //var conversion = await FFmpeg.Conversions.FromSnippet.ExtractAudio(source, destination);
        //await conversion.Start();

        var startInfo =
            new ProcessStartInfo(
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "ffmpeg.exe"),
                $"-y -i \"{source}\" -id3v2_version 3 -write_id3v1 1 \"{destination}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

        var process = Process.Start(startInfo);

        // make sure the output buffer doesn't fill and block the process
        _ = process.StandardOutput.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(await process.StandardError.ReadToEndAsync());
        }
    }
}
