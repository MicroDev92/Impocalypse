using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Scoring;

/// <summary>
/// Companion script for the ScoreEntry prefab.
/// Populates text fields and instantiates fief detail lines.
/// All layout is configured on the prefab in the Editor.
/// </summary>
public class ScoreEntry : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Transform fiefDetailsContainer;

    /// <summary>
    /// Populates the score entry with player data.
    /// </summary>
    /// <param name="playerName">Display name of the player</param>
    /// <param name="roundScore">Score earned this round</param>
    /// <param name="cumulative">Cumulative score across all rounds</param>
    /// <param name="fiefs">List of fiefs for detail breakdown (can be null)</param>
    /// <param name="fiefDetailPrefab">Prefab for fief detail lines (can be null)</param>
    public void SetData(string playerName, int roundScore, int cumulative, List<Fief> fiefs, GameObject fiefDetailPrefab)
    {
        if (playerNameText != null)
            playerNameText.text = playerName;

        if (scoreText != null)
        {
            if (roundScore == cumulative)
                scoreText.text = $"{cumulative} pts";
            else
                scoreText.text = $"{cumulative} pts (this round: +{roundScore})";
        }

        if (fiefs != null && fiefs.Count > 0 && fiefDetailsContainer != null && fiefDetailPrefab != null)
        {
            for (int i = 0; i < fiefs.Count; i++)
            {
                var fief = fiefs[i];
                var detailObj = Instantiate(fiefDetailPrefab, fiefDetailsContainer);
                var detailText = detailObj.GetComponentInChildren<TMP_Text>();
                if (detailText != null)
                {
                    detailText.text = $"Fief {i + 1}: {fief.Tiles.Count} tiles, Str {fief.Strength} x Wealth {fief.Wealth} = {fief.Score}";
                }
            }
        }
    }
}
