using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Extensions;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>0-cost on <see cref="Dark_Blade"/>: unequip this union equip; material returns from Limbo.</summary>
public abstract class Unequip_Union_Base : MonsterCommandCard, IYgoNHandPlayPhaseHighlightOverride
{
    protected override bool MirrorSourceMonsterUpgradeVisual => true;

    protected internal override string? CommandEnergyIconPrefix => "silent";

    protected override int CanonicalEnergyCost => 0;

    public override CardType Type => CardType.Skill;

    public override TargetType TargetType => TargetType.Self;

    protected abstract bool IsMatchingUnionEquip(BaseEquipSpellCard equip);

    protected abstract string CanonicalPortraitPath { get; }

    public Color? GetNHandPlayPhaseHighlightModulateOverride(
        NHandCardHolder holder,
        bool vanillaWouldUseCyanPlayableHighlight)
    {
        _ = holder;
        if (Pile?.Type != YgoCardOptionPile.CustomType)
            return null;
        if (Owner == null)
            return null;
        try
        {
            if (!LocalContext.IsMe(Owner))
                return null;
        }
        catch
        {
            return null;
        }

        if (!vanillaWouldUseCyanPlayableHighlight)
            return null;

        return YgoNHandPlayPhaseHighlightColors.FusionStylePurple;
    }

    public override string PortraitPath
    {
        get
        {
            if (IsCanonical)
                return CanonicalPortraitPath;
            TryResolveSourceMonsterFromStoredPetId();
            return "card.png".CardImagePath();
        }
    }

    private BaseEquipSpellCard? FindAttachedUnionOnHost()
    {
        if (Owner == null || SourceMonster is not Dark_Blade blade)
            return null;
        foreach (BaseEquipSpellCard eq in YgoEquipSpellRegistry.GetEquipsForMonster(blade))
        {
            if (IsMatchingUnionEquip(eq))
                return eq;
        }

        return null;
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable || SourceMonster == null || SourceMonster.FaceDown || Owner == null)
                return false;
            if (SourceMonster is not Dark_Blade)
                return false;
            BaseEquipSpellCard? eq = FindAttachedUnionOnHost();
            if (eq == null)
                return false;
            if (DuelMonsterSummon.CountLiveDuelMonsters(Owner) >= DuelMonsterSummon.MaxDuelMonstersPerPlayer)
                return false;
            Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(SourceMonster, Owner);
            return pet != null;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.PlayerCombatState == null || SourceMonster is not Dark_Blade blade)
            return;
        Player player = Owner;
        BaseEquipSpellCard? equip = FindAttachedUnionOnHost();
        if (equip == null)
            return;
        if (!YgoUnionLimboRegistry.TryGetByEquip(equip, out BaseMonsterCard? limboMon, out int snapHp, out int snapMax))
            return;
        if (limboMon == null || limboMon.Pile?.Type != LimboPile.CustomType)
            return;

        CardPile? limbo = YgoPlayerPiles.Limbo(player);
        if (limbo == null)
            return;

        YgoUnionLimboRegistry.RemovePairForEquip(equip);

        await CardPileCmd.Add(
            new CardModel[] { equip },
            limbo,
            CardPilePosition.Top,
            equip,
            false);

        bool summoned = await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, limboMon, choiceContext);
        if (!summoned)
            return;

        Creature? newPet = TributeSummonSelection.ResolvePetForFieldCard(player, limboMon);
        if (newPet == null || !newPet.IsAlive)
            return;

        await YgoUnionLimboHpRestore.ApplySnapshotAfterSummonAsync(newPet, limboMon, player, snapHp, snapMax);

        if (player.Creature != null && !YgoStumblingField.IsActive(player))
            await MonsterCommandRegistry.SetHasUsedCommandThisTurn(newPet, true, player.Creature, limboMon);
    }
}
