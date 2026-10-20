using System.ComponentModel.DataAnnotations;

namespace Claims.Api.Features.Currencies;

public sealed class CurrencyOptions : IValidatableObject
{
    public const string SectionName = "Currencies";

    /// <summary>
    /// ISO 4217 codes a claim can be submitted in.
    /// </summary>
    [MinLength(1)]
    public string[] Supported { get; init; } = [];

    // Matches the database check constraint, so a misconfigured code fails at startup rather than on insert.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Supported.All(code => code.Length == 3 && code.All(char.IsAsciiLetterUpper)))
        {
            yield return new ValidationResult(
                "Use three-letter uppercase ISO 4217 codes.",
                [nameof(Supported)]);
        }
    }
}
