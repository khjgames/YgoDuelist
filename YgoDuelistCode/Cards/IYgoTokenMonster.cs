namespace YgoDuelist.YgoDuelistCode.Cards;

/// <summary>
/// Marks duel monsters created as YGO Tokens (destroyed by <see cref="YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal.Token_Thanksgiving"/> and similar).
/// Tokens are routed to Limbo instead of the Graveyard or Banished pile when removed from the field or sent to either zone.
/// </summary>
public interface IYgoTokenMonster
{
    /// <summary>Alternate field portraits: <c>token_portraits/{stem}.png</c> plus <c>_{2..N}</c> when <c>N</c> &gt; 1.</summary>
    int TokenAlternatePortraitCount { get; }
}
