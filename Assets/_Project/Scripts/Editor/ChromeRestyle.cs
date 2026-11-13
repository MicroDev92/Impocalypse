using Config;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using _Project.Scripts.Visuals;

/// <summary>
/// Applies the ArtGen panel frames and creature sets, as re-runnable commands.
///
///  GameObject > UI > Impocalypse - Apply Panel Frames
///      Fixes the 9-slice import settings of the four chrome sprites (they are
///      authored at 2x, so PPU 200) and points the inner panels and the top bar
///      of the open scene at them. Sidebars and buttons already reference
///      panel_bg / button_bg, which were replaced in place.
///
///  Impocalypse > Creatures > Use Drawn Set / Use Generated Set
///      Swaps which four creature sprites the player palette hands out, so the
///      two candidate sets can be compared in the running game.
/// </summary>
public static class ChromeRestyle
{
    private const string Panels = "Assets/Art/UI/Panels/";

    [MenuItem("GameObject/UI/Impocalypse - Apply Panel Frames", false, 33)]
    public static void Apply()
    {
        // The sidebar frame is shown a little larger than authored, so its band stays chunky.
        Slice("panel_bg", new Vector4(80, 80, 80, 80), 140);
        Slice("button_bg", new Vector4(48, 48, 48, 48));
        var inset = Slice("panel_inset", new Vector4(40, 40, 40, 40));
        var bar = Slice("topbar_bg", new Vector4(8, 30, 8, 0));

        Transform canvas = null;
        foreach (var rt in Resources.FindObjectsOfTypeAll<RectTransform>())
        {
            if (rt.name == "LeftSidebar" && rt.gameObject.scene.IsValid()) { canvas = rt.parent; break; }
        }
        if (canvas == null)
        {
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

    // ==========================================
    // CREATURES
    // ==========================================

    private static readonly string[] Slots = { "red", "teal", "yellow", "purple" };

    [MenuItem("Impocalypse/Creatures/Use Drawn Set")]
    public static void UseDrawn() => UseCreatures("Assets/Art/Imps/Drawn/");

    [MenuItem("Impocalypse/Creatures/Use Generated Set")]
    public static void UseGenerated() => UseCreatures("Assets/Art/Imps/");

    private static void UseCreatures(string folder)
    {
        var sprites = new Sprite[Slots.Length];
        for (int i = 0; i < Slots.Length; i++)
        {
            string path = folder + "imp_" + Slots[i] + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[ChromeRestyle] Missing creature: {path}");
                return;
            }
            // Full 1024 canvas = one world unit, centre pivot: see ASSET_BRIEF.md.
            if (importer.textureType != TextureImporterType.Sprite || importer.spritePixelsPerUnit != 1024 || !importer.mipmapEnabled)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 1024;
                importer.mipmapEnabled = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.alphaIsTransparency = true;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        foreach (var guid in AssetDatabase.FindAssets("t:PlayerColorPalette"))
        {
            var palette = AssetDatabase.LoadAssetAtPath<PlayerColorPalette>(AssetDatabase.GUIDToAssetPath(guid));
            var so = new SerializedObject(palette);
            var list = so.FindProperty("playerColors");
            for (int i = 0; i < Mathf.Min(list.arraySize, sprites.Length); i++)
                list.GetArrayElementAtIndex(i).FindPropertyRelative("impSprite").objectReferenceValue = sprites[i];
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(palette);
        }

        // The legacy lookup wins over the palette when present, so keep it in step.
        foreach (var visuals in Object.FindObjectsByType<ImpVisuals>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(visuals, "Swap Creatures");
            visuals.redImp = sprites[0];
            visuals.greenImp = sprites[1];
            visuals.blueImp = sprites[2];
            visuals.purpleImp = sprites[3];
            EditorUtility.SetDirty(visuals);
            EditorSceneManager.MarkSceneDirty(visuals.gameObject.scene);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[ChromeRestyle] Creatures now come from {folder}");
    }
}
