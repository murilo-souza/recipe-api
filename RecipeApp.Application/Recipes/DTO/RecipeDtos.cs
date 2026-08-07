namespace RecipeApp.Application.Recipes.DTO;

public record PrepareStepItem(int Id, string Description, short Position);

public record CreateRecipeRequest(string Title, string Description, int CategoryId, string? Image, string[] Ingredients, string[] PrepareSteps);

public record UpdateRecipeRequest(string Title, string Description, int CategoryId, string? Image, string[] Ingredients, string[] PrepareSteps);

public record RecipeSummaryResponse(int Id, string Title, string Description, string? Image, int CategoryId, DateTimeOffset CreatedAt);

public record RecipeResponse(int Id, string Title, string Description, string? Image, int CategoryId, string[] Ingredients, PrepareStepItem[] PrepareSteps, DateTimeOffset CreatedAt);

