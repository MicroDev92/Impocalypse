using UnityEngine;

public class Launcher : MonoBehaviour
{
    private void Awake()
    {
        // Set the nickname in PlayerPrefs if not already set
        if (!PlayerPrefs.HasKey("nickname"))
        {
            string nickname = "Player_" + Random.Range(1000, 9999);
            PlayerPrefs.SetString("nickname", nickname);
        }

        // Assign the nickname to Photon before NetworkManager runs Start()
        Photon.Pun.PhotonNetwork.NickName = PlayerPrefs.GetString("nickname");
    }
}

