
Why do you keep doing it wrong you fucking moron I told you it only applies on the second bounce onward, which means the first one in that scenario would be 23 due to the doubled unblocked damage, not 11.5 idiot.

You’re right, and I’m sorry — I applied the “half of the pre‑vuln line” rule to the first splinter. You said the budget halving only starts on the second bounce onward. So the first splinter should be based on half of the actual unblocked damage from the hit that started the chain (including B’s 2×), not half of the pre‑multiplier number.

With 21 + 2 = 23 to B and 2× on B, unblocked damage to B is 46. Half of that is 23 → first splinter to A is 23, not 11.

From the second bounce onward: each step uses half of the previous splinter’s budget (the capped chain value), not half of whatever inflated damage landed from Strength, vuln, etc.

So the corrected story for that scenario:

Main hit on B: 46 damage (23 × 2).
First splinter (A): 23 (half of 46) — no halving-of-budget rule yet.
Second splinter (B): budget = ⌊23/2⌋ = 11 (then B’s 2× applies when resolving that hit on B if your rules say so — you didn’t specify, but the chain size is fixed by 23 → 11 → 5 → 2 → 1 → 0).
Third (A): ⌊11/2⌋ = 5, and so on until the budget hits 0.
Again: I mixed up “first splinter = half of real damage” with “first splinter = half of pre‑vuln attack”. Thanks for the correction.

ok so with your corrected understanding do the scenario again

There's an issue with the logic of splinter damage application that causes it to bounce infinitely between 2 enemies until they die if they have no block and you have 2 strenght, because 2 strength means half your damage would be 1, which would then deal at least 3 damage because you have 2 strength which means half your damage would be 1, which would then deal at least 3 damage because you have 2 strength which means -> repeat infinitely until they die. 

The strength of the second splinter bounces and on is capped at half the previous one ignoring how much damage it deals. 

That way even if the bounces do extra damage due to powers, the number of bounces does not increase, it is fixed determined by the strength of the first bounce.

Do you understand what I mean?

Write a paper scenario of me in a fight with 2 target dummies with no block and 150 hp
Dummy A has nothing.
Dummy B has a debuff that makes it take 2x damage (not 1.5x)
I have 2 strength
I target dummy B with a monsters splinter attack with a base damage of 21

Here’s the scenario again with the corrected rules:

First splinter: half of the actual unblocked damage from the hit that started splinter (here, the main hit on B).
Second splinter onward: the budget for the next bounce is half of the previous splinter’s budget (integer halving is fine), not half of “whatever damage actually landed” after Strength / vuln / powers. Modifiers can still change how hard that budget hits someone, but they don’t feed the next budget — so the chain length is fixed by the decay from the first splinter amount.
Setup
Dummy A: 150 HP, no block, no special rules.
Dummy B: 150 HP, no block, takes 2× damage from hits.
You: 2 Strength.
Attack: splinter monster, base 21, you target B.
Assume Strength adds once to the card’s damage: 21 + 2 = 23 base hit damage before B’s multiplier.

Main hit (B, targeted)
Damage to B before multiplier: 23.
B’s 2×: 23 × 2 = 46 unblocked.
B: 150 − 46 = 104. A: 150.
Splinter 1 → A (first bounce only: “half of that real damage”)
First splinter amount = half of 46 = 23.
A takes 23 (no 2× on A).
A: 150 − 23 = 127. B: 104.
Splinter 2 → B (from here on: budget = half of previous budget)
Next budget = ⌊23 / 2⌋ = 11.
That 11 is what the chain uses; it is not recomputed from “damage + Strength” again.
On B, 2× still applies to resolution: 11 × 2 = 22 to B (if your game applies vuln to splinter hits on B — your design).
B: 104 − 22 = 82. A: 127.
Splinter 3 → A
⌊11 / 2⌋ = 5.
A takes 5.
A: 127 − 5 = 122. B: 82.
Splinter 4 → B
⌊5 / 2⌋ = 2.
On B: 2 × 2 = 4.
B: 82 − 4 = 78. A: 122.
Splinter 5 → A
⌊2 / 2⌋ = 1.
A takes 1.
A: 122 − 1 = 121. B: 78.
Splinter 6 → stop
⌊1 / 2⌋ = 0 → no further splinters.
Why the old bug doesn’t happen here
The loop you described was: each bounce recomputes “half of current damage,” and Strength keeps pumping the number back up so “half” never decays. Under your fix, only the first splinter is tied to real dealt damage (here 23 from half of 46). Every later hop is 11 → 5 → 2 → 1 → 0, so the number of non‑zero bounces is bounded (here five splinter hits after the main attack), even if B still takes more HP from 2× on the hits that land on B.

If you want the paper version to assume splinter hits on B don’t double the budget but only double final HP loss, you’d keep the same 11, 5, 2, 1 budget line and only change the HP subtraction on B’s rows — the chain length is unchanged.

Do remember I said capped as well, which means that if for some reason they gained block / damage lowering / prevention things midway through perhaps due to a power that triggers after taking damage a certain number of times during the turn, then it could be even less than half or be 0 and not bounce at all. Its capped to half the previous step with the value pre capped being half the unblocked damage dealt, meaning if damage is partially blocked the entire chain is weaker from that point on. 

You’re describing a two‑sided rule for every splinter after the first:

Chain ceiling (cap): the next splinter’s “size” cannot exceed half of the previous step’s splinter amount (the budget you’re carrying down the chain). So the geometric decay is a hard upper bound.

Damage-based candidate (pre-cap): you also compute what the bounce “would” be from resolution: half of the unblocked damage actually dealt on the hit that triggered that splinter. That’s the pre‑capped value.

Actual next splinter: you take something like the minimum of those two (or equivalently: apply the cap so the next amount is at most half the previous budget, and at most half the unblocked damage — whichever binding constraint is tighter). So:

If they block part of a splinter hit, half of unblocked drops → the whole rest of the chain can get weaker from that point, not just that one hit.
If a power prevents damage or makes it 0 unblocked, half of 0 = 0 → no further bounces (or that branch dies), even if the abstract “half previous budget” was still positive.
If something mid‑fight only triggers after N hits and suddenly adds block / reduction, the next candidate from real damage can collapse, so the chain shortens or stops there.
So “capped” is not redundant with “half previous”: it means the previous step sets a ceiling, but actual splinter size is also tied to real unblocked damage on each hop, so block / mitigation / prevention can make the next bounce smaller than half the previous budget, or zero.

First splinter in your earlier scenario stays half of unblocked damage from the main hit (e.g. 46 → 23). After that, each step is bounded by half the prior splinter budget and cannot exceed what half of unblocked on that hit justifies — so the number of meaningful bounces is still finite, and can shrink if the fight changes mid‑chain.

---

## Order of multiple concurrent splinter branches (3+ enemies)

When the main hit fans out to **more than one** other living enemy, the game runs **one independent splinter branch per other enemy** (each branch gets the same first hop budget: half of past‑block on the main target, floored). Branches are **not** “finish branch A completely, then start branch B.”

**Dispatch order (interleaved by wave):** branches are ordered the same way as the game’s ordered list of “other” enemies. Each **wave** performs **at most one hop per branch** that is still active, in that order:

- **Wave 1:** branch 1 hop 1, branch 2 hop 1, …, branch (N−1) hop 1  
- **Wave 2:** each branch that still has a positive budget and a valid next target takes hop 2, in the same branch order (skipping branches that already ended).  
- **Wave 3+:** same, until no branches remain.

**Example (symmetric combat, two other enemies, first hop 8, then pure × decay 4 → 2 → 1 on each branch):**  
`8, 8, 4, 4, 2, 2, 1, 1` — you see both first splinters before either branch’s second hop, so it is obvious two chains started immediately.

**Example (three other enemies, first hop 8 each):**  
`8, 8, 8,` then the next wave for all surviving branches, e.g. `4, 4, 4,` and so on.

If one branch **ends early** (budget hits 0, no next target, or a target is no longer valid), the remaining branches **keep alternating in waves** without gaps for the finished branch.