using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Special Summon only via <see cref="Dark_Flare_Knight"/> battle destruction (see <see cref="YgoDarkFlareKnightMirageKnightSummonState"/>).</summary>
public sealed class Mirage_Knight : EffectMonsterCard
{
    [SavedProperty]
    public bool BanishAtOwnerEndStep { get; set; }

    public Mirage_Knight()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 28,
            baseDef: 20,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Light | YgoCardPackTags.Warrior | YgoCardPackTags.Banish;

    public override bool AttackDealsBlightedDamage => true;

    public override bool AttackDealsFullBlightedDamage => true;

    public override Type[] RelatedCards => new[] { typeof(Mirage_Knight), typeof(Dark_Flare_Knight) };

    public override bool CanSummonDuelMonster => false;

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate =>
        YgoDarkFlareKnightMirageKnightSummonState.IsSummonBypassActive;

    protected override Task OnAfterMonsterAttackHitAsync(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        AttackCommand attackCommand)
    {
        _ = choiceContext;
        _ = cardPlay;
        _ = attackCommand;
        BanishAtOwnerEndStep = true;
        return Task.CompletedTask;
    }

    public override async Task OnOwnerTurnEndFieldCleanupAsync(PlayerChoiceContext ctx, Player owner, Creature pet)
    {
        _ = ctx;
        if (!BanishAtOwnerEndStep)
            return;

        if (!pet.IsAlive)
            return;

        BanishAtOwnerEndStep = false;
        await YgoBanishedService.BanishCard(owner, this);
    }
}
