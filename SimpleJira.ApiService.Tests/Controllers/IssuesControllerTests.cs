using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleJira.ApiService.Controllers;
using SimpleJira.ApiService.Data;
using SimpleJira.ApiService.Models;
using SimpleJira.Contracts;
using Xunit;

namespace SimpleJira.ApiService.Tests.Controllers;

public class IssuesControllerTests
{
    private static JiraDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<JiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var ctx = new JiraDbContext(options);
        ctx.Database.EnsureDeleted();
        ctx.Database.EnsureCreated();

        ctx.Projects.Add(new Project
        {
            Id = SeedProjectId,
            Name = "Alpha",
            Key = "ALPHA",
            Type = "Software",
            Avatar = "A"
        });
        ctx.Users.Add(new User { Id = SeedUserId, Name = "Assignee One" });
        ctx.Users.Add(new User { Id = SeedReporterId, Name = "Reporter One" });
        ctx.SaveChanges();

        return ctx;
    }

    private static Guid SeedProjectId => new("11111111-1111-1111-1111-111111111111");
    private static Guid SeedUserId => new("22222222-2222-2222-2222-222222222222");
    private static Guid SeedReporterId => new("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task CreateIssue_RejectsNegativeStoryPoints()
    {
        using var ctx = BuildContext();
        var controller = new IssuesController(ctx);

        var result = await controller.CreateIssue(
            SeedProjectId,
            new CreateIssueRequest(
                "Bug",
                null,
                -1,
                null,
                null,
                null));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Story points cannot be negative.", badRequest.Value);
    }

    [Fact]
    public async Task UpdateStatus_PersistsNewStatus()
    {
        using var ctx = BuildContext();
        var issue = new Issue
        {
            Id = Guid.NewGuid(),
            ProjectId = SeedProjectId,
            Title = "Sample",
            Summary = "Sample",
            Status = IssueStatus.Todo
        };
        ctx.Issues.Add(issue);
        await ctx.SaveChangesAsync();

        var controller = new IssuesController(ctx);
        var result = await controller.UpdateStatus(issue.Id, new UpdateIssueStatusRequest(IssueStatus.InProgress));

        Assert.IsType<NoContentResult>(result);

        var updated = await ctx.Issues.FindAsync(issue.Id);
        Assert.Equal(IssueStatus.InProgress, updated!.Status);
    }

    [Fact]
    public async Task CreateIssue_RejectsUnknownAssignee()
    {
        using var ctx = BuildContext();
        var controller = new IssuesController(ctx);

        var result = await controller.CreateIssue(
            SeedProjectId,
            new CreateIssueRequest(
                "Bug",
                null,
                null,
                Guid.NewGuid(),
                null,
                null));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Assignee not found.", badRequest.Value);
    }

    [Fact]
    public async Task AssignIssue_SetsAssignee()
    {
        using var ctx = BuildContext();
        var issue = new Issue
        {
            Id = Guid.NewGuid(),
            ProjectId = SeedProjectId,
            Title = "Unassigned",
            Status = IssueStatus.Todo
        };
        ctx.Issues.Add(issue);
        await ctx.SaveChangesAsync();

        var controller = new IssuesController(ctx);
        var result = await controller.AssignIssue(issue.Id, new AssignIssueRequest(SeedUserId));

        Assert.IsType<NoContentResult>(result);
        var updated = await ctx.Issues.FindAsync(issue.Id);
        Assert.Equal(SeedUserId, updated!.AssigneeId);
    }

    [Fact]
    public async Task AddComment_RejectsEmptyBody()
    {
        using var ctx = BuildContext();
        var issue = new Issue
        {
            Id = Guid.NewGuid(),
            ProjectId = SeedProjectId,
            Title = "Comment target",
            Status = IssueStatus.Todo
        };
        ctx.Issues.Add(issue);
        await ctx.SaveChangesAsync();

        var controller = new IssuesController(ctx);
        var result = await controller.AddComment(issue.Id, new AddCommentRequest("", null));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Comment body is required.", badRequest.Value);
    }

    [Fact]
    public async Task LinkIssue_RejectsSelfLink()
    {
        using var ctx = BuildContext();
        var issue = new Issue
        {
            Id = Guid.NewGuid(),
            ProjectId = SeedProjectId,
            Title = "Self",
            Status = IssueStatus.Todo
        };
        ctx.Issues.Add(issue);
        await ctx.SaveChangesAsync();

        var controller = new IssuesController(ctx);
        var result = await controller.LinkIssue(issue.Id, new LinkIssueRequest(issue.Id));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Cannot link an issue to itself.", badRequest.Value);
    }

    [Fact]
    public async Task LinkIssue_CreatesReciprocalLinks()
    {
        using var ctx = BuildContext();
        var issueA = new Issue
        {
            Id = Guid.NewGuid(),
            ProjectId = SeedProjectId,
            Title = "A",
            Status = IssueStatus.Todo
        };
        var issueB = new Issue
        {
            Id = Guid.NewGuid(),
            ProjectId = SeedProjectId,
            Title = "B",
            Status = IssueStatus.Todo
        };
        ctx.Issues.AddRange(issueA, issueB);
        await ctx.SaveChangesAsync();

        var controller = new IssuesController(ctx);
        var result = await controller.LinkIssue(issueA.Id, new LinkIssueRequest(issueB.Id));

        Assert.IsType<NoContentResult>(result);
        var links = ctx.IssueLinks.ToList();
        Assert.Equal(2, links.Count);
        Assert.Contains(links, l => l.IssueId == issueA.Id && l.LinkedIssueId == issueB.Id);
        Assert.Contains(links, l => l.IssueId == issueB.Id && l.LinkedIssueId == issueA.Id);
    }

    [Fact]
    public async Task LinkIssue_DuplicateCallDoesNotAddMoreLinks()
    {
        using var ctx = BuildContext();
        var issueA = new Issue { Id = Guid.NewGuid(), ProjectId = SeedProjectId, Title = "A", Status = IssueStatus.Todo };
        var issueB = new Issue { Id = Guid.NewGuid(), ProjectId = SeedProjectId, Title = "B", Status = IssueStatus.Todo };
        ctx.Issues.AddRange(issueA, issueB);
        await ctx.SaveChangesAsync();

        var controller = new IssuesController(ctx);
        await controller.LinkIssue(issueA.Id, new LinkIssueRequest(issueB.Id));
        await controller.LinkIssue(issueA.Id, new LinkIssueRequest(issueB.Id));

        var links = ctx.IssueLinks.ToList();
        Assert.Equal(2, links.Count); // only reciprocal pair
    }

    [Fact]
    public async Task LinkIssue_UnknownTargetReturnsBadRequest()
    {
        using var ctx = BuildContext();
        var issue = new Issue { Id = Guid.NewGuid(), ProjectId = SeedProjectId, Title = "A", Status = IssueStatus.Todo };
        ctx.Issues.Add(issue);
        await ctx.SaveChangesAsync();

        var controller = new IssuesController(ctx);
        var result = await controller.LinkIssue(issue.Id, new LinkIssueRequest(Guid.NewGuid()));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AddComment_PersistsCommentAndAuthor()
    {
        using var ctx = BuildContext();
        var issue = new Issue { Id = Guid.NewGuid(), ProjectId = SeedProjectId, Title = "Comment", Status = IssueStatus.Todo };
        ctx.Issues.Add(issue);
        await ctx.SaveChangesAsync();

        var controller = new IssuesController(ctx);
        var result = await controller.AddComment(issue.Id, new AddCommentRequest("Hello", SeedReporterId));

        var ok = Assert.IsType<OkObjectResult>(result);
        var commentDto = Assert.IsType<CommentDto>(ok.Value);
        Assert.Equal("Hello", commentDto.Body);
        Assert.Equal(SeedReporterId, commentDto.Author?.Id);
    }

    [Fact]
    public async Task UpdateIssue_RejectsTooLongSummary()
    {
        using var ctx = BuildContext();
        var issue = new Issue { Id = Guid.NewGuid(), ProjectId = SeedProjectId, Title = "Edit", Status = IssueStatus.Todo };
        ctx.Issues.Add(issue);
        await ctx.SaveChangesAsync();

        var controller = new IssuesController(ctx);
        var longSummary = new string('s', 4001);
        var result = await controller.UpdateIssue(issue.Id, new UpdateIssueRequest("Title", longSummary, null));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Summary too long (max 4000).", badRequest.Value);
    }

    [Fact]
    public async Task UpdateIssue_HappyPathUpdatesFields()
    {
        using var ctx = BuildContext();
        var issue = new Issue { Id = Guid.NewGuid(), ProjectId = SeedProjectId, Title = "Old", Summary = "OldS", Status = IssueStatus.Todo, StoryPoints = 1 };
        ctx.Issues.Add(issue);
        await ctx.SaveChangesAsync();

        var controller = new IssuesController(ctx);
        var result = await controller.UpdateIssue(issue.Id, new UpdateIssueRequest("New Title", "New Summary", 5));

        Assert.IsType<NoContentResult>(result);
        var updated = await ctx.Issues.FindAsync(issue.Id);
        Assert.Equal("New Title", updated!.Title);
        Assert.Equal("New Summary", updated.Summary);
        Assert.Equal(5, updated.StoryPoints);
    }

    [Fact]
    public async Task GetIssuesForProject_ReturnsCountsAndUsers()
    {
        using var ctx = BuildContext();
        var issue = new Issue { Id = Guid.NewGuid(), ProjectId = SeedProjectId, Title = "With Data", Status = IssueStatus.Todo, AssigneeId = SeedUserId, ReporterId = SeedReporterId };
        ctx.Issues.Add(issue);
        ctx.Comments.Add(new Comment { Id = Guid.NewGuid(), IssueId = issue.Id, Body = "c1", CreatedAt = DateTime.UtcNow });
        ctx.IssueLinks.Add(new IssueLink { Id = Guid.NewGuid(), IssueId = issue.Id, LinkedIssueId = Guid.NewGuid() });
        await ctx.SaveChangesAsync();

        var controller = new IssuesController(ctx);
        var result = await controller.GetIssuesForProject(SeedProjectId);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dtos = Assert.IsAssignableFrom<IEnumerable<IssueDto>>(ok.Value);
        var dto = Assert.Single(dtos);
        Assert.Equal(1, dto.CommentsCount);
        Assert.Single(dto.LinkedIssueIds);
        Assert.Equal(SeedUserId, dto.Assignee?.Id);
        Assert.Equal(SeedReporterId, dto.Reporter?.Id);
    }
}
