using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;

public sealed class Dark_Magic_Attack : BaseSpellCard, IYgoNeowSignatureDarkMagicSupportSpell
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[]
        {
            new DynamicVar("Mgc", 3m),
            new DynamicVar("Mgc2", 3m)
        };

    public Dark_Magic_Attack()
        : base(cost: 0, cardType: CardType.Attack, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Dark | YgoCardPackTags.Spellcaster | YgoCardPackTags.Bundled;

    public override Type[] BundledCards => new[] { typeof(Dark_Magician) };

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && DuelMonsterFieldRegistry.OrderedFieldMonsters(Owner).Any(YgoMonsterArchetypeKeywords.IsFaceUpDarkMagicianArchetype);

    protected override Type[] PreviewReferencedCardTypes => new[] { typeof(Dark_Magician) };

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        decimal weak = DynamicVars["Mgc"].BaseValue;
        decimal vuln = DynamicVars["Mgc2"].BaseValue;

        foreach (Creature enemy in YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(Owner.Creature.CombatState))
        {
            if (!enemy.IsAlive)
                continue;
            await PowerCmd.Apply<WeakPower>(enemy, weak, Owner.Creature, this);
            await PowerCmd.Apply<VulnerablePower>(enemy, vuln, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].UpgradeValueBy(1m);
        DynamicVars["Mgc2"].UpgradeValueBy(1m);
    }
}
