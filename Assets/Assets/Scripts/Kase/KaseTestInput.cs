using UnityEngine;

public class KaseTestInput : MonoBehaviour
{
    [SerializeField] private Kase kase;
    [SerializeField] private IngredientSO milk;
    [SerializeField] private IngredientSO oat;

    private void Update()
    {
        if (kase == null)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            kase.MalzemeEkle(milk);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            kase.MalzemeEkle(oat);

        if (Input.GetKeyDown(KeyCode.Z))
            kase.SonEkleneniCikar();
    }
}
