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
