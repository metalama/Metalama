# LinqPad Samples

This directory contains sample LinqPad queries that demonstrate Metalama's Workspaces API for analyzing C# codebases.

## Prerequisites

- **LinqPad 8** installed (with `LPRun8.exe` available)
- **Metalama.LinqPad** NuGet package
- **Metalama.Extensions.Metrics** NuGet package (for metrics-related samples)

## Running Tests

Use the included PowerShell script:

```powershell
# Run all samples with default demo solution (FrogHattery)
.\Run-Samples.ps1

# Run all samples with a custom solution
.\Run-Samples.ps1 -SolutionPath "C:\path\to\your\solution.sln"

# Run a single sample
.\Run-Samples.ps1 -Query "All Public Methods.linq"
```

## Sample Categories

### Code Analysis
- **All Public Methods.linq** - Lists public methods grouped by type
- **Public API Surface.linq** - Analyzes public API (types, methods, properties)
- **Inheritance Depth.linq** - Shows inheritance hierarchy depth
- **Large Parameter Lists.linq** - Finds methods with many parameters
- **Async Methods Without CancellationToken.linq** - Identifies async methods missing CancellationToken

### Metrics (require Metalama.Extensions.Metrics)
- **Code Complexity Distribution.linq** - Distribution chart of code complexity
- **Largest Types.linq** - Types ranked by statement count
- **Most Complex Methods.linq** - Methods ranked by complexity

### Aspects & Transformations
- **Aspects Applied per Type.linq** - Shows aspects applied to each type
- **Transformations by Aspect.linq** - Groups transformations by aspect
- **Skipped Aspect Instances.linq** - Lists skipped aspect instances
- **Most Reported Diagnostic Ids.linq** - Most common diagnostics
- **Types with Most Diagnostics.linq** - Types with most diagnostics

## Exit Codes

The script returns the number of failed tests (0 = all passed).
