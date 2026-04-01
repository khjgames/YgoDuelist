# Clarifying weighted tag selection

Design notes for run-long weighted tag selection in the three-pack generator (accumulator + per-tag desire ratios). Complements [Packs_System.md](Packs_System.md).

## What you’re optimizing

Across a **whole run**, pack themes shouldn’t cluster on the same few tags or ignore others forever. The goal is **soft biasing**: still random, but gently steered toward a **target mix** you can tune.

## Two different knobs (terminology)

The word “weight” is used in two senses; keeping them separate avoids confusion.

### Run accumulator (“accum” / “roll weight”)

Each tag has a number that **goes up when that tag is actually used** in a generated pack.

- **Larger accumulator ⇒ less likely to be picked again soon.**

This matches the idea that “tags with higher [accumulated] weight have a smaller chance.”

### Desire ratio (per-tag tuning, e.g. base 10 vs Spell 22 vs attribute 8)

This does **not** mean “pick this tag less because the number is bigger.”

It means **how much you want that tag in the mix** (or how hard repetition should hurt):

- **Higher ratio** (e.g. Spell 22): “I’m okay seeing this often” → the same accumulator **hurts less** → the tag stays competitive longer.
- **Lower ratio** (e.g. attribute 8): “I want this rarer / repetition should sting more” → the same accumulator **hurts more** → it drops off faster after it appears.

So: **big number on the tag = desire / tolerance**, and **big number from drafting = fatigue / anti-repeat.**

## When the accumulator moves

When **one of the three packs** is rolled and its tag mask is chosen:

1. Roll how many tags that pack has (**1 / 2 / 3** with **48% / 37% / 15%**), as in Packs_System.
2. That sets a **bump size** for that pack: **4 / 3 / 2** for single / double / triple.
3. **Every tag bit that ends up on that pack** gets that bump—not split across tags; **each** chosen theme gets the **full** bump for that pack.

Examples:

- A **triple-tag** pack adds **2** to **each** of its three tags.
- A **single-tag** pack adds **4** to its one tag.

Triples spread themes more per pack, but **each** theme is fatigued a bit **less per appearance** than a single-theme pack.

## How a pick is made (conceptually)

For the next random choice among tags still in the pool:

1. Start from **desire ratios** (default ~10, overrides per tag).
2. Combine with each tag’s **current accumulator** so overused tags lose relative probability.
3. **Weighted random** pick (same idea as card weights: build weights, then RNG).

The **exact formula** can vary as long as it respects:

- **More accumulation ⇒ lower** selection weight.
- **Higher desire ratio ⇒ relatively higher** selection weight for the same accumulation.

## Shared lists across the three packs

- The three packs **share** one working tag list for that reward; when a tag is chosen, it is **removed** so later packs in the same reward cannot reuse it (per Packs_System).
- The **accumulator is run-long**: it applies the **next** time packs are generated (and should **persist across saves** once implemented).


Look what I care about is that the two parts of the system work seperately correctly. 
So like, there is a fatigue thing that scales everythings chances by adding the weights of 2, 3 or 4 at a time right? 
First lets discuss how much I want that to impact the chance of an individual tag being chosen.
In general consider the accumulated weight value differences between tags to affect the result this much.

so how the accumulation works is every tag has an entry starting at 0.
and individual tags would gain 2, 3, or 4, when picked.
This accumulation value is relative. What this means is that there's like a tetris baseline effect where if everyone has a common base its subtracted from their accumulators (so if everyone was in the range of 3 to 10 they would all be subtracted by 3 to get them starting at 0 again and keep the ceiling from going too high, meaning they would now be in a range of 0 to 7 instead. 

Now then how does this Tetris'd accumulation factor in to the chance of something being chosen? Its actually very simple, according to the accumulation value "d"
for d = 0, the chance is 100%, for d = 1, the chance is 50%, and for d = 2 onward, the chance is 
1/(d*d*5/8) 
under that formula d = 4 leads to 1/10, d = 7 leads to 1/30.625, and d = 10 leads to 1/62.5

Finally you just multiply whatever their weight according to that formula with their accumulation d is 
by their individual WeightMultiplier /10 which means a WeightMultiplier of 22 would be 2.2x higher resulting weight for a given d.
Then you just pick from 0 to the sum of all their Weights to see what you rolled, simple right? Anything you dont understand about my method or logic and how it differs from your original complicated shit?




What you’re doing (as I understand it)
1. Accumulation (fatigue, per tag)

Each tag has a number, starts at 0.
When that tag is actually put on a pack, it goes up by 2, 3, or 4 (you’ll keep the same mapping as the design doc: e.g. triple → +2, double → +3, single → +4 — same three numbers, just tied to pack size).
2. “Tetris” baseline (relative / keep numbers small)

Take all tags’ accumulators and subtract the minimum so the smallest becomes 0 again.
Example: [3, 5, 10] → [0, 2, 7].
So only differences between tags matter for the next step; the whole field shifts down together so values don’t drift to huge numbers forever.
3. Turn accumulation d into a fatigue factor (same for every tag, given d)
You defined how “strong” a tag is before desire:

d	Fatigue factor (you called it “chance” — see note below)
0   1
1	1/2
≥ 2	1 / (d × d × 5/8) = 8 / (5 d²)
Your checks: d=4 → 1/10, d=7 → 1/30.625, d=10 → 1/62.5 — those match 8/(5d²).

4. Desire (per-tag tuning)

WeightMultiplier / 10 (e.g. 22 → 2.2×).
Final weight = (fatigue factor for that tag’s d) × (WeightMultiplier / 10).
5. RNG

Sum all final weights, roll uniform in [0, sum), walk the list — standard weighted pick.
So: fatigue only changes the first factor from d; desire only scales by r/10. That’s the “two parts work separately” you care about.

You run Tetris after every single tag pick so its always relative. 
d is always an integer after bumps (before Tetris), and Tetris still keeps them non‑negative integers.

