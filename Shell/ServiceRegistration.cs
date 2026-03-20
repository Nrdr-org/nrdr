using Microsoft.JSInterop;
using nrdr.Auth;
using nrdr.Games;

namespace nrdr.Shell;

public interface IShellInteropService
{
    Task PostActionAsync(string gameSlug, string actionId, string side, CancellationToken cancellationToken = default);
}

public static class ServiceRegistration
{
    public static IServiceCollection AddNrdrShell(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddNrdrAuthentication(configuration);
        services.AddScoped<IShellGameCatalogService, EmbeddedShellGameCatalogService>();
        services.AddScoped<IShellInteropService, JsShellInteropService>();
        return services;
    }
}

public sealed class JsShellInteropService(IJSRuntime jsRuntime) : IShellInteropService, IAsyncDisposable
{
    private IJSObjectReference? module;

    public async Task PostActionAsync(string gameSlug, string actionId, string side, CancellationToken cancellationToken = default)
    {
        var jsModule = await GetModuleAsync(cancellationToken);
        await jsModule.InvokeVoidAsync("postShellAction", cancellationToken, gameSlug, actionId, side);
    }

    public async ValueTask DisposeAsync()
    {
        if (module is not null)
        {
            await module.DisposeAsync();
        }
    }

    private async ValueTask<IJSObjectReference> GetModuleAsync(CancellationToken cancellationToken)
    {
        module ??= await jsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            cancellationToken,
            "./js/nrdr-shell.js");

        return module;
    }
}
