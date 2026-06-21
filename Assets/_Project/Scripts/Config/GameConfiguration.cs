using UnityEngine;

namespace Config
{
    /// <summary>
    /// Main game configuration ScriptableObject.
    /// Contains global game settings.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfiguration", menuName = "Impocalypse/Game Configuration")]
    public class GameConfiguration : ScriptableObject
    {
        [Header("Round Settings")]
        [Tooltip("Maximum number of rounds in a game")]
        [Range(1, 10)]
        [SerializeField] private int maxRounds = 4;

        [Tooltip("Time limit for draft phase in seconds (0 = no limit)")]
        [SerializeField] private float draftTimeLimit = 0f;

        [Tooltip("Time limit for build phase in seconds (0 = no limit)")]
        [SerializeField] private float buildTimeLimit = 0f;

        [Header("Player Settings")]
        [Tooltip("Minimum players required to start")]
        [Range(1, 4)]
        [SerializeField] private int minPlayers = 2;

        [Tooltip("Maximum players allowed")]
        [Range(2, 4)]
        [SerializeField] private int maxPlayers = 4;

        [Header("Grid Settings")]
        [Tooltip("Grid width in tiles")]
        [SerializeField] private int gridWidth = 12;

        [Tooltip("Grid height in tiles")]
        [SerializeField] private int gridHeight = 12;

        [Tooltip("Playable area width (excluding borders)")]
        [SerializeField] private int playableWidth = 10;

        [Tooltip("Playable area height (excluding borders)")]
        [SerializeField] private int playableHeight = 10;

        [Header("Debug")]
        [Tooltip("Enable test mode with reduced card counts")]
        [SerializeField] private bool testMode = false;

        // Properties
        public int MaxRounds => maxRounds;
        public float DraftTimeLimit => draftTimeLimit;
        public float BuildTimeLimit => buildTimeLimit;
        public int MinPlayers => minPlayers;
        public int MaxPlayers => maxPlayers;
        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public int PlayableWidth => playableWidth;
        public int PlayableHeight => playableHeight;
        public bool TestMode => testMode;

        /// <summary>
        /// Validates configuration values.
        /// </summary>
        private void OnValidate()
        {
            maxRounds = Mathf.Max(1, maxRounds);
            minPlayers = Mathf.Clamp(minPlayers, 1, maxPlayers);
            maxPlayers = Mathf.Max(minPlayers, maxPlayers);
            gridWidth = Mathf.Max(playableWidth + 2, gridWidth);
            gridHeight = Mathf.Max(playableHeight + 2, gridHeight);
        }
    }
}
