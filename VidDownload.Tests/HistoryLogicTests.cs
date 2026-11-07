using VidDownload.WPF.Control;
using VidDownload.WPF.ViewModels;
using Xunit;

namespace VidDownload.Tests
{
    /// <summary>Чистая логика истории загрузок и предпросмотра (без WPF dispatcher).</summary>
    public class HistoryLogicTests
    {
        [Theory]
        [InlineData(51, 50, true)]
        [InlineData(50, 50, false)]
        [InlineData(10, 50, false)]
        [InlineData(101, 100, true)]
        public void HasMoreEntries_ComparesFetchedPageWithLimit(int fetched, int limit, bool expected)
        {
            Assert.Equal(expected, HistoryViewModel.HasMoreEntries(fetched, limit));
        }

        [Theory]
        [InlineData("Видео", "https://example.com", "Завершено", "завер", true)]
        [InlineData("Видео", "https://example.com", "Завершено", "ОШИБКА", false)]
        [InlineData("Урок", "https://example.com", "Ошибка", "ошиб", true)]
        [InlineData("Урок", "https://example.com", "Отменено", "example", true)]
        [InlineData(null, null, null, "что-нибудь", false)]
        [InlineData(null, null, null, "  ", true)]
        public void MatchesSearch_UsesTitleUrlAndLocalizedStatus(
            string title, string url, string statusText, string query, bool expected)
        {
            Assert.Equal(expected, HistoryViewModel.MatchesSearch(title, url, statusText, query));
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        public void ShouldSwitchToManual_OnlyWhenAutoAndSelectionPresent(
            bool useAutoFormat, bool hasSelection, bool expected)
        {
            Assert.Equal(expected, VideoInfoViewModel.ShouldSwitchToManual(useAutoFormat, hasSelection));
        }

        [Fact]
        public void BuildFormatSelector_ReturnsEmptyForAuto()
        {
            var format = new FormatInfo { FormatId = "137", ACodec = "none" };

            Assert.Equal(string.Empty, VideoInfoViewModel.BuildFormatSelector(true, format));
        }

        [Fact]
        public void BuildFormatSelector_ReturnsEmptyWithoutFormat()
        {
            Assert.Equal(string.Empty, VideoInfoViewModel.BuildFormatSelector(false, null));
        }

        [Fact]
        public void BuildFormatSelector_AppendsBestAudioForVideoOnly()
        {
            var format = new FormatInfo { FormatId = "137", ACodec = "none" };

            Assert.Equal("137+ba", VideoInfoViewModel.BuildFormatSelector(false, format));
        }

        [Fact]
        public void BuildFormatSelector_UsesMuxedFormatIdAsIs()
        {
            var format = new FormatInfo { FormatId = "18", ACodec = "mp4a.40.2" };

            Assert.Equal("18", VideoInfoViewModel.BuildFormatSelector(false, format));
        }
    }
}
