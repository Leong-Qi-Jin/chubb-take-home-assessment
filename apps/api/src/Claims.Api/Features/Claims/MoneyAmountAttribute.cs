using System.ComponentModel.DataAnnotations;

namespace Claims.Api.Features.Claims;

/// <summary>
/// Accepts an ASCII non-negative decimal string with up to two decimal places that fits SQL Server <c>decimal(18,2)</c>, such as <c>1250.00</c>.
/// </summary>
public sealed class MoneyAmountAttribute() : RegularExpressionAttribute("^[0-9]{1,16}([.][0-9]{1,2})?$")
{
    public override string FormatErrorMessage(string name) =>
        "Enter a non-negative amount with up to 16 digits and two decimal places, such as 1250.00.";
}
