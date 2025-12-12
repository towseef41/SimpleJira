using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleJira.ApiService.Data;
using SimpleJira.ApiService.Models;
using SimpleJira.Contracts;

namespace SimpleJira.ApiService.Controllers;

[ApiController]
[Route("projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly JiraDbContext _db;

    public ProjectsController(JiraDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetProjects(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId)
    {
        var query = _db.Projects
            .Include(p => p.Category)
            .Include(p => p.Lead)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, pattern) ||
                EF.Functions.ILike(p.Key, pattern));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => EF.Property<Guid?>(p, "CategoryId") == categoryId);
        }

        var projects = await query
            .OrderBy(p => p.Name)
            .Select(p => new ProjectDto(
                p.Id,
                p.Name,
                p.Key,
                p.Type,
                p.Avatar,
                p.Category == null ? null : new CategoryDto(p.Category.Id, p.Category.Name),
                p.Lead == null ? null : new UserDto(p.Lead.Id, p.Lead.Name)))
            .ToListAsync();

        return Ok(projects);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Project name is required.");
        if (request.Name.Length > 200)
            return BadRequest("Project name too long (max 200).");

        User? lead = null;
        if (request.LeadId is { } leadId)
        {
            lead = await _db.Users.FindAsync(leadId);
            if (lead is null)
                return BadRequest("Lead user not found.");
        }

        Category? category = null;
        if (request.CategoryId is { } catId)
        {
            category = await _db.Categories.FindAsync(catId);
            if (category is null)
                return BadRequest("Category not found.");
        }

        var resolvedId = request.Id is { } id && id != Guid.Empty
            ? id
            : Guid.NewGuid();

        var resolvedKey = NormalizeKey(
            string.IsNullOrWhiteSpace(request.Key)
                ? request.Name
                : request.Key);

        if (string.IsNullOrWhiteSpace(resolvedKey) || resolvedKey.Length < 2 || resolvedKey.Length > 10)
            return BadRequest("Project key must be 2-10 characters.");

        var keyExists = await _db.Projects.AnyAsync(p => p.Key == resolvedKey);
        if (keyExists)
            return Conflict("Project key already exists.");

        if (!string.IsNullOrWhiteSpace(request.Type) && request.Type.Length > 100)
            return BadRequest("Project type too long (max 100).");
        if (!string.IsNullOrWhiteSpace(request.Avatar) && request.Avatar.Length > 500)
            return BadRequest("Avatar URL too long (max 500).");

        var project = new Project
        {
            Id = resolvedId,
            Name = request.Name.Trim(),
            Key = resolvedKey,
            Type = string.IsNullOrWhiteSpace(request.Type) ? "Software" : request.Type.Trim(),
            Avatar = string.IsNullOrWhiteSpace(request.Avatar)
                ? resolvedKey[..Math.Min(resolvedKey.Length, 2)]
                : request.Avatar.Trim(),
            Lead = lead,
            Category = category
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        var dto = new ProjectDto(
            project.Id,
            project.Name,
            project.Key,
            project.Type,
            project.Avatar,
            category is null ? null : new CategoryDto(category.Id, category.Name),
            lead is null ? null : new UserDto(lead.Id, lead.Name));

        return CreatedAtAction(nameof(GetProjects), new { id = project.Id }, dto);
    }

    private static string NormalizeKey(string value)
    {
        var trimmed = value.Trim();
        var key = new string(trimmed
            .Where(char.IsLetterOrDigit)
            .ToArray());

        if (string.IsNullOrWhiteSpace(key))
            key = trimmed;

        key = key.ToUpperInvariant();

        if (key.Length > 10)
            key = key[..10];

        return key;
    }
}
