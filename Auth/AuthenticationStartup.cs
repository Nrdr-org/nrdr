using Microsoft.Extensions.Options;

namespace nrdr.Auth;

public static class AuthenticationStartup
{
    public static IServiceCollection AddNrdrAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions();
        services.Configure<FirebaseAuthenticationOptions>(configuration.GetSection(FirebaseAuthenticationOptions.SectionName));
        services.AddSingleton<IExternalAuthProviderCatalog, StaticExternalAuthProviderCatalog>();
        services.AddSingleton<IAuthUiConfigurationService, AuthUiConfigurationService>();
        services.AddScoped<IAuthSessionService, FirebaseBrowserAuthSessionService>();
        return services;
    }
}
