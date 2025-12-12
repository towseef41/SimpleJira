using SimpleJira.Contracts;

public record IssueDto(
    Guid Id,
    Guid ProjectId,
    string Title,
    string? Summary,
    int? StoryPoints,
    IssueStatus Status,
    UserDto? Assignee,
    UserDto? Reporter,
    int CommentsCount,
    List<Guid> LinkedIssueIds
);
