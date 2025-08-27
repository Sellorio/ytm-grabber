using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Sellorio.YouTubeMusicGrabber.Helpers;
internal static class FfmpegHelper
{
    public static async Task ConvertToMp3Async(string source, string destination, int outputBitrateKbps, bool loudnessNormalization)
    {
        // According to ChatGPT loudnorm should be -14 and TP should be -1 but for my test track these numbers felt more accurate
        var filters = loudnessNormalization ? "-af \"loudnorm=I=-12:TP=0:LRA=11\"" : "";
        var tagsFormat = "-id3v2_version 3 -write_id3v1 1";
        var bitrate = $"-ab {outputBitrateKbps}k";

        var startInfo =
            new ProcessStartInfo(
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "ffmpeg.exe"),
                $"-y -i \"{source}\" {filters} {tagsFormat} {bitrate} \"{destination}\"")
            {
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

        var process = Process.Start(startInfo);

        // make sure the output buffer doesn't fill and block the process
        var errorOutputTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(await errorOutputTask);
        }
    }
}
