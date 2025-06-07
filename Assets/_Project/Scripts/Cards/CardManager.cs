using System.Collections.Generic;
using System.Linq;
using _Project.Scripts.Data;
using _Project.Scripts.Visuals;
using UnityEngine;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

public class CardManager : MonoBehaviour, IOnEventCallback
{
    public GameObject cardPrefab;
    public Transform cardPanel;
    [SerializeField] private ImpVisuals impVisuals;

    private List<Card> activeCards = new();
    private List<Card> selectedCards = new();
    private Dictionary<int, PlayerData> playerDataByActor = new();

    private void Start()
    {
        Card.OnCardConfirmed += OnCardConfirmed;
        PhotonNetwork.AddCallbackTarget(this);
        
        FindFirstObjectByType<DraftHUD>()?.UpdateHUD();
    }

    private void OnDestroy()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
        Card.OnCardConfirmed -= OnCardConfirmed;
    }

    public void OnEvent(EventData photonEvent)
    {
        switch (photonEvent.Code)
        {
            case NetworkEvents.DealCards:
                string[] coords = ((object[])photonEvent.CustomData).Cast<string>().ToArray();
                SpawnCards(coords);
                break;

            case NetworkEvents.AssignPlayerIdentities:
            {
                object[] rawData = (object[])photonEvent.CustomData;
                playerDataByActor.Clear();

                foreach (object[] entry in rawData)
                {
                    int actor = (int)entry[0];
                    string nick = (string)entry[1];
                    string color = (string)entry[2];

                    playerDataByActor[actor] = new PlayerData
                    {
                        ActorNumber = actor,
                        Nickname = nick,
                        ImpColor = color
                    };
                }
                break;
            }

            case NetworkEvents.SpawnImps:
            {
                object[] raw = (object[])photonEvent.CustomData;
                List<ImpSpawnData> impCoords = new();

                foreach (object[] entry in raw)
                {
                    string coord = (string)entry[0];
                    int actor = (int)entry[1];
                    impCoords.Add(new ImpSpawnData { Coord = coord, OwnerActor = actor });
                }

                foreach (var data in impCoords)
                {
                    if (playerDataByActor.TryGetValue(data.OwnerActor, out var owner))
                    {
                        var sprite = impVisuals.GetSprite(owner.ImpColor);
                        SpawnImp(data.Coord, sprite);
                    }
                    else
                    {
                        Debug.LogWarning($"Missing PlayerData for actor {data.OwnerActor}");
                    }
                }
                break;
            }
        }
    }

    private void SpawnCards(string[] coordinates)
    {
        foreach (Transform child in cardPanel)
            Destroy(child.gameObject);

        activeCards.Clear();
        selectedCards.Clear();

        foreach (var coord in coordinates)
        {
            GameObject go = Instantiate(cardPrefab, cardPanel);
            Card card = go.GetComponent<Card>();
            if (card == null) continue;
            card.coordinate = coord;
            activeCards.Add(card);
        }
    }

    private void SpawnImp(string coord, Sprite sprite)
    {
        TileData tile = GridGenerator.Instance.GetTileByCoord(coord);
        if (tile == null) return;

        GameObject imp = new GameObject("Imp");
        imp.transform.position = tile.transform.position;
        imp.transform.localScale = Vector3.one * 0.9f;

        var sr = imp.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 10;
    }

    private void OnCardConfirmed(Card selected)
    {
        if (selectedCards.Contains(selected)) return;

        selectedCards.Add(selected);

        // Spawn local preview for the current player's own imp
        if (playerDataByActor.TryGetValue(PhotonNetwork.LocalPlayer.ActorNumber, out var self))
        {
            var sprite = impVisuals.GetSprite(self.ImpColor);
            SpawnImp(selected.coordinate, sprite);
        }

        if (selectedCards.Count >= 2)
        {
            foreach (var card in activeCards)
            {
                if (!selectedCards.Contains(card))
                    card.Disable();
            }
            
            FindFirstObjectByType<DraftHUD>()?.SetPhase("Waiting for others..."); 

            string[] pickedCoords = selectedCards.Select(c => c.coordinate).ToArray();
            PhotonNetwork.RaiseEvent(NetworkEvents.SubmitPicks, pickedCoords,
                new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
                SendOptions.SendReliable);
        }
    }
}
