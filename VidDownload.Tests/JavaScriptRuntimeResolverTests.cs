using VidDownload.WPF.Services;
using Xunit;

namespace VidDownload.Tests
{
    public class JavaScriptRuntimeResolverTests
    {
        [Fact]
        public void SelectRuntime_PrefersDenoOverNode()
        {
            string? runtime = JavaScriptRuntimeResolver.SelectRuntime(
                denoPath: @"C:\tools\deno.exe",
                nodePath: @"C:\tools\node.exe",
                fileExists: _ => true);

            Assert.Equal("deno:C:\\tools\\deno.exe", runtime);
        }

        [Fact]
        public void SelectRuntime_UsesNodeWhenDenoIsUnavailable()
        {
            string? runtime = JavaScriptRuntimeResolver.SelectRuntime(
                denoPath: @"C:\tools\deno.exe",
                nodePath: @"C:\Program Files\nodejs\node.exe",
                fileExists: path => path.EndsWith("node.exe", System.StringComparison.OrdinalIgnoreCase));

            Assert.Equal("node:C:\\Program Files\\nodejs\\node.exe", runtime);
        }

        [Fact]
        public void SelectRuntime_ReturnsNullWhenNoRuntimeExists()
        {
            string? runtime = JavaScriptRuntimeResolver.SelectRuntime(
                denoPath: @"C:\tools\deno.exe",
                nodePath: @"C:\tools\node.exe",
                fileExists: _ => false);

            Assert.Null(runtime);
        }
    }
}
