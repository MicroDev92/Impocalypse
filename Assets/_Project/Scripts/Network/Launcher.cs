using UnityEngine;
using _Project.Scripts.Utility;

/// <summary>
/// Main menu entry point. Owns the player's name and, for now, drops straight into
/// Quick Play - B2.2 replaces the auto-jump with the actual menu buttons.
/// </summary>
public class Launcher : MonoBehaviour
{
    public const string NicknameKey = "nickname";

    [Tooltip("Jump straight into Quick Play once connected. Keeps one-click editor " +
             "testing working until the menu UI lands (B2.2).")]
    [SerializeField] private bool autoQuickPlay = true;

    private INetworkService _network;

    private void Awake()
    {
        if (!PlayerPrefs.HasKey(NicknameKey))
        {
            PlayerPrefs.SetString(NicknameKey, "Player_" + Random.Range(1000, 9999));
        }

        // Assign before NetworkService.Start() connects - the nickname travels with
        // the join, so setting it afterwards would not reach anyone.
        Photon.Pun.PhotonNetwork.NickName = PlayerPrefs.GetString(NicknameKey);
    }

    private void Start()
    {
        if (!autoQuickPlay) return;

        if (!ServiceLocator.TryGetService(out _network))
        {
            Debug.LogWarning("[Launcher] No INetworkService registered - cannot auto Quick Play");
            return;
        }

        // Coming back to the menu from a finished game, we are already connected and
        // no further OnConnectedToServer is due.
        if (_network.IsConnected)
        {
            _network.QuickPlay();
            return;
        }

        _network.OnConnectedToServer += HandleConnected;
    }

    private void OnDestroy()
    {
        if (_network != null)
        {
            _network.OnConnectedToServer -= HandleConnected;
        }
    }

    private void HandleConnected()
    {
        _network.OnConnectedToServer -= HandleConnected;
        _network.QuickPlay();
    }
}
