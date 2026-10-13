# Metalama.Extensions.DiffEngine

This package provides optional diff tool integration for the Metalama aspect testing framework.

## Installation

Add a reference to this package in your test project:

```xml
<PackageReference Include="Metalama.Extensions.DiffEngine" />
```

## Features

When this package is referenced, the Metalama test framework will automatically launch your configured diff tool when test output differs from expected output. This helps you quickly identify and accept changes to expected output files.

## Configuration

Configure diff tool behavior through the `testRunner.json` configuration file. To edit this file, use the Metalama CLI tool:

```powershell
metalama config edit testRunner
```

This opens the configuration file with the following settings:

```json
{
  "LaunchDiffTool": true,
  "MaxDiffToolInstances": 3
}
```

Without this package, tests work normally but diff tool features are silently disabled.

## Continuous integration builds

When the `ContinuousIntegrationBuild` MSBuild property is `True`, this package does not register its extension assemblies with the test framework. DiffEngine does not launch a diff tool on a build server, so loading these assemblies for each test would only cost time. Test failures are reported in the same way.

To control this behavior, set the `MetalamaEnableDiffEngine` MSBuild property:

| Value | Behavior |
|---|---|
| (not set) | The diff tool integration is enabled, except when `ContinuousIntegrationBuild` is `True`. |
| `True` | The diff tool integration is always enabled. |
| `False` | The diff tool integration is always disabled. |

For example, set `MetalamaEnableDiffEngine` to `True` if your `Directory.Build.props` sets `ContinuousIntegrationBuild` to `True` for local builds too.
