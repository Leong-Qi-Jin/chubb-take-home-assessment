using System.Text.Json;
using Claims.Api.Domain.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Api.Infrastructure.Errors;

/// <summary>
/// The complete set of stable error codes returned in the Problem Details <c>code</c> field.
/// </summary>
public static class ErrorCodes
{
    public const string ValidationFailed = "validation_failed";
    public const string CallerNotIdentified = "caller_not_identified";
    public const string RoleNotAllowed = "role_not_allowed";
    public const string NotAssignedOfficer = "not_assigned_officer";
    public const string ClaimNotFound = "claim_not_found";
    public const string InformationRequestNotFound = "information_request_not_found";
    public const string ClaimStatusNotAllowed = "claim_status_not_allowed";
    public const string ClaimAlreadyAssigned = "claim_already_assigned";
    public const string InformationRequestNotOpen = "information_request_not_open";
    public const string EstimatedLiabilityRequired = "estimated_liability_required";
    public const string ClaimModified = "claim_modified";
    public const string IfMatchRequired = "if_match_required";
    public const string InternalError = "internal_error";
}

public static class ApiProblems
{
    private const string CodeExtension = "code";

    private static readonly Dictionary<string, string> Titles = new()
    {
        [ErrorCodes.ValidationFailed] = "The request is invalid",
        [ErrorCodes.CallerNotIdentified] = "The caller could not be identified",
        [ErrorCodes.RoleNotAllowed] = "The caller's role cannot do this",
        [ErrorCodes.NotAssignedOfficer] = "Only the assigned officer can do this",
        [ErrorCodes.ClaimNotFound] = "Claim not found",
        [ErrorCodes.InformationRequestNotFound] = "Information request not found",
        [ErrorCodes.ClaimStatusNotAllowed] = "Action not allowed in the claim's status",
        [ErrorCodes.ClaimAlreadyAssigned] = "Claim already assigned",
        [ErrorCodes.InformationRequestNotOpen] = "Information request is not open",
        [ErrorCodes.EstimatedLiabilityRequired] = "Estimated liability required",
        [ErrorCodes.ClaimModified] = "The claim has changed",
        [ErrorCodes.IfMatchRequired] = "If-Match header required",
        [ErrorCodes.InternalError] = "An unexpected error occurred",
    };

    private static string TypeFor(string code) => $"https://errors.example/{code.Replace('_', '-')}";

    /// <summary>
    /// Every error code, for publishing in OpenAPI.
    /// </summary>
    public static IEnumerable<string> Codes => Titles.Keys;

    public static ProblemDetails CreateDetails(int status, string code, string detail) =>
        new()
        {
            Type = TypeFor(code),
            Title = Titles[code],
            Status = status,
            Detail = detail,
            Extensions = { [CodeExtension] = code },
        };

    public static ProblemHttpResult Create(
        int status,
        string code,
        string detail,
        IEnumerable<KeyValuePair<string, object?>>? extensions = null)
    {
        var problem = CreateDetails(status, code, detail);

        foreach (var (key, value) in extensions ?? [])
        {
            problem.Extensions[key] = value;
        }

        return TypedResults.Problem(problem);
    }

    public static ValidationProblem ValidationFailed(IDictionary<string, string[]> errors) =>
        TypedResults.ValidationProblem(
            errors,
            title: Titles[ErrorCodes.ValidationFailed],
            type: TypeFor(ErrorCodes.ValidationFailed),
            extensions: [new(CodeExtension, ErrorCodes.ValidationFailed)]);

    public static ProblemHttpResult FromWorkflowError(ClaimWorkflowException exception)
    {
        var (status, code) = exception.Error switch
        {
            ClaimWorkflowError.NotAssignedOfficer => (StatusCodes.Status403Forbidden, ErrorCodes.NotAssignedOfficer),
            ClaimWorkflowError.ClaimAlreadyAssigned => (StatusCodes.Status409Conflict, ErrorCodes.ClaimAlreadyAssigned),
            ClaimWorkflowError.ClaimStatusNotAllowed => (StatusCodes.Status409Conflict, ErrorCodes.ClaimStatusNotAllowed),
            ClaimWorkflowError.InformationRequestNotFound =>
                (StatusCodes.Status404NotFound, ErrorCodes.InformationRequestNotFound),
            ClaimWorkflowError.InformationRequestNotOpen =>
                (StatusCodes.Status409Conflict, ErrorCodes.InformationRequestNotOpen),
            ClaimWorkflowError.EstimatedLiabilityRequired =>
                (StatusCodes.Status409Conflict, ErrorCodes.EstimatedLiabilityRequired),
            _ => throw new ArgumentOutOfRangeException(nameof(exception), exception.Error, null),
        };

        return Create(status, code, exception.Message);
    }

    /// <summary>
    /// Declares the Problem Details responses an endpoint can return, for OpenAPI.
    /// </summary>
    public static RouteHandlerBuilder ProducesProblems(this RouteHandlerBuilder builder, params int[] statusCodes)
    {
        foreach (var statusCode in statusCodes)
        {
            builder.ProducesProblem(statusCode);
        }

        return builder;
    }

    /// <summary>
    /// Adds a code to framework-generated problems, such as request binding failures and unhandled exceptions, and gives every field error a camelCase key matching the JSON property path.
    /// </summary>
    public static void Configure(ProblemDetailsOptions options) =>
        options.CustomizeProblemDetails = context =>
        {
            var problem = context.ProblemDetails;

            if (problem is HttpValidationProblemDetails validationProblem)
            {
                CamelCaseErrorKeys(validationProblem.Errors);
            }

            // Framework 404, 405, and 415 responses for unknown routes or unsupported requests stay uncoded.
            var code = problem.Status switch
            {
                StatusCodes.Status400BadRequest => ErrorCodes.ValidationFailed,
                StatusCodes.Status500InternalServerError => ErrorCodes.InternalError,
                _ => null,
            };

            if (code is not null && problem.Extensions.TryAdd(CodeExtension, code))
            {
                problem.Type = TypeFor(code);
                problem.Title = Titles[code];
            }
        };

    private static void CamelCaseErrorKeys(IDictionary<string, string[]> errors)
    {
        foreach (var key in errors.Keys.ToList())
        {
            var camelCaseKey = string.Join('.', key.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));

            if (camelCaseKey != key)
            {
                errors[camelCaseKey] = errors.TryGetValue(camelCaseKey, out var existing)
                    ? [.. existing, .. errors[key]]
                    : errors[key];
                errors.Remove(key);
            }
        }
    }
}
