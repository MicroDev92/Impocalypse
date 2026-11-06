using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor menu item that creates the full GameScene UI hierarchy.
/// Run via: GameObject > UI > Impocalypse Game Scene UI
///
/// Creates:
///   UICanvas
///   ├── TopBar         (leave button, title, round/phase)
///   ├── LeftSidebar    (PlayerSidebar — current turn + player cards)
///   ├── RightSidebar   (TileInfoPanel)
///   └── BottomBar      (CardFanLayout for draft cards)
///
/// All serialized references are wired automatically.
/// After running, you need to:
///   1. Set Main Camera viewport rect to fit the center gap
///   2. Assign sprites (icons, etc.) in the Inspector
///   3. Assign the GameTheme and PlayerColorPalette ScriptableObjects
///   4. Assign the PlayerCard prefab to PlayerSidebar
/// </summary>
public static class GameSceneUISetup
{
    // Colors matching the design reference (dark infernal theme)
    private static readonly Color BgColor = new(0.10f, 0.05f, 0.04f, 1f);
    private static readonly Color PanelBg = new(0.14f, 0.08f, 0.06f, 0.95f);
    private static readonly Color PanelBorder = new(0.55f, 0.18f, 0.10f, 0.6f);
    private static readonly Color TextPrimary = new(1f, 0.92f, 0.82f, 1f);
    private static readonly Color TextSecondary = new(0.70f, 0.55f, 0.45f, 1f);
    private static readonly Color TextAccent = new(0.95f, 0.55f, 0.15f, 1f);
    private static readonly Color ButtonPrimary = new(0.85f, 0.35f, 0.08f, 1f);

    // Layout constants
    private const float SidebarWidth = 280f;
    private const float TopBarHeight = 50f;
    private const float BottomBarHeight = 150f;
    private const float PanelPadding = 12f;
    private const float BorderWidth = 1.5f;

    [MenuItem("GameObject/UI/Impocalypse Game Scene UI", false, 20)]
    private static void CreateGameSceneUI()
    {
        // Create or find canvas
        var canvas = FindOrCreateCanvas();
        if (canvas == null) return;

        var canvasRect = canvas.GetComponent<RectTransform>();

        // ==========================================
        // TOP BAR
        // ==========================================
        var topBar = CreatePanel(canvas.transform, "TopBar");
        var topBarRect = topBar.GetComponent<RectTransform>();
        topBarRect.anchorMin = new Vector2(0, 1);
        topBarRect.anchorMax = new Vector2(1, 1);
        topBarRect.pivot = new Vector2(0.5f, 1f);
        topBarRect.sizeDelta = new Vector2(0, TopBarHeight);
        ThemeSurface(topBar, 0.95f, UITheme.TopBarFrameScale);

        // Leave button (top-left)
        var leaveBtn = CreateButton(topBar.transform, "LeaveButton", "\u2190 Leave Game", 140f, 34f);
        var leaveBtnRect = leaveBtn.GetComponent<RectTransform>();
        leaveBtnRect.anchorMin = new Vector2(0, 0.5f);
        leaveBtnRect.anchorMax = new Vector2(0, 0.5f);
        leaveBtnRect.pivot = new Vector2(0, 0.5f);
        leaveBtnRect.anchoredPosition = new Vector2(12, 0);

        // Title (center)
        var titleObj = CreateTextObject(topBar.transform, "GameTitle", "IMPOCALYPSE", 24, FontStyles.Bold);
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.3f, 0);
        titleRect.anchorMax = new Vector2(0.7f, 1);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        titleObj.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
        titleObj.GetComponent<TMP_Text>().color = TextAccent;
        titleObj.GetComponent<TMP_Text>().characterSpacing = 6f;

        // Round badge (right of title)
        var roundText = CreateTextObject(topBar.transform, "RoundText", "Round 1 / 4", 16, FontStyles.Normal);
        var roundRect = roundText.GetComponent<RectTransform>();
        roundRect.anchorMin = new Vector2(0.7f, 0);
        roundRect.anchorMax = new Vector2(0.85f, 1);
        roundRect.offsetMin = Vector2.zero;
        roundRect.offsetMax = Vector2.zero;
        roundText.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
        roundText.GetComponent<TMP_Text>().color = TextSecondary;

        // Phase text (far right)
        var phaseText = CreateTextObject(topBar.transform, "PhaseText", "DRAFT PHASE", 16, FontStyles.Bold);
        var phaseRect = phaseText.GetComponent<RectTransform>();
        phaseRect.anchorMin = new Vector2(0.85f, 0);
        phaseRect.anchorMax = new Vector2(1, 1);
        phaseRect.offsetMin = new Vector2(0, 0);
        phaseRect.offsetMax = new Vector2(-12, 0);
        phaseText.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.MidlineRight;
        phaseText.GetComponent<TMP_Text>().color = new Color(0.9f, 0.75f, 0.3f, 1f);

        // ==========================================
        // LEFT SIDEBAR
        // ==========================================
        var leftSidebar = CreatePanel(canvas.transform, "LeftSidebar");
        var leftRect = leftSidebar.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0);
        leftRect.anchorMax = new Vector2(0, 1);
        leftRect.pivot = new Vector2(0, 0.5f);
        leftRect.sizeDelta = new Vector2(SidebarWidth, 0);
        leftRect.offsetMin = new Vector2(0, BottomBarHeight);
        leftRect.offsetMax = new Vector2(SidebarWidth, -TopBarHeight);
        // Sidebars stay a quiet dark backdrop on purpose: an ornate frame around a
        // mostly-empty column is visual noise. The widgets sitting on them (current
        // turn, player cards, pacts) carry the stone framing instead.
        leftSidebar.GetComponent<Image>().color = new Color(BgColor.r, BgColor.g, BgColor.b, 0.9f);

        // The player list owns the whole sidebar now. CurrentTurnPanel used to sit
        // above it saying "Current Turn" while always showing the LOCAL player - a
        // simultaneous draft has no turns - and its instruction line duplicated
        // PickPromptUI and the phase banner. Identifying yourself moved into the list,
        // where the local card is suffixed "(you)".
        var playerListContainer = new GameObject("PlayerListContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        playerListContainer.transform.SetParent(leftSidebar.transform, false);
        var plcRect = playerListContainer.GetComponent<RectTransform>();
        plcRect.anchorMin = new Vector2(0, 0);
        plcRect.anchorMax = new Vector2(1, 1);
        plcRect.offsetMin = new Vector2(PanelPadding, PanelPadding);
        plcRect.offsetMax = new Vector2(-PanelPadding, -PanelPadding);

        var plcVlg = playerListContainer.GetComponent<VerticalLayoutGroup>();
        plcVlg.spacing = 8;
        plcVlg.childAlignment = TextAnchor.UpperCenter;
        plcVlg.childForceExpandWidth = true;
        plcVlg.childForceExpandHeight = false;
        plcVlg.childControlWidth = true;
        plcVlg.childControlHeight = false;

        // Add PlayerSidebar component
        var sidebarComponent = leftSidebar.AddComponent<PlayerSidebar>();
        var sidebarSO = new SerializedObject(sidebarComponent);
        sidebarSO.FindProperty("playerListContainer").objectReferenceValue = playerListContainer.transform;
        sidebarSO.ApplyModifiedProperties();

        // ==========================================
        // RIGHT SIDEBAR
        // ==========================================
        var rightSidebar = CreatePanel(canvas.transform, "RightSidebar");
        var rightRect = rightSidebar.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(1, 0);
        rightRect.anchorMax = new Vector2(1, 1);
        rightRect.pivot = new Vector2(1, 0.5f);
        rightRect.sizeDelta = new Vector2(SidebarWidth, 0);
        rightRect.offsetMin = new Vector2(-SidebarWidth, BottomBarHeight);
        rightRect.offsetMax = new Vector2(0, -TopBarHeight);
        rightSidebar.GetComponent<Image>().color = new Color(BgColor.r, BgColor.g, BgColor.b, 0.9f);

        // Territory Info header
        var tileInfoHeader = CreateTextObject(rightSidebar.transform, "TileInfoHeader", "Territory Info", 18, FontStyles.Bold);
        SetAnchored(tileInfoHeader, new Vector2(0, 1), new Vector2(1, 1), new Vector2(PanelPadding, -PanelPadding), new Vector2(-PanelPadding, -PanelPadding), 30f);
        tileInfoHeader.GetComponent<TMP_Text>().color = TextPrimary;

        // Tile preview panel (bordered card)
        var tilePreviewPanel = CreateBorderedPanel(rightSidebar.transform, "TilePreviewPanel");
        var previewRect = tilePreviewPanel.GetComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0, 1);
        previewRect.anchorMax = new Vector2(1, 1);
        previewRect.pivot = new Vector2(0.5f, 1);
        previewRect.sizeDelta = new Vector2(-PanelPadding * 2, 120);
        previewRect.anchoredPosition = new Vector2(0, -(30 + PanelPadding * 2));

        var tilePreviewImage = new GameObject("TilePreviewImage", typeof(RectTransform), typeof(Image));
        tilePreviewImage.transform.SetParent(tilePreviewPanel.transform, false);
        var previewImgRect = tilePreviewImage.GetComponent<RectTransform>();
        previewImgRect.anchorMin = new Vector2(0.5f, 0.55f);
        previewImgRect.anchorMax = new Vector2(0.5f, 0.55f);
        previewImgRect.sizeDelta = new Vector2(60, 60);
        tilePreviewImage.GetComponent<Image>().color = new Color(1, 1, 1, 0.5f);
        tilePreviewImage.GetComponent<Image>().raycastTarget = false;

        var tileTypeName = CreateTextObject(tilePreviewPanel.transform, "TileTypeName", "Mountain", 16, FontStyles.Bold);
        SetAnchored(tileTypeName, new Vector2(0, 0), new Vector2(1, 0.35f), new Vector2(5, 0), new Vector2(-5, 0));
        tileTypeName.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
        tileTypeName.GetComponent<TMP_Text>().color = TextPrimary;

        // Stats container
        var statsContainer = new GameObject("StatsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        statsContainer.transform.SetParent(rightSidebar.transform, false);
        var statsRect = statsContainer.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0, 0.3f);
        statsRect.anchorMax = new Vector2(1, 1);
        statsRect.offsetMin = new Vector2(PanelPadding, 0);
        statsRect.offsetMax = new Vector2(-PanelPadding, -(120 + 30 + PanelPadding * 3 + 8));
        var statsVlg = statsContainer.GetComponent<VerticalLayoutGroup>();
        statsVlg.spacing = 4;
        statsVlg.childForceExpandWidth = true;
        statsVlg.childForceExpandHeight = false;
        statsVlg.childControlWidth = true;
        // Must be true or CreateStatRow's 24px preferredHeight is ignored and each
        // row falls back to the RectTransform default of 100px - which is what made
        // the info panel look sparse, with ~85px between stat rows.
        statsVlg.childControlHeight = true;
        statsVlg.padding = new RectOffset(4, 4, 4, 4);

        var coordRow = CreateStatRow(statsContainer.transform, "CoordRow", "Position", "—");
        var ownerRow = CreateStatRow(statsContainer.transform, "OwnerRow", "Owner", "Unclaimed");
        var resourceRow = CreateStatRow(statsContainer.transform, "ResourceRow", "Resource", "None");
        var cityRow = CreateStatRow(statsContainer.transform, "CityRow", "City", "None");

        // Empty state text
        var emptyState = new GameObject("EmptyState", typeof(RectTransform));
        emptyState.transform.SetParent(rightSidebar.transform, false);
        var emptyRect = emptyState.GetComponent<RectTransform>();
        StretchFull(emptyRect);
        var emptyText = CreateTextObject(emptyState.transform, "EmptyStateText", "Click a tile to inspect", 14, FontStyles.Italic);
        StretchFull(emptyText.GetComponent<RectTransform>());
        emptyText.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
        emptyText.GetComponent<TMP_Text>().color = TextSecondary;

        // Content wrapper (everything except empty state)
        var tileContentPanel = new GameObject("TileContentPanel", typeof(RectTransform));
        tileContentPanel.transform.SetParent(rightSidebar.transform, false);
        StretchFull(tileContentPanel.GetComponent<RectTransform>());
        // Re-parent tile info children under content panel
        tileInfoHeader.transform.SetParent(tileContentPanel.transform, true);
        tilePreviewPanel.transform.SetParent(tileContentPanel.transform, true);
        statsContainer.transform.SetParent(tileContentPanel.transform, true);
        tileContentPanel.SetActive(false);

        // Add TileInfoPanel component and wire refs
        var tileInfoComponent = rightSidebar.AddComponent<TileInfoPanel>();
        var tileInfoSO = new SerializedObject(tileInfoComponent);
        tileInfoSO.FindProperty("contentPanel").objectReferenceValue = tileContentPanel;
        tileInfoSO.FindProperty("emptyStatePanel").objectReferenceValue = emptyState;
        tileInfoSO.FindProperty("emptyStateText").objectReferenceValue = emptyText.GetComponent<TMP_Text>();
        tileInfoSO.FindProperty("tilePreviewImage").objectReferenceValue = tilePreviewImage.GetComponent<Image>();
        tileInfoSO.FindProperty("tileTypeName").objectReferenceValue = tileTypeName.GetComponent<TMP_Text>();
        tileInfoSO.FindProperty("coordValue").objectReferenceValue = GetStatValue(coordRow);
        tileInfoSO.FindProperty("ownerValue").objectReferenceValue = GetStatValue(ownerRow);
        tileInfoSO.FindProperty("resourceValue").objectReferenceValue = GetStatValue(resourceRow);
        tileInfoSO.FindProperty("cityValue").objectReferenceValue = GetStatValue(cityRow);
        tileInfoSO.FindProperty("cityRow").objectReferenceValue = cityRow;
        tileInfoSO.ApplyModifiedProperties();

        // ==========================================
        // BOTTOM BAR (Card Fan)
        // ==========================================
        var bottomBar = CreatePanel(canvas.transform, "BottomBar");
        var botRect = bottomBar.GetComponent<RectTransform>();
        botRect.anchorMin = new Vector2(0, 0);
        botRect.anchorMax = new Vector2(1, 0);
        botRect.pivot = new Vector2(0.5f, 0);
        botRect.sizeDelta = new Vector2(0, BottomBarHeight);
        ThemeSurface(bottomBar, 0.95f);

        // Card fan container (centered in bottom bar)
        var cardFanObj = new GameObject("CardFanContainer", typeof(RectTransform), typeof(CanvasGroup));
        cardFanObj.transform.SetParent(bottomBar.transform, false);
        var fanRect = cardFanObj.GetComponent<RectTransform>();
        fanRect.anchorMin = new Vector2(0.05f, 0.1f);
        fanRect.anchorMax = new Vector2(0.85f, 0.95f);
        fanRect.offsetMin = Vector2.zero;
        fanRect.offsetMax = Vector2.zero;
        cardFanObj.AddComponent<CardFanLayout>();

        // No Confirm button here. The real one is built by the PickPrompt generator
        // below and wired to PickPromptUI; this bottom-bar copy was never given a
        // listener, so re-running this generator would add a second button that looks
        // identical to the live one and does nothing when pressed.

        // Status text (above card fan)
        var cardStatusText = CreateTextObject(bottomBar.transform, "CardStatusText", "Cards: 10 | Picks left: 2", 14, FontStyles.Normal);
        var csRect = cardStatusText.GetComponent<RectTransform>();
        csRect.anchorMin = new Vector2(0.05f, 0.85f);
        csRect.anchorMax = new Vector2(0.5f, 1f);
        csRect.offsetMin = Vector2.zero;
        csRect.offsetMax = Vector2.zero;
        cardStatusText.GetComponent<TMP_Text>().color = TextSecondary;
        cardStatusText.GetComponent<TMP_Text>().fontSize = 13;

        // ==========================================
        // ADD ModernDraftHUD component to TopBar
        // ==========================================
        var draftHUD = topBar.AddComponent<UI.ModernDraftHUD>();
        var draftSO = new SerializedObject(draftHUD);
        draftSO.FindProperty("roundText").objectReferenceValue = roundText.GetComponent<TMP_Text>();
        draftSO.FindProperty("phaseText").objectReferenceValue = phaseText.GetComponent<TMP_Text>();
        draftSO.FindProperty("statusText").objectReferenceValue = cardStatusText.GetComponent<TMP_Text>();
        draftSO.FindProperty("draftInfoText").objectReferenceValue = cardStatusText.GetComponent<TMP_Text>();
        draftSO.ApplyModifiedProperties();

        // ==========================================
        // REGISTER UNDO
        // ==========================================
        Undo.RegisterCreatedObjectUndo(topBar, "Create Game Scene UI");
        Undo.RegisterCreatedObjectUndo(leftSidebar, "Create Game Scene UI");
        Undo.RegisterCreatedObjectUndo(rightSidebar, "Create Game Scene UI");
        Undo.RegisterCreatedObjectUndo(bottomBar, "Create Game Scene UI");

        Selection.activeGameObject = canvas.gameObject;
        Debug.Log("[GameSceneUISetup] Game Scene UI created successfully.\n" +
                  "Next steps:\n" +
                  "  1. Set Main Camera Viewport Rect (see setup guide)\n" +
                  "  2. Assign GameTheme SO to PlayerSidebar and TileInfoPanel\n" +
                  "  3. Assign PlayerColorPalette to PlayerSidebar and TileInfoPanel\n" +
                  "  4. Create and assign PlayerCard prefab to PlayerSidebar\n" +
                  "  5. Point CardFactory's cardContainer to CardFanContainer\n" +
                  "  6. Remove old DraftHUDContainer and CardPanel if present");
    }

    // ==========================================
    // FOCUSED: add only the two sidebars to an existing canvas
    // (keeps your current TopBar / CardFan - no duplicates)
    // ==========================================

    [MenuItem("GameObject/UI/Impocalypse - Add Scoreboard + Inspector Sidebars", false, 22)]
    private static void AddSidebars()
    {
        var canvas = FindExistingCanvas();
        if (canvas == null)
        {
            Debug.LogError("[GameSceneUISetup] No Canvas found in the open scene. Open GameScene first, then run this again.");
            return;
        }

        BuildLeftSidebar(canvas.transform);
        BuildRightSidebar(canvas.transform);

        Selection.activeGameObject = canvas.gameObject;
        Debug.Log("[GameSceneUISetup] Added LeftSidebar (PlayerSidebar / scoreboard) and RightSidebar (TileInfoPanel / inspector).\n" +
                  "Next steps:\n" +
                  "  1. Assign your PlayerCard prefab to LeftSidebar > PlayerSidebar > 'Player Card Prefab' (REQUIRED for cards to show)\n" +
                  "  2. (Optional) assign GameTheme + PlayerColorPalette to the PlayerSidebar for player colours");
    }

    private static Canvas FindExistingCanvas()
    {
        var named = GameObject.Find("UICanvas");
        if (named != null)
        {
            var c = named.GetComponent<Canvas>();
            if (c != null) return c;
        }
        return UnityEngine.Object.FindFirstObjectByType<Canvas>();
    }

    private static void BuildLeftSidebar(Transform canvasTransform)
    {
        var leftSidebar = CreatePanel(canvasTransform, "LeftSidebar");
        var leftRect = leftSidebar.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0);
        leftRect.anchorMax = new Vector2(0, 1);
        leftRect.pivot = new Vector2(0, 0.5f);
        leftRect.sizeDelta = new Vector2(SidebarWidth, 0);
        leftRect.offsetMin = new Vector2(0, BottomBarHeight);
        leftRect.offsetMax = new Vector2(SidebarWidth, -TopBarHeight);
        // Sidebars stay a quiet dark backdrop on purpose: an ornate frame around a
        // mostly-empty column is visual noise. The widgets sitting on them (player
        // cards, pacts) carry the stone framing instead.
        leftSidebar.GetComponent<Image>().color = new Color(BgColor.r, BgColor.g, BgColor.b, 0.9f);

        // See the note in CreateGameSceneUI: CurrentTurnPanel was removed from both
        // builders. Leaving it in only one is worse than leaving it in both - the
        // sidebar would then depend on which menu command was last run.
        var playerListContainer = new GameObject("PlayerListContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        playerListContainer.transform.SetParent(leftSidebar.transform, false);
        var plcRect = playerListContainer.GetComponent<RectTransform>();
        plcRect.anchorMin = new Vector2(0, 0);
        plcRect.anchorMax = new Vector2(1, 1);
        plcRect.offsetMin = new Vector2(PanelPadding, PanelPadding);
        plcRect.offsetMax = new Vector2(-PanelPadding, -PanelPadding);

        var plcVlg = playerListContainer.GetComponent<VerticalLayoutGroup>();
        plcVlg.spacing = 8;
        plcVlg.childAlignment = TextAnchor.UpperCenter;
        plcVlg.childForceExpandWidth = true;
        plcVlg.childForceExpandHeight = false;
        plcVlg.childControlWidth = true;
        plcVlg.childControlHeight = false;

        var sidebarComponent = leftSidebar.AddComponent<PlayerSidebar>();
        var sidebarSO = new SerializedObject(sidebarComponent);
        sidebarSO.FindProperty("playerListContainer").objectReferenceValue = playerListContainer.transform;
        sidebarSO.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(leftSidebar, "Add Scoreboard Sidebar");
    }

    private static void BuildRightSidebar(Transform canvasTransform)
    {
        var rightSidebar = CreatePanel(canvasTransform, "RightSidebar");
        var rightRect = rightSidebar.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(1, 0);
        rightRect.anchorMax = new Vector2(1, 1);
        rightRect.pivot = new Vector2(1, 0.5f);
        rightRect.sizeDelta = new Vector2(SidebarWidth, 0);
        rightRect.offsetMin = new Vector2(-SidebarWidth, BottomBarHeight);
        rightRect.offsetMax = new Vector2(0, -TopBarHeight);
        rightSidebar.GetComponent<Image>().color = new Color(BgColor.r, BgColor.g, BgColor.b, 0.9f);

        var tileInfoHeader = CreateTextObject(rightSidebar.transform, "TileInfoHeader", "Territory Info", 18, FontStyles.Bold);
        SetAnchored(tileInfoHeader, new Vector2(0, 1), new Vector2(1, 1), new Vector2(PanelPadding, -PanelPadding), new Vector2(-PanelPadding, -PanelPadding), 30f);
        tileInfoHeader.GetComponent<TMP_Text>().color = TextPrimary;

        var tilePreviewPanel = CreateBorderedPanel(rightSidebar.transform, "TilePreviewPanel");
        var previewRect = tilePreviewPanel.GetComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0, 1);
        previewRect.anchorMax = new Vector2(1, 1);
        previewRect.pivot = new Vector2(0.5f, 1);
        previewRect.sizeDelta = new Vector2(-PanelPadding * 2, 120);
        previewRect.anchoredPosition = new Vector2(0, -(30 + PanelPadding * 2));

        var tilePreviewImage = new GameObject("TilePreviewImage", typeof(RectTransform), typeof(Image));
        tilePreviewImage.transform.SetParent(tilePreviewPanel.transform, false);
        var previewImgRect = tilePreviewImage.GetComponent<RectTransform>();
        previewImgRect.anchorMin = new Vector2(0.5f, 0.55f);
        previewImgRect.anchorMax = new Vector2(0.5f, 0.55f);
        previewImgRect.sizeDelta = new Vector2(60, 60);
        tilePreviewImage.GetComponent<Image>().color = new Color(1, 1, 1, 0.5f);
        tilePreviewImage.GetComponent<Image>().raycastTarget = false;

        var tileTypeName = CreateTextObject(tilePreviewPanel.transform, "TileTypeName", "Mountain", 16, FontStyles.Bold);
        SetAnchored(tileTypeName, new Vector2(0, 0), new Vector2(1, 0.35f), new Vector2(5, 0), new Vector2(-5, 0));
        tileTypeName.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
        tileTypeName.GetComponent<TMP_Text>().color = TextPrimary;

        var statsContainer = new GameObject("StatsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        statsContainer.transform.SetParent(rightSidebar.transform, false);
        var statsRect = statsContainer.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0, 0.3f);
        statsRect.anchorMax = new Vector2(1, 1);
        statsRect.offsetMin = new Vector2(PanelPadding, 0);
        statsRect.offsetMax = new Vector2(-PanelPadding, -(120 + 30 + PanelPadding * 3 + 8));
        var statsVlg = statsContainer.GetComponent<VerticalLayoutGroup>();
        statsVlg.spacing = 4;
        statsVlg.childForceExpandWidth = true;
        statsVlg.childForceExpandHeight = false;
        statsVlg.childControlWidth = true;
        // Must be true or CreateStatRow's 24px preferredHeight is ignored and each
        // row falls back to the RectTransform default of 100px - which is what made
        // the info panel look sparse, with ~85px between stat rows.
        statsVlg.childControlHeight = true;
        statsVlg.padding = new RectOffset(4, 4, 4, 4);

        var coordRow = CreateStatRow(statsContainer.transform, "CoordRow", "Position", "—");
        var ownerRow = CreateStatRow(statsContainer.transform, "OwnerRow", "Owner", "Unclaimed");
        var resourceRow = CreateStatRow(statsContainer.transform, "ResourceRow", "Resource", "None");
        var cityRow = CreateStatRow(statsContainer.transform, "CityRow", "City", "None");

        var emptyState = new GameObject("EmptyState", typeof(RectTransform));
        emptyState.transform.SetParent(rightSidebar.transform, false);
        var emptyRect = emptyState.GetComponent<RectTransform>();
        StretchFull(emptyRect);
        var emptyText = CreateTextObject(emptyState.transform, "EmptyStateText", "Click a tile to inspect", 14, FontStyles.Italic);
        StretchFull(emptyText.GetComponent<RectTransform>());
        emptyText.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
        emptyText.GetComponent<TMP_Text>().color = TextSecondary;

        var tileContentPanel = new GameObject("TileContentPanel", typeof(RectTransform));
        tileContentPanel.transform.SetParent(rightSidebar.transform, false);
        StretchFull(tileContentPanel.GetComponent<RectTransform>());
        tileInfoHeader.transform.SetParent(tileContentPanel.transform, true);
        tilePreviewPanel.transform.SetParent(tileContentPanel.transform, true);
        statsContainer.transform.SetParent(tileContentPanel.transform, true);
        tileContentPanel.SetActive(false);

        var tileInfoComponent = rightSidebar.AddComponent<TileInfoPanel>();
        var tileInfoSO = new SerializedObject(tileInfoComponent);
        tileInfoSO.FindProperty("contentPanel").objectReferenceValue = tileContentPanel;
        tileInfoSO.FindProperty("emptyStatePanel").objectReferenceValue = emptyState;
        tileInfoSO.FindProperty("emptyStateText").objectReferenceValue = emptyText.GetComponent<TMP_Text>();
        tileInfoSO.FindProperty("tilePreviewImage").objectReferenceValue = tilePreviewImage.GetComponent<Image>();
        tileInfoSO.FindProperty("tileTypeName").objectReferenceValue = tileTypeName.GetComponent<TMP_Text>();
        tileInfoSO.FindProperty("coordValue").objectReferenceValue = GetStatValue(coordRow);
        tileInfoSO.FindProperty("ownerValue").objectReferenceValue = GetStatValue(ownerRow);
        tileInfoSO.FindProperty("resourceValue").objectReferenceValue = GetStatValue(resourceRow);
        tileInfoSO.FindProperty("cityValue").objectReferenceValue = GetStatValue(cityRow);
        tileInfoSO.FindProperty("cityRow").objectReferenceValue = cityRow;
        tileInfoSO.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(rightSidebar, "Add Inspector Sidebar");
    }

    // ==========================================
    // FOCUSED: rebuild the top bar (replaces the legacy overlapping HUD)
    // ==========================================

    [MenuItem("GameObject/UI/Impocalypse - Rebuild Top Bar", false, 25)]
    private static void RebuildTopBar()
    {
        var canvas = FindExistingCanvas();
        if (canvas == null)
        {
            Debug.LogError("[GameSceneUISetup] No Canvas found in the open scene. Open GameScene first.");
            return;
        }

        // Remove the legacy HUD (the old top bar with overlapping counters).
        var oldHud = Object.FindFirstObjectByType<UI.ModernDraftHUD>(FindObjectsInactive.Include);
        if (oldHud != null)
        {
            Debug.Log($"[GameSceneUISetup] Removing legacy HUD object '{oldHud.gameObject.name}'.");
            Undo.DestroyObjectImmediate(oldHud.gameObject);
        }

        var topBar = CreatePanel(canvas.transform, "TopBar");
        var barRect = topBar.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0, 1);
        barRect.anchorMax = new Vector2(1, 1);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.sizeDelta = new Vector2(0, TopBarHeight);
        ThemeSurface(topBar, 0.95f, UITheme.TopBarFrameScale);

        // Left zone: round | phase | draft info (auto-spaced, can never overlap)
        var left = new GameObject("LeftGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        left.transform.SetParent(topBar.transform, false);
        var leftRect = left.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0);
        leftRect.anchorMax = new Vector2(0.32f, 1);
        leftRect.offsetMin = new Vector2(14, 0);
        leftRect.offsetMax = Vector2.zero;
        var leftHlg = left.GetComponent<HorizontalLayoutGroup>();
        leftHlg.spacing = 18;
        leftHlg.childAlignment = TextAnchor.MiddleLeft;
        leftHlg.childForceExpandWidth = false;
        leftHlg.childForceExpandHeight = false;
        leftHlg.childControlWidth = true;   // size each text to its content
        leftHlg.childControlHeight = true;

        var roundText = CreateTextObject(left.transform, "RoundText", "Round 1 / 4", 18, FontStyles.Normal);
        roundText.GetComponent<TMP_Text>().color = TextSecondary;

        var phaseText = CreateTextObject(left.transform, "PhaseText", "DRAFT PHASE", 18, FontStyles.Bold);
        phaseText.GetComponent<TMP_Text>().color = TextAccent;

        var draftInfo = CreateTextObject(left.transform, "DraftInfoText", "Cards: 0 | Picks: 0", 15, FontStyles.Normal);
        draftInfo.GetComponent<TMP_Text>().color = TextSecondary;

        // Center zone: status messages (Provisions results, harvest notes, etc.)
        var status = CreateTextObject(topBar.transform, "StatusText", "", 16, FontStyles.Italic);
        var statusRect = status.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.32f, 0);
        statusRect.anchorMax = new Vector2(0.72f, 1);
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = Vector2.zero;
        var statusTmp = status.GetComponent<TMP_Text>();
        statusTmp.alignment = TextAlignmentOptions.Center;
        statusTmp.color = TextPrimary;
        statusTmp.enableAutoSizing = true;
        statusTmp.fontSizeMax = 16;
        statusTmp.fontSizeMin = 11;

        // Right zone: text-only resource counters (no icon squares in greybox)
        var right = new GameObject("ResourceGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        right.transform.SetParent(topBar.transform, false);
        var rightRect = right.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0.72f, 0);
        rightRect.anchorMax = new Vector2(1, 1);
        rightRect.offsetMin = Vector2.zero;
        rightRect.offsetMax = new Vector2(-14, 0);
        var rightHlg = right.GetComponent<HorizontalLayoutGroup>();
        rightHlg.spacing = 6;
        rightHlg.childAlignment = TextAnchor.MiddleRight;
        rightHlg.childForceExpandWidth = false;
        rightHlg.childForceExpandHeight = false;
        rightHlg.childControlWidth = true;  // size each text to its content
        rightHlg.childControlHeight = true;

        // Prefix so these read as map info, not the player's own resources
        var onBoard = CreateTextObject(right.transform, "OnBoardLabel", "On board:", 15, FontStyles.Italic);
        onBoard.GetComponent<TMP_Text>().color = TextSecondary;

        var woodAmount = CreateResourceCounter(right.transform, GameLexicon.Resource(Cards.ResourceType.Wood), "charwood");
        var carrotAmount = CreateResourceCounter(right.transform, GameLexicon.Resource(Cards.ResourceType.Carrot), "bloodroot");
        var fishAmount = CreateResourceCounter(right.transform, GameLexicon.Resource(Cards.ResourceType.Fish), "souls");

        // Wire the HUD component
        var hud = topBar.AddComponent<UI.ModernDraftHUD>();
        var so = new SerializedObject(hud);
        so.FindProperty("roundText").objectReferenceValue = roundText.GetComponent<TMP_Text>();
        so.FindProperty("phaseText").objectReferenceValue = phaseText.GetComponent<TMP_Text>();
        so.FindProperty("statusText").objectReferenceValue = statusTmp;
        so.FindProperty("draftInfoText").objectReferenceValue = draftInfo.GetComponent<TMP_Text>();
        so.FindProperty("woodAmount").objectReferenceValue = woodAmount;
        so.FindProperty("carrotAmount").objectReferenceValue = carrotAmount;
        so.FindProperty("fishAmount").objectReferenceValue = fishAmount;
        so.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(topBar, "Rebuild Top Bar");
        Selection.activeGameObject = topBar;
        Debug.Log("[GameSceneUISetup] Top bar rebuilt (left: round/phase/draft info, center: status, right: resource counters). " +
                  "If any stray pieces of the old bar remain in the Hierarchy, delete them manually.");
    }

    /// <summary>Creates a "Label 0" counter pair and returns the amount text (for HUD wiring).</summary>
    /// <summary>
    /// Top-bar board counter. With icon art present it shows [icon] 5/16 - the
    /// icon replaces the word, which keeps the bar readable now that resource
    /// names are long ("Charwood"). Without art it falls back to the word.
    /// </summary>
    private static TMP_Text CreateResourceCounter(Transform parent, string label, string iconName)
    {
        var iconSprite = UITheme.Icon(iconName);
        if (iconSprite != null)
        {
            var iconObj = new GameObject($"{label}Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            iconObj.transform.SetParent(parent, false);
            var iconImage = iconObj.GetComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            var iconLayout = iconObj.GetComponent<LayoutElement>();
            iconLayout.preferredWidth = 24;
            iconLayout.preferredHeight = 24;
        }
        else
        {
            var labelObj = CreateTextObject(parent, $"{label}Label", label, 16, FontStyles.Normal);
            labelObj.GetComponent<TMP_Text>().color = TextSecondary;
        }

        var amountObj = CreateTextObject(parent, $"{label}Amount", "0", 18, FontStyles.Bold);
        var amountTmp = amountObj.GetComponent<TMP_Text>();
        amountTmp.color = TextAccent;
        return amountTmp;
    }

    // ==========================================
    // FOCUSED: add the "Your Pacts" (secret parchment hand) panel
    // ==========================================

    [MenuItem("GameObject/UI/Impocalypse - Add Pacts Panel", false, 24)]
    private static void AddPactsPanel()
    {
        var leftSidebar = GameObject.Find("LeftSidebar");
        if (leftSidebar == null)
        {
            Debug.LogError("[GameSceneUISetup] LeftSidebar not found. Add the sidebars first.");
            return;
        }

        var panel = CreateBorderedPanel(leftSidebar.transform, "PactsPanel");
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.sizeDelta = new Vector2(-PanelPadding * 2, 150);
        rect.anchoredPosition = new Vector2(0, PanelPadding);

        var header = CreateTextObject(panel.transform, "PactsHeader", "Your Pacts", 14, FontStyles.Bold);
        SetAnchored(header, new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -6), new Vector2(-8, -6), 22f);
        header.GetComponent<TMP_Text>().color = TextAccent;

        var list = CreateTextObject(panel.transform, "PactsList", "none yet", 12, FontStyles.Normal);
        var listRect = list.GetComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0, 0);
        listRect.anchorMax = new Vector2(1, 1);
        listRect.offsetMin = new Vector2(8, 6);
        listRect.offsetMax = new Vector2(-8, -30);
        var listTmp = list.GetComponent<TMP_Text>();
        listTmp.color = TextSecondary;
        listTmp.alignment = TextAlignmentOptions.TopLeft;

        var ui = panel.AddComponent<UI.ParchmentHandUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("listText").objectReferenceValue = listTmp;
        so.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(panel, "Add Pacts Panel");
        Selection.activeGameObject = panel;
        Debug.Log("[GameSceneUISetup] Pacts panel added to LeftSidebar (bottom). If it overlaps the player list, shorten PlayerListContainer's bottom offset.");
    }

    // ==========================================
    // FOCUSED: add the draft pick prompt + Confirm button
    // ==========================================

    [MenuItem("GameObject/UI/Impocalypse - Add Pick Prompt", false, 27)]
    private static void AddPickPrompt()
    {
        var canvas = FindExistingCanvas();
        if (canvas == null)
        {
            Debug.LogError("[GameSceneUISetup] No Canvas found in the open scene. Open GameScene first.");
            return;
        }

        var existing = canvas.transform.Find("PickPrompt");
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

        // Sits just above the hand, anchored to the bottom edge so it tracks the
        // reserved card band rather than drifting with resolution.
        // Button only - the "Pick 2 cards (1/2)" line lives in the top bar.
        //
        // Bottom-RIGHT, mirroring the construction phase's Done button: the commit
        // action sits in the same corner in every phase, and it can never collide
        // with the hand, which grows as cards are spent.
        var root = new GameObject("PickPrompt", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(1f, 0f);
        rootRect.anchorMax = new Vector2(1f, 0f);
        rootRect.pivot = new Vector2(1f, 0f);
        rootRect.sizeDelta = new Vector2(240, 56);
        rootRect.anchoredPosition = new Vector2(-40, 120);

        var confirm = CreateButton(root.transform, "ConfirmButton", "Confirm Picks", 220f, 46f);
        var confirmRect = confirm.GetComponent<RectTransform>();
        confirmRect.anchorMin = new Vector2(0.5f, 0.5f);
        confirmRect.anchorMax = new Vector2(0.5f, 0.5f);
        confirmRect.pivot = new Vector2(0.5f, 0.5f);
        confirmRect.anchoredPosition = Vector2.zero;
        confirm.AddComponent<CanvasGroup>(); // dimmed while the pick is incomplete

        var ui = root.AddComponent<UI.PickPromptUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("confirmButton").objectReferenceValue = confirm.GetComponent<Button>();
        so.FindProperty("confirmLabel").objectReferenceValue = confirm.GetComponentInChildren<TMP_Text>();
        so.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(root, "Add Pick Prompt");
        Selection.activeGameObject = root;
        Debug.Log("[GameSceneUISetup] Pick prompt added. Raise its anchoredPosition.y if it overlaps the hand.");
    }

    // ==========================================
    // FOCUSED: add the live Domains list
    // ==========================================

    [MenuItem("GameObject/UI/Impocalypse - Add Domains Panel", false, 26)]
    private static void AddDomainsPanel()
    {
        var leftSidebar = GameObject.Find("LeftSidebar");
        if (leftSidebar == null)
        {
            Debug.LogError("[GameSceneUISetup] LeftSidebar not found. Add the sidebars first.");
            return;
        }

        var existing = leftSidebar.transform.Find("DomainsPanel");
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

        // Sits directly above the Pacts panel at the bottom of the sidebar.
        const float pactsHeight = 150f;
        const float panelHeight = 190f;

        var panel = CreateBorderedPanel(leftSidebar.transform, "DomainsPanel");
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.sizeDelta = new Vector2(-PanelPadding * 2, panelHeight);
        rect.anchoredPosition = new Vector2(0, PanelPadding * 2 + pactsHeight);

        var header = CreateTextObject(panel.transform, "DomainsHeader", "Your Domains", 14, FontStyles.Bold);
        SetAnchored(header, new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -6), new Vector2(-8, -6), 22f);
        header.GetComponent<TMP_Text>().color = TextAccent;

        // Rows container - the list grows and shrinks as domains merge and split.
        var listObj = new GameObject("DomainsList", typeof(RectTransform), typeof(VerticalLayoutGroup));
        listObj.transform.SetParent(panel.transform, false);
        var listRect = listObj.GetComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0, 0);
        listRect.anchorMax = new Vector2(1, 1);
        listRect.offsetMin = new Vector2(8, 6);
        listRect.offsetMax = new Vector2(-8, -30);

        var vlg = listObj.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 2;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;    // or row preferred heights are ignored
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false; // flexible height would spread the rows out

        var empty = CreateTextObject(panel.transform, "DomainsEmpty", "claim tiles to form a domain", 12, FontStyles.Italic);
        var emptyRect = empty.GetComponent<RectTransform>();
        emptyRect.anchorMin = new Vector2(0, 0);
        emptyRect.anchorMax = new Vector2(1, 1);
        emptyRect.offsetMin = new Vector2(8, 6);
        emptyRect.offsetMax = new Vector2(-8, -30);
        var emptyTmp = empty.GetComponent<TMP_Text>();
        emptyTmp.color = TextSecondary;
        emptyTmp.alignment = TextAlignmentOptions.TopLeft;

        var ui = panel.AddComponent<UI.DomainListUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("listContainer").objectReferenceValue = listObj.transform;
        so.FindProperty("headerText").objectReferenceValue = header.GetComponent<TMP_Text>();
        so.FindProperty("emptyText").objectReferenceValue = emptyTmp;

        var rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/FiefDetail.prefab");
        if (rowPrefab != null) so.FindProperty("rowPrefab").objectReferenceValue = rowPrefab;
        so.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(panel, "Add Domains Panel");
        Selection.activeGameObject = panel;
        Debug.Log("[GameSceneUISetup] Domains panel added above the Pacts panel." +
                  (rowPrefab == null ? " NOTE: assign the row prefab manually." : "") +
                  " Shorten PlayerListContainer's bottom offset if they overlap.");
    }

    // ==========================================
    // FOCUSED: add the Game Over results screen
    // ==========================================

    [MenuItem("GameObject/UI/Impocalypse - Add Game Over Screen", false, 23)]
    private static void AddGameOverScreen()
    {
        var canvas = FindExistingCanvas();
        if (canvas == null)
        {
            Debug.LogError("[GameSceneUISetup] No Canvas found in the open scene. Open GameScene first.");
            return;
        }

        // Always-active root holds the GameOverUI component so it keeps listening even
        // while the visible panel is toggled off.
        var root = new GameObject("GameOverScreen", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        StretchFull(root.GetComponent<RectTransform>());

        // Full-screen dark overlay panel (this is what gets shown/hidden).
        var panel = CreatePanel(root.transform, "GameOverPanel");
        StretchFull(panel.GetComponent<RectTransform>());
        if (!UITheme.ApplyPanel(panel))
            panel.GetComponent<Image>().color = new Color(0.05f, 0.02f, 0.02f, 0.92f);

        var title = CreateTextObject(panel.transform, "Title", "GAME OVER", 40, FontStyles.Bold);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(700, 60);
        titleRect.anchoredPosition = new Vector2(0, -60);
        title.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
        title.GetComponent<TMP_Text>().color = TextAccent;
        title.GetComponent<TMP_Text>().characterSpacing = 8f;

        var winner = CreateTextObject(panel.transform, "WinnerText", "Player Wins!", 24, FontStyles.Bold);
        var winnerRect = winner.GetComponent<RectTransform>();
        winnerRect.anchorMin = winnerRect.anchorMax = new Vector2(0.5f, 1f);
        winnerRect.pivot = new Vector2(0.5f, 1f);
        winnerRect.sizeDelta = new Vector2(700, 40);
        winnerRect.anchoredPosition = new Vector2(0, -135);
        winner.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
        winner.GetComponent<TMP_Text>().color = TextPrimary;

        var scoreContainer = new GameObject("ScoreContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        scoreContainer.transform.SetParent(panel.transform, false);
        var scRect = scoreContainer.GetComponent<RectTransform>();
        scRect.anchorMin = scRect.anchorMax = new Vector2(0.5f, 0.5f);
        scRect.pivot = new Vector2(0.5f, 0.5f);
        scRect.sizeDelta = new Vector2(620, 320);
        scRect.anchoredPosition = new Vector2(0, -10);
        var scVlg = scoreContainer.GetComponent<VerticalLayoutGroup>();
        scVlg.spacing = 8;
        scVlg.childAlignment = TextAnchor.UpperCenter;
        scVlg.childForceExpandWidth = true;
        scVlg.childForceExpandHeight = false;
        scVlg.childControlWidth = true;
        // Must control height: score entries grow with their fief breakdown, and a group
        // that ignores child heights positions them by their prefab rect instead - so a
        // tall entry draws straight over the player below it.
        scVlg.childControlHeight = true;

        var playAgain = CreateButton(panel.transform, "PlayAgainButton", "Play Again", 200f, 48f);
        var paRect = playAgain.GetComponent<RectTransform>();
        paRect.anchorMin = paRect.anchorMax = new Vector2(0.5f, 0f);
        paRect.pivot = new Vector2(1f, 0f);
        paRect.anchoredPosition = new Vector2(-10, 70);

        var returnMenu = CreateButton(panel.transform, "ReturnToMenuButton", "Main Menu", 200f, 48f);
        var rmRect = returnMenu.GetComponent<RectTransform>();
        rmRect.anchorMin = rmRect.anchorMax = new Vector2(0.5f, 0f);
        rmRect.pivot = new Vector2(0f, 0f);
        rmRect.anchoredPosition = new Vector2(10, 70);

        var ui = root.AddComponent<GameOverUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("gameOverPanel").objectReferenceValue = panel;
        so.FindProperty("winnerText").objectReferenceValue = winner.GetComponent<TMP_Text>();
        so.FindProperty("scoreContainer").objectReferenceValue = scoreContainer.transform;
        so.FindProperty("playAgainButton").objectReferenceValue = playAgain.GetComponent<Button>();
        so.FindProperty("returnToMenuButton").objectReferenceValue = returnMenu.GetComponent<Button>();

        var scoreEntryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/ScoreEntry.prefab");
        var fiefDetailPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/FiefDetail.prefab");
        if (scoreEntryPrefab != null) so.FindProperty("scoreEntryPrefab").objectReferenceValue = scoreEntryPrefab;
        if (fiefDetailPrefab != null) so.FindProperty("fiefDetailPrefab").objectReferenceValue = fiefDetailPrefab;
        so.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(root, "Add Game Over Screen");
        Selection.activeGameObject = root;
        Debug.Log("[GameSceneUISetup] Game Over screen added." +
                  (scoreEntryPrefab == null ? " NOTE: assign ScoreEntry prefab manually." : "") +
                  (fiefDetailPrefab == null ? " NOTE: assign FiefDetail prefab manually." : ""));
    }

    // ==========================================
    // PREFAB CREATORS
    // ==========================================

    [MenuItem("GameObject/UI/Impocalypse Player Card Prefab", false, 21)]
    private static void CreatePlayerCardPrefab()
    {
        // Create the player card structure that PlayerSidebar instantiates
        var card = new GameObject("PlayerCard", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        var cardRect = card.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(260, 90);

        card.GetComponent<Image>().color = PanelBg;
        card.GetComponent<Image>().raycastTarget = false;
        var le = card.GetComponent<LayoutElement>();
        le.preferredHeight = 90;
        le.minHeight = 90;

        // Left border (colored strip)
        var border = new GameObject("LeftBorder", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(card.transform, false);
        var borderRect = border.GetComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(0, 0);
        borderRect.anchorMax = new Vector2(0, 1);
        borderRect.pivot = new Vector2(0, 0.5f);
        borderRect.sizeDelta = new Vector2(4, 0);
        border.GetComponent<Image>().color = Color.red;
        border.GetComponent<Image>().raycastTarget = false;

        // Player name
        var nameObj = CreateTextObject(card.transform, "PlayerNameText", "Player Name", 16, FontStyles.Bold);
        SetAnchored(nameObj, new Vector2(0, 0.6f), new Vector2(0.6f, 1), new Vector2(14, 0), new Vector2(0, -6));
        nameObj.GetComponent<TMP_Text>().color = TextPrimary;

        // Score
        var scoreObj = CreateTextObject(card.transform, "ScoreText", "Score: 0", 14, FontStyles.Normal);
        SetAnchored(scoreObj, new Vector2(0, 0.35f), new Vector2(0.6f, 0.6f), new Vector2(14, 0), new Vector2(0, 0));
        scoreObj.GetComponent<TMP_Text>().color = TextAccent;

        // Stats row
        var statsRow = new GameObject("StatsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        statsRow.transform.SetParent(card.transform, false);
        var statsRect = statsRow.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0, 0);
        statsRect.anchorMax = new Vector2(1, 0.35f);
        statsRect.offsetMin = new Vector2(14, 4);
        statsRect.offsetMax = new Vector2(-8, 0);
        var hlg = statsRow.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = true;   // size stats to content so they never spill out
        hlg.childControlHeight = true;

        // Stat items: territory, wood, carrot, fish
        var terrStat = CreateMiniStat(statsRow.transform, "TerritoryStat", "0", "territory");
        var woodStat = CreateMiniStat(statsRow.transform, "WoodStat", "0", "charwood");
        var carrotStat = CreateMiniStat(statsRow.transform, "CarrotStat", "0", "bloodroot");
        var fishStat = CreateMiniStat(statsRow.transform, "FishStat", "0", "souls");

        // Add PlayerCardUI and wire
        var cardUI = card.AddComponent<PlayerCardUI>();
        var so = new SerializedObject(cardUI);
        so.FindProperty("playerNameText").objectReferenceValue = nameObj.GetComponent<TMP_Text>();
        so.FindProperty("leftBorder").objectReferenceValue = border.GetComponent<Image>();
        so.FindProperty("cardBackground").objectReferenceValue = card.GetComponent<Image>();
        so.FindProperty("scoreText").objectReferenceValue = scoreObj.GetComponent<TMP_Text>();
        so.FindProperty("territoryCountText").objectReferenceValue = GetStatText(terrStat);
        so.FindProperty("woodCountText").objectReferenceValue = GetStatText(woodStat);
        so.FindProperty("carrotCountText").objectReferenceValue = GetStatText(carrotStat);
        so.FindProperty("fishCountText").objectReferenceValue = GetStatText(fishStat);
        so.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(card, "Create Player Card Prefab");
        Selection.activeGameObject = card;

        Debug.Log("[GameSceneUISetup] PlayerCard created in scene.\n" +
                  "Drag it to Assets/_Project/Prefabs/ to save as prefab,\n" +
                  "then assign it to PlayerSidebar's playerCardPrefab field.\n" +
                  "Delete the scene instance after saving the prefab.");
    }

    // ==========================================
    // HELPERS
    // ==========================================

    private static Canvas FindOrCreateCanvas()
    {
        // Prefer existing UICanvas
        var existing = GameObject.Find("UICanvas");
        if (existing != null)
        {
            var c = existing.GetComponent<Canvas>();
            if (c != null) return c;
        }

        // Create new
        var canvasObj = new GameObject("UICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        Undo.RegisterCreatedObjectUndo(canvasObj, "Create UICanvas");
        return canvas;
    }

    private static GameObject CreatePanel(Transform parent, string name)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<Image>().raycastTarget = false;
        return obj;
    }

    /// <summary>
    /// Themes a top-level UI surface (bars, sidebars, popup panels) with the
    /// 9-sliced panel art, falling back to the flat dark color when the art
    /// isn't imported yet. Inner containers/rows deliberately stay untextured.
    /// </summary>
    private static void ThemeSurface(GameObject surface, float fallbackAlpha, float frameScale = 1f)
    {
        if (UITheme.ApplyPanel(surface, frameScale)) return;

        var image = surface.GetComponent<Image>();
        if (image != null)
            image.color = new Color(BgColor.r, BgColor.g, BgColor.b, fallbackAlpha);
    }

    private static GameObject CreateBorderedPanel(Transform parent, string name)
    {
        // Outer border
        var outer = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
        outer.transform.SetParent(parent, false);
        outer.GetComponent<Image>().color = PanelBg;
        outer.GetComponent<Image>().raycastTarget = false;
        var outline = outer.GetComponent<Outline>();
        outline.effectColor = PanelBorder;
        outline.effectDistance = new Vector2(BorderWidth, -BorderWidth);

        // Themed art replaces the procedural border when available.
        UITheme.ApplyPanel(outer, UITheme.SmallPanelFrameScale);
        return outer;
    }

    private static GameObject CreateTextObject(Transform parent, string name, string text, int fontSize, FontStyles style)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        var tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = TextPrimary;
        UITheme.ApplyFont(tmp, fontSize); // display font for headings, default for small text
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return obj;
    }

    private static GameObject CreateButton(Transform parent, string name, string text, float width, float height)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
        obj.GetComponent<Image>().color = ButtonPrimary;
        UITheme.ApplyButton(obj); // themed art when available, flat orange otherwise

        var btnColors = obj.GetComponent<Button>().colors;
        btnColors.normalColor = Color.white;
        btnColors.highlightedColor = new Color(1, 1, 1, 0.9f);
        btnColors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        obj.GetComponent<Button>().colors = btnColors;

        var label = CreateTextObject(obj.transform, "Label", text, 14, FontStyles.Bold);
        var labelRect = label.GetComponent<RectTransform>();
        StretchFull(labelRect);
        // The button art's horns occupy the top of the sprite, so the visible stone
        // bar is the lower part of the rect. Centring in the raw rect puts text on
        // the horns; inset the top so it centres on the bar itself.
        labelRect.offsetMin = new Vector2(10, 6);
        labelRect.offsetMax = new Vector2(-10, -18);
        label.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
        label.GetComponent<TMP_Text>().color = new Color(1f, 0.95f, 0.9f, 1f);

        return obj;
    }

    private static GameObject CreateStatRow(Transform parent, string name, string label, string value)
    {
        var row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        row.GetComponent<LayoutElement>().preferredHeight = 24;
        row.GetComponent<LayoutElement>().minHeight = 24;

        var hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth = true;
        // Must stay false: force-expanding height gives the ROW a flexible height,
        // and flexible sizes propagate upward - the parent VerticalLayoutGroup then
        // shares all its leftover space between the rows, ignoring the 24px
        // preferredHeight below and leaving the info panel looking sparse.
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        var labelObj = CreateTextObject(row.transform, "Label", label, 14, FontStyles.Normal);
        labelObj.GetComponent<TMP_Text>().color = TextSecondary;
        labelObj.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.MidlineLeft;

        var valueObj = CreateTextObject(row.transform, "Value", value, 14, FontStyles.Bold);
        valueObj.GetComponent<TMP_Text>().color = TextPrimary;
        valueObj.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.MidlineRight;

        return row;
    }

    private static GameObject CreateMiniStat(Transform parent, string name, string value, string iconName = null)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<LayoutElement>().preferredWidth = 40;

        var hlg = obj.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 3;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = true;   // without this, the LayoutElement sizes below are ignored
        hlg.childControlHeight = true;

        // Icon (themed art when available, otherwise the old flat placeholder square)
        var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        icon.transform.SetParent(obj.transform, false);
        icon.GetComponent<LayoutElement>().preferredWidth = 16;
        icon.GetComponent<LayoutElement>().preferredHeight = 16;
        var iconImage = icon.GetComponent<Image>();
        iconImage.raycastTarget = false;
        if (!UITheme.ApplyIcon(iconImage, iconName))
            iconImage.color = TextSecondary;

        // Count text
        var text = CreateTextObject(obj.transform, "Count", value, 13, FontStyles.Bold);
        text.GetComponent<TMP_Text>().color = TextSecondary;

        return obj;
    }

    private static TMP_Text GetStatValue(GameObject statRow)
    {
        var valueObj = statRow.transform.Find("Value");
        return valueObj != null ? valueObj.GetComponent<TMP_Text>() : null;
    }

    private static TMP_Text GetStatText(GameObject miniStat)
    {
        var countObj = miniStat.transform.Find("Count");
        return countObj != null ? countObj.GetComponent<TMP_Text>() : null;
    }

    private static void SetAnchored(GameObject obj, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, float height = 0f)
    {
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        if (height > 0f) rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
