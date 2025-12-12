using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SimpleJira.ApiService.Infrastructure;

public class AllowAllAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    #pragma warning disable CS0618 // ISystemClock obsolete in favor of TimeProvider on options
    public AllowAllAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }
    #pragma warning restore CS0618

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "dev-user"),
            new Claim(ClaimTypes.NameIdentifier, "dev-user")
        }, Scheme.Name);

        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
