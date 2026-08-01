using UnityEngine;
using UnityEngine.UI;
using TMPro;
using _Project.Scripts.Utility;
using Core.Events;

/// <summary>
/// UI controller for the GameRoom scene (lobby).
/// All layout is configured in the Editor — this script manages player list,
/// start button visibility, and status text via events.
/// </summary>
public class GameRoomUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform playerListParent;
    [SerializeField] private GameObject playerEntryPrefab;
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text roomNameText;

    private IEventBus _eventBus;
    private INetworkService _networkService;

    private void Start()
    {
        _networkService = ServiceLocator.GetService<INetworkService>();
        ServiceLocator.TryGetService(out _eventBus);

        if (_eventBus != null)
        {
            _eventBus.Subscribe<PlayerJoinedEvent>(OnPlayerJoined);
            _eventBus.Subscribe<PlayerLeftEvent>(OnPlayerLeft);
            _eventBus.Subscribe<ShowStatusEvent>(OnShowStatus);
        }

        // The EventBus player events come from PlayerService, which does NOT exist in
        // the lobby scene - so subscribe to the network layer directly. These fire
        // straight off Photon's callbacks and work regardless of what else is loaded.
        if (_networkService != null)
        {
            _networkService.OnPlayerJoined += OnNetworkPlayerJoined;
            _networkService.OnPlayerLeft += OnNetworkPlayerLeft;
            _networkService.OnMasterClientChanged += OnMasterChanged;
        }

        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartClicked);
        }

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (_eventBus != null)
        {
            _eventBus.Unsubscribe<PlayerJoinedEvent>(OnPlayerJoined);
            _eventBus.Unsubscribe<PlayerLeftEvent>(OnPlayerLeft);
            _eventBus.Unsubscribe<ShowStatusEvent>(OnShowStatus);
        }

        if (_networkService != null)
        {
            _networkService.OnPlayerJoined -= OnNetworkPlayerJoined;
            _networkService.OnPlayerLeft -= OnNetworkPlayerLeft;
            _networkService.OnMasterClientChanged -= OnMasterChanged;
        }
    }

    private void OnNetworkPlayerJoined(int actorNumber, string nickname)
    {
        UpdateUI();
        if (statusText != null && !string.IsNullOrEmpty(nickname))
            statusText.text = $"{nickname} joined";
    }

    private void OnNetworkPlayerLeft(int actorNumber, string nickname)
    {
        UpdateUI();
        if (statusText != null && !string.IsNullOrEmpty(nickname))
            statusText.text = $"{nickname} left";
    }

    /// <summary>Host migration: whoever inherits master gets the Start button.</summary>
    private void OnMasterChanged(int actorNumber, string nickname) => UpdateUI();

    private void OnPlayerJoined(PlayerJoinedEvent evt)
    {
        BuildPlayerList();
        UpdateStartButton();
    }

    private void OnPlayerLeft(PlayerLeftEvent evt)
    {
        BuildPlayerList();
        UpdateStartButton();
    }

    private void OnShowStatus(ShowStatusEvent evt)
    {
        if (statusText != null)
            statusText.text = evt.Message;
    }

    private void UpdateUI()
    {
        // Room name
        if (roomNameText != null && _networkService != null)
        {
            roomNameText.text = _networkService.CurrentRoomName ?? "Room";
        }

        // Status
        if (statusText != null)
        {
            bool isMaster = _networkService?.IsMasterClient ?? false;
            statusText.text = isMaster
                ? "You are the host"
                : "Waiting for host to start...";
        }

        BuildPlayerList();
        UpdateStartButton();
    }

    private void BuildPlayerList()
    {
        if (playerListParent == null || playerEntryPrefab == null) return;

        // Clear existing entries
        foreach (Transform child in playerListParent)
            Destroy(child.gameObject);

        if (_networkService == null) return;

        var playerIds = _networkService.GetPlayerIds();
        foreach (var actorNumber in playerIds)
        {
            GameObject entry = Instantiate(playerEntryPrefab, playerListParent);
            TMP_Text text = entry.GetComponent<TMP_Text>();
            if (text != null)
            {
                text.text = _networkService.GetPlayerName(actorNumber) ?? $"Player {actorNumber}";
            }
        }
    }

    private void UpdateStartButton()
    {
        if (startButton == null) return;

        bool isMaster = _networkService?.IsMasterClient ?? false;
        startButton.gameObject.SetActive(isMaster);
        startButton.interactable = isMaster;
    }

    private void OnStartClicked()
    {
        if (_networkService?.IsMasterClient == true)
        {
            // Map seed lives in the room's creation properties (see NetworkService) -
            // setting it here would race the scene switch and desync the maps.
            _networkService.LoadScene("GameScene");
        }
    }
}
