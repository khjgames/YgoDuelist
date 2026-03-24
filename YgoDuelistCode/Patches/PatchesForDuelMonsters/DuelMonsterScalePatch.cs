using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.AddCreature))]
public static class DuelMonsterScalePatch
{
    public static void Postfix(Creature creature)
    {
        if (creature?.Monster is not DuelMonsterModel duelMonster)
            return;

        NCreature? nCreature = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (nCreature == null)
            return;

        // Keep this specific summon tiny and interactable.
        nCreature.SetDefaultScaleTo(DuelMonsterSummon.DuelMonsterScale, 0f);
        nCreature.ToggleIsInteractable(on: true);

        var room = NCombatRoom.Instance;
        var player = creature.PetOwner?.Creature ?? creature;
        if (room == null || player == null)
            return;

        var playerNode = room.GetCreatureNode(player);
        if (playerNode == null)
            return;

        // Collect this player's duel monster pets in a deterministic order.
        var duelNodes = new List<NCreature>();
        foreach (var node in room.CreatureNodes)
        {
            if (node.Entity.PetOwner == player.Player &&
                node.Entity.Monster is DuelMonsterModel)
            {
                duelNodes.Add(node);
            }
        }

        // Layout config: tweak to taste.
        const float frontRowYOffset = 35f;   // front row slightly below player
        const float backRowYOffset  = -110f;  // back row slightly above front
        const float xSpacing        = 160f;   // horizontal spacing between summons
        const float backRowXOffset  = 80f;   // center the 2 back-row summons

        for (int i = 0; i < duelNodes.Count; i++)
        {
            var dn = duelNodes[i];

            int row = i / 3;   // 0 = front row (up to 3), 1 = back row (next 2)
            int col = i % 3;

            Vector2 basePos = playerNode.Position;

            float yOffset = row == 0 ? frontRowYOffset : backRowYOffset;
            float xOffset;

            if (row == 0)
            {
                // 3 in front: -x, 0, +x
                xOffset = (col - 1) * xSpacing;
            }
            else
            {
                // 2 in back: centered between the front ones
                // indices 3,4 => col 0,1
                xOffset = (col * xSpacing) - backRowXOffset;
            }

            xOffset += 360f;

            dn.Position = new Vector2(basePos.X + xOffset, basePos.Y + yOffset);
            dn.ToggleIsInteractable(on: true);

            // Swap the simple sprite's texture to the card portrait, if available.
            if (dn.Entity.Monster is DuelMonsterModel m && !string.IsNullOrEmpty(m.PortraitPath))
            {
                var body = dn.Visuals.Body;

                // If the visuals body itself is a Sprite2D (static image enemy), use it directly.
                Sprite2D? sprite = body as Sprite2D;

                // Otherwise, fall back to expected child nodes.
                if (sprite == null)
                {
                    sprite = body.GetNodeOrNull<Sprite2D>("Portrait")
                             ?? body.GetNodeOrNull<Sprite2D>("Sprite");
                }

                TextureRect? texRect = sprite == null
                    ? body.GetNodeOrNull<TextureRect>("Portrait")
                    : null;

                Texture2D? texture = ResourceLoader.Load<Texture2D>(
                    m.PortraitPath,
                    null,
                    ResourceLoader.CacheMode.Reuse
                );

                if (texture != null)
                {
                    if (sprite != null)
                        sprite.Texture = texture;
                    else if (texRect != null)
                        texRect.Texture = texture;
                }
            }

            DuelMonsterPortraitDecorations.RefreshPet(dn.Entity);
        }
    }
}
