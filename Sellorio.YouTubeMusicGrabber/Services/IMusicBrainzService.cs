using System;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models.MusicBrainz;

namespace Sellorio.YouTubeMusicGrabber.Services;

internal interface IMusicBrainzService
{
    Task<RecordingMatch> FindRecordingAsync(string album, string[] artists, string[] possibleTitles, DateOnly? releaseDate, int? releaseYear, int albumTrackCount, bool promptForIdIfNotFound = false);
}