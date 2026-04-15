using System.Text.Json.Serialization;

namespace StsCompanion.Models;

public sealed class RecommendResponse
{
    [JsonPropertyName("recommendations")]
    public Recommendation[] Recommendations { get; set; } = [];
}

public sealed class Recommendation
{
    [JsonPropertyName("cardId")]
    public string CardId { get; set; } = "";

    [JsonPropertyName("total")]
    public double Total { get; set; }

    [JsonPropertyName("hasData")]
    public bool HasData { get; set; }

    [JsonPropertyName("powerScore")]
    public PowerScoreComponent PowerScore { get; set; } = new();

    [JsonPropertyName("synergy")]
    public SynergyScore Synergy { get; set; } = new();

    [JsonPropertyName("countAdjust")]
    public CountAdjustScore CountAdjust { get; set; } = new();

    [JsonPropertyName("upgrade")]
    public UpgradeScore Upgrade { get; set; } = new();
}

public sealed class PowerScoreComponent
{
    [JsonPropertyName("score")]
    public double Score { get; set; }

    [JsonPropertyName("raw")]
    public double? Raw { get; set; }

    [JsonPropertyName("tier")]
    public string? Tier { get; set; }
}

public sealed class SynergyScore
{
    [JsonPropertyName("score")]
    public double Score { get; set; }

    [JsonPropertyName("topPairs")]
    public SynergyPair[] TopPairs { get; set; } = [];
}

public sealed class SynergyPair
{
    [JsonPropertyName("cardId")]
    public string CardId { get; set; } = "";

    [JsonPropertyName("pairWinRate")]
    public double PairWinRate { get; set; }
}

public sealed class CountAdjustScore
{
    [JsonPropertyName("score")]
    public double Score { get; set; }

    [JsonPropertyName("currentCopies")]
    public int CurrentCopies { get; set; }

    [JsonPropertyName("newCopies")]
    public int NewCopies { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}

public sealed class UpgradeScore
{
    [JsonPropertyName("score")]
    public double Score { get; set; }

    [JsonPropertyName("delta")]
    public double? Delta { get; set; }

    [JsonPropertyName("isUpgraded")]
    public bool IsUpgraded { get; set; }
}
