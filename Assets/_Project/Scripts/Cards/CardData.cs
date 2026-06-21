namespace Cards
{
    public enum CardType
    {
        Territory,
        City
    }

    public enum CityStrength
    {
        None = 0,
        Strength1 = 1,
        Strength2 = 2,
        Strength3 = 3
    }

    /// <summary>
    /// Immutable data for a single card in the deck.
    /// Territory cards map to grid coordinates. City cards have a strength value.
    /// Each card has a unique Id for reliable matching during hand-passing.
    /// </summary>
    public class CardData
    {
        private static int _nextUniqueId;

        public string Id { get; }
        public CardType Type { get; }
        public string Coordinate { get; }
        public CityStrength Strength { get; }
        public int UniqueId { get; }

        public CardData(string id, CardType type, string coordinate, CityStrength strength, int uniqueId)
        {
            Id = id;
            Type = type;
            Coordinate = coordinate;
            Strength = strength;
            UniqueId = uniqueId;
        }

        public static CardData Territory(string coordinate)
        {
            int uid = _nextUniqueId++;
            return new CardData($"T:{coordinate}", CardType.Territory, coordinate, CityStrength.None, uid);
        }

        public static CardData City(CityStrength strength)
        {
            int uid = _nextUniqueId++;
            return new CardData($"C:{(int)strength}#{uid}", CardType.City, null, strength, uid);
        }

        /// <summary>
        /// Serializes to network-safe string format.
        /// Territory: "T:A1"  City: "C:2#17" (includes unique id)
        /// </summary>
        public string Serialize()
        {
            return Type == CardType.Territory
                ? $"T:{Coordinate}"
                : $"C:{(int)Strength}#{UniqueId}";
        }

        /// <summary>
        /// Deserializes from network string format.
        /// </summary>
        public static CardData Deserialize(string data)
        {
            if (string.IsNullOrEmpty(data) || data.Length < 3 || data[1] != ':')
                return null;

            char prefix = data[0];
            string value = data.Substring(2);

            if (prefix == 'T')
            {
                return Territory(value);
            }
            else if (prefix == 'C')
            {
                // Parse "2#17" → strength=2, uniqueId=17
                int hashIndex = value.IndexOf('#');
                string strengthStr;
                int uid = _nextUniqueId++;

                if (hashIndex >= 0)
                {
                    strengthStr = value.Substring(0, hashIndex);
                    string uidStr = value.Substring(hashIndex + 1);
                    if (int.TryParse(uidStr, out int parsedUid))
                        uid = parsedUid;
                }
                else
                {
                    strengthStr = value;
                }

                if (int.TryParse(strengthStr, out int strength) && strength >= 1 && strength <= 3)
                {
                    return new CardData($"C:{strength}#{uid}", CardType.City, null, (CityStrength)strength, uid);
                }
            }

            return null;
        }

        public bool IsTerritory => Type == CardType.Territory;
        public bool IsCity => Type == CardType.City;

        public override string ToString()
        {
            return Type == CardType.Territory
                ? $"Territory({Coordinate})"
                : $"City(Str{(int)Strength})";
        }

        /// <summary>
        /// Resets the unique ID counter (call at game start).
        /// </summary>
        public static void ResetUniqueIds()
        {
            _nextUniqueId = 0;
        }
    }
}
