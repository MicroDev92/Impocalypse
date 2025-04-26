using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Card : MonoBehaviour
{
    public string coordinate;
    public TextMeshProUGUI label;
    public Button button;

    private bool isSelected = false;
    private bool impSpawned = false;

    public static int cardsConfirmed = 0;
    public static event System.Action<Card> OnCardConfirmed;

    [SerializeField] private Sprite impSprite;

    private void Start()
    {
        label.text = coordinate;
        button.onClick.AddListener(OnCardClicked);
    }

    private void OnCardClicked()
    {
        if (cardsConfirmed >= 2) return;

        if (!isSelected)
        {
            isSelected = true;
            HighlightTile();
        }
        else if (!impSpawned)
        {
            impSpawned = true;
            SpawnImp();
            cardsConfirmed++;
            OnCardConfirmed?.Invoke(this);
        }
    }

    private void HighlightTile()
    {
        TileData tile = GridGenerator.Instance.GetTileByCoord(coordinate);
        tile?.Highlight(true);
    }

    private void SpawnImp()
    {
        TileData tile = GridGenerator.Instance.GetTileByCoord(coordinate);
        if (tile == null) return;

        GameObject imp = new GameObject("Imp");
        imp.transform.position = tile.transform.position;
        imp.transform.localScale = Vector3.one * 0.2f; // or tweak as needed

        var sr = imp.AddComponent<SpriteRenderer>();

        if (impSprite != null)
        {
            sr.sprite = impSprite;
        }
        else
        {
            Debug.LogWarning("Imp sprite not assigned! Using fallback color.");
            sr.sprite = null;
            sr.color = Color.red;
        }

        sr.sortingOrder = 10; // on top of tile
    }




    public void Disable()
    {
        button.interactable = false;
    }
}