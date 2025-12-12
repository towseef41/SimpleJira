using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using SimpleJira.Contracts;
using SimpleJira.Web.Components.Pages;
using SimpleJira.Web.Services;
using Xunit;

namespace SimpleJira.Web.Tests.Pages;

public class ProjectsPageTests : IDisposable
{
    private readonly TestContext _ctx = new();

    public ProjectsPageTests()
    {
        var handler = new FakeProjectHandler();
        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        _ctx.Services.AddSingleton(handler);
        _ctx.Services.AddSingleton<AuthService>();
        _ctx.Services.AddSingleton<NavigationManager, TestNavManager>();
        _ctx.Services.AddScoped(_ => new JiraApiClient(http, _ctx.Services.GetRequiredService<AuthService>()));
    }

    [Fact]
    public void ShowsEmptyMessageWhenNoProjects()
    {
        var handler = _ctx.Services.GetRequiredService<FakeProjectHandler>();
        _ctx.Services.GetRequiredService<AuthService>().SetToken("t");
        handler.Payload = Array.Empty<ProjectDto>();

        var cut = _ctx.RenderComponent<Projects>();

        cut.WaitForAssertion(() =>
            Assert.Contains("No projects yet.", cut.Markup));
    }

    [Fact]
    public void RendersProjectRow()
    {
        var handler = _ctx.Services.GetRequiredService<FakeProjectHandler>();
        _ctx.Services.GetRequiredService<AuthService>().SetToken("t");
        var projectId = Guid.NewGuid();
        handler.Payload = new[]
        {
            new ProjectDto(projectId, "Alpha", "ALP", "Software", "https://example.com/img.png",
                new CategoryDto(Guid.NewGuid(), "Software"),
                new UserDto(Guid.NewGuid(), "Jane Doe"))
        };

        var cut = _ctx.RenderComponent<Projects>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Alpha", cut.Markup);
            Assert.Contains("Software", cut.Markup);
            Assert.Contains("Jane Doe", cut.Markup);
        });
    }

    [Fact]
    public void NavigatesWhenRowClicked()
    {
        var handler = _ctx.Services.GetRequiredService<FakeProjectHandler>();
        _ctx.Services.GetRequiredService<AuthService>().SetToken("t");
        var projectId = Guid.NewGuid();
        handler.Payload = new[]
        {
            new ProjectDto(projectId, "Alpha", "ALP", "Software", string.Empty, null, null)
        };

        var nav = (TestNavManager)_ctx.Services.GetRequiredService<NavigationManager>();
        var cut = _ctx.RenderComponent<Projects>();

        cut.WaitForAssertion(() =>
        {
            var row = cut.Find("tbody tr");
            row.Click();
            Assert.EndsWith($"/projects/{projectId}", nav.LastUri);
        });
    }

    public void Dispose() => _ctx.Dispose();

    private class FakeProjectHandler : HttpMessageHandler
    {
        public ProjectDto[] Payload { get; set; } = Array.Empty<ProjectDto>();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";

            if (request.Method == HttpMethod.Get && path.Equals("/projects", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(JsonResponse(Payload));
            }

            if (request.Method == HttpMethod.Get && path.EndsWith("categories", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(JsonResponse(Array.Empty<CategoryDto>()));
            }

            if (request.Method == HttpMethod.Get && path.EndsWith("users", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(JsonResponse(Array.Empty<UserDto>()));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static HttpResponseMessage JsonResponse<T>(T payload) =>
            new(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
    }

    private class TestNavManager : NavigationManager
    {
        public string? LastUri { get; private set; }

        public TestNavManager()
        {
            Initialize("http://localhost/", "http://localhost/");
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            LastUri = uri;
            Uri = ToAbsoluteUri(uri).ToString();
        }
    }
}
