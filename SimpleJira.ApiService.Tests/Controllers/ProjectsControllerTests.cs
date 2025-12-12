using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleJira.ApiService.Controllers;
using SimpleJira.ApiService.Data;
using SimpleJira.ApiService.Models;
using SimpleJira.Contracts;
using Xunit;
using System.Collections.Generic;

namespace SimpleJira.ApiService.Tests.Controllers;

public class ProjectsControllerTests
{
    private static JiraDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<JiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var ctx = new JiraDbContext(options);
        ctx.Database.EnsureDeleted();
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public async Task CreateProject_RejectsMissingName()
    {
        using var ctx = BuildContext();
        var controller = new ProjectsController(ctx);

        var result = await controller.CreateProject(
            new CreateProjectRequest(
                null,
                "",
                "AA",
                null,
                null,
                null,
                null));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Project name is required.", badRequest.Value);
    }

    [Fact]
    public async Task CreateProject_RejectsDuplicateKey()
    {
        using var ctx = BuildContext();
        ctx.Projects.Add(new Project
        {
            Id = Guid.NewGuid(),
            Name = "Existing",
            Key = "EXIST",
            Type = "Software",
            Avatar = "EX"
        });
        await ctx.SaveChangesAsync();

        var controller = new ProjectsController(ctx);
        var result = await controller.CreateProject(
            new CreateProjectRequest(
                null,
                "Another",
                "exist",
                null,
                null,
                null,
                null));

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task CreateProject_RejectsInvalidCategory()
    {
        using var ctx = BuildContext();
        var controller = new ProjectsController(ctx);

        var result = await controller.CreateProject(
            new CreateProjectRequest(
                null,
                "With Category",
                "CAT",
                null,
                null,
                Guid.NewGuid(),
                null));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Category not found.", badRequest.Value);
    }

    [Fact]
    public async Task CreateProject_KeyTooShort()
    {
        using var ctx = BuildContext();
        var controller = new ProjectsController(ctx);

        var result = await controller.CreateProject(
            new CreateProjectRequest(
                null,
                "Tiny Key",
                "A",
                null,
                null,
                null,
                null));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Project key must be 2-10 characters.", badRequest.Value);
    }

    [Fact]
    public async Task CreateProject_RejectsOversizedAvatar()
    {
        using var ctx = BuildContext();
        var controller = new ProjectsController(ctx);
        var longAvatar = new string('x', 501);

        var result = await controller.CreateProject(
            new CreateProjectRequest(
                null,
                "Big Avatar",
                "BIGAVA",
                null,
                longAvatar,
                null,
                null));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Avatar URL too long (max 500).", badRequest.Value);
    }

    [Fact]
    public async Task CreateProject_RejectsInvalidLead()
    {
        using var ctx = BuildContext();
        var controller = new ProjectsController(ctx);

        var result = await controller.CreateProject(
            new CreateProjectRequest(
                null,
                "Has Lead",
                "HLEAD",
                null,
                null,
                null,
                Guid.NewGuid()));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Lead user not found.", badRequest.Value);
    }

    [Fact]
    public async Task CreateProject_RejectsTooLongName()
    {
        using var ctx = BuildContext();
        var controller = new ProjectsController(ctx);
        var longName = new string('n', 201);

        var result = await controller.CreateProject(
            new CreateProjectRequest(
                null,
                longName,
                "LONGN",
                null,
                null,
                null,
                null));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Project name too long (max 200).", badRequest.Value);
    }

    [Fact]
    public async Task CreateProject_TruncatesLongKey()
    {
        using var ctx = BuildContext();
        var controller = new ProjectsController(ctx);

        var result = await controller.CreateProject(
            new CreateProjectRequest(
                null,
                "Long Key",
                "ABCDEFGHIJK",
                null,
                null,
                null,
                null));

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var dto = Assert.IsType<ProjectDto>(created.Value);
        Assert.Equal(10, dto.Key.Length);
    }

    [Fact]
    public async Task GetProjects_FiltersBySearchAndCategory()
    {
        using var ctx = BuildContext();
        var software = ctx.Categories.First();
        var serviceDesk = ctx.Categories.Last();

        ctx.Projects.AddRange(
            new Project { Id = Guid.NewGuid(), Name = "Searchable Alpha", Key = "S1", Type = "Software", Avatar = "S", Category = software },
            new Project { Id = Guid.NewGuid(), Name = "Beta", Key = "BETA", Type = "Software", Avatar = "B", Category = serviceDesk }
        );
        await ctx.SaveChangesAsync();

        var controller = new ProjectsController(ctx);
        var result = await controller.GetProjects(null, software.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dtos = Assert.IsAssignableFrom<IEnumerable<ProjectDto>>(ok.Value);
        Assert.Single(dtos);
        Assert.Equal("S1", dtos.First().Key);
    }

    [Fact]
    public async Task CreateProject_NormalizesKeyAndDefaultsFields()
    {
        using var ctx = BuildContext();
        var controller = new ProjectsController(ctx);

        var result = await controller.CreateProject(
            new CreateProjectRequest(
                null,
                "Alpha Project",
                "alpha project",
                null,
                null,
                null,
                null));

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var dto = Assert.IsType<ProjectDto>(created.Value);

        Assert.Equal("ALPHAPROJE", dto.Key);
        Assert.Equal("Software", dto.Type);
        Assert.False(string.IsNullOrWhiteSpace(dto.Avatar));

        var persisted = await ctx.Projects.SingleAsync(p => p.Id == dto.Id);
        Assert.Equal(dto.Key, persisted.Key);
    }
}
