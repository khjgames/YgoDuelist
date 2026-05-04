using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Cyber_Jar : EffectMonsterCard, IMonsterFlipEffect
{
    private static readonly LocString FlipEffectHoverTitle = new("card_keywords", "20041.title");
    private static readonly LocString FlipRevealPreviewPrompt =
        new("cards", "YGODUELIST-CYBER_JAR.flip_preview.selection");

    public Cyber_Jar()
        : base(
            cost: 3,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 9,
            baseDef: 9,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Rock)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | FusionMonsterCard.PackTagsForFusionProfile(DuelMonsterAttribute, DuelMonsterRace);

    protected override bool StumblingBlocksHandSummonInAttackPosition => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            LocString flipDesc = new("cards", "YGODUELIST-CYBER_JAR.flip_effect.description");
            yield return new HoverTip(FlipEffectHoverTitle, flipDesc);
        }
    }

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        Player? player = Owner;
        if (player?.PlayerCombatState == null)
            return;

        var pcs = player.PlayerCombatState;
        List<Creature> duelPets = YgoMpCombatOrder.PetsSnapshotAliveDuelMonstersOrderedByCombatId(pcs);
        YgoMpDiagnostics.VerbosePrint(
            "CyberJar",
            $"OnFlippedFaceUp kill order ownerNet={player.NetId} combatIds=[{string.Join(",", duelPets.ConvertAll(p => p.CombatId.ToString()))}]");
        foreach (Creature pet in YgoDuelMonsterDestructionRules.FilterPetsForMassKill(
                     duelPets,
                     YgoDestructionSourceKind.MonsterEffect))
            await YgoDuelMonsterDestructionRules.KillPetWithinDestructionAsync(
                YgoDestructionSourceKind.MonsterEffect,
                pet);

        int revealCount = IsUpgraded ? 6 : 5;
        CardPile draw = pcs.DrawPile;
        CardPile discard = pcs.DiscardPile;
        CardPile? hand = YgoPlayerPiles.Hand(player);

        var revealed = new List<CardModel>();
        for (int i = 0; i < revealCount; i++)
        {
            await CardPileCmd.ShuffleIfNecessary(choiceContext, player);
            if (draw.IsEmpty)
                break;
            CardModel? top = draw.Cards.Count > 0 ? draw.Cards[0] : null;
            if (top == null)
                break;
            revealed.Add(top);
            await CardPileCmd.Add(top, discard, CardPilePosition.Top, top, false);
        }

        if (revealed.Count > 0)
        {
            var prefs = new CardSelectorPrefs(FlipRevealPreviewPrompt, 0, 0)
            {
                RequireManualConfirmation = true,
                Cancelable = false
            };
            try
            {
                YgoMonsterFormPreviewContext.RestrictMonsterToggleToAttackDefenseOnly = true;
                await YgoPreviewGridSelection.ShowPreviewAsync(choiceContext, revealed, player, prefs);
            }
            finally
            {
                YgoMonsterFormPreviewContext.RestrictMonsterToggleToAttackDefenseOnly = false;
            }
        }

        foreach (CardModel card in revealed)
        {
            if (card is BaseMonsterCard bm
                && bm.DuelMonsterLevel <= 4
                && bm.CanSummonDuelMonster
                && DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            {
                bool summoned = await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, bm, choiceContext);
                if (summoned)
                {
                    Creature? pet = YgoMpCombatOrder.FirstPetWhere(
                        pcs,
                        p => p.IsAlive && DuelMonsterFieldRegistry.HasSourceCard(p, bm));
                    if (pet != null)
                        MonsterCommandRegistry.GetOrCreate(pet).ZeroEnergyMonsterCommandsThisTurn = true;
                }
                else if (hand != null)
                    await CardPileCmd.Add(card, hand, CardPilePosition.Top, card, false);
            }
            else if (hand != null)
                await CardPileCmd.Add(card, hand, CardPilePosition.Top, card, false);
        }
    }

    protected override void OnUpgrade() => base.OnUpgrade();
}
