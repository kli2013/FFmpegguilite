using System;
using System.Collections.Generic;
using System.Globalization;
using FFLiteGUI.Models;
using FFLiteGUI.Strategies;
using FFLiteGUI.Utils;

namespace FFLiteGUI.Services
{
    public class FFmpegCommandBuilder
    {
        private readonly string _ffmpegPath;

        public FFmpegCommandBuilder(string ffmpegPath)
        {
            _ffmpegPath = ffmpegPath;
        }

        public string BuildCommand(string inputFile, string outputFile, VideoSettings settings)
        {
            if (string.IsNullOrEmpty(_ffmpegPath))
                throw new InvalidOperationException("未找到 ffmpeg.exe");

            var args = new List<string>();
            args.Add("-y");
            args.Add("-fflags +genpts");

            // 截取片段（非仅音频模式）
            if (!settings.OnlyAudio && settings.TrimEnabled)
            {
                if (!string.IsNullOrEmpty(settings.TrimStart))
                    args.Add($"-ss {settings.TrimStart}");
                if (!string.IsNullOrEmpty(settings.TrimEnd))
                    args.Add($"-to {settings.TrimEnd}");
            }

            // 硬件解码
            if (!settings.OnlyAudio && settings.HwaccelEnabled && settings.HwaccelDecoder != "无")
            {
                string decoderArgs = HardwareDecoderHelper.BuildHwaccelArgs(settings.HwaccelDecoder);
                if (!string.IsNullOrEmpty(decoderArgs))
                    args.Add(decoderArgs);
            }

            args.Add($"-i \"{PathHelper.Normalize(inputFile)}\"");

            // 视频处理
            if (settings.OnlyAudio)
            {
                args.Add("-vn");
            }
            else
            {
                string vf = BuildFilterChain(settings);
                if (!string.IsNullOrEmpty(vf))
                    args.Add($"-vf \"{vf}\"");
                if (settings.FrameRateType == "custom" && !string.IsNullOrEmpty(settings.FrameRateCustom))
                    args.Add($"-r {settings.FrameRateCustom}");

                IEncoderStrategy strategy = EncoderStrategyFactory.GetStrategy(settings.Encoder);
                string videoParams = strategy.BuildVideoParams(settings);
                if (!string.IsNullOrEmpty(videoParams))
                    args.Add(videoParams);
            }

            // 音频处理 —— 修复音频编码缺失问题
            if (!settings.AudioEnabled)
            {
                args.Add("-an");
            }
            else
            {
                // 确保音频编码器有默认值
                string audioCodec = settings.AudioCodec;
                if (string.IsNullOrEmpty(audioCodec)) audioCodec = "aac";

                if (audioCodec == "copy")
                {
                    if (settings.SpeedEnabled && Math.Abs(settings.SpeedFactor - 1.0) > 0.001)
                    {
                        // 变速时必须重编码音频
                        args.Add("-c:a aac");
                        args.Add($"-b:a {settings.AudioBitrate}");
                        args.Add($"-ar {settings.AudioSamplerate}");
                        string atempo = AtempoFilter(settings.SpeedFactor);
                        if (!string.IsNullOrEmpty(atempo))
                            args.Add($"-af \"{atempo}\"");
                    }
                    else
                    {
                        args.Add("-c:a copy");
                    }
                }
                else
                {
                    args.Add($"-c:a {audioCodec}");
                    args.Add($"-b:a {settings.AudioBitrate}");
                    args.Add($"-ar {settings.AudioSamplerate}");
                    if (settings.SpeedEnabled && Math.Abs(settings.SpeedFactor - 1.0) > 0.001)
                    {
                        string atempo = AtempoFilter(settings.SpeedFactor);
                        if (!string.IsNullOrEmpty(atempo))
                            args.Add($"-af \"{atempo}\"");
                    }
                }
            }

            // 自定义参数
            if (!string.IsNullOrEmpty(settings.CustomArgs))
                args.Add(settings.CustomArgs);

            // 容器优化
            if (!settings.OnlyAudio && (settings.OutputContainer == "mp4" || settings.OutputContainer == "mov"))
                args.Add("-movflags +faststart");

            args.Add($"\"{PathHelper.Normalize(outputFile)}\"");

            return $"\"{_ffmpegPath}\" {string.Join(" ", args)}";
        }

        private string BuildFilterChain(VideoSettings s)
        {
            var filters = new List<string>();

            // 裁剪
            if (s.CropEnabled && !string.IsNullOrEmpty(s.CropWidth) && !string.IsNullOrEmpty(s.CropHeight))
            {
                filters.Add($"crop={s.CropWidth}:{s.CropHeight}:{s.CropLeft}:{s.CropTop}");
            }

            // 缩放
            if (s.ScaleEnabled)
            {
                if (s.ScaleMethod == "width" && !string.IsNullOrEmpty(s.ScaleWidth))
                {
                    filters.Add($"scale={s.ScaleWidth}:-2");
                }
                else if (s.ScaleMethod == "height" && !string.IsNullOrEmpty(s.ScaleHeight))
                {
                    filters.Add($"scale=-2:{s.ScaleHeight}");
                }
                else if (s.ScaleMethod == "exact" && !string.IsNullOrEmpty(s.ScaleWidth) && !string.IsNullOrEmpty(s.ScaleHeight))
                {
                    filters.Add($"scale={s.ScaleWidth}:{s.ScaleHeight}");
                }
            }

            // 旋转
            if (s.Rotate == "90")
                filters.Add("transpose=1");
            else if (s.Rotate == "180")
                filters.Add("transpose=2,transpose=2");
            else if (s.Rotate == "270")
                filters.Add("transpose=2");

            // 翻转
            if (s.Vflip) filters.Add("vflip");
            if (s.Hflip) filters.Add("hflip");

            // 反交错
            if (s.DeinterlaceFilter != "none")
                filters.Add(s.DeinterlaceFilter);

            // 像素格式
            if (s.PixFmtEnabled && !string.IsNullOrEmpty(s.PixFmt))
                filters.Add($"format={s.PixFmt}");

            // 变速 (视频)
            if (s.SpeedEnabled && Math.Abs(s.SpeedFactor - 1.0) > 0.001)
                filters.Add($"setpts={1.0 / s.SpeedFactor}*PTS");

            // 烧录字幕
            if (s.SubtitleEnabled && !string.IsNullOrEmpty(s.SubtitlePath))
            {
                string subPath = PathHelper.EscapeForFilter(s.SubtitlePath);
                filters.Add($"subtitles='{subPath}'");
            }

            return string.Join(",", filters);
        }

        private string AtempoFilter(double factor)
        {
            if (factor <= 0) return "";
            // atempo 范围 0.5~2.0，超过需要级联
            var factors = new List<double>();
            double remain = factor;
            while (remain > 2.0)
            {
                factors.Add(2.0);
                remain /= 2.0;
            }
            while (remain < 0.5)
            {
                factors.Add(0.5);
                remain /= 0.5;
            }
            if (Math.Abs(remain - 1.0) > 0.001)
                factors.Add(remain);
            if (factors.Count == 0)
                return "";
            return string.Join(",", factors.ConvertAll(f => $"atempo={f.ToString(CultureInfo.InvariantCulture)}"));
        }
    }
}
