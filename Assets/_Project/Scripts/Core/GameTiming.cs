/// <summary>
/// Single source of truth for phase time limits. The MASTER enforces these
/// (auto-pick / force-end); clients only display countdowns from them, so a
/// little network skew can never cause disagreement about outcomes.
/// </summary>
public static class GameTiming
{
    /// <summary>Seconds to pick cards from each new hand during the draft.</summary>
    public const float DraftPickSeconds = 45f;

    /// <summary>Seconds for the whole construction phase.</summary>
    public const float BuildSeconds = 90f;
}
