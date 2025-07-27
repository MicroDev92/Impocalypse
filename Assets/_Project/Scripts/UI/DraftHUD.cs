using TMPro;
using UnityEngine;

public enum GamePhase
{
    Draft,
    Build
}

public class DraftHUD : MonoBehaviour
{
    public static DraftHUD Instance { get; private set; }

    [Header("UI References")]
    public TMP_Text roundText;
    public TMP_Text phaseText;
    public TMP_Text statusText;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void SetRound(int round)
    {
        if (roundText != null)
            roundText.text = $"Round {round}";
    }

    public void SetPhase(GamePhase phase)
    {
        if (phaseText != null)
            phaseText.text = $"Phase: {phase.ToString().ToUpper()}";
    }

    public void SetStatus(string status)
    {
        if (statusText != null)
            statusText.text = status;
    }

    public void UpdateHUD(int round, GamePhase phase, string status)
    {
        SetRound(round);
        SetPhase(phase);
        SetStatus(status);
    }
}