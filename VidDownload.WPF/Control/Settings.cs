namespace VidDownload.WPF.Control;

public record Settings
{
    public string Resolution { get; init; } = "1080";
    public string VideoCodec { get; init; } = "av01";
    public string AudioCodec { get; init; } = "aac";
    public string Format { get; init; } = "mp4";

    public Settings() { }

    public Settings(string resolution, string videoCodec, string audioCodec, string format)
    {
        Resolution = resolution;
        VideoCodec = videoCodec;
        AudioCodec = audioCodec;
        Format = format;
    }
}
