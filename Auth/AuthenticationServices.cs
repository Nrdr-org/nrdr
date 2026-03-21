using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace nrdr.Auth;

public sealed class StaticExternalAuthProviderCatalog : IExternalAuthProviderCatalog
{
    private static readonly IReadOnlyList<AuthProviderDefinition> Providers =
    [
        new("google", "Google", "google.com", "Continue with Google", "G"),
        new("apple", "Apple", "apple.com", "Continue with Apple", "A")
    ];

    public IReadOnlyList<AuthProviderDefinition> GetProviders() => Providers;

    public AuthProviderDefinition? Find(string key) =>
        Providers.FirstOrDefault(provider => string.Equals(provider.Key, key, StringComparison.OrdinalIgnoreCase));
}

public sealed class AuthUiConfigurationService(
    IExternalAuthProviderCatalog providerCatalog,
    IOptions<FirebaseAuthenticationOptions> options) : IAuthUiConfigurationService
{
    public AuthUiConfiguration GetConfiguration()
    {
        var settings = options.Value;
        var enabledProviders = settings.EnabledProviders
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var providers = providerCatalog
            .GetProviders()
            .Where(provider => enabledProviders.Count == 0 || enabledProviders.Contains(provider.Key))
            .ToArray();
        var webApp = settings.WebApp;

        return webApp.IsConfigured
            ? new AuthUiConfiguration(true, webApp.Clone(), providers)
            : AuthUiConfiguration.Disabled(providers);
    }
}

public sealed class FirebaseBrowserAuthSessionService(
    IJSRuntime jsRuntime,
    IExternalAuthProviderCatalog providerCatalog,
    IAuthUiConfigurationService authUiConfigurationService) : IAuthSessionService, IAsyncDisposable
{
    private readonly SemaphoreSlim initializeGate = new(1, 1);
    private IJSObjectReference? module;

    public bool IsReady { get; private set; }
    public AppUser? CurrentUser { get; private set; }
    public event Action? Changed;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (IsReady)
        {
            return;
        }

        await initializeGate.WaitAsync(cancellationToken);

        try
        {
            if (IsReady)
            {
                return;
            }

            var configuration = authUiConfigurationService.GetConfiguration();
            if (!configuration.IsEnabled || configuration.Firebase is null)
            {
                CurrentUser = null;
                IsReady = true;
                NotifyChanged();
                return;
            }

            var jsModule = await GetModuleAsync(cancellationToken);
            var browserUser = await jsModule.InvokeAsync<BrowserAuthUser?>(
                "initializeFirebase",
                cancellationToken,
                configuration.Firebase);

            CurrentUser = MapUser(browserUser);
            IsReady = true;
            NotifyChanged();
        }
        finally
        {
            initializeGate.Release();
        }
    }

    public async Task SignInAsync(string providerKey, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        var configuration = authUiConfigurationService.GetConfiguration();
        if (!configuration.IsEnabled || configuration.Firebase is null)
        {
            throw new InvalidOperationException("Firebase authentication is not configured.");
        }

        var provider = providerCatalog.Find(providerKey)
            ?? throw new InvalidOperationException($"Unsupported provider '{providerKey}'.");

        var jsModule = await GetModuleAsync(cancellationToken);
        var browserUser = await jsModule.InvokeAsync<BrowserAuthUser?>(
            "signInWithProvider",
            cancellationToken,
            configuration.Firebase,
            provider.FirebaseProviderId);

        CurrentUser = MapUser(browserUser, provider.Key)
            ?? throw new InvalidOperationException("The selected provider did not return a signed-in user.");
        NotifyChanged();
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        var configuration = authUiConfigurationService.GetConfiguration();
        if (configuration.IsEnabled && configuration.Firebase is not null)
        {
            var jsModule = await GetModuleAsync(cancellationToken);
            await jsModule.InvokeVoidAsync("signOutFromFirebase", cancellationToken, configuration.Firebase);
        }

        CurrentUser = null;
        IsReady = true;
        NotifyChanged();
    }

    public async ValueTask DisposeAsync()
    {
        initializeGate.Dispose();

        if (module is not null)
        {
            await module.DisposeAsync();
        }
    }

    private void NotifyChanged() => Changed?.Invoke();

    private async ValueTask<IJSObjectReference> GetModuleAsync(CancellationToken cancellationToken)
    {
        module ??= await jsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            cancellationToken,
            "./js/nrdr-shell.js");

        return module;
    }

    private AppUser? MapUser(BrowserAuthUser? browserUser, string? fallbackProviderKey = null)
    {
        if (browserUser is null || string.IsNullOrWhiteSpace(browserUser.UserId))
        {
            return null;
        }

        var providerKey = fallbackProviderKey ?? ResolveProviderKey(browserUser.ProviderId);
        var email = browserUser.Email?.Trim();
        var displayName = browserUser.DisplayName?.Trim();

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = !string.IsNullOrWhiteSpace(email)
                ? email.Split('@', 2, StringSplitOptions.TrimEntries)[0]
                : "User";
        }

        return new AppUser(
            browserUser.UserId,
            displayName,
            string.IsNullOrWhiteSpace(email) ? null : email,
            providerKey,
            string.IsNullOrWhiteSpace(browserUser.AvatarUrl) ? null : browserUser.AvatarUrl);
    }

    private string ResolveProviderKey(string? providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            return "external";
        }

        return providerCatalog
            .GetProviders()
            .FirstOrDefault(provider => string.Equals(provider.FirebaseProviderId, providerId, StringComparison.OrdinalIgnoreCase))
            ?.Key
            ?? "external";
    }
}
