using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Persistent combat clones for YGO replay (not ephemeral <see cref="CardModel.IsDupe"/>).
/// </summary>
internal static class YgoReplayCloneFactory
{
    private static readonly FieldInfo CloneOfField = AccessTools.Field(typeof(CardModel), "_cloneOf")!;

    public static CardModel CreatePersistentClone(CardModel source)
    {
        source.AssertMutable();
        if (source.CardScope is not { } scope)
            throw new InvalidOperationException("Cannot clone YGO card without card scope.");

        CardModel clone = scope.CloneCard(source);
        CloneOfField.SetValue(clone, source);

        if (clone is AbstractMonsterCard am && source is AbstractMonsterCard srcAm)
            am.CopyDisplayFormFrom(srcAm);

        YgoReplayCoordinator.MarkReplaySpawn(clone);
        if (CombatManager.Instance?.IsInProgress == true && clone.IsMutable)
            NetCombatCardDb.Instance.IdCardForTesting(clone);
        return clone;
    }

    /// <summary>
    /// Allows <see cref="CardModel.CreateClone"/> from YGO custom zone piles (monster zone, STZ, GY, etc.).
    /// </summary>
    internal static bool TryCloneFromYgoZonePile(CardModel instance, ref CardModel __result)
    {
        CardPile? pile = instance.Pile;
        if (pile == null || pile.Type.IsCombatPile() || instance is not IYgoCard)
            return false;

        instance.AssertMutable();
        if (instance.CardScope is not { } scope)
            throw new InvalidOperationException("Cannot clone YGO card without card scope.");

        __result = scope.CloneCard(instance);
        CloneOfField.SetValue(__result, instance);
        return true;
    }
}
