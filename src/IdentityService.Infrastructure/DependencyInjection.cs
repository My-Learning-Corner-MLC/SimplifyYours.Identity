using IdentityService.Application;
using IdentityService.Infrastructure.Identity;
using IdentityService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;

namespace IdentityService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityServiceDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("IdentityServiceDb");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'IdentityServiceDb' is required to use Identity service persistence.");
            }

            options.UseNpgsql(connectionString);
            options.UseOpenIddict();
        });

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<IdentityServiceDbContext>()
            .AddDefaultTokenProviders();

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<IdentityServiceDbContext>();
            })
            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris("/auth/sign-in");
                options.SetTokenEndpointUris("/auth/token");
                options.SetIssuer(GetRequiredIssuer(configuration));
                options.AllowAuthorizationCodeFlow()
                    .RequireProofKeyForCodeExchange();
                options.AllowRefreshTokenFlow();
                options.AcceptAnonymousClients();
                options.RegisterScopes(
                    OpenIddictConstants.Scopes.Email,
                    OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Scopes.Roles,
                    OpenIddictConstants.Scopes.OfflineAccess);
                ConfigureTokenCredentials(options, configuration);
                options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough();
            });

        services.AddAuthentication();
        services.AddAuthorization();
        services.AddScoped<IUserAccountService, IdentityUserAccountService>();

        return services;
    }

    private static Uri GetRequiredIssuer(IConfiguration configuration)
    {
        var issuer = configuration["Auth:Issuer"];

        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException("Configuration value 'Auth:Issuer' is required.");
        }

        return new Uri(issuer, UriKind.Absolute);
    }

    private static void ConfigureTokenCredentials(
        OpenIddictServerBuilder options,
        IConfiguration configuration)
    {
        var encryptionKey = configuration["Auth:AccessTokenEncryptionKeyBase64"];

        if (!string.IsNullOrWhiteSpace(encryptionKey))
        {
            options.AddEncryptionKey(new SymmetricSecurityKey(Convert.FromBase64String(encryptionKey)));
        }
        else
        {
            options.AddDevelopmentEncryptionCertificate();
        }

        options.AddDevelopmentSigningCertificate();
    }
}
