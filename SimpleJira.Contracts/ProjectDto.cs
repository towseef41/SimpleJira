namespace SimpleJira.Contracts;

public record ProjectDto(
    Guid Id,
    string Name,
    string Key,
    string Type,
    string Avatar,
    CategoryDto? Category,
    UserDto? Lead);
