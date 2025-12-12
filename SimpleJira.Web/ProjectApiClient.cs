using System.Net.Http.Json;

namespace SimpleJira.Web;

public class ProjectsApiClient(HttpClient http)
{
    public async Task<IReadOnlyList<Project>> GetProjectsAsync()
    {
        return await http.GetFromJsonAsync<IReadOnlyList<Project>>("/projects")
               ?? [];
    }
}

public record Project(Guid Id, string Name);
