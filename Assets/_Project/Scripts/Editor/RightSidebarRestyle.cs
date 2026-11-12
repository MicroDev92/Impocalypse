using Construction;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Readability pass over the right sidebar, as a re-runnable command.
/// Run via: GameObject > UI > Impocalypse - Restyle Right Sidebar
///
/// Two playtest findings live here:
///  - "Right sidebar hard to read": everything in it was 14pt LiberationSans in a
///    muted brown, with a 60px preview. Headings move to the display font, body
///    copy stays a plain sans (LuckiestGuy is capitals-only and tiring in
///    paragraphs) but grows to 18pt in a bright cream, and the preview doubles.
///  - "White text above the board during build phase blocks clicking and viewing":
///    the build instruction was a 600px text box floating over the board, and a
///    raycast target. It moves into a Build Phase block at the foot of this
///    sidebar, directly above the Done button it leads to.
///
/// It restyles the objects already in the scene rather than rebuilding them, so
/// every reference other scripts hold stays valid. Safe to run repeatedly.
/// </summary>
public static class RightSidebarRestyle
{
    private const string DisplayFontPath = "Assets/Art/UI/Fonts/LuckiestGuy SDF.asset";

    private static readonly Color Accent = new(0.95f, 0.55f, 0.15f, 1f);
    private static readonly Color TextBright = new(0.96f, 0.91f, 0.83f, 1f);
    private static readonly Color TextSoft = new(0.84f, 0.72f, 0.61f, 1f);
    private static readonly Color Recess = new(0.141f, 0.078f, 0.059f, 0.95f);

    [MenuItem("GameObject/UI/Impocalypse - Restyle Right Sidebar", false, 31)]
    public static void Restyle()
    {
        Transform sidebar = null;
        foreach (var rt in Resources.FindObjectsOfTypeAll<RectTransform>())
        {
            if (rt.name == "RightSidebar" && rt.gameObject.scene.IsValid()) { sidebar = rt; break; }
        }
        if (sidebar == null)
        {
            Debug.LogError("[RightSidebarRestyle] No RightSidebar in the open scene. Open GameScene first.");
            return;
        }

        var display = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DisplayFontPath);
        if (display == null) Debug.LogWarning("[RightSidebarRestyle] Display font not found - headings keep their current font.");

        // Empty state
        Style(sidebar, "EmptyState/EmptyStateText", null, 18, TextSoft, FontStyles.Italic);

        // Header, preview, name
        Style(sidebar, "TileContentPanel/TileInfoHeader", display, 28, Accent, FontStyles.Normal);
        Height(sidebar, "TileContentPanel/TileInfoHeader", 42);

        Height(sidebar, "TileContentPanel/TilePreviewPanel", 216);
        var image = sidebar.Find("TileContentPanel/TilePreviewPanel/TilePreviewImage") as RectTransform;
        if (image != null)
        {
            Undo.RecordObject(image, "Restyle Right Sidebar");
            image.anchorMin = image.anchorMax = new Vector2(0.5f, 0.62f);
            image.sizeDelta = new Vector2(128, 128);
            image.anchoredPosition = Vector2.zero;
        }
        var typeName = sidebar.Find("TileContentPanel/TilePreviewPanel/TileTypeName") as RectTransform;
        if (typeName != null)
        {
            Undo.RecordObject(typeName, "Restyle Right Sidebar");
            typeName.anchorMin = new Vector2(0f, 0f);
            typeName.anchorMax = new Vector2(1f, 0.27f);
            typeName.offsetMin = new Vector2(6, 4);
            typeName.offsetMax = new Vector2(-6, 0);
        }
        Style(sidebar, "TileContentPanel/TilePreviewPanel/TileTypeName", display, 24, TextBright, FontStyles.Normal);

        // Stat rows: label in the display font, value in bold sans
        foreach (var row in new[] { "CoordRow", "OwnerRow", "ResourceRow", "CityRow" })
        {
            string path = "TileContentPanel/StatsContainer/" + row;
            Height(sidebar, path, 32);
            Style(sidebar, path + "/Label", display, 17, TextSoft, FontStyles.Normal);
            Style(sidebar, path + "/Value", null, 18, TextBright, FontStyles.Bold);
        }
        // -1 = let the container size itself from its visible rows. It used to be a
        // fixed height, which left a hole wherever the building row was hidden.
        Height(sidebar, "TileContentPanel/StatsContainer", -1);

        // The rules text: the reason this panel exists
        var desc = Style(sidebar, "TileContentPanel/DescriptionText", null, 18, TextBright, FontStyles.Normal);
        if (desc != null)
        {
            desc.lineSpacing = 14f;
            desc.textWrappingMode = TextWrappingModes.Normal;
            desc.alignment = TextAlignmentOptions.TopLeft;
            EditorUtility.SetDirty(desc);
        }

        BuildGuidance(sidebar, display);

        EditorSceneManager.MarkSceneDirty(sidebar.gameObject.scene);
        Debug.Log("[RightSidebarRestyle] Right sidebar restyled.");
    }

    /// <summary>
    /// The Build Phase block, and the instruction text re-homed into it. The text
    /// object itself is moved rather than replaced, so ConstructionUIHandler keeps
    /// writing to the same reference it always had.
    /// </summary>
    private static void BuildGuidance(Transform sidebar, TMP_FontAsset display)
    {
        var handler = Object.FindFirstObjectByType<ConstructionUIHandler>(FindObjectsInactive.Include);
        if (handler == null)
        {
            Debug.LogWarning("[RightSidebarRestyle] No ConstructionUIHandler - build guidance skipped.");
            return;
        }

        var so = new SerializedObject(handler);
        var instruction = so.FindProperty("instructionText").objectReferenceValue as TMP_Text;
        if (instruction == null)
        {
            Debug.LogWarning("[RightSidebarRestyle] ConstructionUIHandler has no instruction text - build guidance skipped.");
            return;
        }

        var block = sidebar.Find("BuildGuidance") as RectTransform;
        if (block == null)
        {
            var go = new GameObject("BuildGuidance", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "Restyle Right Sidebar");
            block = go.GetComponent<RectTransform>();
            block.SetParent(sidebar, false);
        }
        block.anchorMin = new Vector2(0f, 0f);
        block.anchorMax = new Vector2(1f, 0f);
        block.pivot = new Vector2(0.5f, 0f);
        block.sizeDelta = new Vector2(-52, 168);
        block.anchoredPosition = new Vector2(0, 26);
        var bg = block.GetComponent<Image>();
        bg.color = Recess;
        bg.raycastTarget = false;

        var header = block.Find("Header") as RectTransform;
        if (header == null)
        {
            var go = new GameObject("Header", typeof(RectTransform), typeof(TextMeshProUGUI));
            header = go.GetComponent<RectTransform>();
            header.SetParent(block, false);
        }
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.sizeDelta = new Vector2(-28, 34);
        header.anchoredPosition = new Vector2(0, -12);
        var headerText = header.GetComponent<TextMeshProUGUI>();
        headerText.text = "BUILD PHASE";
        if (display != null) headerText.font = display;
        headerText.fontSize = 24;
        headerText.color = Accent;
        headerText.alignment = TextAlignmentOptions.MidlineLeft;
        headerText.raycastTarget = false;

        // Move the live instruction text in, and stop it eating clicks.
        var rt = instruction.rectTransform;
        Undo.SetTransformParent(rt, block, "Restyle Right Sidebar");
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(14, 12);
        rt.offsetMax = new Vector2(-14, -52);
        rt.localScale = Vector3.one;
        instruction.fontSize = 18;
        instruction.fontStyle = FontStyles.Normal;
        instruction.color = TextBright;
        instruction.alignment = TextAlignmentOptions.TopLeft;
        instruction.textWrappingMode = TextWrappingModes.Normal;
        instruction.overflowMode = TextOverflowModes.Overflow;
        instruction.lineSpacing = 10f;
        instruction.raycastTarget = false;
        instruction.gameObject.SetActive(true);
        EditorUtility.SetDirty(instruction);

        // The Done button sits right under this block: same lettering as the header.
        var done = so.FindProperty("doneButton").objectReferenceValue as Button;
        var doneLabel = done != null ? done.GetComponentInChildren<TMP_Text>(true) : null;
        if (doneLabel != null)
        {
            Undo.RecordObject(doneLabel, "Restyle Right Sidebar");
            if (display != null) doneLabel.font = display;
            doneLabel.fontSize = 26;
            doneLabel.color = TextBright;
            doneLabel.alignment = TextAlignmentOptions.Center;
            EditorUtility.SetDirty(doneLabel);
        }

        // Hidden until the build phase; ConstructionUIHandler shows and hides it.
        block.gameObject.SetActive(false);

        so.FindProperty("guidancePanel").objectReferenceValue = block.gameObject;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(handler);
    }

    private static TMP_Text Style(Transform root, string path, TMP_FontAsset font, float size, Color color, FontStyles style)
    {
        var t = root.Find(path);
        var tmp = t != null ? t.GetComponent<TMP_Text>() : null;
        if (tmp == null)
        {
            Debug.LogWarning($"[RightSidebarRestyle] No text at '{path}' - skipped.");
            return null;
        }

        Undo.RecordObject(tmp, "Restyle Right Sidebar");
        if (font != null) tmp.font = font;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.enableAutoSizing = false;
        EditorUtility.SetDirty(tmp);
        return tmp;
    }

    private static void Height(Transform root, string path, float height)
    {
        var t = root.Find(path);
        if (t == null) return;

        var le = t.GetComponent<LayoutElement>();
        if (le == null) le = Undo.AddComponent<LayoutElement>(t.gameObject);
        Undo.RecordObject(le, "Restyle Right Sidebar");
        le.minHeight = height;
        le.preferredHeight = height;
        EditorUtility.SetDirty(le);
    }
}
