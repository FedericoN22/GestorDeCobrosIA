using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Kiosk.Infrastructure.Persistence;

public sealed class SnakeCaseNamingConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entity in modelBuilder.Metadata.GetEntityTypes())
        {
            entity.SetTableName(ToSnakeCase(entity.GetTableName() ?? entity.DisplayName()));

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.GetColumnName() ?? property.Name));
            }

            foreach (var key in entity.GetKeys())
            {
                key.SetName(ToSnakeCase(key.GetName() ?? string.Empty));
            }
        }
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var sb = new StringBuilder(input.Length);
        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (char.IsUpper(c) && i > 0 &&
                (char.IsLower(input[i - 1]) || (i + 1 < input.Length && char.IsLower(input[i + 1]))))
            {
                sb.Append('_');
            }

            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }
}
