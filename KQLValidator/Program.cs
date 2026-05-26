using Kusto.Language;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: KQLValidator <file1.kql> [file2.kql ...]");
    return 1;
}

var contents = new List<string>();
foreach (var filePath in args)
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

var code = KustoCode.Parse(mergedQuery);
var diagnostics = code.GetDiagnostics()
    .Where(d => d.Severity == DiagnosticSeverity.Error)
    .ToList();

if (diagnostics.Count == 0)
{
    Console.WriteLine("KQL syntax is valid.");
    return 0;
}

Console.Error.WriteLine("KQL syntax is invalid:");
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
