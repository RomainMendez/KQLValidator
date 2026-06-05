using System.Text.Json;

namespace KQLValidator;

public static class JsonSchemaParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static SchemaDefinition ParseJson(string jsonContent)
    {
        var schema = JsonSerializer.Deserialize<SchemaDefinition>(jsonContent, SerializerOptions)
            ?? new SchemaDefinition();

        schema.Functions ??= [];
        schema.Tables ??= [];

        return schema;
    }

    public static string ConvertSchemaToKql(SchemaDefinition schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var statements = new List<string>();

        foreach (var function in schema.Functions)
        {
            statements.Add(ConvertFunctionToKql(function));
        }

        foreach (var table in schema.Tables)
        {
            statements.Add(ConvertTableToKql(table));
        }

        return string.Join(Environment.NewLine, statements);
    }

    private static string ConvertFunctionToKql(FunctionDefinition function)
    {
        ArgumentNullException.ThrowIfNull(function);

        if (string.IsNullOrWhiteSpace(function.Name))
        {
            throw new InvalidDataException("Function name is required.");
        }

        if (string.IsNullOrWhiteSpace(function.Body))
        {
            throw new InvalidDataException($"Function '{function.Name}' body is required.");
        }

        var parameters = string.IsNullOrWhiteSpace(function.Parameters)
            ? "()"
            : function.Parameters.Trim();

        if (!parameters.StartsWith('(') || !parameters.EndsWith(')'))
        {
            throw new InvalidDataException($"Function '{function.Name}' parameters must be wrapped in parentheses.");
        }

        return $"let {function.Name} = {parameters} {{ {function.Body.Trim()} }};";
    }

    private static string ConvertTableToKql(TableDefinition table)
    {
        ArgumentNullException.ThrowIfNull(table);

        if (string.IsNullOrWhiteSpace(table.Name))
        {
            throw new InvalidDataException("Table name is required.");
        }

        if (table.Columns is null || table.Columns.Count == 0)
        {
            throw new InvalidDataException($"Table '{table.Name}' must define at least one column.");
        }

        var columns = string.Join(", ", table.Columns.Select(ConvertColumnToKql));
        return $"let {table.Name} = datatable({columns})[];";
    }

    private static string ConvertColumnToKql(ColumnDefinition column)
    {
        ArgumentNullException.ThrowIfNull(column);

        if (string.IsNullOrWhiteSpace(column.Name))
        {
            throw new InvalidDataException("Column name is required.");
        }

        if (string.IsNullOrWhiteSpace(column.Type))
        {
            throw new InvalidDataException($"Column '{column.Name}' type is required.");
        }

        return $"{column.Name}:{column.Type}";
    }
}
