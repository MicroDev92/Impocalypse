using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Config;
using Players;
using UI;

/// <summary>
/// One player in the left sidebar: portrait, name, infamy, and what they hold.
///
///   [portrait]  NAME (YOU)              [skull] 12
///               [tile] 5  [charwood] 2  [bloodroot] 1  [souls] 0
///
/// The card builds its own contents in code, the same way the pact and domain
/// rows do, so it always uses the shared icon set and display font. Whatever the
/// prefab holds besides the root (background image, layout element, this
/// component) is discarded on Initialize.
///
/// The portrait ring carries the player's colour; the local player's name is
/// additionally marked "(you)" and drawn brighter, because colour alone tells
/// players apart from each other but not you from them.
/// </summary>
public class PlayerCardUI : MonoBehaviour
{
    public const float Height = 64f;
    private const float PortraitSize = 52f;
    private const float Padding = 6f;

    [Header("Root")]
    [SerializeField] private Image cardBackground;

    // Wired by the old prefab generator (GameSceneUISetup). No longer read: the
    // card rebuilds itself. Kept so that generator and existing prefabs still load.
    [SerializeField, HideInInspector] private TMP_Text playerNameText;
    [SerializeField, HideInInspector] private Image leftBorder;
    [SerializeField, HideInInspector] private TMP_Text scoreText;
    [SerializeField, HideInInspector] private TMP_Text territoryCountText;
    [SerializeField, HideInInspector] private TMP_Text woodCountText;
    [SerializeField, HideInInspector] private TMP_Text carrotCountText;
    [SerializeField, HideInInspector] private TMP_Text fishCountText;

    private TMP_Text _name, _score, _tiles, _wood, _root, _souls;
    private PlayerData _playerData;
    private Color _playerColor;
    private bool _isLocal;

    public PlayerData PlayerData => _playerData;

    public void Initialize(PlayerData data, Color playerColor, Sprite portrait, bool isLocalPlayer = false)
    {
        _playerData = data;
        _playerColor = playerColor;
        _isLocal = isLocalPlayer;

        Build(portrait);

        _name.text = isLocalPlayer ? $"{data.Nickname} (you)" : data.Nickname;
        _name.color = isLocalPlayer ? IconRow.Bright : IconRow.Soft;
        UpdateStats(0, 0, 0, 0, 0);
    }

    private void Build(Sprite portrait)
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        var icons = UIIconSet.Instance;

        if (cardBackground == null) cardBackground = GetComponent<Image>();
        if (cardBackground != null)
        {
            if (icons != null && icons.panelInset != null)
            {
                cardBackground.sprite = icons.panelInset;
                cardBackground.type = Image.Type.Sliced;
            }
            cardBackground.raycastTarget = false;
            ApplyBackground(true);
        }

        if (TryGetComponent<LayoutElement>(out var element))
        {
            element.minHeight = element.preferredHeight = Height;
            element.flexibleWidth = 1f;
        }

        // Portrait, or a plain disc in the player's colour if the art is missing.
        var portraitRect = Child("Portrait", typeof(Image));
        portraitRect.anchorMin = portraitRect.anchorMax = new Vector2(0f, 0.5f);
        portraitRect.pivot = new Vector2(0f, 0.5f);
        portraitRect.sizeDelta = new Vector2(PortraitSize, PortraitSize);
        portraitRect.anchoredPosition = new Vector2(Padding, 0f);
        var portraitImage = portraitRect.GetComponent<Image>();
        portraitImage.sprite = portrait;
        portraitImage.color = portrait != null ? Color.white : _playerColor;
        portraitImage.preserveAspect = true;
        portraitImage.raycastTarget = false;

        float left = Padding + PortraitSize + 8f;

        // Top line: name on the left, infamy on the right.
        var top = Child("Top");
        top.anchorMin = new Vector2(0f, 0.5f);
        top.anchorMax = new Vector2(1f, 1f);
        top.offsetMin = new Vector2(left, -2f);
        top.offsetMax = new Vector2(-Padding - 6f, -Padding);

        var scoreRow = IconRow.Create(top, 26f, 4f);
        scoreRow.anchorMin = new Vector2(1f, 0f);
        scoreRow.anchorMax = new Vector2(1f, 1f);
        scoreRow.pivot = new Vector2(1f, 0.5f);
        scoreRow.sizeDelta = new Vector2(78f, 0f);
        scoreRow.anchoredPosition = Vector2.zero;
        scoreRow.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleRight;
        IconRow.Icon(scoreRow, icons != null ? icons.infamy : null, 24f);
        _score = IconRow.Number(scoreRow, "0", IconRow.Accent, 22f);

        _name = IconRow.Number(top, "", IconRow.Bright, 18f);
        var nameRect = _name.rectTransform;
        nameRect.anchorMin = Vector2.zero;
        nameRect.anchorMax = Vector2.one;
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = new Vector2(-82f, 0f);
        _name.overflowMode = TextOverflowModes.Ellipsis;

        // Bottom line: tiles held, then the three common resources.
        var stats = IconRow.Create(transform, 24f, 3f);
        stats.anchorMin = new Vector2(0f, 0f);
        stats.anchorMax = new Vector2(1f, 0.5f);
        stats.offsetMin = new Vector2(left, Padding);
        stats.offsetMax = new Vector2(-Padding, 0f);
        _tiles = Stat(stats, icons != null ? icons.tile : null);
        _wood = Stat(stats, icons != null ? icons.charwood : null);
        _root = Stat(stats, icons != null ? icons.bloodroot : null);
        _souls = Stat(stats, icons != null ? icons.souls : null);
    }

    private static TMP_Text Stat(Transform row, Sprite icon)
    {
        IconRow.Icon(row, icon, 22f);
        var label = IconRow.Number(row, "0", IconRow.Bright, 17f);
        // A fixed slot per number keeps the four columns aligned across players.
        var element = label.gameObject.AddComponent<LayoutElement>();
        element.minWidth = element.preferredWidth = 30f;
        return label;
    }

    private RectTransform Child(string name, params System.Type[] components)
    {
        var go = new GameObject(name, typeof(RectTransform));
        foreach (var type in components) go.AddComponent(type);
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        return rt;
    }

    public void UpdateStats(int score, int territories, int wood, int carrot, int fish)
    {
        if (_score == null) return; // not built yet

        _score.text = score.ToString();
        _tiles.text = territories.ToString();
        _wood.text = wood.ToString();
        _root.text = carrot.ToString();
        _souls.text = fish.ToString();
    }

    /// <summary>Highlights this card. Other players' cards sit a step darker than your own.</summary>
    public void SetHighlighted(bool highlighted) => ApplyBackground(highlighted);

    private void ApplyBackground(bool highlighted)
    {
        if (cardBackground == null) return;

        cardBackground.color = highlighted ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);
    }
}
