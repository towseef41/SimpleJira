using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleJira.ApiService.Data;
using SimpleJira.ApiService.Models;
using SimpleJira.Contracts;

namespace SimpleJira.ApiService.Controllers;

[ApiController]
[Authorize]
public class IssuesController : ControllerBase
{
    private readonly JiraDbContext _db;

    public IssuesController(JiraDbContext db)
    {
        _db = db;
    }

    [HttpGet("projects/{projectId:guid}/issues")]
    public async Task<IActionResult> GetIssuesForProject(Guid projectId)
    {
        return Ok(await _db.Issues
            .Include(i => i.Assignee)
            .Include(i => i.Reporter)
            .Include(i => i.Comments)
            .Include(i => i.Links)
            .Where(i => i.ProjectId == projectId)
            .Select(i => new IssueDto(
                i.Id,
                i.ProjectId,
                i.Title,
                i.Summary,
                i.StoryPoints,
                i.Status,
                i.Assignee == null ? null : new UserDto(i.Assignee.Id, i.Assignee.Name),
                i.Reporter == null ? null : new UserDto(i.Reporter.Id, i.Reporter.Name),
                i.Comments.Count,
                i.Links.Select(l => l.LinkedIssueId).ToList()
            ))
            .ToListAsync());

    }

    [HttpPost("projects/{projectId:guid}/issues")]
    public async Task<IActionResult> CreateIssue(
        Guid projectId,
        [FromBody] CreateIssueRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Title is required.");

        if (request.Title.Length > 200)
            return BadRequest("Title too long (max 200).");

        if (request.Summary?.Length > 4000)
            return BadRequest("Summary too long (max 4000).");

        if (request.StoryPoints is < 0)
            return BadRequest("Story points cannot be negative.");

        User? assignee = null;
        if (request.AssigneeId is { } assigneeId)
        {
            assignee = await _db.Users.FindAsync(assigneeId);
            if (assignee is null)
                return BadRequest("Assignee not found.");
        }

        User? reporter = null;
        if (request.ReporterId is { } reporterId)
        {
            reporter = await _db.Users.FindAsync(reporterId);
            if (reporter is null)
                return BadRequest("Reporter not found.");
        }

        var issue = new Issue
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = request.Title,
            Summary = string.IsNullOrWhiteSpace(request.Summary) ? request.Title : request.Summary,
            StoryPoints = request.StoryPoints,
            Status = Contracts.IssueStatus.Todo,
            Assignee = assignee,
            Reporter = reporter
        };

        _db.Issues.Add(issue);

        if (request.LinkedIssueIds is not null)
        {
            foreach (var linkedId in request.LinkedIssueIds.Distinct())
            {
                var exists = await _db.Issues.AnyAsync(x => x.Id == linkedId);
                if (!exists) continue;

                _db.IssueLinks.Add(new IssueLink
                {
                    Id = Guid.NewGuid(),
                    IssueId = issue.Id,
                    LinkedIssueId = linkedId
                });
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new IssueDto(
            issue.Id,
            issue.ProjectId,
            issue.Title,
            issue.Summary,
            issue.StoryPoints,
            Contracts.IssueStatus.Todo,
            assignee is null ? null : new UserDto(assignee.Id, assignee.Name),
            reporter is null ? null : new UserDto(reporter.Id, reporter.Name),
            0,
            new List<Guid>()));
    }
    [HttpPatch("issues/{issueId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid issueId,
        [FromBody] UpdateIssueStatusRequest request)
    {
        var issue = await _db.Issues.FindAsync(issueId);
        if (issue is null)
            return NotFound();

        // Direct mutation (tracked entity)
        issue.Status = request.Status;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("issues/{issueId:guid}/assignee")]
    public async Task<IActionResult> AssignIssue(
        Guid issueId,
        [FromBody] AssignIssueRequest request)
    {
        var issue = await _db.Issues.FindAsync(issueId);
        if (issue is null) return NotFound();

        issue.AssigneeId = request.UserId;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("issues/{issueId:guid}/comments")]
    public async Task<IActionResult> AddComment(
        Guid issueId,
        [FromBody] AddCommentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
            return BadRequest("Comment body is required.");

        var issue = await _db.Issues.FindAsync(issueId);
        if (issue is null) return NotFound();

        User? author = null;
        if (request.AuthorId is { } authorId)
        {
            author = await _db.Users.FindAsync(authorId);
            if (author is null)
                return BadRequest("Author not found.");
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            IssueId = issueId,
            Body = request.Body.Trim(),
            CreatedAt = DateTime.UtcNow,
            Author = author
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        return Ok(new CommentDto(
            comment.Id,
            comment.IssueId,
            comment.Body,
            author is null ? null : new UserDto(author.Id, author.Name),
            comment.CreatedAt));
    }

    [HttpPost("issues/{issueId:guid}/links")]
    public async Task<IActionResult> LinkIssue(
        Guid issueId,
        [FromBody] LinkIssueRequest request)
    {
        if (issueId == request.TargetIssueId)
            return BadRequest("Cannot link an issue to itself.");

        var issueExists = await _db.Issues.AnyAsync(i => i.Id == issueId);
        if (!issueExists) return NotFound();

        var targetExists = await _db.Issues.AnyAsync(i => i.Id == request.TargetIssueId);
        if (!targetExists) return BadRequest("Target issue not found.");

        var already = await _db.IssueLinks.AnyAsync(l =>
            (l.IssueId == issueId && l.LinkedIssueId == request.TargetIssueId) ||
            (l.IssueId == request.TargetIssueId && l.LinkedIssueId == issueId));
        if (already) return NoContent();

        _db.IssueLinks.Add(new IssueLink
        {
            Id = Guid.NewGuid(),
            IssueId = issueId,
            LinkedIssueId = request.TargetIssueId
        });

        // optional reciprocal
        _db.IssueLinks.Add(new IssueLink
        {
            Id = Guid.NewGuid(),
            IssueId = request.TargetIssueId,
            LinkedIssueId = issueId
        });

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("issues/{issueId:guid}")]
    public async Task<IActionResult> UpdateIssue(
        Guid issueId,
        [FromBody] UpdateIssueRequest request)
    {
        var issue = await _db.Issues.FindAsync(issueId);
        if (issue is null) return NotFound();

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Title is required.");

        if (request.Title.Length > 200)
            return BadRequest("Title too long (max 200).");

        if (request.Summary?.Length > 4000)
            return BadRequest("Summary too long (max 4000).");

        if (request.StoryPoints is < 0)
            return BadRequest("Story points cannot be negative.");

        issue.Title = request.Title.Trim();
        issue.Summary = string.IsNullOrWhiteSpace(request.Summary)
            ? issue.Title
            : request.Summary.Trim();
        issue.StoryPoints = request.StoryPoints;

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
