using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Penguin_Soldier : EffectMonsterCard, IMonsterFlipEffect
{
    private const string ConduitImgBbcode = "[img]res://YgoDuelist/images/card_frames/conduit_icon.png[/img]";
    private static readonly LocString FlipPromptMulti = new("cards", "YGODUELIST-PENGUIN_SOLDIER.flip_return_up_to_2");
    private static readonly LocString FlipPromptSingle = new("cards", "YGODUELIST-PENGUIN_SOLDIER.flip_return_one");

    public Penguin_Soldier()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 7,
            baseDef: 5,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Aqua)
    {
    }

    public override bool UseAlternateUpgradedDescription => true;

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Water;

    public override Type[] RelatedCards => new[] { typeof(Penguin_Soldier) };

    public override int GetDuelMonsterDefensePlayEnergy(bool upgradedOrPreview)
    {
        int n = base.GetDuelMonsterDefensePlayEnergy(upgradedOrPreview);
        return upgradedOrPreview ? Math.Max(0, n - 1) : n;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new EnergyVar(0) }.Concat(base.CanonicalVars);

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Penguin_Soldier || Owner?.PlayerCombatState == null)
            return;

        if (IsUpgraded)
        {
            await YgoFlipReturnOwnFieldMonsterToHand.RunFlipReturnOneOtherControlledWithConduitAsync(
                choiceContext,
                Owner,
                self,
                FlipPromptSingle,
                upgradedGivesEnergy: true);
            return;
        }

        List<NormalMonsterCard> candidates = BuildBounceTargets(Owner, self);
        if (candidates.Count == 0)
            return;

        List<NormalMonsterCard> ordered = await YgoOrderedCardSelection.TryChooseManyAsync(
            choiceContext,
            Owner,
            new CardSelectorPrefs(FlipPromptMulti, 0, 2)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            },
            () => BuildBounceTargets(Owner, self),
            maxResults: 2);
        foreach (NormalMonsterCard target in ordered)
        {
            await YgoFlipReturnOwnFieldMonsterToHand.TryReturnToHandAsync(Owner, target);
            if (Owner.Creature != null)
                await PlayerCmd.GainStars(1, Owner);
        }
    }

    protected override void AddExtraArgsToDescription(LocString description) =>
        description.Add("conduitIcon", ConduitImgBbcode);

    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);

    private static List<NormalMonsterCard> BuildBounceTargets(Player owner, AbstractMonsterCard flipper)
    {
        var list = new List<NormalMonsterCard>();
        foreach (Creature p in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(owner.PlayerCombatState))
        {
            if (!p.IsAlive)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<NormalMonsterCard>(p) is not NormalMonsterCard nm)
                continue;
            if (ReferenceEquals(nm, flipper))
                continue;
            list.Add(nm);
        }

        return list;
    }
}
