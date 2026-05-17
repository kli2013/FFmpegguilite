using FFLiteGUI.Strategies;

namespace FFLiteGUI.Strategies
{
    public static class EncoderStrategyFactory
    {
        public static IEncoderStrategy GetStrategy(string encoder)
        {
            if (string.IsNullOrEmpty(encoder))
                return new SoftwareEncoder();

            string enc = encoder.ToLower();
            if (enc.Contains("libx") || enc.Contains("libsvtav1") || enc.Contains("libvpx") || enc.Contains("mpeg4") || enc.Contains("libxvid") || enc.Contains("libtheora"))
                return new SoftwareEncoder();
            if (enc.Contains("nvenc"))
                return new NVENCEncoder();
            if (enc.Contains("qsv"))
                return new QSVEncoder();
            return new OtherEncoder();
        }
    }
}
