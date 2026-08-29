using System.Linq;
using VidDownload.WPF.Control;
using Xunit;

namespace VidDownload.Tests
{
    public class CommandTests
    {
        private static Settings BaseSettings() => new()
        {
            Resolution = "1080",
            VideoCodec = "av01",
            AudioCodec = "aac",
            Format = "mp4",
            SavePath = @"C:\videos"
        };

        [Fact]
        public void LoadVideo_ContainsRemuxSortAndPath()
        {
            var args = Command.LoadVideo("https://youtu.be/x", BaseSettings(), isPlaylist: false, isCheckCoder: false);

            Assert.Contains("--remux-video", args);
            Assert.Contains("mp4", args);
            Assert.Contains("-S", args);
            Assert.Contains("+codec:av01,res:1080,fps", args);
            Assert.Contains("-P", args);
            Assert.Contains(@"C:\videos", args);
            Assert.Equal("https://youtu.be/x", args.Last());
            Assert.DoesNotContain("--limit-rate", args);
        }

        [Fact]
        public void LoadVideo_WithRateLimit_AddsLimitRate()
        {
            var settings = BaseSettings();
            settings.RateLimit = "5M";

            var args = Command.LoadVideo("url", settings, isPlaylist: false, isCheckCoder: false);

            int index = args.IndexOf("--limit-rate");
            Assert.True(index >= 0);
            Assert.Equal("5M", args[index + 1]);
        }

        [Fact]
        public void LoadVideo_ReEncode_UsesRecodeVideo()
        {
            var args = Command.LoadVideo("url", BaseSettings(), isPlaylist: false, isCheckCoder: true);

            Assert.Contains("--recode-video", args);
            Assert.DoesNotContain("--remux-video", args);
        }

        [Fact]
        public void LoadVideo_Playlist_UsesOutputTemplate()
        {
            var args = Command.LoadVideo("url", BaseSettings(), isPlaylist: true, isCheckCoder: false);

            Assert.Contains("-o", args);
            Assert.Contains(@"C:\videos/%(playlist)s/%(playlist_index)s- %(title)s.%(ext)s", args);
            Assert.DoesNotContain("-P", args);
        }

        [Fact]
        public void LoadVideo_Playlist_AddsYesPlaylistFlag()
        {
            var args = Command.LoadVideo("url", BaseSettings(), isPlaylist: true, isCheckCoder: false);

            Assert.Contains("--yes-playlist", args);
            Assert.DoesNotContain("--no-playlist", args);
        }

        [Fact]
        public void LoadVideo_Single_AddsNoPlaylistFlag()
        {
            var args = Command.LoadVideo("url", BaseSettings(), isPlaylist: false, isCheckCoder: false);

            Assert.Contains("--no-playlist", args);
            Assert.DoesNotContain("--yes-playlist", args);
        }

        [Fact]
        public void LoadAudio_PlaylistFlags()
        {
            var playlistArgs = Command.LoadAudio(BaseSettings(), "url", isPlaylist: true);
            Assert.Contains("--yes-playlist", playlistArgs);

            var singleArgs = Command.LoadAudio(BaseSettings(), "url", isPlaylist: false);
            Assert.Contains("--no-playlist", singleArgs);
        }

        [Fact]
        public void LoadAudio_ContainsExtractAndFormat()
        {
            var args = Command.LoadAudio(BaseSettings(), "url", isPlaylist: false);

            Assert.Contains("-x", args);
            Assert.Contains("--audio-format", args);
            Assert.Contains("aac", args);
            Assert.Contains("url", args);
        }

        [Fact]
        public void LoadAudio_WithRateLimit_AddsLimitRate()
        {
            var settings = BaseSettings();
            settings.RateLimit = "500K";

            var args = Command.LoadAudio(settings, "url", isPlaylist: false);

            int index = args.IndexOf("--limit-rate");
            Assert.True(index >= 0);
            Assert.Equal("500K", args[index + 1]);
        }

        [Fact]
        public void LoadVideo_WithSubtitles_AddsSubArgs()
        {
            var settings = BaseSettings();
            settings.DownloadSubtitles = true;
            settings.SubtitleLanguage = "en";
            settings.EmbedSubtitles = true;

            var args = Command.LoadVideo("url", settings, isPlaylist: false, isCheckCoder: false);

            Assert.Contains("--write-subs", args);
            Assert.Contains("--sub-langs", args);
            Assert.Contains("en", args);
            Assert.Contains("--embed-subs", args);
        }

        // ==== Новые аргументы v0.11 ====

        [Fact]
        public void LoadVideo_WithFormatSelector_UsesFormatFlagInsteadOfSort()
        {
            var settings = BaseSettings();
            settings.FormatSelector = "137+ba";

            var args = Command.LoadVideo("url", settings, isPlaylist: false, isCheckCoder: false);

            int index = args.IndexOf("-f");
            Assert.True(index >= 0);
            Assert.Equal("137+ba", args[index + 1]);
            Assert.DoesNotContain("-S", args);
        }

        [Fact]
        public void LoadVideo_PlaylistItems_AddedOnlyForPlaylist()
        {
            var settings = BaseSettings();
            settings.PlaylistItems = "1-3,7";

            var playlistArgs = Command.LoadVideo("url", settings, isPlaylist: true, isCheckCoder: false);
            var singleArgs = Command.LoadVideo("url", settings, isPlaylist: false, isCheckCoder: false);

            int index = playlistArgs.IndexOf("--playlist-items");
            Assert.True(index >= 0);
            Assert.Equal("1-3,7", playlistArgs[index + 1]);
            Assert.DoesNotContain("--playlist-items", singleArgs);
        }

        [Fact]
        public void LoadVideo_CookiesFromBrowser_AndCookiesFile_AreMutuallyExclusive()
        {
            var browser = BaseSettings();
            browser.CookiesFromBrowser = "chrome";
            var file = BaseSettings();
            file.CookiesFile = @"C:\cookies.txt";

            var browserArgs = Command.LoadVideo("url", browser, isPlaylist: false, isCheckCoder: false);
            var fileArgs = Command.LoadVideo("url", file, isPlaylist: false, isCheckCoder: false);

            Assert.Equal("chrome", browserArgs[browserArgs.IndexOf("--cookies-from-browser") + 1]);
            Assert.DoesNotContain("--cookies", browserArgs);
            int index = fileArgs.IndexOf("--cookies");
            Assert.True(index >= 0);
            Assert.Equal(@"C:\cookies.txt", fileArgs[index + 1]);
            Assert.DoesNotContain("--cookies-from-browser", fileArgs);
        }

        [Fact]
        public void LoadVideo_ProxyRetriesArchiveAndSections()
        {
            var settings = BaseSettings();
            settings.Proxy = "socks5://127.0.0.1:1080";
            settings.Retries = 5;
            settings.UseDownloadArchive = true;
            settings.DownloadSections = "*00:01:30-00:05:00";

            var args = Command.LoadVideo("url", settings, isPlaylist: false, isCheckCoder: false);

            Assert.Equal("socks5://127.0.0.1:1080", args[args.IndexOf("--proxy") + 1]);
            Assert.Equal("5", args[args.IndexOf("--retries") + 1]);
            Assert.Equal("5", args[args.IndexOf("--fragment-retries") + 1]);
            Assert.Equal(System.IO.Path.Combine(@"C:\videos", "downloaded.txt"), args[args.IndexOf("--download-archive") + 1]);
            Assert.Equal("*00:01:30-00:05:00", args[args.IndexOf("--download-sections") + 1]);
        }

        [Fact]
        public void LoadVideo_EmbedThumbnailAndMetadata()
        {
            var settings = BaseSettings();
            settings.EmbedThumbnail = true;
            settings.EmbedMetadata = true;

            var args = Command.LoadVideo("url", settings, isPlaylist: false, isCheckCoder: false);

            Assert.Contains("--embed-thumbnail", args);
            Assert.Contains("--embed-metadata", args);
        }

        [Fact]
        public void LoadVideo_SubtitlesWithSrtConversion()
        {
            var settings = BaseSettings();
            settings.DownloadSubtitles = true;
            settings.SubtitleLanguage = "en";
            settings.ConvertSubsToSrt = true;

            var args = Command.LoadVideo("url", settings, isPlaylist: false, isCheckCoder: false);

            Assert.Contains("--write-subs", args);
            int index = args.IndexOf("--convert-subs");
            Assert.True(index >= 0);
            Assert.Equal("srt", args[index + 1]);
        }

        [Fact]
        public void LoadAudio_AudioQuality()
        {
            var settings = BaseSettings();
            settings.AudioQuality = "0";

            var args = Command.LoadAudio(settings, "url", isPlaylist: false);

            int index = args.IndexOf("--audio-quality");
            Assert.True(index >= 0);
            Assert.Equal("0", args[index + 1]);
        }

        [Fact]
        public void FetchInfo_SingleAndPlaylist()
        {
            var single = Command.FetchInfo("url", isPlaylist: false);
            var playlist = Command.FetchInfo("url", isPlaylist: true);

            Assert.Contains("-J", single);
            Assert.Contains("--no-playlist", single);
            Assert.DoesNotContain("--flat-playlist", single);
            Assert.Equal("url", single.Last());

            Assert.Contains("--yes-playlist", playlist);
            Assert.Contains("--flat-playlist", playlist);
            Assert.DoesNotContain("--no-playlist", playlist);
        }
    }
}
