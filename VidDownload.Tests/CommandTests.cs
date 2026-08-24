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
    }
}
