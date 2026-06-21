namespace Core
{
    /// <summary>
    /// Represents the current phase of gameplay.
    /// </summary>
    public enum GamePhase
    {
        /// <summary>
        /// Lobby/waiting phase before game starts.
        /// </summary>
        Lobby,

        /// <summary>
        /// Draft phase where players pick cards.
        /// </summary>
        Draft,

        /// <summary>
        /// Build phase where imps are placed on the grid.
        /// </summary>
        Build,

        /// <summary>
        /// Scoring phase where points are calculated.
        /// </summary>
        Scoring,

        /// <summary>
        /// Game over phase showing final results.
        /// </summary>
        GameOver
    }
}
