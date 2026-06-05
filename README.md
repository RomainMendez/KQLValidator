# KQLValidator

A small CLI tool to validate KQL query syntax from one or more `.kql` and `.json` schema files.

## Usage


```bash
KQLValidator [--strict] <file1.kql|file1.json> [file2.kql|file2.json ...]
```

The CLI merges all provided files in order and validates the merged query.

- Exit code `0`: syntax is valid
- Exit code `1`: syntax is invalid (or input/usage error)

### Flags

| Flag | Description |
|------|-------------|
| `--strict` | Enable strict (semantic) validation. Fails if any table, function, or variable referenced in the query is not defined within the provided files. Use this when your `.kql` and `.json` schema files contain full table declarations, `let` bindings, or function definitions and you want to ensure complete type safety. |

### Strict mode

Without `--strict`, the validator only checks KQL syntax (parsing). With `--strict`, it also performs semantic analysis: unknown table or function references are reported as errors.

```bash
# Syntax-only validation (lenient)
KQLValidator query.kql

# Full semantic validation (requires all tables/functions to be defined)
KQLValidator --strict schema.kql query.kql

# Mix JSON schema definitions with KQL queries
KQLValidator --strict schema.json query.kql
```

## JSON schema files

JSON schema files can define functions and tables that are converted into KQL `let` bindings before validation.

```json
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
```

Functions and tables are merged in the same order as the files you pass on the command line, so schema files can be combined with existing `.kql` sources as needed.

## Build

```bash
dotnet build /home/runner/work/KQLValidator/KQLValidator/KQLValidator.slnx
```

## GitHub Action artifacts

The workflow at `/home/runner/work/KQLValidator/KQLValidator/.github/workflows/build.yml` publishes self-contained executables for:

- `linux-x64`
- `win-x64`

Artifacts are uploaded as `kqlvalidator-linux-x64` and `kqlvalidator-win-x64`.

The workflow also runs a smoke test using `/home/runner/work/KQLValidator/KQLValidator/tests/smoke/basic-datatable-where.kql` (a `datatable` plus `where` query) against the published executable.

## GitHub Releases

The workflow at `/home/runner/work/KQLValidator/KQLValidator/.github/workflows/release.yml` publishes the same self-contained executables as release assets on tagged releases (`v*`).
