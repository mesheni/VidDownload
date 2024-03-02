namespace VidDownload.WPF.Control;

public class Settings
{
    public string Resolution { get; set;} = "1080";
    public string VideoCodec { get; set;} = "av01";
    public string AudioCodec { get; set;} = "aac";
    public string Format { get; set;} = "mp4";

    public Settings(string resolution, string videoCodec, string audioCodec, string format)
    {
        Resolution = resolution;
        VideoCodec = videoCodec;
        AudioCodec = audioCodec;
        Format = format;
    }

    public Settings() { }



}
