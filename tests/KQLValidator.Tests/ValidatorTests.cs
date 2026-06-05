namespace KQLValidator.Tests;

public class ValidatorTests
{
    // ── Lenient (syntax-only) mode ────────────────────────────────────────────

    [Fact]
    public void Lenient_ValidSyntax_ReturnsValid()
    {
        var kql = """
            datatable(Timestamp:datetime, Value:int)[]
            | where Value > 10
            """;

        var result = Validator.Validate(kql, strict: false);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Lenient_InvalidSyntax_ReturnsErrors()
    {
        var kql = "| where Value > 10";   // pipe with no leading table

        var result = Validator.Validate(kql, strict: false);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Lenient_UndefinedTable_IsAllowed()
    {
        // In lenient mode an unknown table is not a syntax error
        var kql = "MyUndefinedTable | where Value > 10";

        var result = Validator.Validate(kql, strict: false);

        Assert.True(result.IsValid);
    }

    // ── Strict (semantic) mode ────────────────────────────────────────────────

    [Fact]
    public void Strict_ValidKqlWithLetDefinedTable_ReturnsValid()
    {
        var kql = """
            let MyTable = datatable(Timestamp:datetime, Value:int)
            [
                datetime(2026-01-01), 10,
                datetime(2026-01-02), 20
            ];
            MyTable
            | where Value > 15
            """;

        var result = Validator.Validate(kql, strict: true);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Strict_UndefinedTable_ReturnsError()
    {
        var kql = "MyUndefinedTable | where Value > 10";

        var result = Validator.Validate(kql, strict: true);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("MyUndefinedTable"));
    }

    [Fact]
    public void Strict_UndefinedFunction_ReturnsError()
    {
        var kql = "print result = my_undefined_function(42)";

        var result = Validator.Validate(kql, strict: true);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("my_undefined_function"));
    }

    [Fact]
    public void Strict_InvalidSyntax_ReturnsErrors()
    {
        var kql = "| where Value > 10";

        var result = Validator.Validate(kql, strict: true);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    // ── Error message format ──────────────────────────────────────────────────

    [Fact]
    public void Strict_ErrorMessage_IncludesPosition()
    {
        var kql = "MyUndefinedTable | where Value > 10";

        var result = Validator.Validate(kql, strict: true);

        Assert.Contains(result.Errors, e => e.Contains("position"));
    }

    // ── Multi-statement / merged content ─────────────────────────────────────

    [Fact]
    public void Strict_TableDefinedInPreviousStatement_ReturnsValid()
    {
        // Simulates two files merged: first defines the table, second queries it
        var file1 = """
            let SharedTable = datatable(Id:int, Name:string)
            [
                1, "Alice",
                2, "Bob"
            ];
            """;

        var file2 = """
            SharedTable
            | where Id > 1
            """;

        var merged = string.Join(Environment.NewLine, file1, file2);
        var result = Validator.Validate(merged, strict: true);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Lenient_MergedContent_ValidQuery_ReturnsValid()
    {
        var part1 = "let X = 42;";
        var part2 = "print X";
        var merged = string.Join(Environment.NewLine, part1, part2);

        var result = Validator.Validate(merged, strict: false);

        Assert.True(result.IsValid);
    }

    // ── Type-safety in strict mode ────────────────────────────────────────────

    [Fact]
    public void Strict_WrongColumnType_ReturnsError()
    {
        // Value is int, but we're trying to use it as a string with strlen
        var kql = """
            let T = datatable(Value:int)[1,2,3];
            T
            | extend Length = strlen(Value)
            """;

        var result = Validator.Validate(kql, strict: true);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Strict_JsonSchemaDefinitions_ReturnValidWhenMergedWithQuery()
    {
        var schemaJson = """
            {
              "functions": [
                {
                  "name": "myFunction",
                  "parameters": "(x:int, y:string)",
                  "body": "x + 1",
                  "returnType": "int"
                }
              ],
              "tables": [
                {
                  "name": "Logs",
                  "columns": [
                    { "name": "Timestamp", "type": "datetime" },
                    { "name": "Message", "type": "string" }
                  ]
                }
              ]
            }
            """;

        var schemaKql = JsonSchemaParser.ConvertSchemaToKql(JsonSchemaParser.ParseJson(schemaJson));
        var query = """
            Logs
            | extend NextValue = myFunction(1, Message)
            | project Timestamp, Message, NextValue
            """;

        var merged = string.Join(Environment.NewLine, schemaKql, query);
        var result = Validator.Validate(merged, strict: true);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
    }

    [Fact]
    public void JsonSchemaParser_MissingRequiredFunctionBody_Throws()
    {
        var schema = new SchemaDefinition
        {
            Functions =
            [
                new FunctionDefinition
                {
                    Name = "myFunction",
                    Parameters = "(x:int)",
                    Body = ""
                }
            ]
        };

        var exception = Assert.Throws<InvalidDataException>(() => JsonSchemaParser.ConvertSchemaToKql(schema));

        Assert.Contains("body is required", exception.Message);
    }

    // ── Microsoft Defender Advanced Hunting functions ─────────────────────────

    [Fact]
    public void Lenient_FileProfileFunction_WithValidQuery_ReturnsValid()
    {
        // FileProfile is a function from Microsoft Defender Advanced Hunting
        // that returns information about a file based on its SHA1 hash
        var kql = """
            let deviceTable = datatable(DeviceId:string, FileName:string, Sha1:string)
            [
                "device1", "test.exe", "e5fa44f2b31c1fb553b6021e7aab6b74476544c0",
                "device2", "malware.exe", "abc123def456fab1a2b3c4d5e6f7890123456789"
            ];
            deviceTable
            | extend ProfileResult = FileProfile(Sha1)
            | project DeviceId, FileName, Sha1, ProfileResult
            """;

        var result = Validator.Validate(kql, strict: false);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
