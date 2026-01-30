# Metalama.Extensions.HtmlWriter

This package provides optional HTML code writing functionality for Metalama.

## Installation

Add a reference to this package in your project:

```xml
<PackageReference Include="Metalama.Extensions.HtmlWriter" />
```

## Features

When this package is referenced, Metalama can generate HTML output files showing the source code with syntax highlighting and diff visualization. This is useful for:

- Documentation generation
- Debugging aspect transformations
- Visualizing compile-time vs run-time code

## Usage

### Compile-time Pipeline

Enable HTML output in your project by setting the `WriteHtml` MSBuild property:

```xml
<PropertyGroup>
    <MetalamaWriteHtml>true</MetalamaWriteHtml>
</PropertyGroup>
```

### Aspect Tests

Enable HTML output in aspect tests by adding a `metalamaTests.json` file to your test project:

```json
{
  "WriteInputHtml": true,
  "WriteOutputHtml": true
}
```

Without this package, the `WriteHtml` feature and related test options will throw an error with a message indicating that the package needs to be installed.
