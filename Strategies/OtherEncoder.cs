using FFLiteGUI.Models;

namespace FFLiteGUI.Strategies
{
    public class OtherEncoder : IEncoderStrategy
    {
        public string BuildVideoParams(VideoSettings settings)
        {
            var parts = new System.Collections.Generic.List<string>();
            parts.Add($"-c:v {settings.Encoder}");
            
            string bitrate = settings.BitrateVideo.Trim();
            if (int.TryParse(bitrate, out _))
                bitrate += "k";
            parts.Add($"-b:v {bitrate}");
            
            return string.Join(" ", parts);
        }
    }
}
