using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Run history deck panel: separate main / extra / side / trunk sections for YgoDuelist.</summary>
public static class YgoDeckHistoryDisplay
{
    private const string ExtraSectionName = "YgoDeckHistoryExtra";
    private const string SideSectionName = "YgoDeckHistorySide";
    private const string TrunkSectionName = "YgoDeckHistoryTrunk";
    private const string CardContainerNodeName = "CardContainer";

    private static readonly FieldInfo AllCardsField =
        AccessTools.Field(typeof(NDeckHistory), "_allCards")!;

    public static void LoadSplit(
        NDeckHistory screen,
        Player player,
        YgoSerializableDeckLists.SplitResult split)
    {
        ClearExtraSections(screen);

        MegaRichTextLabel themeHeader = screen.GetNode<MegaRichTextLabel>("Header");
        MarginContainer themeMargin = screen.GetNode<MarginContainer>("MarginContainer");
        Control mainContainer = screen.GetNode<Control>("%CardContainer");

        var inspectCards = new List<CardModel>();
        ApplySection(screen, player, themeHeader, mainContainer, split.Main, "YGODUELIST-RUN_HISTORY.deck_header", inspectCards);

        if (split.Extra.Count > 0)
            AddSection(screen, player, ExtraSectionName, split.Extra, "YGODUELIST-RUN_HISTORY.extra_deck_header", inspectCards, themeHeader, themeMargin);

        if (split.Side.Count > 0)
            AddSection(screen, player, SideSectionName, split.Side, "YGODUELIST-RUN_HISTORY.side_deck_header", inspectCards, themeHeader, themeMargin);

        if (split.Trunk.Count > 0)
            AddSection(screen, player, TrunkSectionName, split.Trunk, "YGODUELIST-RUN_HISTORY.trunk_header", inspectCards, themeHeader, themeMargin);

        var allCards = (List<CardModel>)AllCardsField.GetValue(screen)!;
        allCards.Clear();
        allCards.AddRange(inspectCards);
    }

    private static void ClearExtraSections(NDeckHistory screen)
    {
        foreach (string name in new[] { ExtraSectionName, SideSectionName, TrunkSectionName })
        {
            Node? node = screen.GetNodeOrNull(name);
            node?.QueueFreeSafely();
        }
    }

    private static void AddSection(
        NDeckHistory screen,
        Player player,
        string sectionName,
        List<SerializableCard> cards,
        string headerLocKey,
        List<CardModel> inspectCards,
        MegaRichTextLabel themeHeader,
        MarginContainer themeMargin)
    {
        var section = new VBoxContainer { Name = sectionName };
        screen.AddChildSafely(section);

        MegaRichTextLabel header = CreateSectionHeader(themeHeader);
        section.AddChildSafely(header);

        MarginContainer margin = CreateSectionMargin(themeMargin);
        section.AddChildSafely(margin);

        var cardContainer = new HFlowContainer { Name = CardContainerNodeName };
        margin.AddChildSafely(cardContainer);

        ApplySection(screen, player, header, cardContainer, cards, headerLocKey, inspectCards);
    }

    private static MegaRichTextLabel CreateSectionHeader(MegaRichTextLabel source)
    {
        var header = new MegaRichTextLabel
        {
            Name = "Header",
            ClipContents = source.ClipContents,
            CustomMinimumSize = source.CustomMinimumSize,
            BbcodeEnabled = source.BbcodeEnabled,
            ScrollActive = source.ScrollActive,
            AutowrapMode = source.AutowrapMode,
            AutoSizeEnabled = source.AutoSizeEnabled,
            MinFontSize = source.MinFontSize,
            MaxFontSize = source.MaxFontSize,
            MouseFilter = source.MouseFilter,
        };
        CopyRichTextLabelTheme(source, header);
        return header;
    }

    private static MarginContainer CreateSectionMargin(MarginContainer source)
    {
        var margin = new MarginContainer
        {
            Name = "MarginContainer",
            MouseFilter = source.MouseFilter,
        };
        CopyThemeConstantOverride(source, margin, "margin_left");
        CopyThemeConstantOverride(source, margin, "margin_top");
        CopyThemeConstantOverride(source, margin, "margin_right");
        CopyThemeConstantOverride(source, margin, "margin_bottom");
        return margin;
    }

    private static void CopyRichTextLabelTheme(MegaRichTextLabel source, MegaRichTextLabel target)
    {
        CopyThemeFontOverride(source, target, ThemeConstants.RichTextLabel.NormalFont);
        CopyThemeFontOverride(source, target, ThemeConstants.RichTextLabel.BoldFont);
        CopyThemeFontOverride(source, target, ThemeConstants.RichTextLabel.ItalicsFont);

        CopyThemeFontSizeOverride(source, target, ThemeConstants.RichTextLabel.NormalFontSize);
        CopyThemeFontSizeOverride(source, target, ThemeConstants.RichTextLabel.BoldFontSize);
        CopyThemeFontSizeOverride(source, target, ThemeConstants.RichTextLabel.BoldItalicsFontSize);
        CopyThemeFontSizeOverride(source, target, ThemeConstants.RichTextLabel.ItalicsFontSize);
        CopyThemeFontSizeOverride(source, target, ThemeConstants.RichTextLabel.MonoFontSize);

        CopyThemeColorOverride(source, target, ThemeConstants.RichTextLabel.DefaultColor);
        CopyThemeColorOverride(source, target, ThemeConstants.RichTextLabel.FontShadowColor);
        CopyThemeConstantOverride(source, target, "shadow_offset_x");
        CopyThemeConstantOverride(source, target, "shadow_offset_y");
    }

    private static void CopyThemeFontOverride(Control source, Control target, StringName name)
    {
        if (source.HasThemeFontOverride(name))
            target.AddThemeFontOverride(name, source.GetThemeFont(name));
    }

    private static void CopyThemeFontSizeOverride(Control source, Control target, StringName name)
    {
        if (source.HasThemeFontSizeOverride(name))
            target.AddThemeFontSizeOverride(name, source.GetThemeFontSize(name));
    }

    private static void CopyThemeColorOverride(Control source, Control target, StringName name)
    {
        if (source.HasThemeColorOverride(name))
            target.AddThemeColorOverride(name, source.GetThemeColor(name));
    }

    private static void CopyThemeConstantOverride(Control source, Control target, StringName name)
    {
        if (source.HasThemeConstantOverride(name))
            target.AddThemeConstantOverride(name, source.GetThemeConstant(name));
    }

    private static void ApplySection(
        NDeckHistory screen,
        Player player,
        MegaRichTextLabel headerLabel,
        Control cardContainer,
        List<SerializableCard> cards,
        string headerLocKey,
        List<CardModel> inspectCards)
    {
        var header = new LocString("combat_messages", headerLocKey);
        header.Add("totalCards", cards.Count);

        var categories = new LocString("run_history", "DECK_HISTORY.categories");
        var rarityCounts = new Dictionary<CardRarity, int>();
        foreach (CardRarity rarity in Enum.GetValues<CardRarity>())
            rarityCounts[rarity] = 0;

        foreach (SerializableCard card in cards)
        {
            CardRarity rarity = SaveUtil.CardOrDeprecated(card.Id).Rarity;
            rarityCounts[rarity]++;
        }

        foreach (KeyValuePair<CardRarity, int> entry in rarityCounts)
            categories.Add(entry.Key + "Cards", entry.Value);

        var sb = new StringBuilder();
        sb.Append("[gold][b]");
        sb.Append(header.GetFormattedText());
        sb.Append("[/b][/gold]");
        sb.Append(categories.GetFormattedText().Trim(','));
        headerLabel.Text = sb.ToString();

        foreach (Node child in cardContainer.GetChildren())
            child.QueueFreeSafely();

        foreach (IGrouping<SerializableCard, SerializableCard> group in cards.GroupBy(c => c))
        {
            CardModel cardModel = CardModel.FromSerializable(group.Key);
            cardModel.Owner = player;
            inspectCards.Add(cardModel);

            NDeckHistoryEntry entry = NDeckHistoryEntry.Create(
                cardModel,
                group.Count(),
                group.Where(c => c.FloorAddedToDeck.HasValue).Select(c => c.FloorAddedToDeck!.Value));

            entry.Connect(NDeckHistoryEntry.SignalName.Clicked, Callable.From<NDeckHistoryEntry>(OnEntryClicked));
            entry.Connect(
                NClickableControl.SignalName.Focused,
                Callable.From<NClickableControl>(_ => screen.EmitSignal(NDeckHistory.SignalName.Hovered, entry)));
            entry.Connect(
                NClickableControl.SignalName.Unfocused,
                Callable.From<NClickableControl>(_ => screen.EmitSignal(NDeckHistory.SignalName.Unhovered, entry)));

            cardContainer.AddChildSafely(entry);
        }

        void OnEntryClicked(NDeckHistoryEntry clicked)
        {
            List<CardModel> pile = (List<CardModel>)AllCardsField.GetValue(screen)!;
            NGame.Instance.GetInspectCardScreen().Open(pile, pile.IndexOf(clicked.Card));
        }
    }
}
