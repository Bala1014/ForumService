using Microsoft.AspNetCore.Mvc;
using Racinglazing.Forum.Application.Features.Categories;

namespace Racinglazing.Forum.Api.Controllers;

[Route("api/v1/categories")]
public class CategoriesController(ICategoryService categories) : ApiControllerBase
{
    /// <summary>Returns all active categories.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Data(await categories.GetAllAsync(ct));

    /// <summary>Returns a category and its forums.</summary>
    [HttpGet("{categoryId:guid}")]
    public async Task<IActionResult> GetById(Guid categoryId, CancellationToken ct)
        => Data(await categories.GetByIdAsync(categoryId, ct));
}
