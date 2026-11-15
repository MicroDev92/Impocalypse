using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Makes a sidebar row a board-focus target: tapping it lights up its tiles
    /// (see <see cref="BoardFocus"/>), and the row is tinted while it is the
    /// active focus. Rows are rebuilt often, so each one carries its own key and
    /// looks up its own state instead of the list tracking which row is "on".
    /// </summary>
    public class FocusRow : MonoBehaviour
    {
        private static readonly Color ActiveTint = new(1f, 0.83f, 0.42f, 0.20f);

        private string _key;
        private Image _background;

        public static void Attach(RectTransform row, string key, Func<IEnumerable<TileData>> tiles)
        {
            if (row == null || string.IsNullOrEmpty(key) || tiles == null) return;

            // Invisible until active, but always a raycast target so the row can be tapped.
            var background = row.gameObject.AddComponent<Image>();
            background.color = Color.clear;

            var button = row.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => BoardFocus.Toggle(key, tiles));

            var focus = row.gameObject.AddComponent<FocusRow>();
            focus._key = key;
            focus._background = background;
            focus.Refresh();
        }

        private void OnEnable() => BoardFocus.Changed += Refresh;
        private void OnDisable() => BoardFocus.Changed -= Refresh;

        private void Refresh()
        {
            if (_background != null)
                _background.color = BoardFocus.IsActive(_key) ? ActiveTint : Color.clear;
        }
    }
}
