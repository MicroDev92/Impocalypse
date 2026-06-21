using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Config;
using Players;

/// <summary>
/// UI component for a single player card in the sidebar.
/// Displays player name, score, territory count, and per-resource counts.
/// Created by PlayerSidebar at runtime from a prefab.
///
/// Expected hierarchy:
///   PlayerCard (this component + Image background + VerticalLayoutGroup)
///   ├── LeftBorder (Image, 4px wide, player-colored)
///   ├── Content (HorizontalLayoutGroup)
///   │   ├── HeaderRow
///   │   │   ├── PlayerName (TMP_Text, left-aligned)
///   │   │   └── ScoreText  (TMP_Text, right-aligned)
///   │   └── StatsRow (HorizontalLayoutGroup)
///   │       ├── TerritoryStat: icon + TMP_Text
///   │       ├── WoodStat:      icon + TMP_Text
///   │       ├── CarrotStat:    icon + TMP_Text
///   │       └── FishStat:      icon + TMP_Text
/// </summary>
public class PlayerCardUI : MonoBehaviour
{
    [Header("Player Identity")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Image leftBorder;
    [SerializeField] private Image cardBackground;

    [Header("Score")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Stats")]
    [SerializeField] private TMP_Text territoryCountText;
    [SerializeField] private TMP_Text woodCountText;
    [SerializeField] private TMP_Text carrotCountText;
    [SerializeField] private TMP_Text fishCountText;

    [Header("Stat Icons (optional — assign in prefab)")]
    [SerializeField] private Image territoryIcon;
    [SerializeField] private Image woodIcon;
    [SerializeField] private Image carrotIcon;
    [SerializeField] private Image fishIcon;

    private PlayerData _playerData;
    private Color _playerColor;

    public PlayerData PlayerData => _playerData;

    public void Initialize(PlayerData data, Color playerColor, GameTheme theme)
    {
        _playerData = data;
        _playerColor = playerColor;

        if (playerNameText != null)
        {
            playerNameText.text = data.Nickname;
            if (theme != null) playerNameText.color = theme.textPrimary;
        }

        if (leftBorder != null)
            leftBorder.color = playerColor;

        if (cardBackground != null && theme != null)
            cardBackground.color = theme.panelBackground;

        if (scoreText != null && theme != null)
            scoreText.color = theme.textAccent;

        // Set stat text colors
        if (theme != null)
        {
            SetTextColor(territoryCountText, theme.textSecondary);
            SetTextColor(woodCountText, theme.textSecondary);
            SetTextColor(carrotCountText, theme.textSecondary);
            SetTextColor(fishCountText, theme.textSecondary);
        }

        UpdateStats(0, 0, 0, 0, 0);
    }

    public void UpdateStats(int score, int territories, int wood, int carrot, int fish)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";

        if (territoryCountText != null)
            territoryCountText.text = territories.ToString();

        if (woodCountText != null)
            woodCountText.text = wood.ToString();

        if (carrotCountText != null)
            carrotCountText.text = carrot.ToString();

        if (fishCountText != null)
            fishCountText.text = fish.ToString();
    }

    /// <summary>
    /// Highlights this card (e.g. when it's the player's turn).
    /// </summary>
    public void SetHighlighted(bool highlighted, GameTheme theme)
    {
        if (leftBorder != null)
        {
            // Brighter border when highlighted
            leftBorder.color = highlighted
                ? _playerColor
                : _playerColor * 0.6f;
        }

        if (cardBackground != null && theme != null)
        {
            cardBackground.color = highlighted
                ? Color.Lerp(theme.panelBackground, _playerColor, 0.08f)
                : theme.panelBackground;
        }
    }

    private static void SetTextColor(TMP_Text text, Color color)
    {
        if (text != null) text.color = color;
    }
}
