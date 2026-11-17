namespace MyAcademy_PortfolioProject.Infrastructure;

public static class PublicLinks
{
    public static bool IsWeb(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

    public static bool IsAsset(string? value) => !string.IsNullOrWhiteSpace(value)
        && ((value.StartsWith('/') && !value.StartsWith("//") && !value.Contains('\\') && !value.Contains("..")) || IsWeb(value));
}
