using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace _Project.Scripts.Utility
{
    public class InputSystemFixer : MonoBehaviour
    {
        private void Awake()
        {
            FixEventSystem();
        }

        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            FixEventSystem();
        }

        private void FixEventSystem()
        {
            var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            
            if (eventSystems.Length > 1)
            {
                Debug.LogWarning($"[InputSystemFixer] Found {eventSystems.Length} EventSystems. Keeping only the first one.");
                for (int i = 1; i < eventSystems.Length; i++)
                {
                    DestroyImmediate(eventSystems[i].gameObject);
                }
            }

            var es = Object.FindFirstObjectByType<EventSystem>();
            if (es != null)
            {
                var legacy = es.GetComponent<StandaloneInputModule>();
                if (legacy != null)
                {
                    Debug.Log($"[InputSystemFixer] Replacing legacy StandaloneInputModule on {es.gameObject.name}");
                    DestroyImmediate(legacy);
                }

                if (es.GetComponent<InputSystemUIInputModule>() == null)
                {
                    Debug.Log($"[InputSystemFixer] Adding InputSystemUIInputModule to {es.gameObject.name}");
                    es.gameObject.AddComponent<InputSystemUIInputModule>();
                }
            }
            else
            {
                Debug.LogWarning("[InputSystemFixer] No EventSystem found in scene!");
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<InputSystemUIInputModule>();
            }
        }
    }
}
