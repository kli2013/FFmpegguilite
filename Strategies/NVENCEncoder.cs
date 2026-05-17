public class NVENCEncoder : IEncoderStrategy
{
    public string BuildVideoParams(VideoSettings settings)
    {
        var parts = new List<string>();
        parts.Add($"-c:v {settings.Encoder} -preset {settings.Preset}");
        if (settings.RateControlType == "cq")
            parts.Add($"-cq {settings.CqValue}");
        else if (settings.RateControlType == "bitrate")
        {
            string bit = settings.BitrateVideo.Trim();
            if (int.TryParse(bit, out _)) bit += "k";
            parts.Add($"-b:v {bit}");
        }
        return string.Join(" ", parts);
    }
}
