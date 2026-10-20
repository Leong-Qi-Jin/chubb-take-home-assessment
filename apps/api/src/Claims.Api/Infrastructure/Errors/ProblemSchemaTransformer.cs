using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Claims.Api.Infrastructure.Errors;

/// <summary>
/// Describes the Problem Details extensions in OpenAPI so generated client types can match on <c>code</c> and read the conflict payload.
/// </summary>
public sealed class ProblemSchemaTransformer : IOpenApiDocumentTransformer
{
    private const string ErrorCodeSchema = "ErrorCode";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();
        var schemas = document.Components.Schemas;
        schemas[ErrorCodeSchema] = new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Enum = [.. ApiProblems.Codes.Select(code => (JsonNode)code)],
        };

        foreach (var name in new[] { "ProblemDetails", "HttpValidationProblemDetails" })
        {
            if (schemas.TryGetValue(name, out var schema) && schema is OpenApiSchema problem)
            {
                problem.Properties ??= new Dictionary<string, IOpenApiSchema>();
                problem.Properties["code"] = new OpenApiSchemaReference(ErrorCodeSchema, document);
            }
        }

        if (schemas.TryGetValue("ProblemDetails", out var problemSchema)
            && problemSchema is OpenApiSchema problemDetails
            && schemas.ContainsKey("ClaimDetailsResponse"))
        {
            // Always sent with claim_modified; sent with claim_already_assigned only when the take lost a race at save time.
            problemDetails.Properties!["claim"] = new OpenApiSchemaReference("ClaimDetailsResponse", document);
        }

        return Task.CompletedTask;
    }
}
