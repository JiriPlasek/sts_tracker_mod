using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Runs;
using StsCompanion.Models;
using StsCompanion.UI;

namespace StsCompanion.Patches;

/// <summary>
/// Shared logic for requesting recommendations and showing scores on any card selection screen.
/// </summary>
public static class CardScoreHelper
{
    private static Control? _currentScreen;
    private static string[]? _currentCandidateIds;

    public static void RequestScores(Control screen, string[] candidateIds, int[] candidateUpgrades)
    {
        try
        {
            if (Plugin.CurrentConfig?.OverlayEnabled != true) return;

            _currentScreen = screen;
            _currentCandidateIds = candidateIds;

            var runState = RunManager.Instance.DebugOnlyGetState();
            if (runState == null) return;

            var player = LocalContext.GetMe(runState);
            if (player == null) return;

            var characterId = "CHARACTER." + player.Character.Id.Entry;
            var floor = runState.TotalFloor;
            var ascension = runState.AscensionLevel;

            var deck = player.Deck.Cards
                .Select(c => "CARD." + c.Id.Entry)
                .ToArray();

            var request = new RecommendRequest
            {
                Character = characterId,
                Deck = deck,
                Candidates = candidateIds,
                CandidateUpgrades = candidateUpgrades,
                Floor = floor,
                Ascension = ascension
            };

            Plugin.Log($"Card selection opened! Character={characterId} Floor={floor} Candidates={string.Join(", ", candidateIds)}");

            ScoreOverlay.Instance?.ShowLoading(screen, candidateIds.Length);
            _ = FetchAndShow(request);
        }
        catch (Exception ex)
        {
            Plugin.Log($"CardScoreHelper error: {ex.Message}");
        }
    }

    private static async Task FetchAndShow(RecommendRequest request)
    {
        var response = await HttpService.GetRecommendations(request);

        if (response?.Recommendations != null)
        {
            Plugin.Log($"Got {response.Recommendations.Length} recommendations:");
            foreach (var rec in response.Recommendations)
            {
                Plugin.Log($"  {rec.CardId}: total={rec.Total:F1} hasData={rec.HasData}");
            }
        }

        Callable.From(() =>
        {
            if (_currentScreen == null || !GodotObject.IsInstanceValid(_currentScreen)) return;

            if (response?.Recommendations == null || response.Recommendations.Length == 0)
            {
                ScoreOverlay.Instance?.Hide();
                return;
            }

            ScoreOverlay.Instance?.ShowScores(_currentScreen, _currentCandidateIds!, response.Recommendations);
        }).CallDeferred();
    }
}

// ── Post-combat card reward ──

[HarmonyPatch(typeof(NCardRewardSelectionScreen), nameof(NCardRewardSelectionScreen.ShowScreen))]
public static class CardRewardShowPatch
{
    [HarmonyPostfix]
    public static void Postfix(
        NCardRewardSelectionScreen? __result,
        IReadOnlyList<CardCreationResult> options,
        IReadOnlyList<CardRewardAlternative> extraOptions)
    {
        if (__result == null) return;

        var ids = options.Select(o => "CARD." + o.Card.Id.Entry).ToArray();
        var upgrades = options.Select(o => o.Card.IsUpgraded ? 1 : 0).ToArray();
        CardScoreHelper.RequestScores(__result, ids, upgrades);
    }
}

[HarmonyPatch(typeof(NCardRewardSelectionScreen), "SelectCard")]
public static class CardRewardSelectPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        ScoreOverlay.Instance?.Hide();
    }
}

// ── Event card choices (e.g., "choose a card to add") ──

[HarmonyPatch(typeof(NChooseACardSelectionScreen), nameof(NChooseACardSelectionScreen.ShowScreen))]
public static class EventCardChoicePatch
{
    [HarmonyPostfix]
    public static void Postfix(
        NChooseACardSelectionScreen? __result,
        IReadOnlyList<CardModel> cards,
        bool canSkip)
    {
        if (__result == null) return;

        var ids = cards.Select(c => "CARD." + c.Id.Entry).ToArray();
        var upgrades = cards.Select(c => c.IsUpgraded ? 1 : 0).ToArray();
        CardScoreHelper.RequestScores(__result, ids, upgrades);
    }
}

// NChooseACardSelectionScreen cleanup: overlay hides when the screen is freed (_ExitTree)
[HarmonyPatch(typeof(NChooseACardSelectionScreen), nameof(NChooseACardSelectionScreen._ExitTree))]
public static class EventCardClosePatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        ScoreOverlay.Instance?.Hide();
    }
}

// ── Shop ──

[HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.Open))]
public static class ShopCardPatch
{
    [HarmonyPostfix]
    public static void Postfix(NMerchantInventory __instance)
    {
        try
        {
            if (Plugin.CurrentConfig?.OverlayEnabled != true) return;

            var ids = new List<string>();
            var upgrades = new List<int>();

            // NMerchantCard slots are in %CharacterCards and %ColorlessCards
            var charCards = __instance.GetNodeOrNull<Control>("%CharacterCards");
            var colorlessCards = __instance.GetNodeOrNull<Control>("%ColorlessCards");

            CollectMerchantCards(charCards, ids, upgrades);
            CollectMerchantCards(colorlessCards, ids, upgrades);

            if (ids.Count == 0) return;

            Plugin.Log($"Shop opened with {ids.Count} cards: {string.Join(", ", ids)}");
            CardScoreHelper.RequestScores(__instance, ids.ToArray(), upgrades.ToArray());
        }
        catch (Exception ex)
        {
            Plugin.Log($"ShopCardPatch error: {ex.Message}");
        }
    }

    private static void CollectMerchantCards(Control? container, List<string> ids, List<int> upgrades)
    {
        if (container == null) return;

        foreach (var child in container.GetChildren())
        {
            if (child is NMerchantCard merchantCard)
            {
                var entry = merchantCard.Entry as MerchantCardEntry;
                var card = entry?.CreationResult?.Card;
                if (card != null)
                {
                    ids.Add("CARD." + card.Id.Entry);
                    upgrades.Add(card.IsUpgraded ? 1 : 0);
                }
            }
        }
    }
}
