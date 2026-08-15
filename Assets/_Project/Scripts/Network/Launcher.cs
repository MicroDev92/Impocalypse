using UnityEngine;

/// <summary>
/// Nickname bootstrap for the menu scene. Runs before NetworkService connects so the
/// local player always has a name, whether or not they ever open the name field.
/// Choosing a game is MainMenuUI's job.
/// </summary>
public class Launcher : MonoBehaviour
{
    public const string NicknameKey = "nickname";

    private void Awake()
    {
        if (!PlayerPrefs.HasKey(NicknameKey))
        {
            PlayerPrefs.SetString(NicknameKey, "Player_" + Random.Range(1000, 9999));
        }

        // Assign here, not later: NetworkService has execution order -150, so its
        // Start() connects immediately after this Awake, and the nickname travels with
        // the join. Setting it after that point would reach nobody.
        Photon.Pun.PhotonNetwork.NickName = PlayerPrefs.GetString(NicknameKey);
    }
}
