using Kusto.Language;

namespace KQLValidator;

public static class Validator
{
    public static ValidationResult Validate(string kql, bool strict)
    {
        var code = strict
            ? KustoCode.ParseAndAnalyze(kql)
            : KustoCode.Parse(kql);

        var errors = code.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.HasLocation
                ? $"{d.Message} (position {d.Start}-{d.End})"
                : d.Message)
            .ToList();

        return new ValidationResult(errors);
    }
}

public sealed record ValidationResult(IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}
