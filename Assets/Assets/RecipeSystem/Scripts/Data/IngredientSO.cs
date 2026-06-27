using UnityEngine;

[CreateAssetMenu(fileName = "NewIngredient", menuName = "SummerJam/Ingredient")]
public class IngredientSO : ScriptableObject
{
    public string ingredientName;
    public GameObject visualPrefab;
}
