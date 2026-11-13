using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Applies the ArtGen buildings, card art, card frame and background, as a
/// re-runnable command. Run via: GameObject > UI > Impocalypse - Apply Board Art
///
///  - Buildings: every reference to a sub-sprite of the old building_markers
///    sheet (in the open scene and in Card.prefab) is re-pointed at the new
///    single sprite of the same name, so nothing has to be re-wired by hand.
///  - Card art: the empty pact / provisions slots of every CardArtSet are filled.
///  - Card frame and background textures were replaced in place; this only fixes
///    their import settings.
///  - Background: a tiled rock texture and a soft glow behind the board, as plain
///    world sprites under a "Backdrop" root.
/// </summary>
public static class BoardArtApply
{
    private const string Buildings = "Assets/Art/UI/Buildings/";
    private const string Cards = "Assets/Art/UI/Cards/";
    private const string OldSheet = Buildings + "building_markers.png";
    private const string CardPrefab = "Assets/_Project/Prefabs/Card.prefab";
    private const string Texture = "Assets/Art/UI/Background/bg_texture_tileable.png";
    private const string Glow = "Assets/Art/UI/Background/bg_glow.png";
    private const float CardContentScale = 1.25f;

    [MenuItem("GameObject/UI/Impocalypse - Apply Board Art", false, 34)]
    public static void Apply()
    {
        var art = new Dictionary<string, Sprite>();
        foreach (var name in new[] { "citadel", "farm", "bazaar", "warcamp", "hellgate", "lair" })
            art[name] = Import(Buildings + "building_" + name + ".png", 1024, true);
        art["pact"] = Import(Cards + "card_pact.png", 1024, true);
        art["provisions"] = Import(Cards + "card_provisions.png", 1024, true);

        // Shown at a quarter of its authored size, so it needs mipmaps to stay clean.
        Import("Assets/Art/UI/card_frame.png", 400, true);
        var texture = Import(Texture, 128, false, repeat: true);
        var glow = Import(Glow, 100, false);

        int changed = 0;
        foreach (var component in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            changed += Repoint(component, art);

        var prefab = PrefabUtility.LoadPrefabContents(CardPrefab);
        int inPrefab = 0;
        foreach (var component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            inPrefab += Repoint(component, art);
        // The new frame is thinner than the old one, so the parchment is larger:
        // grow the content with it instead of leaving it marooned in the middle.
        var content = prefab.transform.Find("Content");
        if (content != null && !Mathf.Approximately(content.localScale.x, CardContentScale))
        {
            content.localScale = Vector3.one * CardContentScale;
            inPrefab++;
        }
        if (inPrefab > 0) PrefabUtility.SaveAsPrefabAsset(prefab, CardPrefab);
        PrefabUtility.UnloadPrefabContents(prefab);

        BuildBackdrop(texture, glow);

        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"[BoardArtApply] Re-pointed {changed} scene and {inPrefab} card prefab sprite references.");
    }

    private static Sprite Import(string path, float pixelsPerUnit, bool mipmaps, bool repeat = false)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[BoardArtApply] Missing art: {path}");
            return null;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.mipmapEnabled = mipmaps;
        importer.filterMode = FilterMode.Bilinear;
        importer.alphaIsTransparency = true;
        importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    /// <summary>
    /// Walks every serialized sprite reference on a component. One that points
    /// into the old sheet is swapped for the new sprite of the same name; an empty
    /// "pact" or "provisions" slot is filled.
    /// </summary>
    private static int Repoint(MonoBehaviour component, Dictionary<string, Sprite> art)
    {
        if (component == null) return 0;

        int count = 0;
        var so = new SerializedObject(component);
        var prop = so.GetIterator();
        while (prop.NextVisible(true))
        {
            if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;

            var current = prop.objectReferenceValue as Sprite;
            Sprite replacement = null;
            if (current != null && AssetDatabase.GetAssetPath(current) == OldSheet)
                art.TryGetValue(current.name, out replacement);
            else if (prop.objectReferenceValue == null && (prop.name == "pact" || prop.name == "provisions") && prop.type.Contains("Sprite"))
                art.TryGetValue(prop.name, out replacement);

            if (replacement == null || replacement == current) continue;
            prop.objectReferenceValue = replacement;
            count++;
        }

        if (count > 0)
        {
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(component);
        }
        return count;
    }

    private static void BuildBackdrop(Sprite texture, Sprite glow)
    {
        var grid = Object.FindFirstObjectByType<GridGenerator>();
        if (grid == null || texture == null) return;

        // Centre on the coordinate frame if it exists, else on the grid object.
        var centre = grid.transform.position;
        var frame = GameObject.Find("BoardFrame");
        if (frame != null)
        {
            var renderers = frame.GetComponentsInChildren<SpriteRenderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                centre = bounds.center;
            }
        }

        var root = GameObject.Find("Backdrop");
        if (root == null)
        {
            root = new GameObject("Backdrop");
            Undo.RegisterCreatedObjectUndo(root, "Apply Board Art");
        }
        root.transform.position = new Vector3(centre.x, centre.y, 5f);

        var rock = Child(root.transform, "Rock");
        rock.sprite = texture;
        rock.drawMode = SpriteDrawMode.Tiled;
        rock.tileMode = SpriteTileMode.Continuous;
        rock.size = new Vector2(96f, 64f);   // far past anything the camera can pan to
        rock.sortingOrder = -1000;
        rock.color = Color.white;

        var halo = Child(root.transform, "Glow");
        halo.sprite = glow;
        halo.drawMode = SpriteDrawMode.Simple;
        halo.sortingOrder = -999;
        halo.color = new Color(1f, 1f, 1f, 0.35f);
        halo.transform.localScale = Vector3.one * 5.4f;   // 5.12 units * 5.4: a soft halo about twice the board
    }

    private static SpriteRenderer Child(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t == null)
        {
            t = new GameObject(name, typeof(SpriteRenderer)).transform;
            t.SetParent(parent, false);
        }
        t.localPosition = Vector3.zero;
        return t.GetComponent<SpriteRenderer>();
    }
}
