using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform cardPanel;
    private List<Card> activeCards = new();

    private void Start()
    {
        Card.OnCardConfirmed += OnCardConfirmed;
        SpawnInitialCards();
    }

    private void SpawnInitialCards()
    {
        if (GameManager.Instance == null || GameManager.Instance.rules == null)
        {
            Debug.LogError("GameManager or GameRules is not ready yet!");
            return;
        }

        int count = GameManager.Instance.CardsPerPlayer;
        string[] coords = GridGenerator.Instance.GetRandomCoords(count);

        foreach (var coord in coords)
        {
            GameObject go = Instantiate(cardPrefab, cardPanel);
            Card card = go.GetComponent<Card>();

            if (card == null)
            {
                Debug.LogError("Card prefab is missing Card.cs script!");
                continue;
            }

            card.coordinate = coord;
            activeCards.Add(card);
        }
    }


    private void OnCardConfirmed(Card selected)
    {
        if (Card.cardsConfirmed >= 2)
        {
            foreach (var card in activeCards)
            {
                if (card != selected)
                    card.Disable();
            }

            GameManager.Instance.currentRound++;
            Debug.Log($"➡ Round {GameManager.Instance.currentRound} - Direction: {(GameManager.Instance.IsClockwise ? "Clockwise" : "Counter-clockwise")}");
        }
    }
}