namespace FFLiteGUI.Models
{
    public class AudioSettings
    {
        public bool Enabled { get; set; } = true;
        public bool OnlyAudio { get; set; }
        public string Codec { get; set; } = "aac";
        public string Bitrate { get; set; } = "128k";
        public string Samplerate { get; set; } = "44100";
        public string Format { get; set; } = "mp3";   // 仅提取音频时的容器格式

        public AudioSettings()
        {
        }

        public AudioSettings(AudioSettings other)
        {
            Enabled = other.Enabled;
            OnlyAudio = other.OnlyAudio;
            Codec = other.Codec;
            Bitrate = other.Bitrate;
            Samplerate = other.Samplerate;
            Format = other.Format;
        }
    }
}
