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

var result = KQLValidator.Validator.Validate(mergedQuery, strict);

if (result.IsValid)
{
    Console.WriteLine(strict ? "KQL is valid (strict)." : "KQL syntax is valid.");
    return 0;
}

Console.Error.WriteLine(strict ? "KQL is invalid (strict):" : "KQL syntax is invalid:");
foreach (var error in result.Errors)
{
    Console.Error.WriteLine($"- {error}");
}

return 1;
