using UnityEngine;

[CreateAssetMenu(fileName = "GameRules", menuName = "Game/Rules", order = 1)]
public class GameRules : ScriptableObject
{
    public bool testMode = true;
    public int numberOfPlayers = 4;

    public int cardsPerPlayer_3Players = 12;
    public int cardsPerPlayer_4Players = 10;
    public int cardsPerPlayer_Test = 4;

    public int GetCardsPerPlayer()
    {
        if (testMode) return cardsPerPlayer_Test;

        return numberOfPlayers switch
        {
            3 => cardsPerPlayer_3Players,
            4 => cardsPerPlayer_4Players,
            _ => 4
        };
    }

    public bool IsClockwise(int round)
    {
        return round % 2 != 0;
    }
}