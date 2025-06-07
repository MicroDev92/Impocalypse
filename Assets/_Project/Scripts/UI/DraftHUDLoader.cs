using UnityEngine;

public class DraftHUDLoader : MonoBehaviour
{
    [SerializeField] private GameObject draftHudPrefab;

    void Start()
    {
        if (FindFirstObjectByType<DraftHUD>() == null && draftHudPrefab != null)
        {
            GameObject hud = Instantiate(draftHudPrefab, transform);
        }
    }
}