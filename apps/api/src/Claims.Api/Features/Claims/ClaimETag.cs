using Microsoft.Net.Http.Headers;

namespace Claims.Api.Features.Claims;

/// <summary>
/// Exposes the claim's row version as a strong ETag for optimistic concurrency.
/// </summary>
public static class ClaimETag
{
    public static string Format(byte[] version) => $"\"{Convert.ToHexString(version)}\"";

    public static void Write(HttpResponse response, byte[] version) =>
        response.Headers.ETag = Format(version);

    /// <summary>
    /// Uses strong comparison, as RFC 9110 requires for If-Match. <c>*</c> matches any version; an unparseable header matches none.
    /// </summary>
    public static bool Matches(IList<EntityTagHeaderValue> ifMatch, byte[] version)
    {
        var current = Format(version);
        return ifMatch.Any(tag =>
            tag.Equals(EntityTagHeaderValue.Any)
            || (!tag.IsWeak && string.Equals(tag.Tag.Value, current, StringComparison.OrdinalIgnoreCase)));
    }
}
