using System.Linq;
using VidDownload.WPF.Control;
using Xunit;

namespace VidDownload.Tests
{
    /// <summary>Проверка перевода x264-опций под конкретные семейства кодеков ffmpeg.</summary>
    public class ConversionArgsTests
    {
        private static ConversionOptions BaseVideoOptions(string codec, string? preset = "medium", int? crf = 23) => new()
        {
            VideoCodec = codec,
            AudioCodec = "aac",
            Preset = preset ?? "medium",
            Crf = crf,
            OutputFormat = "mp4"
        };

        // ==== Качество: -crf только у кодеков, которые его понимают ====

        [Theory]
        [InlineData("h264_nvenc")]
        [InlineData("hevc_nvenc")]
        [InlineData("av1_nvenc")]
        [InlineData("h264_amf")]
        [InlineData("hevc_amf")]
        [InlineData("h264_qsv")]
        [InlineData("hevc_qsv")]
        [InlineData("mpeg4")]
        [InlineData("wmv2")]
        public void Build_HardwareOrMpeg4_NeverEmitsCrf(string codec)
        {
            var args = FFmpegAction.BuildConversionParameters(BaseVideoOptions(codec));

            Assert.DoesNotContain(args, a => a.StartsWith("-crf"));
        }

        [Fact]
        public void Build_Nvenc_QualityBecomesVbrCq()
        {
            var args = FFmpegAction.BuildConversionParameters(BaseVideoOptions("h264_nvenc"));

            Assert.Contains("-rc vbr", args);
            Assert.Contains("-cq 23", args);
            Assert.Contains("-b:v 0", args);
        }

        [Fact]
        public void Build_Amf_QualityBecomesCqpQuantizers()
        {
            var args = FFmpegAction.BuildConversionParameters(BaseVideoOptions("h264_amf"));

            Assert.Contains("-rc cqp", args);
            Assert.Contains("-qp_i 23", args);
            Assert.Contains("-qp_p 25", args);
        }

        [Fact]
        public void Build_Qsv_QualityBecomesGlobalQuality()
        {
            var args = FFmpegAction.BuildConversionParameters(BaseVideoOptions("h264_qsv"));

            Assert.Contains("-global_quality 23", args);
        }

        [Fact]
        public void Build_Mpeg4_QualityBecomesQscale()
        {
            var args = FFmpegAction.BuildConversionParameters(BaseVideoOptions("mpeg4"));

            // crf 23 → середина шкалы qscale 2-31
            Assert.Contains("-q:v 15", args);
        }

        [Fact]
        public void Build_Vp9_QualityNeedsZeroBitrateCap()
        {
            var args = FFmpegAction.BuildConversionParameters(BaseVideoOptions("libvpx-vp9"));

            Assert.Contains("-crf 23", args);
            Assert.Contains("-b:v 0", args);
        }

        [Fact]
        public void Build_Av1_QualityNeedsZeroBitrateCap()
        {
            var args = FFmpegAction.BuildConversionParameters(BaseVideoOptions("libaom-av1"));

            Assert.Contains("-crf 23", args);
            Assert.Contains("-b:v 0", args);
            // без cpu-used AV1 кодируется единицы кадров в секунду
            Assert.Contains(args, a => a.StartsWith("-cpu-used "));
        }

        [Fact]
        public void Build_NoCrf_BitrateUsedInstead()
        {
            var options = BaseVideoOptions("h264_nvenc", crf: null);
            options.VideoBitrate = 4000;

            var args = FFmpegAction.BuildConversionParameters(options);

            Assert.Contains("-b:v 4000k", args);
            Assert.DoesNotContain("-cq 23", args);
        }

        // ==== Пресеты: имена переводятся под семейство ====

        [Theory]
        [InlineData("h264_nvenc", "ultrafast", "-preset p1")]
        [InlineData("hevc_nvenc", "veryslow", "-preset p7")]
        [InlineData("h264_nvenc", "medium", "-preset p4")]
        [InlineData("h264_amf", "ultrafast", "-quality speed")]
        [InlineData("h264_amf", "medium", "-quality balanced")]
        [InlineData("hevc_amf", "veryslow", "-quality quality")]
        [InlineData("h264_qsv", "ultrafast", "-preset veryfast")]
        [InlineData("hevc_qsv", "veryslow", "-preset veryslow")]
        [InlineData("libvpx-vp9", "medium", "-cpu-used 4")]
        [InlineData("libaom-av1", "medium", "-cpu-used 5")]
        [InlineData("libx264", "fast", "-preset fast")]
        [InlineData("libx265", "slow", "-preset slow")]
        public void Build_Preset_MappedPerFamily(string codec, string preset, string expected)
        {
            var args = FFmpegAction.BuildConversionParameters(BaseVideoOptions(codec, preset));

            Assert.Contains(expected, args);
        }

        [Fact]
        public void Build_Mpeg4AndWmv2_NoPresetAtAll()
        {
            Assert.All(
                new[] { "mpeg4", "wmv2" },
                codec =>
                {
                    var args = FFmpegAction.BuildConversionParameters(BaseVideoOptions(codec, "veryslow"));
                    Assert.DoesNotContain(args, a => a.StartsWith("-preset ") || a.StartsWith("-quality "));
                });
        }

        [Fact]
        public void Build_X264_MediumPresetOmitted()
        {
            var args = FFmpegAction.BuildConversionParameters(BaseVideoOptions("libx264", "medium"));

            Assert.DoesNotContain(args, a => a.StartsWith("-preset "));
        }

        // ==== Субтитры и аудио ====

        [Theory]
        [InlineData("mp4", "-c:s mov_text")]
        [InlineData("mov", "-c:s mov_text")]
        [InlineData("mkv", "-c:s copy")]
        [InlineData("webm", "-sn")]
        [InlineData("flv", "-sn")]
        [InlineData("wmv", "-sn")]
        [InlineData("avi", "-sn")]
        [InlineData("ts", "-sn")]
        public void Build_SubtitlesHandledPerContainer(string format, string expected)
        {
            var options = BaseVideoOptions("libx264");
            options.OutputFormat = format;

            var args = FFmpegAction.BuildConversionParameters(options);

            Assert.Contains(expected, args);
        }

        [Fact]
        public void Build_AudioOnly_HasVnSnAndCodec()
        {
            var options = new ConversionOptions
            {
                AudioOnly = true,
                AudioCodec = "libmp3lame",
                OutputFormat = "mp3"
            };

            var args = FFmpegAction.BuildConversionParameters(options);

            Assert.Contains("-vn", args);
            Assert.Contains("-c:a libmp3lame", args);
            Assert.Contains("-sn", args);
            Assert.DoesNotContain(args, a => a.StartsWith("-c:v "));
        }

        [Fact]
        public void Build_AudioOnly_Wav_IgnoresBitrate()
        {
            var options = new ConversionOptions
            {
                AudioOnly = true,
                AudioCodec = "pcm_s16le",
                AudioBitrate = 192,
                OutputFormat = "wav"
            };

            var args = FFmpegAction.BuildConversionParameters(options);

            Assert.DoesNotContain(args, a => a.StartsWith("-b:a "));
        }

        [Fact]
        public void Build_AudioCopy_IgnoresBitrate()
        {
            var options = BaseVideoOptions("libx264");
            options.AudioCodec = "copy";
            options.AudioBitrate = 192;

            var args = FFmpegAction.BuildConversionParameters(options);

            Assert.DoesNotContain(args, a => a.StartsWith("-b:a "));
        }

        [Fact]
        public void Build_LossyAudio_KeepsBitrate()
        {
            var options = BaseVideoOptions("libx264");
            options.AudioBitrate = 192;

            var args = FFmpegAction.BuildConversionParameters(options);

            Assert.Contains("-b:a 192k", args);
        }

        // ==== Списки кодеков ====

        [Fact]
        public void Resolve_WmvWithNvenc_FallsBackToFormatCodec()
        {
            // раньше подставлялся libx264, который контейнер WMV не принимает
            var list = ConversionOptions.ResolveVideoCodecList("wmv", "nvenc");

            Assert.Equal(new[] { "wmv2" }, list);
        }

        [Fact]
        public void Resolve_WebmWithAmf_FallsBackToFormatCodec()
        {
            var list = ConversionOptions.ResolveVideoCodecList("webm", "amf");

            Assert.Equal("libvpx-vp9", list.First());
        }

        [Fact]
        public void Resolve_Mp4WithNvenc_Intersects()
        {
            var list = ConversionOptions.ResolveVideoCodecList("mp4", "nvenc");

            Assert.Equal(new[] { "h264_nvenc", "hevc_nvenc", "av1_nvenc" }, list);
        }

        [Fact]
        public void Resolve_Mp4WithNone_CpuCodecs()
        {
            var list = ConversionOptions.ResolveVideoCodecList("mp4", "");

            Assert.Contains("libx264", list);
            Assert.Contains("mpeg4", list);
            Assert.DoesNotContain("h264_nvenc", list);
        }

        // ==== Семейства ====

        [Theory]
        [InlineData("libx264", "x264")]
        [InlineData("libx265", "x265")]
        [InlineData("mpeg4", "mpeg4")]
        [InlineData("wmv2", "mpeg4")]
        [InlineData("libvpx-vp9", "vp9")]
        [InlineData("libaom-av1", "av1")]
        [InlineData("h264_nvenc", "nvenc")]
        [InlineData("hevc_nvenc", "nvenc")]
        [InlineData("av1_nvenc", "nvenc")]
        [InlineData("h264_amf", "amf")]
        [InlineData("hevc_qsv", "qsv")]
        public void GetCodecFamily_GroupsEncoders(string codec, string family)
        {
            Assert.Equal(family, ConversionOptions.GetCodecFamily(codec));
        }
    }
}
