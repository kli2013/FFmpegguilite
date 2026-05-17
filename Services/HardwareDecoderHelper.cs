using System.Collections.Generic;

namespace FFLiteGUI.Services
{
    public static class HardwareDecoderHelper
    {
        private static readonly Dictionary<string, string> DecoderMap = new Dictionary<string, string>
        {
            {"无", "none"},
            {"auto (自动通用)", "auto"},
            {"cuda (NVIDIA通用)", "cuda"},
            {"h264_cuvid (NVIDIA H.264)", "h264_cuvid"},
            {"hevc_cuvid (NVIDIA HEVC)", "hevc_cuvid"},
            {"vp9_cuvid (NVIDIA VP9)", "vp9_cuvid"},
            {"av1_cuvid (NVIDIA AV1)", "av1_cuvid"},
            {"qsv (Intel通用)", "qsv"},
            {"h264_qsv (Intel H.264)", "h264_qsv"},
            {"hevc_qsv (Intel HEVC)", "hevc_qsv"},
            {"vaapi (Linux VAAPI)", "vaapi"},
            {"videotoolbox (macOS)", "videotoolbox"}
        };

        public static string MapDecoder(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                return "none";

            if (DecoderMap.TryGetValue(displayName, out string decoder))
                return decoder;

            return "none";
        }

        public static string[] GetDecoderOptions()
        {
            return new[]
            {
                "无",
                "auto (自动通用)",
                "cuda (NVIDIA通用)",
                "h264_cuvid (NVIDIA H.264)",
                "hevc_cuvid (NVIDIA HEVC)",
                "vp9_cuvid (NVIDIA VP9)",
                "av1_cuvid (NVIDIA AV1)",
                "qsv (Intel通用)",
                "h264_qsv (Intel H.264)",
                "hevc_qsv (Intel HEVC)",
                "vaapi (Linux VAAPI)",
                "videotoolbox (macOS)"
            };
        }

        public static string BuildHwaccelArgs(string decoderDisplay)
        {
            string decoderKey = MapDecoder(decoderDisplay);
            if (decoderKey == "none")
                return "";

            switch (decoderKey)
            {
                case "auto":
                    return "-hwaccel auto";
                case "cuda":
                    return "-hwaccel cuda -hwaccel_output_format cuda";
                case "qsv":
                    return "-hwaccel qsv -hwaccel_output_format qsv";
                case "vaapi":
                    return "-hwaccel vaapi -hwaccel_output_format vaapi";
                case "videotoolbox":
                    return "-hwaccel videotoolbox";
                case "h264_cuvid":
                case "hevc_cuvid":
                case "vp9_cuvid":
                case "av1_cuvid":
                case "h264_qsv":
                case "hevc_qsv":
                    return $"-c:v {decoderKey}";
                default:
                    return "";
            }
        }
    }
}
