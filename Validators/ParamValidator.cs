using System.Collections.Generic;
using FFLiteGUI.Models;

namespace FFLiteGUI.Validators
{
    public static class ParamValidator
    {
        public static bool ValidateCrf(int crf, string encoder)
        {
            if (encoder.Contains("libx264") || encoder.Contains("libx265") ||
                encoder.Contains("libvpx") || encoder.Contains("libsvtav1"))
            {
                return crf >= 0 && crf <= 51;
            }
            return true;
        }

        public static bool ValidateBitrate(string bitrate, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(bitrate))
            {
                error = "比特率不能为空";
                return false;
            }
            string clean = bitrate.Trim().ToLower();
            if (clean.EndsWith("k"))
                clean = clean.Substring(0, clean.Length - 1);
            if (int.TryParse(clean, out _))
                return true;
            error = "比特率格式错误，应为纯数字或数字+k (如 1900 或 1900k)";
            return false;
        }

        public static List<string> ValidateSettings(VideoSettings settings)
        {
            var errors = new List<string>();

            switch (settings.RateControlType)
            {
                case "crf":
                    if (!ValidateCrf(settings.CrfValue, settings.Encoder))
                        errors.Add("CRF 值必须在 0~51 之间");
                    break;
                case "cq":
                    if (settings.CqValue < 0 || settings.CqValue > 51)
                        errors.Add("CQ 值必须在 0~51 之间");
                    break;
                case "global_quality":
                    if (settings.GlobalQuality < 1 || settings.GlobalQuality > 51)
                        errors.Add("Global Quality 值必须在 1~51 之间");
                    break;
                case "bitrate":
                    if (!ValidateBitrate(settings.BitrateVideo, out string bitErr))
                        errors.Add(bitErr);
                    break;
            }

            if (!string.IsNullOrEmpty(settings.AudioBitrate))
                ValidateBitrate(settings.AudioBitrate, out string audioErr);

            return errors;
        }
    }
}
