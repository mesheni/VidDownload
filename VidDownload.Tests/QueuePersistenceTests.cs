using System.IO;
using VidDownload.WPF.Control;
using VidDownload.WPF.Services;
using Xunit;

namespace VidDownload.Tests
{
    public class QueuePersistenceTests
    {
        private static string TempStorePath() =>
            Path.Combine(Path.GetTempPath(), $"queue-test-{System.Guid.NewGuid():N}.json");

        [Fact]
        public void SaveLoad_RoundtripPreservesItems()
        {
            string path = TempStorePath();
            try
            {
                var items = new[]
                {
                    new QueuedItemDto
                    {
                        Url = "https://youtu.be/a",
                        Options = new Settings { SavePath = @"C:\v", Resolution = "720", Proxy = "socks5://h:1" },
                        IsPlaylist = true,
                        IsAudioOnly = false,
                        IsReEncode = true
                    },
                    new QueuedItemDto
                    {
                        Url = "https://youtu.be/b",
                        Options = new Settings { SavePath = @"C:\v2", AudioQuality = "0" }
                    }
                };

                QueuePersistenceService.Save(items, path);
                var loaded = QueuePersistenceService.Load(path);

                Assert.Equal(2, loaded.Count);
                Assert.Equal("https://youtu.be/a", loaded[0].Url);
                Assert.True(loaded[0].IsPlaylist);
                Assert.True(loaded[0].IsReEncode);
                Assert.Equal("720", loaded[0].Options.Resolution);
                Assert.Equal("socks5://h:1", loaded[0].Options.Proxy);
                Assert.Equal("0", loaded[1].Options.AudioQuality);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void Save_EmptyList_RemovesFile()
        {
            string path = TempStorePath();
            try
            {
                QueuePersistenceService.Save(new[] { new QueuedItemDto { Url = "u" } }, path);
                Assert.True(File.Exists(path));

                QueuePersistenceService.Save(System.Array.Empty<QueuedItemDto>(), path);
                Assert.False(File.Exists(path));
                Assert.Empty(QueuePersistenceService.Load(path));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }
}
