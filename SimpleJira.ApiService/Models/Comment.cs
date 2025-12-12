namespace SimpleJira.ApiService.Models;

public class Comment
{
    public Guid Id { get; set; }
    public Guid IssueId { get; set; }
    public Issue? Issue { get; set; }

    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? AuthorId { get; set; }
    public User? Author { get; set; }
}
