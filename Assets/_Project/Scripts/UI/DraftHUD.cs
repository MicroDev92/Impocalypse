// File: _Project/Scripts/UI/DraftHUD.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DraftHUD : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text roundText;
    public TMP_Text playerTurnText;
    public TMP_Text phaseLabel;

    public void SetRound(int round)
    {
        if (roundText != null)
            roundText.text = $"Round {round}";
    }

    public void SetPlayerName(string name)
    {
        if (playerTurnText != null)
            playerTurnText.text = $"You are: {name}";
    }

    public void SetPhase(string phase)
    {
        if (phaseLabel != null)
            phaseLabel.text = phase;
    }

    public void UpdateHUD()
    {
        SetRound(GameManager.Instance.currentRound);
        SetPlayerName(Photon.Pun.PhotonNetwork.LocalPlayer.NickName);
        SetPhase("Pick 2 cards");
    }
}