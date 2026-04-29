using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Amazoness_Swords_Woman : EffectMonsterCard
{
    /// <summary>Thorns from this card currently stacked on the player (removed when face-down or when this leaves the field).</summary>
    internal decimal PendingThornsOnPlayer;

    /// <summary>After the one-time grant for this field presence, flips to face-down do not re-grant on flip-up.</summary>
    internal bool ThornsGrantExhaustedForThisField;

    public Amazoness_Swords_Woman()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 15,
            baseDef: 16,
            baseMgc: 3,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    public override async Task SyncPlayerThornsFromFieldPetPresenceAsync(Creature pet, Creature? applier, CardModel? sourceCard)
    {
        if (applier == null)
            return;

        if (FaceDown)
        {
            if (PendingThornsOnPlayer > 0m)
                await StripThornsAsync(applier);
            return;
        }

        if (PendingThornsOnPlayer > 0m || ThornsGrantExhaustedForThisField)
            return;

        decimal amt = DynamicVars["Mgc"].BaseValue;
        if (amt <= 0m)
            return;

        await PowerCmd.Apply<ThornsPower>(applier, amt, applier, this);
        PendingThornsOnPlayer = amt;
        ThornsGrantExhaustedForThisField = true;
    }

    public override async Task StripPlayerThornsGrantedFromFieldPresenceAsync(Creature playerCreature)
    {
        if (PendingThornsOnPlayer > 0m)
            await StripThornsAsync(playerCreature);
    }

    private async Task StripThornsAsync(Creature playerCreature)
    {
        decimal n = PendingThornsOnPlayer;
        PendingThornsOnPlayer = 0m;
        if (n <= 0m)
            return;

        ThornsPower? t = playerCreature.GetPower<ThornsPower>();
        if (t == null)
            return;

        await PowerCmd.ModifyAmount(t, -n, playerCreature, this);
    }

    public override async Task OnPetDiedAfterOptionPileHandlingAsync(DuelMonsterPetDeathContext ctx)
    {
        if (ctx.Player.Creature != null)
            await StripPlayerThornsGrantedFromFieldPresenceAsync(ctx.Player.Creature);
        await base.OnPetDiedAfterOptionPileHandlingAsync(ctx);
    }

    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Amazoness_Swords_Woman),
    };

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 4m;
    }
}
