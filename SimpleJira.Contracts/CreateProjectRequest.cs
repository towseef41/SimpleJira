namespace SimpleJira.Contracts;

public record CreateProjectRequest(
    Guid? Id,
    string Name,
    string? Key,
    string? Type,
    string? Avatar,
    Guid? CategoryId,
    Guid? LeadId);
