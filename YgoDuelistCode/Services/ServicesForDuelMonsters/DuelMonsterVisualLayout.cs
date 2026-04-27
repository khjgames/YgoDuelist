using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class DuelMonsterVisualLayout
{
    private const float FrontRowYOffset = 35f;
    private const float BackRowYOffset = -110f;
    private const float XSpacing = 160f;
    private const float BackRowXOffset = 80f;
    private const float DuelFieldXOffset = 360f;

    public static void ApplyForOwner(Player? owner)
    {
        if (owner?.Creature == null || owner.PlayerCombatState == null)
            return;

        NCombatRoom? room = NCombatRoom.Instance;
        if (room == null)
            return;

        NCreature? playerNode = room.GetCreatureNode(owner.Creature);
        if (playerNode == null || !GodotObject.IsInstanceValid(playerNode))
            return;

        List<NCreature> duelNodes = new();
        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(owner.PlayerCombatState))
        {
            if (pet.PetOwner != owner || pet.Monster is not DuelMonsterModel)
                continue;

            NCreature? node = room.GetCreatureNode(pet);
            if (node != null && GodotObject.IsInstanceValid(node))
                duelNodes.Add(node);
        }

        for (int i = 0; i < duelNodes.Count; i++)
            ApplyDuelMonsterSlot(duelNodes[i], playerNode.Position, i);
    }

    public static void ApplyNewlyAddedDuelMonster(Creature? creature)
    {
        if (creature?.Monster is not DuelMonsterModel)
            return;

        NCreature? nCreature = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (nCreature == null || !GodotObject.IsInstanceValid(nCreature))
            return;

        nCreature.SetDefaultScaleTo(DuelMonsterSummon.DuelMonsterScale, 0f);
        nCreature.ToggleIsInteractable(on: true);
        ApplyForOwner(creature.PetOwner);
    }

    private static void ApplyDuelMonsterSlot(NCreature node, Vector2 playerPosition, int index)
    {
        node.SetDefaultScaleTo(DuelMonsterSummon.DuelMonsterScale, 0f);
        node.ToggleIsInteractable(on: true);

        int row = index / 3;
        int col = index % 3;

        float yOffset = row == 0 ? FrontRowYOffset : BackRowYOffset;
        float xOffset = row == 0
            ? (col - 1) * XSpacing
            : (col * XSpacing) - BackRowXOffset;

        xOffset += DuelFieldXOffset;
        node.Position = new Vector2(playerPosition.X + xOffset, playerPosition.Y + yOffset);

        ApplyPortraitTexture(node);
        DuelMonsterPortraitDecorations.RefreshPet(node.Entity);
    }

    private static void ApplyPortraitTexture(NCreature node)
    {
        if (node.Entity.Monster is not DuelMonsterModel monster || string.IsNullOrEmpty(monster.PortraitPath))
            return;

        Node? visualsRoot = node.Visuals;
        Node2D? body = visualsRoot?.GetNodeOrNull<Node2D>("%Visuals");

        Sprite2D? sprite = body as Sprite2D;
        if (body != null && sprite == null)
            sprite = body.GetNodeOrNull<Sprite2D>("Portrait") ?? body.GetNodeOrNull<Sprite2D>("Sprite");

        TextureRect? texRect = sprite == null && body != null
            ? body.GetNodeOrNull<TextureRect>("Portrait")
            : null;

        Texture2D? texture = ResourceLoader.Load<Texture2D>(
            monster.PortraitPath,
            null,
            ResourceLoader.CacheMode.Reuse);

        if (texture == null)
            return;

        if (sprite != null)
            sprite.Texture = texture;
        else if (texRect != null)
            texRect.Texture = texture;
    }
}
