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
        }
    }
}
