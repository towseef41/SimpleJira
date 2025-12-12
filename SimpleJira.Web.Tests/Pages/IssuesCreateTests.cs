using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using SimpleJira.Contracts;
using SimpleJira.Web.Components.Pages;
using SimpleJira.Web.Services;
using Xunit;
using AngleSharp.Dom;

namespace SimpleJira.Web.Tests.Pages;

public class IssuesCreateTests : IDisposable
{
    private readonly TestContext _ctx = new();

    public IssuesCreateTests()
    {
        var handler = new FakeIssueHandler();
        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        _ctx.Services.AddSingleton(handler);
        _ctx.Services.AddSingleton<AuthService>();
        _ctx.Services.AddScoped(_ => new JiraApiClient(http, _ctx.Services.GetRequiredService<AuthService>()));
    }

    [Fact]
    public void RequiresTitle()
    {
        var cut = _ctx.RenderComponent<Issues>(p => p.Add(c => c.ProjectId, Guid.NewGuid()));
        cut.Find("button.btn-primary").Click(); // open create dialog
        cut.Find("button.btn-primary").Click(); // attempt create

        cut.WaitForAssertion(() =>
            Assert.Contains("Title", cut.Markup));
    }

    [Fact]
    public void ShowsErrorWhenCreateFails()
    {
        var handler = _ctx.Services.GetRequiredService<FakeIssueHandler>();
        handler.CreateStatus = HttpStatusCode.InternalServerError;
        var projectId = handler.ProjectId;

        _ctx.Services.GetRequiredService<AuthService>().SetToken("t");
        var cut = _ctx.RenderComponent<Issues>(p => p.Add(c => c.ProjectId, projectId));
        cut.Find("button.btn-primary").Click(); // open dialog
        cut.Find("input[placeholder=\"Issue title\"]").Input("Bug");
        FindButtonByText(cut, "Create").Click(); // create

        cut.WaitForAssertion(() =>
            Assert.Contains("CreateIssue failed", cut.Markup));
    }

    [Fact]
    public void ClosesOnSuccess()
    {
        var handler = _ctx.Services.GetRequiredService<FakeIssueHandler>();
        handler.CreateStatus = HttpStatusCode.OK;
        var projectId = handler.ProjectId;

        var cut = _ctx.RenderComponent<Issues>(p => p.Add(c => c.ProjectId, projectId));
        cut.Find("button.btn-primary").Click(); // open dialog
        cut.Find("input[placeholder=\"Issue title\"]").Input("Bug");
        FindButtonByText(cut, "Create").Click(); // create

        cut.WaitForAssertion(() =>
            Assert.Empty(cut.FindAll("h5").Where(h => h.TextContent.Contains("Create Issue"))));
    }

    public void Dispose() => _ctx.Dispose();

    private class FakeIssueHandler : HttpMessageHandler
    {
        public HttpStatusCode CreateStatus { get; set; } = HttpStatusCode.OK;
        public Guid ProjectId { get; } = Guid.NewGuid();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";

            if (request.Method == HttpMethod.Get && path.EndsWith("users", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(JsonResponse(Array.Empty<UserDto>()));
            }

            if (request.Method == HttpMethod.Get && path.StartsWith("/projects", StringComparison.OrdinalIgnoreCase) && path.EndsWith("issues"))
            {
                return Task.FromResult(JsonResponse(Array.Empty<IssueDto>()));
            }

            if (request.Method == HttpMethod.Get && path.Equals("/projects", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(JsonResponse(new[]
                {
                    new ProjectDto(ProjectId, "Demo", "DEMO", "Software", "D", null, null)
                }));
            }

            if (request.Method == HttpMethod.Post && path.Contains($"/projects/{ProjectId}/issues"))
            {
                if (CreateStatus != HttpStatusCode.OK && CreateStatus != HttpStatusCode.Created)
                {
                    throw new HttpRequestException($"CreateIssue failed {(int)CreateStatus}");
                }
                return Task.FromResult(new HttpResponseMessage(CreateStatus));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static HttpResponseMessage JsonResponse<T>(T payload) =>
            new(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
    }

    private static IElement FindButtonByText(IRenderedFragment cut, string text) =>
        cut.FindAll("button").First(b => b.TextContent.Contains(text, StringComparison.OrdinalIgnoreCase));
}
