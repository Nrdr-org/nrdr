using System.Text.Json.Serialization;

namespace nrdr.Auth;

public sealed class FirebaseAuthenticationOptions
{
    public const string SectionName = "Authentication:Firebase";

    public FirebaseWebAppOptions WebApp { get; set; } = new();
}

public sealed class FirebaseWebAppOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string AuthDomain { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
    public string MessagingSenderId { get; set; } = string.Empty;
    public string StorageBucket { get; set; } = string.Empty;
    public string MeasurementId { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(AuthDomain) &&
        !string.IsNullOrWhiteSpace(ProjectId) &&
        !string.IsNullOrWhiteSpace(AppId);

    public FirebaseWebAppOptions Clone() => new()
    {
        ApiKey = ApiKey,
        AuthDomain = AuthDomain,
        ProjectId = ProjectId,
        AppId = AppId,
        MessagingSenderId = MessagingSenderId,
        StorageBucket = StorageBucket,
        MeasurementId = MeasurementId
    };
}

public sealed record AuthProviderDefinition(
    string Key,
    string DisplayName,
    string FirebaseProviderId,
    string ButtonLabel,
    string BadgeText);

public sealed record AuthUiConfiguration(
    bool IsEnabled,
    FirebaseWebAppOptions? Firebase,
    IReadOnlyList<AuthProviderDefinition> Providers)
{
    public static AuthUiConfiguration Disabled(IReadOnlyList<AuthProviderDefinition> providers) => new(false, null, providers);
}

public sealed record AppUser(
    string UserId,
    string DisplayName,
    string? Email,
    string ProviderKey,
    string? PictureUrl);

internal sealed class BrowserAuthUser
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("providerId")]
    public string? ProviderId { get; set; }

    [JsonPropertyName("avatarUrl")]
    public string? AvatarUrl { get; set; }
}
