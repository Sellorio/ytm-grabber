# Performance issue when reporting progress for downloads in powershell
$ProgressPreference = 'SilentlyContinue'

# Download yt-dlp
Invoke-WebRequest -Uri "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe" -OutFile "$PSScriptRoot\Sellorio.YouTubeMusicGrabber\yt-dlp.exe"

# Download FFmpeg static build (Windows 64-bit)
$ffmpegZip = "$PSScriptRoot\ffmpeg_temp.zip"
Invoke-WebRequest -Uri "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-lgpl.zip" -OutFile $ffmpegZip

# Extract ffmpeg.exe and ffprobe.exe only
$extractPath = "$PSScriptRoot\ffmpeg_temp"
Expand-Archive -Path $ffmpegZip -DestinationPath $extractPath

# Find and copy ffmpeg.exe and ffprobe.exe to current directory
$binPath = Get-ChildItem "$extractPath\*\bin" -Directory | Select-Object -First 1
Copy-Item "$binPath\ffmpeg.exe" -Destination "$PSScriptRoot\Sellorio.YouTubeMusicGrabber\"
Copy-Item "$binPath\ffprobe.exe" -Destination "$PSScriptRoot\Sellorio.YouTubeMusicGrabber\"

# Cleanup
Remove-Item $ffmpegZip -Force
Remove-Item $extractPath -Recurse -Force
