namespace KQLValidator;

public sealed class SchemaDefinition
{
    public List<FunctionDefinition> Functions { get; set; } = [];

    public List<TableDefinition> Tables { get; set; } = [];
}

public sealed class FunctionDefinition
{
    public string Name { get; set; } = string.Empty;

    public string Parameters { get; set; } = "()";

    public string Body { get; set; } = string.Empty;

    public string? ReturnType { get; set; }
}

public sealed class TableDefinition
{
    public string Name { get; set; } = string.Empty;

    public List<ColumnDefinition> Columns { get; set; } = [];
}

public sealed class ColumnDefinition
{
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;
}
