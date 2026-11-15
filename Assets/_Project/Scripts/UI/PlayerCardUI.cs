using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cards;
using Players;
using UI;
using _Project.Scripts.Utility;

/// <summary>
/// One player in the left sidebar: portrait, name, infamy, and what they hold.
///
///   [portrait]  NAME (YOU)                    [skull] 12
///               (tile 5) (charwood 2) (bloodroot 1) (souls 0)
///
/// The card is also the way to read the board (see <see cref="BoardFocus"/>),
/// for ANY player's card, whoever is looking:
///  - portrait or name: light up that player's whole territory;
///  - a stat chip: light up that player's tiles of that kind. The tiles chip is
///    the whole territory, so it and the portrait are the same focus.
/// The chips are deliberately separate, pill-shaped targets and the stats row
/// never selects the player, so a slightly-off tap cannot do the other thing.
///
/// The card builds its own contents in code, the same way the pact and domain
/// rows do, so it always uses the shared icon set and display font. Whatever the
/// prefab holds besides the root (background image, layout element, this
/// component) is discarded on Initialize.
/// </summary>
public class PlayerCardUI : MonoBehaviour
{
    /// <summary>
    /// Phones get a taller card with bigger chips: a 24-unit chip is fine under a
    /// mouse and too small under a thumb. Settable so the large layout can be
    /// checked in the Editor.
    /// </summary>
    public static bool LargeCards { get; set; } = Application.isMobilePlatform;

    /// <summary>Card height for the current layout (used by the prefab setup).</summary>
    public static float Height => LargeCards ? 96f : 64f;

    private const float Padding = 6f;

    private static readonly Color ChipIdle = new(0.24f, 0.17f, 0.14f, 1f);
    private static readonly Color ChipActive = new(0.47f, 0.29f, 0.16f, 1f);
    private static readonly Color Gold = new(1f, 0.83f, 0.42f, 1f);

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

    private sealed class Chip
    {
        public string Key;
        public Image Body;
        public Image Ring;
        public TMP_Text Number;
    }

    private readonly List<Chip> _chips = new();
    private TMP_Text _name, _score;
    private Image _portraitRing;
    private PlayerData _playerData;
    private Color _playerColor;
    private bool _isLocal;

    public PlayerData PlayerData => _playerData;

    private string PlayerKey => $"player:{_playerData.ActorNumber}";

    public void Initialize(PlayerData data, Color playerColor, Sprite portrait, bool isLocalPlayer = false)
    {
        _playerData = data;
        _playerColor = playerColor;
        _isLocal = isLocalPlayer;

        Build(portrait);

        _name.text = isLocalPlayer ? $"{data.Nickname} (you)" : data.Nickname;
        UpdateStats(0, 0, 0, 0, 0);

        BoardFocus.Changed -= RefreshActive;
        BoardFocus.Changed += RefreshActive;
        RefreshActive();
    }

    private void OnDestroy() => BoardFocus.Changed -= RefreshActive;

    // ==========================================
    // BUILD
    // ==========================================

    private void Build(Sprite portrait)
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
        _chips.Clear();

        bool large = LargeCards;
        float height = Height;
        float portraitSize = large ? 66f : 52f;
        float chipHeight = large ? 40f : 24f;
        float iconSize = large ? 28f : 19f;
        float numberSize = large ? 20f : 16f;
        float nameSize = large ? 20f : 18f;

        var icons = UIIconSet.Instance;

        if (cardBackground == null) cardBackground = GetComponent<Image>();
        if (cardBackground != null)
        {
            if (icons != null && icons.panelInset != null)
            {
                cardBackground.sprite = icons.panelInset;
                cardBackground.type = Image.Type.Sliced;
            }
            cardBackground.color = Color.white;
            cardBackground.raycastTarget = false;
        }

        if (TryGetComponent<LayoutElement>(out var element))
        {
            element.minHeight = element.preferredHeight = height;
            element.flexibleWidth = 1f;
        }

        // Ring behind the portrait, shown while this player is the focus.
        var ringRect = Child("PortraitRing", typeof(Image));
        ringRect.anchorMin = ringRect.anchorMax = new Vector2(0f, 0.5f);
        ringRect.pivot = new Vector2(0f, 0.5f);
        ringRect.sizeDelta = new Vector2(portraitSize + 8f, portraitSize + 8f);
        ringRect.anchoredPosition = new Vector2(Padding - 4f, 0f);
        _portraitRing = ringRect.GetComponent<Image>();
        _portraitRing.sprite = icons != null ? icons.portraitRing : null;
        _portraitRing.color = Gold;
        _portraitRing.raycastTarget = false;

        // Portrait, or a plain disc in the player's colour if the art is missing.
        var portraitRect = Child("Portrait", typeof(Image), typeof(Button));
        portraitRect.anchorMin = portraitRect.anchorMax = new Vector2(0f, 0.5f);
        portraitRect.pivot = new Vector2(0f, 0.5f);
        portraitRect.sizeDelta = new Vector2(portraitSize, portraitSize);
        portraitRect.anchoredPosition = new Vector2(Padding, 0f);
        var portraitImage = portraitRect.GetComponent<Image>();
        portraitImage.sprite = portrait;
        portraitImage.color = portrait != null ? Color.white : _playerColor;
        portraitImage.preserveAspect = true;
        portraitRect.GetComponent<Button>().onClick.AddListener(TogglePlayer);

        float left = Padding + portraitSize + 8f;
        float chipsTop = Padding + chipHeight + 2f;   // from the bottom edge

        // Top line: name on the left, infamy on the right. The whole line is a
        // second, wider target for "show this player".
        var top = Child("Top", typeof(Image), typeof(Button));
        top.anchorMin = new Vector2(0f, 0f);
        top.anchorMax = new Vector2(1f, 1f);
        top.offsetMin = new Vector2(left, chipsTop);
        top.offsetMax = new Vector2(-Padding - 4f, -Padding + 2f);
        top.GetComponent<Image>().color = Color.clear;   // invisible, but a raycast target
        top.GetComponent<Button>().onClick.AddListener(TogglePlayer);

        var scoreRow = IconRow.Create(top, 26f, 4f);
        scoreRow.anchorMin = new Vector2(1f, 0f);
        scoreRow.anchorMax = new Vector2(1f, 1f);
        scoreRow.pivot = new Vector2(1f, 0.5f);
        scoreRow.sizeDelta = new Vector2(78f, 0f);
        scoreRow.anchoredPosition = Vector2.zero;
        scoreRow.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleRight;
        IconRow.Icon(scoreRow, icons != null ? icons.infamy : null, 24f);
        _score = IconRow.Number(scoreRow, "0", IconRow.Accent, 22f);

        _name = IconRow.Number(top, "", IconRow.Bright, nameSize);
        var nameRect = _name.rectTransform;
        nameRect.anchorMin = Vector2.zero;
        nameRect.anchorMax = Vector2.one;
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = new Vector2(-82f, 0f);
        _name.overflowMode = TextOverflowModes.Ellipsis;

        // Bottom line: four chips sharing the width equally, so they always fit.
        var stats = Child("Stats", typeof(HorizontalLayoutGroup));
        stats.anchorMin = new Vector2(0f, 0f);
        stats.anchorMax = new Vector2(1f, 0f);
        stats.pivot = new Vector2(0.5f, 0f);
        stats.offsetMin = new Vector2(left, Padding);
        stats.offsetMax = new Vector2(-Padding, Padding + chipHeight);
        var row = stats.GetComponent<HorizontalLayoutGroup>();
        row.spacing = 4f;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = true;
        row.childForceExpandHeight = true;

        AddChip(stats, icons, icons != null ? icons.tile : null, PlayerKey, iconSize, numberSize, null);
        AddChip(stats, icons, icons != null ? icons.charwood : null, ResourceKey(ResourceType.Wood), iconSize, numberSize, ResourceType.Wood);
        AddChip(stats, icons, icons != null ? icons.bloodroot : null, ResourceKey(ResourceType.Carrot), iconSize, numberSize, ResourceType.Carrot);
        AddChip(stats, icons, icons != null ? icons.souls : null, ResourceKey(ResourceType.Fish), iconSize, numberSize, ResourceType.Fish);
    }

    private string ResourceKey(ResourceType resource) => $"res:{_playerData.ActorNumber}:{(int)resource}";

    /// <summary>One stat as a pill: icon + number, the whole pill tappable. Null resource = all tiles.</summary>
    private void AddChip(Transform parent, UIIconSet icons, Sprite icon, string key, float iconSize, float numberSize, ResourceType? resource)
    {
        var go = new GameObject("Chip", typeof(RectTransform), typeof(Image), typeof(Button), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        var body = go.GetComponent<Image>();
        body.sprite = icons != null ? icons.chip : null;
        body.type = Image.Type.Sliced;
        body.color = ChipIdle;

        var layout = go.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(7, 4, 0, 0);
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        go.GetComponent<LayoutElement>().flexibleWidth = 1f;

        IconRow.Icon(go.transform, icon, iconSize);
        var number = IconRow.Number(go.transform, "0", IconRow.Bright, numberSize);

        // Outline shown while this chip is the focus. Not part of the layout.
        var ringGo = new GameObject("Ring", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        var ringRect = (RectTransform)ringGo.transform;
        ringRect.SetParent(go.transform, false);
        ringRect.anchorMin = Vector2.zero;
        ringRect.anchorMax = Vector2.one;
        ringRect.offsetMin = ringRect.offsetMax = Vector2.zero;
        ringGo.GetComponent<LayoutElement>().ignoreLayout = true;
        var ring = ringGo.GetComponent<Image>();
        ring.sprite = icons != null ? icons.chipRing : null;
        ring.type = Image.Type.Sliced;
        ring.color = Gold;
        ring.raycastTarget = false;
        ring.enabled = false;

        if (resource.HasValue)
        {
            var wanted = resource.Value;
            go.GetComponent<Button>().onClick.AddListener(() => BoardFocus.Toggle(key, () => TilesWith(wanted)));
        }
        else
        {
            go.GetComponent<Button>().onClick.AddListener(TogglePlayer);
        }

        _chips.Add(new Chip { Key = key, Body = body, Ring = ring, Number = number });
    }

    private RectTransform Child(string name, params System.Type[] components)
    {
        var go = new GameObject(name, typeof(RectTransform));
        foreach (var type in components) go.AddComponent(type);
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        return rt;
    }

    // ==========================================
    // BOARD FOCUS
    // ==========================================

    private void TogglePlayer() => BoardFocus.Toggle(PlayerKey, OwnedTiles);

    private IEnumerable<TileData> OwnedTiles()
    {
        var grid = ServiceLocator.GetService<IGridService>();
        return grid != null ? grid.GetOwnedTiles(_playerData.ActorNumber) : Enumerable.Empty<TileData>();
    }

    /// <summary>This player's tiles that yield the resource, from terrain or from a farm built on them.</summary>
    private IEnumerable<TileData> TilesWith(ResourceType resource) =>
        OwnedTiles().Where(t => t.BaseResource == resource || t.AddedResource == resource);

    /// <summary>Shows which of this card's targets, if any, is the current focus.</summary>
    private void RefreshActive()
    {
        if (_playerData == null || _name == null) return;

        bool playerActive = BoardFocus.IsActive(PlayerKey);
        if (_portraitRing != null) _portraitRing.enabled = playerActive;
        _name.color = playerActive ? Gold : (_isLocal ? IconRow.Bright : IconRow.Soft);

        foreach (var chip in _chips)
        {
            bool active = BoardFocus.IsActive(chip.Key);
            chip.Body.color = active ? ChipActive : ChipIdle;
            chip.Ring.enabled = active;
            chip.Number.color = active ? Gold : IconRow.Bright;
        }
    }

    // ==========================================
    // STATS
    // ==========================================

    public void UpdateStats(int score, int territories, int wood, int carrot, int fish)
    {
        if (_score == null || _chips.Count < 4) return; // not built yet

        _score.text = score.ToString();
        _chips[0].Number.text = territories.ToString();
        _chips[1].Number.text = wood.ToString();
        _chips[2].Number.text = carrot.ToString();
        _chips[3].Number.text = fish.ToString();
    }
}
