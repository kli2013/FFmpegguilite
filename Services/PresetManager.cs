using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using FFLiteGUI.Models;

namespace FFLiteGUI.Services
{
    public class PresetManager
    {
        private readonly string _presetFile;

        public PresetManager(string presetFilePath)
        {
            _presetFile = presetFilePath;
        }

        public Dictionary<string, VideoSettings> LoadPresets()
        {
            if (!File.Exists(_presetFile))
                return new Dictionary<string, VideoSettings>();

            try
            {
                string json = File.ReadAllText(_presetFile);
                return JsonConvert.DeserializeObject<Dictionary<string, VideoSettings>>(json) ?? new Dictionary<string, VideoSettings>();
            }
            catch
            {
                return new Dictionary<string, VideoSettings>();
            }
        }

        public void SavePresets(Dictionary<string, VideoSettings> presets)
        {
            try
            {
                string dir = Path.GetDirectoryName(_presetFile);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                string json = JsonConvert.SerializeObject(presets, Formatting.Indented);
                File.WriteAllText(_presetFile, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"保存预设失败: {ex.Message}", ex);
            }
        }
    }
}
