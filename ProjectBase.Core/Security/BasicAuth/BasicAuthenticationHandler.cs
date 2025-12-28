using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectBase.Core.Results;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace ProjectBase.Core.Security.BasicAuth;

public sealed class BasicAuthenticationHandler(
    IOptionsMonitor<BasicAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IBasicAuthCredentialValidator validator
) : AuthenticationHandler<BasicAuthenticationOptions>(options, logger, encoder)
{
    private readonly IBasicAuthCredentialValidator _validator = validator;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeaderValues))
            return AuthenticateResult.NoResult();

        var authHeader = authHeaderValues.ToString();
        if (string.IsNullOrWhiteSpace(authHeader))
            return AuthenticateResult.NoResult();

        if (!AuthenticationHeaderValue.TryParse(authHeader, out var headerValue))
            return AuthenticateResult.Fail("Invalid Authorization header");

        if (!string.Equals(headerValue.Scheme, "Basic", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        if (string.IsNullOrWhiteSpace(headerValue.Parameter))
            return AuthenticateResult.Fail("Missing credentials");

        string decoded;
        try
        {
            var credentialBytes = Convert.FromBase64String(headerValue.Parameter);
            decoded = Encoding.UTF8.GetString(credentialBytes);
        }
        catch
        {
            return AuthenticateResult.Fail("Invalid Base64 credentials");
        }

        var separatorIndex = decoded.IndexOf(':');
        if (separatorIndex <= 0)
            return AuthenticateResult.Fail("Invalid credentials format");

        var username = decoded[..separatorIndex];
        var password = decoded[(separatorIndex + 1)..];

        var validation = await _validator.ValidateAsync(username, password, Context.RequestAborted);
        if (validation is null)
            return AuthenticateResult.Fail("Invalid username or password");

        var identity = new ClaimsIdentity(validation.Claims, BasicAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, BasicAuthenticationDefaults.AuthenticationScheme);
        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (Response.HasStarted)
            return Task.CompletedTask;

        // realm kaldırıldı: istemciye sadece Basic challenge gönderiyoruz
        Response.Headers.WWWAuthenticate = "Basic charset=\"UTF-8\"";
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/json";

        var payload = Result.Fail(ResultType.Unauthorized, "Basic authentication gerekli");
        return Response.WriteAsJsonAsync(payload);
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        if (Response.HasStarted)
            return Task.CompletedTask;

        Response.StatusCode = StatusCodes.Status403Forbidden;
        Response.ContentType = "application/json";

        var payload = Result.Fail(ResultType.Forbidden, "Bu işlemi yapmaya yetkiniz yok");
        return Response.WriteAsJsonAsync(payload);
    }
}
