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
using YgoDuelist.YgoDuelistCode.Services;

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

        // Snapshot empty layout before the main section is populated (duplicate after that would copy card rows).
        var headerPrototype = (MegaRichTextLabel)screen.GetNode<MegaRichTextLabel>("Header").Duplicate();
        var marginPrototype = (MarginContainer)screen.GetNode<MarginContainer>("MarginContainer").Duplicate();
        ClearCardContainer(marginPrototype);

        MegaRichTextLabel mainHeader = screen.GetNode<MegaRichTextLabel>("Header");
        Control mainContainer = screen.GetNode<Control>("%CardContainer");

        var inspectCards = new List<CardModel>();
        ApplySection(screen, player, mainHeader, mainContainer, split.Main, "YGODUELIST-RUN_HISTORY.deck_header", inspectCards);

        if (split.Extra.Count > 0)
            AddSection(screen, player, ExtraSectionName, split.Extra, "YGODUELIST-RUN_HISTORY.extra_deck_header", inspectCards, headerPrototype, marginPrototype);

        if (split.Side.Count > 0)
            AddSection(screen, player, SideSectionName, split.Side, "YGODUELIST-RUN_HISTORY.side_deck_header", inspectCards, headerPrototype, marginPrototype);

        if (split.Trunk.Count > 0)
            AddSection(screen, player, TrunkSectionName, split.Trunk, "YGODUELIST-RUN_HISTORY.trunk_header", inspectCards, headerPrototype, marginPrototype);

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
        MegaRichTextLabel headerPrototype,
        MarginContainer marginPrototype)
    {
        var section = new VBoxContainer { Name = sectionName };
        screen.AddChildSafely(section);

        var header = (MegaRichTextLabel)headerPrototype.Duplicate();
        header.Name = "Header";
        section.AddChildSafely(header);

        var margin = (MarginContainer)marginPrototype.Duplicate();
        margin.Name = "MarginContainer";
        section.AddChildSafely(margin);

        Control cardContainer = margin.GetNode<Control>(CardContainerNodeName);
        ApplySection(screen, player, header, cardContainer, cards, headerLocKey, inspectCards);
    }

    private static void ClearCardContainer(MarginContainer margin)
    {
        Node container = margin.GetNode(CardContainerNodeName);
        foreach (Node child in container.GetChildren())
            child.QueueFreeSafely();
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
