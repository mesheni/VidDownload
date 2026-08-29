using System.Linq;
using VidDownload.WPF.Control;
using VidDownload.WPF.ViewModels;
using Xunit;

namespace VidDownload.Tests
{
    public class MetadataParserTests
    {
        private const string SingleVideoJson = @"
{
  ""id"": ""abcd1234"",
  ""title"": ""Test Video"",
  ""uploader"": ""Channel"",
  ""duration"": 3725,
  ""thumbnail"": ""https://example.com/t.webp"",
  ""webpage_url"": ""https://youtu.be/abcd1234"",
  ""formats"": [
    { ""format_id"": ""140"", ""ext"": ""m4a"", ""resolution"": ""audio only"", ""vcodec"": ""none"", ""acodec"": ""mp4a.40.2"", ""filesize"": 3000000 },
    { ""format_id"": ""160"", ""ext"": ""mp4"", ""resolution"": ""256x144"", ""vcodec"": ""avc1.4d400c"", ""acodec"": ""mp4a.40.2"", ""format_note"": ""144p"" },
    { ""format_id"": ""137"", ""ext"": ""mp4"", ""resolution"": ""1920x1080"", ""vcodec"": ""avc1.640028"", ""acodec"": ""none"", ""fps"": 30.0, ""filesize"": 125829120, ""format_note"": ""1080p"" },
    { ""format_id"": ""18"", ""ext"": ""mp4"", ""resolution"": ""640x360"", ""vcodec"": ""avc1.42001E"", ""acodec"": ""mp4a.40.2"", ""format_note"": ""360p"" }
  ]
}";

        private const string PlaylistJson = @"
{
  ""_type"": ""playlist"",
  ""id"": ""PL123"",
  ""title"": ""My Playlist"",
  ""entries"": [
    { ""id"": ""v1"", ""title"": ""First"", ""duration"": 61, ""url"": ""https://youtu.be/v1"", ""playlist_index"": 1 },
    { ""id"": ""v2"", ""title"": ""Second"", ""duration"": 122, ""playlist_index"": 2 },
    { ""id"": ""v3"", ""title"": ""Third"", ""url"": ""https://youtu.be/v3"", ""playlist_index"": 3 }
  ]
}";

        [Fact]
        public void Parse_SingleVideo_FillsFieldsAndFormats()
        {
            var info = MetadataParser.ParseVideoInfo(SingleVideoJson);

            Assert.Equal("Test Video", info.Title);
            Assert.Equal("Channel", info.Uploader);
            Assert.Equal(3725, info.Duration);
            Assert.False(info.IsPlaylistResult);
            Assert.Equal(4, info.Formats.Count);

            var f137 = info.Formats.First(f => f.FormatId == "137");
            Assert.True(f137.IsVideoOnly);
            Assert.False(f137.IsAudioOnly);
            Assert.Equal(125829120, f137.Filesize);

            var f140 = info.Formats.First(f => f.FormatId == "140");
            Assert.True(f140.IsAudioOnly);
        }

        [Fact]
        public void Parse_Playlist_FillsEntriesAndFallbackUrls()
        {
            var info = MetadataParser.ParseVideoInfo(PlaylistJson);

            Assert.True(info.IsPlaylistResult);
            Assert.Equal(3, info.Entries.Count);
            Assert.Equal("First", info.Entries[0].Title);
            Assert.Equal("https://youtu.be/v1", info.Entries[0].Url);

            // url отсутствовал — должен подставиться id
            Assert.Equal("v2", info.Entries[1].Url);
            Assert.Equal(2, info.Entries[1].Index);
        }

        [Fact]
        public void GetSelectableVideoFormats_FiltersAudioAndSortsByHeight()
        {
            var info = MetadataParser.ParseVideoInfo(SingleVideoJson);

            var selectable = MetadataParser.GetSelectableVideoFormats(info);

            Assert.Equal(3, selectable.Count);
            Assert.Equal("137", selectable[0].FormatId);
            Assert.DoesNotContain(selectable, f => f.IsAudioOnly);
        }

        [Fact]
        public void BuildPlaylistItems_CompressesRanges()
        {
            var result = VideoInfoViewModel.BuildPlaylistItems(new[] { 1, 2, 3, 7, 10, 11, 12 });
            Assert.Equal("1-3,7,10-12", result);
        }

        [Fact]
        public void BuildPlaylistItems_SingleAndUnsorted()
        {
            Assert.Equal("5", VideoInfoViewModel.BuildPlaylistItems(new[] { 5 }));
            Assert.Equal("1,4,6", VideoInfoViewModel.BuildPlaylistItems(new[] { 6, 4, 1 }));
        }
    }
}
