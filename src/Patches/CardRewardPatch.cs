using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
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

            var deckCards = player.Deck.Cards.ToArray();
            var deck = deckCards.Select(c => "CARD." + c.Id.Entry).ToArray();
            var deckFloors = deckCards.Select(c => c.FloorAddedToDeck ?? 0).ToArray();

            var proMode = ModConfigBridge.GetValue("proMode", Plugin.CurrentConfig?.ProMode ?? false);
            var request = new RecommendRequest
            {
                Character = characterId,
                Deck = deck,
                DeckFloors = deckFloors,
                Candidates = candidateIds,
                CandidateUpgrades = candidateUpgrades,
                Floor = floor,
                Ascension = ascension,
                ProMode = proMode
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
        ShopScoreHelper.ScoreShop(__instance);
    }
}

// Re-score shop when returning from a sub-screen (card removal, relic card pick, etc.)
[HarmonyPatch(typeof(NMerchantInventory), "OnActiveScreenUpdated")]
public static class ShopRefocusPatch
{
    [HarmonyPostfix]
    public static void Postfix(NMerchantInventory __instance)
    {
        try
        {
            // Only re-score if the shop is the active screen again
            var isCurrent = MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext.ActiveScreenContext.Instance.IsCurrent(__instance);
            if (!isCurrent) return;

            Plugin.Log("Shop regained focus — re-scoring");
            ShopScoreHelper.ScoreShop(__instance);
        }
        catch (Exception ex)
        {
            Plugin.Log($"ShopRefocusPatch error: {ex.Message}");
        }
    }
}

public static class ShopScoreHelper
{
    public static void ScoreShop(NMerchantInventory shop)
    {
        try
        {
            if (Plugin.CurrentConfig?.OverlayEnabled != true) return;

            // Clear any previous shop badges
            ScoreOverlay.Instance?.Hide();
            ScoreOverlay.Instance?.HideShopItems();

            var ids = new List<string>();
            var upgrades = new List<int>();

            var charCards = shop.GetNodeOrNull<Control>("%CharacterCards");
            var colorlessCards = shop.GetNodeOrNull<Control>("%ColorlessCards");

            CollectMerchantCards(charCards, ids, upgrades);
            CollectMerchantCards(colorlessCards, ids, upgrades);

            if (ids.Count == 0) return;

            Plugin.Log($"Shop scoring {ids.Count} cards: {string.Join(", ", ids)}");
            CardScoreHelper.RequestScores(shop, ids.ToArray(), upgrades.ToArray());

            _ = RequestShopRelicPotionScores(shop);
        }
        catch (Exception ex)
        {
            Plugin.Log($"ShopScoreHelper error: {ex.Message}");
        }
    }

    private static async Task RequestShopRelicPotionScores(NMerchantInventory shop)
    {
        try
        {
            var runState = RunManager.Instance.DebugOnlyGetState();
            if (runState == null) return;
            var player = LocalContext.GetMe(runState);
            if (player == null) return;
            var characterId = "CHARACTER." + player.Character.Id.Entry;

            var relicContainer = shop.GetNodeOrNull<Control>("%Relics");
            var potionContainer = shop.GetNodeOrNull<Control>("%Potions");

            var relicIds = new List<string>();
            var relicNodes = new List<Control>();
            if (relicContainer != null)
            {
                foreach (var child in relicContainer.GetChildren())
                {
                    if (child is NMerchantRelic merchantRelic)
                    {
                        var entry = merchantRelic.Entry as MerchantRelicEntry;
                        if (entry?.Model != null)
                        {
                            relicIds.Add("RELIC." + entry.Model.Id.Entry);
                            relicNodes.Add(merchantRelic);
                        }
                    }
                }
            }

            var potionIds = new List<string>();
            var potionNodes = new List<Control>();
            if (potionContainer != null)
            {
                foreach (var child in potionContainer.GetChildren())
                {
                    if (child is NMerchantPotion merchantPotion)
                    {
                        var entry = merchantPotion.Entry as MerchantPotionEntry;
                        if (entry?.Model != null)
                        {
                            potionIds.Add("POTION." + entry.Model.Id.Entry);
                            potionNodes.Add(merchantPotion);
                        }
                    }
                }
            }

            if (relicIds.Count == 0 && potionIds.Count == 0) return;

            Plugin.Log($"Shop relics: {string.Join(", ", relicIds)} | potions: {string.Join(", ", potionIds)}");

            var request = new ShopScoreRequest
            {
                Character = characterId,
                RelicIds = relicIds.Count > 0 ? relicIds.ToArray() : null,
                PotionIds = potionIds.Count > 0 ? potionIds.ToArray() : null
            };

            var response = await HttpService.GetShopScores(request);
            if (response == null) return;

            Callable.From(() =>
            {
                if (!GodotObject.IsInstanceValid(shop)) return;

                // Attach relic scores
                var relicLookup = new Dictionary<string, ShopRelicScore>();
                foreach (var r in response.Relics) relicLookup[r.RelicId] = r;

                for (var i = 0; i < relicNodes.Count; i++)
                {
                    if (!GodotObject.IsInstanceValid(relicNodes[i])) continue;
                    if (!relicLookup.TryGetValue(relicIds[i], out var score)) continue;

                    var text = score.HasData ? $"{(score.WinRateDelta >= 0 ? "+" : "")}{score.WinRateDelta:F1}%" : "?";
                    var color = !score.HasData ? new Color(0.5f, 0.5f, 0.55f)
                        : score.WinRateDelta >= 3 ? new Color(0.3f, 0.85f, 0.4f)
                        : score.WinRateDelta >= 0 ? new Color(0.95f, 0.85f, 0.3f)
                        : new Color(0.9f, 0.3f, 0.3f);

                    var badge = ScoreOverlay.Instance!.CreateShopBadge(text, color);
                    relicNodes[i].AddChild(badge);
                    badge.Position = new Vector2(20, 100);
                }

                // Attach potion scores
                var potionLookup = new Dictionary<string, ShopPotionScore>();
                foreach (var p in response.Potions) potionLookup[p.PotionId] = p;

                for (var i = 0; i < potionNodes.Count; i++)
                {
                    if (!GodotObject.IsInstanceValid(potionNodes[i])) continue;
                    if (!potionLookup.TryGetValue(potionIds[i], out var score)) continue;

                    var text = score.HasData ? $"{score.PickRate:F0}%" : "?";
                    var color = !score.HasData ? new Color(0.5f, 0.5f, 0.55f)
                        : score.PickRate >= 50 ? new Color(0.3f, 0.85f, 0.4f)
                        : score.PickRate >= 25 ? new Color(0.95f, 0.85f, 0.3f)
                        : new Color(0.9f, 0.3f, 0.3f);

                    var badge = ScoreOverlay.Instance!.CreateShopBadge(text, color);
                    potionNodes[i].AddChild(badge);
                    badge.Position = new Vector2(10, 90);
                }
            }).CallDeferred();
        }
        catch (Exception ex)
        {
            Plugin.Log($"Shop relic/potion scoring error: {ex.Message}");
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

// ── Grid-based card selection (e.g., "choose 2 out of 5" in '?' rooms) ──

[HarmonyPatch(typeof(NSimpleCardSelectScreen), nameof(NSimpleCardSelectScreen.AfterOverlayOpened))]
public static class SimpleCardSelectPatch
{
    internal static bool _scored;

    [HarmonyPostfix]
    public static void Postfix(NSimpleCardSelectScreen __instance)
    {
        try
        {
            if (Plugin.CurrentConfig?.OverlayEnabled != true) return;
            if (_scored) return; // AfterOverlayShown can fire multiple times

            // Access _cards from the base class via Harmony traverse
            var cards = Traverse.Create(__instance).Field<IReadOnlyList<CardModel>>("_cards").Value;
            if (cards == null || cards.Count == 0) return;

            _scored = true;

            var ids = cards.Select(c => "CARD." + c.Id.Entry).ToArray();
            var upgrades = cards.Select(c => c.IsUpgraded ? 1 : 0).ToArray();

            Plugin.Log($"Grid card selection opened! Cards={string.Join(", ", ids)}");

            // Delay to let the grid finish its async init + animate-in
            _ = DelayedRequestScores(__instance, ids, upgrades);
        }
        catch (Exception ex)
        {
            Plugin.Log($"SimpleCardSelectPatch error: {ex.Message}");
        }
    }

    private static async Task DelayedRequestScores(Control screen, string[] ids, int[] upgrades)
    {
        await screen.ToSignal(screen.GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
        CardScoreHelper.RequestScores(screen, ids, upgrades);
    }
}

[HarmonyPatch(typeof(NCardGridSelectionScreen), nameof(NCardGridSelectionScreen._ExitTree))]
public static class SimpleCardSelectClosePatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        SimpleCardSelectPatch._scored = false;
        ScoreOverlay.Instance?.Hide();
    }
}
