using UnityEngine;

public class Launcher : MonoBehaviour
{
    private void Start()
    {
        NetworkManager.Instance.ConnectToPhoton();
    }
}
