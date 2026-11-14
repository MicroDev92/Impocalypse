using TMPro;
using UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Brings the top bar in line with the restyled sidebars, as a re-runnable
/// command. Run via: GameObject > UI > Impocalypse - Restyle Top Bar
///
/// The bar was 50 units of 15-18pt text with the old icon sheet. It grows to 60,
/// everything moves to the display font at readable sizes, the resource counters
/// use the ArtGen icons, and all of it is centred on the bar's face (the strip
/// above the wooden lip) rather than on the raw rect. The sidebars move down to
/// make room.
/// </summary>
public static class TopBarRestyle
{
    private const string DisplayFontPath = "Assets/Art/UI/Fonts/LuckiestGuy SDF.asset";
    private const float BarHeight = 60f;
    private const float Lip = 13f;          // ink + wooden band at the foot of topbar_bg, in canvas units
    private const float SidebarGap = 4f;
    private const float IconSize = 34f;

    [MenuItem("GameObject/UI/Impocalypse - Restyle Top Bar", false, 35)]
    public static void Restyle()
    {
        RectTransform bar = null;
        foreach (var rt in Resources.FindObjectsOfTypeAll<RectTransform>())
        {
            if (rt.name == "TopBar" && rt.gameObject.scene.IsValid()) { bar = rt; break; }
        }
        if (bar == null)
        {
            Debug.LogError("[TopBarRestyle] No TopBar in the open scene. Open GameScene first.");
            return;
        }

        var display = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DisplayFontPath);
        var icons = AssetDatabase.LoadAssetAtPath<UIIconSet>("Assets/Resources/UIIconSet.asset");

        Undo.RecordObject(bar, "Restyle Top Bar");
        bar.sizeDelta = new Vector2(bar.sizeDelta.x, BarHeight);

        // Every block sits on the face: lift its bottom edge clear of the lip.
        Group(bar, "LeftGroup", 20f, 18f);
        Group(bar, "ResourceGroup", 8f, 20f);
        var status = bar.Find("StatusText") as RectTransform;
        if (status != null)
        {
            Undo.RecordObject(status, "Restyle Top Bar");
            status.offsetMin = new Vector2(status.offsetMin.x, Lip);
        }

        Text(bar, "LeftGroup/RoundText", display, 24, IconRow.Bright, TextAlignmentOptions.MidlineLeft);
        Text(bar, "LeftGroup/PhaseText", display, 24, IconRow.Accent, TextAlignmentOptions.MidlineLeft);
        Text(bar, "LeftGroup/DraftInfoText", display, 16, IconRow.Soft, TextAlignmentOptions.MidlineLeft);
        Text(bar, "StatusText", display, 22, IconRow.Bright, TextAlignmentOptions.Center);
        var onBoard = Text(bar, "ResourceGroup/OnBoardLabel", display, 16, IconRow.Soft, TextAlignmentOptions.MidlineRight);
        if (onBoard != null) onBoard.text = "On board";

        Counter(bar, "Charwood", icons != null ? icons.charwood : null, display);
        Counter(bar, "Bloodroot", icons != null ? icons.bloodroot : null, display);
        Counter(bar, "Souls", icons != null ? icons.souls : null, display);

        // Sidebars start just under the taller bar.
        foreach (var name in new[] { "LeftSidebar", "RightSidebar" })
        {
            var side = bar.parent.Find(name) as RectTransform;
            if (side == null) continue;
            Undo.RecordObject(side, "Restyle Top Bar");
            side.offsetMax = new Vector2(side.offsetMax.x, -(BarHeight + SidebarGap));
        }

        EditorSceneManager.MarkSceneDirty(bar.gameObject.scene);
        Debug.Log("[TopBarRestyle] Top bar restyled.");
    }

    private static void Group(Transform bar, string path, float spacing, float sidePadding)
    {
        var rt = bar.Find(path) as RectTransform;
        if (rt == null) return;

        Undo.RecordObject(rt, "Restyle Top Bar");
        rt.offsetMin = new Vector2(rt.offsetMin.x, Lip);

        var layout = rt.GetComponent<HorizontalLayoutGroup>();
        if (layout == null) return;
        Undo.RecordObject(layout, "Restyle Top Bar");
        layout.spacing = spacing;
        layout.padding = new RectOffset((int)sidePadding, (int)sidePadding, 0, 0);
        layout.childControlHeight = true;
        layout.childForceExpandHeight = true;   // children fill the face, so Midline alignment centres them
        EditorUtility.SetDirty(layout);
    }

    private static TMP_Text Text(Transform bar, string path, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment)
    {
        var t = bar.Find(path);
        var tmp = t != null ? t.GetComponent<TMP_Text>() : null;
        if (tmp == null)
        {
            Debug.LogWarning($"[TopBarRestyle] No text at '{path}' - skipped.");
            return null;
        }

        Undo.RecordObject(tmp, "Restyle Top Bar");
        if (font != null) tmp.font = font;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Normal;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        EditorUtility.SetDirty(tmp);
        return tmp;
    }

    private static void Counter(Transform bar, string resource, Sprite icon, TMP_FontAsset font)
    {
        var image = bar.Find($"ResourceGroup/{resource}Icon")?.GetComponent<Image>();
        if (image != null)
        {
            Undo.RecordObject(image, "Restyle Top Bar");
            if (icon != null) image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            EditorUtility.SetDirty(image);

            var element = image.GetComponent<LayoutElement>();
            if (element != null)
            {
                Undo.RecordObject(element, "Restyle Top Bar");
                element.minWidth = element.preferredWidth = IconSize;
                element.minHeight = element.preferredHeight = IconSize;
                element.flexibleHeight = 0;
                EditorUtility.SetDirty(element);
            }
        }

        Text(bar, $"ResourceGroup/{resource}Amount", font, 24, IconRow.Bright, TextAlignmentOptions.MidlineLeft);
    }
}
