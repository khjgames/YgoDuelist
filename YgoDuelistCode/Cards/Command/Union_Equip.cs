using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;
using YgoDuelist.YgoDuelistCode.Extensions;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Patches;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>0-cost: equip this Union-Effect monster to a <see cref="Dark_Blade"/> on the field (material to Limbo).</summary>
public sealed class Union_Equip : MonsterCommandCard, IYgoNHandPlayPhaseHighlightOverride
{
    private static readonly LocString PickHostPrompt = new("cards", "YGODUELIST-UNION_EQUIP.pick_dark_blade");

    protected override bool MirrorSourceMonsterUpgradeVisual => true;

    protected internal override string? CommandEnergyIconPrefix => "silent";

    protected override int CanonicalEnergyCost => 0;

    public override CardType Type => CardType.Skill;

    public override TargetType TargetType => TargetType.Self;

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

        return YgoNHandPlayPhaseHighlightColors.UnionEffectPlayableGreen;
    }

    private static BaseEquipSpellCard? ProtoEquipForSource(BaseMonsterCard? source) =>
        source switch
        {
            Pitch_Dark_Dragon => ModelDb.Card<Pitch_Dark_Dragon_Union_Equip>() as BaseEquipSpellCard,
            Kiryu => ModelDb.Card<Kiryu_Union_Equip>() as BaseEquipSpellCard,
            _ => null,
        };

    public override string PortraitPath
    {
        get
        {
            TryResolveSourceMonsterFromStoredPetId();
            if (SourceMonster is BaseMonsterCard bm && bm is IUnionEffectMonster)
            {
                BaseEquipSpellCard? proto = ProtoEquipForSource(bm);
                if (proto != null && !string.IsNullOrEmpty(proto.PortraitPath))
                    return proto.PortraitPath;
            }

            // Compendium / preview: no field source — match first union-effect implementation used by this command.
            return ModelDb.Card<Pitch_Dark_Dragon>().PortraitPath;
        }
    }

    private static List<Dark_Blade> BuildDarkBladeHosts(Player player, BaseEquipSpellCard proto)
    {
        var list = new List<Dark_Blade>();
        foreach (BaseMonsterCard bm in DuelMonsterFieldRegistry.GetFieldMonsters(player))
        {
            if (bm is not Dark_Blade m)
                continue;
            if (TributeSummonSelection.ResolvePetForFieldCard(player, m) is not { IsAlive: true })
                continue;
            if (YgoEquipSpellTargetRules.IsLegalEquipTarget(proto, m))
                list.Add(m);
        }

        return list;
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable || SourceMonster == null || SourceMonster.FaceDown || Owner == null)
                return false;
            if (SourceMonster is not IUnionEffectMonster)
                return false;
            BaseEquipSpellCard? proto = ProtoEquipForSource(SourceMonster);
            if (proto == null)
                return false;
            if (YgoSpellTrapZoneBridge.CountNonFieldCards(Owner) >= 5)
                return false;
            if (BuildDarkBladeHosts(Owner, proto).Count == 0)
                return false;
            Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(SourceMonster, Owner);
            return pet != null;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.PlayerCombatState == null || SourceMonster is not NormalMonsterCard sourceNm)
            return;
        if (sourceNm is not IUnionEffectMonster)
            return;
        BaseMonsterCard source = sourceNm;
        Player player = Owner;
        BaseEquipSpellCard? proto = ProtoEquipForSource(source);
        if (proto == null)
            return;

        List<Dark_Blade> hosts = BuildDarkBladeHosts(player, proto);
        if (hosts.Count == 0)
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(sourceNm, player);
        if (pet == null || !pet.IsAlive)
            return;

        int snapHp = pet.CurrentHp;
        int snapMax = pet.MaxHp;

        Dark_Blade? host = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(PickHostPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
            },
            () => BuildDarkBladeHosts(player, proto));

        if (host == null || !hosts.Contains(host))
            return;

        CombatState? cs = pet.CombatState ?? player.Creature?.CombatState;
        if (cs == null)
            return;
        CardPile? limbo = YgoPlayerPiles.Limbo(player);
        if (limbo == null)
            return;

        await DuelMonsterPetDeathPatch.ReleaseLiveFieldMonsterToLimboAsync(player, pet, source);

        BaseEquipSpellCard? equip = limbo.Cards.OfType<BaseEquipSpellCard>()
            .FirstOrDefault(e => e.GetType() == proto.GetType());
        if (equip == null)
        {
            equip = source switch
            {
                Pitch_Dark_Dragon => cs.CreateCard<Pitch_Dark_Dragon_Union_Equip>(player),
                Kiryu => cs.CreateCard<Kiryu_Union_Equip>(player),
                _ => null,
            };
        }

        if (equip == null)
            return;

        if (source.IsUpgraded && !equip.IsUpgraded)
            CardCmd.Upgrade(equip, CardPreviewStyle.None);

        EquipSpellPlayPayload.SetPending(equip, host);
        await YgoSpellTrapZoneBridge.ActivateEquipSpellAsync(equip, host);

        YgoUnionLimboRegistry.RegisterPair(source, equip, snapHp, snapMax);

        Creature? hostPet = TributeSummonSelection.ResolvePetForFieldCard(player, host);
        if (hostPet != null && player.Creature != null && !YgoStumblingField.IsActive(player))
            await MonsterCommandRegistry.SetHasUsedCommandThisTurn(hostPet, true, player.Creature, equip);
    }
}
