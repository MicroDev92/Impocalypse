using UnityEditor;
using UnityEngine;

/// <summary>
/// One-time scaffolding (TopBar workflow): generates the static coordinate frame
/// (coal slabs + letter/number/skull glyphs) as plain SpriteRenderer objects.
///
/// The frame is 100% static decoration, so it does NOT belong in the runtime tile
/// pipeline: no TileData, no colliders, no per-run regeneration. Run this command
/// once, prefab-ize the resulting "BoardFrame" object, and keep the instance in
/// the scene. Re-run only if the board dimensions or frame art change.
///
/// Positions match GridGenerator exactly: world (x, y, 0), border cells of the
/// gridDimensions rectangle. The frame must stay at the scene root - GridGenerator
/// destroys all children of the Grid object every generation.
/// </summary>
public static class BoardFrameGenerator
{
    private const string RootName = "BoardFrame";
    private const float GlyphCellFraction = 0.7f; // matches TileData.labelCellFraction

    [MenuItem("Tools/Impocalypse/Generate Board Frame")]
    public static void Generate()
    {
        var grid = Object.FindFirstObjectByType<GridGenerator>();
        if (grid == null)
        {
            Debug.LogError("[BoardFrame] No GridGenerator found in the open scene. Open GameScene first.");
            return;
        }

        if (grid.coordTileBackground == null)
        {
            Debug.LogError("[BoardFrame] GridGenerator has no Coord Tile Background sprite assigned.");
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

        // Same letter ordering as the old runtime path: A at the top row.
        var letters = grid.letterSprites;

        for (int x = 0; x < dimX; x++)
        {
            for (int y = 0; y < dimY; y++)
            {
                bool edgeX = x == 0 || x == dimX - 1;
                bool edgeY = y == 0 || y == dimY - 1;
                if (!edgeX && !edgeY) continue; // playable cell - GridGenerator's job

                Sprite glyph = null;
                string name;

                if (edgeX && edgeY)
                {
                    glyph = grid.cornerSkullSprite;
                    name = $"Corner_{x}_{y}";
                }
                else if (edgeX)
                {
                    int letterIndex = letters.Length - y; // reversed: A on top
                    if (letterIndex >= 0 && letterIndex < letters.Length)
                        glyph = letters[letterIndex];
                    name = $"Letter_{x}_{y}";
                }
                else
                {
                    int numberIndex = x - 1;
                    if (numberIndex >= 0 && numberIndex < grid.numberSprites.Length)
                        glyph = grid.numberSprites[numberIndex];
                    name = $"Number_{x}_{y}";
                }

                CreateFrameCell(root.transform, name, new Vector3(x, y, 0f),
                    grid.coordTileBackground, glyph);
            }
        }

        Selection.activeGameObject = root;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);

        Debug.Log($"[BoardFrame] Generated {root.transform.childCount} frame cells. " +
                  "Now drag 'BoardFrame' into Assets/_Project/Prefabs to prefab-ize it, and save the scene.");
    }

    private static void CreateFrameCell(Transform parent, string name, Vector3 position,
        Sprite slabSprite, Sprite glyphSprite)
    {
        var cell = new GameObject(name);
        cell.transform.SetParent(parent, false);
        cell.transform.position = position;

        // Slab, normalized to exactly one grid cell (same rule as TileData.FitToCell).
        var slab = new GameObject("Slab");
        slab.transform.SetParent(cell.transform, false);
        var slabRenderer = slab.AddComponent<SpriteRenderer>();
        slabRenderer.sprite = slabSprite;
        slabRenderer.sortingOrder = 0;

        var slabSize = slabSprite.bounds.size;
        if (slabSize.x > 0f && slabSize.y > 0f)
            slab.transform.localScale = new Vector3(1f / slabSize.x, 1f / slabSize.y, 1f);

        // Glyph, normalized to a fraction of the cell, drawn above the slab.
        if (glyphSprite != null)
        {
            var glyphObj = new GameObject("Glyph");
            glyphObj.transform.SetParent(cell.transform, false);
            var glyphRenderer = glyphObj.AddComponent<SpriteRenderer>();
            glyphRenderer.sprite = glyphSprite;
            glyphRenderer.sortingOrder = 1;

            var glyphSize = glyphSprite.bounds.size;
            if (glyphSize.x > 0f && glyphSize.y > 0f)
            {
                float k = GlyphCellFraction / Mathf.Max(glyphSize.x, glyphSize.y);
                glyphObj.transform.localScale = new Vector3(k, k, 1f);
            }
        }
    }
}
