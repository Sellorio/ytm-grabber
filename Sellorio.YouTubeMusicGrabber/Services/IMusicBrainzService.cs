using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models.MusicBrainz;

namespace Sellorio.YouTubeMusicGrabber.Services;

internal interface IMusicBrainzService
{
    Task<RecordingMatch> FindRecordingAsync(string album, IList<string> artists, IList<string> possibleTitles, DateOnly? releaseDate, int? releaseYear, int albumTrackCount, bool promptForIdIfNotFound = false);
    Task<Recording> GetRecordingByIdAsync(Guid recordingId);
    Task<Release> GetReleaseByIdAsync(Guid releaseId);
    int GetTrackNumberWithoutAdornments(string trackNumber);
}