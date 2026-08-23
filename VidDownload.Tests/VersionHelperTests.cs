using VidDownload.WPF.Services;
using Xunit;

namespace VidDownload.Tests
{
    public class VersionHelperTests
    {
        [Fact]
        public void ShorterSegmentVersion_IsComparedCorrectly()
        {
            // Старая реализация со склейкой цифр считала 202513 < 20241219
            Assert.True(VersionHelper.CompareDotted("2025.1.3", "2024.12.19") > 0);
            Assert.True(VersionHelper.CompareDotted("2024.12.19", "2025.1.3") < 0);
        }

        [Fact]
        public void EqualVersions_CompareZero()
        {
            Assert.Equal(0, VersionHelper.CompareDotted("2025.08.23", "2025.08.23"));
        }

        [Fact]
        public void Suffix_IsIgnored()
        {
            Assert.Equal(0, VersionHelper.CompareDotted("2025.01.01-rc1", "2025.01.01"));
        }

        [Fact]
        public void LeadingV_IsTrimmed()
        {
            Assert.Equal(0, VersionHelper.CompareDotted("v0.8.0", "0.8.0"));
            Assert.True(VersionHelper.CompareDotted("0.9.0", "v0.8.0") > 0);
        }

        [Fact]
        public void DifferentLengths_AreComparedBySegments()
        {
            Assert.True(VersionHelper.CompareDotted("1.2", "1.2.1") < 0);
            Assert.True(VersionHelper.CompareDotted("1.3", "1.2.99") > 0);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("abc")]
        [InlineData("latest")]
        public void InvalidVersions_AreNotValid(string? version)
        {
            Assert.False(VersionHelper.IsValidDotted(version));
        }

        [Theory]
        [InlineData("2025.08.23")]
        [InlineData("0.8.0")]
        [InlineData("2024.12.19-rc1")]
        public void ValidVersions_AreValid(string version)
        {
            Assert.True(VersionHelper.IsValidDotted(version));
        }
    }
}
