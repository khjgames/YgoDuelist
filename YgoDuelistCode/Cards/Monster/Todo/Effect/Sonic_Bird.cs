using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Sonic_Bird : EffectMonsterCard
{
    public Sonic_Bird()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 14,
            baseDef: 10,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.WingedBeast)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Ritual | YgoCardPackTags.Wind | YgoCardPackTags.Spell;

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet)
    {
        await base.OnSummoned(player, choiceContext, duelMonsterPet);
        if (YgoDuelMonsterSummonStyleContext.CurrentNormalOrTribute != true)
            return;

        var ctx = choiceContext ?? new BlockingPlayerChoiceContext();
        await YgoRitualDeckSearchService.TrySearchAndAddToHandAsync(player, ctx, RitualDeckSearchKind.RitualSpellOnly);
    }

    public override async Task OnFlipSummonedFromCommandMenuAsync(PlayerChoiceContext choiceContext, Player player)
    {
        var ctx = choiceContext ?? new BlockingPlayerChoiceContext();
        await YgoRitualDeckSearchService.TrySearchAndAddToHandAsync(player, ctx, RitualDeckSearchKind.RitualSpellOnly);
    }
}
