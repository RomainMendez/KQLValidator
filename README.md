# KQLValidator

A small CLI tool to validate KQL query syntax from one or more `.kql` files.

## Usage


```bash
KQLValidator file1.kql [file2.kql ...]
```

The CLI merges all provided files in order and validates the merged query.

- Exit code `0`: syntax is valid
- Exit code `1`: syntax is invalid (or input/usage error)

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
