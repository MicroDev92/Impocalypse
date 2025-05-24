// ✅ NetworkManager.cs
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.NickName = "Player_" + Random.Range(1000, 9999);
        PhotonNetwork.GameVersion = "1.0";
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("✅ Connected to Photon Master Server.");

        PhotonNetwork.JoinOrCreateRoom("LocalTestRoom", new RoomOptions
        {
            MaxPlayers = 4,
            IsOpen = true,
            IsVisible = true
        }, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"✅ Joined Room: {PhotonNetwork.CurrentRoom.Name}");

        foreach (var player in PhotonNetwork.PlayerList)
        {
            Debug.Log($"🧍 Player {player.NickName} - Actor {player.ActorNumber}");
        }

        SceneManager.LoadScene("GameRoom");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"❌ Failed to join room: {message}");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"❌ Failed to create room: {message}");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"⚠ Disconnected from Photon: {cause}");
    }
}