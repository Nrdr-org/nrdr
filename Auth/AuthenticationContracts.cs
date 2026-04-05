namespace nrdr.Auth;

public interface IExternalAuthProviderCatalog
{
    IReadOnlyList<AuthProviderDefinition> GetProviders();
    AuthProviderDefinition? Find(string key);
}

public interface IAuthUiConfigurationService
{
    AuthUiConfiguration GetConfiguration();
}

public interface IAuthSessionService
{
    bool IsReady { get; }
    AppUser? CurrentUser { get; }
    event Action? Changed;

    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task SignInAsync(string providerKey, CancellationToken cancellationToken = default);
    Task SignInWithEmailAsync(string email, string password, CancellationToken cancellationToken = default);
    Task SignUpWithEmailAsync(string email, string password, CancellationToken cancellationToken = default);
    Task SendPasswordResetAsync(string email, CancellationToken cancellationToken = default);
    Task SignOutAsync(CancellationToken cancellationToken = default);
}
