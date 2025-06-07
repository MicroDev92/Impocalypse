using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Card : MonoBehaviour
{
    public string coordinate;
    public TextMeshProUGUI label;
    public Button button;

    private bool isHighlighted = false;
    private bool isConfirmed = false;
    private TileData linkedTile;

    public static event System.Action<Card> OnCardConfirmed;

    private void Start()
    {
        if (label == null || button == null)
        {
            Debug.LogError("Card prefab is missing label or button reference.");
            return;
        }

        label.text = coordinate;
        button.onClick.AddListener(OnCardClicked);
        linkedTile = GridGenerator.Instance.GetTileByCoord(coordinate);
    }

    private void OnCardClicked()
    {
        if (!isHighlighted)
        {
            SetHighlight(true);
            isHighlighted = true;
        }
        else if (!isConfirmed)
        {
            isConfirmed = true;
            SetHighlight(false);
            OnCardConfirmed?.Invoke(this);
        }
    }

    private void SetHighlight(bool state)
    {
        linkedTile?.Highlight(state);
    }

    public void Disable()
    {
        button.interactable = false;
        SetHighlight(false);
    }
}