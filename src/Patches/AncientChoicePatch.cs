using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Runs;
using StsCompanion.Models;
using StsCompanion.UI;

namespace StsCompanion.Patches;

[HarmonyPatch(typeof(NAncientEventLayout), nameof(NAncientEventLayout.OnSetupComplete))]
public static class AncientChoicePatch
{
    [HarmonyPostfix]
    public static void Postfix(NAncientEventLayout __instance)
    {
        try
        {
            if (Plugin.CurrentConfig?.OverlayEnabled != true) return;

            Plugin.Log("AncientChoicePatch: OnSetupComplete fired, delaying...");

            // Delay to let buttons get created and laid out
            _ = DelayedSetup(__instance);
        }
        catch (Exception ex)
        {
            Plugin.Log($"AncientChoicePatch error: {ex.Message}");
        }
    }

    private static async Task DelayedSetup(NAncientEventLayout screen)
    {
        await screen.ToSignal(screen.GetTree().CreateTimer(1.0), SceneTreeTimer.SignalName.Timeout);

        if (!GodotObject.IsInstanceValid(screen)) return;

        var buttons = screen.OptionButtons.ToList();
        Plugin.Log($"AncientChoicePatch: Found {buttons.Count} buttons after delay");
        if (buttons.Count == 0) return;

        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null) return;

        var player = LocalContext.GetMe(runState);
        if (player == null) return;

        var characterId = "CHARACTER." + player.Character.Id.Entry;
        var actIndex = runState.CurrentActIndex;

        var candidates = new List<string>();
        foreach (var button in buttons)
        {
            var rawKey = button.Option?.TextKey ?? "";
            // Strip prefix: "NEOW.pages.INITIAL.options.SMALL_CAPSULE" → "SMALL_CAPSULE"
            var textKey = rawKey.Contains('.') ? rawKey[(rawKey.LastIndexOf('.') + 1)..] : rawKey;
            Plugin.Log($"  Button rawKey={rawKey} textKey={textKey} size={button.Size} pos={button.Position}");
            if (!string.IsNullOrEmpty(textKey))
                candidates.Add(textKey);
        }

        if (candidates.Count == 0) return;

        Plugin.Log($"Ancient choice opened! Character={characterId} Act={actIndex} Options={string.Join(", ", candidates)}");

        _ = FetchAndShowAncientScores(screen, buttons, characterId, actIndex, candidates.ToArray());
    }

    private static async Task FetchAndShowAncientScores(
        NAncientEventLayout screen,
        List<NEventOptionButton> buttons,
        string characterId,
        int actIndex,
        string[] candidates)
    {
        var request = new AncientRequest
        {
            Character = characterId,
            ActIndex = actIndex,
            Candidates = candidates
        };

        var response = await HttpService.GetAncientScores(request);

        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(screen)) return;
            if (response?.Recommendations == null || response.Recommendations.Length == 0) return;

            var recByKey = new Dictionary<string, AncientRecommendation>();
            foreach (var rec in response.Recommendations)
                recByKey[rec.TextKey] = rec;

            var bestWinRate = response.Recommendations.Where(r => r.HasData).Select(r => r.WinRate).DefaultIfEmpty(0).Max();

            foreach (var button in buttons)
            {
                if (!GodotObject.IsInstanceValid(button)) continue;

                var rawKey = button.Option?.TextKey ?? "";
                var textKey = rawKey.Contains('.') ? rawKey[(rawKey.LastIndexOf('.') + 1)..] : rawKey;
                if (string.IsNullOrEmpty(textKey) || !recByKey.TryGetValue(textKey, out var rec)) continue;

                var isBest = rec.HasData && rec.WinRate >= bestWinRate - 0.1;
                var scoreText = rec.HasData ? $"{rec.WinRate:F1}%" : "?";
                var color = GetWinRateColor(rec.WinRate, rec.HasData);

                var badge = CreateAncientBadge(scoreText, color, isBest, rec);

                // Disable clipping up the tree so badge isn't cut off
                button.ClipContents = false;
                var ancestor = button.GetParent() as Control;
                while (ancestor != null && ancestor != screen)
                {
                    ancestor.ClipContents = false;
                    ancestor = ancestor.GetParent() as Control;
                }

                button.AddChild(badge);
                badge.Position = new Vector2(-badge.GetMinimumSize().X - 10, (button.Size.Y - badge.GetMinimumSize().Y) / 2);
            }
        }).CallDeferred();
    }

    private static PanelContainer CreateAncientBadge(string scoreText, Color scoreColor, bool isBest, AncientRecommendation rec)
    {
        var panel = new PanelContainer();
        panel.MouseFilter = Control.MouseFilterEnum.Ignore;

        var bg = new StyleBoxFlat();
        bg.BgColor = new Color(0.03f, 0.03f, 0.08f, 0.85f);
        bg.BorderColor = isBest ? new Color(0.9f, 0.75f, 0.35f) : new Color(0.4f, 0.4f, 0.5f, 0.5f);
        bg.SetBorderWidthAll(isBest ? 2 : 1);
        bg.ContentMarginLeft = 8;
        bg.ContentMarginRight = 8;
        bg.ContentMarginTop = 3;
        bg.ContentMarginBottom = 3;
        bg.SetCornerRadiusAll(4);
        panel.AddThemeStyleboxOverride("panel", bg);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 6);
        panel.AddChild(hbox);

        var bestMark = isBest ? " \u2605" : "";
        var scoreLabel = new Label();
        scoreLabel.Text = $"WR: {scoreText}{bestMark}";
        scoreLabel.AddThemeColorOverride("font_color", scoreColor);
        scoreLabel.AddThemeFontSizeOverride("font_size", 14);
        hbox.AddChild(scoreLabel);

        if (rec.HasData)
        {
            var pickLabel = new Label();
            pickLabel.Text = $"Pick: {rec.PickRate:F0}%";
            pickLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.65f));
            pickLabel.AddThemeFontSizeOverride("font_size", 11);
            hbox.AddChild(pickLabel);
        }

        return panel;
    }

    private static Color GetWinRateColor(double winRate, bool hasData)
    {
        if (!hasData) return new Color(0.5f, 0.5f, 0.55f);
        return winRate switch
        {
            >= 30 => new Color(0.3f, 0.85f, 0.4f),
            >= 20 => new Color(0.95f, 0.85f, 0.3f),
            _ => new Color(0.9f, 0.3f, 0.3f)
        };
    }
}
