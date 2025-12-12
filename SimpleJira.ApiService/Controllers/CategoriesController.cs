using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleJira.ApiService.Data;
using SimpleJira.ApiService.Models;
using SimpleJira.Contracts;

namespace SimpleJira.ApiService.Controllers;

[ApiController]
[Route("categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly JiraDbContext _db;

    public CategoriesController(JiraDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _db.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name))
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Category name is required.");

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim()
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategories), new { id = category.Id }, new CategoryDto(category.Id, category.Name));
    }
}
