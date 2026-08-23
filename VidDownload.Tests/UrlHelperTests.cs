using VidDownload.WPF.Services;
using Xunit;

namespace VidDownload.Tests
{
    public class UrlHelperTests
    {
        [Theory]
        [InlineData("https://youtu.be/dQw4w9WgXcQ")]
        [InlineData("http://example.com/video")]
        [InlineData("https://www.youtube.com/watch?v=abc")]
        [InlineData("ytsearch:cool video")]
        [InlineData("  https://youtu.be/x  ")]
        public void ValidReferences_AreAccepted(string value)
        {
            Assert.True(UrlHelper.LooksLikeVideoReference(value));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("not a url")]
        [InlineData("ftp://example.com/file")]
        [InlineData("localhost/video")]
        public void InvalidReferences_AreRejected(string? value)
        {
            Assert.False(UrlHelper.LooksLikeVideoReference(value));
        }
    }
}
