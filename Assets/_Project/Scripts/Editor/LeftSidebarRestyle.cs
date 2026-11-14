using TMPro;
using UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Turns the left sidebar's pact and domain lists into icon rows, as a
/// re-runnable command. Run via: GameObject > UI > Impocalypse - Restyle Left Sidebar
///
/// It does three things, all idempotent:
///  - imports the ArtGen icons as sprites and (re)fills Resources/UIIconSet;
///  - restyles the two panel headers and empty-state texts to match the right
///    sidebar (display font, readable sizes);
///  - adds the row container the pact list needs and wires both list scripts.
/// </summary>
public static class LeftSidebarRestyle
{
    private const string IconFolder = "Assets/Art/UI/Icons/";
    private const string IconSetPath = "Assets/Resources/UIIconSet.asset";
    private const string DisplayFontPath = "Assets/Art/UI/Fonts/LuckiestGuy SDF.asset";
    private const float HeaderHeight = 30f;
    private const float ListTop = 44f;

    [MenuItem("GameObject/UI/Impocalypse - Restyle Left Sidebar", false, 32)]
    public static void Restyle()
    {
        var display = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DisplayFontPath);
        BuildIconSet(display);

        Transform sidebar = null;
        foreach (var rt in Resources.FindObjectsOfTypeAll<RectTransform>())
        {
            if (rt.name == "LeftSidebar" && rt.gameObject.scene.IsValid()) { sidebar = rt; break; }
        }
        if (sidebar == null)
        {
            Debug.LogError("[LeftSidebarRestyle] No LeftSidebar in the open scene. Open GameScene first.");
            return;
        }

        PlayerList(sidebar);

        // ---- Pacts ----
        Header(sidebar, "PactsPanel/PactsHeader", display);
        var pactsEmpty = Body(sidebar, "PactsPanel/PactsList");
        var pactsPanel = sidebar.Find("PactsPanel");
        if (pactsPanel != null)
        {
            var rows = RowContainer(pactsPanel, "PactsRows");
            Scrollable(pactsPanel, rows);
            var pacts = pactsPanel.GetComponent<ParchmentHandUI>();
            if (pacts != null)
            {
                var so = new SerializedObject(pacts);
                so.FindProperty("rowContainer").objectReferenceValue = rows;
                if (pactsEmpty != null) so.FindProperty("listText").objectReferenceValue = pactsEmpty;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(pacts);
            }
        }

        // ---- Domains ----
        Header(sidebar, "DomainsPanel/DomainsHeader", display);
        Body(sidebar, "DomainsPanel/DomainsEmpty");
        var domainsPanel = sidebar.Find("DomainsPanel");
        if (domainsPanel != null)
        {
            // The original DomainsList belongs to the sidebar prefab, and a prefab
            // child cannot be moved under a new parent in the scene. So the rows get
            // a fresh container inside the scroll viewport, and the old one is retired.
            var rows = RowContainer(domainsPanel, "DomainsRows");
            Scrollable(domainsPanel, rows);

            var old = domainsPanel.Find("DomainsList");
            if (old != null)
            {
                Undo.RecordObject(old.gameObject, "Restyle Left Sidebar");
                old.gameObject.SetActive(false);
            }

            var domains = domainsPanel.GetComponent<DomainListUI>();
            if (domains != null)
            {
                var so = new SerializedObject(domains);
                so.FindProperty("listContainer").objectReferenceValue = rows;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(domains);
            }
        }

        EditorSceneManager.MarkSceneDirty(sidebar.gameObject.scene);
        Debug.Log("[LeftSidebarRestyle] Left sidebar restyled.");
    }

    private const string PlayerCardPrefab = "Assets/_Project/Prefabs/PlayerCard.prefab";
    private static readonly string[] PortraitSlots = { "red", "teal", "yellow", "purple" };

    /// <summary>
    /// Player list: portraits into the palette, the card prefab stripped to its
    /// root (PlayerCardUI builds the rest), and the list sized by its cards.
    /// </summary>
    private static void PlayerList(Transform sidebar)
    {
        var portraits = new Sprite[PortraitSlots.Length];
        for (int i = 0; i < PortraitSlots.Length; i++)
            portraits[i] = Portrait("Assets/Art/Imps/portrait_" + PortraitSlots[i] + ".png");

        foreach (var guid in AssetDatabase.FindAssets("t:PlayerColorPalette"))
        {
            var palette = AssetDatabase.LoadAssetAtPath<Config.PlayerColorPalette>(AssetDatabase.GUIDToAssetPath(guid));
            var so = new SerializedObject(palette);
            var list = so.FindProperty("playerColors");
            for (int i = 0; i < Mathf.Min(list.arraySize, portraits.Length); i++)
                list.GetArrayElementAtIndex(i).FindPropertyRelative("portrait").objectReferenceValue = portraits[i];
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(palette);
        }

        var prefab = PrefabUtility.LoadPrefabContents(PlayerCardPrefab);
        for (int i = prefab.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(prefab.transform.GetChild(i).gameObject);
        var layoutGroup = prefab.GetComponent<LayoutGroup>();
        if (layoutGroup != null) Object.DestroyImmediate(layoutGroup);
        var element = prefab.GetComponent<LayoutElement>();
        if (element == null) element = prefab.AddComponent<LayoutElement>();
        element.minHeight = element.preferredHeight = PlayerCardUI.Height;
        element.flexibleWidth = 1f;
        var background = prefab.GetComponent<Image>();
        if (background != null)
        {
            background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Panels/panel_inset.png");
            background.type = Image.Type.Sliced;
            background.color = Color.white;
        }
        PrefabUtility.SaveAsPrefabAsset(prefab, PlayerCardPrefab);
        PrefabUtility.UnloadPrefabContents(prefab);

        var container = sidebar.Find("PlayerListContainer");
        if (container == null) return;
        ConfigureRows(container.gameObject);
        var listElement = container.GetComponent<LayoutElement>();
        if (listElement != null)
        {
            Undo.RecordObject(listElement, "Restyle Left Sidebar");
            // -1 on both: the list is exactly as tall as its cards, and - because the
            // cards' own minimum then counts - it is never squeezed when the pact and
            // domain lists below it run long. Those scroll; the players do not.
            listElement.minHeight = -1f;
            listElement.preferredHeight = -1f;
            EditorUtility.SetDirty(listElement);
        }
    }

    private static Sprite Portrait(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[LeftSidebarRestyle] Missing portrait: {path}");
            return null;
        }
        if (importer.textureType != TextureImporterType.Sprite || !importer.mipmapEnabled)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void BuildIconSet(TMP_FontAsset display)
    {
        var set = AssetDatabase.LoadAssetAtPath<UIIconSet>(IconSetPath);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<UIIconSet>();
            AssetDatabase.CreateAsset(set, IconSetPath);
        }

        set.charwood = Icon("charwood");
        set.bloodroot = Icon("bloodroot");
        set.souls = Icon("souls");
        set.demonsilk = Icon("demonsilk");
        set.soulwine = Icon("soulwine");
        set.ectoplasm = Icon("ectoplasm");
        set.anyResource = Icon("any");
        set.strength = Icon("strength");
        set.wealth = Icon("wealth");
        set.infamy = Icon("infamy");
        set.tile = Icon("tile");
        set.domain = Icon("domain");
        set.pact = Icon("pact");
        set.treasure = Icon("treasure");
        set.displayFont = display;
        set.panelInset = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Panels/panel_inset.png");

        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssets();
    }

    /// <summary>Loads an icon, first making sure it is imported as a mipmapped sprite (it is shown at ~1/9 size).</summary>
    private static Sprite Icon(string name)
    {
        string path = IconFolder + "icon_" + name + ".png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[LeftSidebarRestyle] Missing icon: {path}");
            return null;
        }

        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single ||
            !importer.mipmapEnabled || importer.filterMode != FilterMode.Bilinear)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void Header(Transform root, string path, TMP_FontAsset display)
    {
        var tmp = Find(root, path);
        if (tmp == null) return;

        Undo.RecordObject(tmp, "Restyle Left Sidebar");
        if (display != null) tmp.font = display;
        tmp.fontSize = 22;
        tmp.fontStyle = FontStyles.Normal;
        tmp.color = IconRow.Accent;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;

        var rt = tmp.rectTransform;
        Undo.RecordObject(rt, "Restyle Left Sidebar");
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, HeaderHeight);
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -8f);
        EditorUtility.SetDirty(tmp);
    }

    /// <summary>The "nothing here yet" line under a header.</summary>
    private static TMP_Text Body(Transform root, string path)
    {
        var tmp = Find(root, path);
        if (tmp == null) return null;

        Undo.RecordObject(tmp, "Restyle Left Sidebar");
        tmp.fontSize = 16;
        tmp.fontStyle = FontStyles.Italic;
        tmp.color = IconRow.Soft;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.raycastTarget = false;

        var rt = tmp.rectTransform;
        Undo.RecordObject(rt, "Restyle Left Sidebar");
        rt.offsetMax = new Vector2(rt.offsetMax.x, -ListTop);
        EditorUtility.SetDirty(tmp);
        return tmp;
    }

    /// <summary>
    /// Puts a row container inside a masked, vertically scrolling viewport that
    /// fills the panel under its header. The rows keep their identity, so the list
    /// scripts' references stay valid; a long list scrolls (wheel or drag) instead
    /// of running out of the panel.
    /// </summary>
    private static void Scrollable(Transform panel, Transform rows)
    {
        var viewport = panel.Find("Viewport") as RectTransform;
        if (viewport == null)
        {
            var go = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            Undo.RegisterCreatedObjectUndo(go, "Restyle Left Sidebar");
            viewport = (RectTransform)go.transform;
            viewport.SetParent(panel, false);
        }
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(8, 8);
        viewport.offsetMax = new Vector2(-8, -ListTop);

        // Invisible, but it has to be a raycast target or a drag has nothing to land on.
        var hit = viewport.GetComponent<Image>();
        hit.color = Color.clear;
        hit.raycastTarget = true;

        var content = (RectTransform)rows;
        if (content.parent != viewport) Undo.SetTransformParent(content, viewport, "Restyle Left Sidebar");
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = new Vector2(0f, content.offsetMin.y);
        content.offsetMax = new Vector2(0f, 0f);
        content.anchoredPosition = Vector2.zero;
        content.localScale = Vector3.one;

        var fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = Undo.AddComponent<ContentSizeFitter>(content.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = viewport.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        scroll.inertia = true;
        EditorUtility.SetDirty(scroll);
    }

    private static Transform RowContainer(Transform panel, string name)
    {
        var rt = (panel.Find(name) ?? panel.Find("Viewport/" + name)) as RectTransform;
        if (rt == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(go, "Restyle Left Sidebar");
            rt = (RectTransform)go.transform;
            rt.SetParent(panel, false);
        }
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(8, 8);
        rt.offsetMax = new Vector2(-8, -ListTop);
        ConfigureRows(rt.gameObject);
        return rt;
    }

    private static void ConfigureRows(GameObject container)
    {
        var layout = container.GetComponent<VerticalLayoutGroup>();
        if (layout == null) layout = Undo.AddComponent<VerticalLayoutGroup>(container);
        Undo.RecordObject(layout, "Restyle Left Sidebar");
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = 4;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        EditorUtility.SetDirty(layout);
    }

    private static TMP_Text Find(Transform root, string path)
    {
        var t = root.Find(path);
        var tmp = t != null ? t.GetComponent<TMP_Text>() : null;
        if (tmp == null) Debug.LogWarning($"[LeftSidebarRestyle] No text at '{path}' - skipped.");
        return tmp;
    }
}
