using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Claims.Api.Features.Claims;

namespace Claims.Api.IntegrationTests.Infrastructure;

public sealed record ApiResponse(HttpStatusCode Status, string? ETag, JsonElement Body)
{
    public ClaimDetailsResponse Claim => Body.Deserialize<ClaimDetailsResponse>(ApiClient.Json)!;

    public string? ErrorCode => Body.TryGetProperty("code", out var code) ? code.GetString() : null;
}

/// <summary>
/// Sends JSON requests and captures the status, ETag, and body together for concise assertions.
/// </summary>
public static class ApiClient
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static object MotorClaim(string currency = "SGD") => new
    {
        type = "Motor",
        policyNumber = "POL-123456",
        incident = new
        {
            occurredAt = DateTimeOffset.UtcNow.AddDays(-2),
            location = "Orchard Road, Singapore",
            description = "Rear-ended at a junction.",
        },
        motor = new { vehicleRegistration = "SBA1234A" },
        property = (object?)null,
        currency,
    };

    public static Task<ApiResponse> ApiGetAsync(this HttpClient client, string path) =>
        client.SendJsonAsync(HttpMethod.Get, path);

    public static Task<ApiResponse> ApiPostAsync(this HttpClient client, string path, object? body = null, string? ifMatch = null) =>
        client.SendJsonAsync(HttpMethod.Post, path, body, ifMatch);

    public static Task<ApiResponse> ApiPutAsync(this HttpClient client, string path, object body, string? ifMatch = null) =>
        client.SendJsonAsync(HttpMethod.Put, path, body, ifMatch);

    public static async Task<ApiResponse> SendJsonAsync(
        this HttpClient client,
        HttpMethod method,
        string path,
        object? body = null,
        string? ifMatch = null)
    {
        using var request = new HttpRequestMessage(method, $"/api/v1{path}");

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        if (ifMatch is not null)
        {
            request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        }

        using var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        var json = content.Length > 0 ? JsonSerializer.Deserialize<JsonElement>(content) : default;

        return new ApiResponse(response.StatusCode, response.Headers.ETag?.ToString(), json);
    }

    /// <summary>
    /// Submits a motor claim as the client's claimant and returns the created response.
    /// </summary>
    public static async Task<ApiResponse> SubmitClaimAsync(this HttpClient claimant, object? body = null)
    {
        var response = await claimant.ApiPostAsync("/claims", body ?? MotorClaim());
        return response.Status == HttpStatusCode.Created
            ? response
            : throw new InvalidOperationException($"Submitting a claim failed with {response.Status}: {response.Body}");
    }
}
