using System.Collections.Generic;
using VidDownload.WPF.ViewModels;
using Xunit;

namespace VidDownload.Tests
{
    /// <summary>Чистая логика главного окна, не требующая WPF-диспетчера.</summary>
    public class MainViewModelLogicTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("5")]
        [InlineData("5M")]
        [InlineData("500K")]
        [InlineData("1.5M")]
        [InlineData("2G")]
        [InlineData("5m")]
        [InlineData("10.25g")]
        public void RateLimit_ValidValues_AreAccepted(string? value)
        {
            Assert.True(MainViewModel.IsValidRateLimit(value));
        }

        [Theory]
        [InlineData("5x")]
        [InlineData("-5M")]
        [InlineData("M")]
        [InlineData("1,5M")]
        [InlineData("5 M")]
        [InlineData("5MB")]
        [InlineData("1.5.5M")]
        public void RateLimit_InvalidValues_AreRejected(string? value)
        {
            Assert.False(MainViewModel.IsValidRateLimit(value));
        }

        [Fact]
        public void QueueSummary_IncludesActiveQueuedAndPausedCounts()
        {
            static string GetString(string key) => key switch
            {
                "QueueActiveCount" => "{0} downloading",
                "QueueQueuedCount" => "{0} queued",
                "QueueSummaryPaused" => "{0} paused",
                _ => key
            };

            Assert.Equal(
                "1 downloading  ·  2 queued  ·  3 paused",
                MainViewModel.BuildQueueSummary(1, 2, 3, GetString));
        }

        [Fact]
        public void QueueSummary_SkipsZeroCounts()
        {
            static string GetString(string key) => key switch
            {
                "QueueActiveCount" => "{0} downloading",
                "QueueQueuedCount" => "{0} queued",
                "QueueSummaryPaused" => "{0} paused",
                _ => key
            };

            Assert.Equal("3 paused", MainViewModel.BuildQueueSummary(0, 0, 3, GetString));
        }

        [Fact]
        public void QueueSummary_IsEmptyWithoutItems()
        {
            Assert.Equal(string.Empty, MainViewModel.BuildQueueSummary(0, 0, 0, key => key));
        }

        [Fact]
        public void FailureReason_UsesFirstNonEmptyLine()
        {
            Assert.Equal("HTTP 403", MainViewModel.ShortenFailureReason("HTTP 403\r\nmore details"));
            Assert.Equal("network error", MainViewModel.ShortenFailureReason("\n \nnetwork error"));
        }

        [Fact]
        public void FailureReason_IsEmptyForEmptyError()
        {
            Assert.Equal(string.Empty, MainViewModel.ShortenFailureReason(null));
            Assert.Equal(string.Empty, MainViewModel.ShortenFailureReason("   "));
        }

        [Fact]
        public void FailureReason_IsTruncatedAtHundredChars()
        {
            string reason = MainViewModel.ShortenFailureReason(new string('a', 150));

            Assert.Equal(101, reason.Length);
            Assert.EndsWith("…", reason);
        }

        [Fact]
        public void AudioQuality_UsesHumanLabelsForExtremes()
        {
            static string GetString(string key) => key switch
            {
                "AudioQualityBest" => "0 — best",
                "AudioQualityWorst" => "9 — lowest",
                _ => key
            };

            Assert.Equal("0 — best", MainViewModel.GetAudioQualityDisplay("0", GetString));
            Assert.Equal("9 — lowest", MainViewModel.GetAudioQualityDisplay("9", GetString));
        }

        [Fact]
        public void AudioQuality_MiddleValuesStayAsNumbers()
        {
            Assert.Equal("5", MainViewModel.GetAudioQualityDisplay("5", key => key));
            Assert.Equal(string.Empty, MainViewModel.GetAudioQualityDisplay(null, key => key));
        }

        [Fact]
        public void ResolveSelection_PrefersSavedValueAndFallsBackToDefault()
        {
            var items = new List<string> { "", "720", "1080", "2160" };

            Assert.Equal("720", MainViewModel.ResolveSelection("720", items, "1080"));
            Assert.Equal("1080", MainViewModel.ResolveSelection("", items, "1080"));
            Assert.Equal("1080", MainViewModel.ResolveSelection("480", items, "1080"));
        }
    }
}
