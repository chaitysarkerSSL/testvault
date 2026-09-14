using System.Text;
using System.Text.Json;

namespace TestVault.Web.Json;

/// <summary>
/// Converts PascalCase DTO property names to snake_case for the wire, e.g.
/// TotalRuns -&gt; total_runs, RunStartedAt -&gt; run_started_at. Required
/// because the existing React frontend consumes snake_case JSON
/// (backend/routes/*.js return raw SQL rows, whose columns are already
/// snake_case) and Phase 4 must not change that contract.
///
/// Every Application DTO property in this solution was deliberately named
/// to mirror its source SQL column 1:1 in PascalCase (see each DTO's own
/// doc comments), so a simple per-character conversion is sufficient here -
/// there are no acronyms (e.g. "ID", "URL") anywhere in this set that would
/// need special-casing to avoid an awkward split.
/// </summary>
public class SnakeCaseJsonNamingPolicy : JsonNamingPolicy
{
    public static readonly SnakeCaseJsonNamingPolicy Instance = new();

    public override string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var builder = new StringBuilder(name.Length + 4);

        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];

            if (char.IsUpper(current))
            {
                if (i > 0)
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(current));
            }
            else
            {
                builder.Append(current);
            }
        }

        return builder.ToString();
    }
}
