namespace SimpleJira.Contracts;

public record UpdateIssueRequest(
    string Title,
    string? Summary,
    int? StoryPoints);
