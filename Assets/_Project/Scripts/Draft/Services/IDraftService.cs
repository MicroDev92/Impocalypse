using System;
using System.Collections.Generic;
using Cards;

namespace Draft.Services
{
    /// <summary>
    /// Interface for the draft phase service.
    /// Manages card dealing, picking, and hand passing.
    /// </summary>
    public interface IDraftService
    {
        /// <summary>
        /// Current round number.
        /// </summary>
        int CurrentRound { get; }

        /// <summary>
        /// Whether the current round passes clockwise.
        /// </summary>
        bool IsClockwise { get; }

        /// <summary>
        /// Number of cards dealt per player.
        /// </summary>
        int CardsPerPlayer { get; }

        /// <summary>
        /// Number of picks allowed per round.
        /// </summary>
        int PicksPerRound { get; }

        /// <summary>
        /// Whether the local player has submitted picks.
        /// </summary>
        bool HasSubmittedPicks { get; }

        /// <summary>
        /// Whether all players have submitted picks.
        /// </summary>
        bool AllPicksSubmitted { get; }

        /// <summary>
        /// Starts the draft phase (first round — builds deck).
        /// </summary>
        void StartDraft();

        /// <summary>
        /// Starts a new game round of drafting (uses existing deck).
        /// </summary>
        void StartNewGameRound();

        /// <summary>
        /// Deals cards to all players.
        /// Only called by master client.
        /// </summary>
        void DealCards();

        /// <summary>
        /// Submits the local player's picks.
        /// </summary>
        /// <param name="coordinates">Selected coordinates</param>
        void SubmitPicks(string[] coordinates);

        /// <summary>
        /// Passes hands to the next player.
        /// Only called by master client.
        /// </summary>
        void PassHands();

        /// <summary>
        /// Ends the draft phase and transitions to build.
        /// </summary>
        void EndDraft();

        /// <summary>
        /// Fired when cards are received.
        /// </summary>
        event Action<string[]> OnCardsReceived;

        /// <summary>
        /// Fired when all players have submitted picks.
        /// </summary>
        event Action<Dictionary<int, string[]>> OnAllPicksSubmitted;

        /// <summary>
        /// Fired when hands are passed.
        /// </summary>
        event Action<bool> OnHandsPassed;

        /// <summary>
        /// Fired when the draft phase ends.
        /// </summary>
        event Action OnDraftEnded;

        /// <summary>
        /// Gets city cards picked by a player during the draft (for construction phase).
        /// </summary>
        IReadOnlyList<CardData> GetPlayerCityCards(int actorNumber);

        /// <summary>
        /// Master broadcasts every player's secret parchments so all clients can compute
        /// identical final scores (call when final-round scoring begins).
        /// </summary>
        void BroadcastParchmentsForReveal();

        /// <summary>
        /// Removes a city card from a player's stock once it has been built, so it is not
        /// re-offered in later rounds' construction phases.
        /// </summary>
        void ConsumeCityCard(int actorNumber, CardData cityCard);

        /// <summary>
        /// Gets the secret parchment cards a player has drafted (scored at game end).
        /// </summary>
        IReadOnlyList<CardData> GetPlayerParchments(int actorNumber);
    }
}
