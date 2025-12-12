namespace SimpleJira.Contracts;

public record CreateIssueRequest(
    string Title,
    string? Summary,
    int? StoryPoints,
    Guid? AssigneeId,
    Guid? ReporterId,
    List<Guid>? LinkedIssueIds);
