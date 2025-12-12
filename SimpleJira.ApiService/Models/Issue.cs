using SimpleJira.Contracts;

namespace SimpleJira.ApiService.Models;

public class Issue
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public int? StoryPoints { get; set; }
    public IssueStatus Status { get; set; }

    public Guid? AssigneeId { get; set; }
    public User? Assignee { get; set; }

    public Guid? ReporterId { get; set; }
    public User? Reporter { get; set; }

    public List<Comment> Comments { get; set; } = new();
    public List<IssueLink> Links { get; set; } = new();
}
