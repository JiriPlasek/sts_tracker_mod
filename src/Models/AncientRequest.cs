using System.Text.Json.Serialization;

namespace StsCompanion.Models;

public class AncientRequest
{
    [JsonPropertyName("character")]
    public string Character { get; set; } = "";

    [JsonPropertyName("actIndex")]
    public int ActIndex { get; set; }

    [JsonPropertyName("candidates")]
    public string[] Candidates { get; set; } = [];
}

public class AncientResponse
{
    [JsonPropertyName("recommendations")]
    public AncientRecommendation[] Recommendations { get; set; } = [];
}

public class AncientRecommendation
{
    [JsonPropertyName("textKey")]
    public string TextKey { get; set; } = "";

    [JsonPropertyName("pickRate")]
    public double PickRate { get; set; }

    [JsonPropertyName("winRate")]
    public double WinRate { get; set; }

    [JsonPropertyName("timesOffered")]
    public int TimesOffered { get; set; }

    [JsonPropertyName("timesPicked")]
    public int TimesPicked { get; set; }

    [JsonPropertyName("hasData")]
    public bool HasData { get; set; }
}
