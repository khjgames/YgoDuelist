using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;

public sealed class Graceful_Dice : BaseSpellCard
{
    private static readonly LocString RollPreviewPrompt =
        new("cards", "YGODUELIST-GRACEFUL_DICE.roll_result.selection");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 1m) };

    public Graceful_Dice()
        : base(cost: 0, rarity: CardRarity.Rare, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellQuickPlay)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Burn | YgoCardPackTags.Chance;

    public override Type[] RelatedCards => new[] { typeof(Graceful_Dice) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip tip in base.ExtraHoverTips)
                yield return tip;
            yield return YgoDeterministicRngResultDisplay.Rolled6SampleHoverTip();
        }
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState is not CombatState cs)
            return;

        int diceCount = (int)DynamicVars["Mgc"].BaseValue;
        if (diceCount <= 0)
            return;

        ulong mixBase = YgoDeterministicRng.MixDuelMonsterAttack(Owner.Creature, null, cardPlay);
        var preview = new List<CardModel>(diceCount);
        int total = 0;
        for (int i = 0; i < diceCount; i++)
        {
            ulong mix = mixBase ^ ((ulong)(i + 1) << 32);
            int face = YgoDeterministicRng.RollDie(cs, 6, $"GRACEFUL_DICE-D6-{i}", mix);
            total += face;
            preview.Add(YgoDeterministicRngResultDisplay.CreateD6RollResultCard(cs, Owner, face));
        }

        await YgoPreviewGridSelection.ShowPreviewAsync(choiceContext, preview, Owner, RollPreviewPrompt);

        await PowerCmd.Apply<GracefulDicePower>(Owner.Creature, total, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].BaseValue = 2m;
    }
}
