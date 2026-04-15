using System.Text.Json.Serialization;

namespace StsCompanion.Models;

public sealed class RecommendRequest
{
    [JsonPropertyName("character")]
    public string Character { get; set; } = "";

    [JsonPropertyName("deck")]
    public string[] Deck { get; set; } = [];

    [JsonPropertyName("candidates")]
    public string[] Candidates { get; set; } = [];

    [JsonPropertyName("candidateUpgrades")]
    public int[]? CandidateUpgrades { get; set; }

    [JsonPropertyName("floor")]
    public int Floor { get; set; }

    [JsonPropertyName("ascension")]
    public int Ascension { get; set; }

    [JsonPropertyName("deckFloors")]
    public int[]? DeckFloors { get; set; }

    [JsonPropertyName("proMode")]
    public bool ProMode { get; set; }
}
