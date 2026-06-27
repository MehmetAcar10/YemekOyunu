using UnityEngine;
using StarterAssets;

/// <summary>
/// Sahne yuklendiginde imleç durumunu uygular. FPS sahnelerinde (Anasahne)
/// imleç kilitli/gizli; fare tabanli mini-oyunlarda (BalikTutma) serbest/gorunur.
/// </summary>
[DisallowMultipleComponent]
public class SceneCursorState : MonoBehaviour
{
    [Tooltip("true = imleç kilitli/gizli (FPS), false = serbest/gorunur (fare mini-oyun)")]
    [SerializeField] private bool lockCursor = false;

    private void Start()
    {
        Apply();
    }

    private void OnEnable()
    {
        Apply();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            Apply();
    }

    private void Apply()
    {
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockCursor;

        // FPS oyuncusu varsa look/lock ayarlarini da senkronla
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var inputs = player.GetComponentInChildren<StarterAssetsInputs>(true);
            if (inputs != null)
            {
                inputs.cursorLocked = lockCursor;
                inputs.cursorInputForLook = lockCursor;
            }
        }
    }
}
