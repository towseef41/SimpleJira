using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleJira.ApiService.Data;
using SimpleJira.Contracts;

namespace SimpleJira.ApiService.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly JiraDbContext _db;

    public UsersController(JiraDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.Users
            .OrderBy(u => u.Name)
            .Select(u => new UserDto(u.Id, u.Name))
            .ToListAsync();

        return Ok(users);
    }
}
