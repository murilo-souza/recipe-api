using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RecipeApp.Application.Common.Exceptions;
using RecipeApp.Application.Recipes.DTO;
using RecipeApp.Application.Recipes.Interface;
using System.Security.Claims;

namespace RecipeApp.Api.Controllers
{
    [Route("api/recipe")]
    [ApiController]
    public class RecipesController : ControllerBase
    {
        private readonly IRecipeService _recipeService;

        public RecipesController(IRecipeService recipeService)
        {
            _recipeService = recipeService;
        }


        [Authorize]
        [HttpGet("get-all-recipes")]
        public async Task<IActionResult> GetAllRecipes()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new { value = "Token inválido ou sem ID", statusCode = 401 });
            }

            try
            {
                var recipes = await _recipeService.GetAllRecipesAsync(userId);
                return Ok(recipes);
            }
            catch (Exception ex)
            {
                // Log the exception (you can use a logging framework here)
                return StatusCode(StatusCodes.Status500InternalServerError, new { value = "Erro ao obter receitas", statusCode = 500 });
            }
        }

        [Authorize]
        [HttpGet("get-recipe-by-id")]
        public async Task<IActionResult> GetRecipeById([FromQuery] int recipeId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new { value = "Token inválido ou sem ID", statusCode = 401 });
            }

            try
            {
                var recipe = await _recipeService.GetRecipeByIdAsync(userId, recipeId);
                if (recipe is null)
                {
                    return NotFound();
                }
                return Ok(recipe);
            }
            catch (Exception ex)
            {
                // Log the exception (you can use a logging framework here)
                return StatusCode(StatusCodes.Status500InternalServerError, new { value = "Erro ao obter receita", statusCode = 500 });
            }
        }

        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> CreateRecipe([FromBody] CreateRecipeRequest request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new { value = "Token inválido ou sem ID", statusCode = 401 });
            }

            try
            {
                var recipe = await _recipeService.CreateRecipeAsync(userId, request);
                return Ok(recipe);
            }
            catch (Exception ex)
            {
                Console.Write(ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { value = "Erro ao criar receita", statusCode = 500 });
            }
        }

        [Authorize]
        [HttpPut("update")]
        public async Task<IActionResult> UpdateRecipe([FromQuery] int recipeId, [FromBody] UpdateRecipeRequest request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new { value = "Token inválido ou sem ID", statusCode = 401 });
            }

            try
            {
                await _recipeService.UpdateRecipeAsync(userId, recipeId, request);
                return NoContent(); // 204 - sucesso sem corpo de resposta, já que o service não retorna nada
            }
            catch (RecipeNotFoundException)
            {
                return NotFound();
            }
            catch (ForbiddenAccessException)
            {
                return NotFound(); // devolve 404 também, não 403 — não vaza que o ID existe
            }
        }

        [Authorize]
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteRecipe([FromQuery] int recipeId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new { value = "Token inválido ou sem ID", statusCode = 401 });
            }
            try
            {
                await _recipeService.DeleteRecipeAsync(userId, recipeId);
                return NoContent(); // 204 - sucesso sem corpo de resposta
            }
            catch (RecipeNotFoundException)
            {
                return NotFound();
            }
            catch (ForbiddenAccessException)
            {
                return NotFound(); // devolve 404 também, não 403 — não vaza que o ID existe
            }
        }
    }
}
