# Clarifying weighted tag selection

Design notes for run-long weighted tag selection in the three-pack generator (**fatigue** + **desire**, then one weighted roll), **fatigue relief** when the player confirms a pack, and **Tetris** rebasing. Complements [Packs_System.md](Packs_System.md).

Implementation constants (fatigue bumps, relief amounts) live in `YgoCardPackGenerator.cs` (`Single_Tag_Fatigue` / `Double_Tag_Fatigue` / `Triple_Tag_Fatigue` and `Chosen_*_Tag_Fatigue_Relief`).

## What you’re optimizing

Across a **whole run**, pack themes shouldn’t cluster on the same few tags or ignore others forever. The goal is **soft biasing**: still random, but gently steered toward a **target mix** you can tune. **Choosing** a sealed pack **feeds back** into fatigue: that pack’s tags get **relief** so they show up **sooner** in future weighted rolls than tags you keep skipping.

## Two parts, separate and multiplied

1. **Fatigue** — per-tag integer accumulation, “Tetris” baseline, then a **fatigue factor** `F(d)` from the post-Tetris value `d`.
2. **Desire** — per-tag **WeightMultiplier** (neutral **10**). Scales the result: `(WeightMultiplier / 10)`.

**Final weight for a tag** (before normalization across candidates):

`weight = F(d) × (WeightMultiplier / 10)`

Then **weighted RNG**: sum all candidate weights, draw uniform in `[0, sum)`, walk the list.

`F(d)` is **not** “this tag wins the roll”; it is **how much of your baseline contribution remains after fatigue** for that tag. True pick probability still depends on **everyone’s** weights.

---

## Accumulation (raw, before Tetris)

- Every rollable tag has an integer accumulator, run-long, starting at **0** (persist across saves).
- When a tag is **actually placed on a pack’s mask** during generation, that tag’s accumulator increases by a **bump** that depends on **that pack’s** tag count (see `YgoCardPackGenerator`: `Single_Tag_Fatigue`, `Double_Tag_Fatigue`, `Triple_Tag_Fatigue`).
- **Each** tag bit on that pack gets the **full** bump for that pack (not split across tags).

---

## Fatigue relief (when the player confirms a sealed pack)

When the player **locks in** one of the three packs (after confirm on the sealed-pack screen, before deck assignment):

- For **each** tag bit on that pack’s mask, subtract a **relief** amount. The relief depends on how many tags are on **that** mask (`Chosen_Single_Tag_Fatigue_Relief`, `Chosen_Double_Tag_Fatigue_Relief`, `Chosen_Triple_Tag_Fatigue_Relief`).
- Relief is applied **once per pack reward**, to the **chosen** pack only; not to the two packs left behind.
- Then run **Tetris** (below). Intermediate values may go **negative** before Tetris; that is expected.

Effect: chosen themes become **less fatigued** relative to the rest of the run, so future pack rolls **favor** them a bit more than if you had never picked them.

---

## Tetris baseline (relative accumulation)

After **every** operation that changes raw accumulators for this system (each tag pick during generation, and after **fatigue relief** on pack confirm), rebase **all** tags that participate:

1. Consider the current raw integer accumulators for **every** participating tag (tags not stored count as **0**).
2. Let `m` = **minimum** among those values (so if any tag is negative, `m` is negative).
3. Subtract `m` from **every** participating tag’s accumulator.

So only **differences** between tags matter for the next step. If `m` is negative, subtracting `m` **adds** `|m|` to everyone: all values become non-negative, and the global minimum becomes **0**. Examples:

- `[3, 5, 10] → [0, 2, 7]`
- `[-2, 0, 5] → [0, 2, 7]`

After Tetris, `d` is always a **non-negative integer** for every participating tag (and stored values are clamped to the implementation max where applicable).

---

## Fatigue factor `F(d)` from Tetris’d `d`

Use the post-Tetris accumulation `d` for that tag:

| `d` | Fatigue factor `F(d)` |
|-----|------------------------|
| 0 | **1** (100% of baseline contribution from fatigue) |
| 1 | **1/2** (50%) |
| ≥ 2 | **1 / (d × d × 5/8)** = **8 / (5 d²)** |

Checks:

- `d = 4` → `F = 1/10`
- `d = 7` → `F = 1/30.625` (= 8/245)
- `d = 10` → `F = 1/62.5`

---

## Desire: WeightMultiplier

- Each tag has a **WeightMultiplier** (integer tuning). **10** = neutral → factor **1.0**.
- Examples: **22** → **2.2×** final weight for that `d`; **8** → **0.8×**.

---

## One weighted pick (algorithm sketch)

For the next tag among candidates still in the working list:

1. For each candidate, let `d` = that tag’s accumulator **after the last Tetris** (first pick of the run: all zeros; after any pick, you already ran Tetris on everyone).
2. `F = F(d)` from the table above.
3. `weight = F × (WeightMultiplier / 10)`.
4. Sum weights, roll uniform in `[0, sum)`, select the tag.

**After** a tag is chosen and placed on the pack mask during generation:

5. Add that tag’s bump (from the pack’s tag count) to its raw accumulator.
6. Run **Tetris** on **all** participating tags.

Repeat for the next tag in the same pack or the next pack in the reward.

**After** the player confirms a pack in the reward flow:

7. Apply **fatigue relief** to that pack’s mask, then **Tetris** again.

---

## Shared lists across the three packs (unchanged)

For **one card reward**, the three packs **share** one working tag list; choosing a tag **removes** it for the rest of that reward (per Packs_System).

Accumulators and Tetris apply to the **full set of tags** that participate in pack rolling for the run (not only tags still in this reward’s list), unless you intentionally scope otherwise in implementation.

---

## Summary

| Piece | Role |
|--------|------|
| Fatigue bumps on generation | Per-tag increase when a tag lands on a rolled pack (amount by pack size); constants in `YgoCardPackGenerator`. |
| Fatigue relief on pack confirm | Per-tag decrease for the **chosen** pack’s tags (amount by that pack’s tag count); constants in `YgoCardPackGenerator`. |
| Tetris after each change | Subtract global minimum among participating tags; clears negative intermediates when `m < 0`; keeps values relative and bounded. |
| `F(d)` | Maps post-Tetris `d` to fatigue multiplier. |
| `WeightMultiplier / 10` | Desire; independent multiplier. |
| Sum and roll | Standard weighted random. |

This replaces earlier exploratory formulas; implementation should follow this document and the named constants in code.
