namespace VidDownload.WPF.Control
{
    using System.Collections.Generic;
    using System.Linq;

    public class ConversionOptions
    {
        public string InputPath { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public string OutputFormat { get; set; } = "mp4";
        public string VideoCodec { get; set; } = "libx264";
        public string AudioCodec { get; set; } = "aac";
        public string HardwareEncoder { get; set; } = string.Empty;
        public int? Crf { get; set; }
        public int? VideoBitrate { get; set; }
        public int? AudioBitrate { get; set; }
        public string Preset { get; set; } = "medium";

        public static readonly IReadOnlyList<string> AllFormats = new[]
        {
            "MP4", "MKV", "MOV", "AVI", "WebM", "FLV", "WMV", "TS"
        };

        public static readonly IReadOnlyList<string> CpuVideoCodecs = new[]
        {
            "libx264", "libx265", "libaom-av1", "libvpx-vp9", "mpeg4"
        };

        public static readonly IReadOnlyList<string> CpuAudioCodecs = new[]
        {
            "aac", "mp3", "opus", "flac", "wmav2", "copy"
        };

        public static readonly IReadOnlyList<string> Presets = new[]
        {
            "ultrafast", "superfast", "veryfast", "faster", "fast",
            "medium", "slow", "slower", "veryslow"
        };

        private static readonly Dictionary<string, string> NvencCodecMap = new()
        {
            { "h264_nvenc", "H.264 (NVENC)" },
            { "hevc_nvenc", "H.265 (NVENC)" },
            { "av1_nvenc", "AV1 (NVENC)" }
        };

        private static readonly Dictionary<string, string> AmfCodecMap = new()
        {
            { "h264_amf", "H.264 (AMF)" },
            { "hevc_amf", "H.265 (AMF)" }
        };

        private static readonly Dictionary<string, string> QsvCodecMap = new()
        {
            { "h264_qsv", "H.264 (QSV)" },
            { "hevc_qsv", "H.265 (QSV)" }
        };

        private static readonly Dictionary<string, IReadOnlyList<string>> FormatVideoCodecs = new()
        {
            { "mp4", new[] { "libx264", "libx265", "mpeg4", "libaom-av1", "h264_nvenc", "hevc_nvenc", "av1_nvenc", "h264_amf", "hevc_amf", "h264_qsv", "hevc_qsv" } },
            { "mkv", new[] { "libx264", "libx265", "libaom-av1", "libvpx-vp9", "mpeg4", "h264_nvenc", "hevc_nvenc", "av1_nvenc", "h264_amf", "hevc_amf", "h264_qsv", "hevc_qsv" } },
            { "mov", new[] { "libx264", "libx265", "mpeg4", "h264_nvenc", "hevc_nvenc", "h264_amf", "hevc_amf", "h264_qsv", "hevc_qsv" } },
            { "avi", new[] { "libx264", "mpeg4", "h264_nvenc", "h264_amf", "h264_qsv" } },
            { "webm", new[] { "libvpx-vp9", "libaom-av1", "av1_nvenc" } },
            { "flv", new[] { "libx264", "h264_nvenc", "h264_amf", "h264_qsv" } },
            { "wmv", new[] { "wmv2" } },
            { "ts", new[] { "libx264", "libx265", "h264_nvenc", "hevc_nvenc", "h264_amf", "hevc_amf", "h264_qsv", "hevc_qsv" } }
        };

        private static readonly Dictionary<string, IReadOnlyList<string>> FormatAudioCodecs = new()
        {
            { "mp4", new[] { "aac", "mp3", "copy" } },
            { "mkv", new[] { "aac", "mp3", "opus", "flac", "copy" } },
            { "mov", new[] { "aac", "mp3", "copy" } },
            { "avi", new[] { "mp3", "aac", "copy" } },
            { "webm", new[] { "opus", "copy" } },
            { "flv", new[] { "aac", "mp3", "copy" } },
            { "wmv", new[] { "wmav2" } },
            { "ts", new[] { "aac", "mp3", "copy" } }
        };

        public static IReadOnlyList<string> GetVideoCodecsForFormat(string format)
        {
            format = format.ToLower();
            return FormatVideoCodecs.TryGetValue(format, out var list) ? list : CpuVideoCodecs;
        }

        public static IReadOnlyList<string> GetAudioCodecsForFormat(string format)
        {
            format = format.ToLower();
            return FormatAudioCodecs.TryGetValue(format, out var list) ? list : CpuAudioCodecs;
        }

        public static IReadOnlyList<string> GetVideoCodecsForHardwareEncoder(string hwEncoder)
        {
            hwEncoder = (hwEncoder ?? string.Empty).ToLower();
            return hwEncoder switch
            {
                "nvenc" => NvencCodecMap.Keys.ToList(),
                "amf" => AmfCodecMap.Keys.ToList(),
                "qsv" => QsvCodecMap.Keys.ToList(),
                _ => CpuVideoCodecs
            };
        }

        public static string GetHardwareEncoderDisplayName(string codec)
        {
            if (NvencCodecMap.TryGetValue(codec, out var nv)) return nv;
            if (AmfCodecMap.TryGetValue(codec, out var amf)) return amf;
            if (QsvCodecMap.TryGetValue(codec, out var qsv)) return qsv;
            return codec;
        }

        public static string? DetectHardwareEncoder(string codec)
        {
            if (NvencCodecMap.ContainsKey(codec)) return "nvenc";
            if (AmfCodecMap.ContainsKey(codec)) return "amf";
            if (QsvCodecMap.ContainsKey(codec)) return "qsv";
            return null;
        }

        public static readonly IReadOnlyList<string> HardwareEncoderTypes = new[] { "", "nvenc", "amf", "qsv" };
    }
}
