using Core;

namespace UI.Abstractions
{
    /// <summary>
    /// Interface for the draft phase HUD.
    /// Abstracts HUD functionality for testability.
    /// </summary>
    public interface IDraftHUD
    {
        /// <summary>
        /// Sets the current round number display.
        /// </summary>
        /// <param name="round">Round number (1-based)</param>
        void SetRound(int round);

        /// <summary>
        /// Sets the current phase display.
        /// </summary>
        /// <param name="phase">Current game phase</param>
        void SetPhase(GamePhase phase);

        /// <summary>
        /// Sets the status message display.
        /// </summary>
        /// <param name="status">Status message</param>
        void SetStatus(string status);

        /// <summary>
        /// Updates all HUD elements at once.
        /// </summary>
        /// <param name="round">Round number</param>
        /// <param name="phase">Current phase</param>
        /// <param name="status">Status message</param>
        void UpdateHUD(int round, GamePhase phase, string status);

        /// <summary>
        /// Shows the HUD.
        /// </summary>
        void Show();

        /// <summary>
        /// Hides the HUD.
        /// </summary>
        void Hide();

        /// <summary>
        /// Whether the HUD is currently visible.
        /// </summary>
        bool IsVisible { get; }
    }
}
