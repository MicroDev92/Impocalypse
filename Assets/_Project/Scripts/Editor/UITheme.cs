using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Art lookup for the UI generator commands. Keeps every sprite/font path in one
/// place so re-theming is an art swap, not a code edit.
///
/// Every getter degrades gracefully: if the art isn't imported yet, the generator
/// falls back to the old flat-color look instead of erroring, so the commands stay
/// runnable at any point in the art pass.
///
/// Expected assets (import as Sprite (2D and UI)):
///   Assets/Art/UI/Panels/panel_bg.png   - 9-sliced panel background
///   Assets/Art/UI/Panels/button_bg.png  - 9-sliced button background
///   Assets/Art/UI/Icons/ui_icons.png    - multi-sprite sheet, sub-sprites named:
///                                         charwood, bloodroot, souls,
///                                         citadel, territory, infamy
///   Assets/Art/UI/Fonts/LuckiestGuy SDF.asset - display font
/// </summary>
public static class UITheme
{
    public const string PanelSpritePath = "Assets/Art/UI/Panels/panel_bg.png";
    public const string ButtonSpritePath = "Assets/Art/UI/Panels/button_bg.png";
    public const string IconSheetPath = "Assets/Art/UI/Icons/ui_icons.png";
    public const string DisplayFontPath = "Assets/Art/UI/Fonts/LuckiestGuy SDF.asset";

    /// <summary>
    /// Text at or above this size gets the chunky display font (titles, phase,
    /// buttons, counters). Smaller text keeps the clean default so dense stats
    /// and status lines stay legible - display fonts hurt readability at 5-6px
    /// letter heights.
    /// </summary>
    public const int DisplayFontMinSize = 16;

    // ==========================================
    // PANELS & BUTTONS
    // ==========================================

    /// <summary>
    /// Applies the themed 9-sliced panel art to an Image, or leaves the caller's
    /// flat color alone when the art is missing.
    /// </summary>
    /// <summary>
    /// Frame thickness dial. The sprite's 9-slice borders are fixed in source
    /// pixels, so a thin surface (the ~50px top bar) needs a bigger multiplier or
    /// its top and bottom borders overlap and smear into each other.
    /// Higher multiplier = thinner rendered frame.
    /// </summary>
    /// Sidebar value is tuned so the rendered frame (~12px) matches the existing
    /// 12px content padding - otherwise panel content sits on top of the stone.
    public const float TopBarFrameScale = 4f;
    public const float SidebarFrameScale = 3f;
    public const float SmallPanelFrameScale = 3f;

    public static bool ApplyPanel(GameObject panel, float frameScale = 1f)
    {
        var image = panel != null ? panel.GetComponent<Image>() : null;
        var sprite = LoadSprite(PanelSpritePath);
        if (image == null || sprite == null) return false;

        image.sprite = sprite;
        image.type = Image.Type.Sliced;   // corners stay crisp at any panel size
        image.color = Color.white;        // show the art untinted
        image.pixelsPerUnitMultiplier = frameScale;

        // The hand-drawn border replaces the old procedural outline.
        var outline = panel.GetComponent<Outline>();
        if (outline != null) Object.DestroyImmediate(outline);

        return true;
    }

    /// <summary>Applies the themed 9-sliced button art. Returns false if missing.</summary>
    /// Button art is imported at a high PPU (1000), which already scales the
    /// borders down to ~23px - so no extra thinning here, or the horns vanish.
    public const float ButtonFrameScale = 1f;

    public static bool ApplyButton(GameObject button)
    {
        var image = button != null ? button.GetComponent<Image>() : null;
        var sprite = LoadSprite(ButtonSpritePath);
        if (image == null || sprite == null) return false;

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        image.pixelsPerUnitMultiplier = ButtonFrameScale;
        return true;
    }

    // ==========================================
    // ICONS
    // ==========================================

    /// <summary>
    /// Looks up a named sub-sprite from the UI icon sheet (case-insensitive).
    /// Returns null when the sheet or the named slice is missing.
    /// </summary>
    public static Sprite Icon(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return null;

        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(IconSheetPath))
        {
            if (asset is Sprite sprite &&
                string.Equals(sprite.name, spriteName, System.StringComparison.OrdinalIgnoreCase))
            {
                return sprite;
            }
        }
        return null;
    }

    /// <summary>Assigns an icon to an Image if both exist; returns false otherwise.</summary>
    public static bool ApplyIcon(Image target, string spriteName)
    {
        var sprite = Icon(spriteName);
        if (target == null || sprite == null) return false;

        target.sprite = sprite;
        target.color = Color.white;
        target.preserveAspect = true;
        return true;
    }

    // ==========================================
    // FONT
    // ==========================================

    /// <summary>
    /// The font for a given size: chunky display font for headings and buttons,
    /// engine default for small dense text. Null means "leave TMP's default".
    /// </summary>
    public static TMP_FontAsset FontFor(int fontSize)
    {
        return fontSize >= DisplayFontMinSize ? DisplayFont : null;
    }

    public static TMP_FontAsset DisplayFont =>
        AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DisplayFontPath);

    /// <summary>Applies the size-appropriate font to a TMP component.</summary>
    public static void ApplyFont(TMP_Text tmp, int fontSize)
    {
        if (tmp == null) return;
        var font = FontFor(fontSize);
        if (font != null) tmp.font = font;
    }

    // ==========================================

    private static Sprite LoadSprite(string path) =>
        AssetDatabase.LoadAssetAtPath<Sprite>(path);
}
