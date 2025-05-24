using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

public class CardManager : MonoBehaviour, IOnEventCallback
{
    public GameObject cardPrefab;
    public Transform cardPanel;
    private const byte Event_CardDeal = 1;
    private List<Card> activeCards = new();
    private int confirmedCount = 0;
    [SerializeField] private Sprite impSprite;

    private void Start()
    {
        Card.OnCardConfirmed += OnCardConfirmed;

        if (PhotonNetwork.IsMasterClient)
            DealInitialCards();

        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDestroy()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
        Card.OnCardConfirmed -= OnCardConfirmed;
    }

    private void DealInitialCards()
    {
        int count = GameManager.Instance.CardsPerPlayer;

        // Get enough coords to give each player a unique hand
        var players = PhotonNetwork.CurrentRoom.Players.Values.ToList();
        var allCoords = GridGenerator.Instance.GetRandomCoords(count * players.Count);

        for (int i = 0; i < players.Count; i++)
        {
            string[] playerCoords = allCoords.Skip(i * count).Take(count).ToArray();

            object[] data = new object[] { playerCoords };

            PhotonNetwork.RaiseEvent(Event_CardDeal, data,
                new RaiseEventOptions { TargetActors = new int[] { players[i].ActorNumber } },
                new SendOptions { Reliability = true });
        }
    }


    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == Event_CardDeal)
        {
            object[] data = (object[])photonEvent.CustomData;
            string[] coords = ((object[])data[0]).Cast<string>().ToArray();
            SpawnCards(coords);
        }

        if (photonEvent.Code == 2)
        {
            var coord = (string)photonEvent.CustomData;
            SpawnImpRemote(coord);
        }
    }

    private void SpawnCards(string[] coordinates)
    {
        foreach (Transform child in cardPanel)
            Destroy(child.gameObject);

        activeCards.Clear();
        confirmedCount = 0;

        foreach (var coord in coordinates)
        {
            GameObject go = Instantiate(cardPrefab, cardPanel);
            Card card = go.GetComponent<Card>();
            if (card == null) continue;
            card.coordinate = coord;
            activeCards.Add(card);
        }
    }
    
    [PunRPC]
    private void SpawnImpRemote(string coord)
    {
        TileData tile = GridGenerator.Instance.GetTileByCoord(coord);
        if (tile == null) return;

        GameObject imp = new GameObject("Imp");
        imp.transform.position = tile.transform.position;
        imp.transform.localScale = Vector3.one * 0.9f;

        var sr = imp.AddComponent<SpriteRenderer>();
        sr.sprite = impSprite;
        sr.sortingOrder = 10;
    }

    private void OnCardConfirmed(Card selected)
    {
        confirmedCount++;

        if (confirmedCount >= 2)
        {
            foreach (var card in activeCards)
            {
                if (card != selected)
                    card.Disable();
            }

            GameManager.Instance.currentRound++;
            Debug.Log($"➡ Round {GameManager.Instance.currentRound} - Direction: {(GameManager.Instance.IsClockwise ? "Clockwise" : "Counter-clockwise")}");

            if (PhotonNetwork.IsMasterClient)
                DealInitialCards();
        }
    }
}