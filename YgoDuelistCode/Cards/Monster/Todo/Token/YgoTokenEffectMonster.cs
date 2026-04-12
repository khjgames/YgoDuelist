using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Extensions;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Token;

public abstract class YgoTokenEffectMonster : EffectMonsterCard, IYgoTokenMonster
{
    private int? _tokenPortraitVariantIndex;
    private string? _tokenPortraitPathOverride;

    protected YgoTokenEffectMonster(
        int cost,
        CardType type,
        CardRarity rarity,
        TargetType target,
        int duelMonsterLevel,
        DuelMonsterAttribute duelMonsterAttribute,
        int baseAtk,
        int baseDef,
        int baseMgc,
        DuelMonsterRace duelMonsterRace)
        : base(cost, type, rarity, target, duelMonsterLevel, duelMonsterAttribute, baseAtk, baseDef, baseMgc, duelMonsterRace)
    {
    }

    public override bool CanSummonDuelMonster => false;

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => true;

    /// <inheritdoc />
    public virtual int TokenAlternatePortraitCount => 1;

    internal int? TokenPortraitVariantIndex => _tokenPortraitVariantIndex;

    internal void ApplyTokenPortraitVariant(int index1Based, string fullPortraitPath)
    {
        _tokenPortraitVariantIndex = index1Based;
        _tokenPortraitPathOverride = fullPortraitPath;
    }

    public override string PortraitPath =>
        _tokenPortraitPathOverride ?? $"token_portraits/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    public override string CustomPortraitPath
    {
        get
        {
            string stem = Id.Entry.RemovePrefix().ToLowerInvariant();
            string file = _tokenPortraitVariantIndex switch
            {
                null or 1 => $"{stem}.png",
                int n => $"{stem}_{n}.png"
            };
            return $"token_portraits/{file}".BigCardImagePath();
        }
    }
}
