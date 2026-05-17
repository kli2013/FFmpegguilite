using System.Collections.Generic;

namespace FFLiteGUI.Models
{
    public class TrackInfo
    {
        public int Index { get; set; }
        public string Type { get; set; }      // "video", "audio", "subtitle"
        public string Codec { get; set; }
        public string FilePath { get; set; }
        public bool Enabled { get; set; }
        public Dictionary<string, string> EncSettings { get; set; }

        // 画中画相关（仅视频）
        public bool OverlayEnabled { get; set; }
        public string OverlayX { get; set; }
        public string OverlayY { get; set; }
        public bool PadEnabled { get; set; }
        public string PadWidth { get; set; }
        public string PadHeight { get; set; }
        public string OffsetX { get; set; }
        public string OffsetY { get; set; }

        public TrackInfo()
        {
            Enabled = true;
            EncSettings = new Dictionary<string, string>();
            OverlayEnabled = true;
            OverlayX = "W-w-10";
            OverlayY = "H-h-10";
            PadEnabled = false;
            PadWidth = "";
            PadHeight = "";
            OffsetX = "0";
            OffsetY = "0";
        }

        public TrackInfo(int index, string type, string codec, string filePath, bool enabled)
            : this()
        {
            Index = index;
            Type = type;
            Codec = codec;
            FilePath = filePath;
            Enabled = enabled;
        }

        public bool IsEncoding()
        {
            if (EncSettings == null) return false;
            return EncSettings.TryGetValue("encoder", out string encoder) && encoder != "copy";
        }
    }
}
