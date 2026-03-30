using System.Text.Json.Serialization;

namespace StsCompanion.Models;

public sealed class SyncResponse
{
    [JsonPropertyName("imported")]
    public int Imported { get; set; }

    [JsonPropertyName("skipped")]
    public int Skipped { get; set; }

    [JsonPropertyName("errors")]
    public string[] Errors { get; set; } = [];
}
