using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Equip;

/// <summary>Cestus of Dagla — Spellcaster equip; Splinter on battle damage (mod mapping for piercing burn).</summary>
public sealed class Cestus_of_Dagla : BaseEquipSpellCard
{
    private const int PrintedAtkBonus = 5;
    private int _bonusAtk = PrintedAtkBonus;

    public Cestus_of_Dagla()
        : base(1, CardRarity.Common, TargetType.Self)
    {
    }

    public override bool GrantsSplinterDamage => true;

    public override bool CanEquipTo(BaseMonsterCard target) => target.DuelMonsterRace == DuelMonsterRace.Spellcaster;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) => new StatEffectTotal(_bonusAtk, 0);

    protected override void OnUpgrade()
    {
        _bonusAtk = PrintedAtkBonus + YgoStatUpgradeScaling.GetStatUpgradeBonus(PrintedAtkBonus);
        EnergyCost.UpgradeBy(-1);
    }
}
