using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// On Summon: gains {Mgc} <see cref="PumpkingRitualPower"/>. That power grants <see cref="NecroticEvolutionPower"/> while <see cref="Castle_of_Dark_Illusions"/> is face-up on your field.
/// </summary>
public sealed class Pumpking_the_King_of_Ghosts : EffectMonsterCard
{
    public override int AttackPortionCount => 2;
    public Pumpking_the_King_of_Ghosts()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 18,
            baseDef: 20,
            baseMgc: 3,
            duelMonsterRace: DuelMonsterRace.Zombie)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Zombie;

    public override YgoCardArchetype CardArchetypes => YgoCardArchetype.ZombieBoost;

    public override Type[] RelatedCards => GetRelatedCards();

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<PumpkingRitualPower>();
            yield return HoverTipFactory.FromPower<NecroticEvolutionPower>();
        }
    }

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet) =>
        await RunOnSummonedAsync(
            player,
            choiceContext,
            duelMonsterPet,
            async () =>
            {
                decimal stacks = DynamicVars["Mgc"].BaseValue;
                if (stacks <= 0m)
                    return;

                if (duelMonsterPet.GetPower<PumpkingRitualPower>() is { } existing)
                    await PowerCmd.ModifyAmount(existing, stacks, player.Creature, this);
                else
                    await PowerCmd.Apply<PumpkingRitualPower>(duelMonsterPet, stacks, player.Creature, this);
            });

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 5m;
    }
}
