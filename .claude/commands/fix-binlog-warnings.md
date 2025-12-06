---
description: Fix compiler warnings found in MSBuild binlog files
---

Analyze the binlog files in `$REPO/artifacts/logs` and fix all compiler warnings.

## Process

1. **Extract warnings** from all `*.binlog` files using:
   ```
   dotnet msbuild "<binlog>" -flp:warningsonly -nologo 2>&1 | grep -i warning
   ```

2. **Deduplicate warnings** - the same warning often appears multiple times across different project configurations (net8.0, net472, etc.). Fix each unique source location only once.

3. **Group by file** and fix warnings in batches when possible.

4. **Common warning types**:
   - `IDE0005`: Remove unnecessary using directive
   - `IDE0047`: Remove unnecessary parentheses
   - `CS8603`: Possible null reference return - add null-forgiving operator or change nullable cast
   - `CS8604`: Possible null reference argument
   - `CS8618`: Non-nullable field uninitialized
   - `CS1591`: Missing XML comment for publicly visible type/member

5. **Verify fixes** by building the affected projects:
   ```
   dotnet build "<project>" -c Debug --no-restore -v q
   ```

6. **Report summary** of warnings fixed, grouped by warning code.

## Guidelines

- Do NOT fix warnings in generated code or test output files
- Do NOT change logic or behavior - only fix the warning minimally
- For null reference warnings, prefer adding `!` operator over changing signatures
- Skip warnings about missing package readmes (NU5048)
