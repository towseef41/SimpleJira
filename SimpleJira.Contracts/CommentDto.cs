namespace SimpleJira.Contracts;

public record CommentDto(
    Guid Id,
    Guid IssueId,
    string Body,
    UserDto? Author,
    DateTime CreatedAt);
