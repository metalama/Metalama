# Gap 7 — File-based C# apps, the `#:` ignored directives, and the shebang (`#!`)

Research date: 2026-09-03. Target: .NET 11 GA, November 2026.
All statements below were verified against primary sources (dotnet/sdk, dotnet/roslyn,
dotnet/docs, dotnet/csharplang, dotnet/core release notes, dotnet/vscode-csharp) at the
commits reachable from `main` on 2026-09-03. Blog aggregators were not used.

Version anchors used throughout:

| Branch / tag | `MajorVersion.MinorVersion` in `eng/Versions.props` | Ships with |
|---|---|---|
| `dotnet/roslyn` `release/dev17.14` | 4.14 | VS 2022 17.14, .NET 10 preview era |
| `dotnet/roslyn` `release/dev18.0` | 5.0 | VS 2026 18.0, **.NET 10 GA (Nov 2025)** |
| `dotnet/roslyn` `release/dev18.3` | 5.3 | VS 2026 18.3 |
| `dotnet/roslyn` `main` | **5.12** | VS 2026 18.12, **.NET 11 timeframe** |

`dotnet/core/release-notes/11.0/preview` currently contains `preview1` … `preview7`;
Preview 7 is the newest (SDK `11.0.100-preview.7.26381.103`).

---

## 1. The complete `#:` directive set in the .NET 11 SDK

### 1.1 Authoritative list

From `dotnet/sdk` `documentation/general/dotnet-run-file.md` (section
*Directives for project metadata*), the .NET 11 SDK recognises **seven** directive kinds.
The dispatch is a literal `switch` in
`src/Cli/Microsoft.DotNet.FileBasedPrograms/FileLevelDirectiveHelpers.cs`
(`CSharpDirective.Parse`, around line 375):

```csharp
switch (context.DirectiveKind)
{
    case "sdk": return Sdk.Parse(context);
    case "property": return Property.Parse(context);
    case "package": return Package.Parse(context);
    case "project": return Project.Parse(context);
    case "ref": return Ref.Parse(context);
    case "include" or "exclude": return IncludeOrExclude.Parse(context);
    default:
        context.ReportError(string.Format(FileBasedProgramsResources.UnrecognizedDirective, context.DirectiveKind));
        return null;
}
```

`FileBasedProgramsResources.UnrecognizedDirective` = `"Unrecognized directive '{0}'."`
Any other kind is an error, deliberately reserving the namespace for future use.

Verbatim example block from the SDK spec:

```cs
#:sdk Microsoft.NET.Sdk.Web
#:property TargetFramework=net11.0
#:property LangVersion=preview
#:package System.CommandLine@2.0.0-*
#:package Microsoft.Build@17.0.0 ExcludeAssets=runtime PrivateAssets=all
#:project ../MyLibrary
#:ref ../lib/lib.cs
#:include ./**/*.cs
```

### 1.2 Grammar (exact, from the SDK spec)

General shape: `#:<kind> <name> [<separator> <value>] [Name=Value ...]`

- The **kind** immediately follows `#:` with no space (the compiler requires
  `hash.HasTrailingTrivia == false` between `#` and `:`, see §2.5).
- The **name** must be separated from the kind by whitespace. Leading and trailing
  whitespace is not part of the name or value.
- The remainder after the kind is split into whitespace-separated tokens.
- **Separators**: `@` (package/sdk version), `=` (property value and item metadata).
  Whitespace may surround the separator, so these are equivalent:
  - `#:property Xyz="abc "` ≡ `#:property Xyz ="abc "` ≡ `#:property Xyz = "abc "`
  - `#:package Package@1.0.0 Note="see the docs"` ≡ `#:package Package @ 1.0.0 Note = "see the docs"`
- **Value required / optional / disallowed**:
  - required for `#:property`
  - optional for `#:package`, `#:sdk`
  - disallowed for `#:project`, `#:ref`, `#:include` (and `#:exclude`)
- **Quoting**: a value is written bare or wrapped *entirely* in double quotes. A quoted
  value is lexed as a regular C# string literal, so escapes are decoded:
  - `#:property Description="Hello World"` → `Hello World`
  - `#:property Path="a\\b"` → `a\b`
  - `#:property Text="a\"b"` → `a"b`
  - Verbatim (`@"..."`) and raw (`"""..."""`) literals are **not** supported
    (`ExpectedSimpleStringLiteralInDirective`).
  - Quotes may only enclose a whole value: `#:property A=B` and `#:property A="B"` are
    legal, `#:property A=B"C"` is an error.
  - Unterminated quote or invalid escape (`"a\q"`) is an error.
  - Either side of a separator may be quoted: `#:package "Humanizer"@2.0`.
- **Trailing MSBuild item metadata** as `Name=Value` tokens is supported by
  `#:package`, `#:project`, `#:ref` only. Each metadata name must be a unique valid XML
  element name; each value may be quoted to contain whitespace. When `#:package` gives a
  version after `@`, it may not also give `Version` metadata. Extra tokens on the other
  kinds are an error.
- **Windows paths**: a bare value keeps a backslash literal, so prefer
  `#:project C:\src\lib` bare, or forward slashes if quoting
  (`#:project "C:/src/my lib"`); a quoted backslash path must escape (`"C:\\src\\my lib"`).
- **Legacy mode** (backward compatibility): a directive whose value contains no double
  quotes is still accepted when its trailing whitespace-separated tokens cannot be parsed
  as the new metadata form; the whole remainder after name+separator is taken verbatim as
  one value, including internal whitespace. For `#:package` / `#:project` / `#:ref`, if
  *every* trailing token is a valid `Name=Value`, they are treated as metadata instead
  (`#:project path A=B` → value `path`, metadata `A=B`).
  Analyzer **CA2267** (`PreferQuotedFileBasedProgramDirective`, `RuleLevel.BuildWarning`)
  flags legacy directives and offers a code fix to rewrite them into the quoted form.
- **MSBuild variables** (`$(...)`) are passed through literally to MSBuild. In `#:project`
  and `#:ref` the variables may not survive `dotnet project convert`, because those values
  must be re-rooted relative to the target directory and resolved to a project file.

### 1.3 Per-directive semantics and MSBuild translation

| Directive | Translation into the virtual project | Notes |
|---|---|---|
| `#:sdk Name[@Version]` | first one → `<Project Sdk="{Name}/{Version}">` (or `Sdk="{Name}"`); subsequent ones → `<Sdk Name="…" Version="…" />` | empty name is an error; empty version allowed but yields `Version=""` |
| `#:property Name=Value` | `<Name>Value</Name>` in a `<PropertyGroup>` | value required; empty/invalid name is an error |
| `#:package Name[@Version] [Meta=Val …]` | `<PackageReference Include="…" Version="…">` with metadata as child elements | version may be omitted for central package management |
| `#:project Path [Meta=Val …]` | `<ProjectReference Include="…" />` | if `Path` is a directory, the single project inside is resolved; 0 or >1 projects is an error |
| `#:ref File.cs [Meta=Val …]` | a *virtual* `File.cs.csproj` is synthesised, then `<ProjectReference Include="File.cs.csproj" />` | see §1.5 |
| `#:include Glob` | `<{ItemType} Include="Glob" />`, item type by extension | see mapping below |
| `#:exclude Glob` | `<{ItemType} Remove="Glob" />` | same mapping |

Default extension→item-type mapping, MSBuild property `FileBasedProgramsItemMapping`
(`FileLevelDirectiveHelpers.cs`, `IncludeOrExclude.DefaultMappingString`):

```
.cs=Compile;.resx=EmbeddedResource;.json=None;.razor=Content;.dll=Reference
```

`.dll` → `Reference` is **new in .NET 11 Preview 6** (dotnet/sdk#54396).

### 1.4 Duplicate handling

- `#:sdk`, `#:property`, `#:package` — duplicates compared case-insensitively by kind+name.
  Same unevaluated value → ignored. Different value → error.
- `#:project`, `#:ref`, `#:include`, `#:exclude` — duplicates allowed, translated to
  MSBuild items; any resulting duplicate-item warning is MSBuild's/the compiler's business.

### 1.5 `#:ref` in detail

From `dotnet-run-file.md`:

- References another `.cs` file **as a library**, not a `.csproj` and not a directory.
- A virtual project is created for the referenced file (e.g. `lib.cs` → virtual
  `lib.cs.csproj`) and a `<ProjectReference Include="lib.cs.csproj" />` is injected.
- The referenced file is itself a file-based program with its own virtual project,
  defaulting to `OutputType=Exe`; a library file without an entry point should use
  `#:property OutputType=Library`.
- Because it is a **separate assembly**, `internal` members of the referenced file are not
  visible to the referencing file.
- Transitive: a referenced file may itself contain `#:ref` (and any other directive).
- Relative paths resolve relative to the file containing the directive; MSBuild variables
  such as `$(MSBuildProjectDirectory)` may be used.
- `dotnet project convert` creates a separate library project per `#:ref` in a sibling
  directory, recursively.
- **Still gated**: "This directive is currently gated under a feature flag that can be
  enabled by setting the MSBuild property `ExperimentalFileBasedProgramEnableRefDirective=true`."
  The constant lives at `FileLevelDirectiveHelpers.cs`
  `CSharpDirective.Ref.ExperimentalFileBasedProgramEnableRefDirective`; when disabled the
  error is `ExperimentalFeatureDisabled` = *"This is an experimental feature, set MSBuild
  property '{0}' to 'true' to enable it."*

### 1.6 Which SDK version shipped what

| Directive | First shipped | Evidence |
|---|---|---|
| `#:sdk`, `#:property`, `#:package`, `#:project` | **.NET 10 SDK** (`dotnet run file.cs`, C# 14 `ignored-directives` speclet) | `dotnet/docs` `file-based-apps.md`: "**This article applies to:** ✔️ .NET 10 SDK and later versions" |
| `#:include` | **.NET 11 Preview 3 and .NET SDK 10.0.300 and later** | explicit `[!NOTE]` in `dotnet/docs/docs/core/sdk/file-based-apps.md`; dotnet/sdk#52347; `core` P3 notes "File-based apps can be split across files" |
| `#:exclude` | existed behind a feature flag; **flag removed in .NET 11 Preview 5** (dotnet/sdk#53775) | `core` P5 notes: "The `#:include` and `#:exclude` directives no longer require feature flags" |
| directives inside `#:include`d files processed transitively | **.NET 11 Preview 5**, flag removed (dotnet/sdk#54012) | `core` P5 notes |
| `#:ref` | **.NET 11 Preview 5** (dotnet/sdk#53480), still behind `ExperimentalFileBasedProgramEnableRefDirective` | `core` P5 notes; SDK spec |
| `.dll` in `#:include` (→ `Reference`) | **.NET 11 Preview 6** (dotnet/sdk#54396) | `core` P6 notes; learn *What's new … SDK for .NET 11* |
| duplicate `#:sdk`/`#:property`/`#:package` allowed across included files when values match | **.NET 11 Preview 6** (dotnet/sdk#54206) | `core` P6 notes |
| duplicate `#:project` / `#:ref` allowed | **.NET 11 Preview 5** (dotnet/sdk#54035) | `core` P5 notes |
| `dotnet reference add/list/remove --file app.cs` writing `#:project` | **.NET 11 Preview 7** (dotnet/sdk#54443) | `core` P7 notes; learn *What's new … SDK for .NET 11* |
| `dotnet package add/list`, `dotnet nuget why` on a file-based app | **.NET 11 Preview 5** (dotnet/sdk#53535) | `core` P5 notes |

**Documentation lag to be aware of.** `dotnet/docs/docs/core/sdk/file-based-apps.md`
(`ms.date: 04/22/2026`) and the learn *What's new in the SDK and tooling for .NET 11* page
still enumerate only **five** directives — `#:include`, `#:package`, `#:project`,
`#:property`, `#:sdk`. Neither mentions `#:exclude` or `#:ref`. The SDK source and the SDK
spec are ahead of the documentation.

### 1.7 Where directives may appear

- The C# language restricts `#:` (and `#!`) to **before the first C# token** and **before
  any `#if`** — see §2.
- The SDK collects directives from the entry-point file **and from every other `Compile`
  item in the project**, whether those items came from MSBuild or from `#:include`. The
  order in which non-entry-point files are processed is unspecified across SDK versions but
  deterministic within a version.
- `#!` is removed by `dotnet project convert` along with `#:`.
- In a project-based program, `#:` is an **error** (see §2.4). The learn preprocessor page
  says these "generate warnings when encountered in a project-based compilation" — that is
  inaccurate; the compiler reports errors (`ERR_*`, not `WRN_*`).

---

## 2. The Roslyn representation

### 2.1 `IgnoredDirectiveTriviaSyntax` in `Syntax.xml`

`dotnet/roslyn` `main`, `src/Compilers/CSharp/Portable/Syntax/Syntax.xml`, lines 5227-5241:

```xml
<Node Name="IgnoredDirectiveTriviaSyntax" Base="DirectiveTriviaSyntax">
  <Kind Name="IgnoredDirectiveTrivia"/>
  <Field Name="HashToken" Type="SyntaxToken" Override="true">
    <Kind Name="HashToken"/>
  </Field>
  <Field Name="ColonToken" Type="SyntaxToken">
    <Kind Name="ColonToken"/>
  </Field>
  <Field Name="Content" Type="SyntaxToken" Optional="true">
    <Kind Name="StringLiteralToken"/>
  </Field>
  <Field Name="EndOfDirectiveToken" Type="SyntaxToken" Override="true">
    <Kind Name="EndOfDirectiveToken"/>
  </Field>
  <Field Name="IsActive" Type="bool" Override="true"/>
</Node>
```

**The node has five fields, not four.** The `Content` field — an *optional* `SyntaxToken`
of kind `StringLiteralToken` — is a real child of the node and appears in `Update`,
`WithContent`, and the `SyntaxFactory` overloads.

`SyntaxKind.IgnoredDirectiveTrivia = 9080`.
`SyntaxFacts.IsPreprocessorDirective(SyntaxKind.IgnoredDirectiveTrivia)` returns `true`,
and `IsTrivia` likewise. `DirectiveTriviaSyntax.DirectiveNameToken` returns the
`ColonToken` for this kind (`src/Compilers/CSharp/Portable/Syntax/DirectiveTriviaSyntax.cs`).

### 2.2 Shape history — this changed between Roslyn 4.14 and 5.0, not 5.0 and 5.12

| | Roslyn 4.14 (`release/dev17.14`) | Roslyn 5.0 (`release/dev18.0`, **.NET 10 GA**) | Roslyn 5.12 (`main`, .NET 11) |
|---|---|---|---|
| `Content` field | **absent** | **present** | present, unchanged |
| `Update` signature | `(hashToken, colonToken, endOfDirectiveToken, isActive)` | `(hashToken, colonToken, content, endOfDirectiveToken, isActive)` | same as 5.0 |
| `SyntaxFactory` overloads | `IgnoredDirectiveTrivia(bool)`, `IgnoredDirectiveTrivia(SyntaxToken, SyntaxToken, SyntaxToken, bool)` | adds `IgnoredDirectiveTrivia(SyntaxToken content, bool isActive)`, 5-arg full overload | same as 5.0 |
| Public-API status | **`PublicAPI.Unshipped.txt`, every member tagged `[RSEXPERIMENTAL005]`** (the `SyntaxKind` constant and the visitor/rewriter methods were *not* tagged) | **`PublicAPI.Shipped.txt`, no experimental attribute** | shipped, stable |
| Directive text location | leading `PreprocessingMessageTrivia` on `EndOfDirectiveToken` | the `Content` token; `EndOfDirectiveToken.GetLeadingTrivia()` is **empty** | same as 5.0 |

Conclusion for the .NET 11 question as posed: **the node and its fields did not change
between Roslyn 5.0 and Roslyn 5.12.** The interesting delta is one release earlier, and it
matters to anyone whose code was written against the .NET 10-preview (Roslyn 4.14) shape:
the `Update` arity changed and the accessors lost `[RSEXPERIMENTAL005]`.

`dotnet/roslyn` issue **77697 "API for IgnoredDirectiveTrivia"** (opened 2025-03-20, closed
completed 2025-04-11, labels `Concept-API`, `Area-Compilers`, `api-approved`,
`Feature - Run File`) is the API-review issue. It proposed `IgnoredDirectiveTrivia = 9079`
(shipped value is **9080**) and the four-field shape, and its *Alternative Designs* section
explicitly raised moving the text out of `PreprocessingMessageTrivia` into the node —
which is what eventually happened for `#:` but not for `#!`.

### 2.3 Exact public API surface as of Roslyn 5.12 (from `PublicAPI.Shipped.txt`)

```
Microsoft.CodeAnalysis.CSharp.SyntaxKind.IgnoredDirectiveTrivia = 9080
Microsoft.CodeAnalysis.CSharp.SyntaxKind.ShebangDirectiveTrivia = 8922

Microsoft.CodeAnalysis.CSharp.Syntax.IgnoredDirectiveTriviaSyntax
  .ColonToken.get -> SyntaxToken
  .Content.get -> SyntaxToken
  override .HashToken.get -> SyntaxToken
  override .EndOfDirectiveToken.get -> SyntaxToken
  override .IsActive.get -> bool
  .Update(SyntaxToken hashToken, SyntaxToken colonToken, SyntaxToken content, SyntaxToken endOfDirectiveToken, bool isActive) -> IgnoredDirectiveTriviaSyntax
  .WithHashToken / .WithColonToken / .WithContent / .WithEndOfDirectiveToken / .WithIsActive
  override .Accept(CSharpSyntaxVisitor) / .Accept<TResult>(CSharpSyntaxVisitor<TResult>)

static SyntaxFactory.IgnoredDirectiveTrivia(bool isActive)
static SyntaxFactory.IgnoredDirectiveTrivia(SyntaxToken content, bool isActive)
static SyntaxFactory.IgnoredDirectiveTrivia(SyntaxToken hashToken, SyntaxToken colonToken, SyntaxToken content, SyntaxToken endOfDirectiveToken, bool isActive)

virtual  CSharpSyntaxVisitor.VisitIgnoredDirectiveTrivia(IgnoredDirectiveTriviaSyntax node) -> void
virtual  CSharpSyntaxVisitor<TResult>.VisitIgnoredDirectiveTrivia(IgnoredDirectiveTriviaSyntax node) -> TResult?
override CSharpSyntaxRewriter.VisitIgnoredDirectiveTrivia(IgnoredDirectiveTriviaSyntax node) -> SyntaxNode?
```

`PublicAPI.Unshipped.txt` on `main` contains **no** `IgnoredDirective*` or `Shebang*`
entries, i.e. nothing about these nodes is pending in .NET 11.

### 2.4 The compiler does not interpret the directives

The compiler **lexes and parses** `#:` into a syntax node and **ignores its content
entirely**. It never reads the kind (`sdk`, `package`, …), never validates it, never acts
on it. The C# 14 speclet is titled *"Ignored directives for file-based apps"*
(`dotnet/csharplang/proposals/csharp-14.0/ignored-directives.md`, champion issue
csharplang#8617) and adds a grammar production:

```antlr
PP_Ignored
    : PP_IgnoredToken Input_Character*
    ;
PP_IgnoredToken
    : '!'
    | ':'
    ;
```

"These are parsed regardless of language version."

The **SDK** parses the content. The parser is
`src/Cli/Microsoft.DotNet.FileBasedPrograms/FileLevelDirectiveHelpers.cs` in `dotnet/sdk`,
and it drives Roslyn's public tokenizer:

```csharp
public static SyntaxTokenParser CreateTokenizer(SourceText text)
{
    return SyntaxFactory.CreateTokenParser(text,
        CSharpParseOptions.Default.WithFeatures([new("FileBasedProgram", "true")]));
}
```

then reads `IgnoredDirectiveTriviaSyntax.Content`:

```csharp
if (trivia.GetStructure() is IgnoredDirectiveTriviaSyntax { Content: { RawKind: (int)SyntaxKind.StringLiteralToken } content })
{
    var contentText = content.Text.AsSpan();
    ...
}
var parts = Patterns.Whitespace.Split(message.ToString(), 2);
var name  = parts.Length > 0 ? parts[0] : "";   // the directive kind, e.g. "package"
var value = parts.Length > 1 ? parts[1] : "";   // the rest of the line
```

The SDK spec states the design intent: "dotnet CLI can look for them via a regex or Roslyn
lexer without any knowledge of defined conditional symbols and can do that efficiently by
stopping the search when it sees the first 'C# token'."

**The same SDK source is vendored into Roslyn** for IDE use, under
`src/Workspaces/CSharp/Portable/SyncedSource/FileBasedPrograms/` (`FileLevelDirectiveHelpers.cs`,
`VirtualProjectBuilder.cs`, `ProjectLocator.cs`, `Sha256Hasher.cs`, `MSBuildUtilities.cs`,
`Extensions.cs`, `ExternalHelpers.cs`, `IBuildService.cs`, plus the resx and xlf), kept in
sync by `eng/ensure-sources-synced.cs`; `SyncedSource/commitid.txt` pins the dotnet/sdk
commit (currently `9ea4e48db1d9e9737e5fcc9adfe54ed5015a60da`). So the IDE parses `#:`
directive *content* with byte-identical logic to the CLI.

### 2.5 The feature flag that enables `#:`

`CSharpParseOptions` (`src/Compilers/CSharp/Portable/CSharpParseOptions.cs`):

```csharp
/// <remarks>
/// In this mode, ignored directives <c>#:</c> are allowed.
/// </remarks>
internal bool FileBasedProgram => HasFeature(Feature.FileBasedProgram);
```

It is **internal**; it is set through the general feature mechanism,
`CSharpParseOptions.WithFeatures([new("FileBasedProgram", "true")])`, or on the command
line as `-features:FileBasedProgram` / `/features:FileBasedProgram`.
`FileLevelDirectiveDiagnosticAnalyzer` detects it as
`tree.Options.Features.ContainsKey("FileBasedProgram")`.

Parsing with **default** parse options therefore produces errors on a file-based app source.

### 2.6 Parser behaviour and diagnostics

`src/Compilers/CSharp/Portable/Parser/DirectiveParser.cs`, `ParseDirective` default branch:

```csharp
if (contextualKind == SyntaxKind.ExclamationToken)
{
    // Always parse as a shebang directive, but report an error if not at position 0
    if (hashPosition != 0 || hash.HasTrailingTrivia)
    {
        hash = this.AddError(hash, ErrorCode.ERR_PPShebangNotOnFirstLine);
    }
    result = this.ParseShebangDirective(hash, this.EatToken(SyntaxKind.ExclamationToken), isActive);
}
else if (contextualKind == SyntaxKind.ColonToken && !hash.HasTrailingTrivia)
{
    result = this.ParseIgnoredDirective(hash, this.EatToken(SyntaxKind.ColonToken), isActive, isFollowingToken);
}
```

```csharp
private DirectiveTriviaSyntax ParseIgnoredDirective(SyntaxToken hash, SyntaxToken colon, bool isActive, bool isFollowingToken)
{
    if (isActive)
    {
        if (!lexer.Options.FileBasedProgram)
            colon = this.AddError(colon, ErrorCode.ERR_PPIgnoredNeedsFileBasedProgram);
        if (isFollowingToken)
            colon = this.AddError(colon, ErrorCode.ERR_PPIgnoredFollowsToken);
        if (_context.SeenAnyIfDirectives)
            colon = this.AddError(colon, ErrorCode.ERR_PPIgnoredFollowsIf);
    }
    SyntaxToken endOfDirective = this.lexer.LexEndOfDirectiveWithOptionalContent(out SyntaxToken content);
    return SyntaxFactory.IgnoredDirectiveTrivia(hash, colon, content, endOfDirective, isActive);
}
```

Note `!hash.HasTrailingTrivia`: `# :package X` is **not** an ignored directive (it falls
through to `BadDirectiveTrivia` / `ERR_PPDirectiveExpected`). By contrast the shebang path
tolerates trivia (`# !xyz` still parses as a shebang, with CS9378).

Diagnostics (from `ErrorCode.cs` and `CSharpResources.resx` on `main`):

| Code | `ErrorCode` | Message | Present in |
|---|---|---|---|
| CS9297 | `ERR_PPIgnoredFollowsToken` | `'#:' directives cannot be after first token in file` | 4.14, 5.0, 5.3, 5.12 |
| CS9298 | `ERR_PPIgnoredNeedsFileBasedProgram` | `'#:' directives can be only used in file-based programs ('-features:FileBasedProgram')` | 4.14, 5.0, 5.3, 5.12 |
| CS9299 | `ERR_PPIgnoredFollowsIf` | `'#:' directives cannot be after '#if' directive` | 4.14, 5.0, 5.3, 5.12 |
| CS9314 | `ERR_PPShebangInProjectBasedProgram` | `'#!' directives can be only used in scripts or file-based programs` | **added in 5.0**; absent in 4.14 |
| CS9378 | `ERR_PPShebangNotOnFirstLine` | `'#!' must be the first characters on the first line of the file` | **added in 5.12 (`main`) only**; absent in 5.0 and 5.3 |
| CS1040 | `ERR_BadDirectivePlacement` | `Preprocessor directives must appear as the first non-whitespace character on a line` | pre-existing; **was** the misplaced-shebang error until CS9378 |

Caution: several XML comments inside
`src/Compilers/CSharp/Test/Syntax/Parsing/IgnoredDirectiveParsingTests.cs` still say
"error CS9282" / "error CS9283" for `ERR_PPIgnoredNeedsFileBasedProgram` /
`ERR_PPIgnoredFollowsIf`. Those comments are stale; `ErrorCode.cs` is authoritative
(9298 and 9299 respectively; 9282 and 9283 are extension-member errors).

### 2.7 What the tree actually looks like

From `IgnoredDirectiveParsingTests.FeatureFlag` for source:

```cs
#!xyz
#:name value
```

```
CompilationUnit
  EndOfFileToken
    (leading) ShebangDirectiveTrivia
      HashToken
      ExclamationToken
      EndOfDirectiveToken
        (leading) PreprocessingMessageTrivia "xyz"
        (trailing) EndOfLineTrivia "\n"
    (leading) IgnoredDirectiveTrivia
      HashToken
      ColonToken
      StringLiteralToken "name value"
      EndOfDirectiveToken
```

From `IgnoredDirectiveParsingTests.Api` for `#:abc`:

```csharp
var root = SyntaxFactory.ParseCompilationUnit(source, options: TestOptions.Regular.WithFeature("FileBasedProgram"));
var trivia = root.EndOfFileToken.GetLeadingTrivia().Single();
Assert.Equal(SyntaxKind.IgnoredDirectiveTrivia, trivia.Kind());
Assert.True(SyntaxFacts.IsPreprocessorDirective(trivia.Kind()));
Assert.True(SyntaxFacts.IsTrivia(trivia.Kind()));
var structure = (IgnoredDirectiveTriviaSyntax)trivia.GetStructure()!;
Assert.Equal(":", structure.DirectiveNameToken.ToFullString());
Assert.Empty(structure.EndOfDirectiveToken.GetLeadingTrivia());   // <- content is NOT trivia
var content = structure.Content;
Assert.Equal(SyntaxKind.StringLiteralToken, content.Kind());
Assert.Equal("abc", content.ToString());
```

The `Content` token is created by the lexer as
`SyntaxToken.StringLiteral(message)` where `message` is the raw remainder of the line
(`Lexer.LexEndOfDirectiveWithOptionalContent`, `Lexer.LexOptionalPreprocessingMessage`).
It is a `StringLiteralToken` **without quotes**: `Text` and `ValueText` both hold the raw
line text, including any leading space after `#:`. It is `Optional`, so an empty `#:`
yields `default(SyntaxToken)` for `Content`.

---

## 3. The shebang (`#!`)

### 3.1 Node shape

`Syntax.xml`, lines 5213-5226, preceded by an explanatory comment:

```xml
<!-- The text following '!' up to end of line is attached as leading trivia of kind
     PreprocessingMessageTrivia on the EndOfDirectiveToken. -->
<Node Name="ShebangDirectiveTriviaSyntax" Base="DirectiveTriviaSyntax">
  <Kind Name="ShebangDirectiveTrivia"/>
  <Field Name="HashToken" Type="SyntaxToken" Override="true"><Kind Name="HashToken"/></Field>
  <Field Name="ExclamationToken" Type="SyntaxToken"><Kind Name="ExclamationToken"/></Field>
  <Field Name="EndOfDirectiveToken" Type="SyntaxToken" Override="true"><Kind Name="EndOfDirectiveToken"/></Field>
  <Field Name="IsActive" Type="bool" Override="true"/>
</Node>
```

`SyntaxKind.ShebangDirectiveTrivia = 8922` (long-standing, from C# scripting).

**Asymmetry with `IgnoredDirectiveTriviaSyntax`, and a trap for rewriters.**
`ShebangDirectiveTriviaSyntax.Content` exists as a public property but is **not** a field
in `Syntax.xml` and is **not** part of `Update`. It is hand-written in
`src/Compilers/CSharp/Portable/Syntax/ShebangDirectiveTriviaSyntax.cs`:

```csharp
partial class ShebangDirectiveTriviaSyntax
{
    public SyntaxToken Content
    {
        get
        {
            var token = InternalSyntax.SyntaxToken.StringLiteral(this.EndOfDirectiveToken.LeadingTrivia.ToString());
            return token != null ? new SyntaxToken(this, token, GetChildPosition(2), GetChildIndex(2)) : default;
        }
    }

    public ShebangDirectiveTriviaSyntax WithContent(SyntaxToken content) { … }
}
```

and, on the green node, `WithContent` rewrites the `EndOfDirectiveToken`'s leading trivia
to a `PreprocessingMessage`, throwing `ArgumentException` if the token kind is neither
`StringLiteralToken` nor `None`. So:

- `IgnoredDirectiveTriviaSyntax.Content` — a real child token; visible to
  `ChildNodesAndTokens()`, present in `Update`, round-trips through a generic rewriter.
- `ShebangDirectiveTriviaSyntax.Content` — a **synthesised** token that is not a child;
  the real storage is `PreprocessingMessageTrivia` on `EndOfDirectiveToken`, and
  `Update` has only four parameters.

`ShebangDirectiveTriviaSyntax.Content` / `WithContent` were **added in Roslyn 5.0**
(present in `release/dev18.0` `PublicAPI.Shipped.txt`, absent from both the shipped and
unshipped API files of `release/dev17.14`). `Update` was not changed.

### 3.2 The "first line, first character" rule

Speclet (`ignored-directives.md`): "the compiler should report a warning if the `#!`
directive is not placed at the first line and the first character in the file (not even a
BOM marker can be in front of it), because otherwise shells won't recognize it."

Implementation is an **error**, not a warning, and the exact predicate is:

```csharp
if (hashPosition != 0 || hash.HasTrailingTrivia)
    hash = this.AddError(hash, ErrorCode.ERR_PPShebangNotOnFirstLine);
```

- `hashPosition != 0` — the `#` must be at absolute offset 0 of the `SourceText`.
- `hash.HasTrailingTrivia` — nothing may sit between `#` and `!` (catches `# !xyz`).
- The shebang is **always parsed as a shebang** even when misplaced (error recovery
  requested by dotnet/roslyn#78054 "ShebangDirective should be unconditionally parsed",
  milestone 18.0 P1, closed 2025-04-18), so a fixer can act on it.

### 3.3 CS9378 replaced CS1040 — this is the .NET 11 change

- Roslyn 5.0 / 5.3 (`release/dev18.0`, `release/dev18.3`): the same code path calls
  `this.AddError(hash, ErrorCode.ERR_BadDirectivePlacement)` → **CS1040**
  *"Preprocessor directives must appear as the first non-whitespace character on a line"*.
- Roslyn 5.12 (`main`): `ErrorCode.ERR_PPShebangNotOnFirstLine = 9378` → **CS9378**
  *"'#!' must be the first characters on the first line of the file"*.

Driver: **dotnet/roslyn issue 83111**, *"Misleading error for #! not being on the first
line and column"*, opened 2026-04-09 against Roslyn 5.7.0, closed 2026-04-10, originally
reported by @333fred in review of dotnet/sdk#53614. The complaint was that CS1040 "doesn't
make sense — the directive is the first character on its line".

`ERR_PPShebangNotOnFirstLine` is absent from `release/dev18.3` (Roslyn 5.3) and present in
`main` (Roslyn 5.12), so CS9378 is on track for .NET 11 GA. Regression test
`IgnoredDirectiveParsingTests.ShebangIncorrectlyPlaced_FileBasedProgram` carries the
comment: "Shebang on the first column of a non-first line should get a specific error, not
the generic 'must appear as the first non-whitespace character on a line' error."

Diagnostic locations from the tests:

| Source | Diagnostic |
|---|---|
| `␠#!xyz` (leading space) | `CS9378` at (1,2) on `#`; plus `CS9314` at (1,3) on `!` in project-based mode |
| `# !xyz` | `CS9378` at (1,1) on `#` |
| `// Comment\n#!xyz` | `CS9378` at (2,1) on `#` |

Note that CS9378 is reported **in addition to** CS9314 in project-based mode.

### 3.4 Shebang, line numbers, `#line` and `FileLinePositionSpan`

There is **no special handling anywhere**. Facts:

- The shebang is ordinary leading trivia on the first token of the compilation unit. It is
  not stripped, not remapped, and not skipped.
- Consequently it occupies **line 1** of the file (1-based, as reported in diagnostics) and
  every subsequent line is shifted by one. A file executed as `./app.cs` reports its first
  statement on line 2.
- No shebang handling exists in `src/Compilers/CSharp/Portable/CSharpLineDirectiveMap.cs`,
  `src/Compilers/Core/Portable/Syntax/LineDirectiveMap.cs`, or
  `src/Compilers/CSharp/Portable/Syntax/CSharpSyntaxTree.cs` (grep for `shebang` /
  `IgnoredDirective` returns nothing in all three). `#line` therefore composes with a
  shebang exactly as it does with any other leading trivia, and
  `SyntaxTree.GetMappedLineSpan` / `FileLinePositionSpan` are unaffected beyond the
  ordinary offset.
- The SDK does not inject `#line` into the compilation: the virtual project adds a
  `<Compile Include="{entryPointFilePath}" />` pointing at the user's original file, so
  positions are positions in that file.
- Practical consequence documented by `dotnet/docs`: "Use `LF` line endings instead of
  `CRLF` when you add a shebang. Don't include a BOM in the file." The Roslyn LSP
  discovery pass additionally accepts a UTF-8 BOM before `#!` (see §5.3), but the compiler
  rule (`hashPosition != 0`) does not.

### 3.5 Recommended shebang forms

From `dotnet/docs/docs/core/sdk/file-based-apps.md`:

```csharp
#!/usr/bin/env -S dotnet --
#:package Spectre.Console

using Spectre.Console;
AnsiConsole.MarkupLine("[green]Hello, World![/]");
```

`--` stops `dotnet` from consuming arguments that match its own parameters; `-S` lets `env`
split the remaining text. If `-S` is unavailable, `#!/usr/bin/env dotnet`.
`dotnet path.cs` is a shortcut for `dotnet run --file path.cs`.

The SDK spec adds that `#!/usr/bin/env dotnet run` "might not work in all shells" because
`dotnet run` may be passed to `env` as a single argument.

### 3.6 `#!` as the multi-file entry-point marker (CA2266)

When a file-based app uses `#:include`, the entry-point file "should start with `#!` to
clearly distinguish it from included files. This helps IDEs to properly handle multi-file
scenarios and discover entry points."

Analyzer **CA2266** `MissingShebangInFileBasedProgram`
(`dotnet/sdk/src/Microsoft.CodeAnalysis.NetAnalyzers/.../Usage/MissingShebangInFileBasedProgram.cs`,
`DiagnosticCategory.Usage`, `RuleLevel.BuildWarning`) warns when the entry point lacks the
shebang in that scenario. Since .NET 11 Preview 7 (dotnet/sdk#54553) it also fires for
`#:ref`. Introduced by dotnet/sdk#53614 and Roslyn PR 80575 ("File-based programs live
directive diagnostics").

Analyzer **CA2267** `PreferQuotedFileBasedProgramDirective` (also `RuleLevel.BuildWarning`)
flags legacy unquoted directive values and offers a code fix.

Both ship inside the SDK's `Microsoft.CodeAnalysis.NetAnalyzers`, i.e. **analyzers do run
over file-based apps** (see §4.4).

---

## 4. How a file-based app is actually compiled

### 4.1 The virtual project

`dotnet/sdk/src/Cli/Microsoft.DotNet.FileBasedPrograms/VirtualProjectBuilder.cs`.

- **Path**: `GetVirtualProjectPath(entryPointFilePath) => entryPointFilePath + ".csproj"`,
  i.e. `app.cs` → `app.cs.csproj`, *beside the source file*. It exists **only in memory**
  (an MSBuild `ProjectRootElement` built from a string); nothing is written there.
  `TryGetEntryPointFilePathFromVirtualProjectPath` inverts the mapping.
- **Baseline**: "The implicit project file is the default project that would be created by
  running `dotnet new console`." Kept in sync by test `DotnetProjectConvertTests.SameAsTemplate`.

`GetDefaultProperties(targetFramework)`:

```csharp
yield return ("OutputType", "Exe");
if (targetFramework != null) yield return ("TargetFramework", targetFramework);
yield return ("ImplicitUsings", "enable");
yield return ("Nullable", "enable");
yield return ("PublishAot", "true");
yield return ("PackAsTool", "true");
```

The emitted project skeleton (virtual case):

```xml
<Project>
  <PropertyGroup>
    <FileBasedAppArtifactsPath>{artifactsPath}</FileBasedAppArtifactsPath>
    <AssemblyName>{fileNameWithoutExtension}</AssemblyName>
    <RootNamespace>$(AssemblyName)</RootNamespace>
    <FileBasedProgram>true</FileBasedProgram>
    <EntryPointFilePath>{entryPointFilePath}</EntryPointFilePath>
    <FileBasedProgramsItemMapping>.cs=Compile;.resx=EmbeddedResource;.json=None;.razor=Content;.dll=Reference</FileBasedProgramsItemMapping>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <DisableDefaultItemsInProjectFolder>true</DisableDefaultItemsInProjectFolder>
    <!-- only when the single default SDK is used: -->
    <EnableDefaultEmbeddedResourceItems>false</EnableDefaultEmbeddedResourceItems>
    <EnableDefaultNoneItems>false</EnableDefaultNoneItems>
    <!-- then GetDefaultProperties(...) -->
  </PropertyGroup>
  <ItemGroup><Clean Include="{artifactsPath}/*" /></ItemGroup>
  <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />
  <PropertyGroup>
    <!-- #:property directives here -->
    <RestoreUseStaticGraphEvaluation>false</RestoreUseStaticGraphEvaluation>
    <Features>$(Features);FileBasedProgram</Features>
    <UserSecretsId>…hash of entry point path…</UserSecretsId>
  </PropertyGroup>
  <!-- #:package / #:project / #:ref / #:include / #:exclude items -->
  <ItemGroup>
    <Compile Include="{entryPointFilePath}" Exclude="@(Compile)" />
  </ItemGroup>
  <ItemGroup>
    <RuntimeHostConfigurationOption Include="EntryPointFilePath" Value="…" />
    <RuntimeHostConfigurationOption Include="EntryPointFileDirectoryPath" Value="…" />
  </ItemGroup>
  <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />
</Project>
```

Key line for tooling authors: **`<Features>$(Features);FileBasedProgram</Features>`**.
That MSBuild property is what reaches `csc` as `-features:FileBasedProgram`
(`Microsoft.CSharp.Core.targets` passes `Features="$(Features)"` to the `Csc` task, and
`CommandLineArgsForDesignTimeEvaluation` appends `-features:$(Features)`).
Any compiler replacement or design-time host that drops `Features` turns every `#:`
directive into CS9298.

`RestoreUseStaticGraphEvaluation` is forced to `false`
(`StaticGraphRestoreNotSupported` resource exists for the error case).

### 4.2 TargetFramework and LangVersion

- **TargetFramework**: the CLI passes `TargetFramework = $"net{Product.TargetFrameworkVersion}"`
  (`VirtualProjectBuildingCommand.TargetFramework`, `CSharpCompilerCommand.TargetFramework`),
  i.e. the SDK's own band — **`net11.0` for the .NET 11 SDK**. A host that supplies no
  default (for example `MSBuildWorkspace`) gets the fallback written into the project:
  `<TargetFramework>net$(BundledNETCoreAppTargetFrameworkVersion)</TargetFramework>`.
  Overridable with `#:property TargetFramework=…`.
- **LangVersion**: **not set at all** — neither by `VirtualProjectBuilder` nor anywhere in
  `Microsoft.NET.Sdk` (grep for `LangVersion` across
  `Microsoft.NET.Sdk.props/.targets`, `Microsoft.NET.Sdk.CSharp.props/.targets`,
  `Microsoft.NET.Sdk.BeforeCommon.props` returns nothing). The compiler default applies:
  `LanguageVersionFacts.MapSpecifiedToEffectiveVersion` maps
  `Default`/`Latest`/`LatestMajor` to `LanguageVersion.CSharp15` on Roslyn `main`
  (`CurrentVersion => LanguageVersion.CSharp15`, `CSharp15 = 1500`). Both the SDK spec and
  the csharplang speclet show `#:property LangVersion=preview` as the way to opt into
  preview features.
- Other implicit defaults: `OutputType=Exe`, `ImplicitUsings=enable`, `Nullable=enable`,
  `PublishAot=true`, `PackAsTool=true`, `AssemblyName`/`RootNamespace` = file name without
  extension, `UserSecretsId` = hash of the entry-point path.

### 4.3 Build outputs and caching

- Outputs go under the artifacts output layout when enabled, otherwise under a temp/appdata
  subdirectory named `{fileNameWithoutExtension}-{sha256 of the full path}`
  (`VirtualProjectBuilder.GetArtifactsPath`, `Sha256Hasher.HashWithNormalizedCasing`),
  created with `0700` permissions; the run fails if that cannot be done.
  `dotnet/docs` gives the shape as `<temp>/dotnet/runfile/<appname>-<appfilesha>/bin/<configuration>/`.
- Background cleanup every 2 days removes artifacts unused for 30 days; disable with
  `DOTNET_CLI_DISABLE_FILE_BASED_APP_ARTIFACTS_AUTOMATIC_CLEANUP=true`; manual:
  `dotnet clean file-based-apps` (with `--days`).
- Diagnostics for caching decisions: `DOTNET_CLI_CONTEXT_VERBOSE=true`.

### 4.4 Analyzers, source generators, and the `Csc` task

**On the full build path, yes — everything normal runs.** The virtual project imports
`Sdk.props`/`Sdk.targets` of `Microsoft.NET.Sdk`, so `CoreCompile` and the `Csc` task
execute exactly as for a `.csproj`: the SDK's `Microsoft.CodeAnalysis.NetAnalyzers`
(including CA2266/CA2267 above), any analyzers or source generators brought in by
`#:package`, and any custom targets from `Directory.Build.props` / `Directory.Build.targets`
in the file's directory tree (which *are* imported — the docs devote a section to it).

**But there are three build levels** (`src/Cli/dotnet/Commands/Run/BuildLevel.cs`):

```csharp
internal enum BuildLevel
{
    None,   // Build outputs are up to date.
    Csc,    // Only direct C# compilation is needed.
    All,    // MSBuild is needed.
}
```

The `Csc` level **bypasses MSBuild entirely**. `VirtualProjectBuildingCommand` caches the
csc command line taken from the `CoreCompile` target's return items —
`Microsoft.CSharp.Core.targets` declares `Returns="@(CscCommandLineArgs)"`, fed from the
`Csc` task's `CommandLineArgs` output:

```csharp
cache.CurrentEntry.CscArguments = coreCompileResult.Items
    .Select(static i => i.GetMetadata(Constants.Identity))
    .Where(static a => a != "/noconfig")
    .Select(Escape)
    .ToImmutableArray();
```

writes them to `{artifactsPath}/csc.rsp` (`CSharpCompilerCommand.WriteCscRspFile`), and on
a later `dotnet run` replays them straight at the **Roslyn compiler server**:

```csharp
var buildRequest = BuildServerConnection.CreateBuildRequest(
    requestId: EntryPointFileFullPath,
    language: RequestLanguage.CSharpCompile,
    arguments: ["/noconfig", "/nologo", $"@{EscapeSingleArg(rspPath)}"],
    …);
var pipeName = BuildServerConnection.GetPipeName(clientDirectory: ClientDirectory); // <sdk>/Roslyn/bincore
```

Consequences:

- Analyzer and generator **references survive**, because they are `/analyzer:` entries in
  the cached response file (`s_pathOptions` includes `analyzer:`, `analyzerconfig:`,
  `additionalfile:`, `reference:`, `embed:`, `resource:`, `linkresource:`, `ruleset:`,
  `keyfile:`, `link:`).
- A **custom `Csc` MSBuild task or replacement compiler is *not* re-invoked** on that path;
  only stock `csc` runs, from `<sdk>/Roslyn/bincore`, against the arguments the custom task
  reported through `CommandLineArgs` on the previous full build.
- Opt-out: MSBuild property **`FileBasedProgramCanSkipMSBuild=false`**, or the CLI flags
  `--no-cache` / running `dotnet build file.cs`.
- The cache is also refused (`CanSaveCache`) when the app has a `#:project` directive, a
  `#:ref` directive, or a glob `#:include` (containing `*` or `?`).
- The SDK falls back to full MSBuild when a framework pack is missing or when `CS0006` is
  seen in the fast-path output.

### 4.5 `dotnet build` / `dotnet publish` / `dotnet pack` / `dotnet run` differences

| Command | Behaviour |
|---|---|
| `dotnet run file.cs` | Full pipeline with the up-to-date check and the three build levels above. Falls back to project-based `dotnet run` if a project file is found in the current directory or via `--project`; use `--file` to force. `dotnet run -` reads the program from stdin (single-file compilation, no launch-profile lookup). |
| `dotnet file.cs` | Shortcut for `dotnet run --file file.cs`, when the path is a valid target path, not a DLL, not a built-in command, and not a NuGet tool name. |
| `dotnet build file.cs` | Supported (needed for IDE support). Forces a full build, bypassing the "skip build" optimisation. Output defaults to the temp artifacts directory; `--output` or `#:property OutputPath=./output` changes it. |
| `dotnet publish file.cs` | Supported. **`PublishAot=true` is implicit**, so publish uses Native AOT and *building* emits AOT warnings. Opt out with `#:property PublishAot=false`. `PublishDir` defaults to `./artifacts/` next to the `.cs` file (unless the artifacts output layout is enabled). |
| `dotnet pack file.cs` | Supported. **`PackAsTool=true` is implicit**; opt out with `#:property PackAsTool=false`. `PackageOutputPath` also defaults to `./artifacts/`. |
| `dotnet restore file.cs` | Supported. |
| `dotnet clean file.cs` | Cleans the app's artifacts. `dotnet clean file-based-apps` cleans all cached file-based-app artifacts. |
| `dotnet project convert file.cs` | Materialises the implicit project on disk. `#:` and `#!` are removed from the `.cs` files; `#:include`d files are copied into the new project directory; `#:ref` becomes a sibling library project, recursively. `EnableDefault*Items` and the synthetic `Compile` item are not preserved. |
| `dotnet package add/list`, `dotnet nuget why`, `dotnet reference add/list/remove` | Operate on the virtual project and edit the `#:package` / `#:project` / `#:ref` directives in place (`--file app.cs`). |

`RuntimeHostConfigurationOption`s make the entry point discoverable at run time (not for
`Publish`/`Pack`):

```csharp
string? filePath      = AppContext.GetData("EntryPointFilePath") as string;
string? directoryPath = AppContext.GetData("EntryPointFileDirectoryPath") as string;
```

`EntryPointFilePath` is also exposed to analyzers via `CompilerVisibleProperty`.

Launch profiles: a flat `<AppName>.run.json` beside the source, or the traditional
`Properties/launchSettings.json` (which wins, with a warning, if both exist).

---

## 5. IDE behaviour (VS Code C# extension, Roslyn LSP, Visual Studio)

Primary source: `dotnet/roslyn` `docs/features/file-based-programs-vscode.md`.

### 5.1 Classification of a loose `.cs` file

`LooseDocumentKind` (`src/LanguageServer/Microsoft.CodeAnalysis.LanguageServer/FileBasedPrograms/LooseDocumentKind.cs`):

```csharp
internal enum LooseDocumentKind
{
    MiscellaneousFileWithNoReferences,
    MiscellaneousFileWithStandardReferences,
    MiscellaneousFileWithStandardReferencesAndSemanticErrors,
    FileBasedApp,
}
```

Decision tree (abridged from the spec):

1. In a loaded project → **Project-Based App**.
2. `enableFileBasedPrograms` off → **Misc File With No References**.
3. Not a plain `.cs` (e.g. `.csx`) → **Misc File With No References**.
4. No absolute path / not on disk → **Misc File With Standard References**.
5. Has `#!` → **File-Based App**; restore if needed, show semantic errors.
6. Has `#:` → go to 7, else 8.
7. Has top-level statements → **File-Based App**; else **Misc File With Standard References**.
8. `enableFileBasedProgramsWhenAmbiguous` off → **Misc File With Standard References**;
   on → heuristics: top-level statements + not inside a `.csproj` cone →
   **Misc File With Standard References and Semantic Errors** (rich misc files, **not restored**).

### 5.2 "Do not restore loose files which lack file-based app directives"

**dotnet/roslyn issue 81252**, *"Do not restore loose files which lack file-based app
directives"*, `Area-IDE`, `Feature - Run File`, **milestone 18.3** (Roslyn 5.3), opened
2025-11-14, closed completed 2025-11-21. It removed the "no top-level statements"
criterion so that a file is *only ever* restored and treated as a file-based app when it
carries `#!` or `#:`. Rationale in the issue: the "part of a loaded project" condition
changes ambiently over time (projects load asynchronously, files move), which produced
frequent unwanted restore pop-ups and stray artifacts in the user's repository. The
replacement is a single "canonical miscellaneous files project" under the temp directory
(dotnet/roslyn#80743), used as a base project for loose files, giving semantic errors,
completion and Quick Info for the core library without a restore.

So, as of .NET 11:

```cs
Console.WriteLine("Hello World!");        // never a file-based app in the editor
```

```cs
#!/usr/bin/env dotnet                      // or ANY #: directive
Console.WriteLine("Hello World");          // affirmatively a file-based app; restored
```

Follow-on work: dotnet/roslyn#84385 "Extract file-based app entry point heuristic";
dotnet/roslyn#78878 "Determine if a source file contains top-level statements without doing
a full additional parse".

### 5.3 Automatic discovery and the `#!` byte requirement

The Roslyn LSP discovers and loads file-based apps across the opened workspace folders
(dotnet/roslyn#82863). Setting: **`dotnet.fileBasedApps.enableAutomaticDiscovery`**
(added in vscode-csharp 2.134.x, PR dotnet/vscode-csharp#9096); disabled by default in the
stable channel, enabled by default in prerelease for the first release.

Excluded from discovery: folders containing a `.csproj`, folders named `artifacts`, `bin`,
`obj`, and hidden folders (`.git`, `.vs`). Dot-prefixed directories are skipped
(dotnet/roslyn#83547).

**A discoverable file must start with the byte sequence `0x23 0x21` (`#!`), or
`0xEF 0xBB 0xBF 0x23 0x21` (UTF-8 BOM then `#!`).** Reason given: `#:` will eventually be
allowed in non-entry-point files, so `#:` alone can no longer identify an entry point, and
scanning for top-level statements is too expensive for a broad discovery pass.

A cache file in the user temp directory records the previous pass's start time, the paths
of file-based apps found, and the folders found to contain `.csproj` files.

### 5.4 Opt-out settings

| Setting | Default | Effect |
|---|---|---|
| `dotnet.projects.enableFileBasedPrograms` | `true` (release) | master switch; `false` reverts to the old miscellaneous-files experience |
| `dotnet.projects.enableFileBasedProgramsWhenAmbiguous` | `false` in release, `true` in prerelease | governs only the heuristic case (top-level statements, no directives); ignored when the master switch is off |
| `dotnet.fileBasedApps.enableAutomaticDiscovery` | off in stable, on in prerelease | workspace-wide discovery pass |

### 5.5 `FileBasedProgramsProjectSystem` and the live directive analyzer

`FileBasedProgramsProjectSystem` manages projects for file-based apps and miscellaneous
files. It translates the entry-point file into a virtual MSBuild project, runs a
design-time build on it, restores when assets are missing, and uses file watchers on the
project globs to redo the design-time build when `#:` directives change.

`FileLevelDirectiveDiagnosticAnalyzer`
(`src/Features/CSharp/Portable/Diagnostics/Analyzers/FileBasedPrograms/FileLevelDirectiveDiagnosticAnalyzer.cs`)
reports directive-content errors live in the editor:

```csharp
public const string DiagnosticId = "FileBasedPrograms";   // literally this string
// defaultSeverity: DiagnosticSeverity.Error, enforceOnBuild: EnforceOnBuild.Never,
// isConfigurable: false, category: syntax-tree-without-semantics
context.RegisterSyntaxTreeAction(context =>
{
    if (!tree.Options.Features.ContainsKey("FileBasedProgram")) return;
    if (!root.ContainsDirectives) return;
    FileLevelDirectiveHelpers.FindLeadingDirectives(new SourceFile(tree.FilePath, tree.GetText(...)),
                                                    root.GetLeadingTrivia(), errorReporter, builder: null);
    …
});
```

`helpLinkUri` points at
`https://learn.microsoft.com/dotnet/csharp/language-reference/preprocessor-directives#file-based-apps`.

Completion providers exist for every directive kind
(`src/Features/CSharp/Portable/Completion/CompletionProviders/FileBasedPrograms/`):
`SdkAppDirectiveCompletionProvider`, `PackageAppDirectiveCompletionProvider`,
`ProjectAppDirectiveCompletionProvider`, `PropertyAppDirectiveCompletionProvider`,
`IncludeAppDirectiveCompletionProvider`, **`RefAppDirectiveCompletionProvider`**, and the
shared `AbstractAppDirectiveCompletionProvider`.

Formatting explicitly preserves `#:` directives (dotnet/roslyn#82996, vscode-csharp 2.134.x).

### 5.6 The malformed configuration the IDE rejects

"It is not valid for a file-based app *entry point* to be a member of an ordinary project."
An error is reported for the presence of `#:` / `#!` in ordinary projects, and, depending on
load order, such a file may or may not also be detected as an entry point. The user must
either delete the directives or remove the file from the project.

### 5.7 Visual Studio

Visual Studio 2026 supports the file-based-app directives in the C# editor. I did not find
a primary source describing a **.NET 11-specific** change to how `devenv.exe` opens and
analyses a loose `.cs` file carrying these directives; the detailed classification and
restore behaviour above is documented only for the Roslyn LSP / VS Code path. Treat the
Visual Studio side as unverified (open question 5 below).

### 5.8 vscode-csharp / Roslyn version correlation (from `dotnet/vscode-csharp/CHANGELOG.md`)

| Extension | Roslyn | Relevant item |
|---|---|---|
| 2.150.x | 5.12.0-1.26428.1 | current |
| 2.149.x | 5.11.0-1.26405.8 | "File-based apps: Add support for `#:ref` directive" (dotnet/roslyn#83985) |
| 2.147.x | 5.10.0-1.26376.1 | "File-based apps: avoid running a few irrelevant editor features" (#84575) |
| 2.134.x | 5.7.0-1.26203.6 | `dotnet.fileBasedApps.enableAutomaticDiscovery`; automatic discovery (#82863); "Preserve #: file-based app directives during formatting" (#82996) |
| earlier | 5.x | `EnableFileBasedProgramsWhenAmbiguous` (#81513); live directive diagnostics (#80575); canonical misc files project (#80748) |

Also: "force using a single msbuild node for design-time builds" for file-based apps
(dotnet/roslyn#84183); "Improve classification of file-based app directives" (#82627);
"Implement csproj-in-cone check" (#82633).

---

## 6. Consolidated facts a syntax-rewriting toolchain must know

1. `SyntaxKind.IgnoredDirectiveTrivia = 9080` and `SyntaxKind.ShebangDirectiveTrivia = 8922`
   are both `IsPreprocessorDirective` and `IsTrivia`. Any `SyntaxKind` switch generated
   from `Syntax.xml` must handle `IgnoredDirectiveTriviaSyntax` with **five** fields:
   `HashToken`, `ColonToken`, `Content` (optional `StringLiteralToken`), `EndOfDirectiveToken`,
   `IsActive`.
2. A rewriter generated against Roslyn 4.14's `Syntax.xml` will call the 4-argument
   `IgnoredDirectiveTriviaSyntax.Update`, which no longer exists from Roslyn 5.0 onward.
3. `ShebangDirectiveTriviaSyntax.Content` is *not* a child token; it is derived from
   `EndOfDirectiveToken.LeadingTrivia`. `Update` takes four arguments. `WithContent` throws
   `ArgumentException` for a token kind other than `StringLiteralToken` or `None`.
4. Parsing a file-based app source with default `CSharpParseOptions` produces CS9298 for
   every `#:` and CS9314 for `#!`. The enabling switch is
   `CSharpParseOptions.WithFeatures([new("FileBasedProgram", "true")])` /
   `-features:FileBasedProgram`, surfaced through the MSBuild `Features` property.
5. `#:` and `#!` are legal only before the first C# token and before any `#if`; the shebang
   additionally only at absolute offset 0 with no trivia between `#` and `!`.
6. Emitting any C# text before a shebang, or shifting the file start, converts a valid
   shebang into CS9378 (previously CS1040). Generated code that prepends a header to a
   file-based app entry point breaks it.
7. On the `BuildLevel.Csc` fast path, `dotnet run file.cs` replays a cached `csc.rsp`
   directly against the Roslyn build server; a replacement `Csc` task is not re-invoked.
   `FileBasedProgramCanSkipMSBuild=false` disables that path.
8. The virtual project is `app.cs.csproj` in memory, never on disk;
   `MSBuildWorkspace`-style hosts go through
   `src/Workspaces/MSBuild/Core/MSBuild/FileBasedProgramsProjectLoader.cs` and
   `IFileBasedProgramService` (`src/Workspaces/Core/Portable/FileBasedPrograms/`).
9. `#:` directives are no longer confined to the entry-point file: since .NET 11 Preview 5
   the SDK reads them from every `Compile` item, so `IgnoredDirectiveTrivia` can appear in
   any source file of a file-based app.
10. The IDE parses directive content with the SDK's own code, vendored into
    `src/Workspaces/CSharp/Portable/SyncedSource/FileBasedPrograms/`.

---

## 7. Open questions

1. Will `#:ref` be ungated (no `ExperimentalFileBasedProgramEnableRefDirective`) by .NET 11
   GA? It is still described as experimental in `dotnet-run-file.md` on `main`, and the
   learn *What's new for .NET 11* page does not mention it.
2. Will `#:exclude` and `#:ref` be added to
   `dotnet/docs/docs/core/sdk/file-based-apps.md` before GA? The documentation currently
   lists five directives; the implementation supports seven.
3. Is there a .NET 11-specific change in Visual Studio (`devenv.exe`, not the LSP) for
   opening a loose `.cs` file with `#:`/`#!`? No primary source found.
4. Will `LanguageVersion.CSharp15 = 1500` remain unshipped-then-shipped as expected, making
   the effective default `LangVersion` for a `net11.0` file-based app C# 15 at GA?
   (It is in `PublicAPI.Unshipped.txt` today.)
5. Does `ERR_PPShebangNotOnFirstLine` (CS9378) get serviced back to any 5.x release branch,
   or is it .NET 11 only? Only `main` carries it today; `release/dev18.3` does not, and
   no `release/dev18.4`…`dev18.12` branches exist publicly.
6. The exact list of "a few irrelevant editor features" disabled for file-based apps
   (dotnet/roslyn#84575) was not inspected.

---

## 8. Source index

SDK
- https://github.com/dotnet/sdk/blob/main/documentation/general/dotnet-run-file.md
- https://github.com/dotnet/sdk/blob/main/src/Cli/Microsoft.DotNet.FileBasedPrograms/FileLevelDirectiveHelpers.cs
- https://github.com/dotnet/sdk/blob/main/src/Cli/Microsoft.DotNet.FileBasedPrograms/VirtualProjectBuilder.cs
- https://github.com/dotnet/sdk/blob/main/src/Cli/dotnet/Commands/Run/VirtualProjectBuildingCommand.cs
- https://github.com/dotnet/sdk/blob/main/src/Cli/dotnet/Commands/Run/CSharpCompilerCommand.cs
- https://github.com/dotnet/sdk/blob/main/src/Cli/dotnet/Commands/Run/BuildLevel.cs
- https://github.com/dotnet/sdk/blob/main/src/Cli/dotnet/Commands/Run/RunFileBuildCacheEntry.cs
- https://github.com/dotnet/sdk/blob/main/src/Microsoft.CodeAnalysis.NetAnalyzers/src/Microsoft.CodeAnalysis.NetAnalyzers/Microsoft.NetCore.Analyzers/Usage/MissingShebangInFileBasedProgram.cs
- https://github.com/dotnet/sdk/blob/main/src/Microsoft.CodeAnalysis.NetAnalyzers/src/Microsoft.CodeAnalysis.NetAnalyzers/Microsoft.NetCore.Analyzers/Usage/PreferQuotedFileBasedProgramDirective.cs

Roslyn
- https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Syntax/Syntax.xml (lines 5213-5241)
- https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Syntax/DirectiveTriviaSyntax.cs
- https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Syntax/ShebangDirectiveTriviaSyntax.cs
- https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Parser/DirectiveParser.cs
- https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Parser/Lexer.cs
- https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Errors/ErrorCode.cs
- https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/CSharpResources.resx
- https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/CSharpParseOptions.cs
- https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Syntax/SyntaxKindFacts.cs
- https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/LanguageVersion.cs
- https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/PublicAPI.Shipped.txt
- https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/PublicAPI.Unshipped.txt
- https://github.com/dotnet/roslyn/blob/release/dev17.14/src/Compilers/CSharp/Portable/PublicAPI.Unshipped.txt
- https://github.com/dotnet/roslyn/blob/release/dev18.0/src/Compilers/CSharp/Portable/PublicAPI.Shipped.txt
- https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Test/Syntax/Parsing/IgnoredDirectiveParsingTests.cs
- https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/MSBuildTask/Microsoft.CSharp.Core.targets
- https://github.com/dotnet/roslyn/blob/main/docs/features/file-based-programs-vscode.md
- https://github.com/dotnet/roslyn/blob/main/src/LanguageServer/Microsoft.CodeAnalysis.LanguageServer/FileBasedPrograms/LooseDocumentKind.cs
- https://github.com/dotnet/roslyn/blob/main/src/Features/CSharp/Portable/Diagnostics/Analyzers/FileBasedPrograms/FileLevelDirectiveDiagnosticAnalyzer.cs
- https://github.com/dotnet/roslyn/tree/main/src/Workspaces/CSharp/Portable/SyncedSource/FileBasedPrograms
- https://github.com/dotnet/roslyn/issues/77697 (API for IgnoredDirectiveTrivia)
- https://github.com/dotnet/roslyn/issues/81252 (Do not restore loose files which lack file-based app directives)
- https://github.com/dotnet/roslyn/issues/78054 (ShebangDirective should be unconditionally parsed)
- https://github.com/dotnet/roslyn/issues/83111 (Misleading error for #! not being on the first line and column)

csharplang
- https://github.com/dotnet/csharplang/blob/main/proposals/csharp-14.0/ignored-directives.md
- https://github.com/dotnet/csharplang/issues/8617

Docs / release notes
- https://github.com/dotnet/docs/blob/main/docs/core/sdk/file-based-apps.md
- https://github.com/dotnet/docs/blob/main/docs/csharp/language-reference/preprocessor-directives.md
- https://github.com/dotnet/docs/blob/main/docs/core/whats-new/dotnet-11/sdk.md
- https://learn.microsoft.com/dotnet/core/sdk/file-based-apps
- https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/sdk
- https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview3/sdk.md
- https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview5/sdk.md
- https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview6/sdk.md
- https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/sdk.md
- https://github.com/dotnet/vscode-csharp/blob/main/CHANGELOG.md
