using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor menu item that builds the main menu into the open Launcher scene.
/// Run via: GameObject > UI > Impocalypse - Add Main Menu
///
/// Creates:
///   UICanvas
///   └── MainMenu            (MainMenuUI component, all references wired)
///       ├── Background
///       ├── Title
///       ├── NameRow         (label + name field)
///       ├── QuickPlayButton
///       ├── CreateGameButton
///       ├── JoinRow         (join button + code field)
///       ├── StatusText
///       └── BottomRow       (Options - reserved and disabled - + Quit)
///
/// Colors mirror GameSceneUISetup so the menu and the game read as one product;
/// they are duplicated rather than shared because these generator commands are
/// each self-contained (see UISetupMenu for the same pattern).
/// </summary>
public static class MainMenuSetup
{
    private static readonly Color BgColor = new(0.10f, 0.05f, 0.04f, 1f);
    private static readonly Color FieldBg = new(0.06f, 0.03f, 0.03f, 0.95f);
    private static readonly Color FieldBorder = new(0.55f, 0.18f, 0.10f, 0.6f);
    private static readonly Color TextPrimary = new(1f, 0.92f, 0.82f, 1f);
    private static readonly Color TextSecondary = new(0.70f, 0.55f, 0.45f, 1f);
    private static readonly Color TextAccent = new(0.95f, 0.55f, 0.15f, 1f);
    private static readonly Color ButtonPrimary = new(0.85f, 0.35f, 0.08f, 1f);

    private const float ColumnWidth = 420f;
    private const float ButtonHeight = 64f;

    [MenuItem("GameObject/UI/Impocalypse - Add Main Menu", false, 28)]
    private static void AddMainMenu()
    {
        var canvas = FindOrCreateCanvas();

        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            Debug.LogWarning("[MainMenuSetup] No EventSystem in this scene - the buttons will " +
                             "not respond to clicks. Add one via GameObject > UI > Event System.");
        }

        var root = new GameObject("MainMenu", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        StretchFull(root.GetComponent<RectTransform>());

        var background = CreatePanel(root.transform, "Background");
        StretchFull(background.GetComponent<RectTransform>());
        if (!UITheme.ApplyPanel(background))
            background.GetComponent<Image>().color = BgColor;

        // ---- Title ----
        var title = CreateTextObject(root.transform, "Title", "IMPOCALYPSE", 64, FontStyles.Bold);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(900, 90);
        titleRect.anchoredPosition = new Vector2(0, -110);
        title.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
        title.GetComponent<TMP_Text>().color = TextAccent;
        title.GetComponent<TMP_Text>().characterSpacing = 10f;

        // ---- Name row ----
        var nameRow = new GameObject("NameRow", typeof(RectTransform));
        nameRow.transform.SetParent(root.transform, false);
        var nameRowRect = nameRow.GetComponent<RectTransform>();
        nameRowRect.anchorMin = nameRowRect.anchorMax = new Vector2(0.5f, 0.5f);
        nameRowRect.pivot = new Vector2(0.5f, 0.5f);
        nameRowRect.sizeDelta = new Vector2(ColumnWidth, 52);
        nameRowRect.anchoredPosition = new Vector2(0, 150);

        var nameLabel = CreateTextObject(nameRow.transform, "Label", "NAME", 18, FontStyles.Bold);
        var nameLabelRect = nameLabel.GetComponent<RectTransform>();
        nameLabelRect.anchorMin = nameLabelRect.anchorMax = new Vector2(0f, 0.5f);
        nameLabelRect.pivot = new Vector2(0f, 0.5f);
        nameLabelRect.sizeDelta = new Vector2(90, 52);
        nameLabelRect.anchoredPosition = Vector2.zero;
        nameLabel.GetComponent<TMP_Text>().color = TextSecondary;
        nameLabel.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.MidlineLeft;

        var nameInput = CreateInputField(nameRow.transform, "NameInput", "Your name", 20);
        var nameInputRect = nameInput.GetComponent<RectTransform>();
        nameInputRect.anchorMin = nameInputRect.anchorMax = new Vector2(1f, 0.5f);
        nameInputRect.pivot = new Vector2(1f, 0.5f);
        nameInputRect.sizeDelta = new Vector2(320, 52);
        nameInputRect.anchoredPosition = Vector2.zero;
        nameInput.GetComponent<TMP_InputField>().characterLimit = 12;

        // ---- Quick Play ----
        var quickPlay = CreateButton(root.transform, "QuickPlayButton", "QUICK PLAY", ColumnWidth, ButtonHeight);
        PlaceCentered(quickPlay, 60);

        // ---- Create Game ----
        var createGame = CreateButton(root.transform, "CreateGameButton", "CREATE GAME", ColumnWidth, ButtonHeight);
        PlaceCentered(createGame, -20);

        // ---- Join row ----
        var joinRow = new GameObject("JoinRow", typeof(RectTransform));
        joinRow.transform.SetParent(root.transform, false);
        var joinRowRect = joinRow.GetComponent<RectTransform>();
        joinRowRect.anchorMin = joinRowRect.anchorMax = new Vector2(0.5f, 0.5f);
        joinRowRect.pivot = new Vector2(0.5f, 0.5f);
        joinRowRect.sizeDelta = new Vector2(ColumnWidth, ButtonHeight);
        joinRowRect.anchoredPosition = new Vector2(0, -100);

        var joinGame = CreateButton(joinRow.transform, "JoinGameButton", "JOIN GAME", 250f, ButtonHeight);
        var joinRect = joinGame.GetComponent<RectTransform>();
        joinRect.anchorMin = joinRect.anchorMax = new Vector2(0f, 0.5f);
        joinRect.pivot = new Vector2(0f, 0.5f);
        joinRect.anchoredPosition = Vector2.zero;

        var codeInput = CreateInputField(joinRow.transform, "CodeInput", "CODE", 24);
        var codeRect = codeInput.GetComponent<RectTransform>();
        codeRect.anchorMin = codeRect.anchorMax = new Vector2(1f, 0.5f);
        codeRect.pivot = new Vector2(1f, 0.5f);
        codeRect.sizeDelta = new Vector2(150, ButtonHeight);
        codeRect.anchoredPosition = Vector2.zero;

        var codeField = codeInput.GetComponent<TMP_InputField>();
        codeField.characterLimit = RoomCode.Length;
        codeField.textComponent.alignment = TextAlignmentOptions.Center;
        codeField.textComponent.characterSpacing = 6f;
        codeField.placeholder.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

        // ---- Status ----
        var status = CreateTextObject(root.transform, "StatusText", "Connecting...", 18, FontStyles.Normal);
        var statusRect = status.GetComponent<RectTransform>();
        statusRect.anchorMin = statusRect.anchorMax = new Vector2(0.5f, 0.5f);
        statusRect.pivot = new Vector2(0.5f, 0.5f);
        statusRect.sizeDelta = new Vector2(700, 40);
        statusRect.anchoredPosition = new Vector2(0, -180);
        status.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
        status.GetComponent<TMP_Text>().color = TextSecondary;

        // ---- Bottom row ----
        var options = CreateButton(root.transform, "OptionsButton", "Options", 200f, 48f);
        var optRect = options.GetComponent<RectTransform>();
        optRect.anchorMin = optRect.anchorMax = new Vector2(0.5f, 0f);
        optRect.pivot = new Vector2(1f, 0f);
        optRect.anchoredPosition = new Vector2(-10, 60);

        // Deliberately dead. The slot is reserved so the layout does not need
        // redesigning when sound lands and there is finally something to put in it;
        // MainMenuUI does not reference it at all.
        options.GetComponent<Button>().interactable = false;

        var quit = CreateButton(root.transform, "QuitButton", "Quit", 200f, 48f);
        var quitRect = quit.GetComponent<RectTransform>();
        quitRect.anchorMin = quitRect.anchorMax = new Vector2(0.5f, 0f);
        quitRect.pivot = new Vector2(0f, 0f);
        quitRect.anchoredPosition = new Vector2(10, 60);

        // ---- Wire ----
        var ui = root.AddComponent<MainMenuUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("nameInput").objectReferenceValue = nameInput.GetComponent<TMP_InputField>();
        so.FindProperty("quickPlayButton").objectReferenceValue = quickPlay.GetComponent<Button>();
        so.FindProperty("createGameButton").objectReferenceValue = createGame.GetComponent<Button>();
        so.FindProperty("joinGameButton").objectReferenceValue = joinGame.GetComponent<Button>();
        so.FindProperty("codeInput").objectReferenceValue = codeField;
        so.FindProperty("statusText").objectReferenceValue = status.GetComponent<TMP_Text>();
        so.FindProperty("quitButton").objectReferenceValue = quit.GetComponent<Button>();
        so.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(root, "Add Main Menu");
        Selection.activeGameObject = root;
        Debug.Log("[MainMenuSetup] Main menu added to the open scene.");
    }

    // ==========================================
    // HELPERS
    // ==========================================

    private static void PlaceCentered(GameObject obj, float y)
    {
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, y);
    }

    private static Canvas FindOrCreateCanvas()
    {
        var existing = GameObject.Find("UICanvas");
        if (existing != null)
        {
            var found = existing.GetComponent<Canvas>();
            if (found != null) return found;
        }

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
        // The older generators still use enableWordWrapping, which Unity 6 deprecated.
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
        UITheme.ApplyButton(obj);

        var btnColors = obj.GetComponent<Button>().colors;
        btnColors.normalColor = Color.white;
        btnColors.highlightedColor = new Color(1, 1, 1, 0.9f);
        btnColors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btnColors.disabledColor = new Color(0.45f, 0.42f, 0.40f, 1f);
        obj.GetComponent<Button>().colors = btnColors;

        var label = CreateTextObject(obj.transform, "Label", text, 20, FontStyles.Bold);
        var labelRect = label.GetComponent<RectTransform>();
        StretchFull(labelRect);
        // The button art's horns occupy the top of the sprite, so the visible stone bar
        // is the lower part of the rect - inset the top or the text sits on the horns.
        labelRect.offsetMin = new Vector2(10, 6);
        labelRect.offsetMax = new Vector2(-10, -18);
        label.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
        label.GetComponent<TMP_Text>().color = new Color(1f, 0.95f, 0.9f, 1f);

        return obj;
    }

    /// <summary>
    /// TMP input fields need their viewport, text and placeholder built and assigned by
    /// hand - the component does nothing useful until all three are wired.
    /// </summary>
    private static GameObject CreateInputField(Transform parent, string name, string placeholderText, int fontSize)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(Outline));
        obj.transform.SetParent(parent, false);

        // Flat, not panel art: fields need to read as recessed and typable, and the
        // 9-sliced stone frame at this size just muddies the text behind it.
        obj.GetComponent<Image>().color = FieldBg;
        var outline = obj.GetComponent<Outline>();
        outline.effectColor = FieldBorder;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(obj.transform, false);
        var taRect = textArea.GetComponent<RectTransform>();
        StretchFull(taRect);
        taRect.offsetMin = new Vector2(12, 6);
        taRect.offsetMax = new Vector2(-12, -6);

        var placeholder = CreateTextObject(textArea.transform, "Placeholder", placeholderText, fontSize, FontStyles.Italic);
        StretchFull(placeholder.GetComponent<RectTransform>());
        var placeholderText_ = placeholder.GetComponent<TMP_Text>();
        placeholderText_.color = TextSecondary;
        placeholderText_.alignment = TextAlignmentOptions.MidlineLeft;

        var textObj = CreateTextObject(textArea.transform, "Text", string.Empty, fontSize, FontStyles.Normal);
        StretchFull(textObj.GetComponent<RectTransform>());
        var tmpText = textObj.GetComponent<TMP_Text>();
        tmpText.alignment = TextAlignmentOptions.MidlineLeft;

        var input = obj.GetComponent<TMP_InputField>();
        input.textViewport = taRect;
        input.textComponent = tmpText;
        input.placeholder = placeholderText_;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.customCaretColor = true;
        input.caretColor = TextAccent;
        input.selectionColor = new Color(TextAccent.r, TextAccent.g, TextAccent.b, 0.35f);

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
