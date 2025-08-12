using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Sellorio.YouTubeMusicGrabber.Services;

namespace Sellorio.YouTubeMusicGrabber.Commands;

[Verb("sync", HelpText = "Maintains a music library using \"--add\" to add songs (can be repeated). When removing music, simply delete the files. A \"ytm-manifest.yml\" is used to maintain local metadata.")]
internal class SyncCommand : ICommand
{
    [Option('o', "output-path", Required = true, HelpText = "The root path of the music library. This will contain the manifest and music will be in Album/Track folder structure inside.")]
    public string OutputPath { get; set; }

    [Option("add-albums", HelpText = "Adds full albums instead of just getting the track. For playlists, retrieves all albums instead of all tracks directly added to the playlist.")]
    public bool AddAlbums { get; set; }

    [Option('a', "add", HelpText = "Downloads and adds the given youtube url to the manifest. Can be repeated.")]
    public IEnumerable<string> AddUris { get; set; }

    public async Task ExecuteAsync(IServiceProvider serviceProvider)
    {
        if (!Directory.Exists(OutputPath))
        {
            Directory.CreateDirectory(OutputPath);
        }

        var syncService = serviceProvider.GetRequiredService<ISyncService>();

        var manifest = await syncService.ReadOrCreateManifestAsync(OutputPath);

        await syncService.CleanUpManifestAsync(OutputPath, manifest);

        foreach (var uri in AddUris)
        {
            await syncService.ProcessAddAsync(OutputPath, manifest, uri, AddAlbums);
        }
    }
}
