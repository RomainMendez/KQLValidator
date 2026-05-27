# KQLValidator

A small CLI tool to validate KQL query syntax from one or more `.kql` files.

## Usage


```bash
KQLValidator [--strict] file1.kql [file2.kql ...]
```

The CLI merges all provided files in order and validates the merged query.

- Exit code `0`: syntax is valid
- Exit code `1`: syntax is invalid (or input/usage error)

### Flags

| Flag | Description |
|------|-------------|
| `--strict` | Enable strict (semantic) validation. Fails if any table, function, or variable referenced in the query is not defined within the provided files. Use this when your `.kql` files contain full schema definitions (table declarations, `let` bindings, function definitions) and you want to ensure complete type safety. |

### Strict mode

Without `--strict`, the validator only checks KQL syntax (parsing). With `--strict`, it also performs semantic analysis: unknown table or function references are reported as errors.

```bash
# Syntax-only validation (lenient)
KQLValidator query.kql

# Full semantic validation (requires all tables/functions to be defined)
KQLValidator --strict schema.kql query.kql
```

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
