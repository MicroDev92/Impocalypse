using System.Collections.Generic;
using UnityEngine;
using Core;

/// <summary>
/// Interface for the main game manager service.
/// Defines the contract for game state management and coordination.
/// </summary>
public interface IGameManager
{
    /// <summary>
    /// Current round number (1-based)
    /// </summary>
    int CurrentRound { get; }
    
    /// <summary>
    /// Current game phase
    /// </summary>
    GamePhase CurrentPhase { get; }
    
    /// <summary>
    /// Total number of players in the game
    /// </summary>
    int TotalPlayers { get; }
    
    /// <summary>
    /// Number of cards per player for the current game
    /// </summary>
    int CardsPerPlayer { get; }
    
    /// <summary>
    /// Whether the current round is clockwise (true) or counter-clockwise (false)
    /// </summary>
    bool IsClockwise { get; }
    
    /// <summary>
    /// Maximum number of rounds in the game
    /// </summary>
    int MaxRounds { get; }
    
    /// <summary>
    /// Game rules configuration
    /// </summary>
    GameRules Rules { get; }
    
    /// <summary>
    /// Advances the game phase (Draft -> Build -> Next Round)
    /// </summary>
    void AdvancePhase();
    
    /// <summary>
    /// Checks if the game is over
    /// </summary>
    /// <returns>True if game has ended</returns>
    bool IsGameOver();
    
    /// <summary>
    /// Starts a new game round
    /// </summary>
    void StartNewRound();
    
    /// <summary>
    /// Ends the current game
    /// </summary>
    void EndGame();

    /// <summary>
    /// Adds round scores to cumulative totals.
    /// </summary>
    void AddRoundScores(Dictionary<int, int> roundScores);

    /// <summary>
    /// Gets cumulative scores across all rounds.
    /// </summary>
    Dictionary<int, int> GetCumulativeScores();
}
