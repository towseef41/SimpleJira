using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using SimpleJira.Contracts;
using SimpleJira.Web.Components.Shared;
using SimpleJira.Web.Services;
using Xunit;
using Microsoft.AspNetCore.Components;

namespace SimpleJira.Web.Tests.Components;

public class CreateProjectDialogTests : IDisposable
{
    private readonly TestContext _ctx = new();

    public CreateProjectDialogTests()
    {
        var handler = new FakeApiMessageHandler();
        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        _ctx.Services.AddSingleton(handler);
        _ctx.Services.AddSingleton<AuthService>();
        _ctx.Services.AddScoped(_ => new JiraApiClient(http, _ctx.Services.GetRequiredService<AuthService>()));
    }

    [Fact]
    public async Task ShowsValidation_WhenNameMissing()
    {
        var cut = _ctx.RenderComponent<CreateProjectDialog>();
        await cut.InvokeAsync(() => cut.Instance.Open());

        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Project name is required.", cut.Markup));
    }

    [Fact]
    public async Task ClosesOnSuccess()
    {
        var handler = _ctx.Services.GetRequiredService<FakeApiMessageHandler>();
        handler.ProjectCreateStatusCode = HttpStatusCode.Created;

        var created = false;
        var cut = _ctx.RenderComponent<CreateProjectDialog>(p =>
            p.Add(c => c.OnCreated, EventCallback.Factory.Create(this, () => created = true)));

        await cut.InvokeAsync(() => cut.Instance.Open());

        cut.Find("input[placeholder=\"Project name\"]").Change("Alpha");
        cut.Find("input[placeholder=\"Project key\"]").Change("AL");
        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() => Assert.True(created));
        cut.MarkupMatches(string.Empty); // dialog closed
    }

    [Fact]
    public async Task ShowsApiErrorOnFailure()
    {
        var handler = _ctx.Services.GetRequiredService<FakeApiMessageHandler>();
        handler.ProjectCreateStatusCode = HttpStatusCode.InternalServerError;

        var cut = _ctx.RenderComponent<CreateProjectDialog>();
        await cut.InvokeAsync(() => cut.Instance.Open());

        cut.Find("input[placeholder=\"Project name\"]").Change("Alpha");
        cut.Find("input[placeholder=\"Project key\"]").Change("AL");
        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Failed to create project (500", cut.Markup));
    }

    public void Dispose() => _ctx.Dispose();

    private class FakeApiMessageHandler : HttpMessageHandler
    {
        public HttpStatusCode ProjectCreateStatusCode { get; set; } = HttpStatusCode.Created;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";

            if (request.Method == HttpMethod.Get && path.EndsWith("categories", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(JsonResponse(new[]
                {
                    new CategoryDto(Guid.NewGuid(), "Software")
                }));
            }

            if (request.Method == HttpMethod.Get && path.EndsWith("users", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(JsonResponse(new[]
                {
                    new UserDto(Guid.NewGuid(), "Jane Doe")
                }));
            }

            if (request.Method == HttpMethod.Post && path.EndsWith("projects", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(ProjectCreateStatusCode));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static HttpResponseMessage JsonResponse<T>(T payload) =>
            new(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
    }
}
