using System.Net.Http.Json;

namespace SimpleJira.Web;

public class IssuesApiClient(HttpClient http)
{
    public async Task<IReadOnlyList<Issue>> GetIssuesForProjectAsync(Guid projectId)
    {
        return await http.GetFromJsonAsync<IReadOnlyList<Issue>>(
            $"/projects/{projectId}/issues") ?? [];
    }

    public async Task UpdateIssueStatusAsync(Guid issueId, string status)
    {
        await http.PatchAsJsonAsync(
            $"/issues/{issueId}/status",
            new { status });
    }
}

public record Issue(Guid Id, string Title, string Status);
