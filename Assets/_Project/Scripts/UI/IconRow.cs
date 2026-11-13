using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Builds one sidebar line out of icons and short numbers, left to right.
    /// Rows are made in code, not from a prefab: a row is nothing but a layout
    /// group, and the two lists that use it need different contents every time.
    /// </summary>
    public static class IconRow
    {
        public static readonly Color Bright = new(0.96f, 0.91f, 0.83f, 1f);
        public static readonly Color Soft = new(0.84f, 0.72f, 0.61f, 1f);
        public static readonly Color Accent = new(0.95f, 0.55f, 0.15f, 1f);

        public const float Height = 36f;
        public const float IconSize = 32f;
        public const float NumberSize = 24f;
        public const float WordSize = 16f;

        public static RectTransform Create(Transform parent, float height = Height, float spacing = 5f)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);

            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var element = go.GetComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            return rt;
        }

        /// <summary>Adds an icon. A missing sprite adds nothing, so rows degrade to their numbers.</summary>
        public static Image Icon(Transform row, Sprite sprite, float size = IconSize)
        {
            if (sprite == null) return null;

            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(row, false);

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            var element = go.GetComponent<LayoutElement>();
            element.minWidth = element.preferredWidth = size;
            element.minHeight = element.preferredHeight = size;
            return image;
        }

        /// <summary>A number or short word in the display font.</summary>
        public static TMP_Text Number(Transform row, string text, Color color, float size = NumberSize)
            => Label(row, text, color, size, true);

        /// <summary>A small connecting word ("each") in the plain font.</summary>
        public static TMP_Text Word(Transform row, string text, float size = WordSize)
            => Label(row, text, Soft, size, false);

        private static TMP_Text Label(Transform row, string text, Color color, float size, bool display)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(row, false);

            var label = go.GetComponent<TextMeshProUGUI>();
            var icons = UIIconSet.Instance;
            if (display && icons != null && icons.displayFont != null) label.font = icons.displayFont;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }
    }
}
