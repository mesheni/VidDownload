namespace VidDownload.WPF.Control
{
    using System;
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

        /// <summary>Режим «только аудио»: видео отбрасывается (-vn), кодируется только звук.</summary>
        public bool AudioOnly { get; set; }

        public static readonly IReadOnlyList<string> AllFormats = new[]
        {
            "MP4", "MKV", "MOV", "AVI", "WebM", "FLV", "WMV", "TS"
        };

        public static readonly IReadOnlyList<string> AudioOnlyFormats = new[]
        {
            "MP3", "AAC", "FLAC", "OPUS", "WAV", "M4A"
        };

        /// <summary>Аудиокодек ffmpeg для аудио-формата в режиме «только аудио».</summary>
        public static string GetAudioCodecForAudioFormat(string format) => (format ?? string.Empty).ToUpperInvariant() switch
        {
            "MP3" => "libmp3lame",
            "AAC" or "M4A" => "aac",
            "FLAC" => "flac",
            "OPUS" => "libopus",
            "WAV" => "pcm_s16le",
            _ => "libmp3lame"
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

        // ==== Перевод x264-опций под конкретный энкодер ====
        // У каждого семейства свой набор опций: -crf есть только у x264/x265/vp9/av1,
        // nvenc использует -cq, qsv — -global_quality, amf/mpeg4 — квантизаторы;
        // имена пресетов тоже различаются (nvenc: p1..p7, amf: -quality, vp9/av1: -cpu-used).

        /// <summary>Пресеты nvenc (p1 — самый быстрый, p7 — лучшее качество).</summary>
        private static readonly Dictionary<string, string> NvencPresetMap = new()
        {
            { "ultrafast", "p1" }, { "superfast", "p2" }, { "veryfast", "p3" },
            { "faster", "p4" }, { "fast", "p4" }, { "medium", "p4" },
            { "slow", "p5" }, { "slower", "p6" }, { "veryslow", "p7" }
        };

        /// <summary>Скорость libvpx-vp9: -cpu-used 0-8 (8 — быстрейший).</summary>
        private static readonly Dictionary<string, int> Vp9CpuUsedMap = new()
        {
            { "ultrafast", 8 }, { "superfast", 8 }, { "veryfast", 7 }, { "faster", 6 },
            { "fast", 5 }, { "medium", 4 }, { "slow", 3 }, { "slower", 2 }, { "veryslow", 1 }
        };

        /// <summary>Скорость libaom-av1: без -cpu-used кодирование идёт единицы кадров в секунду.</summary>
        private static readonly Dictionary<string, int> Av1CpuUsedMap = new()
        {
            { "ultrafast", 8 }, { "superfast", 8 }, { "veryfast", 8 }, { "faster", 7 },
            { "fast", 6 }, { "medium", 5 }, { "slow", 4 }, { "slower", 3 }, { "veryslow", 2 }
        };

        /// <summary>Качество amf: -quality speed/balanced/quality.</summary>
        private static readonly Dictionary<string, string> AmfQualityMap = new()
        {
            { "ultrafast", "speed" }, { "superfast", "speed" }, { "veryfast", "speed" },
            { "faster", "balanced" }, { "fast", "balanced" }, { "medium", "balanced" },
            { "slow", "quality" }, { "slower", "quality" }, { "veryslow", "quality" }
        };

        /// <summary>Пресеты qsv: ultrafast/superfast/faster-синонимы отсутствуют — клампим в veryfast.</summary>
        private static readonly string[] QsvPresets = { "veryfast", "faster", "fast", "medium", "slow", "slower", "veryslow" };

        /// <summary>Семейство кодека: определяет, какие опции качества и скорости он понимает.</summary>
        public static string GetCodecFamily(string? codec)
        {
            string c = (codec ?? string.Empty).ToLowerInvariant();
            if (c.EndsWith("_nvenc"))
                return "nvenc";
            if (c.EndsWith("_amf"))
                return "amf";
            if (c.EndsWith("_qsv"))
                return "qsv";
            return c switch
            {
                "libx264" => "x264",
                "libx265" => "x265",
                "mpeg4" or "wmv2" => "mpeg4",
                "libvpx-vp9" => "vp9",
                "libaom-av1" => "av1",
                _ => "x264"
            };
        }

        /// <summary>Аргументы скорости под семейство кодеков; x264-имена пресетов переводятся в эквиваленты.</summary>
        public static List<string> GetPresetArgs(string? codec, string? preset)
        {
            string family = GetCodecFamily(codec);
            string name = (preset ?? "medium").ToLowerInvariant();

            switch (family)
            {
                case "x264":
                case "x265":
                    return name != "medium" && Presets.Contains(name)
                        ? new List<string> { $"-preset {name}" }
                        : new List<string>();

                case "mpeg4":
                    // mpeg4/wmv2 не знают пресетов
                    return new List<string>();

                case "vp9":
                    return new List<string>
                    {
                        "-deadline good",
                        $"-cpu-used {MapPresetValue(Vp9CpuUsedMap, name, 4)}",
                        "-row-mt 1"
                    };

                case "av1":
                    return new List<string> { $"-cpu-used {MapPresetValue(Av1CpuUsedMap, name, 5)}" };

                case "nvenc":
                    return new List<string> { $"-preset {(NvencPresetMap.TryGetValue(name, out var p) ? p : "p4")}" };

                case "amf":
                    return new List<string> { $"-quality {(AmfQualityMap.TryGetValue(name, out var q) ? q : "balanced")}" };

                case "qsv":
                    return new List<string> { $"-preset {(QsvPresets.Contains(name) ? name : "veryfast")}" };
            }

            return new List<string>();
        }

        /// <summary>
        /// Аргументы качества под семейство кодеков: CRF есть только у x264/x265/vp9/av1,
        /// nvenc — -cq, qsv — -global_quality, amf — -qp_i/-qp_p, mpeg4 — шкала -q:v 2-31.
        /// Если CRF не задан, используется битрейт.
        /// </summary>
        public static List<string> GetQualityArgs(string? codec, int? crf, int? bitrateKbps)
        {
            string family = GetCodecFamily(codec);
            bool hasQuality = crf is > 0;

            switch (family)
            {
                case "x264":
                case "x265":
                    if (hasQuality)
                        return new List<string> { $"-crf {crf}" };
                    break;

                case "mpeg4":
                    if (hasQuality)
                    {
                        int qscale = 2 + (int)Math.Round(crf.Value * 29.0 / 51.0);
                        return new List<string> { $"-q:v {qscale}" };
                    }
                    break;

                case "vp9":
                case "av1":
                    // в режиме константного качества vp9/av1 нужен нулевой битрейт-кап
                    if (hasQuality)
                        return new List<string> { $"-crf {crf}", "-b:v 0" };
                    break;

                case "nvenc":
                    if (hasQuality)
                        return new List<string> { "-rc vbr", $"-cq {crf}", "-b:v 0" };
                    break;

                case "amf":
                    if (hasQuality)
                    {
                        int qpP = Math.Min(crf.Value + 2, 51);
                        return new List<string> { "-rc cqp", $"-qp_i {crf}", $"-qp_p {qpP}" };
                    }
                    break;

                case "qsv":
                    if (hasQuality)
                        return new List<string> { $"-global_quality {crf}" };
                    break;
            }

            if (bitrateKbps is > 0)
                return new List<string> { $"-b:v {bitrateKbps}k" };

            return new List<string>();
        }

        /// <summary>Аудиокодеки, для которых имеет смысл -b:a; copy и lossless его не принимают.</summary>
        public static bool SupportsAudioBitrate(string? audioCodec)
        {
            string c = (audioCodec ?? string.Empty).ToLowerInvariant();
            if (c == "copy" || c == "flac" || c == "alac" || c == "wavpack" || c.StartsWith("pcm"))
                return false;
            return true;
        }

        /// <summary>Судьба субтитров: mp4/mov — mov_text, mkv — копия, прочие контейнеры — отбросить.</summary>
        public static List<string> GetSubtitleArgs(string? outputFormat, bool audioOnly)
        {
            if (audioOnly)
                return new List<string> { "-sn" };

            return (outputFormat ?? string.Empty).ToLowerInvariant() switch
            {
                "mp4" or "mov" => new List<string> { "-c:s mov_text" },
                "mkv" => new List<string> { "-c:s copy" },
                _ => new List<string> { "-sn" }
            };
        }

        /// <summary>
        /// Кодеки для выбора: пересечение списка кодировщика и формата.
        /// При пустом пересечении (например, WMV + NVENC) — первый кодек формата, а не libx264,
        /// иначе контейнер отвергнет кодек на этапе мьюксинга.
        /// </summary>
        public static List<string> ResolveVideoCodecList(string? format, string? hardwareEncoder)
        {
            var candidates = GetVideoCodecsForHardwareEncoder(hardwareEncoder ?? string.Empty);
            var formatCodecs = GetVideoCodecsForFormat(format ?? string.Empty);
            var formatSet = new HashSet<string>(formatCodecs, StringComparer.OrdinalIgnoreCase);

            var list = candidates.Where(codec => formatSet.Contains(codec)).ToList();
            if (list.Count == 0)
                list.Add(formatCodecs.FirstOrDefault() ?? "libx264");

            return list;
        }

        private static int MapPresetValue(Dictionary<string, int> map, string preset, int fallback) =>
            map.TryGetValue(preset, out var value) ? value : fallback;
    }
}
