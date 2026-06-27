using UnityEngine;
using UnityEngine.SceneManagement;
using Summerjam.Utils;

/// <summary>
/// Oyuncu (Player tag) bu objenin trigger hacmine girip etkilesim tusuna (E)
/// basinca hedef sahneye gecer. AkvaryumTriger -> BalikTutma gibi.
/// </summary>
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class SceneTriggerOnKey : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string targetSceneName = "BalıkTutma";
    [SerializeField] private string promptText = "E - Balık Tut";

    private int m_InsideCount;
    private bool m_Loading;
    private GUIStyle m_Style;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
            m_InsideCount++;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
            m_InsideCount = Mathf.Max(0, m_InsideCount - 1);
    }

    private void Update()
    {
        if (m_Loading || m_InsideCount <= 0)
            return;

        if (Input.GetKeyDown(interactKey))
            LoadTargetScene();
    }

    private void LoadTargetScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
            return;

        m_Loading = true;

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(targetSceneName);
        else
            SceneManager.LoadScene(targetSceneName);
    }

    private void OnGUI()
    {
        if (m_InsideCount <= 0 || m_Loading || string.IsNullOrEmpty(promptText))
            return;

        if (m_Style == null)
        {
            m_Style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            m_Style.normal.textColor = Color.white;
        }

        float w = 360f, h = 44f;
        GUI.Label(new Rect((Screen.width - w) * 0.5f, Screen.height * 0.78f, w, h), promptText, m_Style);
    }
}
