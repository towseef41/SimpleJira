using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using SimpleJira.Contracts;
using SimpleJira.Web.Components.Pages;
using SimpleJira.Web.Services;
using Xunit;

namespace SimpleJira.Web.Tests.Pages;

public class UsersPageTests : IDisposable
{
    private readonly TestContext _ctx = new();
    private readonly FakeUsersHandler _handler = new();

    public UsersPageTests()
    {
        var http = new HttpClient(_handler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        _ctx.Services.AddSingleton<AuthService>();
        _ctx.Services.AddScoped(_ => new JiraApiClient(http, _ctx.Services.GetRequiredService<AuthService>()));
    }

    [Fact]
    public void ShowsEmptyState()
    {
        _handler.Users = Array.Empty<UserDto>();
        _ctx.Services.GetRequiredService<AuthService>().SetToken("t");
        var cut = _ctx.RenderComponent<Users>();

        cut.WaitForAssertion(() =>
            Assert.Contains("No users found.", cut.Markup));
    }

    [Fact]
    public void RendersUsersTable()
    {
        _handler.Users = new[]
        {
            new UserDto(Guid.NewGuid(), "Alice"),
            new UserDto(Guid.NewGuid(), "Bob")
        };

        _ctx.Services.GetRequiredService<AuthService>().SetToken("t");
        var cut = _ctx.RenderComponent<Users>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Alice", cut.Markup);
            Assert.Contains("Bob", cut.Markup);
        });
    }

    [Fact]
    public void ShowsErrorOnFailure()
    {
        _handler.ShouldThrow = true;

        _ctx.Services.GetRequiredService<AuthService>().SetToken("t");
        var cut = _ctx.RenderComponent<Users>();

        cut.WaitForAssertion(() =>
            Assert.Contains("error", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose() => _ctx.Dispose();

    private class FakeUsersHandler : HttpMessageHandler
    {
        public UserDto[] Users { get; set; } = Array.Empty<UserDto>();
        public bool ShouldThrow { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";

            if (request.Method == HttpMethod.Get && path.Equals("/users", StringComparison.OrdinalIgnoreCase))
            {
                if (ShouldThrow)
                    throw new HttpRequestException("error");

                return Task.FromResult(JsonResponse(Users));
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
