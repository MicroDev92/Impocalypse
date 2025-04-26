using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Photon.Realtime;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI References")]
    public Transform playerListParent;
    public GameObject playerEntryPrefab;
    public Button startGameButton;
    public TMP_Text statusText;

    private Dictionary<int, GameObject> playerEntries = new();
    private Room currentRoom;
    private Player localPlayer;
    
    [Header("Game Logic")]
    public GameRules rules;
    public int currentRound = 1;

    public int CardsPerPlayer => rules.GetCardsPerPlayer();
    public bool IsClockwise => rules.IsClockwise(currentRound);


    private void Awake()
{
    if (Instance == null)
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (rules == null)
        {
            rules = Resources.Load<GameRules>("DevRules");
        }
    }
    else
    {
        Destroy(gameObject);
    }
}


    /// <summary>
    /// Called by NetworkManager after joining room and before loading GameRoom scene.
    /// </summary>
    public void SetRoom(Room room, Player local)
    {
        currentRoom = room;
        localPlayer = local;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameRoom")
        {
            // Re-find UI elements
            playerListParent = GameObject.Find("PlayerListPanel")?.transform;
            startGameButton = GameObject.Find("StartGameButton")?.GetComponent<Button>();
            statusText = GameObject.Find("StatusText")?.GetComponent<TMP_Text>();

            if (startGameButton != null)
            {
                startGameButton.onClick.RemoveAllListeners();
                startGameButton.onClick.AddListener(OnStartGameClicked);

                var isHost = IsMasterClient();
                
                startGameButton.gameObject.SetActive(isHost);
                startGameButton.interactable = isHost;
            }

            if (statusText != null)
            {
                statusText.text = IsMasterClient() ? "You are the host" : "Waiting for host to start...";
            }

            BuildPlayerList();
        }
    }

    private void BuildPlayerList()
    {
        if (currentRoom == null || playerListParent == null) return;

        foreach (Transform child in playerListParent)
            Destroy(child.gameObject);

        playerEntries.Clear();

        foreach (var player in currentRoom.Players.Values)
        {
            GameObject entry = Instantiate(playerEntryPrefab, playerListParent);
            TMP_Text text = entry.GetComponent<TMP_Text>();
            text.text = player.NickName;
            playerEntries[player.ActorNumber] = entry;
        }
    }

    private bool IsMasterClient()
    {
        if (currentRoom == null || localPlayer == null)
            return false;

        int lowestActor = int.MaxValue;

        foreach (var player in currentRoom.Players.Values)
        {
            if (player.ActorNumber < lowestActor)
                lowestActor = player.ActorNumber;
        }

        return localPlayer.ActorNumber == lowestActor;
    }

    private void OnStartGameClicked()
    {
        SceneManager.LoadScene("GameScene");
    }
}
