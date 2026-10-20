using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Claims.Api.Infrastructure.Persistence.Configurations;

internal static class EnumPropertyExtensions
{
    /// <summary>
    /// Stores the enum member name, matching the API's string enum contract and the SQL check constraints.
    /// </summary>
    public static PropertyBuilder<TEnum> HasEnumName<TEnum>(this PropertyBuilder<TEnum> property)
        where TEnum : struct, Enum =>
        property.HasConversion<string>().HasMaxLength(50);
}
