using UnityEngine;

[DefaultExecutionOrder(-100)]
public class InteractionRuntimeBootstrap : MonoBehaviour
{
  private void Awake()
  {
    SummerJamGameBootstrap.EnsurePlayModeSetup();
    Destroy(gameObject);
  }
}
