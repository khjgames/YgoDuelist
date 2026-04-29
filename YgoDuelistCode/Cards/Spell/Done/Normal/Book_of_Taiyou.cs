using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;

public sealed class Book_of_Taiyou : BaseSpellCard
{
    public Book_of_Taiyou()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Light;

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        foreach (var creature in Owner.Creature.CombatState.Creatures)
        {
            if (!creature.IsAlive)
                continue;

            if (creature.Block > 0m)
                await CreatureCmd.LoseBlock(creature, creature.Block);

            while (creature.GetPower<ArtifactPower>() != null)
                await PowerCmd.Remove<ArtifactPower>(creature);
        }
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
    }
}
