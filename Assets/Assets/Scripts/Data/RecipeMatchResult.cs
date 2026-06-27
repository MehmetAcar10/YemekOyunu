public struct RecipeMatchResult
{
    public RecipeSO Recipe;
    public RecipeMatchQuality Quality;
    public int Difference;

    public bool IsValid => Recipe != null;

    public static RecipeMatchResult None => new RecipeMatchResult
    {
        Recipe = null,
        Quality = RecipeMatchQuality.None,
        Difference = int.MaxValue
    };
}
