using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Tyrant_Dragon : EffectMonsterCard, IMonsterActivatedEffect
{
    public Tyrant_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 29,
            baseDef: 25,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dragon)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Fire | YgoCardPackTags.Dragon | YgoCardPackTags.Burn;

    public override Type[] RelatedCards => new[] { typeof(Tyrant_Dragon) };

    public int ActivatedEffectEnergyCost => 1;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-TYRANT_DRAGON.activated_effect.description";

    public bool IsActivatedEffectAvailable => Owner?.Creature != null;

    protected override int GetAttackDefendResolutionCount(Player? player)
    {
        int n = base.GetAttackDefendResolutionCount(player);
        if (Owner?.PlayerCombatState == null)
            return n;
        foreach (Creature p in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(Owner.PlayerCombatState))
        {
            if (!DuelMonsterFieldRegistry.HasSourceCard(p, this))
                continue;
            if (MonsterCommandRegistry.TryGet(p, out var s) && s.TyrantDragonDoubleAttackThisTurn)
                return n + 1;
        }

        return n;
    }

    public Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        if (source is not Tyrant_Dragon || Owner?.Creature == null)
            return Task.CompletedTask;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, Owner);
        if (pet == null)
            return Task.CompletedTask;

        MonsterCommandRegistry.GetOrCreate(pet).TyrantDragonDoubleAttackThisTurn = true;
        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
        return Task.CompletedTask;
    }
}
