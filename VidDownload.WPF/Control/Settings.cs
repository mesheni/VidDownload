namespace VidDownload.WPF.Control;
/// <summary>
/// TODO: Напишите краткую документацию для этого кода
/// </summary>

/// <summary>
/// Класс Settings определяет свойства для разрешения видео, кодеков и формата.
/// Он содержит два конструктора - один для инициализации свойств и конструктор по умолчанию.
/// Это позволяет создать экземпляр Settings со значениями по умолчанию или пользовательскими значениями.
/// </summary>
public class Settings
{
    public string Resolution { get; set; } = "1080";
    public string VideoCodec { get; set; } = "av01";
    public string AudioCodec { get; set; } = "aac";
    public string Format { get; set; } = "mp4";

    public Settings(string resolution, string videoCodec, string audioCodec, string format)
    {
        Resolution = resolution;
        VideoCodec = videoCodec;
        AudioCodec = audioCodec;
        Format = format;
    }

    public Settings() { }



}
