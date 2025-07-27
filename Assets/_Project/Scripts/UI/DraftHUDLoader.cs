using UnityEngine;

public class DraftHUDLoader : MonoBehaviour
{
    [SerializeField] private GameObject draftHudPrefab;

    void Start()
    {
        if (FindFirstObjectByType<DraftHUD>() == null && draftHudPrefab != null)
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Instantiate(draftHudPrefab, canvas.transform);
            }
        }
    }
}