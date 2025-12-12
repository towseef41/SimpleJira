using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using SimpleJira.Web.Components.Pages;
using SimpleJira.Web.Services;
using Xunit;

namespace SimpleJira.Web.Tests.Pages;

public class LoginTests : IDisposable
{
    private readonly TestContext _ctx = new();

    public LoginTests()
    {
        var handler = new FakeAuthHandler();
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
    public void ShowsValidationWhenUsernameMissing()
    {
        var cut = _ctx.RenderComponent<Login>();
        cut.Find("button").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Username is required.", cut.Markup));
    }

    [Fact]
    public void ShowsErrorOnFailedLogin()
    {
        var handler = _ctx.Services.GetRequiredService<FakeAuthHandler>();
        handler.StatusCode = HttpStatusCode.Unauthorized;

        var cut = _ctx.RenderComponent<Login>();
        cut.Find("input").Input("demo");
        cut.Find("button").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Login failed.", cut.Markup));
    }

    [Fact]
    public void NavigatesOnSuccess()
    {
        var handler = _ctx.Services.GetRequiredService<FakeAuthHandler>();
        handler.StatusCode = HttpStatusCode.OK;

        var nav = (TestNavManager)_ctx.Services.GetRequiredService<NavigationManager>();
        var cut = _ctx.RenderComponent<Login>();
        cut.Find("input").Input("demo");
        cut.Find("button").Click();

        cut.WaitForAssertion(() =>
            Assert.Equal("/projects", nav.LastUri));
    }

    public void Dispose() => _ctx.Dispose();

    private class FakeAuthHandler : HttpMessageHandler
    {
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.Unauthorized;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (request.Method == HttpMethod.Post && path.EndsWith("auth/token", StringComparison.OrdinalIgnoreCase))
            {
                var response = new HttpResponseMessage(StatusCode);
                if (StatusCode == HttpStatusCode.OK)
                {
                    response.Content = new StringContent(
                        JsonSerializer.Serialize(new { token = "devtoken" }),
                        Encoding.UTF8,
                        "application/json");
                }
                return Task.FromResult(response);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
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
            Uri = uri;
        }
    }
}
