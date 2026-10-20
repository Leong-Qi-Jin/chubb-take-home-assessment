using System.Globalization;

namespace Claims.Api.Features.Shared;

public sealed record MoneyResponse(string Amount, string Currency)
{
    public static MoneyResponse From(decimal amount, string currency) =>
        new(amount.ToString("0.00", CultureInfo.InvariantCulture), currency);

    public static MoneyResponse? From(decimal? amount, string currency) =>
        amount is { } value ? From(value, currency) : null;
}
