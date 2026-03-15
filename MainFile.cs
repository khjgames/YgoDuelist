using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using BaseLib.Utils;
using YgoDuelist.YgoDuelistCode.Cards.Monster;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Elemental;
using YgoDuelist.YgoDuelistCode.Cards.Spell;
using YgoDuelist.YgoDuelistCode.Character;
using YgoDuelist.YgoDuelistCode.Nodes;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "YgoDuelist";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Harmony harmony = new(ModId);

        harmony.PatchAll();

        ModHelper.AddModelToPool<YgoDuelistRelicPool, GraveyardRelic>();
        ModHelper.AddModelToPool<YgoDuelistRelicPool, CardOptionsRelic>();

        ModHelper.AddModelToPool<YgoDuelistCardPool, Pot_Of_Greed>();
        ModHelper.AddModelToPool<YgoDuelistCardPool, Foolish_Burial>();
        ModHelper.AddModelToPool<YgoDuelistCardPool, Monster_Reborn>();
        ModHelper.AddModelToPool<YgoDuelistCardPool, Muka_Muka>();
        ModHelper.AddModelToPool<YgoDuelistCardPool, Enraged_Muka_Muka>();
        ModHelper.AddModelToPool<YgoDuelistCardPool, Witchs_Apprentice>();
        ModHelper.AddModelToPool<YgoDuelistCardPool, Milus_Radiant>();
        ModHelper.AddModelToPool<YgoDuelistCardPool, Bladefly>();
        ModHelper.AddModelToPool<YgoDuelistCardPool, Hoshiningen>();
        ModHelper.AddModelToPool<YgoDuelistCardPool, Little_Chimera>();
        ModHelper.AddModelToPool<YgoDuelistCardPool, Star_Boy>();

        // Prewarm pool for the ZGO option-hand holders so NodePool.Get<NYgoOptionCardHolder>()
        // is valid when the option UI first appears.
        GeneratedNodePool.Init(NYgoOptionCardHolder.NewInstanceForPool, 8);
    }
}