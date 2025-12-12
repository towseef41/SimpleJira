using SimpleJira.Contracts;
namespace SimpleJira.ApiService.Models;

public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;

    public string Key { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;

    public User? Lead { get; set; }
    public Category? Category { get; set; }
}
