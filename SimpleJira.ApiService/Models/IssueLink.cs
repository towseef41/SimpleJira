namespace SimpleJira.ApiService.Models;

public class IssueLink
{
    public Guid Id { get; set; }
    public Guid IssueId { get; set; }
    public Guid LinkedIssueId { get; set; }

    public Issue? Issue { get; set; }
}
