using System;
using VidDownload.WPF.Services;
using Xunit;

namespace VidDownload.Tests
{
    public class FFmpegStampTests
    {
        private static string Stamp(string tag, string asset, int year, int month, int day) =>
            FFmpegService.BuildStamp(tag, asset, new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc));

        [Fact]
        public void EmptyStoredStamp_MeansUpdateAvailable()
        {
            Assert.True(FFmpegService.IsNewerBuild(string.Empty, Stamp("latest", "a.zip", 2026, 11, 1)));
        }

        [Fact]
        public void LegacyTagOnlyStamp_TriggersOneReinstall()
        {
            Assert.True(FFmpegService.IsNewerBuild("latest", Stamp("latest", "a.zip", 2026, 11, 1)));
        }

        [Fact]
        public void SameBuildWithinStaleAge_IsNotUpdate()
        {
            string installed = Stamp("latest", "a.zip", 2026, 11, 1);
            string current = Stamp("latest", "a.zip", 2026, 11, 20);

            Assert.False(FFmpegService.IsNewerBuild(installed, current));
        }

        [Fact]
        public void SameBuildOlderThanStaleAge_IsUpdate()
        {
            string installed = Stamp("latest", "a.zip", 2026, 9, 1);
            string current = Stamp("latest", "a.zip", 2026, 11, 20);

            Assert.True(FFmpegService.IsNewerBuild(installed, current));
        }

        [Fact]
        public void DifferentAssetName_IsUpdate()
        {
            string installed = Stamp("latest", "ffmpeg-n7.1.zip", 2026, 11, 1);
            string current = Stamp("latest", "ffmpeg-n8.0.zip", 2026, 11, 2);

            Assert.True(FFmpegService.IsNewerBuild(installed, current));
        }
    }
}
