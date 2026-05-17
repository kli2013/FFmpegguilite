namespace FFLiteGUI.Models
{
    public class FilterSettings
    {
        // 帧率
        public string FrameRateType { get; set; } = "keep";   // keep / custom
        public string FrameRateCustom { get; set; } = "30";

        // 缩放
        public bool ScaleEnabled { get; set; }
        public string ScaleWidth { get; set; } = "";
        public string ScaleHeight { get; set; } = "";
        public string ScaleMethod { get; set; } = "width";    // width / height / exact

        // 裁剪
        public bool CropEnabled { get; set; }
        public string CropLeft { get; set; } = "0";
        public string CropTop { get; set; } = "0";
        public string CropWidth { get; set; } = "iw/2";
        public string CropHeight { get; set; } = "ih";

        // 旋转
        public string Rotate { get; set; } = "none";   // none / 90 / 180 / 270

        // 翻转
        public bool Vflip { get; set; }
        public bool Hflip { get; set; }

        // 变速
        public bool SpeedEnabled { get; set; }
        public double SpeedFactor { get; set; } = 1.0;

        // 反交错
        public string DeinterlaceFilter { get; set; } = "none";

        // 像素格式
        public bool PixFmtEnabled { get; set; } = true;
        public string PixFmt { get; set; } = "yuv420p";

        // 烧录字幕
        public bool SubtitleEnabled { get; set; }
        public string SubtitlePath { get; set; } = "";

        // 截取片段
        public bool TrimEnabled { get; set; }
        public string TrimStart { get; set; } = "0";
        public string TrimEnd { get; set; } = "";

        public FilterSettings()
        {
        }

        public FilterSettings(FilterSettings other)
        {
            FrameRateType = other.FrameRateType;
            FrameRateCustom = other.FrameRateCustom;
            ScaleEnabled = other.ScaleEnabled;
            ScaleWidth = other.ScaleWidth;
            ScaleHeight = other.ScaleHeight;
            ScaleMethod = other.ScaleMethod;
            CropEnabled = other.CropEnabled;
            CropLeft = other.CropLeft;
            CropTop = other.CropTop;
            CropWidth = other.CropWidth;
            CropHeight = other.CropHeight;
            Rotate = other.Rotate;
            Vflip = other.Vflip;
            Hflip = other.Hflip;
            SpeedEnabled = other.SpeedEnabled;
            SpeedFactor = other.SpeedFactor;
            DeinterlaceFilter = other.DeinterlaceFilter;
            PixFmtEnabled = other.PixFmtEnabled;
            PixFmt = other.PixFmt;
            SubtitleEnabled = other.SubtitleEnabled;
            SubtitlePath = other.SubtitlePath;
            TrimEnabled = other.TrimEnabled;
            TrimStart = other.TrimStart;
            TrimEnd = other.TrimEnd;
        }
    }
}
