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
}
