using UnityEngine;

namespace Cards
{
    /// <summary>
    /// The art used to illustrate a card face. Shared by the draft hand and the
    /// construction buttons so the mapping from card type to sprite lives in one
    /// place - a Hellgate must not look like one thing in hand and another while
    /// building.
    ///
    /// Territory cards are deliberately absent: they draw their own tile's terrain
    /// sprite, so board and card can never disagree.
    /// </summary>
    [System.Serializable]
    public class CardArtSet
    {
        public Sprite citadel;
        public Sprite farm;
        public Sprite bazaar;
        public Sprite warcamp;
        public Sprite hellgate;
        public Sprite pact;
        public Sprite provisions;

        /// <summary>Art for a card, or null when the card has none (e.g. territories).</summary>
        public Sprite SpriteFor(CardData card)
        {
            if (card == null) return null;
            if (card.IsCity) return citadel;
            if (card.IsFarm) return card.Farm == FarmKind.Market ? bazaar : farm;
            if (card.IsCamp) return warcamp;
            if (card.IsSkyTower) return hellgate;
            if (card.IsParchment) return pact;
            if (card.IsProvisions) return provisions;
            return null;
        }
    }
}
