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

### Symbols

To make the code navigable, the HTML files can identify the symbol of each identifier. Enable it with the
`MetalamaWriteHtmlSymbols` MSBuild property, or with the `AddHtmlSymbols` test option (`// @AddHtmlSymbols` in a test file):

```xml
<PropertyGroup>
    <MetalamaWriteHtml>true</MetalamaWriteHtml>
    <MetalamaWriteHtmlSymbols>true</MetalamaWriteHtmlSymbols>
</PropertyGroup>
```

Each identifier then gets one of these attributes, whose value is the documentation comment id of the symbol, as in the
`commentId` field of DocFx cross-reference maps:

- `data-def`: the identifier declares the symbol.
- `data-ref`: the identifier references the symbol.

An overriding member also gets `data-overrides`, the id of the overridden member.

Locals, parameters, type parameters and local functions have no documentation id. Their identifiers get a key that is
unique in the file instead: `data-local-def` on the declaration and `data-local-ref` on each reference.

Without this package, the `WriteHtml` feature and related test options will throw an error with a message indicating that the package needs to be installed.
