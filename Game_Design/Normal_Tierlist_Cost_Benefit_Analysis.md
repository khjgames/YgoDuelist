

Assigning normal monster cards a Tier. 

I want you to make it so by default all Normal Monster Cards have a CombinedTier
(which is a number somewhere in the range of 0.4 to 2) (clamped at end to min 0.4, max 2)

How Tiers are assigned is.

ScoringCriteria

float LowLevelATKEfficiency(Card){
switch (Card.ATK){
case 0: return 0.7;
case 1: return 0.85;
case 2: return 1;
case 3: return 1.25;
case 4: return 0.5;
case 5: return 0.6;
case 6: return 0.8;
case 7: return 0.95;
case 8: return 1.1;
case 9: return 1.05;
case 10: return 0.8;
case 11: return 0.8;
case 12: return 0.9;
case 13: return 0.6;
case 14: return 0.6;
case 15: return 0.65;
case 16: return 0.7;
case 17: return 0.75;
case 18: return 0.85;
case 19: return 1;
}
}

float LowLevelDEFEfficiency(Card){
switch (Card.DEF){
case 0: return 0.6;
case 1: return 0.75;
case 2: return 0.9;
case 3: return 1.25;
case 4: return 0.4;
case 5: return 0.5;
case 6: return 0.9;
case 7: return 1.15;
case 8: return 1.1;
case 9: return 0.75;
case 10: return 0.75;
case 11: return 0.85;
case 12: return 0.55;
case 13: return 0.6;
case 14: return 0.65;
case 15: return 0.7;
case 16: return 0.75;
case 17: return 0.85;
case 18: return 0.9;
case 19: return 1;
case 20: return 1.15;
case 21: return 1.25;
}
}

float MediumLevelATKEfficiency(Card){
switch (Card.ATK){
case 0: return 0.4;
case 1: return 0.45;
case 2: return 0.5;
case 3: return 0.55;
case 4: return 0.6;
case 5: return 0.75;
case 6: return 0.85;
case 7: return 1;
case 8: return 1.3;
case 9: return 0.6;
case 10: return 0.7;
case 11: return 0.8;
case 12: return 0.9;
case 13: return 1.05;
case 14: return 1.25;
case 15: return 1.05;
case 16: return 1.05;
case 17: return 1.15;
case 18: return 0.6;
case 19: return 0.65;
case 20: return 0.7;
case 21: return 0.75;
case 22: return 0.85;
case 23: return 0.95;
case 24: return 1.05;
case 25: return 1.15;
case 26: return 1.25;
}
}

float MediumLevelDEFEfficiency(Card){
switch (Card.DEF){
case 0: return 0.4;
case 1: return 0.45;
case 2: return 0.5;
case 3: return 0.55;
case 4: return 0.6;
case 5: return 0.75;
case 6: return 0.85;
case 7: return 1;
case 8: return 0.6;
case 9: return 0.7;
case 10: return 0.8;
case 11: return 0.9;
case 12: return 1.05;
case 13: return 1.25;
case 14: return 1.05;
case 15: return 1.05;
case 16: return 1.15;
case 17: return 0.6;
case 18: return 0.65;
case 19: return 0.7;
case 20: return 0.75;
case 21: return 0.85;
case 22: return 0.95;
case 23: return 1.05;
case 24: return 1.15;
case 25: return 1.25;
case 26: return 1.35;
case 27: return 1.45;
case 28: return 1.55;
case 29: return 1.65;
case 30: return 1.75;
}
}

float HighLevelATKEfficiency(Card){
switch (Card.ATK){
case 0: return 0.3;
case 1: return 0.35;
case 2: return 0.4;
case 3: return 0.45;
case 4: return 0.5;
case 5: return 0.55;
case 6: return 0.7;
case 7: return 0.8;
case 8: return 1.05;
case 9: return 1.2;
case 10: return 1.35;
case 11: return 0.5;
case 12: return 0.6;
case 13: return 0.7;
case 14: return 0.8;
case 15: return 0.9;
case 16: return 1;
case 17: return 1.15;
case 18: return 1.35;
case 19: return 1.1;
case 20: return 1.1;
case 21: return 1.2;
case 22: return 0.65;
case 23: return 0.7;
case 24: return 0.75;
case 25: return 0.85;
case 26: return 0.95;
case 27: return 1.05;
case 28: return 1.15;
case 29: return 1.25;
case 30: return 1.35;
}
}

float HighLevelDEFEfficiency(Card){
switch (Card.DEF){
case 0: return 0.35;
case 1: return 0.4;
case 2: return 0.45;
case 3: return 0.5;
case 4: return 0.55;
case 5: return 0.7;
case 6: return 0.8;
case 7: return 1.05;
case 8: return 1.2;
case 9: return 1.35;
case 10: return 0.5;
case 11: return 0.6;
case 12: return 0.7;
case 13: return 0.8;
case 14: return 0.9;
case 15: return 1;
case 16: return 1.15;
case 17: return 1.35;
case 18: return 1.1;
case 19: return 1.1;
case 20: return 1.2;
case 21: return 0.65;
case 22: return 0.7;
case 23: return 0.75;
case 24: return 0.85;
case 25: return 0.95;
case 26: return 1.05;
case 27: return 1.15;
case 28: return 1.25;
case 29: return 1.35;
case 30: return 1.45;
}
}


for Level 1, 2, 3, 4 ->
ScoredATKEfficiency = LowLevelATKEfficiency(Card);
ScoredDEFEfficiency = LowLevelDEFEfficiency(Card);

for Level 5 and 6 -> 
ScoredATKEfficiency = MediumLevelATKEfficiency(Card);
ScoredDEFEfficiency = MediumLevelDEFEfficiency(Card);

for Level 7 and 8 ->
ScoredATKEfficiency = HighLevelATKEfficiency(Card);
ScoredDEFEfficiency = HighLevelDEFEfficiency(Card);

PrimaryEfficiency = math.max(ScoredATKEfficiency, ScoredDEFEfficiency) * 0.8
SecondaryEfficiency = math.max(ScoredATKEfficiency, ScoredDEFEfficiency) * 0.2
CombinedEfficiency = PrimaryEfficiency + SecondaryEfficiency 

CombinedTier = math.clamp(CombinedEfficiency, 0.4, 2)


/// <summary>
/// Multiplier for YGO pack reward weighted picks of this specific card (within its own rarity)(<see cref="YgoDuelist.YgoDuelistCode.Services.YgoCardPackGenerator"/>).
/// Applied to base weight before trunk copies, related bonus, and duplicate-in-pack damping. Default <c>1</c>.
/// </summary>
public override float PackWeightMultiplier => CombinedTier; // Worse cards appear less often.
