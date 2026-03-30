using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using StsCompanion.Models;

namespace StsCompanion.UI;

public sealed class ScoreOverlay
{
    public static ScoreOverlay? Instance { get; private set; }

    private readonly List<Control> _nodes = new();
    private PanelContainer? _tooltip;

    public static void Create()
    {
        Instance = new ScoreOverlay();
    }

    public void ShowLoading(Control screen, int cardCount)
    {
        Hide();

        var containers = FindCardContainers(screen);
        if (containers.Count == 0) return;

        Callable.From(() =>
        {
            var holders = containers.SelectMany(CollectCardHolders).ToList();
            for (var i = 0; i < holders.Count && i < cardCount; i++)
            {
                var badge = CreateScoreBadge("...", Colors.White, false, null);
                AttachToCard(holders[i], badge);
            }
        }).CallDeferred();
    }

    public void ShowScores(Control screen, string[] candidateIds, Recommendation[] recommendations)
    {
        Hide();

        var containers = FindCardContainers(screen);
        if (containers.Count == 0) return;

        // Build lookup: cardId → recommendation
        var recByCard = new Dictionary<string, Recommendation>();
        foreach (var rec in recommendations)
            recByCard[rec.CardId] = rec;

        var bestScore = recommendations.Length > 0 ? recommendations.Max(r => r.Total) : 0;

        Callable.From(() =>
        {
            var holders = containers.SelectMany(CollectCardHolders).ToList();

            // Match holders to candidates by position order (candidateIds matches holder order)
            for (var i = 0; i < holders.Count && i < candidateIds.Length; i++)
            {
                if (!recByCard.TryGetValue(candidateIds[i], out var rec)) continue;

                var isBest = rec.Total >= bestScore - 0.01;
                var scoreColor = GetScoreColor(rec.Total);
                var scoreText = rec.HasData ? $"{rec.Total:F1}" : "?";

                var badge = CreateScoreBadge(scoreText, scoreColor, isBest, rec);
                AttachToCard(holders[i], badge);
            }
        }).CallDeferred();
    }

    public void Hide()
    {
        HideTooltip();
        foreach (var node in _nodes)
        {
            if (GodotObject.IsInstanceValid(node))
            {
                node.GetParent()?.RemoveChild(node);
                node.QueueFree();
            }
        }
        _nodes.Clear();
    }

    /// <summary>
    /// Finds the card row container in different screen types.
    /// NCardRewardSelectionScreen uses "UI/CardRow", NChooseACardSelectionScreen uses "CardRow",
    /// NMerchantInventory uses "%CharacterCards" and "%ColorlessCards" (returned as parent wrapper).
    /// </summary>
    /// <summary>
    /// Returns the container(s) holding card slots, or null.
    /// For shop, returns a list since cards span two containers.
    /// </summary>
    private static List<Control> FindCardContainers(Control screen)
    {
        // Card reward: UI/CardRow
        var cardRow = screen.GetNodeOrNull<Control>("UI/CardRow");
        if (cardRow != null) return new List<Control> { cardRow };

        // Event choose-a-card: CardRow
        cardRow = screen.GetNodeOrNull<Control>("CardRow");
        if (cardRow != null) return new List<Control> { cardRow };

        // Shop: %CharacterCards and %ColorlessCards
        var containers = new List<Control>();
        var charCards = screen.GetNodeOrNull<Control>("%CharacterCards");
        if (charCards != null) containers.Add(charCards);
        var colorlessCards = screen.GetNodeOrNull<Control>("%ColorlessCards");
        if (colorlessCards != null) containers.Add(colorlessCards);
        if (containers.Count > 0) return containers;

        return new List<Control>();
    }

    /// <summary>
    /// Collects card slot nodes — NGridCardHolder (rewards, events) or NMerchantCard (shop).
    /// </summary>
    private static List<Control> CollectCardHolders(Control container)
    {
        // Try NGridCardHolder first (card rewards, events)
        var holders = container.GetChildren().OfType<NGridCardHolder>().Cast<Control>().ToList();
        if (holders.Count > 0) return holders;

        // Try NMerchantCard (shop) — check child containers
        var merchantCards = new List<Control>();
        foreach (var child in container.GetChildren())
        {
            if (child is NMerchantCard mc)
            {
                merchantCards.Add(mc);
            }
            else if (child is Control ctrl)
            {
                merchantCards.AddRange(ctrl.GetChildren().OfType<NMerchantCard>().Cast<Control>());
                merchantCards.AddRange(ctrl.GetChildren().OfType<NGridCardHolder>().Cast<Control>());
            }
        }
        if (merchantCards.Count > 0) return merchantCards;

        // Recursive fallback
        var result = new List<Control>();
        foreach (var child in container.GetChildren())
        {
            if (child is NGridCardHolder h) result.Add(h);
            else if (child is NMerchantCard m) result.Add(m);
        }
        return result;
    }

    private PanelContainer CreateScoreBadge(string scoreText, Color scoreColor, bool isBest, Recommendation? rec)
    {
        var panel = new PanelContainer();
        panel.MouseFilter = Control.MouseFilterEnum.Stop;

        var bg = new StyleBoxFlat();
        bg.BgColor = new Color(0.03f, 0.03f, 0.08f, 0.9f);
        bg.BorderColor = isBest ? new Color(0.9f, 0.75f, 0.35f) : new Color(0.4f, 0.4f, 0.5f, 0.6f);
        bg.SetBorderWidthAll(isBest ? 2 : 1);
        bg.ContentMarginLeft = 12;
        bg.ContentMarginRight = 12;
        bg.ContentMarginTop = 4;
        bg.ContentMarginBottom = 4;
        bg.SetCornerRadiusAll(4);
        panel.AddThemeStyleboxOverride("panel", bg);

        var label = new Label();
        var bestMark = isBest ? " ★" : "";
        label.Text = $"{scoreText}{bestMark}";
        label.AddThemeColorOverride("font_color", scoreColor);
        label.AddThemeFontSizeOverride("font_size", 18);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        panel.AddChild(label);

        if (rec != null)
        {
            panel.MouseEntered += () => ShowTooltip(panel, rec);
            panel.MouseExited += () => HideTooltip();
        }

        return panel;
    }

    private void ShowTooltip(Control anchor, Recommendation rec)
    {
        HideTooltip();

        _tooltip = new PanelContainer();
        _tooltip.MouseFilter = Control.MouseFilterEnum.Ignore;

        var bg = new StyleBoxFlat();
        bg.BgColor = new Color(0.02f, 0.02f, 0.06f, 0.95f);
        bg.BorderColor = new Color(0.5f, 0.45f, 0.3f);
        bg.SetBorderWidthAll(1);
        bg.ContentMarginLeft = 10;
        bg.ContentMarginRight = 10;
        bg.ContentMarginTop = 8;
        bg.ContentMarginBottom = 8;
        bg.SetCornerRadiusAll(4);
        _tooltip.AddThemeStyleboxOverride("panel", bg);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 3);
        _tooltip.AddChild(vbox);

        var title = new Label();
        title.Text = FormatCardId(rec.CardId);
        title.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        title.AddThemeFontSizeOverride("font_size", 14);
        vbox.AddChild(title);

        AddScoreRow(vbox, "Baseline", rec.Baseline.Score);
        AddScoreRow(vbox, "Archetype", rec.Archetype.Score);
        AddScoreRow(vbox, "Synergy", rec.Synergy.Score);
        AddScoreRow(vbox, "Copies", rec.CountAdjust.Score);
        AddScoreRow(vbox, "Upgrade", rec.Upgrade.Score);

        if (rec.Archetype.MatchedArchetype != null)
        {
            var archLabel = new Label();
            archLabel.Text = $"Archetype: {rec.Archetype.MatchedArchetype}";
            archLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.55f));
            archLabel.AddThemeFontSizeOverride("font_size", 10);
            vbox.AddChild(archLabel);
        }

        if (rec.CountAdjust.Note != null)
        {
            var noteLabel = new Label();
            noteLabel.Text = rec.CountAdjust.Note;
            noteLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.55f));
            noteLabel.AddThemeFontSizeOverride("font_size", 10);
            vbox.AddChild(noteLabel);
        }

        var parent = anchor.GetParent();
        if (parent != null)
        {
            parent.AddChild(_tooltip);
            _tooltip.Position = anchor.Position + new Vector2(-20, -170);
            _nodes.Add(_tooltip);
        }
    }

    private void HideTooltip()
    {
        if (_tooltip != null && GodotObject.IsInstanceValid(_tooltip))
        {
            _tooltip.GetParent()?.RemoveChild(_tooltip);
            _tooltip.QueueFree();
            _nodes.Remove(_tooltip);
        }
        _tooltip = null;
    }

    private static void AddScoreRow(VBoxContainer parent, string name, double score)
    {
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 6);

        var nameLabel = new Label();
        nameLabel.Text = name;
        nameLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.75f));
        nameLabel.AddThemeFontSizeOverride("font_size", 12);
        nameLabel.CustomMinimumSize = new Vector2(70, 0);
        hbox.AddChild(nameLabel);

        var scoreLabel = new Label();
        scoreLabel.Text = $"{score:F1}";
        scoreLabel.AddThemeColorOverride("font_color", GetScoreColor(score));
        scoreLabel.AddThemeFontSizeOverride("font_size", 12);
        hbox.AddChild(scoreLabel);

        parent.AddChild(hbox);
    }

    private void AttachToCard(Control holder, PanelContainer badge)
    {
        var parent = holder.GetParent();
        if (parent == null) return;

        parent.AddChild(badge);
        _nodes.Add(badge);

        // Different offset for shop cards (smaller, have price tag) vs reward cards (larger)
        if (holder is NMerchantCard)
            badge.Position = holder.Position + new Vector2(40, 140);
        else
            badge.Position = holder.Position + new Vector2(-40, 220);
    }

    private static string FormatCardId(string id)
    {
        var name = id.Contains('.') ? id[(id.IndexOf('.') + 1)..] : id;
        return string.Join(" ", name.Split('_').Select(w =>
            w.Length > 0 ? char.ToUpper(w[0]) + w[1..].ToLower() : w));
    }

    private static Color GetScoreColor(double score) => score switch
    {
        >= 6.5 => new Color(0.3f, 0.85f, 0.4f),
        >= 4.0 => new Color(0.95f, 0.85f, 0.3f),
        _ => new Color(0.9f, 0.3f, 0.3f)
    };
}
