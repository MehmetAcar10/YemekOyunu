using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "SummerJam/Recipe")]
public class RecipeSO : ScriptableObject
{
    public List<IngredientSO> requiredIngredients = new List<IngredientSO>();
    public float requiredRotations = 3f;
    public GameObject resultPrefab;
}
