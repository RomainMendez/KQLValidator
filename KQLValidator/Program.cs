using KQLValidator;
using System.Text.Json;

const string Usage = "Usage: KQLValidator [--strict] <file1.kql|file1.json> [file2.kql|file2.json ...]";

if (args.Length == 0)
{
    Console.Error.WriteLine(Usage);
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
    Console.Error.WriteLine(Usage);
    return 1;
}

var contents = new List<string>();
foreach (var filePath in filePaths)
{
    if (!IsSupportedFile(filePath))
    {
        Console.Error.WriteLine($"Argument is not a .kql or .json file: {filePath}");
        return 1;
    }

    if (!File.Exists(filePath))
    {
        Console.Error.WriteLine($"File not found: {filePath}");
        return 1;
    }

    try
    {
        contents.Add(await ReadContentAsync(filePath));
    }
    catch (Exception ex) when (ex is JsonException or InvalidDataException)
    {
        Console.Error.WriteLine($"Invalid JSON schema file '{filePath}': {ex.Message}");
        return 1;
    }
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

static bool IsSupportedFile(string filePath) =>
    filePath.EndsWith(".kql", StringComparison.OrdinalIgnoreCase) ||
    filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

static async Task<string> ReadContentAsync(string filePath)
{
    if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
    {
        var json = await File.ReadAllTextAsync(filePath);
        var schema = JsonSchemaParser.ParseJson(json);
        return JsonSchemaParser.ConvertSchemaToKql(schema);
    }

    return await File.ReadAllTextAsync(filePath);
}
