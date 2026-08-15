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

    public void Initialize(PlayerData data, Color playerColor, GameTheme theme, bool isLocalPlayer = false)
    {
        _playerData = data;
        _playerColor = playerColor;

        if (playerNameText != null)
        {
            // The colour bars tell players apart from each other but not you from them,
            // and four friends with similar names cannot otherwise find themselves on
            // screen. Text rather than styling on purpose: it survives any later
            // restyle of this card, and it cannot be missed.
            playerNameText.text = isLocalPlayer ? $"{data.Nickname}  (you)" : data.Nickname;
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
        HideEmptyIcons();
    }

    /// <summary>
    /// Hides any icon Image that has no sprite — those render as solid boxes that cover
    /// the score/stat text. Keeps the card background and the player-coloured left border.
    /// Once real sprites are assigned in the prefab, the icons show automatically.
    /// </summary>
    private void HideEmptyIcons()
    {
        foreach (var img in GetComponentsInChildren<Image>(true))
        {
            if (img == cardBackground || img == leftBorder) continue;
            img.enabled = img.sprite != null;
        }
    }

    public void UpdateStats(int score, int territories, int wood, int carrot, int fish)
    {
        if (scoreText != null)
            scoreText.text = $"{GameLexicon.ScoreName}: {score}";

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
