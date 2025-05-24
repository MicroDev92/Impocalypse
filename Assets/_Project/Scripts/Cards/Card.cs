using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class Card : MonoBehaviourPun
{
    public string coordinate;
    public TextMeshProUGUI label;
    public Button button;
    [SerializeField] private Sprite impSprite;

    private bool isSelected = false;
    private bool impSpawned = false;

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
    }

    private void OnCardClicked()
    {
        if (!isSelected)
        {
            isSelected = true;
            HighlightTile();
        }
        else if (!impSpawned)
        {
            impSpawned = true;
            
            PhotonNetwork.RaiseEvent(
                eventCode: 2, // or some other event code for imp spawning
                eventContent: coordinate,
                new RaiseEventOptions { Receivers = ReceiverGroup.All },
                new SendOptions { Reliability = true }
            );

            OnCardConfirmed?.Invoke(this);
        }
    }

    private void HighlightTile()
    {
        TileData tile = GridGenerator.Instance.GetTileByCoord(coordinate);
        tile?.Highlight(true);
    }
    public void Disable() => button.interactable = false;
}