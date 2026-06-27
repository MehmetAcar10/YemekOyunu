using System.Collections.Generic;

using UnityEngine;



[CreateAssetMenu(fileName = "NewRecipe", menuName = "SummerJam/Recipe")]

public class RecipeSO : ScriptableObject

{

    [Header("Malzemeler")]

    public List<IngredientSO> requiredIngredients = new List<IngredientSO>();



    [Header("Karistirma")]

    public float requiredRotations = 1f;



    [Header("Sonuc")]

    public GameObject resultPrefab;

    public IngredientSO resultInventoryItem;

    public IngredientSO slimInventoryItem;



    [Header("Tarif tipi")]

    public bool isMainRecipe;

    public bool isFallback;

    public int priority;

}
