using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StsCompanion;

public sealed class Config
{
    [JsonPropertyName("apiToken")]
    public string ApiToken { get; set; } = "";

    [JsonPropertyName("apiUrl")]
    public string ApiUrl { get; set; } = "https://ststracker.app";

    [JsonPropertyName("overlayEnabled")]
    public bool OverlayEnabled { get; set; } = true;

    [JsonPropertyName("autoUploadRuns")]
    public bool AutoUploadRuns { get; set; } = true;

    [JsonPropertyName("syncActiveRun")]
    public bool SyncActiveRun { get; set; } = true;

    [JsonPropertyName("badgeScale")]
    public float BadgeScale { get; set; } = 1.0f;

    [JsonPropertyName("tooltipScale")]
    public float TooltipScale { get; set; } = 1.0f;

    public static Config? Load()
    {
        var assemblyDir = Path.GetDirectoryName(typeof(Config).Assembly.Location);
        if (assemblyDir == null) return null;

        var configPath = Path.Combine(assemblyDir, "sts_companion_config.cfg");
        if (!File.Exists(configPath))
        {
            // Create default config for user to fill in
            var defaultConfig = new Config();
            var defaultJson = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, defaultJson);
            Plugin.Log($"Created default sts_companion_config.cfg at {configPath}");
            return defaultConfig;
        }

        try
        {
            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<Config>(json);
        }
        catch (JsonException ex)
        {
            Plugin.Log($"Failed to parse sts_companion_config.cfg: {ex.Message}");
            return null;
        }
    }
}
