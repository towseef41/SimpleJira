using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SimpleJira.ApiService.Controllers;
using Xunit;

namespace SimpleJira.ApiService.Tests.Controllers;

public class AuthControllerTests
{
    private static IConfiguration BuildConfig(string key) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = key,
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience"
            })
            .Build();

    [Fact]
    public void IssueToken_ReturnsToken_ForValidUsername()
    {
        var controller = new AuthController(BuildConfig(new string('k', 40)));

        var result = controller.IssueToken(new DevLoginRequest("tester"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var token = ok.Value?.GetType().GetProperty("token")?.GetValue(ok.Value)?.ToString();
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void IssueToken_RejectsMissingUsername()
    {
        var controller = new AuthController(BuildConfig(new string('k', 40)));

        var result = controller.IssueToken(new DevLoginRequest(""));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Username is required.", badRequest.Value);
    }

    [Fact]
    public void IssueToken_WithShortKeyThrows()
    {
        var controller = new AuthController(BuildConfig("short-key"));

        Assert.Throws<ArgumentOutOfRangeException>(() => controller.IssueToken(new DevLoginRequest("tester")));
    }
}
