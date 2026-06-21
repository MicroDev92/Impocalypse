namespace Cards
{
    /// <summary>
    /// Interface for listening to card selection events.
    /// Replaces the static event pattern for better decoupling.
    /// </summary>
    public interface ICardSelectionListener
    {
        /// <summary>
        /// Called when a card is selected (first click - highlight).
        /// </summary>
        /// <param name="card">The selected card</param>
        void OnCardSelected(Card card);

        /// <summary>
        /// Called when a card is confirmed (second click - lock in).
        /// </summary>
        /// <param name="card">The confirmed card</param>
        void OnCardConfirmed(Card card);

        /// <summary>
        /// Called when a card is deselected.
        /// </summary>
        /// <param name="card">The deselected card</param>
        void OnCardDeselected(Card card);
    }
}
