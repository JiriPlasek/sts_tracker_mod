using System.Text.Json.Serialization;

namespace StsCompanion.Models;

public class ShopScoreRequest
{
    [JsonPropertyName("character")]
    public string Character { get; set; } = "";

    [JsonPropertyName("relicIds")]
    public string[]? RelicIds { get; set; }

    [JsonPropertyName("potionIds")]
    public string[]? PotionIds { get; set; }
}

public class ShopScoreResponse
{
    [JsonPropertyName("relics")]
    public ShopRelicScore[] Relics { get; set; } = [];

    [JsonPropertyName("potions")]
    public ShopPotionScore[] Potions { get; set; } = [];
}

public class ShopRelicScore
{
    [JsonPropertyName("relicId")]
    public string RelicId { get; set; } = "";

    [JsonPropertyName("winRate")]
    public double WinRate { get; set; }

    [JsonPropertyName("winRateDelta")]
    public double WinRateDelta { get; set; }

    [JsonPropertyName("timesAcquired")]
    public int TimesAcquired { get; set; }

    [JsonPropertyName("hasData")]
    public bool HasData { get; set; }
}

public class ShopPotionScore
{
    [JsonPropertyName("potionId")]
    public string PotionId { get; set; } = "";

    [JsonPropertyName("pickRate")]
    public double PickRate { get; set; }

    [JsonPropertyName("timesOffered")]
    public int TimesOffered { get; set; }

    [JsonPropertyName("timesPicked")]
    public int TimesPicked { get; set; }

    [JsonPropertyName("hasData")]
    public bool HasData { get; set; }
}
