using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Central rules for which duel monsters survive <see cref="CreatureCmd.Kill"/> and which field monsters
/// may be chosen for destruction targeting, keyed by <see cref="YgoDestructionSourceKind"/>.
/// </summary>
public static class YgoDuelMonsterDestructionRules
{
    private static YgoDestructionSourceKind ResolveKind(YgoDestructionSourceKind kind) =>
        YgoDestructionSourceContext.Current ?? kind;

    public static bool PetResistsIncomingKill(Creature pet, YgoDestructionSourceKind kind)
    {
        if (pet == null || !pet.IsAlive || pet.Monster is not DuelMonsterModel)
            return false;

        bool magic = pet.GetPower<MagicProtectionKeywordPower>() != null
            || pet.GetPower<FrontierWisemanSpellShieldPower>() != null;
        bool monsterProt = pet.GetPower<MonsterProtectionKeywordPower>() != null
            || pet.GetPower<FrontierWisemanMonsterShieldPower>() != null;
        YgoDestructionSourceKind k = ResolveKind(kind);

        if (k is YgoDestructionSourceKind.SpellEffect or YgoDestructionSourceKind.TrapEffect)
            return magic;
        if (k == YgoDestructionSourceKind.MonsterEffect)
            return monsterProt;
        return false;
    }

    /// <summary>
    /// Duel monster or equip spell whose host resists <paramref name="kind"/> (field zone only; caller filters hand if needed).
    /// </summary>
    public static Creature? TryResolvePetLinkedToSpellTrapZoneCard(Player player, CardModel card) =>
        card switch
        {
            BaseMonsterCard bm => TributeSummonSelection.ResolvePetForFieldCard(player, bm),
            BaseEquipSpellCard eq =>
                YgoEquipSpellRegistry.GetEquippedMonster(eq) is { } host
                    ? TributeSummonSelection.ResolvePetForFieldCard(player, host)
                    : null,
            _ => null,
        };

    public static bool FieldSpellTrapZoneCardResistsDestroy(Player player, CardModel card, YgoDestructionSourceKind kind)
    {
        Creature? pet = TryResolvePetLinkedToSpellTrapZoneCard(player, card);
        return pet != null && PetResistsIncomingKill(pet, kind);
    }

    public static IEnumerable<CardModel> FilterSpellTrapZoneCardsForMassDestroy(
        Player player,
        IEnumerable<CardModel> cards,
        YgoDestructionSourceKind kind) =>
        cards.Where(c => !FieldSpellTrapZoneCardResistsDestroy(player, c, kind));

    /// <summary>Kill unless the pet resists this destruction source (spell/trap vs monster).</summary>
    public static async Task KillPetWithinDestructionAsync(YgoDestructionSourceKind kind, Creature pet, bool force = true)
    {
        if (pet == null || !pet.IsAlive)
            return;
        if (PetResistsIncomingKill(pet, kind))
            return;
        await CreatureCmd.Kill(pet, force);
    }

    public static IEnumerable<Creature> FilterPetsForMassKill(IEnumerable<Creature> pets, YgoDestructionSourceKind kind) =>
        pets.Where(p => p.IsAlive && !PetResistsIncomingKill(p, kind));

    public static IEnumerable<BaseMonsterCard> FilterFieldMonstersForDestroySelection(
        Player player,
        IEnumerable<BaseMonsterCard> candidates,
        YgoDestructionSourceKind kind) =>
        candidates.Where(m =>
        {
            Creature? pet = TributeSummonSelection.ResolvePetForFieldCard(player, m);
            return pet == null || !PetResistsIncomingKill(pet, kind);
        });
}
