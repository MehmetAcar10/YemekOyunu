using UnityEngine;
using StarterAssets;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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

    public void ApplyCursorState()
    {
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

#if ENABLE_INPUT_SYSTEM
            if (lockCursor)
            {
                var playerInputs = player.GetComponentsInChildren<PlayerInput>(true);
                for (int i = 0; i < playerInputs.Length; i++)
                {
                    playerInputs[i].enabled = true;
                    if (!playerInputs[i].inputIsActive)
                        playerInputs[i].ActivateInput();
                }
            }
#endif
        }
    }
}
