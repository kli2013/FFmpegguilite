public static class EncoderStrategyFactory
{
    public static IEncoderStrategy GetStrategy(string encoder)
    {
        if (encoder.Contains("libx") || encoder.Contains("libsvtav1"))
            return new SoftwareEncoder();
        if (encoder.Contains("nvenc"))
            return new NVENCEncoder();
        if (encoder.Contains("qsv"))
            return new QSVEncoder();
        return new OtherEncoder();
    }
}
