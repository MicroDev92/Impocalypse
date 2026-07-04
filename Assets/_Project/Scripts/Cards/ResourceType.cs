namespace Cards
{
    /// <summary>
    /// A resource that contributes to a fief's Wealth (= number of DISTINCT resources).
    ///
    ///  - The three commons (Wood/Carrot/Fish) appear on terrain and from Common Farms.
    ///  - Special/luxury resources come only from Special Farms (restricted placement).
    ///  - WildcardCommon is the Market: it counts as whichever common best fills the fief,
    ///    resolved during scoring.
    ///
    /// Special_* are greybox placeholders — rename/extend for the hellish theme.
    /// </summary>
    public enum ResourceType
    {
        None = 0,

        // Common (basic) resources
        Wood = 1,
        Carrot = 2,
        Fish = 3,

        // Special / luxury resources (placeholders)
        Special_A = 10,
        Special_B = 11,
        Special_C = 12,

        // Market wildcard: counts as any one common of the player's choice
        WildcardCommon = 99
    }

    public static class ResourceTypes
    {
        /// <summary>The three basic resources, in fixed order.</summary>
        public static readonly ResourceType[] Commons =
            { ResourceType.Wood, ResourceType.Carrot, ResourceType.Fish };

        /// <summary>True if the resource is one of the three basic/common resources.</summary>
        public static bool IsCommon(ResourceType r) =>
            r == ResourceType.Wood || r == ResourceType.Carrot || r == ResourceType.Fish;

        /// <summary>True if the resource is a special/luxury resource.</summary>
        public static bool IsSpecial(ResourceType r) =>
            r == ResourceType.Special_A || r == ResourceType.Special_B || r == ResourceType.Special_C;

        /// <summary>The base resource a terrain tile produces on its own (None if it has none).</summary>
        public static ResourceType FromTerrain(TileData.TileType type) => type switch
        {
            TileData.TileType.Wood => ResourceType.Wood,
            TileData.TileType.Carrot => ResourceType.Carrot,
            TileData.TileType.Fish => ResourceType.Fish,
            _ => ResourceType.None
        };
    }
}
