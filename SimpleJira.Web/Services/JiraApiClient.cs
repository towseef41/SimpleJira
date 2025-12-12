using System.Net.Http.Json;
using SimpleJira.Contracts;

namespace SimpleJira.Web.Services;

public class JiraApiClient
{
    private readonly HttpClient _http;
    private readonly AuthService _auth;

    public JiraApiClient(HttpClient http, AuthService auth)
    {
        _http = http;
        _auth = auth;
        ApplyToken();
    }

    // --------------------
    // Projects
    // --------------------

    public async Task<List<ProjectDto>> GetProjectsAsync(string? search = null, Guid? categoryId = null)
    {
        var url = "projects";
        var query = new List<string>();

        if (!string.IsNullOrWhiteSpace(search))
            query.Add($"search={Uri.EscapeDataString(search)}");
        if (categoryId.HasValue)
            query.Add($"categoryId={categoryId}");

        if (query.Any())
            url += "?" + string.Join("&", query);

        return await _http.GetFromJsonAsync<List<ProjectDto>>(url) ?? [];
    }

    public async Task<HttpResponseMessage> CreateProjectAsync(CreateProjectRequest request)
        => await _http.PostAsJsonAsync("projects", request);

    // Backwards compat helper until UI is expanded
    public Task<HttpResponseMessage> CreateProjectAsync(Guid id, string name) =>
        CreateProjectAsync(new CreateProjectRequest(
            id,
            name,
            null,
            null,
            null,
            null,
            null));

    // --------------------
    // Categories
    // --------------------

    public async Task<List<CategoryDto>> GetCategoriesAsync()
        => await _http.GetFromJsonAsync<List<CategoryDto>>("categories") ?? [];

    public async Task CreateCategoryAsync(string name)
        => await _http.PostAsJsonAsync("categories", new CreateCategoryRequest(name));

    // --------------------
    // Issues
    // --------------------

    public async Task<List<IssueDto>> GetIssuesAsync(Guid projectId)
        => await _http.GetFromJsonAsync<List<IssueDto>>(
            $"projects/{projectId}/issues") ?? [];

    public async Task CreateIssueAsync(
        Guid projectId,
        string title,
        string? summary = null,
        int? storyPoints = null,
        Guid? assigneeId = null,
        Guid? reporterId = null,
        List<Guid>? linkedIssueIds = null)
        => await _http.PostAsJsonAsync(
            $"projects/{projectId}/issues",
            new CreateIssueRequest(
                title,
                summary,
                storyPoints,
                assigneeId,
                reporterId,
                linkedIssueIds));

    public async Task<HttpResponseMessage> UpdateIssueStatusAsync(Guid issueId, IssueStatus status)
        => await _http.PatchAsJsonAsync(
            $"issues/{issueId}/status",
            new UpdateIssueStatusRequest(status));

    public async Task<HttpResponseMessage> UpdateIssueAsync(Guid issueId, UpdateIssueRequest request)
        => await _http.PatchAsJsonAsync(
            $"issues/{issueId}",
            request);

    public async Task<List<UserDto>> GetUsersAsync()
        => await _http.GetFromJsonAsync<List<UserDto>>("users") ?? [];

    public async Task AssignIssueAsync(Guid issueId, Guid? userId)
        => await _http.PatchAsJsonAsync(
            $"issues/{issueId}/assignee",
            new AssignIssueRequest(userId));

    public async Task<string?> GetDevTokenAsync(string username)
    {
        var resp = await _http.PostAsJsonAsync("auth/token", new { username });
        if (!resp.IsSuccessStatusCode) return null;

        var payload = await resp.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        if (payload is not null && payload.TryGetValue("token", out var token))
        {
            _auth.SetToken(token);
            ApplyToken();
            return token;
        }

        return null;
    }

    public void ApplyToken()
    {
        _http.DefaultRequestHeaders.Authorization = null;
        if (_auth.HasToken)
        {
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _auth.Token);
        }
    }
}
