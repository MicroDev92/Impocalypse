using System;
using UnityEngine;

namespace Config
{
    /// <summary>
    /// Shared UI color palette and style constants for the entire game.
    /// Assign a single instance to all UI scripts that need consistent styling.
    /// Create via: Assets > Create > Impocalypse > Game Theme
    /// </summary>
    [CreateAssetMenu(fileName = "GameTheme", menuName = "Impocalypse/Game Theme")]
    public class GameTheme : ScriptableObject
    {
        [Header("Backgrounds")]
        [Tooltip("Main scene background / camera clear color")]
        public Color background = new Color(0.10f, 0.05f, 0.04f, 1f);          // #1a0e0a

        [Tooltip("Panel / card background")]
        public Color panelBackground = new Color(0.14f, 0.08f, 0.06f, 0.95f);  // dark brown

        [Tooltip("Inner content area background (e.g. input fields, nested cards)")]
        public Color surfaceBackground = new Color(0.10f, 0.06f, 0.04f, 1f);

        [Header("Borders & Accents")]
        [Tooltip("Default panel border color")]
        public Color panelBorder = new Color(0.55f, 0.18f, 0.10f, 0.8f);       // muted red-brown

        [Tooltip("Highlighted / active border")]
        public Color activeBorder = new Color(0.90f, 0.45f, 0.12f, 1f);        // bright orange

        [Tooltip("Primary accent (buttons, highlights)")]
        public Color accentPrimary = new Color(0.90f, 0.40f, 0.10f, 1f);       // orange

        [Tooltip("Secondary accent (subtitles, secondary info)")]
        public Color accentSecondary = new Color(0.65f, 0.30f, 0.65f, 1f);     // purple

        [Header("Text")]
        public Color textPrimary = new Color(1f, 0.92f, 0.82f, 1f);            // warm white
        public Color textSecondary = new Color(0.70f, 0.55f, 0.45f, 1f);       // muted tan
        public Color textAccent = new Color(0.95f, 0.55f, 0.15f, 1f);          // orange text
        public Color textOnAccent = new Color(1f, 0.95f, 0.90f, 1f);           // white-on-orange

        [Header("Buttons")]
        public Color buttonPrimary = new Color(0.85f, 0.35f, 0.08f, 1f);       // orange gradient start
        public Color buttonPrimaryEnd = new Color(0.70f, 0.20f, 0.05f, 1f);    // orange gradient end
        public Color buttonSecondary = new Color(0.20f, 0.12f, 0.10f, 0.9f);   // dark muted
        public Color buttonDisabled = new Color(0.25f, 0.18f, 0.15f, 0.6f);

        [Header("Status Colors")]
        public Color readyColor = new Color(0.30f, 0.75f, 0.30f, 1f);          // green
        public Color notReadyColor = new Color(0.80f, 0.25f, 0.20f, 1f);       // red
        public Color warningColor = new Color(0.90f, 0.75f, 0.20f, 1f);        // yellow

        [Header("Phase Accent Colors")]
        public Color draftPhaseColor = new Color(0.90f, 0.75f, 0.30f, 1f);     // gold
        public Color buildPhaseColor = new Color(0.55f, 0.78f, 0.45f, 1f);     // green
        public Color scoringPhaseColor = new Color(0.45f, 0.65f, 0.90f, 1f);   // blue
        public Color gameOverPhaseColor = new Color(0.90f, 0.40f, 0.35f, 1f);  // red

        [Header("Dimensions")]
        [Tooltip("Corner radius for panels (used by code that generates rounded sprites)")]
        public float panelCornerRadius = 8f;

        [Tooltip("Default panel border width")]
        public float panelBorderWidth = 1.5f;

        [Tooltip("Sidebar width as fraction of screen (0-1)")]
        [Range(0.15f, 0.30f)]
        public float sidebarWidthFraction = 0.20f;

        [Tooltip("Top bar height in pixels")]
        public float topBarHeight = 50f;

        [Tooltip("Bottom bar height in pixels")]
        public float bottomBarHeight = 140f;
    }
}
