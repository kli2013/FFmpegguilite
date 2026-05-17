public class PresetManager
{
    private readonly string presetFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FFLiteGUI", "presets.json");

    public Dictionary<string, VideoSettings> LoadPresets()
    {
        if (!File.Exists(presetFile)) return new Dictionary<string, VideoSettings>();
        string json = File.ReadAllText(presetFile);
        return JsonConvert.DeserializeObject<Dictionary<string, VideoSettings>>(json);
    }

    public void SavePreset(string name, VideoSettings settings)
    {
        var presets = LoadPresets();
        presets[name] = settings;
        string json = JsonConvert.SerializeObject(presets, Formatting.Indented);
        File.WriteAllText(presetFile, json);
    }
}
