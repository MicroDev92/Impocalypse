using UnityEngine;
using UnityEngine.UI;
using World;

/// <summary>
/// Puts the board back where it started.
///
/// Pan and zoom previously had no way home: drag the board off screen and nothing
/// short of a phase change brought it back, which looks like the game breaking
/// rather than a view you moved. FitGridToScreen already resets both position and
/// orthographic size, so this is a button in front of a method that existed.
/// </summary>
[RequireComponent(typeof(Button))]
public class RecenterBoardButton : MonoBehaviour
{
    private ModernCameraController _camera;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(Recenter);

        _camera = FindFirstObjectByType<ModernCameraController>();
        if (_camera == null)
            Debug.LogWarning("[RecenterBoardButton] No ModernCameraController in the scene - recenter will do nothing.");
    }

    private void Recenter()
    {
        // Re-find rather than trust the cached reference: the camera is not destroyed
        // in normal play, but a null here would silently make the button dead, and a
        // dead button is worse than no button.
        if (_camera == null) _camera = FindFirstObjectByType<ModernCameraController>();
        _camera?.FitGridToScreen();
    }
}
