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

    // ---- Card rules text ----

    /// <summary>
    /// The card's display name for the inspector panel.
    /// </summary>
    public static string CardName(CardData card)
    {
        if (card == null) return "";
        if (card.IsTerritory) return $"Territory {card.Coordinate}";
        if (card.IsCity) return $"{CityName} (Strength {(int)card.Strength})";
        if (card.IsCamp) return $"{CampName} #{card.Amount}";
        if (card.IsSkyTower) return SkyTowerName;
        if (card.IsProvisions) return "Provisions";
        if (card.IsParchment) return ParchmentName;
        if (card.IsFarm)
        {
            return card.Farm switch
            {
                FarmKind.Market => MarketName,
                FarmKind.Special => $"Luxury Farm: {Resource(card.Resource)}",
                _ => $"Farm: {Resource(card.Resource)}"
            };
        }
        return card.ToString();
    }

    /// <summary>
    /// Full rules text for a card. Cards in hand are far too small to carry this,
    /// so it lives in the inspector panel (hover a card to read it) - that's where
    /// a new player finds out what a Hellgate is or where a Luxury Farm may go.
    /// </summary>
    public static string CardDescription(CardData card)
    {
        if (card == null) return "";

        if (card.IsTerritory)
            return "Claim this tile. Connected tiles you own form a Domain, which scores Strength x Wealth at the end of each round.";

        if (card.IsCity)
        {
            string extra = card.Strength == CityStrength.Strength3
                ? $" Strength 3 citadels can only be built on {TerrainShort(TileData.TileType.Mountain)}."
                : "";
            return $"Build on a tile you own. Adds {(int)card.Strength} Strength to that Domain.{extra}";
        }

        if (card.IsCamp)
            return $"Claim any empty tile, even one you have no card for. The claim is provisional: whoever plays that tile's real Territory card evicts you. Lower numbers resolve first (#{card.Amount}).";

        if (card.IsSkyTower)
            return "Build on two tiles you own in different Domains. The two Domains merge into one, however far apart they are.";

        if (card.IsProvisions)
            return "Resolves immediately when picked: draw 2 cards from the deck and play them at once.";

        if (card.IsFarm)
        {
            return card.Farm switch
            {
                FarmKind.Market => "Build on an owned tile with no resource. Counts as whichever common resource its Domain is missing.",
                FarmKind.Special => $"Build on an owned {TerrainShort(card.AllowedTerrain)} tile. Adds {Resource(card.Resource)} to that Domain - luxuries count toward Wealth like any other resource.",
                _ => $"Build on an owned tile that produces nothing. Adds {Resource(card.Resource)} to that Domain."
            };
        }

        if (card.IsParchment)
        {
            return card.Rule switch
            {
                ParchmentRule.Treasure => $"Kept secret until the end of the game, then scores {card.Amount} {ScoreName}.",
                ParchmentRule.PerResource => $"Kept secret until the end of the game, then scores {card.Amount} {ScoreName} for every {Resource(card.Resource)} tile you own.",
                ParchmentRule.PerFief => $"Kept secret until the end of the game, then scores {card.Amount} {ScoreName} per {FiefName} you control.",
                ParchmentRule.LargestFief => $"Kept secret until the end of the game, then scores {card.Amount} {ScoreName} per tile in your largest {FiefName}.",
                ParchmentRule.PerCity => $"Kept secret until the end of the game, then scores {card.Amount} {ScoreName} per {CityName} you built.",
                _ => "A secret pact, scored at the end of the game."
            };
        }

        return "";
    }

    public const string CityName = "Citadel";
    public const string SkyTowerName = "Hellgate";
    public const string CampName = "Warcamp";
    public const string MarketName = "Bazaar";
}
