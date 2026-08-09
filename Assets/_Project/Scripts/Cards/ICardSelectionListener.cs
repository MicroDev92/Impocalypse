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
        /// <returns>
        /// False if the pick was refused (the quota is already full). The card must
        /// then leave its visual state alone - otherwise it turns "confirmed" green
        /// for a pick that was never accepted.
        /// </returns>
        bool OnCardConfirmed(Card card);

        /// <summary>
        /// Called when a confirmed card is clicked again to take the pick back.
        /// Picks are only submitted explicitly, so a misclick must be undoable.
        /// </summary>
        void OnCardUnconfirmed(Card card);

        /// <summary>
        /// Called when a card is deselected.
        /// </summary>
        /// <param name="card">The deselected card</param>
        void OnCardDeselected(Card card);
    }
}
