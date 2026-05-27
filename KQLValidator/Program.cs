using Kusto.Language;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: KQLValidator [--strict] <file1.kql> [file2.kql ...]");
    return 1;
}

var strict = false;
var filePaths = new List<string>();
foreach (var arg in args)
{
    if (arg == "--strict")
    {
        strict = true;
    }
    else
    {
        filePaths.Add(arg);
    }
}

if (filePaths.Count == 0)
{
    Console.Error.WriteLine("Usage: KQLValidator [--strict] <file1.kql> [file2.kql ...]");
    return 1;
}

var contents = new List<string>();
foreach (var filePath in filePaths)
{
    if (!filePath.EndsWith(".kql", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine($"Argument is not a .kql file: {filePath}");
        return 1;
    }

    if (!File.Exists(filePath))
    {
        Console.Error.WriteLine($"File not found: {filePath}");
        return 1;
    }

    contents.Add(await File.ReadAllTextAsync(filePath));
}

var mergedQuery = string.Join(Environment.NewLine, contents);
if (string.IsNullOrWhiteSpace(mergedQuery))
{
    Console.Error.WriteLine("No KQL content found in the provided files.");
    return 1;
}

var code = strict
    ? KustoCode.ParseAndAnalyze(mergedQuery)
    : KustoCode.Parse(mergedQuery);

var diagnostics = code.GetDiagnostics()
    .Where(d => d.Severity == DiagnosticSeverity.Error)
    .ToList();

if (diagnostics.Count == 0)
{
    Console.WriteLine(strict ? "KQL is valid (strict)." : "KQL syntax is valid.");
    return 0;
}

Console.Error.WriteLine(strict ? "KQL is invalid (strict):" : "KQL syntax is invalid:");
foreach (var diagnostic in diagnostics)
{
    if (diagnostic.HasLocation)
    {
        Console.Error.WriteLine($"- {diagnostic.Message} (position {diagnostic.Start}-{diagnostic.End})");
    }
    else
    {
        Console.Error.WriteLine($"- {diagnostic.Message}");
    }
}

return 1;
