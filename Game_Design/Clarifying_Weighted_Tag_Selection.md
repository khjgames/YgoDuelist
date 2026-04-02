# Clarifying weighted tag selection

Design notes for run-long weighted tag selection in the three-pack generator (**fatigue** + **desire**, then one weighted roll). Complements [Packs_System.md](Packs_System.md).

## What you’re optimizing

Across a **whole run**, pack themes shouldn’t cluster on the same few tags or ignore others forever. The goal is **soft biasing**: still random, but gently steered toward a **target mix** you can tune.

## Two parts, separate and multiplied

1. **Fatigue** — per-tag integer accumulation, “Tetris” baseline, then a **fatigue factor** `F(d)` from the post-Tetris value `d`.
2. **Desire** — per-tag **WeightMultiplier** (neutral **10**). Scales the result: `(WeightMultiplier / 10)`.

**Final weight for a tag** (before normalization across candidates):

`weight = F(d) × (WeightMultiplier / 10)`

Then **weighted RNG**: sum all candidate weights, draw uniform in `[0, sum)`, walk the list.

`F(d)` is **not** “this tag wins the roll”; it is **how much of your baseline contribution remains after fatigue** for that tag. True pick probability still depends on **everyone’s** weights.

---

## Accumulation (raw, before Tetris)

- Every rollable tag has an integer accumulator, run-long, starting at **0** (persist across saves when implemented).
- When a tag is **actually placed on a pack’s mask**, that tag’s accumulator increases by **2**, **3**, or **4** depending on **that pack’s** tag count (same as Packs_System):
  - **Single-tag pack** → **+4**
  - **Double-tag pack** → **+3**
  - **Triple-tag pack** → **+2**
- **Each** tag bit on that pack gets the **full** bump for that pack (not split across tags).

---

## Tetris baseline (relative accumulation)

After **every individual tag pick** (each time one tag is chosen and removed from the working list for that reward), rebase **all** tags that participate in this system:

1. Consider the current raw integer accumulators (after any bump from that pick, if the pick just applied one).
2. Let `m` = **minimum** among those values.
3. Subtract `m` from **every** tag’s accumulator.

So only **differences** between tags matter for the next step; values stay bounded. Example: `[3, 5, 10] → [0, 2, 7]`.

`d` is always a **non-negative integer** after bumps and after Tetris.

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

**After** a tag is chosen and placed on the pack mask:

5. Add that tag’s bump (**+2 / +3 / +4** from the pack’s tag count) to its raw accumulator.
6. Run **Tetris** on **all** participating tags (subtract global minimum so the smallest becomes 0).

Repeat for the next tag in the same pack or the next pack in the reward.

---

## Shared lists across the three packs (unchanged)

For **one card reward**, the three packs **share** one working tag list; choosing a tag **removes** it for the rest of that reward (per Packs_System).

Accumulators and Tetris apply to the **full set of tags** that participate in pack rolling for the run (not only tags still in this reward’s list), unless you intentionally scope otherwise in implementation.

---

## Summary

| Piece | Role |
|--------|------|
| +2 / +3 / +4 on pick | Fatigue bumps (by pack size). |
| Tetris after each tag pick | Keep values relative and bounded. |
| `F(d)` | Maps post-Tetris `d` to fatigue multiplier. |
| `WeightMultiplier / 10` | Desire; independent multiplier. |
| Sum and roll | Standard weighted random. |

This replaces earlier exploratory formulas; implementation should follow this document.
