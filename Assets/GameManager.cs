// ✅ GameManager.cs
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    [Header("UI References")]
    public Transform playerListParent;
    public GameObject playerEntryPrefab;
    public Button startGameButton;
    public TMP_Text statusText;

    [Header("Game Logic")]
    public GameRules rules;
    public int currentRound = 1;

    public int CardsPerPlayer => rules.GetCardsPerPlayer(TotalPlayers);
    public bool IsClockwise => rules.IsClockwise(currentRound);
    public int TotalPlayers => PhotonNetwork.CurrentRoom?.PlayerCount ?? 1;

    private Dictionary<int, GameObject> playerEntries = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            PhotonNetwork.AddCallbackTarget(this);

            if (rules == null)
                rules = Resources.Load<GameRules>("DevRules");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameRoom")
        {
            playerListParent = GameObject.Find("PlayerListPanel")?.transform;
            startGameButton = GameObject.Find("StartGameButton")?.GetComponent<Button>();
            statusText = GameObject.Find("StatusText")?.GetComponent<TMP_Text>();

            if (startGameButton != null)
            {
                startGameButton.onClick.RemoveAllListeners();
                startGameButton.onClick.AddListener(OnStartGameClicked);

                startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
                startGameButton.interactable = PhotonNetwork.IsMasterClient;
            }

            if (statusText != null)
            {
                statusText.text = PhotonNetwork.IsMasterClient
                    ? "You are the host"
                    : "Waiting for host to start...";
            }

            BuildPlayerList();
        }
    }

    private void BuildPlayerList()
    {
        if (playerListParent == null) return;

        foreach (Transform child in playerListParent)
            Destroy(child.gameObject);

        playerEntries.Clear();

        foreach (var player in PhotonNetwork.PlayerList)
        {
            GameObject entry = Instantiate(playerEntryPrefab, playerListParent);
            TMP_Text text = entry.GetComponent<TMP_Text>();
            text.text = player.NickName;
            playerEntries[player.ActorNumber] = entry;
        }
    }

    private void OnStartGameClicked()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("GameScene");
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Player joined: {newPlayer.NickName}");
        BuildPlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        BuildPlayerList();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
            startGameButton.interactable = PhotonNetwork.IsMasterClient;
        }
    }
}
