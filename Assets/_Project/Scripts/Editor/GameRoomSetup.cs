using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor menu item that builds the GameRoom (lobby) UI.
/// Run via: GameObject > UI > Impocalypse - Build GameRoom UI
///
/// The lobby was the one screen that never got an art pass: stock Unity sprites,
/// LiberationSans, a 160x30 button with its label hanging outside it, and a
/// near-fullscreen translucent white box. This applies the language the main menu
/// already settled (stone panels, horned buttons, LuckiestGuy, dark backdrop)
/// rather than inventing anything new - deliberately, so it stays a theming pass
/// and not a redesign.
///
/// Colors mirror MainMenuSetup; these generator commands are each self-contained
/// (see UISetupMenu for the same pattern).
/// </summary>
public static class GameRoomSetup
{
    private static readonly Color BgColor = new(0.10f, 0.05f, 0.04f, 1f);
    private static readonly Color TextPrimary = new(1f, 0.92f, 0.82f, 1f);
    private static readonly Color TextSecondary = new(0.70f, 0.55f, 0.45f, 1f);
    private static readonly Color TextAccent = new(0.95f, 0.55f, 0.15f, 1f);
    private static readonly Color ButtonPrimary = new(0.85f, 0.35f, 0.08f, 1f);

    [MenuItem("GameObject/UI/Impocalypse - Build GameRoom UI", false, 29)]
    private static void BuildGameRoomUI()
    {
        var ui = Object.FindFirstObjectByType<GameRoomUI>();
        if (ui == null)
        {
            Debug.LogError("[GameRoomSetup] No GameRoomUI in the open scene. Open GameRoom first.");
            return;
        }

        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[GameRoomSetup] No Canvas in the open scene.");
            return;
        }

        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            Debug.LogWarning("[GameRoomSetup] No EventSystem - buttons will not respond.");

        // Re-running replaces rather than stacks.
        var previous = canvas.transform.Find("Lobby");
        if (previous != null)
        {
            Undo.DestroyObjectImmediate(previous.gameObject);
            Debug.Log("[GameRoomSetup] Replaced the existing Lobby.");
        }

        var root = new GameObject("Lobby", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        StretchFull(root.GetComponent<RectTransform>());

        var background = CreatePanel(root.transform, "Background");
        StretchFull(background.GetComponent<RectTransform>());
        if (!UITheme.ApplyPanel(background))
            background.GetComponent<Image>().color = BgColor;

        // Room code is the hero element: the host reads it out loud, so it is the one
        // thing on this screen that has to carry across a room.
        var codeLabel = CreateTextObject(root.transform, "RoomCodeLabel", "ROOM CODE", 22, FontStyles.Bold);
        var clRect = codeLabel.GetComponent<RectTransform>();
        clRect.anchorMin = clRect.anchorMax = new Vector2(0.5f, 1f);
        clRect.pivot = new Vector2(0.5f, 1f);
        clRect.sizeDelta = new Vector2(700, 30);
        clRect.anchoredPosition = new Vector2(0, -90);
        var clTmp = codeLabel.GetComponent<TMP_Text>();
        clTmp.alignment = TextAlignmentOptions.Center;
        clTmp.color = TextSecondary;
        clTmp.characterSpacing = 8f;

        var codeText = CreateTextObject(root.transform, "RoomNameText", "----", 84, FontStyles.Bold);
        var ctRect = codeText.GetComponent<RectTransform>();
        ctRect.anchorMin = ctRect.anchorMax = new Vector2(0.5f, 1f);
        ctRect.pivot = new Vector2(0.5f, 1f);
        ctRect.sizeDelta = new Vector2(700, 110);
        ctRect.anchoredPosition = new Vector2(0, -124);
        var ctTmp = codeText.GetComponent<TMP_Text>();
        ctTmp.alignment = TextAlignmentOptions.Center;
        ctTmp.color = TextAccent;
        ctTmp.characterSpacing = 16f;

        // Player list
        var listPanel = CreatePanel(root.transform, "PlayerListPanel");
        var lpRect = listPanel.GetComponent<RectTransform>();
        lpRect.anchorMin = lpRect.anchorMax = new Vector2(0.5f, 0.5f);
        lpRect.pivot = new Vector2(0.5f, 0.5f);
        lpRect.sizeDelta = new Vector2(560, 400);
        lpRect.anchoredPosition = new Vector2(0, 30);
        if (!UITheme.ApplyPanel(listPanel, UITheme.SmallPanelFrameScale))
            listPanel.GetComponent<Image>().color = new Color(BgColor.r, BgColor.g, BgColor.b, 0.95f);

        var listHeader = CreateTextObject(listPanel.transform, "PlayersHeader", "PLAYERS", 20, FontStyles.Bold);
        var lhRect = listHeader.GetComponent<RectTransform>();
        lhRect.anchorMin = new Vector2(0f, 1f);
        lhRect.anchorMax = new Vector2(1f, 1f);
        lhRect.pivot = new Vector2(0.5f, 1f);
        lhRect.sizeDelta = new Vector2(-60, 32);
        lhRect.anchoredPosition = new Vector2(0, -24);
        var lhTmp = listHeader.GetComponent<TMP_Text>();
        lhTmp.alignment = TextAlignmentOptions.MidlineLeft;
        lhTmp.color = TextSecondary;

        var listContainer = new GameObject("PlayerListContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        listContainer.transform.SetParent(listPanel.transform, false);
        var lcRect = listContainer.GetComponent<RectTransform>();
        lcRect.anchorMin = Vector2.zero;
        lcRect.anchorMax = Vector2.one;
        lcRect.offsetMin = new Vector2(30, 28);
        lcRect.offsetMax = new Vector2(-30, -64);
        var lcVlg = listContainer.GetComponent<VerticalLayoutGroup>();
        lcVlg.spacing = 10;
        lcVlg.childAlignment = TextAnchor.UpperCenter;
        lcVlg.childControlWidth = true;
        lcVlg.childForceExpandWidth = true;
        lcVlg.childControlHeight = true;
        lcVlg.childForceExpandHeight = false;

        // Status
        var status = CreateTextObject(root.transform, "StatusText", "Waiting for host...", 20, FontStyles.Italic);
        var stRect = status.GetComponent<RectTransform>();
        stRect.anchorMin = stRect.anchorMax = new Vector2(0.5f, 0.5f);
        stRect.pivot = new Vector2(0.5f, 0.5f);
        stRect.sizeDelta = new Vector2(760, 40);
        stRect.anchoredPosition = new Vector2(0, -228);
        var stTmp = status.GetComponent<TMP_Text>();
        stTmp.alignment = TextAlignmentOptions.Center;
        stTmp.color = TextSecondary;

        var start = CreateButton(root.transform, "StartGameButton", "START GAME", 420f, 80f, 26);
        var sRect = start.GetComponent<RectTransform>();
        sRect.anchorMin = sRect.anchorMax = new Vector2(0.5f, 0.5f);
        sRect.pivot = new Vector2(0.5f, 0.5f);
        sRect.anchoredPosition = new Vector2(0, -320);

        // Leave sits centred below Start. It was top-left on the argument that
        // opposite intentions should not be adjacent - but that is the GameScene's
        // stakes, not the lobby's: leaving a room here costs a rejoin with the code,
        // not everyone's game. The 44px gap and the smaller button carry the
        // hierarchy. Start is host-only (GameRoomUI.UpdateStartButton), so a guest
        // sees Leave alone with empty space above it, which reads fine.
        var leave = CreateButton(root.transform, "LeaveButton", "LEAVE", 180f, 52f, 18);
        var lvRect = leave.GetComponent<RectTransform>();
        lvRect.anchorMin = lvRect.anchorMax = new Vector2(0.5f, 0.5f);
        lvRect.pivot = new Vector2(0.5f, 0.5f);
        lvRect.anchoredPosition = new Vector2(0, -430);

        var so = new SerializedObject(ui);
        so.FindProperty("playerListParent").objectReferenceValue = listContainer.transform;
        so.FindProperty("startButton").objectReferenceValue = start.GetComponent<Button>();
        so.FindProperty("leaveButton").objectReferenceValue = leave.GetComponent<Button>();
        so.FindProperty("statusText").objectReferenceValue = status.GetComponent<TMP_Text>();
        so.FindProperty("roomNameText").objectReferenceValue = codeText.GetComponent<TMP_Text>();
        var entry = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/PlayerEntry.prefab");
        if (entry != null) so.FindProperty("playerEntryPrefab").objectReferenceValue = entry;
        so.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(root, "Build GameRoom UI");
        Selection.activeGameObject = root;
        Debug.Log("[GameRoomSetup] Lobby built and wired." +
                  (entry == null ? " NOTE: PlayerEntry prefab not found." : ""));
    }

    // ==========================================
    // HELPERS
    // ==========================================

    private static GameObject CreatePanel(Transform parent, string name)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<Image>().raycastTarget = false;
        return obj;
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
        UITheme.ApplyFont(tmp, fontSize);
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return obj;
    }

    private static GameObject CreateButton(Transform parent, string name, string text,
        float width, float height, int fontSize)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
        obj.GetComponent<Image>().color = ButtonPrimary;
        UITheme.ApplyButton(obj);

        var colors = obj.GetComponent<Button>().colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1, 1, 1, 0.9f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.disabledColor = new Color(0.45f, 0.42f, 0.40f, 1f);
        obj.GetComponent<Button>().colors = colors;

        var labelObj = CreateTextObject(obj.transform, "Label", text, fontSize, FontStyles.Bold);
        var lrt = labelObj.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        // The button art's horns sit at the top of the sprite, so the visible bar is
        // the lower part of the rect - centring in the raw rect puts text on the horns.
        lrt.offsetMin = new Vector2(10, 6);
        lrt.offsetMax = new Vector2(-10, -18);
        var lblTmp = labelObj.GetComponent<TMP_Text>();
        lblTmp.alignment = TextAlignmentOptions.Center;
        lblTmp.color = new Color(1f, 0.95f, 0.9f, 1f);

        return obj;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
