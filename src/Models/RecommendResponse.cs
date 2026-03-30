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

    [JsonPropertyName("baseline")]
    public BaselineScore Baseline { get; set; } = new();

    [JsonPropertyName("archetype")]
    public ArchetypeScore Archetype { get; set; } = new();

    [JsonPropertyName("synergy")]
    public SynergyScore Synergy { get; set; } = new();

    [JsonPropertyName("countAdjust")]
    public CountAdjustScore CountAdjust { get; set; } = new();

    [JsonPropertyName("upgrade")]
    public UpgradeScore Upgrade { get; set; } = new();
}

public sealed class BaselineScore
{
    [JsonPropertyName("score")]
    public double Score { get; set; }

    [JsonPropertyName("winDelta")]
    public double WinDelta { get; set; }

    [JsonPropertyName("pickRate")]
    public double PickRate { get; set; }

    [JsonPropertyName("timingScore")]
    public double TimingScore { get; set; }
}

public sealed class ArchetypeScore
{
    [JsonPropertyName("score")]
    public double Score { get; set; }

    [JsonPropertyName("matchedArchetype")]
    public string? MatchedArchetype { get; set; }

    [JsonPropertyName("archetypeWinRate")]
    public double? ArchetypeWinRate { get; set; }
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
