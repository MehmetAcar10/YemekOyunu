using StarterAssets;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class CursorUnlockOnTrigger : MonoBehaviour
{
    [SerializeField]
    private string m_PlayerTag = "Player";

    private int m_InsideCount;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(m_PlayerTag))
        {
            return;
        }

        m_InsideCount++;
        if (m_InsideCount == 1)
        {
            SetCursorFree(true, other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(m_PlayerTag))
        {
            return;
        }

        m_InsideCount = Mathf.Max(0, m_InsideCount - 1);
        if (m_InsideCount == 0)
        {
            SetCursorFree(false, other);
        }
    }

    private void OnDisable()
    {
        if (m_InsideCount > 0)
        {
            m_InsideCount = 0;
            SetCursorFree(false, null);
        }
    }

    private void SetCursorFree(bool free, Collider playerCollider)
    {
        Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = free;

        StarterAssetsInputs inputs = null;
        if (playerCollider != null)
        {
            inputs = playerCollider.GetComponentInParent<StarterAssetsInputs>();
        }
        if (inputs == null)
        {
            var playerGo = GameObject.FindGameObjectWithTag(m_PlayerTag);
            if (playerGo != null)
            {
                inputs = playerGo.GetComponentInChildren<StarterAssetsInputs>(true);
            }
        }

        if (inputs != null)
        {
            inputs.cursorLocked = !free;
            inputs.cursorInputForLook = !free;
            if (free)
            {
                inputs.look = Vector2.zero;
            }
        }
    }
}
