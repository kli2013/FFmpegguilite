public class FFmpegCommandBuilder
{
    private readonly string ffmpegPath;
    public FFmpegCommandBuilder(string ffmpegExePath) => ffmpegPath = ffmpegExePath;

    public string BuildCommand(string inputFile, string outputFile, VideoSettings settings)
    {
        var args = new List<string>();
        args.Add($"-y");
        args.Add($"-fflags +genpts");

        // 截取片段（非仅音频模式）
        if (!settings.OnlyAudio && settings.TrimEnabled)
        {
            if (!string.IsNullOrEmpty(settings.TrimStart))
                args.Add($"-ss {settings.TrimStart}");
            if (!string.IsNullOrEmpty(settings.TrimEnd))
                args.Add($"-to {settings.TrimEnd}");
        }

        // 硬件解码
        if (!settings.OnlyAudio && settings.HwaccelEnabled && settings.HwaccelDecoder != "none")
        {
            string decoder = HardwareDecoderHelper.MapDecoder(settings.HwaccelDecoder);
            if (decoder.StartsWith("-hwaccel"))
                args.Add(decoder);
            else if (decoder.Contains("_cuvid") || decoder.Contains("_qsv"))
                args.AddRange(new[] { "-c:v", decoder });
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
            args.Add(strategy.BuildVideoParams(settings));
        }

        // 音频处理
        if (!settings.AudioEnabled)
            args.Add("-an");
        else
        {
            if (settings.AudioCodec == "copy")
            {
                if (settings.SpeedEnabled && settings.SpeedFactor != 1.0)
                {
                    // 需要重编码以应用 atempo
                    args.Add("-c:a aac");
                    args.Add($"-b:a {settings.AudioBitrate}");
                    args.Add($"-ar {settings.AudioSamplerate}");
                    args.Add($"-af \"atempo={settings.SpeedFactor.ToString(System.Globalization.CultureInfo.InvariantCulture)}\"");
                }
                else
                    args.Add("-c:a copy");
            }
            else
            {
                args.Add($"-c:a {settings.AudioCodec}");
                args.Add($"-b:a {settings.AudioBitrate}");
                args.Add($"-ar {settings.AudioSamplerate}");
                if (settings.SpeedEnabled && settings.SpeedFactor != 1.0)
                    args.Add($"-af \"atempo={settings.SpeedFactor.ToString(System.Globalization.CultureInfo.InvariantCulture)}\"");
            }
        }

        // 自定义参数
        if (!string.IsNullOrEmpty(settings.CustomArgs))
            args.Add(settings.CustomArgs);

        // 容器优化
        if (!settings.OnlyAudio && (settings.OutputContainer == "mp4" || settings.OutputContainer == "mov"))
            args.Add("-movflags +faststart");

        args.Add($"\"{PathHelper.Normalize(outputFile)}\"");
        return $"\"{ffmpegPath}\" {string.Join(" ", args)}";
    }

    private string BuildFilterChain(VideoSettings s)
    {
        var filters = new List<string>();
        if (s.CropEnabled && !string.IsNullOrEmpty(s.CropWidth) && !string.IsNullOrEmpty(s.CropHeight))
            filters.Add($"crop={s.CropWidth}:{s.CropHeight}:{s.CropLeft}:{s.CropTop}");
        if (s.ScaleEnabled)
        {
            if (s.ScaleMethod == "width" && int.TryParse(s.ScaleWidth, out int w))
                filters.Add($"scale={w}:-2");
            else if (s.ScaleMethod == "height" && int.TryParse(s.ScaleHeight, out int h))
                filters.Add($"scale=-2:{h}");
            else if (s.ScaleMethod == "exact" && int.TryParse(s.ScaleWidth, out int ew) && int.TryParse(s.ScaleHeight, out int eh))
                filters.Add($"scale={ew}:{eh}");
        }
        if (s.Rotate == "90") filters.Add("transpose=1");
        else if (s.Rotate == "180") filters.Add("transpose=2,transpose=2");
        else if (s.Rotate == "270") filters.Add("transpose=2");
        if (s.Vflip) filters.Add("vflip");
        if (s.Hflip) filters.Add("hflip");
        if (s.DeinterlaceFilter != "none") filters.Add(s.DeinterlaceFilter);
        if (s.PixFmtEnabled) filters.Add($"format={s.PixFmt}");
        if (s.SpeedEnabled && s.SpeedFactor != 1.0)
            filters.Add($"setpts={1.0 / s.SpeedFactor}*PTS");
        if (s.SubtitleEnabled && !string.IsNullOrEmpty(s.SubtitlePath))
        {
            string subPath = PathHelper.EscapeForFilter(s.SubtitlePath);
            filters.Add($"subtitles='{subPath}'");
        }
        return string.Join(",", filters);
    }
}
