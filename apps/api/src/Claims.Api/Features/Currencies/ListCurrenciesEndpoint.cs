using Claims.Api.Infrastructure.Errors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace Claims.Api.Features.Currencies;

public static class ListCurrenciesEndpoint
{
    public static IEndpointRouteBuilder MapListCurrencies(this IEndpointRouteBuilder api)
    {
        api.MapGet("/currencies", HandleAsync)
            .WithTags("Currencies")
            .WithName("ListCurrencies")
            .RequireAuthorization()
            .ProducesProblems(StatusCodes.Status401Unauthorized);

        return api;
    }

    private static Ok<string[]> HandleAsync(IOptions<CurrencyOptions> options) => TypedResults.Ok(options.Value.Supported);
}
