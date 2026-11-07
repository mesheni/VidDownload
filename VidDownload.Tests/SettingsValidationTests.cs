using System;
using System.IO;
using VidDownload.WPF.ViewModels;
using Xunit;

namespace VidDownload.Tests
{
    public class SettingsValidationTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        [InlineData("http://127.0.0.1:8080")]
        [InlineData("https://proxy.example.com")]
        [InlineData("socks4://10.0.0.1:1080")]
        [InlineData("socks5://127.0.0.1:1080")]
        [InlineData("SOCKS5H://localhost:1080")]
        [InlineData("socks5://user:pass@127.0.0.1:1080")]
        [InlineData("  socks5://127.0.0.1:1080  ")]
        public void ValidProxies_AreAccepted(string? value)
        {
            Assert.True(SettingsViewModel.IsValidProxy(value));
        }

        [Theory]
        [InlineData("not a proxy")]
        [InlineData("127.0.0.1:1080")]
        [InlineData("localhost:1080")]
        [InlineData("ftp://127.0.0.1:21")]
        [InlineData("http://")]
        [InlineData("socks5://")]
        [InlineData("socks5://:1080")]
        public void InvalidProxies_AreRejected(string? value)
        {
            Assert.False(SettingsViewModel.IsValidProxy(value));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void EmptyCookiesPath_IsAccepted(string? path)
        {
            Assert.True(SettingsViewModel.IsValidCookiesFile(path));
        }

        [Fact]
        public void MissingCookiesFile_IsRejected()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
            Assert.False(SettingsViewModel.IsValidCookiesFile(path));
        }
    }
}
