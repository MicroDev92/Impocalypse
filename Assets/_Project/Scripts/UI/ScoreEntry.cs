using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
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

        if (fiefs != null && fiefDetailsContainer != null && fiefDetailPrefab != null)
        {
            // Only list fiefs that actually score, biggest first, capped - a player can
            // hold dozens of 0-point single-tile fiefs that would otherwise flood the panel.
            var scoring = fiefs.Where(f => f.Score > 0)
                               .OrderByDescending(f => f.Score)
                               .Take(8)
                               .ToList();

            // Guarantee the lines stack instead of overlapping, even if the prefab's
            // container has no layout group set up.
            if (fiefDetailsContainer.GetComponent<VerticalLayoutGroup>() == null)
            {
                var vlg = fiefDetailsContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 2;
                vlg.childForceExpandHeight = false;
                vlg.childControlHeight = true;
                vlg.childControlWidth = true;
            }

            int index = 1;
            foreach (var fief in scoring)
            {
                var detailObj = Instantiate(fiefDetailPrefab, fiefDetailsContainer);
                var detailText = detailObj.GetComponentInChildren<TMP_Text>();
                if (detailText != null)
                    detailText.text = $"Fief {index}: {fief.Tiles.Count} tiles, Str {fief.Strength} x Wealth {fief.Wealth} = {fief.Score}";
                index++;
            }
        }
    }
}
