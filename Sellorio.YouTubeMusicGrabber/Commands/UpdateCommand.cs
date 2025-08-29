using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Sellorio.YouTubeMusicGrabber.Services;
using System;
using System.Threading.Tasks;

namespace Sellorio.YouTubeMusicGrabber.Commands
{
    [Verb("update", HelpText = "Redownloads the latest yt-dlp and ffmpeg exe's.")]
    internal class UpdateCommand : ICommand
    {
        public async Task ExecuteAsync(IServiceProvider serviceProvider)
        {
            var prerequisiteService = serviceProvider.GetRequiredService<IPrerequisiteService>();

            await prerequisiteService.EnsureYouTubeDlpAsync(force: true);
            await prerequisiteService.EnsureFfmpegAsync(force: true);
        }
    }
}
