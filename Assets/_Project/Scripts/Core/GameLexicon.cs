using Cards;

/// <summary>
/// Single source of truth for every player-facing name in Impocalypse.
/// Code keeps its internal (greybox/Bunny-Kingdom-era) identifiers - enums and fields
/// never rename, because that breaks scene serialization and buys players nothing.
/// All UI reads display names from here, which also makes future localization trivial.
/// </summary>
public static class GameLexicon
{
    // ---- Meta terms ----
    public const string ScoreName = "Infamy";       // victory points
    public const string FiefName = "Domain";        // a connected group of owned tiles
    public const string ParchmentName = "Pact";     // secret end-game scoring card

    // ---- Resources (internal id -> display name) ----
    public static string Resource(ResourceType r) => r switch
    {
        ResourceType.Wood => "Charwood",
        ResourceType.Carrot => "Bloodroot",
        ResourceType.Fish => "Souls",
        ResourceType.Special_A => "Demonsilk",
        ResourceType.Special_B => "Soulwine",
        ResourceType.Special_C => "Ectoplasm",
        ResourceType.WildcardCommon => "Any resource",
        _ => ""
    };

    // ---- Terrains ----
    public static string Terrain(TileData.TileType t) => t switch
    {
        TileData.TileType.Plains => "Ashlands",
        TileData.TileType.Grass => "Ashlands",
        TileData.TileType.Water => "The Styx",
        TileData.TileType.Fish => "The Styx",
        TileData.TileType.Wood => "Gloomwood Grove",
        TileData.TileType.Carrot => "Bloodroot Field",
        TileData.TileType.Mountain => "Brimstone Peaks",
        _ => t.ToString()
    };

    /// <summary>Short terrain names for tight spaces (card labels, buttons).</summary>
    public static string TerrainShort(TileData.TileType t) => t switch
    {
        TileData.TileType.Plains => "Ashlands",
        TileData.TileType.Grass => "Ashlands",
        TileData.TileType.Water => "Styx",
        TileData.TileType.Fish => "Styx",
        TileData.TileType.Wood => "Grove",
        TileData.TileType.Carrot => "Field",
        TileData.TileType.Mountain => "Peaks",
        _ => t.ToString()
    };

    // ---- Buildings ----
    public static string Building(BuildingKind b) => b switch
    {
        BuildingKind.City => "Citadel",
        BuildingKind.CommonFarm => "Farm",
        BuildingKind.SpecialFarm => "Luxury Farm",
        BuildingKind.Market => "Bazaar",
        BuildingKind.Camp => "Warcamp",
        _ => ""
    };

    public const string CityName = "Citadel";
    public const string SkyTowerName = "Hellgate";
    public const string CampName = "Warcamp";
    public const string MarketName = "Bazaar";
}
