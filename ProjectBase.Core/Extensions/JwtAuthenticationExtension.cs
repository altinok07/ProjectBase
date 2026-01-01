using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProjectBase.Core.Results;
using ProjectBase.Core.Security.BasicAuth;
using ProjectBase.Core.Security.Jwt;
using ProjectBase.Core.Security.Models;
using System.Security.Claims;
using System.Text;

namespace ProjectBase.Core.Extensions;

public static class JwtAuthenticationExtension
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("JWT");
        var tokenOptions = jwtSection.Get<TokenSettings>() ?? throw new ArgumentException("JWT token options cannot be null.");

        // Token üretiminde ve diğer yerlerde kullanılabilmesi için Options + servis kaydı
        services.Configure<TokenSettings>(jwtSection);
        services.AddSingleton<ITokenSettings>(sp => sp.GetRequiredService<IOptions<TokenSettings>>().Value);

        // Hem interface hem de concrete type inject edilebilsin diye self registration
        services.AddSingleton<JwtTokenGenerator>();
        services.AddSingleton<IJwtTokenGenerator>(sp => sp.GetRequiredService<JwtTokenGenerator>());

        // BasicAuth (opsiyonel) - config üzerinden kullanıcı doğrulama
        var basicSection = configuration.GetSection("BasicAuth");
        services.Configure<BasicAuthSettings>(basicSection);
        services.AddSingleton<IBasicAuthCredentialValidator, ConfigBasicAuthCredentialValidator>();

        const string smartScheme = "JWT_OR_BASIC";

        services.AddAuthentication(options =>
        {
            // [Authorize] için otomatik seçim: "Authorization: Basic ..." => Basic, aksi halde JWT
            options.DefaultAuthenticateScheme = smartScheme;
            options.DefaultChallengeScheme = smartScheme;
            options.DefaultScheme = smartScheme;
        })
            .AddPolicyScheme(smartScheme, "JWT or Basic", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var header = context.Request.Headers.Authorization.ToString();
                    if (header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
                        return BasicAuthenticationDefaults.AuthenticationScheme;

                    // default: Bearer
                    return JwtBearerDefaults.AuthenticationScheme;
                };
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // Development ortamında false, production'da true olmalı
                // Reverse proxy (nginx, IIS) HTTPS sağlıyorsa false da olabilir
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenOptions.AccessTokenSecurityKey)),

                    ValidateIssuer = true,
                    ValidIssuer = tokenOptions.Issuer,

                    ValidateAudience = true,
                    ValidAudience = tokenOptions.Audience,

                    ValidateLifetime = true,
                    // Sunucular arası saat farkı için tolerans (5 dakika)
                    ClockSkew = TimeSpan.FromMinutes(5),

                    RequireSignedTokens = true,

                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },

                    RequireExpirationTime = true,

                    // Handler tarafında ClaimTypes.Role ürettiğimiz için role mapping net olsun
                    RoleClaimType = ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    // Token doğrulama sırasında exception olduysa da (signature/issuer/audience/expiry),
                    // token hiç gelmediyse de en sonda Challenge tetiklenir. Body'yi sadece burada yazalım.
                    OnChallenge = context =>
                    {
                        context.HandleResponse(); // default 401 metnini bastır

                        if (context.Response.HasStarted)
                            return Task.CompletedTask;

                        var isAuthFailure = context.AuthenticateFailure != null;
                        var message = isAuthFailure
                            ? "Token doğrulaması başarısız"
                            : "Geçerli bir token sağlayın";

                        var payload = Result.Fail(ResultType.Unauthorized, message);

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsJsonAsync(payload);
                    },

                    // Authentication başarısız olduğunda pipeline'a devam etsin, Challenge yazsın
                    OnAuthenticationFailed = context =>
                    {
                        return Task.CompletedTask;
                    },

                    OnForbidden = context =>
                    {
                        if (context.Response.HasStarted)
                            return Task.CompletedTask;

                        var payload = Result.Fail(ResultType.Forbidden, "Bu işlemi yapmaya yetkiniz yok");
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsJsonAsync(payload);
                    }
                };

            });

        services.AddAuthentication()
            .AddScheme<BasicAuthenticationOptions, BasicAuthenticationHandler>(
                BasicAuthenticationDefaults.AuthenticationScheme,
                _ => { });

        return services;
    }
}