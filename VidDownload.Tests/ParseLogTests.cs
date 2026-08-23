using VidDownload.WPF.Control;
using VidDownload.WPF.Services;
using Xunit;

namespace VidDownload.Tests
{
    public class ParseLogTests
    {
        [Fact]
        public void StandardLine_ParsesAllFields()
        {
            var progress = ParseLog.ParseProgressLine(
                "[download]  42.7% of ~10.55MiB at  2.50MiB/s ETA 00:12:34");

            Assert.Equal(43, progress.Percent);
            Assert.Equal("10.55 MiB", progress.TotalSize);
            Assert.Equal("2.50 MiB/s", progress.Speed);
            Assert.Equal("00:12:34", progress.Eta);
            Assert.Null(progress.DestinationPath);
        }

        [Fact]
        public void UnknownSpeedAndEta_KeepsPreviousValues()
        {
            var previous = new DownloadProgress
            {
                Percent = 37,
                Speed = "5.00 MiB/s",
                Eta = "01:00:00",
                TotalSize = "9.99 MiB"
            };

            var progress = ParseLog.ParseProgressLine(
                "[download]  50.0% of 10.00MiB at Unknown B/s ETA Unknown", previous);

            Assert.Equal(50, progress.Percent);
            Assert.Equal("10.00 MiB", progress.TotalSize);
            Assert.Equal("5.00 MiB/s", progress.Speed);
            Assert.Equal("01:00:00", progress.Eta);
        }

        [Fact]
        public void FinalLineWithIn_ParsesPercentAndSize()
        {
            var progress = ParseLog.ParseProgressLine(
                "[download] 100% of 10.00MiB in 00:00:10 at 2.50MiB/s");

            Assert.Equal(100, progress.Percent);
            Assert.Equal("10.00 MiB", progress.TotalSize);
        }

        [Fact]
        public void NoPercentLine_KeepsPreviousPercent_ParsesSpeedAndSize()
        {
            var previous = new DownloadProgress { Percent = 37 };

            var progress = ParseLog.ParseProgressLine(
                "[download]   1.50MiB at 2.00MiB/s ETA 00:00:05", previous);

            Assert.Equal(37, progress.Percent);
            Assert.Equal("1.50 MiB", progress.TotalSize);
            Assert.Equal("2.00 MiB/s", progress.Speed);
            Assert.Equal("00:00:05", progress.Eta);
        }

        [Fact]
        public void DestinationLine_PreservesPercent_AndSetsPath()
        {
            var previous = new DownloadProgress { Percent = 82, Speed = "3.00 MiB/s" };

            var progress = ParseLog.ParseProgressLine(
                "[download] Destination: C:\\videos\\cool clip.mp4", previous);

            Assert.Equal(82, progress.Percent);
            Assert.Equal("3.00 MiB/s", progress.Speed);
            Assert.Equal(@"C:\videos\cool clip.mp4", progress.DestinationPath);
        }

        [Fact]
        public void MergerLine_SetsDestinationPath()
        {
            var progress = ParseLog.ParseProgressLine(
                "[Merger] Merging formats into \"C:\\videos\\clip.mp4\"");

            Assert.Equal(@"C:\videos\clip.mp4", progress.DestinationPath);
        }

        [Fact]
        public void RandomNoticeLine_DoesNotResetProgress()
        {
            var previous = new DownloadProgress
            {
                Percent = 55,
                Speed = "2.00 MiB/s",
                Eta = "00:01:00",
                TotalSize = "8.00 MiB"
            };

            var progress = ParseLog.ParseProgressLine("[youtube] Extracting URL: https://youtu.be/x", previous);

            Assert.Equal(55, progress.Percent);
            Assert.Equal("2.00 MiB/s", progress.Speed);
            Assert.Equal("00:01:00", progress.Eta);
            Assert.Equal("8.00 MiB", progress.TotalSize);
            Assert.Equal("[youtube] Extracting URL: https://youtu.be/x", progress.StatusMessage);
        }

        [Fact]
        public void Percent_RoundsInsteadOfTruncating()
        {
            var progress = ParseLog.ParseProgressLine(
                "[download]  99.9% of ~10.00MiB at  2.50MiB/s ETA 00:00:01");

            Assert.Equal(100, progress.Percent);
        }
    }
}
