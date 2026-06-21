using UnityEngine;

namespace Config
{
    /// <summary>
    /// Resource definition ScriptableObject.
    /// Defines a single resource type with its properties.
    /// </summary>
    [CreateAssetMenu(fileName = "ResourceDefinition", menuName = "Impocalypse/Resource Definition")]
    public class ResourceDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this resource")]
        [SerializeField] private string resourceId;

        [Tooltip("Display name shown in UI")]
        [SerializeField] private string displayName;

        [Header("Visuals")]
        [Tooltip("Icon sprite for UI")]
        [SerializeField] private Sprite icon;

        [Tooltip("Color used for tiles of this resource")]
        [SerializeField] private Color tileColor = Color.white;

        [Tooltip("Sprite used for tiles of this resource")]
        [SerializeField] private Sprite tileSprite;

        [Header("Gameplay")]
        [Tooltip("Base value when collected")]
        [SerializeField] private int baseValue = 1;

        [Tooltip("Spawn weight (higher = more common)")]
        [Range(0f, 1f)]
        [SerializeField] private float spawnWeight = 0.1f;

        [Header("Audio")]
        [Tooltip("Sound played when collected")]
        [SerializeField] private AudioClip collectSound;

        // Properties
        public string ResourceId => resourceId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public Color TileColor => tileColor;
        public Sprite TileSprite => tileSprite;
        public int BaseValue => baseValue;
        public float SpawnWeight => spawnWeight;
        public AudioClip CollectSound => collectSound;

        /// <summary>
        /// Validates configuration values.
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                resourceId = name.ToLowerInvariant().Replace(" ", "_");
            }

            if (string.IsNullOrEmpty(displayName))
            {
                displayName = name;
            }

            baseValue = Mathf.Max(0, baseValue);
        }
    }
}
