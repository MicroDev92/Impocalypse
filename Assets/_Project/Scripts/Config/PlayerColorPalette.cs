using System;
using UnityEngine;

namespace Config
{
    /// <summary>
    /// Player color palette ScriptableObject.
    /// Defines colors and sprites for each player slot.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerColorPalette", menuName = "Impocalypse/Player Color Palette")]
    public class PlayerColorPalette : ScriptableObject
    {
        [Serializable]
        public class PlayerColorEntry
        {
            [Tooltip("Color name/ID (e.g., 'red', 'blue')")]
            public string colorId;

            [Tooltip("Display name for this color")]
            public string displayName;

            [Tooltip("Primary color")]
            public Color primaryColor = Color.white;

            [Tooltip("Secondary/accent color")]
            public Color secondaryColor = Color.gray;

            [Tooltip("Imp sprite for this color")]
            public Sprite impSprite;

            [Tooltip("UI highlight color")]
            public Color uiHighlightColor = Color.yellow;
        }

        [Header("Player Colors")]
        [Tooltip("Color definitions for each player slot")]
        [SerializeField] private PlayerColorEntry[] playerColors = new PlayerColorEntry[]
        {
            new() { colorId = "red", displayName = "Red", primaryColor = Color.red },
            new() { colorId = "green", displayName = "Green", primaryColor = Color.green },
            new() { colorId = "blue", displayName = "Blue", primaryColor = Color.blue },
            new() { colorId = "purple", displayName = "Purple", primaryColor = new Color(0.5f, 0f, 0.5f) }
        };

        /// <summary>
        /// Gets color entry by index (player slot).
        /// </summary>
        /// <param name="index">Player index (0-based)</param>
        /// <returns>Color entry or null if out of range</returns>
        public PlayerColorEntry GetColorByIndex(int index)
        {
            if (index < 0 || index >= playerColors.Length)
            {
                Debug.LogWarning($"[PlayerColorPalette] Invalid color index: {index}");
                return playerColors.Length > 0 ? playerColors[0] : null;
            }

            return playerColors[index];
        }

        /// <summary>
        /// Gets color entry by color ID.
        /// </summary>
        /// <param name="colorId">Color ID (e.g., "red")</param>
        /// <returns>Color entry or null if not found</returns>
        public PlayerColorEntry GetColorById(string colorId)
        {
            foreach (var entry in playerColors)
            {
                if (entry.colorId.Equals(colorId, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            Debug.LogWarning($"[PlayerColorPalette] Color not found: {colorId}");
            return playerColors.Length > 0 ? playerColors[0] : null;
        }

        /// <summary>
        /// Gets imp sprite for a color ID.
        /// </summary>
        /// <param name="colorId">Color ID (e.g., "red")</param>
        /// <returns>Imp sprite or null</returns>
        public Sprite GetImpSprite(string colorId)
        {
            return GetColorById(colorId)?.impSprite;
        }

        /// <summary>
        /// Gets primary color for a color ID.
        /// </summary>
        /// <param name="colorId">Color ID</param>
        /// <returns>Primary color</returns>
        public Color GetPrimaryColor(string colorId)
        {
            return GetColorById(colorId)?.primaryColor ?? Color.white;
        }

        /// <summary>
        /// Gets the number of available colors.
        /// </summary>
        public int ColorCount => playerColors.Length;

        /// <summary>
        /// Gets all color IDs.
        /// </summary>
        public string[] GetAllColorIds()
        {
            var ids = new string[playerColors.Length];
            for (int i = 0; i < playerColors.Length; i++)
            {
                ids[i] = playerColors[i].colorId;
            }
            return ids;
        }
    }
}
