using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ChickenCatchHUD : MonoBehaviour
{
    [SerializeField]
    private Text m_Label;
    [SerializeField]
    private string m_Format = "Yakalanan Tavuk: {0}";

    private void Awake()
    {
        if (m_Label == null)
        {
            m_Label = GetComponentInChildren<Text>(true);
        }
        Refresh(ChickenCatcher.CaughtCount);
    }

    private void OnEnable()
    {
        ChickenCatcher.CaughtCountChanged += Refresh;
        Refresh(ChickenCatcher.CaughtCount);
    }

    private void OnDisable()
    {
        ChickenCatcher.CaughtCountChanged -= Refresh;
    }

    private void Refresh(int count)
    {
        if (m_Label == null)
        {
            return;
        }

        m_Label.text = string.Format(m_Format, count);
    }
}
