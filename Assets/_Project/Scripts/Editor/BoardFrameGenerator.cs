using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates the static coordinate frame around the board: one stone slab per
/// border cell, with the A-J / 1-10 labels as TextMeshPro text in the display
/// font and a skull in each corner.
///
/// The labels used to be twenty separate PNGs. As text they stay sharp at any
/// zoom, match the rest of the UI lettering, and cost no texture memory.
///
/// The frame is 100% static decoration, so it does NOT belong in the runtime tile
/// pipeline: no TileData, no colliders, no per-run regeneration. This command
/// replaces the "BoardFrame" object in the open scene and re-saves its prefab.
/// Re-run it only if the board dimensions or frame art change.
///
/// Positions match GridGenerator exactly: world (x, y, 0), border cells of the
/// gridDimensions rectangle. The frame must stay at the scene root - GridGenerator
/// destroys all children of the Grid object every generation.
/// </summary>
public static class BoardFrameGenerator
{
    private const string RootName = "BoardFrame";
    private const string PrefabPath = "Assets/_Project/Prefabs/BoardFrame.prefab";
    private const string SlabPath = "Assets/Art/UI/Grid/frame_slab.png";
    private const string SkullPath = "Assets/Art/UI/Grid/frame_skull.png";
    private const string FontPath = "Assets/Art/UI/Fonts/LuckiestGuy SDF.asset";

    private const float SkullCellFraction = 0.72f;
    private const float LabelFontSize = 6.2f;    // world-space TMP: about 0.45 units of cap height
    private const float LabelNudgeY = -0.03f;    // the font's capitals sit a touch high in their line

    private static readonly Color LabelTop = new(1f, 0.83f, 0.42f, 1f);
    private static readonly Color LabelBottom = new(1f, 0.54f, 0.16f, 1f);

    [MenuItem("Tools/Impocalypse/Generate Board Frame")]
    public static void Generate()
    {
        var grid = Object.FindFirstObjectByType<GridGenerator>();
        if (grid == null)
        {
            Debug.LogError("[BoardFrame] No GridGenerator found in the open scene. Open GameScene first.");
            return;
        }

        var slab = ImportTile(SlabPath);
        var skull = ImportTile(SkullPath);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (slab == null || font == null)
        {
            Debug.LogError("[BoardFrame] Missing frame_slab.png or the display font - nothing generated.");
            return;
        }

        // Replace any existing frame so the command is safely re-runnable.
        var existing = GameObject.Find(RootName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Generate Board Frame");

        int dimX = grid.gridDimensions.x;
        int dimY = grid.gridDimensions.y;

        for (int x = 0; x < dimX; x++)
        {
            for (int y = 0; y < dimY; y++)
            {
                bool edgeX = x == 0 || x == dimX - 1;
                bool edgeY = y == 0 || y == dimY - 1;
                if (!edgeX && !edgeY) continue; // playable cell - GridGenerator's job

                Transform cell;
                if (edgeX && edgeY)
                {
                    cell = CreateCell(root.transform, $"Corner_{x}_{y}", x, y, slab);
                    if (skull != null) AddSprite(cell, "Skull", skull, SkullCellFraction, 1);
                }
                else if (edgeX)
                {
                    // A on the top row, counting down the board.
                    char letter = (char)('A' + (dimY - 2 - y));
                    cell = CreateCell(root.transform, $"Letter_{x}_{y}", x, y, slab);
                    AddLabel(cell, letter.ToString(), font);
                }
                else
                {
                    cell = CreateCell(root.transform, $"Number_{x}_{y}", x, y, slab);
                    AddLabel(cell, x.ToString(), font);
                }
            }
        }

        PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log($"[BoardFrame] Generated {root.transform.childCount} frame cells and saved {PrefabPath}.");
    }

    /// <summary>Frame art is authored like a terrain tile: 1024 px = one world unit.</summary>
    private static Sprite ImportTile(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return null;

        if (importer.textureType != TextureImporterType.Sprite || importer.spritePixelsPerUnit != 1024 || !importer.mipmapEnabled)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 1024;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Transform CreateCell(Transform parent, string name, int x, int y, Sprite slab)
    {
        var cell = new GameObject(name).transform;
        cell.SetParent(parent, false);
        cell.position = new Vector3(x, y, 0f);
        AddSprite(cell, "Slab", slab, 1f, 0);
        return cell;
    }

    /// <summary>A sprite scaled to a fraction of the cell, whatever its pixel size.</summary>
    private static void AddSprite(Transform cell, string name, Sprite sprite, float cellFraction, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(cell, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;

        var size = sprite.bounds.size;
        if (size.x > 0f && size.y > 0f)
        {
            float k = cellFraction / Mathf.Max(size.x, size.y);
            go.transform.localScale = new Vector3(k, k, 1f);
        }
    }

    private static void AddLabel(Transform cell, string text, TMP_FontAsset font)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(cell, false);
        go.transform.localPosition = new Vector3(0f, LabelNudgeY, 0f);

        var label = go.AddComponent<TextMeshPro>();
        label.rectTransform.sizeDelta = Vector2.one;
        label.font = font;
        label.text = text;
        label.fontSize = LabelFontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.color = Color.white;
        label.enableVertexGradient = true;
        label.colorGradient = new VertexGradient(LabelTop, LabelTop, LabelBottom, LabelBottom);
        label.sortingOrder = 1;
    }
}
