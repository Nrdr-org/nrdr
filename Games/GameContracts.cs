namespace nrdr.Games;

public interface IShellGameCatalogService
{
    Task<ShellGame> GetCurrentGameAsync(CancellationToken cancellationToken = default);
}

public sealed record ShellGame(
    string Title,
    string Slug,
    string FrameSrc,
    string ViewportLabel,
    string TabIconHref,
    string TabIconType)
{
    public static ShellGame CreateFallback(string slug)
    {
        var safeSlug = string.IsNullOrWhiteSpace(slug) ? "game" : slug.Trim().Trim('/');
        var safeTitle = HumanizeSlug(safeSlug);
        return new ShellGame(
            safeTitle,
            safeSlug,
            $"{safeSlug}/index.html",
            $"{safeTitle} game viewport",
            BuildTabIconDataUri(safeTitle),
            "image/svg+xml");
    }

    public static ShellGame Create(string title, string slug)
    {
        var safeSlug = string.IsNullOrWhiteSpace(slug) ? "game" : slug.Trim().Trim('/');
        var safeTitle = string.IsNullOrWhiteSpace(title) ? HumanizeSlug(safeSlug) : title.Trim();
        return new ShellGame(
            safeTitle,
            safeSlug,
            $"{safeSlug}/index.html",
            $"{safeTitle} game viewport",
            BuildTabIconDataUri(safeTitle),
            "image/svg+xml");
    }

    public static string HumanizeSlug(string value)
    {
        return string.Join(
            " ",
            value
                .Split(['-', '_', ' '], StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => char.ToUpperInvariant(segment[0]) + segment[1..].ToLowerInvariant()));
    }

    private static string BuildTabIconDataUri(string title)
    {
        var monogram = title[0].ToString().ToUpperInvariant();
        var svg = $$"""
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64">
          <defs>
            <linearGradient id="bg" x1="0" y1="0" x2="1" y2="1">
              <stop offset="0%" stop-color="#1a2a3c" />
              <stop offset="100%" stop-color="#09111a" />
            </linearGradient>
            <linearGradient id="gold" x1="0" y1="0" x2="1" y2="1">
              <stop offset="0%" stop-color="#f3d08a" />
              <stop offset="100%" stop-color="#b98a34" />
            </linearGradient>
          </defs>
          <rect width="64" height="64" fill="#05080c" />
          <path d="M32 4 54 14v19c0 13-8.8 22.7-22 27C18.8 55.7 10 46 10 33V14Z" fill="url(#bg)" stroke="url(#gold)" stroke-width="3"/>
          <path d="M32 14 43 19v9.5c0 6.6-4.3 11.8-11 15.1-6.7-3.3-11-8.5-11-15.1V19Z" fill="none" stroke="#f0c984" stroke-width="3"/>
          <text x="32" y="40" text-anchor="middle" font-size="22" font-family="Avenir Next, Trebuchet MS, sans-serif" fill="#f0c984">{{monogram}}</text>
        </svg>
        """;

        return $"data:image/svg+xml,{Uri.EscapeDataString(svg)}";
    }
}
