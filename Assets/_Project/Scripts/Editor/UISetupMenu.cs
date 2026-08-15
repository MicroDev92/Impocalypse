using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor menu items for creating UI component hierarchies.
/// Creates properly structured GameObjects with all references wired up,
/// ready to be saved as prefabs or placed directly in scenes.
/// </summary>
public static class UISetupMenu
{
    [MenuItem("GameObject/UI/Phase Announcement", false, 10)]
    private static void CreatePhaseAnnouncement()
    {
        // Find or validate parent Canvas
        var canvas = GetOrCreateCanvas();
        if (canvas == null) return;

        // Root object with CanvasGroup
        var root = new GameObject("PhaseAnnouncement", typeof(RectTransform), typeof(CanvasGroup));
        root.transform.SetParent(canvas.transform, false);
        GameObjectUtility.SetParentAndAlign(root, canvas.gameObject);
        Undo.RegisterCreatedObjectUndo(root, "Create Phase Announcement");

        var rootRect = root.GetComponent<RectTransform>();
        StretchFull(rootRect);

        var canvasGroup = root.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // Background overlay (semi-transparent dark)
        var bgObj = CreateChild(root, "Overlay", typeof(Image));
        var bgRect = StretchFull(bgObj.GetComponent<RectTransform>());
        var bgImage = bgObj.GetComponent<Image>();
        bgImage.color = new Color(0.05f, 0.03f, 0.02f, 0.75f);
        bgImage.raycastTarget = true;

        // Banner panel (centered horizontal strip, 30% of screen height)
        var bannerObj = CreateChild(root, "Banner", typeof(Image));
        var bannerRect = bannerObj.GetComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0f, 0.35f);
        bannerRect.anchorMax = new Vector2(1f, 0.65f);
        bannerRect.offsetMin = Vector2.zero;
        bannerRect.offsetMax = Vector2.zero;
        var bannerImage = bannerObj.GetComponent<Image>();
        bannerImage.color = new Color(0.12f, 0.10f, 0.08f, 0.95f);
        bannerImage.raycastTarget = false;

        // Accent line — top edge of banner
        var topLine = CreateChild(bannerObj, "AccentLineTop", typeof(Image));
        var topLineRect = topLine.GetComponent<RectTransform>();
        topLineRect.anchorMin = new Vector2(0f, 1f);
        topLineRect.anchorMax = new Vector2(1f, 1f);
        topLineRect.pivot = new Vector2(0.5f, 1f);
        topLineRect.sizeDelta = new Vector2(0f, 3f);
        topLine.GetComponent<Image>().color = new Color(0.9f, 0.75f, 0.3f, 1f);
        topLine.GetComponent<Image>().raycastTarget = false;

        // Accent line — bottom edge of banner
        var botLine = CreateChild(bannerObj, "AccentLineBottom", typeof(Image));
        var botLineRect = botLine.GetComponent<RectTransform>();
        botLineRect.anchorMin = new Vector2(0f, 0f);
        botLineRect.anchorMax = new Vector2(1f, 0f);
        botLineRect.pivot = new Vector2(0.5f, 0f);
        botLineRect.sizeDelta = new Vector2(0f, 3f);
        botLine.GetComponent<Image>().color = new Color(0.9f, 0.75f, 0.3f, 1f);
        botLine.GetComponent<Image>().raycastTarget = false;

        // Phase title text (upper portion of banner)
        var titleObj = CreateChild(bannerObj, "PhaseTitle", typeof(TextMeshProUGUI));
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.45f);
        titleRect.anchorMax = new Vector2(1f, 0.95f);
        titleRect.offsetMin = new Vector2(40f, 0f);
        titleRect.offsetMax = new Vector2(-40f, 0f);
        var titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleText.text = "DRAFT";
        titleText.fontSize = 52;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.92f, 0.75f, 1f);
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        titleText.characterSpacing = 12f;
        titleText.raycastTarget = false;

        // Phase subtitle text (lower portion of banner)
        var subObj = CreateChild(bannerObj, "PhaseSubtitle", typeof(TextMeshProUGUI));
        var subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0f, 0.1f);
        subRect.anchorMax = new Vector2(1f, 0.45f);
        subRect.offsetMin = new Vector2(40f, 0f);
        subRect.offsetMax = new Vector2(-40f, 0f);
        var subText = subObj.GetComponent<TextMeshProUGUI>();
        subText.text = "Round 1 of 4 \u2022 Choose your territories";
        subText.fontSize = 22;
        subText.color = new Color(0.8f, 0.75f, 0.65f, 1f);
        subText.alignment = TextAlignmentOptions.Center;
        subText.textWrappingMode = TextWrappingModes.NoWrap;
        subText.raycastTarget = false;

        // Add the PhaseAnnouncementBanner component and wire references
        var announcement = root.AddComponent<UI.PhaseAnnouncementBanner>();
        var so = new SerializedObject(announcement);
        so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
        so.FindProperty("phaseText").objectReferenceValue = titleText;
        so.ApplyModifiedProperties();

        // Ensure it renders on top
        root.transform.SetAsLastSibling();

        Selection.activeGameObject = root;
        Debug.Log("[UISetupMenu] PhaseAnnouncement created. Save as prefab if desired.");
    }

    [MenuItem("GameObject/UI/Card Animator (Service)", false, 11)]
    private static void CreateCardAnimator()
    {
        var go = new GameObject("CardAnimator", typeof(CardAnimator));
        Undo.RegisterCreatedObjectUndo(go, "Create Card Animator");

        // Parent to selection if it's a reasonable parent, otherwise root
        if (Selection.activeGameObject != null)
        {
            GameObjectUtility.SetParentAndAlign(go, Selection.activeGameObject);
        }

        Selection.activeGameObject = go;
        Debug.Log("[UISetupMenu] CardAnimator created. Place in scene alongside other services.");
    }

    // ==========================================
    // HELPERS
    // ==========================================

    private static Canvas GetOrCreateCanvas()
    {
        // If something is selected under a Canvas, use that Canvas
        if (Selection.activeGameObject != null)
        {
            var parentCanvas = Selection.activeGameObject.GetComponentInParent<Canvas>();
            if (parentCanvas != null) return parentCanvas;
        }

        // Look for UICanvas by name
        var uiCanvas = GameObject.Find("UICanvas");
        if (uiCanvas != null)
        {
            var canvas = uiCanvas.GetComponent<Canvas>();
            if (canvas != null) return canvas;
        }

        // Look for any ScreenSpaceOverlay canvas
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return canvas;
        }

        Debug.LogError("[UISetupMenu] No Canvas found. Create a Canvas first.");
        return null;
    }

    private static GameObject CreateChild(GameObject parent, string name, params System.Type[] components)
    {
        var types = new System.Type[components.Length + 1];
        types[0] = typeof(RectTransform);
        components.CopyTo(types, 1);

        var obj = new GameObject(name, types);
        obj.transform.SetParent(parent.transform, false);
        return obj;
    }

    private static RectTransform StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }
}
