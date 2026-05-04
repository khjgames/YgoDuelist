using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Field → GY search — <see cref="Services.YgoFieldToGraveyardDeckSearch"/>; name lock — <see cref="Patches.CardModelSanganNameLockCanPlayPatch"/>.</summary>
public sealed class Sangan : EffectMonsterCard, IFieldToGraveyardDeckSearchEffect
{
    private static readonly LocString SearchPrompt = new("cards", "YGODUELIST-SANGAN.search_deck");

    public Sangan()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 10,
            baseDef: 6,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    LocString IFieldToGraveyardDeckSearchEffect.FieldToGraveyardSearchPrompt => SearchPrompt;

    int IFieldToGraveyardDeckSearchEffect.FieldToGraveyardSearchMaxPrintedAtk => 15;

    bool IFieldToGraveyardDeckSearchEffect.FieldToGraveyardSearchApplyNameLock => true;

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Fiend | YgoCardPackTags.Draw;

    public override Type[] RelatedCards => new[] { typeof(Sangan) };
}
