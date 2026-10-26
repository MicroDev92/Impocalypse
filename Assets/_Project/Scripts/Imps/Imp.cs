using UnityEngine;

namespace Imps
{
    /// <summary>
    /// Represents an imp entity on the grid.
    /// </summary>
    public class Imp : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Grounding")]
        [Tooltip("Size of the contact shadow in world units. Without one an imp reads " +
                 "as a sticker laid over the board - nothing says it sits ON a tile.")]
        [SerializeField] private Vector2 shadowSize = new(0.70f, 0.22f);

        [Tooltip("How far below the imp's origin the shadow sits. The imp sprite is " +
                 "0.78 tall and centred, so its feet are near -0.39; a shadow much above " +
                 "that is simply hidden behind the imp's own body.")]
        [SerializeField] private float shadowOffsetY = -0.38f;

        [Tooltip("Opacity of the shadow once the imp has landed.")]
        [Range(0f, 1f)]
        [SerializeField] private float shadowOpacity = 0.40f;

        [Tooltip("Height at which the shadow is widest and faintest. Matches " +
                 "ImpSpawner.dropHeight so a falling imp's shadow tightens as it nears.")]
        [SerializeField] private float shadowSpreadHeight = 2.2f;

        private SpriteRenderer _shadow;
        private static Sprite _shadowSprite;

        private string _coordinate;
        private int _ownerActorNumber;
        private string _colorId;

        public string Coordinate => _coordinate;
        public int OwnerActorNumber => _ownerActorNumber;
        public string ColorId => _colorId;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            EnsureShadow();
        }

        /// <summary>
        /// Builds the contact shadow once per imp. It is a child so it follows the imp
        /// around the board for free; the drop animation then pins it to the ground via
        /// <see cref="SetAirborneOffset"/>.
        /// </summary>
        private void EnsureShadow()
        {
            if (_shadow != null) return;

            // The pool's template is a live GameObject whose Awake has already run, so
            // every clone arrives with a shadow baked in. _shadow is not serialized and
            // cannot see it, so adopt the existing child rather than adding a second.
            var existing = transform.Find("Shadow");
            if (existing != null)
            {
                _shadow = existing.GetComponent<SpriteRenderer>();
                if (_shadow != null)
                {
                    if (_shadow.sprite == null) _shadow.sprite = ShadowSprite();
                    SetAirborneOffset(Vector3.zero);
                    return;
                }
            }

            var go = new GameObject("Shadow");
            go.transform.SetParent(transform, false);
            _shadow = go.AddComponent<SpriteRenderer>();
            _shadow.sprite = ShadowSprite();
            SetAirborneOffset(Vector3.zero);
        }

        /// <summary>
        /// A soft radial blob, built in code rather than shipped as art: it needs no
        /// import settings, and a hard-edged ellipse would read as a decal stuck to the
        /// tile rather than as a shadow.
        /// </summary>
        private static Sprite ShadowSprite()
        {
            if (_shadowSprite != null) return _shadowSprite;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "ImpContactShadow"
            };

            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    // Squared falloff; linear leaves a visible rim.
                    float a = Mathf.Clamp01(1f - d);
                    px[y * size + x] = new Color32(0, 0, 0, (byte)(a * a * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply();

            // 64 PPU over 64px makes the sprite exactly one world unit, so shadowSize
            // reads straight off the inspector as world units.
            _shadowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
            return _shadowSprite;
        }

        /// <summary>
        /// Positions the shadow for an imp currently <paramref name="worldOffset"/> away
        /// from where it will land. The shadow stays put on the ground and tightens as
        /// the imp drops onto it; a shadow that rides along with a falling imp reads as
        /// the imp sliding down glass.
        /// </summary>
        public void SetAirborneOffset(Vector3 worldOffset)
        {
            if (_shadow == null) return;

            // The shadow is a child of a scaled imp, so undo that scale to keep the
            // shadow measured in world units.
            float sx = Mathf.Approximately(transform.localScale.x, 0f) ? 1f : transform.localScale.x;
            float sy = Mathf.Approximately(transform.localScale.y, 0f) ? 1f : transform.localScale.y;

            _shadow.transform.localPosition = new Vector3(
                -worldOffset.x / sx,
                (shadowOffsetY - worldOffset.y) / sy,
                0f);

            float height = Mathf.Max(0f, worldOffset.y);
            float closeness = 1f - Mathf.Clamp01(height / Mathf.Max(0.01f, shadowSpreadHeight));

            // High up: wide and faint. Underfoot: tight and dark.
            float spread = Mathf.Lerp(1.45f, 1f, closeness);
            _shadow.transform.localScale = new Vector3(
                shadowSize.x * spread / sx,
                shadowSize.y * spread / sy,
                1f);

            var c = Color.black;
            c.a = shadowOpacity * Mathf.Lerp(0.3f, 1f, closeness);
            _shadow.color = c;
        }

        /// <summary>
        /// Initializes the imp with spawn data.
        /// </summary>
        /// <param name="coordinate">Grid coordinate</param>
        /// <param name="ownerActorNumber">Owner's actor number</param>
        /// <param name="colorId">Color identifier</param>
        /// <param name="sprite">Sprite to display</param>
        /// <param name="position">World position</param>
        public void Initialize(string coordinate, int ownerActorNumber, string colorId, Sprite sprite, Vector3 position)
        {
            _coordinate = coordinate;
            _ownerActorNumber = ownerActorNumber;
            _colorId = colorId;

            transform.position = position;
            transform.localScale = Vector3.one * 0.9f;

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
                spriteRenderer.sortingOrder = 10;
            }

            EnsureShadow();
            if (_shadow != null && spriteRenderer != null)
            {
                _shadow.sortingLayerID = spriteRenderer.sortingLayerID;
                _shadow.sortingOrder = spriteRenderer.sortingOrder - 1;
            }
            // Imps are pooled, so a reused one can still carry the last drop's offset.
            SetAirborneOffset(Vector3.zero);

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Resets the imp for pooling.
        /// </summary>
        public void Reset()
        {
            _coordinate = "";
            _ownerActorNumber = -1;
            _colorId = "";

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = null;
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Sets the sprite.
        /// </summary>
        public void SetSprite(Sprite sprite)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }

        /// <summary>
        /// Sets the sorting order.
        /// </summary>
        public void SetSortingOrder(int order)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = order;
            }

            // The shadow travels with the imp through tucking, or a tucked imp leaves
            // its shadow drawing over the building it ducked behind.
            if (_shadow != null) _shadow.sortingOrder = order - 1;
        }
    }
}
