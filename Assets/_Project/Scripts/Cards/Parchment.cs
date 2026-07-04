namespace Cards
{
    /// <summary>
    /// How a Parchment scores at the end of the game.
    ///  - Treasure: flat Amount points, unconditional.
    ///  - The Mission rules score conditionally against the player's final board.
    ///
    /// Greybox set — extend with more mission types (and cross-player "most of X"
    /// comparisons) later. Each rule uses CardData.Amount as its points/per-unit value
    /// and CardData.Resource where a resource is relevant.
    /// </summary>
    public enum ParchmentRule
    {
        None = 0,
        Treasure,        // Amount points, flat
        PerResource,     // Amount points per owned tile producing Resource
        PerFief,         // Amount points per fief you own
        LargestFief,     // Amount points per tile in your largest fief
        PerCity          // Amount points per city you own
    }
}
