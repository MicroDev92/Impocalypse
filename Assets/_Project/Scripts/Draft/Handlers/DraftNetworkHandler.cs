using UnityEngine;

namespace Draft.Handlers
{
    /// <summary>
    /// DEPRECATED: This system has been replaced by DraftService which handles
    /// all network events (DealCards, SubmitPicks, SpawnImps) directly.
    /// Kept as empty shell to avoid missing script references in scenes.
    /// Remove this component from your GameScene when convenient.
    /// </summary>
    [System.Obsolete("Use DraftService instead. Remove this component from scenes.")]
    public class DraftNetworkHandler : MonoBehaviour
    {
        private void Awake()
        {
            Debug.LogWarning("[DraftNetworkHandler] DEPRECATED - This component does nothing. Remove it from the scene and use DraftService instead.");
        }
    }
}
