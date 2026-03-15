# PatchesForCommandSystem: Diff vs ZGO (ZGO = base, YgoDuelist = changed)

**Base:** `ZGO/Patches/PatchesForCommandSystem/*.cs` (28 files, flat)  
**Changed:** `YgoDuelist/YgoDuelistCode/Patches/PatchesForCommandSystem/{CombatLifecycle|OptionHandLayout|OptionTargetingAndPlay}/*.cs`

## Intentional differences only

- **Namespace / usings:** `ZGO.Patches` → `YgoDuelist.YgoDuelistCode.Patches`; `ZGO.Nodes` → `YgoDuelist.YgoDuelistCode.Nodes`; same for `Services`, `Piles`, `Cards.Command`, `Cards.Core`, `Models`.
- **Log tags:** `[ZGO]` → `[YgoDuelist]` in all `GD.Print` and comments.
- **Class name:** `ZgoCombatEndClearPatch` → `YgoCombatEndClearPatch`.
- **Logger / types:** `ZGO.MainFile` → `YgoDuelist.MainFile`; `ZGO.Services.DuelMonsterFieldRegistry` → `DuelMonsterFieldRegistry` (with `using YgoDuelist.YgoDuelistCode.Services`).
- **Comment text:** "ZGO" → "YgoDuelist" in summary/docs.

## Trivial / cosmetic

- **DuelMonsterRightClickUiPatch:** YgoDuelist adds explicit `using System;` (for `Exception`); ZGO may rely on global usings.
- **Trailing newlines:** Some files differ by final newline or `\ No newline at end of file`; no behavior change.
- **MonsterCommandTurnResetPatch / YgoSecondHandLayoutPatch / YgoOptionHandUiPatch:** One fewer trailing blank line in YgoDuelist.

## File mapping (ZGO → YgoDuelist subfolder)

| ZGO (base) | YgoDuelist path |
|------------|-----------------|
| ZgoCombatEndClearPatch.cs | CombatLifecycle/YgoCombatEndClearPatch.cs |
| MonsterCommandTurnResetPatch.cs | CombatLifecycle/MonsterCommandTurnResetPatch.cs |
| DuelMonsterRightClickUiPatch.cs | CombatLifecycle/DuelMonsterRightClickUiPatch.cs |
| YgoSecondHandLayoutPatch.cs | OptionHandLayout/YgoSecondHandLayoutPatch.cs |
| YgoOptionHandUiPatch.cs | OptionHandLayout/YgoOptionHandUiPatch.cs |
| NHandCardHolderSetTargetPositionOptionHolderPatch.cs | OptionHandLayout/ |
| NCardPlayCenterCardOptionHolderPatch.cs | OptionHandLayout/ |
| NCardHolderConnectSignalsOptionPatch.cs | OptionHandLayout/ |
| YgoSecondHandHandBridge.cs | OptionHandLayout/ |
| NPlayerHandOnCombatStateChangedPatch.cs | OptionHandLayout/ |
| YgoSecondHandHoverPatch.cs | OptionHandLayout/ |
| YgoSecondHandSetDefaultTargetsPatch.cs | OptionHandLayout/ |
| YgoSecondHandMegaLabelPatch.cs | OptionHandLayout/ |
| YgoSecondHandGlowPatch.cs | OptionHandLayout/ |
| PlayCardFromOptionPilePatch.cs | OptionTargetingAndPlay/ |
| NTargetManagerStartTargetingOptionHolderPatch.cs | OptionTargetingAndPlay/ |
| NTargetManagerProcessOptionHolderPatch.cs | OptionTargetingAndPlay/ |
| NTargetManagerOptionHolderFirstClickPatch.cs | OptionTargetingAndPlay/ |
| NTargetManagerOptionHolderArrowPatch.cs | OptionTargetingAndPlay/ |
| NPlayerHandStartCardPlayOptionPatch.cs | OptionTargetingAndPlay/ |
| NPlayerHandReturnHolderToHandPatch.cs | OptionTargetingAndPlay/ |
| NPlayerHandOnHolderPressedOptionPatch.cs | OptionTargetingAndPlay/ |
| NMouseCardPlayStartAsyncOptionPilePatch.cs | OptionTargetingAndPlay/ |
| NMouseCardPlayOptionHolderPlayZonePatch.cs | OptionTargetingAndPlay/ |
| NCreatureOptionHolderTargetingNoPlayerHighlightPatch.cs | OptionTargetingAndPlay/ |
| NCardPlayTryPlayCardOptionLogPatch.cs | OptionTargetingAndPlay/ |
| CardPileCmdOptionPilePlayPatch.cs | OptionTargetingAndPlay/ |
| NCardPlayCannotPlayOptionPilePatch.cs | OptionTargetingAndPlay/ |

---

## Per-file diffs (unified diff: `-` = ZGO base, `+` = YgoDuelist changed)

## ZgoCombatEndClearPatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\ZgoCombatEndClearPatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\CombatLifecycle\\YgoCombatEndClearPatch.cs" index e6170c4..b9a0470 100644 --- "a/ZGO\\Patches\\PatchesForCommandSystem\\ZgoCombatEndClearPatch.cs" +++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\CombatLifecycle\\YgoCombatEndClearPatch.cs" @@ -6,17 +6,17 @@ using MegaCrit.Sts2.Core.Hooks;  using MegaCrit.Sts2.Core.Nodes.Rooms;  using MegaCrit.Sts2.Core.Rooms;  using MegaCrit.Sts2.Core.Runs; -using ZGO.Services; +using YgoDuelist.YgoDuelistCode.Services;   -namespace ZGO.Patches; +namespace YgoDuelist.YgoDuelistCode.Patches;    /// <summary> -/// At end of every combat, clear ZGO combat-scoped state so the next combat +/// At end of every combat, clear YgoDuelist combat-scoped state so the next combat  /// does not see data from previous combats (e.g. CalcDuelMonsterStats using  /// old field monsters, command state, or normal-summon tracking).  /// </summary>  [HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))] -public static class ZgoCombatEndClearPatch +public static class YgoCombatEndClearPatch  {      [HarmonyPostfix]      public static async void Postfix(IRunState runState, CombatState? combatState, CombatRoom room)

## MonsterCommandTurnResetPatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\MonsterCommandTurnResetPatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\CombatLifecycle\\MonsterCommandTurnResetPatch.cs" index ac82721..02935db 100644 --- "a/ZGO\\Patches\\PatchesForCommandSystem\\MonsterCommandTurnResetPatch.cs" +++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\CombatLifecycle\\MonsterCommandTurnResetPatch.cs" @@ -6,9 +6,9 @@ using MegaCrit.Sts2.Core.Entities.Players;  using MegaCrit.Sts2.Core.GameActions.Multiplayer;  using MegaCrit.Sts2.Core.Hooks;  using MegaCrit.Sts2.Core.Models; -using ZGO.Services; +using YgoDuelist.YgoDuelistCode.Services;   -namespace ZGO.Patches; +namespace YgoDuelist.YgoDuelistCode.Patches;    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterPlayerTurnStart))]  public static class MonsterCommandTurnResetPatch @@ -37,4 +37,3 @@ public static class MonsterCommandTurnResetPatch          await Task.CompletedTask;      }  } -

## DuelMonsterRightClickUiPatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\DuelMonsterRightClickUiPatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\CombatLifecycle\\DuelMonsterRightClickUiPatch.cs" index 9f76d46..761548e 100644 --- "a/ZGO\\Patches\\PatchesForCommandSystem\\DuelMonsterRightClickUiPatch.cs" +++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\CombatLifecycle\\DuelMonsterRightClickUiPatch.cs" @@ -1,3 +1,4 @@ +using System;  using System.Collections.Generic;  using System.Linq;  using Godot; @@ -14,13 +15,13 @@ using MegaCrit.Sts2.Core.Models;  using MegaCrit.Sts2.Core.Nodes.Cards;  using MegaCrit.Sts2.Core.Nodes.Combat;  using MegaCrit.Sts2.Core.Nodes.Rooms; -using ZGO.Cards.Command; -using ZGO.Cards.Core; -using ZGO.Models; -using ZGO.Piles; -using ZGO.Services; +using YgoDuelist.YgoDuelistCode.Cards.Command; +using YgoDuelist.YgoDuelistCode.Cards.Core; +using YgoDuelist.YgoDuelistCode.Models; +using YgoDuelist.YgoDuelistCode.Piles; +using YgoDuelist.YgoDuelistCode.Services;   -namespace ZGO.Patches; +namespace YgoDuelist.YgoDuelistCode.Patches;    // Tracks which pet the player is currently hovering, so right-click logic  // can be driven off the same focus/hover behavior the base game uses. @@ -59,7 +60,7 @@ public static class DuelMonsterHoverTrackerPatch                  return;                _hoveredPet = creature; -            GD.Print($"[ZGO] Hover start on duel pet: {creature.Monster?.GetType().Name}"); +            GD.Print($"[YgoDuelist] Hover start on duel pet: {creature.Monster?.GetType().Name}");          }          catch          { @@ -73,7 +74,7 @@ public static class DuelMonsterHoverTrackerPatch      {          if (_hoveredPet == __instance.Entity)          { -            GD.Print($"[ZGO] Hover end on duel pet: {__instance.Entity.Monster?.GetType().Name}"); +            GD.Print($"[YgoDuelist] Hover end on duel pet: {__instance.Entity.Monster?.GetType().Name}");              _hoveredPet = null;          }      } @@ -93,7 +94,7 @@ public static class DuelMonsterRightClickUiPatch      {          try          { -            //GD.Print($"[ZGO] NCombatUi._Input event: {inputEvent.GetType().Name}"); +            //GD.Print($"[YgoDuelist] NCombatUi._Input event: {inputEvent.GetType().Name}");                if (inputEvent is not InputEventMouseButton mouse ||                  mouse.ButtonIndex != MouseButton.Right || @@ -102,13 +103,13 @@ public static class DuelMonsterRightClickUiPatch                  return;              }   -            GD.Print("[ZGO] Right mouse button pressed."); +            GD.Print("[YgoDuelist] Right mouse button pressed.");                // Only run when a combat is actually running.              var combatState = CombatManager.Instance.DebugOnlyGetState();              if (combatState == null || !CombatManager.Instance.IsInProgress)              { -                GD.Print("[ZGO] Right-click ignored: no active combat."); +                GD.Print("[YgoDuelist] Right-click ignored: no active combat.");                  return;              }   @@ -118,17 +119,17 @@ public static class DuelMonsterRightClickUiPatch              var pet = DuelMonsterHoverTrackerPatch.CurrentHoveredPet;              if (pet == null)              { -                GD.Print("[ZGO] Right-click: no currently hovered duel pet."); +                GD.Print("[YgoDuelist] Right-click: no currently hovered duel pet.");                  return;              }   -            GD.Print($"[ZGO] Right-click on duel pet: {pet.Monster?.GetType().Name}"); +            GD.Print($"[YgoDuelist] Right-click on duel pet: {pet.Monster?.GetType().Name}");              OpenMonsterOptions(pet);          }          catch (Exception e)          {              // Log and swallow; this patch must never crash the game. -            ZGO.MainFile.Logger.Error($"DuelMonsterRightClickUiPatch error: {e}"); +            YgoDuelist.MainFile.Logger.Error($"DuelMonsterRightClickUiPatch error: {e}");          }      }   @@ -138,7 +139,7 @@ public static class DuelMonsterRightClickUiPatch          if (player?.PlayerCombatState == null)              return;   -        var sourceCard = ZGO.Services.DuelMonsterFieldRegistry.GetSourceCardForPet(pet); +        var sourceCard = DuelMonsterFieldRegistry.GetSourceCardForPet(pet);          if (sourceCard is not NormalMonsterCard monsterCard)              return;   @@ -146,7 +147,7 @@ public static class DuelMonsterRightClickUiPatch          if (optionPile == null)              return;   -        GD.Print($"[ZGO] Clearing YgoCardOptionPile. Previous count={optionPile.Cards.Count}"); +        GD.Print($"[YgoDuelist] Clearing YgoCardOptionPile. Previous count={optionPile.Cards.Count}");          optionPile.Clear();            // Use CombatState.CreateCard<T> so we get proper mutable instances @@ -175,10 +176,10 @@ public static class DuelMonsterRightClickUiPatch          exit.InitializeSource(monsterCard);          commands.Add(exit);   -        GD.Print($"[ZGO] Adding {commands.Count} command cards to YgoCardOptionPile."); +        GD.Print($"[YgoDuelist] Adding {commands.Count} command cards to YgoCardOptionPile.");          foreach (var c in commands)          { -            GD.Print($"[ZGO]  - {c.Id.Entry} ({c.GetType().Name})"); +            GD.Print($"[YgoDuelist]  - {c.Id.Entry} ({c.GetType().Name})");          }            // Move each command card into the YgoCardOptionPile using the same @@ -193,4 +194,4 @@ public static class DuelMonsterRightClickUiPatch          // view so any UI subscribers (that own NCards/NCardHolders) can update.          YgoOptionHandBridge.SyncFromOptionPile(player);      } -} \ No newline at end of file +}

## YgoSecondHandLayoutPatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\YgoSecondHandLayoutPatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionHandLayout\\YgoSecondHandLayoutPatch.cs" index 3fc1c1f..2ee0ca9 100644 --- "a/ZGO\\Patches\\PatchesForCommandSystem\\YgoSecondHandLayoutPatch.cs" +++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionHandLayout\\YgoSecondHandLayoutPatch.cs" @@ -7,9 +7,9 @@ using MegaCrit.Sts2.Core.Extensions;  using MegaCrit.Sts2.Core.Helpers;  using MegaCrit.Sts2.Core.Nodes.Cards.Holders;  using MegaCrit.Sts2.Core.Nodes.Combat; -using ZGO.Nodes; +using YgoDuelist.YgoDuelistCode.Nodes;   -namespace ZGO.Patches; +namespace YgoDuelist.YgoDuelistCode.Patches;    [HarmonyPatch(typeof(NPlayerHand), "RefreshLayout")]  public static class YgoSecondHandLayoutPatch @@ -20,9 +20,9 @@ public static class YgoSecondHandLayoutPatch          var all = __instance.ActiveHolders.ToList();          var main = all.Where(h => h is not NYgoOptionCardHolder).ToList();          var opts = all.Where(h => h is NYgoOptionCardHolder).ToList(); -        GD.Print("[ZGO] YgoSecondHandLayoutPatch ENTER all=", all.Count, " main=", main.Count, " opts=", opts.Count, " FocusedHolderId=", __instance.FocusedHolder?.GetInstanceId() ?? 0, " FocusedIsOpt=", __instance.FocusedHolder is NYgoOptionCardHolder); +        GD.Print("[YgoDuelist] YgoSecondHandLayoutPatch ENTER all=", all.Count, " main=", main.Count, " opts=", opts.Count, " FocusedHolderId=", __instance.FocusedHolder?.GetInstanceId() ?? 0, " FocusedIsOpt=", __instance.FocusedHolder is NYgoOptionCardHolder);          for (int o = 0; o < opts.Count; o++) -            GD.Print("[ZGO]   opts[", o, "] holderId=", opts[o].GetInstanceId(), " model=", (opts[o] as NYgoOptionCardHolder)?.CardModel?.GetType().Name ?? "?"); +            GD.Print("[YgoDuelist]   opts[", o, "] holderId=", opts[o].GetInstanceId(), " model=", (opts[o] as NYgoOptionCardHolder)?.CardModel?.GetType().Name ?? "?");            // ----- Main hand: vanilla layout logic, but over 'main' only -----          int count = main.Count; @@ -157,9 +157,9 @@ public static class YgoSecondHandLayoutPatch                  // Ensure option row is on top so it receives clicks instead of main hand.                  holder.ZIndex = 10;   -                GD.Print("[ZGO] YgoSecondHandLayoutPatch: AFTER holder.Position.Y = ", holder.Position.Y); -                GD.Print("[ZGO] YgoSecondHandLayoutPatch: AFTER holder.TargetPosition.Y = ", holder.TargetPosition.Y); -                GD.Print("[ZGO] YgoSecondHandLayoutPatch: AFTER holder.Hitbox.Position.Y = ", holder.Hitbox.Position.Y); +                GD.Print("[YgoDuelist] YgoSecondHandLayoutPatch: AFTER holder.Position.Y = ", holder.Position.Y); +                GD.Print("[YgoDuelist] YgoSecondHandLayoutPatch: AFTER holder.TargetPosition.Y = ", holder.TargetPosition.Y); +                GD.Print("[YgoDuelist] YgoSecondHandLayoutPatch: AFTER holder.Hitbox.Position.Y = ", holder.Hitbox.Position.Y);                    if (optFocusedIdx == i)                  { @@ -192,8 +192,7 @@ public static class YgoSecondHandLayoutPatch              }          }   -        GD.Print("[ZGO] YgoSecondHandLayoutPatch EXIT return false"); +        GD.Print("[YgoDuelist] YgoSecondHandLayoutPatch EXIT return false");          return false;      }  } -

## YgoOptionHandUiPatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\YgoOptionHandUiPatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionHandLayout\\YgoOptionHandUiPatch.cs" index d712ce5..31b457d 100644 --- "a/ZGO\\Patches\\PatchesForCommandSystem\\YgoOptionHandUiPatch.cs" +++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionHandLayout\\YgoOptionHandUiPatch.cs" @@ -10,14 +10,14 @@ using MegaCrit.Sts2.Core.Models;  using MegaCrit.Sts2.Core.Nodes.Cards.Holders;  using MegaCrit.Sts2.Core.Nodes.Combat;  using MegaCrit.Sts2.Core.Nodes.Rooms; -using ZGO.Nodes; -using ZGO.Piles; -using ZGO.Services; +using YgoDuelist.YgoDuelistCode.Nodes; +using YgoDuelist.YgoDuelistCode.Piles; +using YgoDuelist.YgoDuelistCode.Services;   -namespace ZGO.Patches; +namespace YgoDuelist.YgoDuelistCode.Patches;    /// <summary> -/// Hooks into the combat UI lifecycle and mirrors the logical ZGO option +/// Hooks into the combat UI lifecycle and mirrors the logical YgoDuelist option  /// hand (maintained by <see cref="YgoOptionHandBridge"/>) into a concrete  /// row of NCards/NCardHolders. This is what makes the option pile behave  /// as a fully functional second hand with normal hover and targeting. @@ -70,17 +70,17 @@ public static class YgoOptionHandUiPatch      {          try          { -            GD.Print("[ZGO] OnOptionsChanged ENTER player=", player?.GetHashCode() ?? 0, " cardsCount=", cards?.Count ?? 0); +            GD.Print("[YgoDuelist] OnOptionsChanged ENTER player=", player?.GetHashCode() ?? 0, " cardsCount=", cards?.Count ?? 0);              if (cards != null && cards.Count > 0)              {                  for (int i = 0; i < cards.Count; i++) -                    GD.Print("[ZGO]   cards[", i, "]=", cards[i]?.GetType().Name ?? "null", " id=", cards[i]?.GetHashCode() ?? 0); +                    GD.Print("[YgoDuelist]   cards[", i, "]=", cards[i]?.GetType().Name ?? "null", " id=", cards[i]?.GetHashCode() ?? 0);              }              var room = NCombatRoom.Instance;              var ui = room?.Ui;              if (room == null || ui == null)              { -                GD.Print("[ZGO] OnOptionsChanged EXIT room or ui null"); +                GD.Print("[YgoDuelist] OnOptionsChanged EXIT room or ui null");                  return;              }   @@ -91,23 +91,23 @@ public static class YgoOptionHandUiPatch              }              catch              { -                GD.Print("[ZGO] OnOptionsChanged EXIT GetMe threw"); +                GD.Print("[YgoDuelist] OnOptionsChanged EXIT GetMe threw");                  return;              }                if (me != player)              { -                GD.Print("[ZGO] OnOptionsChanged EXIT me != player"); +                GD.Print("[YgoDuelist] OnOptionsChanged EXIT me != player");                  return;              }                Vector2 viewportSize = ui.GetViewportRect().Size;              RebuildForPlayer(player, cards, ui, viewportSize); -            GD.Print("[ZGO] OnOptionsChanged EXIT RebuildForPlayer done"); +            GD.Print("[YgoDuelist] OnOptionsChanged EXIT RebuildForPlayer done");          }          catch (Exception e)          { -            ZGO.MainFile.Logger.Error($"YgoOptionHandUiPatch.OnOptionsChanged error: {e}"); +            YgoDuelist.MainFile.Logger.Error($"YgoOptionHandUiPatch.OnOptionsChanged error: {e}");          }      }   @@ -116,10 +116,10 @@ public static class YgoOptionHandUiPatch          var hand = NPlayerHand.Instance;          if (hand == null)          { -            GD.Print("[ZGO] RebuildForPlayer hand=null"); +            GD.Print("[YgoDuelist] RebuildForPlayer hand=null");              return;          } -        GD.Print("[ZGO] RebuildForPlayer START handId=", hand.GetInstanceId(), " cardsCount=", cards?.Count ?? 0); +        GD.Print("[YgoDuelist] RebuildForPlayer START handId=", hand.GetInstanceId(), " cardsCount=", cards?.Count ?? 0);            if (_holdersByPlayer.TryGetValue(player, out var existing))          { @@ -131,7 +131,7 @@ public static class YgoOptionHandUiPatch              int pruned = existing.RemoveAll(h => canPrune(h));              if (pruned > 0)              { -                GD.Print("[ZGO] RebuildForPlayer PRUNED ", pruned, " stale holder(s) (no longer in hand, not current play)"); +                GD.Print("[YgoDuelist] RebuildForPlayer PRUNED ", pruned, " stale holder(s) (no longer in hand, not current play)");                  foreach (var h in toFree)                  {                      if (GodotObject.IsInstanceValid(h) && h.IsInsideTree()) @@ -140,16 +140,16 @@ public static class YgoOptionHandUiPatch              }              if (existing.Count > 0)              { -                GD.Print("[ZGO] RebuildForPlayer REMOVING ", existing.Count, " existing holders. FocusedHolder=", hand.FocusedHolder?.GetInstanceId() ?? 0, " FocusedHolderIsOption=", hand.FocusedHolder is NYgoOptionCardHolder); +                GD.Print("[YgoDuelist] RebuildForPlayer REMOVING ", existing.Count, " existing holders. FocusedHolder=", hand.FocusedHolder?.GetInstanceId() ?? 0, " FocusedHolderIsOption=", hand.FocusedHolder is NYgoOptionCardHolder);                  for (int i = 0; i < existing.Count; i++)                  {                      var h = existing[i];                      string modelName = h.CardModel?.GetType().Name ?? "null";                      bool stillInHand = GodotObject.IsInstanceValid(h) && h.GetParent() == container; -                    GD.Print("[ZGO]   remove[", i, "] holderId=", h.GetInstanceId(), " model=", modelName, " IsInsideTree=", h.IsInsideTree(), " isFocused=", (hand.FocusedHolder == h), " stillInHand=", stillInHand); +                    GD.Print("[YgoDuelist]   remove[", i, "] holderId=", h.GetInstanceId(), " model=", modelName, " IsInsideTree=", h.IsInsideTree(), " isFocused=", (hand.FocusedHolder == h), " stillInHand=", stillInHand);                      if (hand.FocusedHolder == h)                      { -                        GD.Print("[ZGO]   >>> clearing FocusedHolder (was option holder being removed)"); +                        GD.Print("[YgoDuelist]   >>> clearing FocusedHolder (was option holder being removed)");                          var focusedProp = AccessTools.Property(typeof(NPlayerHand), "FocusedHolder");                          var lastIdxField = AccessTools.Field(typeof(NPlayerHand), "_lastFocusedHolderIdx");                          focusedProp?.SetValue(hand, null); @@ -168,7 +168,7 @@ public static class YgoOptionHandUiPatch                              h.QueueFree();                      }                  } -                GD.Print("[ZGO] RebuildForPlayer REMOVED all. Now FocusedHolder=", hand.FocusedHolder?.GetInstanceId() ?? 0); +                GD.Print("[YgoDuelist] RebuildForPlayer REMOVED all. Now FocusedHolder=", hand.FocusedHolder?.GetInstanceId() ?? 0);              }              existing.Clear();          } @@ -176,38 +176,38 @@ public static class YgoOptionHandUiPatch          {              existing = new List<NYgoOptionCardHolder>();              _holdersByPlayer[player] = existing; -            GD.Print("[ZGO] RebuildForPlayer no existing holders"); +            GD.Print("[YgoDuelist] RebuildForPlayer no existing holders");          }            if (cards == null || cards.Count == 0)          { -            GD.Print("[ZGO] RebuildForPlayer EXIT cards null or empty"); +            GD.Print("[YgoDuelist] RebuildForPlayer EXIT cards null or empty");              return;          }            PendingOptionHolderToFreeAfterReturnToHand = null;          int count = cards.Count; -        GD.Print("[ZGO] RebuildForPlayer ADDING ", count, " holders"); +        GD.Print("[YgoDuelist] RebuildForPlayer ADDING ", count, " holders");          for (int i = 0; i < count; i++)          {              var model = cards[i];              if (model == null)              { -                GD.Print("[ZGO]   add[", i, "] SKIP model=null"); +                GD.Print("[YgoDuelist]   add[", i, "] SKIP model=null");                  continue;              }              string modelName = model.GetType().Name;              var holder = NYgoOptionCardHolder.Create(); -            GD.Print("[ZGO]   add[", i, "] CREATE holderId=", holder.GetInstanceId(), " model=", modelName); +            GD.Print("[YgoDuelist]   add[", i, "] CREATE holderId=", holder.GetInstanceId(), " model=", modelName);              var handField = AccessTools.Field(typeof(NHandCardHolder), "_hand");              handField?.SetValue(holder, hand);              YgoSecondHandHandBridge.RegisterOptionHolder(hand, holder, -1); -            GD.Print("[ZGO]   add[", i, "] after RegisterOptionHolder holderId=", holder.GetInstanceId(), " IsInsideTree=", holder.IsInsideTree(), " ChildCount=", hand.GetChildCount()); +            GD.Print("[YgoDuelist]   add[", i, "] after RegisterOptionHolder holderId=", holder.GetInstanceId(), " IsInsideTree=", holder.IsInsideTree(), " ChildCount=", hand.GetChildCount());              holder.Initialize(model, i, count, viewportSize); -            GD.Print("[ZGO]   add[", i, "] after Initialize holderId=", holder.GetInstanceId(), " CardNode=", holder.CardNode?.GetInstanceId() ?? 0, " CardModel=", holder.CardModel?.GetType().Name ?? "null"); +            GD.Print("[YgoDuelist]   add[", i, "] after Initialize holderId=", holder.GetInstanceId(), " CardNode=", holder.CardNode?.GetInstanceId() ?? 0, " CardModel=", holder.CardModel?.GetType().Name ?? "null");              existing.Add(holder);          } -        GD.Print("[ZGO] RebuildForPlayer END totalHolders=", existing.Count, " handActiveHoldersCount=", hand.ActiveHolders.Count); +        GD.Print("[YgoDuelist] RebuildForPlayer END totalHolders=", existing.Count, " handActiveHoldersCount=", hand.ActiveHolders.Count);          ValidateSecondHandHolders(player);      }   @@ -231,7 +231,7 @@ public static class YgoOptionHandUiPatch          int pruned = holders.RemoveAll(h => canPrune(h));          if (pruned > 0)          { -            GD.Print("[ZGO] ValidateSecondHand: pruned ", pruned, " stale holder(s)"); +            GD.Print("[YgoDuelist] ValidateSecondHand: pruned ", pruned, " stale holder(s)");              foreach (var h in toFree)              {                  if (GodotObject.IsInstanceValid(h) && h.IsInsideTree()) @@ -259,9 +259,9 @@ public static class YgoOptionHandUiPatch              if (!inTree || !visible || !cardOk || !hitboxOk || !hitboxVisible || !hitboxEnabled || !inActive || focusStale)              {                  if (!anyFail) -                    GD.Print("[ZGO] ValidateSecondHand: FAILURES for player ", player.GetHashCode(), " handId=", hand.GetInstanceId()); +                    GD.Print("[YgoDuelist] ValidateSecondHand: FAILURES for player ", player.GetHashCode(), " handId=", hand.GetInstanceId());                  anyFail = true; -                GD.Print("[ZGO]   holder[", i, "] ", modelName, " id=", h.GetInstanceId(), +                GD.Print("[YgoDuelist]   holder[", i, "] ", modelName, " id=", h.GetInstanceId(),                      " inTree=", inTree, " visible=", visible,                      " cardOk=", cardOk, " hitboxOk=", hitboxOk, " hitboxVisible=", hitboxVisible, " hitboxEnabled=", hitboxEnabled,                      " inActiveHolders=", inActive, " focusStale=", focusStale); @@ -270,19 +270,19 @@ public static class YgoOptionHandUiPatch            if (activeOpts.Count != holders.Count)          { -            GD.Print("[ZGO] ValidateSecondHand: active option count mismatch activeOpts=", activeOpts.Count, " tracked=", holders.Count); +            GD.Print("[YgoDuelist] ValidateSecondHand: active option count mismatch activeOpts=", activeOpts.Count, " tracked=", holders.Count);              anyFail = true;          }            var focused = hand.FocusedHolder;          if (focused is NYgoOptionCardHolder optFocused && !holders.Contains(optFocused))          { -            GD.Print("[ZGO] ValidateSecondHand: FocusedHolder is option holder not in our list id=", optFocused.GetInstanceId()); +            GD.Print("[YgoDuelist] ValidateSecondHand: FocusedHolder is option holder not in our list id=", optFocused.GetInstanceId());              anyFail = true;          }            if (anyFail) -            GD.Print("[ZGO] ValidateSecondHand: END (had failures)"); +            GD.Print("[YgoDuelist] ValidateSecondHand: END (had failures)");      }        private static void ClearAll() @@ -301,4 +301,3 @@ public static class YgoOptionHandUiPatch          _holdersByPlayer.Clear();      }  } -

## PlayCardFromOptionPilePatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\PlayCardFromOptionPilePatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\PlayCardFromOptionPilePatch.cs"
index 89cfe5c..8d70352 100644
--- "a/ZGO\\Patches\\PatchesForCommandSystem\\PlayCardFromOptionPilePatch.cs"
+++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\PlayCardFromOptionPilePatch.cs"
@@ -14,12 +14,12 @@ using MegaCrit.Sts2.Core.Nodes.Cards;
 using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
 using MegaCrit.Sts2.Core.Nodes.Combat;
 using MegaCrit.Sts2.Core.Helpers;
-using ZGO.Cards.Command;
-using ZGO.Nodes;
-using ZGO.Piles;
-using ZGO.Services;
+using YgoDuelist.YgoDuelistCode.Cards.Command;
+using YgoDuelist.YgoDuelistCode.Nodes;
+using YgoDuelist.YgoDuelistCode.Piles;
+using YgoDuelist.YgoDuelistCode.Services;
 
-namespace ZGO.Patches;
+namespace YgoDuelist.YgoDuelistCode.Patches;
 
 /// <summary>
 /// When PlayCardAction runs, the game's ExecuteAction returns early if the card is not in Hand.
@@ -61,22 +61,22 @@ public static class PlayCardFromOptionPilePatch
     /// </summary>
     private static async Task ExecutePlayFromOptionPileAsync(PlayCardAction action)
     {
-        GD.Print("[ZGO] PlayCardFromOptionPile: ExecutePlayFromOptionPileAsync START");
+        GD.Print("[YgoDuelist] PlayCardFromOptionPile: ExecutePlayFromOptionPileAsync START");
         try
         {
             var card = action.NetCombatCard.ToCardModel();
             if (card == null)
             {
-                GD.Print("[ZGO] PlayCardFromOptionPile: card is null, exiting");
+                GD.Print("[YgoDuelist] PlayCardFromOptionPile: card is null, exiting");
                 return;
             }
-            GD.Print("[ZGO] PlayCardFromOptionPile: card=", card.Id.Entry, " type=", card.GetType().Name, " TargetType=", card.TargetType);
+            GD.Print("[YgoDuelist] PlayCardFromOptionPile: card=", card.Id.Entry, " type=", card.GetType().Name, " TargetType=", card.TargetType);
 
             var pile = card.Pile;
             var optionPile = YgoCardOptionPile.CustomType.GetPile(action.Player);
             if (pile == null || optionPile == null || pile != optionPile)
             {
-                GD.Print("[ZGO] PlayCardFromOptionPile: card not in option pile, exiting");
+                GD.Print("[YgoDuelist] PlayCardFromOptionPile: card not in option pile, exiting");
                 return;
             }
 
@@ -86,27 +86,27 @@ public static class PlayCardFromOptionPilePatch
             bool needsTarget = card.TargetType == TargetType.AnyEnemy || card.TargetType == TargetType.AnyAlly;
             if (needsTarget && target == null)
             {
-                GD.Print("[ZGO] PlayCardFromOptionPile: card requires target but target is null (TargetId=", action.TargetId, ") - skipping play so card is not consumed");
+                GD.Print("[YgoDuelist] PlayCardFromOptionPile: card requires target but target is null (TargetId=", action.TargetId, ") - skipping play so card is not consumed");
                 Log.Warn($"Attempted to play card {card} with TargetType of type 'Any', but no target was passed to the play card action!");
                 return;
             }
 
             if (!card.CanPlay(out _, out _) || !card.IsValidTarget(target))
             {
-                GD.Print("[ZGO] PlayCardFromOptionPile: CanPlay false or invalid target, card=", card?.Id.Entry ?? "null");
+                GD.Print("[YgoDuelist] PlayCardFromOptionPile: CanPlay false or invalid target, card=", card?.Id.Entry ?? "null");
                 if (card is Exit_Monster_Options exit)
                 {
-                    GD.Print("[ZGO] PlayCardFromOptionPile: running Exit_Monster_Options.OnClickedOption()");
+                    GD.Print("[YgoDuelist] PlayCardFromOptionPile: running Exit_Monster_Options.OnClickedOption()");
                     TaskHelper.RunSafely(exit.OnClickedOption());
                 }
                 else if (card is Toggle_Die_For_You toggle)
                 {
-                    GD.Print("[ZGO] PlayCardFromOptionPile: running Toggle_Die_For_You.OnClickedOption()");
+                    GD.Print("[YgoDuelist] PlayCardFromOptionPile: running Toggle_Die_For_You.OnClickedOption()");
                     TaskHelper.RunSafely(toggle.OnClickedOption());
                 }
                 else
-                    GD.Print("[ZGO] PlayCardFromOptionPile: card is not Exit_Monster_Options or Toggle_Die_For_You, skipping OnClickedOption");
-                GD.Print("[ZGO] PlayCardFromOptionPile: calling action.Cancel() and returning");
+                    GD.Print("[YgoDuelist] PlayCardFromOptionPile: card is not Exit_Monster_Options or Toggle_Die_For_You, skipping OnClickedOption");
+                GD.Print("[YgoDuelist] PlayCardFromOptionPile: calling action.Cancel() and returning");
                 action.Cancel();
                 return;
             }
@@ -162,7 +162,7 @@ public static class PlayCardFromOptionPilePatch
         }
         finally
         {
-            GD.Print("[ZGO] PlayCardFromOptionPile: ExecutePlayFromOptionPileAsync END");
+            GD.Print("[YgoDuelist] PlayCardFromOptionPile: ExecutePlayFromOptionPileAsync END");
         }
     }
 }

## NTargetManagerStartTargetingOptionHolderPatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\NTargetManagerStartTargetingOptionHolderPatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\NTargetManagerStartTargetingOptionHolderPatch.cs"
index 05c3fae..4405159 100644
--- "a/ZGO\\Patches\\PatchesForCommandSystem\\NTargetManagerStartTargetingOptionHolderPatch.cs"
+++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\NTargetManagerStartTargetingOptionHolderPatch.cs"
@@ -5,9 +5,9 @@ using HarmonyLib;
 using MegaCrit.Sts2.Core.Entities.Cards;
 using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
 using MegaCrit.Sts2.Core.Nodes.Combat;
-using ZGO.Nodes;
+using YgoDuelist.YgoDuelistCode.Nodes;
 
-namespace ZGO.Patches;
+namespace YgoDuelist.YgoDuelistCode.Patches;
 
 /// <summary>
 /// For option-row plays the mouse is often in the "cancel zone" (bottom 5% of screen),
@@ -30,7 +30,7 @@ public static class NTargetManagerStartTargetingOptionHolderPatch
 
     static void Prefix(NTargetManager __instance, TargetType validTargetsType, object __1)
     {
-        GD.Print("[ZGO] StartTargeting ENTER validTargetsType=", validTargetsType, " secondArgType=", __1?.GetType().Name ?? "null");
+        GD.Print("[YgoDuelist] StartTargeting ENTER validTargetsType=", validTargetsType, " secondArgType=", __1?.GetType().Name ?? "null");
     }
 
     static void Postfix(NTargetManager __instance)
@@ -41,12 +41,12 @@ public static class NTargetManagerStartTargetingOptionHolderPatch
 
         var cardPlay = CurrentCardPlayField?.GetValue(hand);
         var isOptionHolder = cardPlay is NCardPlay cp && cp.Holder is NYgoOptionCardHolder;
-        GD.Print("[ZGO] StartTargeting POSTFIX cardPlay=", cardPlay?.GetType().Name ?? "null", " holderIsOption=", isOptionHolder);
+        GD.Print("[YgoDuelist] StartTargeting POSTFIX cardPlay=", cardPlay?.GetType().Name ?? "null", " holderIsOption=", isOptionHolder);
 
         if (!isOptionHolder)
             return;
 
-        GD.Print("[ZGO] StartTargeting: option holder play - disabling exit-early condition so targeting/arrow stay active");
+        GD.Print("[YgoDuelist] StartTargeting: option holder play - disabling exit-early condition so targeting/arrow stay active");
         ExitEarlyConditionField?.SetValue(__instance, NoOpExitCondition);
     }
 }

## NTargetManagerProcessOptionHolderPatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\NTargetManagerProcessOptionHolderPatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\NTargetManagerProcessOptionHolderPatch.cs"
index baebd66..37eeebd 100644
--- "a/ZGO\\Patches\\PatchesForCommandSystem\\NTargetManagerProcessOptionHolderPatch.cs"
+++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\NTargetManagerProcessOptionHolderPatch.cs"
@@ -2,9 +2,9 @@ using System.Reflection;
 using HarmonyLib;
 using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
 using MegaCrit.Sts2.Core.Nodes.Combat;
-using ZGO.Nodes;
+using YgoDuelist.YgoDuelistCode.Nodes;
 
-namespace ZGO.Patches;
+namespace YgoDuelist.YgoDuelistCode.Patches;
 
 /// <summary>
 /// NTargetManager._Process calls FinishTargeting(cancel: true) when _exitEarlyCondition() is true.

## NTargetManagerOptionHolderFirstClickPatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\NTargetManagerOptionHolderFirstClickPatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\NTargetManagerOptionHolderFirstClickPatch.cs"
index 6185607..cd1c5c9 100644
--- "a/ZGO\\Patches\\PatchesForCommandSystem\\NTargetManagerOptionHolderFirstClickPatch.cs"
+++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\NTargetManagerOptionHolderFirstClickPatch.cs"
@@ -3,9 +3,9 @@ using System.Reflection;
 using HarmonyLib;
 using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
 using MegaCrit.Sts2.Core.Nodes.Combat;
-using ZGO.Nodes;
+using YgoDuelist.YgoDuelistCode.Nodes;
 
-namespace ZGO.Patches;
+namespace YgoDuelist.YgoDuelistCode.Patches;
 
 /// <summary>
 /// When the user starts playing a targeting card from the option row by clicking (not drag),
@@ -74,7 +74,7 @@ public static class NTargetManagerOptionHolderFirstClickPatch
         }
 
         // Would finish with no target while playing from option row: keep targeting active.
-        GD.Print("[ZGO] FinishTargeting: blocking (no hovered target, option holder) - wait for enemy click or right-click to cancel");
+        GD.Print("[YgoDuelist] FinishTargeting: blocking (no hovered target, option holder) - wait for enemy click or right-click to cancel");
         return false;
     }
 }

## NTargetManagerOptionHolderArrowPatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\NTargetManagerOptionHolderArrowPatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\NTargetManagerOptionHolderArrowPatch.cs"
index 41d0155..a313d55 100644
--- "a/ZGO\\Patches\\PatchesForCommandSystem\\NTargetManagerOptionHolderArrowPatch.cs"
+++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\NTargetManagerOptionHolderArrowPatch.cs"
@@ -2,9 +2,9 @@ using Godot;
 using HarmonyLib;
 using MegaCrit.Sts2.Core.Entities.Cards;
 using MegaCrit.Sts2.Core.Nodes.Combat;
-using ZGO.Nodes;
+using YgoDuelist.YgoDuelistCode.Nodes;
 
-namespace ZGO.Patches;
+namespace YgoDuelist.YgoDuelistCode.Patches;
 
 /// <summary>
 /// When starting targeting from an option-row card, record the frame for first-click handling

## NPlayerHandStartCardPlayOptionPatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\NPlayerHandStartCardPlayOptionPatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\NPlayerHandStartCardPlayOptionPatch.cs"
index 39398b7..f292c60 100644
--- "a/ZGO\\Patches\\PatchesForCommandSystem\\NPlayerHandStartCardPlayOptionPatch.cs"
+++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\NPlayerHandStartCardPlayOptionPatch.cs"
@@ -1,9 +1,9 @@
 using HarmonyLib;
 using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
 using MegaCrit.Sts2.Core.Nodes.Combat;
-using ZGO.Nodes;
+using YgoDuelist.YgoDuelistCode.Nodes;
 
-namespace ZGO.Patches;
+namespace YgoDuelist.YgoDuelistCode.Patches;
 
 /// <summary>
 /// When starting a card play from the option row (second hand), skip the "drag into play zone"
@@ -19,13 +19,13 @@ public static class NPlayerHandStartCardPlayOptionPatch
         if (holder is NYgoOptionCardHolder)
         {
             startedViaShortcut = true;
-            Godot.GD.Print("[ZGO] StartCardPlay PREFIX: option holder - set startedViaShortcut=true");
+            Godot.GD.Print("[YgoDuelist] StartCardPlay PREFIX: option holder - set startedViaShortcut=true");
         }
     }
 
     static void Postfix(NHandCardHolder holder)
     {
         if (holder is NYgoOptionCardHolder)
-            Godot.GD.Print("[ZGO] StartCardPlay POSTFIX: option holder - NMouseCardPlay created and Start() called");
+            Godot.GD.Print("[YgoDuelist] StartCardPlay POSTFIX: option holder - NMouseCardPlay created and Start() called");
     }
 }

## NPlayerHandReturnHolderToHandPatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\NPlayerHandReturnHolderToHandPatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\NPlayerHandReturnHolderToHandPatch.cs"
index 095e3eb..8344419 100644
--- "a/ZGO\\Patches\\PatchesForCommandSystem\\NPlayerHandReturnHolderToHandPatch.cs"
+++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\NPlayerHandReturnHolderToHandPatch.cs"
@@ -4,10 +4,10 @@ using HarmonyLib;
 using MegaCrit.Sts2.Core.Entities.Players;
 using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
 using MegaCrit.Sts2.Core.Nodes.Combat;
-using ZGO.Nodes;
-using ZGO.Services;
+using YgoDuelist.YgoDuelistCode.Nodes;
+using YgoDuelist.YgoDuelistCode.Services;
 
-namespace ZGO.Patches;
+namespace YgoDuelist.YgoDuelistCode.Patches;
 
 /// <summary>
 /// When an option holder is returned to hand after a cancelled drag, the stored index

## NPlayerHandOnHolderPressedOptionPatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\NPlayerHandOnHolderPressedOptionPatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\NPlayerHandOnHolderPressedOptionPatch.cs"
index cc545a3..996e899 100644
--- "a/ZGO\\Patches\\PatchesForCommandSystem\\NPlayerHandOnHolderPressedOptionPatch.cs"
+++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\NPlayerHandOnHolderPressedOptionPatch.cs"
@@ -2,9 +2,9 @@ using Godot;
 using HarmonyLib;
 using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
 using MegaCrit.Sts2.Core.Nodes.Combat;
-using ZGO.Nodes;
+using YgoDuelist.YgoDuelistCode.Nodes;
 
-namespace ZGO.Patches;
+namespace YgoDuelist.YgoDuelistCode.Patches;
 
 /// <summary>
 /// Ensures option row holders receive the same play flow as main hand: when the user
@@ -18,6 +18,6 @@ public static class NPlayerHandOnHolderPressedOptionPatch
     static void Prefix(NCardHolder holder)
     {
         if (holder is NYgoOptionCardHolder)
-            GD.Print("[ZGO] OnHolderPressed option holder id=", holder.GetInstanceId(), " card=", (holder as NYgoOptionCardHolder)?.CardModel?.GetType().Name ?? "?");
+            GD.Print("[YgoDuelist] OnHolderPressed option holder id=", holder.GetInstanceId(), " card=", (holder as NYgoOptionCardHolder)?.CardModel?.GetType().Name ?? "?");
     }
 }

## NMouseCardPlayStartAsyncOptionPilePatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\NMouseCardPlayStartAsyncOptionPilePatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\NMouseCardPlayStartAsyncOptionPilePatch.cs"
index 805233b..3fd00e1 100644
--- "a/ZGO\\Patches\\PatchesForCommandSystem\\NMouseCardPlayStartAsyncOptionPilePatch.cs"
+++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\NMouseCardPlayStartAsyncOptionPilePatch.cs"
@@ -3,9 +3,9 @@ using Godot;
 using HarmonyLib;
 using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
 using MegaCrit.Sts2.Core.Nodes.Combat;
-using ZGO.Nodes;
+using YgoDuelist.YgoDuelistCode.Nodes;
 
-namespace ZGO.Patches;
+namespace YgoDuelist.YgoDuelistCode.Patches;
 
 /// <summary>
 /// When the player drags a card from the option hand (second row), StartAsync can run while
@@ -23,13 +23,13 @@ public static class NMouseCardPlayStartAsyncOptionPilePatch
 
         if (!GodotObject.IsInstanceValid(holder) || !holder.IsInsideTree())
         {
-            GD.Print("[ZGO] StartAsync: option holder INVALID or not in tree - cancelling play");
+            GD.Print("[YgoDuelist] StartAsync: option holder INVALID or not in tree - cancelling play");
             __instance.CancelPlayCard();
             __result = Task.CompletedTask;
             return false;
         }
 
-        GD.Print("[ZGO] StartAsync: option holder valid - continuing to targeting (TargetType=", holder.CardNode?.Model?.TargetType.ToString() ?? "null", ")");
+        GD.Print("[YgoDuelist] StartAsync: option holder valid - continuing to targeting (TargetType=", holder.CardNode?.Model?.TargetType.ToString() ?? "null", ")");
         return true;
     }
 }

## NMouseCardPlayOptionHolderPlayZonePatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\NMouseCardPlayOptionHolderPlayZonePatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\NMouseCardPlayOptionHolderPlayZonePatch.cs"
index 4c9dbc5..9159886 100644
--- "a/ZGO\\Patches\\PatchesForCommandSystem\\NMouseCardPlayOptionHolderPlayZonePatch.cs"
+++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\NMouseCardPlayOptionHolderPlayZonePatch.cs"
@@ -1,9 +1,9 @@
 using HarmonyLib;
 using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
 using MegaCrit.Sts2.Core.Nodes.Combat;
-using ZGO.Nodes;
+using YgoDuelist.YgoDuelistCode.Nodes;
 
-namespace ZGO.Patches;
+namespace YgoDuelist.YgoDuelistCode.Patches;
 
 /// <summary>
 /// After targeting, NMouseCardPlay checks IsCardInPlayZone() and cancels if the mouse

## NHandCardHolderSetTargetPositionOptionHolderPatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\NHandCardHolderSetTargetPositionOptionHolderPatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionHandLayout\\NHandCardHolderSetTargetPositionOptionHolderPatch.cs"
index d64912d..f08c5ca 100644
--- "a/ZGO\\Patches\\PatchesForCommandSystem\\NHandCardHolderSetTargetPositionOptionHolderPatch.cs"
+++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionHandLayout\\NHandCardHolderSetTargetPositionOptionHolderPatch.cs"
@@ -1,9 +1,9 @@
 using Godot;
 using HarmonyLib;
 using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
-using ZGO.Nodes;
+using YgoDuelist.YgoDuelistCode.Nodes;
 
-namespace ZGO.Patches;
+namespace YgoDuelist.YgoDuelistCode.Patches;
 
 /// <summary>
 /// During targeting, NMouseCardPlay.LerpToMouse sets the holder's target position to the mouse every frame.

## NCreatureOptionHolderTargetingNoPlayerHighlightPatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\NCreatureOptionHolderTargetingNoPlayerHighlightPatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\NCreatureOptionHolderTargetingNoPlayerHighlightPatch.cs"
index 889c8f9..136ce5f 100644
--- "a/ZGO\\Patches\\PatchesForCommandSystem\\NCreatureOptionHolderTargetingNoPlayerHighlightPatch.cs"
+++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\NCreatureOptionHolderTargetingNoPlayerHighlightPatch.cs"
@@ -5,7 +5,7 @@ using MegaCrit.Sts2.Core.Entities.Cards;
 using MegaCrit.Sts2.Core.Nodes;
 using MegaCrit.Sts2.Core.Nodes.Combat;
 
-namespace ZGO.Patches;
+namespace YgoDuelist.YgoDuelistCode.Patches;
 
 /// <summary>
 /// When targeting enemies (AnyEnemy), hovering the local player's creature still calls HighlightPlayer

## NCardPlayTryPlayCardOptionLogPatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\NCardPlayTryPlayCardOptionLogPatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\NCardPlayTryPlayCardOptionLogPatch.cs"
index 8c5f3ec..e7590a8 100644
--- "a/ZGO\\Patches\\PatchesForCommandSystem\\NCardPlayTryPlayCardOptionLogPatch.cs"
+++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\NCardPlayTryPlayCardOptionLogPatch.cs"
@@ -3,9 +3,9 @@ using HarmonyLib;
 using MegaCrit.Sts2.Core.Entities.Creatures;
 using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
 using MegaCrit.Sts2.Core.Nodes.Combat;
-using ZGO.Nodes;
+using YgoDuelist.YgoDuelistCode.Nodes;
 
-namespace ZGO.Patches;
+namespace YgoDuelist.YgoDuelistCode.Patches;
 
 /// <summary>
 /// Diagnostic: log when TryPlayCard is called for an option holder so we can see if targeting
@@ -18,6 +18,6 @@ public static class NCardPlayTryPlayCardOptionLogPatch
     {
         if (__instance.Holder is not NYgoOptionCardHolder)
             return;
-        GD.Print("[ZGO] TryPlayCard ENTER option holder TargetType=", __instance.Holder.CardNode?.Model?.TargetType.ToString() ?? "null", " target=", target != null ? "CREATURE" : "NULL");
+        GD.Print("[YgoDuelist] TryPlayCard ENTER option holder TargetType=", __instance.Holder.CardNode?.Model?.TargetType.ToString() ?? "null", " target=", target != null ? "CREATURE" : "NULL");
     }
 }

## NCardPlayCenterCardOptionHolderPatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\NCardPlayCenterCardOptionHolderPatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionHandLayout\\NCardPlayCenterCardOptionHolderPatch.cs"
index 6703ee7..7baad30 100644
--- "a/ZGO\\Patches\\PatchesForCommandSystem\\NCardPlayCenterCardOptionHolderPatch.cs"
+++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionHandLayout\\NCardPlayCenterCardOptionHolderPatch.cs"
@@ -1,8 +1,8 @@
 using HarmonyLib;
 using MegaCrit.Sts2.Core.Nodes.Combat;
-using ZGO.Nodes;
+using YgoDuelist.YgoDuelistCode.Nodes;
 
-namespace ZGO.Patches;
+namespace YgoDuelist.YgoDuelistCode.Patches;
 
 /// <summary>
 /// CenterCard() only sets the holder's target position (tweened). For option holders we skip

## NCardHolderConnectSignalsOptionPatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\NCardHolderConnectSignalsOptionPatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionHandLayout\\NCardHolderConnectSignalsOptionPatch.cs"
index 404d1b7..99296a2 100644
--- "a/ZGO\\Patches\\PatchesForCommandSystem\\NCardHolderConnectSignalsOptionPatch.cs"
+++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionHandLayout\\NCardHolderConnectSignalsOptionPatch.cs"
@@ -3,9 +3,9 @@ using Godot;
 using HarmonyLib;
 using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
 using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
-using ZGO.Nodes;
+using YgoDuelist.YgoDuelistCode.Nodes;
 
-namespace ZGO.Patches;
+namespace YgoDuelist.YgoDuelistCode.Patches;
 
 /// <summary>
 /// Option holders are built in code (no scene); GetNode("%Hitbox") can fail if the

## YgoSecondHandHandBridge.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\YgoSecondHandHandBridge.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionHandLayout\\YgoSecondHandHandBridge.cs"
index 9db218c..f1a31d4 100644
--- "a/ZGO\\Patches\\PatchesForCommandSystem\\YgoSecondHandHandBridge.cs"
+++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionHandLayout\\YgoSecondHandHandBridge.cs"
@@ -3,9 +3,9 @@ using Godot;
 using HarmonyLib;
 using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
 using MegaCrit.Sts2.Core.Nodes.Combat;
-using ZGO.Nodes;
+using YgoDuelist.YgoDuelistCode.Nodes;
 
-namespace ZGO.Patches;
+namespace YgoDuelist.YgoDuelistCode.Patches;
 
 /// <summary>
 /// Helper to register NYgoOptionCardHolder instances with the real NPlayerHand,
@@ -19,9 +19,8 @@ public static class YgoSecondHandHandBridge
 
     public static void RegisterOptionHolder(NPlayerHand hand, NYgoOptionCardHolder holder, int index)
     {
-        GD.Print("[ZGO] RegisterOptionHolder ENTER handId=", hand.GetInstanceId(), " holderId=", holder.GetInstanceId(), " index=", index, " handChildCountBefore=", hand.GetChildCount());
+        GD.Print("[YgoDuelist] RegisterOptionHolder ENTER handId=", hand.GetInstanceId(), " holderId=", holder.GetInstanceId(), " index=", index, " handChildCountBefore=", hand.GetChildCount());
         AddCardHolderMethod.Invoke(hand, new object[] { holder, index });
-        GD.Print("[ZGO] RegisterOptionHolder EXIT handId=", hand.GetInstanceId(), " holderId=", holder.GetInstanceId(), " handChildCountAfter=", hand.GetChildCount(), " holderIsInsideTree=", holder.IsInsideTree());
+        GD.Print("[YgoDuelist] RegisterOptionHolder EXIT handId=", hand.GetInstanceId(), " holderId=", holder.GetInstanceId(), " handChildCountAfter=", hand.GetChildCount(), " holderIsInsideTree=", holder.IsInsideTree());
     }
 }
-

## NPlayerHandOnCombatStateChangedPatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\NPlayerHandOnCombatStateChangedPatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionHandLayout\\NPlayerHandOnCombatStateChangedPatch.cs"
index 68d5391..6917fbb 100644
--- "a/ZGO\\Patches\\PatchesForCommandSystem\\NPlayerHandOnCombatStateChangedPatch.cs"
+++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionHandLayout\\NPlayerHandOnCombatStateChangedPatch.cs"
@@ -7,7 +7,7 @@ using MegaCrit.Sts2.Core.Entities.Cards;
 using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
 using MegaCrit.Sts2.Core.Nodes.Combat;
 
-namespace ZGO.Patches;
+namespace YgoDuelist.YgoDuelistCode.Patches;
 
 /// <summary>
 /// Prevents ObjectDisposedException when a deferred CombatStateChanged runs after we've

## CardPileCmdOptionPilePlayPatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\CardPileCmdOptionPilePlayPatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\CardPileCmdOptionPilePlayPatch.cs"
index a9996b9..8e52cf5 100644
--- "a/ZGO\\Patches\\PatchesForCommandSystem\\CardPileCmdOptionPilePlayPatch.cs"
+++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\CardPileCmdOptionPilePlayPatch.cs"
@@ -3,9 +3,9 @@ using HarmonyLib;
 using MegaCrit.Sts2.Core.Commands;
 using MegaCrit.Sts2.Core.Entities.Cards;
 using MegaCrit.Sts2.Core.Models;
-using ZGO.Piles;
+using YgoDuelist.YgoDuelistCode.Piles;
 
-namespace ZGO.Patches;
+namespace YgoDuelist.YgoDuelistCode.Patches;
 
 /// <summary>
 /// When a card in the option pile is "played" (Command Defend, Command Attack), the game calls

## YgoSecondHandHoverPatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\YgoSecondHandHoverPatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionHandLayout\\YgoSecondHandHoverPatch.cs"
index aacb8ea..fb338c2 100644
--- "a/ZGO\\Patches\\PatchesForCommandSystem\\YgoSecondHandHoverPatch.cs"
+++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionHandLayout\\YgoSecondHandHoverPatch.cs"
@@ -2,9 +2,9 @@ using System.Collections.Generic;
 using Godot;
 using HarmonyLib;
 using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
-using ZGO.Nodes;
+using YgoDuelist.YgoDuelistCode.Nodes;
 
-namespace ZGO.Patches;
+namespace YgoDuelist.YgoDuelistCode.Patches;
 
 /// <summary>
 /// Adds hover-based zooming for NYgoOptionCardHolder so second-hand cards
@@ -46,4 +46,3 @@ internal static class YgoSecondHandHoverState
 {
     internal static readonly Dictionary<NYgoOptionCardHolder, Tween> ScaleTweens = new();
 }
-

## YgoSecondHandSetDefaultTargetsPatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\YgoSecondHandSetDefaultTargetsPatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionHandLayout\\YgoSecondHandSetDefaultTargetsPatch.cs"
index 9693f62..2ff4310 100644
--- "a/ZGO\\Patches\\PatchesForCommandSystem\\YgoSecondHandSetDefaultTargetsPatch.cs"
+++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionHandLayout\\YgoSecondHandSetDefaultTargetsPatch.cs"
@@ -1,8 +1,8 @@
 using HarmonyLib;
 using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
-using ZGO.Nodes;
+using YgoDuelist.YgoDuelistCode.Nodes;
 
-namespace ZGO.Patches;
+namespace YgoDuelist.YgoDuelistCode.Patches;
 
 /// <summary>
 /// Prevents null refs in NHandCardHolder.SetDefaultTargets for NYgoOptionCardHolder
@@ -30,4 +30,3 @@ public static class YgoSecondHandSetDefaultTargetsPatch
         return false;
     }
 }
-

## YgoSecondHandMegaLabelPatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\YgoSecondHandMegaLabelPatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionHandLayout\\YgoSecondHandMegaLabelPatch.cs"
index 1a6cdeb..9ccd5ad 100644
--- "a/ZGO\\Patches\\PatchesForCommandSystem\\YgoSecondHandMegaLabelPatch.cs"
+++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionHandLayout\\YgoSecondHandMegaLabelPatch.cs"
@@ -1,8 +1,8 @@
 using HarmonyLib;
 using MegaCrit.Sts2.addons.mega_text;
-using ZGO.Nodes;
+using YgoDuelist.YgoDuelistCode.Nodes;
 
-namespace ZGO.Patches;
+namespace YgoDuelist.YgoDuelistCode.Patches;
 
 /// <summary>
 /// Skip MegaLabel's font-override assertion for labels created by
@@ -21,4 +21,3 @@ public static class YgoSecondHandMegaLabelPatch
         return true;
     }
 }
-

## YgoSecondHandGlowPatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\YgoSecondHandGlowPatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionHandLayout\\YgoSecondHandGlowPatch.cs"
index 53745b0..195e7da 100644
--- "a/ZGO\\Patches\\PatchesForCommandSystem\\YgoSecondHandGlowPatch.cs"
+++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionHandLayout\\YgoSecondHandGlowPatch.cs"
@@ -1,9 +1,9 @@
 using HarmonyLib;
 using MegaCrit.Sts2.Core.Combat;
 using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
-using ZGO.Nodes;
+using YgoDuelist.YgoDuelistCode.Nodes;
 
-namespace ZGO.Patches;
+namespace YgoDuelist.YgoDuelistCode.Patches;
 
 /// <summary>
 /// Prevents null-ref in NHandCardHolder.ShouldGlowGold for NYgoOptionCardHolder
@@ -35,4 +35,3 @@ public static class YgoSecondHandGlowPatch
         return false;
     }
 }
-

## NCardPlayCannotPlayOptionPilePatch.cs
diff --git "a/ZGO\\Patches\\PatchesForCommandSystem\\NCardPlayCannotPlayOptionPilePatch.cs" "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\NCardPlayCannotPlayOptionPilePatch.cs"
index 8f55956..fd8806b 100644
--- "a/ZGO\\Patches\\PatchesForCommandSystem\\NCardPlayCannotPlayOptionPilePatch.cs"
+++ "b/YgoDuelist\\YgoDuelistCode\\Patches\\PatchesForCommandSystem\\OptionTargetingAndPlay\\NCardPlayCannotPlayOptionPilePatch.cs"
@@ -4,10 +4,10 @@ using MegaCrit.Sts2.Core.Entities.Cards;
 using MegaCrit.Sts2.Core.Nodes.Combat;
 using MegaCrit.Sts2.Core.Helpers;
 using MegaCrit.Sts2.Core.Models;
-using ZGO.Cards.Command;
-using ZGO.Piles;
+using YgoDuelist.YgoDuelistCode.Cards.Command;
+using YgoDuelist.YgoDuelistCode.Piles;
 
-namespace ZGO.Patches;
+namespace YgoDuelist.YgoDuelistCode.Patches;
 
 /// <summary>
 /// Same structure as PlayCardFromOptionPilePatch: hook the exact method the game calls when it would

