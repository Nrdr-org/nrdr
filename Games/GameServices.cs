using System.Text.RegularExpressions;

namespace nrdr.Games;

public sealed class EmbeddedShellGameCatalogService(HttpClient httpClient) : IShellGameCatalogService
{
    private static readonly Regex WasmBundlePattern = new(
        "(?<stem>[A-Za-z0-9_-]+)-[A-Fa-f0-9]{8,}_bg\\.wasm",
        RegexOptions.Compiled);

    private ShellGame? cachedGame;

    public async Task<ShellGame> GetCurrentGameAsync(CancellationToken cancellationToken = default)
    {
        if (cachedGame is not null)
        {
            return cachedGame;
        }

        const string slug = "canonical";
        var title = await InferTitleAsync(slug, cancellationToken);
        cachedGame = ShellGame.Create(title, slug);
        return cachedGame;
    }

    private async Task<string> InferTitleAsync(string slug, CancellationToken cancellationToken)
    {
        try
        {
            var markup = await httpClient.GetStringAsync($"{slug}/index.html", cancellationToken);
            var match = WasmBundlePattern.Match(markup);
            if (match.Success)
            {
                return ShellGame.HumanizeSlug(match.Groups["stem"].Value);
            }
        }
        catch
        {
        }

        return ShellGame.HumanizeSlug(slug);
    }
}
