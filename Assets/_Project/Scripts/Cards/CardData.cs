namespace Cards
{
    public enum CardType
    {
        Territory,
        City,
        Farm,
        Camp,
        SkyTower,
        Provisions,
        Parchment
    }

    public enum CityStrength
    {
        None = 0,
        Strength1 = 1,
        Strength2 = 2,
        Strength3 = 3
    }

    /// <summary>
    /// The flavour of a Farm card:
    ///  - Common: adds a chosen basic resource to a resource-less tile.
    ///  - Special: adds a luxury resource, restricted to a matching terrain.
    ///  - Market: a wildcard that counts as any one common (player's choice).
    /// </summary>
    public enum FarmKind
    {
        None,
        Common,
        Special,
        Market
    }

    /// <summary>
    /// Immutable data for a single card in the deck.
    /// Territory cards map to grid coordinates; City cards have a strength; Farm cards
    /// add a resource (see FarmKind/ResourceType). Each card has a unique Id for reliable
    /// matching during hand-passing and placement.
    /// </summary>
    public class CardData
    {
        private static int _nextUniqueId;

        public string Id { get; }
        public CardType Type { get; }
        public string Coordinate { get; }                 // Territory
        public CityStrength Strength { get; }             // City
        public FarmKind Farm { get; }                     // Farm
        public ResourceType Resource { get; }             // Farm (common/special); WildcardCommon for Market
        public TileData.TileType AllowedTerrain { get; }  // Special Farm placement restriction
        public int UniqueId { get; }

        private CardData(string id, CardType type, string coordinate, CityStrength strength,
                         FarmKind farm, ResourceType resource, TileData.TileType allowedTerrain, int uniqueId)
        {
            Id = id;
            Type = type;
            Coordinate = coordinate;
            Strength = strength;
            Farm = farm;
            Resource = resource;
            AllowedTerrain = allowedTerrain;
            UniqueId = uniqueId;
        }

        // ---- Factories ----

        public static CardData Territory(string coordinate)
        {
            int uid = _nextUniqueId++;
            return new CardData($"T:{coordinate}", CardType.Territory, coordinate, CityStrength.None,
                                FarmKind.None, ResourceType.None, TileData.TileType.Grass, uid);
        }

        public static CardData City(CityStrength strength)
        {
            int uid = _nextUniqueId++;
            return new CardData($"C:{(int)strength}#{uid}", CardType.City, null, strength,
                                FarmKind.None, ResourceType.None, TileData.TileType.Grass, uid);
        }

        /// <summary>Common Farm: adds a basic resource to a resource-less tile.</summary>
        public static CardData CommonFarm(ResourceType common)
        {
            int uid = _nextUniqueId++;
            return new CardData($"FC:{(int)common}#{uid}", CardType.Farm, null, CityStrength.None,
                                FarmKind.Common, common, TileData.TileType.Grass, uid);
        }

        /// <summary>Special Farm: adds a luxury resource, restricted to a terrain type.</summary>
        public static CardData SpecialFarm(ResourceType special, TileData.TileType allowedTerrain)
        {
            int uid = _nextUniqueId++;
            return new CardData($"FS:{(int)special}:{(int)allowedTerrain}#{uid}", CardType.Farm, null, CityStrength.None,
                                FarmKind.Special, special, allowedTerrain, uid);
        }

        /// <summary>Market: a wildcard that counts as any one common (player's choice).</summary>
        public static CardData Market()
        {
            int uid = _nextUniqueId++;
            return new CardData($"FM:#{uid}", CardType.Farm, null, CityStrength.None,
                                FarmKind.Market, ResourceType.WildcardCommon, TileData.TileType.Grass, uid);
        }

        // ---- Queries ----

        public bool IsTerritory => Type == CardType.Territory;
        public bool IsCity => Type == CardType.City;
        public bool IsFarm => Type == CardType.Farm;

        // ---- Serialization (network-safe strings) ----

        public string Serialize()
        {
            switch (Type)
            {
                case CardType.Territory:
                    return $"T:{Coordinate}";
                case CardType.City:
                    return $"C:{(int)Strength}#{UniqueId}";
                case CardType.Farm:
                    switch (Farm)
                    {
                        case FarmKind.Common:  return $"FC:{(int)Resource}#{UniqueId}";
                        case FarmKind.Special: return $"FS:{(int)Resource}:{(int)AllowedTerrain}#{UniqueId}";
                        case FarmKind.Market:  return $"FM:#{UniqueId}";
                    }
                    break;
            }
            return Id;
        }

        public static CardData Deserialize(string data)
        {
            if (string.IsNullOrEmpty(data)) return null;

            int colon = data.IndexOf(':');
            if (colon < 1) return null;

            string prefix = data.Substring(0, colon);
            string rest = data.Substring(colon + 1);

            switch (prefix)
            {
                case "T":
                    return Territory(rest);

                case "C":
                {
                    SplitUid(rest, out string sv, out int uid);
                    if (int.TryParse(sv, out int s) && s >= 1 && s <= 3)
                        return new CardData($"C:{s}#{uid}", CardType.City, null, (CityStrength)s,
                                            FarmKind.None, ResourceType.None, TileData.TileType.Grass, uid);
                    return null;
                }

                case "FC":
                {
                    SplitUid(rest, out string rv, out int uid);
                    if (int.TryParse(rv, out int r))
                        return new CardData($"FC:{r}#{uid}", CardType.Farm, null, CityStrength.None,
                                            FarmKind.Common, (ResourceType)r, TileData.TileType.Grass, uid);
                    return null;
                }

                case "FS":
                {
                    SplitUid(rest, out string body, out int uid);
                    var parts = body.Split(':');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int r) && int.TryParse(parts[1], out int terr))
                        return new CardData($"FS:{r}:{terr}#{uid}", CardType.Farm, null, CityStrength.None,
                                            FarmKind.Special, (ResourceType)r, (TileData.TileType)terr, uid);
                    return null;
                }

                case "FM":
                {
                    SplitUid(rest, out _, out int uid);
                    return new CardData($"FM:#{uid}", CardType.Farm, null, CityStrength.None,
                                        FarmKind.Market, ResourceType.WildcardCommon, TileData.TileType.Grass, uid);
                }
            }

            return null;
        }

        /// <summary>Splits a "value#uid" tail, assigning a fresh uid if none is present.</summary>
        private static void SplitUid(string s, out string value, out int uid)
        {
            uid = _nextUniqueId++;
            int hash = s.IndexOf('#');
            if (hash >= 0)
            {
                value = s.Substring(0, hash);
                if (int.TryParse(s.Substring(hash + 1), out int parsed)) uid = parsed;
            }
            else
            {
                value = s;
            }
        }

        public override string ToString()
        {
            return Type switch
            {
                CardType.Territory => $"Territory({Coordinate})",
                CardType.City => $"City(Str{(int)Strength})",
                CardType.Farm => Farm == FarmKind.Market ? "Market" : $"Farm({Farm}:{Resource})",
                _ => Type.ToString()
            };
        }

        /// <summary>Resets the unique ID counter (call at game start).</summary>
        public static void ResetUniqueIds()
        {
            _nextUniqueId = 0;
        }
    }
}
