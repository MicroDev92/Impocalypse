using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies the ArtGen panel frames, as a re-runnable command.
///
///  GameObject > UI > Impocalypse - Apply Panel Frames
///      Fixes the 9-slice import settings of the four chrome sprites (they are
///      authored at 2x, so PPU 200) and points the inner panels and the top bar
///      of the open scene at them. Sidebars and buttons already reference
///      panel_bg / button_bg, which were replaced in place.
/// </summary>
public static class ChromeRestyle
{
    private const string Panels = "Assets/Art/UI/Panels/";

    [MenuItem("GameObject/UI/Impocalypse - Apply Panel Frames", false, 33)]
    public static void Apply()
    {
        // The sidebar frame is shown a little larger than authored, so its band stays chunky.
        Slice("panel_bg", new Vector4(80, 80, 80, 80), 140);
        var button = Slice("button_bg", new Vector4(48, 48, 48, 48));
        var inset = Slice("panel_inset", new Vector4(40, 40, 40, 40));
        var bar = Slice("topbar_bg", new Vector4(8, 30, 8, 0));

        Transform canvas = null;
        foreach (var rt in Resources.FindObjectsOfTypeAll<RectTransform>())
        {
            if (rt.name == "LeftSidebar" && rt.gameObject.scene.IsValid()) { canvas = rt.parent; break; }
        }
        int labels = CentreButtonLabels(button);
        if (canvas == null)
        {
            if (labels > 0) EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[ChromeRestyle] Sprites re-imported. No game UI in the open scene, so nothing to re-point.");
            return;
        }

        Set(canvas, "TopBar", bar);
        Set(canvas, "LeftSidebar/PactsPanel", inset);
        Set(canvas, "LeftSidebar/DomainsPanel", inset);
        Set(canvas, "RightSidebar/TileContentPanel/TilePreviewPanel", inset);
        Set(canvas, "RightSidebar/BuildGuidance", inset);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("[ChromeRestyle] Panel frames applied.");
    }

    /// <summary>Canvas units from the button rect's bottom edge to its face (outline + lip).</summary>
    public const float ButtonLipInset = 12f;
    /// <summary>Canvas units from the button rect's top edge to its face (outline).</summary>
    public const float ButtonTopInset = 5f;

    /// <summary>
    /// Centres every button label in the open scenes on the button's face. The
    /// face is not the middle of the rect: the lip takes the bottom 12 units.
    /// </summary>
    private static int CentreButtonLabels(Sprite button)
    {
        int count = 0;
        foreach (var image in Resources.FindObjectsOfTypeAll<Image>())
        {
            if (button == null || image.sprite != button || !image.gameObject.scene.IsValid()) continue;
            foreach (var label in image.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                var rt = label.rectTransform;
                Undo.RecordObject(rt, "Apply Panel Frames");
                Undo.RecordObject(label, "Apply Panel Frames");
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(10, ButtonLipInset);
                rt.offsetMax = new Vector2(-10, -ButtonTopInset);
                label.alignment = TMPro.TextAlignmentOptions.Center;
                label.margin = Vector4.zero;
                EditorUtility.SetDirty(label);
                EditorUtility.SetDirty(rt);
                count++;
            }
        }
        return count;
    }

    /// <summary>Border is left, bottom, right, top in source pixels.</summary>
    private static Sprite Slice(string name, Vector4 border, float pixelsPerUnit = 200)
    {
        string path = Panels + name + ".png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[ChromeRestyle] Missing sprite: {path}");
            return null;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.spriteBorder = border;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void Set(Transform root, string path, Sprite sprite)
    {
        var t = root.Find(path);
        var image = t != null ? t.GetComponent<Image>() : null;
        if (image == null || sprite == null)
        {
            Debug.LogWarning($"[ChromeRestyle] No image at '{path}' - skipped.");
            return;
        }

        Undo.RecordObject(image, "Apply Panel Frames");
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        EditorUtility.SetDirty(image);
    }
}
