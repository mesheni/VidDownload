using VidDownload.WPF.Control;
using Xunit;

namespace VidDownload.Tests
{
    public class TimecodesTests
    {
        [Theory]
        [InlineData("90", 90)]
        [InlineData("1:30", 90)]
        [InlineData("01:02:03", 3723)]
        [InlineData("1:00:05.5", 3605.5)]
        [InlineData(" 2:05 ", 125)]
        public void TryParse_AcceptsSupportedFormats(string input, double expected)
        {
            Assert.True(Timecodes.TryParse(input, out double seconds));
            Assert.Equal(expected, seconds, 3);
        }

        [Theory]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("1:2:3:4")]
        [InlineData("-1:00")]
        public void TryParse_RejectsInvalid(string input)
        {
            Assert.False(Timecodes.TryParse(input, out _));
        }

        [Fact]
        public void TryBuildSection_BuildsYtDlpSection()
        {
            Assert.True(Timecodes.TryBuildSection("1:30", "5:00", out string section));
            Assert.Equal("*00:01:30-00:05:00", section);
        }

        [Fact]
        public void TryBuildSection_RejectsMissingOrInverted()
        {
            Assert.False(Timecodes.TryBuildSection("", "5:00", out _));
            Assert.False(Timecodes.TryBuildSection("1:30", "", out _));
            Assert.False(Timecodes.TryBuildSection("5:00", "1:30", out _));
            Assert.False(Timecodes.TryBuildSection("bad", "5:00", out _));
        }
    }
}
