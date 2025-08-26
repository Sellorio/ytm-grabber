# YouTube Music Grabber

A command line tool used to pull down music and metadata from YouTube. Supports:

* Pulling playlists as individual tracks or full albums
* Pulling albums (YouTube lists albums as playlists)
* Pulling individual tracks
* Pulling YouTube videos that do not have music metadata and then adding metadata either from MusicBrainz or manually

## How to build

Currently, ytm-grabber doesn't have a published binary so to use it you'll need to have Visual Studio installed.

1. Clone the repository `https://github.com/Sellorio/ytm-grabber.git`
2. Run `setup-environment.ps1` to download the latest ffmpeg and yt-dlp binaries. These will download into the folder containing the csproj file.
3. Open `Sellorio.YouTubeMusicGrabber.sln` and compile the solution (F6/Ctrl+Shift+B)
4. Copy/move the ffmpeg and yt-dlp binaries into the binaries folder (Sellorio.YouTubeMusicGrabber\bin\Debug\net9.0)
5. Follow the instructions in Setting Up Cookies.
6. Run the app! Commands and parameters are described below.

## How to run

### Commands: grab-single

Grabs a single track from YouTube.

```sh
ytm-grabber.exe grab-single https://music.youtube.com/watch?v=ABC123ABC123 --output-filename abc123.mp3 --quality High
```

#### Parameters

* _youTubeUrl_

The URL to the track or video to grab.

* _--output-filename / -o_

The where to save the downloaded mp3 file.

* _--quality / -q_

The quality of the audio file to produce. Most music on YouTube is 500kbps OPUS so there is no concern for data loss
when converting down to mp3 of a lower bitrate.

Options: Medium (196kbps), High (256kbps), VeryHigh (320kbps). Defaults to High.

### Commands: sync

Grabs multiple tracks/playlists/albums from YouTube and keeps track of what's already
been downloaded. Quality is always 256kbps.

```sh
ytm-grabber.exe sync \
    --output-path ./Music \
    --add-albums \
    --add https://music.youtube.com/playlist?list=ABC123ABC123 \
    --add https://music.youtube.com/playlist?list=ABC123ABC123
```

#### Parameters

* _--add / -a_

A YouTube Video/Music Track/Playlist/Album url to be grabbed. You can specify this parameter multiple times
to add multiple items with a single run.

* _--add-albums_

Grabs full albums instead of individual tracks.

Example 1: if you specify a single track in an `--add`, that track's entire album will be grabbed.

Example 2: if you specify a playlist in an `--add`, every album present in that playlist will be grabbed in full.

* _--output-path / -o_

The folder where music should be downloaded to. If the folder doesn't exist, it will be created. Inside that folder,
a `ytm-manifest.yml` is created which tracks what music has been added and where the file is.

Music is then downloaded into the following structure:

```
ALBUM (YEAR)\NUMBER - TITLE.mp3
```

Albums with the same name and year will instead use this structure:

```
ALBUM (YEAR) (YouTubeID)\NUMBER - TITLE.mp3
```

If albums do not have a release year specified then the following structure is used:

```
ALBUM\NUMBER - TITLE.m3
```

* _--skip / -s_

When adding a playlist using `--add`, skips the first `n` entries. This is useful when
grabbing a large playlist and having to rerun the tool due to errors/interruptions. Rechecking
which items have been downloaded still takes time.

### Setting Up Cookies

There are two scenarios that require a logged in user's cookies when using ytm-grabber:

1. The track is age-restricted and requires an age-verified account
2. The track is restricted to YouTube Premium subscribers

Thankfully, neither of these are common but if you are downloading a reasonable amount of music, you will likely run into these issues.

1. Can be solved by using a dummy account
2. Requires an active YouTube Premium account

I have used this app extensively for my own use and have not been banned yet. Even when I hit the YouTube rate limit, waiting 30 minutes
resolves this.

#### How to get the cookies.txt

1. In your browser, install an extension that lets you download your cookies for a site:

* Firefox: https://addons.mozilla.org/en-US/firefox/addon/cookies-txt/
* Chrome/Edge: https://chrome.google.com/webstore/detail/get-cookiestxt-locally/cclelndahbckbenkjhflpdbgdldlbecc
* Opera: https://addons.opera.com/en/extensions/details/edit-this-cookie

2. Open a new Private Browsing window
3. Navigate to https://music.youtube.com/ and login to the account you want to use in ytm-grabber
4. Use the extension to get the current site cookies
5. Save the cookies.exe file in the binary folder (Sellorio.YouTubeMusicGrabber\bin\Debug\net9.0)
6. Close your Private Browsing window

The reason we use a Private Browsing window is to avoid cookies being invalidated. YouTube changes the cookies
frequently while you are logged in. You can avoid this somewhat by using a Private Browsing window and closing
it after saving the cookies.

You will still need to get a new cookies.txt every few hours of use unfortunately.

## Feature Wishlist

These are things I'd like to add in future to this.

* Automatic download and updating of ffmpeg and yt-dlp binaries at runtime
* Detect cookies expiration and wait for user to get new cookies instead of erroring out
* Give user the option to provide an alternative YouTube URL for downloading unavailable tracks
* Soundcloud download options?
* Quality option for sync command?
