using System.Collections.Generic;
using FFLiteGUI.Models;

namespace FFLiteGUI.Strategies
{
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
                string bitrate = settings.BitrateVideo.Trim();
                if (int.TryParse(bitrate, out _))
                    bitrate += "k";
                parts.Add($"-b:v {bitrate}");
            }
            return string.Join(" ", parts);
        }
    }
}
