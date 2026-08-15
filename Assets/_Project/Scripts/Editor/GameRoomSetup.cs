using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor menu item that adds the Leave button to the open GameRoom scene.
/// Run via: GameObject > UI > Impocalypse - Add GameRoom Leave Button
///
/// Deliberately narrow: it adds the one control the lobby was missing and wires it,
/// and touches nothing that is already there. Restyling GameRoom is Milestone D7.
/// </summary>
public static class GameRoomSetup
{
    private static readonly Color TextPrimary = new(1f, 0.92f, 0.82f, 1f);
    private static readonly Color ButtonPrimary = new(0.85f, 0.35f, 0.08f, 1f);

    [MenuItem("GameObject/UI/Impocalypse - Add GameRoom Leave Button", false, 29)]
    private static void AddLeaveButton()
    {
        var ui = Object.FindFirstObjectByType<GameRoomUI>();
        if (ui == null)
        {
            Debug.LogError("[GameRoomSetup] No GameRoomUI in the open scene. Open GameRoom first.");
            return;
        }

        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[GameRoomSetup] No Canvas in the open scene.");
            return;
        }

        var previous = canvas.transform.Find("LeaveButton");
        if (previous != null)
        {
            Undo.DestroyObjectImmediate(previous.gameObject);
            Debug.Log("[GameRoomSetup] Replaced the existing Leave button.");
        }

        // Top-left, away from Start: leaving and starting are opposite intentions and
        // should not sit next to each other where a misclick ends someone's game.
        var leave = CreateButton(canvas.transform, "LeaveButton", "LEAVE", 180f, 52f, 18);
        var rect = leave.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(30, -30);

        var so = new SerializedObject(ui);
        so.FindProperty("leaveButton").objectReferenceValue = leave.GetComponent<Button>();
        so.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(leave, "Add Leave Button");
        Selection.activeGameObject = leave;
        Debug.Log("[GameRoomSetup] Leave button added and wired.");
    }

    private static GameObject CreateButton(Transform parent, string name, string text,
        float width, float height, int fontSize)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
        obj.GetComponent<Image>().color = ButtonPrimary;
        UITheme.ApplyButton(obj);

        var colors = obj.GetComponent<Button>().colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1, 1, 1, 0.9f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.disabledColor = new Color(0.45f, 0.42f, 0.40f, 1f);
        obj.GetComponent<Button>().colors = colors;

        var labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(obj.transform, false);

        var tmp = labelObj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = TextPrimary;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        UITheme.ApplyFont(tmp, fontSize);

        var labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        // The button sprite's horns sit at the top, so the visible bar is the lower
        // part of the rect - centring in the raw rect would put the text on the horns.
        labelRect.offsetMin = new Vector2(10, 6);
        labelRect.offsetMax = new Vector2(-10, -18);

        return obj;
    }
}
