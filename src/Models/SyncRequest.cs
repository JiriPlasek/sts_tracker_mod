using System.Text.Json.Serialization;

namespace StsCompanion.Models;

public sealed class SyncRequest
{
    [JsonPropertyName("files")]
    public SyncFile[] Files { get; set; } = [];
}

public sealed class SyncFile
{
    [JsonPropertyName("filename")]
    public string Filename { get; set; } = "";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}
