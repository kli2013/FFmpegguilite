public class VideoSettings
{
    // 视频编码
    public string Encoder { get; set; } = "libx265";
    public string Preset { get; set; } = "medium";
    public string RateControlType { get; set; } = "crf";
    public int CrfValue { get; set; } = 25;
    public int CqValue { get; set; } = 35;
    public int GlobalQuality { get; set; } = 25;
    public string BitrateVideo { get; set; } = "1900k";

    // 滤镜
    public bool ScaleEnabled { get; set; }
    public string ScaleWidth { get; set; } = "";
    public string ScaleHeight { get; set; } = "";
    public string ScaleMethod { get; set; } = "width";   // width/height/exact

    public bool CropEnabled { get; set; }
    public string CropLeft { get; set; } = "0";
    public string CropTop { get; set; } = "0";
    public string CropWidth { get; set; } = "iw/2";
    public string CropHeight { get; set; } = "ih";

    public string Rotate { get; set; } = "none";   // none/90/180/270
    public bool Vflip { get; set; }
    public bool Hflip { get; set; }

    public bool SpeedEnabled { get; set; }
    public double SpeedFactor { get; set; } = 1.0;

    public string DeinterlaceFilter { get; set; } = "none";

    public bool PixFmtEnabled { get; set; } = true;
    public string PixFmt { get; set; } = "yuv420p";

    public bool SubtitleEnabled { get; set; }
    public string SubtitlePath { get; set; } = "";

    public bool TrimEnabled { get; set; }
    public string TrimStart { get; set; } = "0";
    public string TrimEnd { get; set; } = "";

    // 帧率
    public string FrameRateType { get; set; } = "keep";
    public string FrameRateCustom { get; set; } = "30";

    // 硬件解码
    public bool HwaccelEnabled { get; set; }
    public string HwaccelDecoder { get; set; } = "none";

    // 自定义参数
    public string CustomArgs { get; set; } = "";

    // 输出设置
    public string OutputDir { get; set; } = "";
    public string OutputSuffix { get; set; } = "";
    public string CustomOutputName { get; set; } = "";
    public string OutputContainer { get; set; } = "mp4";

    // 音频设置
    public bool AudioEnabled { get; set; } = true;
    public bool OnlyAudio { get; set; }
    public string AudioCodec { get; set; } = "aac";
    public string AudioBitrate { get; set; } = "128k";
    public string AudioSamplerate { get; set; } = "44100";
    public string AudioFormat { get; set; } = "mp3";   // only audio 时使用
}
