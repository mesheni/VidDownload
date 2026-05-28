namespace VidDownload.WPF.Resources
{
    internal static class LocalizedStrings
    {
        public static string GetString(string key) => Res.ResourceManager.GetString(key, Res.Culture);
    }
}
