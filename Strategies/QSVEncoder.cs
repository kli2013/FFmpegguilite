using FFLiteGUI.Models;

namespace FFLiteGUI.Strategies
{
    public class QSVEncoder : IEncoderStrategy
    {
        public string BuildVideoParams(VideoSettings settings)
        {
            var parts = new System.Collections.Generic.List<string>();
            parts.Add($"-c:v {settings.Encoder} -preset {settings.Preset}");
            
            string rc = settings.RateControlType;
            if (rc == "global_quality")
            {
                parts.Add($"-global_quality {settings.GlobalQuality}");
            }
            else if (rc == "bitrate")
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
