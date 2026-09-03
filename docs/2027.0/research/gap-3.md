# Gap 3 — C# Interceptors in the .NET 11 wave (Roslyn 5.x)

Research date: 2026-09-03. All statements verified against `dotnet/roslyn` `main`
(Roslyn version prefix **5.12.0-1**, per `eng/Versions.props`: `MajorVersion=5`, `MinorVersion=12`,
`PatchVersion=0`, `PreReleaseVersionLabel=1`), `dotnet/csharplang` `main`, `dotnet/sdk` `main`,
`dotnet/docs` `main`, and learn.microsoft.com.

Roslyn `main` also reports `LanguageVersion.CurrentVersion => LanguageVersion.CSharp15`
(`src/Compilers/CSharp/Portable/LanguageVersion.cs`), and `CSharp15 = 1500` is the only new
`LanguageVersion` in `PublicAPI.Unshipped.txt`. So `main` today is the .NET 11 / C# 15 wave.

---

## 0. Executive summary (the short answer)

**The interceptors feature did not change in the .NET 11 wave.** But the negative in the inventory
was derived from the wrong evidence. Here is the correct evidence, and the facts that matter to
Metalama regardless of whether anything changed:

| Question | Answer | Evidence |
|---|---|---|
| Public API changed in Roslyn 5.x? | **No.** Every interceptor API is in `PublicAPI.Shipped.txt`; `PublicAPI.Unshipped.txt` (97 lines, all C# 15 syntax + unions + closed + unsafe-evolution + PreCompilation) contains **zero** interceptor entries. | `src/Compilers/CSharp/Portable/PublicAPI.{Shipped,Unshipped}.txt` |
| Feature doc changed? | **No.** `docs/features/interceptors.md` last modified **2024-12-13** (`Mark interceptors feature as stable and deprecate file path based attributes (#76312)`). Untouched for ~21 months. | GitHub commits API on that path |
| `InterceptableLocation.cs` changed? | **No.** Last modified **2025-02-17** (`Make InterceptableLocation implement IEquatable<InterceptableLocation> (#77137)`), i.e. the .NET 10 wave. | GitHub commits API |
| Version 2 encoding? | **Does not exist.** Compiler rejects `version != 1` outright; `CS9232` text hardcodes *"The latest supported version is '1'."* | `SourceMethodSymbolWithAttributes.DecodeInterceptsLocationChecksumBased`; `CSharpResources.resx` |
| Content checksum still xxHash128? | **Yes**, 16 bytes, `System.IO.Hashing.XxHash128` over UTF-16 code units forced to little-endian. | `SourceText.GetContentHash()` |
| Interceptable member set widened? | **Only for `extension` block methods** (which landed 2025-06-26, `Extensions: interceptors (#79010)`, .NET 10 wave). Properties, indexers, constructors, operators, local functions, delegates, function pointers remain **not** interceptable. | `InterceptorsTests.cs` `Extensions_01..Extensions_29`; `ERR_InterceptableMethodMustBeOrdinary` tests |
| `InterceptorsNamespaces` opt-in still required? | **Yes, unconditionally**, and it is checked before file resolution. `InterceptorsPreviewNamespaces` still works as an MSBuild-only alias. | `Csc.AddInterceptorsNamespaces`; `DecodeInterceptsLocationChecksumBased` |
| Interceptor from `RegisterPreCompilationSourceOutput`? | **Emittable but the location API is unavailable there** — the pre-compilation phase has no `Compilation`, no `SemanticModel` and no syntax store (accessing them throws `InvalidOperationException`). | `docs/features/pre-compilation-source-outputs.md` |
| Interaction with runtime async / unions / closed / unsafe evolution? | **None specified and none implemented.** Zero occurrences of "intercept" in the runtime-async design doc, in `proposals/unsafe-evolution.md`, or in the .NET 11 compiler breaking-change doc. | grep of all three |

**The single most important fact for Metalama** (unchanged, but load-bearing):
the version 1 `InterceptsLocation` data embeds the **xxHash128 content checksum of the entire file
containing the intercepted call**, and the compiler resolves the target file **purely by that
checksum — the path is never used for matching**. Any rewrite of a user source file, however
cosmetic, changes the checksum and makes every interceptor targeting that file fail with **CS9234**.

---

## 1. Complete public API surface

### 1.1 `Microsoft.CodeAnalysis.CSharp.InterceptableLocation`

File: `src/Compilers/CSharp/Portable/Utilities/InterceptableLocation.cs`
(note the path — *Utilities*, not *Compilation*).

```csharp
namespace Microsoft.CodeAnalysis.CSharp;

/// <summary>Denotes an interceptable call. Used by source generators to generate '[InterceptsLocation]' attributes.</summary>
/// <seealso href="https://github.com/dotnet/roslyn/issues/72133" />
/// <seealso href="https://github.com/dotnet/csharplang/issues/7009" />
public abstract class InterceptableLocation : IEquatable<InterceptableLocation>
{
    private protected InterceptableLocation() { }

    public abstract int Version { get; }
    public abstract string Data { get; }
    public abstract string GetDisplayLocation();

    public abstract override bool Equals(object? obj);
    public abstract override int GetHashCode();
    public abstract bool Equals(InterceptableLocation? other);
}
```

Notes carried by the XML documentation:

* `Version` — "The version of the location encoding. Used as an argument to 'InterceptsLocationAttribute'."
* `Data` — "Opaque data which references a call when used as an argument to 'InterceptsLocationAttribute'.
  The value does not require escaping, i.e. it is valid in a string literal when wrapped in `"` (double-quote) characters."
  Consequence: it is safe to interpolate straight into a `"..."` literal without escaping. Base64 alphabet only.
* `GetDisplayLocation()` — "Gets a human-readable representation of the location, suitable for including in comments in generated code."

There is **no** public constructor, no public subclass, and no public factory other than
`SemanticModel.GetInterceptableLocation`. A framework cannot construct an `InterceptableLocation`
without a `SemanticModel` over the compilation that contains the call.

The concrete type is `internal sealed class InterceptableLocation1 : InterceptableLocation`,
holding `_checksum` (`ImmutableArray<byte>`, 16 bytes), `_path`, `_resolver`
(`SourceReferenceResolver?`), `_position`, `_lineNumberOneIndexed`, `_characterNumberOneIndexed`.

`InterceptableLocation1.ContentHashLength = 16` (internal const).

### 1.2 Exact `PublicAPI.Shipped.txt` entries

From `src/Compilers/CSharp/Portable/PublicAPI.Shipped.txt` (line numbers as of today):

```
16:  abstract Microsoft.CodeAnalysis.CSharp.InterceptableLocation.Data.get -> string!
17:  abstract Microsoft.CodeAnalysis.CSharp.InterceptableLocation.Equals(Microsoft.CodeAnalysis.CSharp.InterceptableLocation? other) -> bool
18:  abstract Microsoft.CodeAnalysis.CSharp.InterceptableLocation.GetDisplayLocation() -> string!
19:  abstract Microsoft.CodeAnalysis.CSharp.InterceptableLocation.Version.get -> int
269: Microsoft.CodeAnalysis.CSharp.InterceptableLocation
3516: override abstract Microsoft.CodeAnalysis.CSharp.InterceptableLocation.Equals(object? obj) -> bool
3517: override abstract Microsoft.CodeAnalysis.CSharp.InterceptableLocation.GetHashCode() -> int
4701: static Microsoft.CodeAnalysis.CSharp.CSharpExtensions.GetInterceptableLocation(this Microsoft.CodeAnalysis.SemanticModel? semanticModel, Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax! node, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) -> Microsoft.CodeAnalysis.CSharp.InterceptableLocation?
4702: static Microsoft.CodeAnalysis.CSharp.CSharpExtensions.GetInterceptorMethod(this Microsoft.CodeAnalysis.SemanticModel? semanticModel, Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax! node, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) -> Microsoft.CodeAnalysis.IMethodSymbol?
4703: static Microsoft.CodeAnalysis.CSharp.CSharpExtensions.GetInterceptsLocationAttributeSyntax(this Microsoft.CodeAnalysis.CSharp.InterceptableLocation! location) -> string!
```

`PublicAPI.Unshipped.txt` contains **no** interceptor lines. This is the decisive negative for
"did the interceptor API change in the .NET 11 wave": Roslyn's public-API tracking would require an
`Unshipped` entry for any addition.

Note the receiver is `SemanticModel?` (nullable) on both extension methods, so
`someModel.GetInterceptableLocation(...)` returns `null` rather than throwing when the model is null.

**There is a third extension method the assignment did not name**:
`GetInterceptsLocationAttributeSyntax`. Implementation
(`src/Compilers/CSharp/Portable/CSharpExtensions.cs`, ~line 1706):

```csharp
/// <summary>
/// Gets an attribute list syntax consisting of an InterceptsLocationAttribute, which intercepts the call referenced by parameter <paramref name="location"/>.
/// </summary>
public static string GetInterceptsLocationAttributeSyntax(this InterceptableLocation location)
{
    return $"""[global::System.Runtime.CompilerServices.InterceptsLocationAttribute({location.Version}, "{location.Data}")]""";
}
```

So the recommended emission is a fully global-qualified attribute list, produced by Roslyn itself.

### 1.3 `GetInterceptableLocation` implementation

`src/Compilers/CSharp/Portable/Compilation/CSharpSemanticModel.cs`, ~line 5270:

```csharp
#pragma warning disable RSEXPERIMENTAL002 // Internal usage of experimental API
public InterceptableLocation? GetInterceptableLocation(InvocationExpressionSyntax node, CancellationToken cancellationToken)
{
    CheckSyntaxNode(node);
    if (node.GetInterceptableNameSyntax() is not { } nameSyntax)
    {
        return null;
    }

    return GetInterceptableLocationInternal(nameSyntax, cancellationToken);
}

// Factored out for ease of test authoring, especially for scenarios involving unsupported syntax.
internal InterceptableLocation GetInterceptableLocationInternal(SyntaxNode nameSyntax, CancellationToken cancellationToken)
{
    var tree = nameSyntax.SyntaxTree;
    var text = tree.GetText(cancellationToken);
    var path = tree.FilePath;
    var checksum = text.GetContentHash();

    var lineSpan = nameSyntax.Location.GetLineSpan().Span.Start;
    var lineNumberOneIndexed = lineSpan.Line + 1;
    var characterNumberOneIndexed = lineSpan.Character + 1;

    return new InterceptableLocation1(checksum, path, Compilation.Options.SourceReferenceResolver, nameSyntax.Position, lineNumberOneIndexed, characterNumberOneIndexed);
}
```

`#pragma warning disable RSEXPERIMENTAL002` is now **dead**: `RSEXPERIMENTAL002` no longer exists in
`src/Compilers/Core/Portable/InternalUtilities/RoslynExperiments.cs`. That file today defines
`RSEXPERIMENTAL001` (NullableDisabledSemanticModel), `RSEXPERIMENTAL004` (GeneratorHostOutputs),
`RSEXPERIMENTAL006` (PreviewLanguageFeatureApi), `RSEXPERIMENTAL007` (PreCompilationSourceOutput),
and comments recording `RSEXPERIMENTAL003` and `RSEXPERIMENTAL005` as retired. `RSEXPERIMENTAL002`
was the interceptors experimental identifier (issue #72133); it was retired when the API shipped,
which confirms the interceptor API is fully stable and non-experimental.

### 1.4 `GetInterceptableNameSyntax` — which syntax yields a location

`src/Compilers/CSharp/Portable/Syntax/SyntaxNodeExtensions.cs`, line 366:

```csharp
internal static SimpleNameSyntax? GetInterceptableNameSyntax(this InvocationExpressionSyntax invocation)
{
    // If a qualified name is used as a valid receiver of an invocation syntax at some point,
    // we probably want to treat it similarly to a MemberAccessExpression.
    // However, we don't expect to encounter it.
    Debug.Assert(invocation.Expression is not QualifiedNameSyntax);

    return invocation.Expression switch
    {
        MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
        MemberBindingExpressionSyntax memberBinding => memberBinding.Name,
        SimpleNameSyntax name => name,
        _ => null
    };
}
```

So the API accepts only `InvocationExpressionSyntax`, and only when its `Expression` is
`X.Name(...)`, `X?.Name(...)` (member binding), or a bare `Name(...)`. Everything else returns
`null`. `ptr->M()` is a `MemberAccessExpressionSyntax` of kind
`SyntaxKind.PointerMemberAccessExpression`, so it is covered by the first arm.

### 1.5 `GetInterceptorMethod` (the reverse direction)

```csharp
/// <summary>If the call represented by <paramref name="node"/> is referenced in an InterceptsLocationAttribute,
/// returns the original definition symbol which is decorated with that attribute. Otherwise, returns null.</summary>
public static IMethodSymbol? GetInterceptorMethod(this SemanticModel? semanticModel, InvocationExpressionSyntax node, CancellationToken cancellationToken = default)
```

Backed by `CSharpCompilation.TryGetInterceptor(SimpleNameSyntax?)`, which first forces
`((SourceModuleSymbol)SourceModule).DiscoverInterceptorsIfNeeded()` and then looks up
`(node.SyntaxTree.GetText().GetContentHash(), node.Position)` in the compilation's
`_interceptions` dictionary. Tracking issue: <https://github.com/dotnet/roslyn/issues/72093>.

---

## 2. Version 1 data encoding — exact byte layout, and validation

### 2.1 Attribute shape recognised by the compiler

`src/Compilers/Core/Portable/Symbols/Attributes/AttributeDescription.cs`:

```csharp
private static readonly byte[][] s_signaturesOfInterceptsLocationAttribute =
    { s_signature_HasThis_Void_String_Int32_Int32, s_signature_HasThis_Void_Int32_String };

internal static readonly AttributeDescription InterceptsLocationAttribute =
    new AttributeDescription("System.Runtime.CompilerServices", "InterceptsLocationAttribute", s_signaturesOfInterceptsLocationAttribute);
```

Two constructors are recognised, by namespace + type name + constructor signature, not by identity
in any particular assembly:

1. `InterceptsLocationAttribute(string filePath, int line, int character)` — **deprecated**;
   using it produces **CS9270** (`WRN_InterceptsLocationAttributeUnsupportedSignature`, put into the
   .NET 9 warning wave by PR #76642): "'InterceptsLocationAttribute(string, int, int)' is not
   supported. Move to 'InterceptableLocation'-based generation of these attributes instead.
   (https://github.com/dotnet/roslyn/issues/72133)". It still functions.
2. `InterceptsLocationAttribute(int version, string data)` — the supported form.

The attribute type is **not in the BCL** (no
`src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/InterceptsLocationAttribute.cs`
in `dotnet/runtime` `main`). Generators must declare it themselves. Roslyn's own test sources declare
it as follows (moved into `TestSources.InterceptsLocationAttribute` by PR #82172 on 2026-02-03):

```csharp
namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class InterceptsLocationAttribute : Attribute
{
    public InterceptsLocationAttribute(string filePath, int line, int character) { }
    public InterceptsLocationAttribute(int version, string data) { }
}
```

The feature document recommends a `file class` declaration so that multiple generators do not
collide: "A generator which needs to declare this attribute should use a file-local declaration to
ensure it doesn't conflict with other generators that need to do the same thing." It also states
"File-local declarations of this type (`file class InterceptsLocationAttribute`) are valid and usages
are recognized by the compiler when they are within the same file and compilation."

### 2.2 The encoding (writer side)

`InterceptableLocation1.Data`:

```csharp
public override int Version => 1;
public override string Data
{
    get
    {
        if (_lazyData is null)
            _lazyData = makeData();

        return _lazyData;

        string makeData()
        {
            var builder = PooledBlobBuilder.GetInstance();
            builder.WriteBytes(_checksum, start: 0, 16);
            builder.WriteInt32(_position);

            var displayFileName = Path.GetFileName(_path);
            builder.WriteUTF8(displayFileName);

            var bytes = builder.ToArray();
            builder.Free();
            return Convert.ToBase64String(bytes);
        }
    }
}
```

Layout, base64-encoded (`Convert.ToBase64String`, standard alphabet, `=` padding):

| Offset | Size | Content |
|---|---|---|
| 0 | 16 | xxHash128 content checksum of the **whole file** containing the intercepted call |
| 16 | 4 | `int32`, **little-endian**, `SyntaxNode.Position` of the simple-name syntax |
| 20 | rest | UTF-8 bytes of `Path.GetFileName(path)` — the display file name, used only for diagnostics |

Minimum decoded length is 20 bytes. The display name may be empty (a tree with an empty `FilePath`
yields an empty tail).

`BlobBuilder.WriteInt32` writes little-endian regardless of host endianness; the decoder uses
`BinaryPrimitives.ReadInt32LittleEndian` explicitly.

The feature document states the same three items:

> The "version 1" data encoding is a base64-encoded string consisting of the following data:
> - 16 byte xxHash128 content checksum of the file containing the intercepted call.
> - int32 in little-endian format for the position (i.e. `SyntaxNode.Position`) of the call in syntax.
> - utf-8 string data containing a display file name, used for error reporting.

### 2.3 The decoding (compiler side)

`InterceptableLocation1.Decode(string? data)` returns
`(ReadOnlyMemory<byte> checksum, int position, string displayFileName)?`, or `null` on:
`data is null`; `Convert.FromBase64String` throwing `FormatException`;
`bytes.Length < 20`; the UTF-8 decode of the tail throwing `ArgumentException`
(the encoding is `new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)`).

The in-source comment restates the format:

```csharp
// format:
// - 16 bytes of target file content hash (xxHash128)
// - int32 position (little endian)
// - utf-8 display filename
const int hashIndex = 0;
const int hashSize = 16;
const int positionIndex = hashIndex + hashSize;
const int positionSize = sizeof(int);
const int displayNameIndex = positionIndex + positionSize;
const int minLength = displayNameIndex;
```

There is a second, independent decoder in the IDE/workspaces layer:
`src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Utilities/InterceptsLocationUtilities.cs`,
which defines

```csharp
internal record struct InterceptsLocationData(ImmutableArray<byte> ContentHash, int Position)
```

and

```csharp
public static bool TryGetInterceptsLocationData(AttributeData attribute, out InterceptsLocationData result)
{
    if (attribute is
        {
            AttributeClass.Name: "InterceptsLocationAttribute",
            ConstructorArguments: [{ Value: int version }, { Value: string attributeData }]
        })
    {
        return TryGetInterceptsLocationData(version, attributeData, out result);
    }

    result = default;
    return false;
}

public static bool TryGetInterceptsLocationData(int version, string attributeData, out InterceptsLocationData result)
{
    if (version == 1)
        return TryGetInterceptsLocationDataVersion1(attributeData, out result);

    // Add more supported versions here in the future if the compiler adds any.

    result = default;
    return false;
}
```

That comment is direct evidence that version 2 does not exist and is anticipated. Note that the
workspaces decoder matches the attribute by **simple name only** (`AttributeClass.Name`), unlike the
compiler which matches namespace + name + constructor signature.

### 2.4 The content hash: `SourceText.GetContentHash()`

`src/Compilers/Core/Portable/Text/SourceText.cs`:

```csharp
private const int CharBufferSize = 32 * 1024;
private static readonly ObjectPool<XxHash128> s_contentHashPool = new ObjectPool<XxHash128>(() => new XxHash128(), trackLeaks: false);

public ImmutableArray<byte> GetContentHash()   // lazily cached in _lazyContentHash
```

The algorithm streams the text in 32K-char chunks, reinterprets each chunk as bytes, and (on
big-endian hosts only) byte-swaps each `char` so the result is platform-independent. So the hash is
over the UTF-16 code units of the text, little-endian — **not** over the file's on-disk bytes, and
**not** affected by the file's encoding, BOM or `SourceHashAlgorithm`.

The XML documentation on `GetContentHash` carries a warning that is directly relevant to any tool
that persists an `InterceptsLocation` string:

> "This hash is safe to use across platforms and across processes, as long as the same version of
> Roslyn is used in all those locations. As such, it is safe to use as a fast proxy for comparing
> text instances in different memory spaces. **Different versions of Roslyn may produce different
> content hashes.**"

`InterceptableLocation1` itself repeats the caveat in a code comment:

> "Note: the goal of implementing equality here is so that incremental state tables etc. can detect
> and use it. This encoding which uses the checksum of the referenced file may not be stable across
> incremental runs in practice, but it seems correct in principle to implement equality here anyway."

`InterceptableLocation1.GetHashCode()` uses only the first 4 bytes of the checksum and the position;
`Equals` compares checksum, path, position, line and character.

### 2.5 How the compiler validates a supplied location

`SourceMethodSymbolWithAttributes.DecodeInterceptsLocationChecksumBased`
(`src/Compilers/CSharp/Portable/Symbols/Source/SourceMethodSymbolWithAttributes.cs`, ~line 1024).
Order of checks, exactly as implemented:

1. `if (version != 1)` → **CS9232** `ERR_InterceptsLocationUnsupportedVersion`, return.
2. `InterceptableLocation1.Decode(data)` fails → **CS9231** `ERR_InterceptsLocationDataInvalidFormat`, return.
3. Namespace opt-in check against `((CSharpParseOptions)attributeNameSyntax.SyntaxTree.Options).InterceptorsNamespaces`.
   On failure: **CS9206** `ERR_InterceptorGlobalNamespace` if the interceptor is in the global
   namespace, otherwise **CS9137** `ERR_InterceptorsFeatureNotEnabled` with a suggested property
   string built as
   `$"<InterceptorsNamespaces>$(InterceptorsNamespaces);{string.Join(".", namespaceNames)}</InterceptorsNamespaces>"`.
4. `ReportBadInterceptsLocation` → **CS9138** `ERR_InterceptorContainingTypeCannotBeGeneric`,
   **CS9146** `ERR_InterceptorMethodMustBeOrdinary`, **CS9161** `ERR_InterceptorCannotUseUnmanagedCallersOnly`.
5. `var matchingTrees = DeclaringCompilation.GetSyntaxTreesByContentHash(hash);`
   * `Count > 1` → **CS9233** `ERR_InterceptsLocationDuplicateFile`:
     "Cannot intercept a call in file '{0}' because it is duplicated elsewhere in the compilation."
   * `Count == 0` → **CS9234** `ERR_InterceptsLocationFileNotFound`:
     "Cannot intercept a call in file '{0}' because a matching file was not found in the compilation."
     **This is the diagnostic reported when the checksum does not match, that is, when the source has
     been rewritten.** `{0}` is the display file name carried in the blob, not a path.
6. `position < 0 || position > root.EndPosition` → **CS9235** `ERR_InterceptsLocationDataInvalidPosition`.
7. `root.FindToken(position)` must sit under a `SimpleNameSyntax` that is the name of an
   `InvocationExpressionSyntax`, a `MemberAccessExpressionSyntax` or a `MemberBindingExpressionSyntax`
   under an invocation. Otherwise **CS9151** `ERR_InterceptorNameNotInvoked` or **CS9141**
   `ERR_InterceptorPositionBadToken`.
8. `position != referencedToken.Position` → **CS9235** again (the position must be the exact start
   of the token, not merely inside it).
9. Success: `DeclaringCompilation.AddInterception(matchingTree.GetText().GetContentHash(), position, attributeLocation, this)`.

The exact matching switch in step 7:

```csharp
var referencedToken = root.FindToken(position);
switch (referencedToken)
{
    case { Parent: SimpleNameSyntax { Parent: MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax } memberAccess } rhs } when memberAccess.Name == rhs:
    case { Parent: SimpleNameSyntax { Parent: MemberBindingExpressionSyntax { Parent: InvocationExpressionSyntax } memberBinding } rhs1 } when memberBinding.Name == rhs1:
    case { Parent: SimpleNameSyntax { Parent: InvocationExpressionSyntax invocation } simpleName } when invocation.Expression == simpleName:
        // happy case
        break;
    ...
}
```

**File matching is by content hash only.** The `_path` / display name plays no role in resolution.
`CSharpCompilation.GetSyntaxTreesByContentHash`:

```csharp
internal OneOrMany<SyntaxTree> GetSyntaxTreesByContentHash(ReadOnlyMemory<byte> contentHash)
{
    Debug.Assert(contentHash.Length == InterceptableLocation1.ContentHashLength);
    ...
    foreach (var tree in SyntaxTrees)
    {
        var text = tree.GetText();
        var hash = text.GetContentHash().AsMemory();
        builder[hash] = builder.TryGetValue(hash, out var existing) ? existing.Add(tree) : OneOrMany.Create(tree);
    }
}
```

Two consequences that matter to a source-rewriting tool:

* Two syntax trees with **identical text** in one compilation make every interceptor targeting
  either of them fail with CS9233, no matter what their paths are. This replaced the old path-keyed
  behaviour; see closed issue <https://github.com/dotnet/roslyn/issues/76341>,
  "Interceptors should not internally identify calls using paths", fixed by PR #76344.
* The lookup enumerates **all** `SyntaxTrees` of the compilation being compiled, which for a
  generator-driven build is the final compilation including generated trees. So the *validation*
  can resolve a call in a generated file, even though `GetInterceptableLocation` during a standard
  generator phase only ever sees the pre-generator compilation.

Registration and lookup key (`CSharpCompilation.cs`, ~2555 and ~2582):

```csharp
internal void AddInterception(ImmutableArray<byte> contentHash, int position, Location attributeLocation, MethodSymbol interceptor)
// backing store:
// ConcurrentDictionary<(ImmutableArray<byte> ContentHash, int Position), OneOrMany<(Location AttributeLocation, MethodSymbol Interceptor)>>
// with comparer InterceptorKeyComparer.Instance

internal (Location AttributeLocation, MethodSymbol Interceptor)? TryGetInterceptor(SimpleNameSyntax? node)
{
    ...
    ((SourceModuleSymbol)SourceModule).DiscoverInterceptorsIfNeeded();
    ...
    var key = (node.SyntaxTree.GetText().GetContentHash(), node.Position);
    if (_interceptions.TryGetValue(key, out var interceptionsAtAGivenLocation) && interceptionsAtAGivenLocation is [var oneInterception])
        return oneInterception;
    ...
}
```

Note the `is [var oneInterception]` pattern: when more than one interceptor is registered at the
same location, `TryGetInterceptor` returns `null` and the duplicate is reported as **CS9153**
`ERR_DuplicateInterceptor` during lowering.

### 2.6 Complete diagnostic table

Enum member names and numeric values from `src/Compilers/CSharp/Portable/Errors/ErrorCode.cs`;
messages from `src/Compilers/CSharp/Portable/CSharpResources.resx`.

| ID | `ErrorCode` | Message |
|---|---|---|
| CS9137 | `ERR_InterceptorsFeatureNotEnabled` | The 'interceptors' feature is not enabled in this namespace. Add '{0}' to your project. |
| CS9138 | `ERR_InterceptorContainingTypeCannotBeGeneric` | Method '{0}' cannot be used as an interceptor because its containing type has type parameters. |
| CS9139 | `ERR_InterceptorPathNotInCompilation` | Cannot intercept: compilation does not contain a file with path '{0}'. |
| CS9140 | `ERR_InterceptorPathNotInCompilationWithCandidate` | Cannot intercept: compilation does not contain a file with path '{0}'. Did you mean to use path '{1}'? |
| CS9141 | `ERR_InterceptorPositionBadToken` | The provided line and character number does not refer to an interceptable method name, but rather to token '{0}'. |
| CS9142 | `ERR_InterceptorLineOutOfRange` | The given file has '{0}' lines, which is fewer than the provided line number '{1}'. |
| CS9143 | `ERR_InterceptorCharacterOutOfRange` | The given line is '{0}' characters long, which is fewer than the provided character number '{1}'. |
| CS9144 | `ERR_InterceptorSignatureMismatch` | Cannot intercept method '{0}' with interceptor '{1}' because the signatures do not match. |
| (CS9145) | commented out: `ERR_InterceptorPathNotInCompilationWithUnmappedCandidate` | retired; still listed on the stale Learn page |
| CS9146 | `ERR_InterceptorMethodMustBeOrdinary` | An interceptor method must be an ordinary member method. |
| CS9147 | `ERR_InterceptorMustReferToStartOfTokenPosition` | The provided line and character number does not refer to the start of token '{0}'. Did you mean to use line '{1}' and character '{2}'? |
| CS9148 | `ERR_InterceptorMustHaveMatchingThisParameter` | Interceptor must have a 'this' parameter matching parameter '{0}' on '{1}'. |
| CS9149 | `ERR_InterceptorMustNotHaveThisParameter` | Interceptor must not have a 'this' parameter because '{0}' does not have a 'this' parameter. |
| CS9150 | `ERR_InterceptorFilePathCannotBeNull` | Interceptor cannot have a 'null' file path. |
| CS9151 | `ERR_InterceptorNameNotInvoked` | Possible method name '{0}' cannot be intercepted because it is not being invoked. |
| CS9152 | `ERR_InterceptorNonUniquePath` | Cannot intercept a call in file with path '{0}' because multiple files in the compilation have this path. |
| CS9153 | `ERR_DuplicateInterceptor` | The indicated call is intercepted multiple times. |
| CS9154 | `WRN_InterceptorSignatureMismatch` | Intercepting a call to '{0}' with interceptor '{1}', but the signatures do not match. |
| CS9155 | `ERR_InterceptorNotAccessible` | Cannot intercept call with '{0}' because it is not accessible within '{1}'. |
| CS9156 | `ERR_InterceptorScopedMismatch` | Cannot intercept call to '{0}' with '{1}' because of a difference in 'scoped' modifiers or '[UnscopedRef]' attributes. |
| CS9157 | `ERR_InterceptorLineCharacterMustBePositive` | Line and character numbers provided to InterceptsLocationAttribute must be positive. |
| CS9158 | `WRN_NullabilityMismatchInReturnTypeOnInterceptor` | Nullability of reference types in return type doesn't match interceptable method '{0}'. |
| CS9159 | `WRN_NullabilityMismatchInParameterTypeOnInterceptor` | Nullability of reference types in type of parameter '{0}' doesn't match interceptable method '{1}'. |
| CS9160 | `ERR_InterceptorCannotInterceptNameof` | A nameof operator cannot be intercepted. |
| CS9161 | `ERR_InterceptorCannotUseUnmanagedCallersOnly` | An interceptor cannot be marked with 'UnmanagedCallersOnlyAttribute'. |
| CS9177 | `ERR_InterceptorArityNotCompatible` | Method '{0}' must be non-generic or have arity {1} to match '{2}'. |
| CS9178 | `ERR_InterceptorCannotBeGeneric` | Method '{0}' must be non-generic to match '{1}'. |
| CS9206 | `ERR_InterceptorGlobalNamespace` | An interceptor cannot be declared in the global namespace. |
| CS9207 | `ERR_InterceptableMethodMustBeOrdinary` | Cannot intercept '{0}' because it is not an invocation of an ordinary member method. |
| **CS9231** | `ERR_InterceptsLocationDataInvalidFormat` | The data argument to InterceptsLocationAttribute is not in the correct format. |
| **CS9232** | `ERR_InterceptsLocationUnsupportedVersion` | Version '{0}' of the interceptors format is not supported. **The latest supported version is '1'.** |
| **CS9233** | `ERR_InterceptsLocationDuplicateFile` | Cannot intercept a call in file '{0}' because it is duplicated elsewhere in the compilation. |
| **CS9234** | `ERR_InterceptsLocationFileNotFound` | Cannot intercept a call in file '{0}' because a matching file was not found in the compilation. |
| **CS9235** | `ERR_InterceptsLocationDataInvalidPosition` | The data argument to InterceptsLocationAttribute refers to an invalid position in file '{0}'. |
| CS9270 | `WRN_InterceptsLocationAttributeUnsupportedSignature` | 'InterceptsLocationAttribute(string, int, int)' is not supported. Move to 'InterceptableLocation'-based generation of these attributes instead. (https://github.com/dotnet/roslyn/issues/72133) |

CS9139 / CS9140 / CS9142 / CS9143 / CS9147 / CS9150 / CS9152 / CS9157 belong to the deprecated
`(string, int, int)` path only (`DecodeInterceptsLocationAttributeExperimentalCompat`).

The public reference page is
<https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/source-generator-errors>
(source: `docs/csharp/language-reference/compiler-messages/source-generator-errors.md` in
`dotnet/docs`). **It is stale**: it still advertises `<Features>InterceptorsPreview</Features>` as
the opt-in, still calls interceptors experimental, and still lists retired CS9145. It is not a
reliable source for the current opt-in mechanism.

### 2.7 Is there a version 2 in `main`? No.

Three independent confirmations:

1. `if (version != 1) { diagnostics.Add(ErrorCode.ERR_InterceptsLocationUnsupportedVersion, attributeLocation, version); return; }`
2. CS9232's resource string hardcodes "The latest supported version is '1'."
3. `InterceptsLocationUtilities.TryGetInterceptsLocationData` carries the placeholder comment
   "Add more supported versions here in the future if the compiler adds any."

The feature document reserves the right: "a version number. The compiler may introduce new
encodings for the location in the future, with corresponding new version numbers."

---

## 3. Which call sites are interceptable — did the set change?

### 3.1 Restriction text from `docs/features/interceptors.md`

Under **Detailed design → InterceptsLocationAttribute**:

> "Any 'ordinary method' (i.e. with `MethodKind.Ordinary`) can have its calls intercepted."
>
> "In addition to 'ordinary' forms `M()` and `receiver.M()`, a call within a conditional access,
> e.g. of the form `receiver?.M()` can be intercepted. A call whose receiver is a pointer member
> access, e.g. of the form `ptr->M()`, can also be intercepted."

Under **Non-invocation method usages** — this is the restriction list the assignment asks for,
quoted in full, it is two sentences:

> "Conversion to delegate type, address-of, etc. usages of methods cannot be intercepted."
>
> "Interception can only occur for calls to ordinary member methods--not constructors, delegates,
> properties, local functions, operators, etc. Support for more member kinds may be added in the
> future."

Under **Position**:

> "The location of the call is the location of the simple name syntax which denotes the
> interceptable method. For example, in `app.MapGet(...)`, the name syntax for `MapGet` would be
> considered the location of the call. For a static method call like `System.Console.WriteLine(...)`,
> the name syntax for `WriteLine` is the location of the call. If we allow intercepting calls to
> property accessors in the future (e.g `obj.Property`), we would also be able to use the name
> syntax in this way."

Under **Arity**:

> "Interceptors cannot be declared in generic types at any level of nesting."

### 3.2 What the implementation actually accepts today

**Not interceptable** (CS9207 `ERR_InterceptableMethodMustBeOrdinary`), confirmed by baselines in
`src/Compilers/CSharp/Test/Semantic/Semantics/InterceptorsTests.cs`:

* delegate invocation — `Diagnostic(ErrorCode.ERR_InterceptableMethodMustBeOrdinary, "InterceptsLocation").WithArguments("action")` (~line 1454)
* delegate-typed field invocation — `.WithArguments("a")` (~line 2202)
* local function invocation — `.WithArguments("local")` (~line 2205)
* function pointer invocation — `.WithArguments("fnptr")` (~line 2207)
* property invocation — `.WithArguments("P")` (~line 7633)

**Not reachable at all**, because `GetInterceptableNameSyntax` requires an
`InvocationExpressionSyntax`: property access, indexer access (`ElementAccessExpressionSyntax`),
object creation, operators (including C# 15 extension operators and C# 14 user-defined compound
assignment operators), and `nameof` (CS9160 guards it explicitly).

**Newly interceptable since the doc was written: C# 14 `extension` block methods.**
PR `Extensions: interceptors (#79010)`, merged **2025-06-26** — Roslyn 5.0 / .NET 10 wave, that is,
*before* the .NET 11 wave. `InterceptorsTests.cs` contains 28 tests `Extensions_01` … `Extensions_29`
(`08` is missing), all tagged `[Fact, CompilerTrait(CompilerFeature.Extensions)]`:

| Test | Scenario, from its leading comment |
|---|---|
| `Extensions_01` | original calls use extensions, interceptors are classic |
| `Extensions_02`, `_03` | extension receiver shapes, including `extension(C<object> o)` |
| `Extensions_04`–`_06` | `this`-parameter mismatch → CS9148 `Interceptor must have a 'this' parameter matching parameter 'int i' on 'E.extension(int).M()'.` |
| `Extensions_07` | original calls use static extension, interceptors with and without `this` |
| `Extensions_09` | original calls use non-extension invocations, interceptor is an extension method |
| `Extensions_10` | original call uses non-extension instance invocations, interceptor is a static extension method |
| `Extensions_11` | original call uses classic extension invocations, interceptor is a new extension method |
| `Extensions_12` | original call uses new extension invocations, interceptors are new extension methods |
| `Extensions_13` | interception within extension body |
| `Extensions_17` | `extension(System.ValueType v)` |
| `Extensions_18` | `extension(ref S s)` |
| `Extensions_25` | `scoped` difference in receiver: `extension(ref RS s)` vs `extension(scoped ref RS s)`, both succeed |
| `Extensions_26`, `_27` | classic-extension call intercepted by new extension method, with `ref` arguments and implicit `ref` receiver |
| `Extensions_28` | generic new extension method interceptor: `extension<T>(C<T> t) { public void Method<U>(U u) }` intercepting `new C<object>().M(42)`, output `(System.Object, System.Int32)` |
| `Extensions_29` | `extension(__arglist)` → CS1669 |

Representative source (`Extensions_26`, complete):

```csharp
// original
int i = 0;
new object().M(ref i);

public static class Extensions
{
    public static void M(this object o, ref int i) => throw null;
}

// interceptor
static class Interceptors
{
    extension(object o)
    {
        [System.Runtime.CompilerServices.InterceptsLocation(1, "…")]
        public void Method(ref int i) { System.Console.Write("ran"); }
    }
}
```

Diagnostics now render extension members in the new display form, for example
`E.extension(int).M()` and `E.extension(object).M(object)` in CS9144 and CS9148 arguments.

Extension scenarios are exercised with
`RegularPreviewWithInterceptors = TestOptions.RegularPreview.WithFeature(CodeAnalysis.Feature.InterceptorsNamespaces, "global")`,
that is, `LanguageVersion.Preview` plus the interceptors namespace flag.

**Conclusion for item (3) of the assignment**: the interceptable set has **not** widened in the
.NET 11 wave. It widened once, in the .NET 10 wave, to include methods declared in `extension`
blocks (on both sides: as the intercepted target and as the interceptor). Properties, indexers,
constructors, operators, local functions, delegates and function pointers remain not interceptable;
extension properties, extension indexers and extension operators are not interceptable either, since
none of them is an `InvocationExpressionSyntax` bound to a `MethodKind.Ordinary` method.
`docs/features/interceptors.md` has not been updated to mention extension blocks; it is stale on
this point, and it also still contains the pre-.NET-8 sentence "The implementation currently
requires the interceptor to be an extension method for this comparison to work. We plan on
addressing this before releasing .NET 8."

### 3.3 Signature matching rules (unchanged, from the doc)

* Instance-versus-static comparison uses the static method "as if it is an extension in reduced form".
* "The returns and parameters, including the `this` parameter, must have the same ref kinds and types."
* A **warning** rather than an error when types differ but are not distinct to the runtime
  (`object` versus `dynamic`) — CS9154.
* Safe nullability differences are allowed silently; unsafe ones give CS9158 / CS9159.
* "Method names and parameter names are not required to match."
* "Parameter default values are not required to match. When intercepting, default values on the
  interceptor method are ignored."
* "`params` modifiers are not required to match."
* "`scoped` modifiers and `[UnscopedRef]` must be equivalent." (CS9156)
* "In general, attributes which normally affect the behavior of the call site, such as
  `[CallerLineNumber]` are ignored on the interceptor of an intercepted call." The exception is
  attributes that affect safety capabilities, such as `[UnscopedRef]`, which must match.
* "Arity does not need to match between intercepted and interceptor methods."
* A generic interceptor must have arity equal to the *sum* of the original method's arity and its
  containing types' arities, and receives all those type arguments outermost-to-innermost.

Accessibility: "An interceptor must be accessible at the location where interception is occurring",
but "An interceptor contained in a file-local type is permitted to intercept a call in another file,
even though the interceptor is not normally *visible* at the call site."

Struct receiver capture: an interceptor whose `this` parameter takes a struct by reference can
intercept a struct instance method call "even if such capture is not permitted when the interceptor
is called directly", so `new S().Original()` can be intercepted by `static void Interceptor(this ref S s)`
although `new S().Interceptor()` would be CS1510.

Implementation strategy, relevant to any tool that also rewrites: decoding happens during the
**binding** phase and is collected in a `ConcurrentSet` on the compilation; substitution happens
during **lowering**, in `LocalRewriter_Call.cs`, when a `BoundCall` is lowered, after the receiver
and arguments have been lowered. Errors about signature incompatibility, duplicate interception and
accessibility are reported at that later point, which is why the document says some interceptor
diagnostics appear only in a command-line build.

---

## 4. `InterceptorsNamespaces` / `InterceptorsPreviewNamespaces` in the .NET 11 SDK

### 4.1 Compiler feature flag

`src/Compilers/Core/Portable/CommandLine/Feature.cs`. This file is **new in the .NET 11 wave**
(`Centralize recognized feature flags (#81591)`, 2025-12-10) and centralises every `/features:` name:

```csharp
internal static class Feature
{
    internal const string Strict = "strict";
    internal const string UseLegacyStrongNameProvider = "UseLegacyStrongNameProvider";
    internal const string UpdatedMemorySafetyRules = "updated-memory-safety-rules";
    internal const string EnableGeneratorCache = "enable-generator-cache";
    internal const string PdbPathDeterminism = "pdb-path-determinism";
    internal const string DebugDeterminism = "debug-determinism";
    internal const string DebugAnalyzers = "debug-analyzers";
    internal const string RuntimeAsync = "runtime-async";
    internal const string PEVerifyCompat = "peverify-compat";
    internal const string FileBasedProgram = "FileBasedProgram";
    internal const string NullablePublicOnly = "nullablePublicOnly";
    internal const string RunNullableAnalysis = "run-nullable-analysis";
    internal const string InterceptorsNamespaces = "InterceptorsNamespaces";
    internal const string NoRefSafetyRulesAttribute = "noRefSafetyRulesAttribute";
    internal const string DisableLengthBasedSwitch = "disable-length-based-switch";
    internal const string ExperimentalDataSectionStringLiterals = "experimental-data-section-string-literals";

    // For testing
    internal const string Experiment = "Experiment";
    internal const string Test = "Test";

    [Conditional("DEBUG")]
    internal static void AssertValidFeature(string s) { /* Debug.Assert on unknown flags */ }
}
```

There is exactly **one** compiler feature name: `InterceptorsNamespaces`. The command line is
`/features:InterceptorsNamespaces=<semicolon-separated namespace list>`.
`InterceptorsPreviewNamespaces` is **not** a compiler feature name, and neither is
`InterceptorsPreview`.

Parsing, `CSharpParseOptions.InterceptorsNamespaces` (internal,
`ImmutableArray<ImmutableArray<string>>`, memoised via `ImmutableInterlocked.InterlockedInitialize`
into `_interceptorsNamespaces`): the flag value is split on `;` into namespaces, and each namespace
on `.` into segments. Example given in the source: `[["System", "Threading"], ["System", "Collections"]]`.

Matching, `SourceMethodSymbolWithAttributes.DecodeInterceptsLocationChecksumBased.isDeclaredInNamespace`:

```csharp
static bool isDeclaredInNamespace(ArrayBuilder<string> thisNamespaceNames, ImmutableArray<string> namespaceSegments)
{
    Debug.Assert(namespaceSegments.Length > 0);
    if (namespaceSegments is ["global"])
    {
        return true;
    }

    if (namespaceSegments.Length > thisNamespaceNames.Count)
    {
        // the enabled NS has more components than interceptor's NS, so it will never match.
        return false;
    }

    for (var i = 0; i < namespaceSegments.Length; i++)
    {
        if (namespaceSegments[i] != thisNamespaceNames[i])
        {
            return false;
        }
    }
    return true;
}
```

* Matching is a **namespace prefix** match, outermost-to-innermost. Enabling `MyApp.Generated` also
  enables `MyApp.Generated.Interceptors`.
* The literal single segment **`global`** is a wildcard that enables every namespace. This is what
  Roslyn's own test suite uses:
  `TestOptions.Regular.WithFeature(CodeAnalysis.Feature.InterceptorsNamespaces, "global")`.
  A project can therefore write `<InterceptorsNamespaces>global</InterceptorsNamespaces>` to disable
  the gate entirely.
* An interceptor declared in the **global namespace** is always an error (CS9206), regardless of
  opt-in, because `getNamespaceNames` returns an empty builder in that case.

### 4.2 MSBuild plumbing

`src/Compilers/Core/MSBuildTask/Microsoft.CSharp.Core.targets`, on the `<Csc>` task invocation
(lines 123-125):

```xml
Features="$(Features)"
InterceptorsNamespaces="$(InterceptorsNamespaces)"
InterceptorsPreviewNamespaces="$(InterceptorsPreviewNamespaces)"
```

`src/Compilers/Core/MSBuildTask/Csc.cs`:

```csharp
internal static void AddInterceptorsNamespaces(CommandLineBuilderExtension commandLine, string? interceptorsNamespaces, string? interceptorsPreviewNamespaces)
{
    var interceptorsNamespacesIsNullOrEmpty = string.IsNullOrEmpty(interceptorsNamespaces);
    var interceptorsPreviewNamespacesIsNullOrEmpty = string.IsNullOrEmpty(interceptorsPreviewNamespaces);
    if (interceptorsNamespacesIsNullOrEmpty && interceptorsPreviewNamespacesIsNullOrEmpty)
    {
        return;
    }

    var featureValue = interceptorsNamespacesIsNullOrEmpty ? interceptorsPreviewNamespaces
        : interceptorsPreviewNamespacesIsNullOrEmpty ? interceptorsNamespaces
        : $"{interceptorsNamespaces};{interceptorsPreviewNamespaces}";
    commandLine.AppendSwitchIfNotNull("/features:", $"InterceptorsNamespaces={featureValue}");
}

public string? InterceptorsNamespaces
{
    set { _store[nameof(InterceptorsNamespaces)] = value; }
    get { return (string?)_store[nameof(InterceptorsNamespaces)]; }
}

/// <remarks>Alias for <see cref="InterceptorsNamespaces"/>.</remarks>
public string? InterceptorsPreviewNamespaces
{
    set { _store[nameof(InterceptorsPreviewNamespaces)] = value; }
    get { return (string?)_store[nameof(InterceptorsPreviewNamespaces)]; }
}
```

This matches the feature document exactly:

> "For compatibility, the property `<InterceptorsPreviewNamespaces>` can be used as an alias for
> `<InterceptorsNamespaces>`. If both properties have non-empty values, they will be concatenated
> together in the order `$(InterceptorsNamespaces);$(InterceptorsPreviewNamespaces)` when passed to
> the compiler."

The rename happened in `Use InterceptorsNamespaces feature name instead of InterceptorsPreviewNamespaces (#74865)`,
2024-08-27. `<Features>InterceptorsPreview</Features>`, still documented on Learn, is obsolete and
has no effect.

### 4.3 Is the opt-in still required? Yes.

Nothing in Roslyn `main` removes it, and the gate is checked in
`DecodeInterceptsLocationChecksumBased` before file resolution. There is no property, language
version, or SDK switch that disables the check.

### 4.4 What the .NET 11 SDK enables by default

`dotnet/sdk` `src/Cli/dotnet/Commands/Run/CSharpCompilerCommand.Generated.cs`, which encodes the
expected compiler command line for a file-based program, contains:

```
"/features:InterceptorsNamespaces=;Microsoft.AspNetCore.Http.Generated;Microsoft.Extensions.Configuration.Binder.SourceGeneration;Microsoft.Extensions.Validation.Generated",
```

So three namespaces are pre-enabled by the web / extensions targets on .NET 11:

* `Microsoft.AspNetCore.Http.Generated` (Request Delegate Generator, minimal APIs)
* `Microsoft.Extensions.Configuration.Binder.SourceGeneration` (configuration binder generator)
* `Microsoft.Extensions.Validation.Generated` (validation generator)

The leading `;` is the empty `$(InterceptorsNamespaces)` being prepended. A user project that adds
its own generator must still append to the property, as the feature document shows:

```xml
<InterceptorsNamespaces>$(InterceptorsNamespaces);Microsoft.AspNetCore.Http.Generated;MyLibrary.Generated</InterceptorsNamespaces>
```

EF Core documents the same pattern for `Microsoft.EntityFrameworkCore.GeneratedInterceptors`
(precompiled queries, NativeAOT).

---

## 5. Interceptors and the new `RegisterPreCompilationSourceOutput` stage

Design document: `docs/features/pre-compilation-source-outputs.md`, new in the .NET 11 wave.
Implementation PR: <https://github.com/dotnet/roslyn/pull/83088>.
API review: <https://github.com/dotnet/roslyn/issues/83089>.

### 5.1 The new API

All registration members are `[Experimental(RoslynExperiments.PreCompilationSourceOutput)]`, that is
`RSEXPERIMENTAL007`. From `src/Compilers/Core/Portable/PublicAPI.Unshipped.txt`:

```
[RSEXPERIMENTAL007]Microsoft.CodeAnalysis.IncrementalGeneratorInitializationContext.RegisterPreCompilationSourceOutput<TSource>(Microsoft.CodeAnalysis.IncrementalValueProvider<TSource> source, System.Action<Microsoft.CodeAnalysis.PreCompilationSourceProductionContext, TSource>! action) -> void
[RSEXPERIMENTAL007]Microsoft.CodeAnalysis.IncrementalGeneratorInitializationContext.RegisterPreCompilationSourceOutput<TSource>(Microsoft.CodeAnalysis.IncrementalValuesProvider<TSource> source, System.Action<Microsoft.CodeAnalysis.PreCompilationSourceProductionContext, TSource>! action) -> void
[RSEXPERIMENTAL007]Microsoft.CodeAnalysis.PreCompilationSourceProductionContext
[RSEXPERIMENTAL007]Microsoft.CodeAnalysis.PreCompilationSourceProductionContext.AddSource(string! hintName, Microsoft.CodeAnalysis.Text.SourceText! sourceText) -> void
[RSEXPERIMENTAL007]Microsoft.CodeAnalysis.PreCompilationSourceProductionContext.AddSource(string! hintName, string! source) -> void
[RSEXPERIMENTAL007]Microsoft.CodeAnalysis.PreCompilationSourceProductionContext.CancellationToken.get -> System.Threading.CancellationToken
[RSEXPERIMENTAL007]Microsoft.CodeAnalysis.PreCompilationSourceProductionContext.PreCompilationSourceProductionContext() -> void
Microsoft.CodeAnalysis.IncrementalGeneratorOutputKind.PreCompilation = 16 -> Microsoft.CodeAnalysis.IncrementalGeneratorOutputKind
const Microsoft.CodeAnalysis.WellKnownGeneratorOutputs.PreCompilationSourceOutput = "PreCompilationSourceOutput" -> string!
```

`IncrementalGeneratorOutputKind.PreCompilation = 16` (`0b10000`) and
`WellKnownGeneratorOutputs.PreCompilationSourceOutput` are **not** marked experimental; only the
registration methods and the context type are.

Node implementation:
`src/Compilers/Core/Portable/SourceGeneration/Nodes/PreCompilationSourceOutputNode.cs`:

```csharp
internal sealed class PreCompilationSourceOutputNode<TInput> : AbstractSourceOutputNode<TInput>
{
    public override IncrementalGeneratorOutputKind Kind => IncrementalGeneratorOutputKind.PreCompilation;
    protected override string StepName => WellKnownGeneratorOutputs.PreCompilationSourceOutput;

    protected override void InvokeUserAction(AdditionalSourcesCollection sources, DiagnosticBag diagnostics, DriverStateTable.Builder graphState, TInput item, CancellationToken cancellationToken)
    {
        var context = new PreCompilationSourceProductionContext(sources, graphState.DriverState.ChecksumAlgorithm, cancellationToken);
        _action(context, item, cancellationToken);
    }
}
```

`PreCompilationSourceProductionContext` deliberately has **no `ReportDiagnostic`**:

> "Pre-compilation is an early phase focused purely on producing source; diagnostic reporting should
> be done in a separate analyzer."

### 5.2 Execution order, from the design document

```
1. RegisterPostInitializationOutput
   +-- Source added to initial compilation (takes no inputs)

2. RegisterPreCompilationSourceOutput          <- NEW
   +-- Reads non-compilation inputs (additional files, parse options, etc.)
   +-- Source added to initial compilation
   +-- Compilation is rebuilt with new sources

3. RegisterSourceOutput / RegisterImplementationSourceOutput
   +-- Reads full compilation (now includes post-init AND pre-compilation sources)
   +-- Source is part of final output but not fed back into compilation
```

Driver steps, quoted:

> 1. Post-initialization sources are collected as today.
> 2. A `DriverStateTable.Builder` is created **without** the compilation or syntax store - these are not yet available.
> 3. Pre-compilation output nodes are evaluated for all generators. Their sources are parsed into syntax trees.
> 4. The initial compilation is augmented: `compilation = compilation.AddSyntaxTrees(preCompilationTrees)`.
> 5. `DriverStateTable.Builder.SetCompilation` is called, which stores the compilation and creates the `SyntaxStore.Builder` internally.
> 6. Standard source output nodes execute against the augmented compilation.

Phase enforcement, quoted:

> "To catch this, the `DriverStateTable.Builder` does **not** have the `Compilation` or `SyntaxStore`
> set during the pre-compilation phase. Accessing either property throws an
> `InvalidOperationException`."
>
> "When a pre-compilation output fails (whether from accessing compilation-dependent inputs or from
> any other exception), the generator is placed in **error state**: a diagnostic is reported, and
> the generator's standard phase is **skipped entirely**. Other generators are unaffected."

### 5.3 Consequences for interceptors (derived; stated as such)

`docs/features/pre-compilation-source-outputs.md` contains **zero** occurrences of "intercept", and
`GeneratorDriverTests_PreCompilation.cs` (1658 lines) contains zero as well. So the following is
mechanism-derived, not documented:

1. **A pre-compilation output cannot call `GetInterceptableLocation`.** That API is an extension on
   `SemanticModel`. During the pre-compilation phase the `DriverStateTable.Builder` has neither
   `Compilation` nor `SyntaxStore` set, and accessing either throws `InvalidOperationException`.
   Piping `CompilationProvider` into `RegisterPreCompilationSourceOutput` puts the generator into
   error state and skips its standard phase. So an interceptor emitted at this stage would need a
   hand-built attribute string, which requires reproducing xxHash128 over the file's UTF-16 code
   units and the exact `SyntaxNode.Position` — precisely what CS9270's message tells generator
   authors not to do.
2. **A pre-compilation output can still emit an interceptor targeting an unchanged user file**, if
   it obtained the location some other way, because pre-compilation trees are added to the initial
   compilation and the checksum of the user's file is unaffected by that addition.
3. **Calls inside pre-compilation-generated files become interceptable by the standard phase**,
   because those trees are in the compilation that `RegisterSourceOutput` sees. A standard-phase
   generator can therefore call `GetInterceptableLocation` on an invocation in another generator's
   pre-compilation output. This is a genuinely new capability in the .NET 11 wave, and the document
   advertises the general form of it: "pre-compilation sources are visible to *all* generators'
   standard phases, not just the generator that produced them."
4. **Calls inside standard-phase (`RegisterSourceOutput`) output remain effectively un-targetable**
   by the API, because that source is "part of final output but not fed back into compilation", so
   no `SemanticModel` in any generator phase ever sees it. The compiler's *validation* would still
   resolve such a file, since `GetSyntaxTreesByContentHash` enumerates the final compilation's trees,
   so the restriction is one of API reach, not of compiler capability.
5. **A pre-compilation output changes the compilation's tree set but not any existing tree's text**,
   so it cannot invalidate an interceptor — unless it emits a file whose text happens to be
   character-identical to an existing tree, which would trigger CS9233 for that text.

### 5.4 Reparse caveat

> "Like post-initialization trees, pre-compilation trees are reparsed when parse options change
> between driver runs. A unified `RequiresConstantTreeReparse` check handles both post-init and
> pre-compilation trees in a single pass."

Reparsing does not change text, hence does not change content hashes.

### 5.5 The competing proposal

The document compares itself at length with
<https://github.com/dotnet/roslyn/issues/81395> ("Two-Phase Incremental Generators", proposing
`RegisterDeclarationOutput`). Key distinction it draws: `RegisterPreCompilationSourceOutput` adds
**no additional compilation phase** (trees are added via `AddSyntaxTrees` to the initial
compilation), whereas `RegisterDeclarationOutput` would require a third compilation. The two are
described as complementary, not competing. The primary motivation is Razor: replacing Razor's
private intermediate compilation is reported as "roughly 50% performance improvement".

---

## 6. Interaction with C# 15 and preview features

### 6.1 Runtime async

* `docs/compilers/CSharp/Runtime Async Design.md` (22.7 KB): **zero** occurrences of "intercept".
* `InterceptorsTests.cs` (9566 lines): **zero** occurrences of `async`.
* Feature flag: `/features:runtime-async` (`Feature.RuntimeAsync = "runtime-async"`).
* Feature status table: "Runtime Async | main | Main feature merged into main in preview"
  (<https://github.com/dotnet/roslyn/issues/75960>); "Runtime Async Streams" is still on the
  `features/runtime-async-streams` branch and listed as in progress.

**No rule exists either way.** Mechanically, interception is a bind-time attribute decode plus a
lowering-time substitution of the method symbol on a `BoundCall` (`LocalRewriter_Call.cs`), while
runtime async is an emit strategy for the *containing* method. Nothing in the interceptor code path
inspects async-ness, and the signature-matching rules make no mention of it. An interceptor is an
ordinary method and may be declared `async`, subject only to the normal requirement that its return
type and parameters match. Treat "a runtime-async method can be intercepted" and "an interceptor may
itself be runtime-async" as **unverified, with no code path forbidding either**; there are no tests,
so there is also no guarantee.

### 6.2 Unions, closed classes

* `proposals/csharp-15.0/unions.md` and `proposals/csharp-15.0/closed-hierarchies.md`: no interceptor
  interaction.
* New public API is syntax and symbol shape only: `SyntaxKind.UnionDeclaration = 9082`,
  `SyntaxKind.UnionKeyword = 8452`, `UnionDeclarationSyntax` (with `Update`, `With*`, `Add*` and
  visitor members), `ITypeSymbol.IsUnion`, `ITypeSymbol.UnionCaseTypes`, `Conversion.IsUnion`,
  `CommonConversion.IsUnion`; `SyntaxKind.ClosedKeyword = 8453`, `ITypeSymbol.IsClosed`,
  `ITypeSymbol.GetClosedDerivedTypeInfo(CancellationToken)`, `ClosedDerivedTypeInfo`
  (`ClosedDerivedTypes`, `IsComplete`). Also `WellKnownMemberNames.HasValuePropertyName = "HasValue"`
  and `WellKnownMemberNames.TryGetValueMethodName = "TryGetValue"`.
* No interceptor-specific rule. A method declared inside a `union` or a `closed` type is still an
  ordinary method and its calls are interceptable by the general rule; nothing special is written
  down about it.
* Both introduce contextual keywords and therefore parse-level breaking changes, documented in
  `docs/compilers/CSharp/Compiler Breaking Changes - DotNet 11.md`.

### 6.3 Unsafe evolution

**The premise in the assignment is not borne out.** `proposals/unsafe-evolution.md` (66 KB) contains
**zero** occurrences of "intercept", case-insensitive. Its list of constructs that require an
`unsafe` context, under "Redefining expressions that require unsafe contexts", is exactly:

> * [Pointer indirections][pointer-indirection]
> * [Pointer member access][pointer-member-access]
> * [Pointer element access][pointer-element-access]
> * Function pointer invocation
> * Element access on a fixed-size buffer
> * `stackalloc` under the conditions defined below

plus the general indirect rule:

> "In addition to these expressions, expressions and statements can also conditionally require an
> `unsafe` context if they depend on any symbol that is marked as `unsafe`. For example, calling a
> method that is *requires-unsafe* will cause the _invocation_expression_ to require an `unsafe`
> context. Statements with invocations embedded (such as `using`s, `foreach`, and similar) can also
> require an `unsafe` context when they use a *requires-unsafe* member."

The connection to interceptors is real but indirect and unspecified:

* Pointer member access (`ptr->M()`) is explicitly interceptable per the interceptors document, and
  it always requires an `unsafe` context. So interception of `ptr->M()` implies the call site is in
  an unsafe context; nothing says whether the *interceptor* must be.
* Function pointer invocation is **not** interceptable (CS9207, `.WithArguments("fnptr")`).
* Whether replacing a *requires-unsafe* callee with a non-*requires-unsafe* interceptor, or the
  reverse, changes the call site's unsafe requirement is **not specified anywhere**, and the
  substitution happens in lowering, after the binder has already decided the unsafe requirement.
  This is a genuine open question.

The only .NET 11 wave commit that touched `InterceptorsTests.cs` is in fact an unsafe-evolution
commit — `Unsafe evolution: check overrides and implementations (#82172)`, **2026-02-03**, sha
`86c2b0f5a98afa1b1f7112f29f0dbcfc4929fc16`. Its diff against `InterceptorsTests.cs` is **11 lines
and purely mechanical**: it replaced the inline `s_attributesSource` string literal with
`TestSources.InterceptsLocationAttribute`. **No behavioural interceptor change.**

New unsafe-evolution public API in the wave, all `[RSEXPERIMENTAL006]`
(`RoslynExperiments.PreviewLanguageFeatureApi`):

* `Microsoft.CodeAnalysis.ISymbol.RequiresUnsafeContext.get -> bool`
* `Microsoft.CodeAnalysis.IModuleSymbol.MemorySafetyRulesVersion.get -> MemorySafetyRulesVersion`
* `CSharpCompilationOptions.MemorySafetyRulesVersion` and `.WithMemorySafetyRulesVersion(MemorySafetyRulesVersion)`
* `MemorySafetyRulesVersion.Version1 = 1`, `.Version2 = 2`
* `SyntaxKind.SafeKeyword = 8454`, `SyntaxKind.UnsafeExpression = 8769`
* `Syntax.UnsafeExpressionSyntax` (the `unsafe(expr)` expression form) plus `SyntaxFactory.UnsafeExpression(...)`, visitor and rewriter members

Feature flag: `/features:updated-memory-safety-rules`. Metadata attributes synthesised by the
compiler: `MemorySafetyRulesAttribute` (assembly-level opt-in) and `RequiresUnsafeAttribute`
(per-member). "It is an error to apply the `MemorySafetyRulesAttribute` or `RequiresUnsafeAttribute`
to any symbol explicitly in source."

### 6.4 .NET 11 compiler breaking changes — nothing about interceptors

`docs/compilers/CSharp/Compiler Breaking Changes - DotNet 11.md`, 328 lines, section headings:

1. The *safe-context* of a collection expression of Span/ReadOnlySpan type is now *declaration-block*
2. Scenarios requiring compiler to synthesize a `ref readonly` returning delegate now require `System.Runtime.InteropServices.InAttribute`
3. Scenarios utilizing `ref readonly` local functions now require `InAttribute`
4. Dynamic evaluation of `&&`/`||` operators is not allowed with the left operand statically typed as an interface
5. `nameof(this.)` in attributes is disallowed
6. Parsing of `with` within a switch-expression-arm
7. `with()` as a collection expression element is treated as collection construction *arguments*
8. Pointer types no longer require an unsafe context
9. `safe` is a contextual keyword
10. `unsafe` required for more members (introduced in Visual Studio 2026 version 18.9; "fixed under `langversion:16`"; new diagnostic **CS9363**; example fix `int b = unsafe(c[null]);`)
11. `closed` is a contextual keyword in type declaration contexts (Visual Studio 2026 version 18.10; CS9380 / CS1519)
12. `union` is a contextual keyword in type declaration contexts

Zero mentions of interceptors, generators, xxHash or content hashes.

Side observation, out of scope but worth recording: item 10 already refers to `langversion:16` and
Visual Studio 18.9 / 18.10, while `LanguageVersion.CSharp16` does **not** yet exist in the public
API — `PublicAPI.Unshipped.txt` adds only `CSharp15 = 1500`, and `LanguageVersion.CurrentVersion`
and `MapSpecifiedToEffectiveVersion(Latest)` both resolve to `CSharp15`.

---

## 7. IDE and design-time behaviour

Two facts a tool hosted inside the analyzer process should know.

**7.1 The IDE resolves interceptors by re-decoding the attribute itself, not by asking the compiler.**
`src/Workspaces/SharedUtilitiesAndExtensions/Compiler/CSharp/Services/SemanticFacts/CSharpSemanticFacts.cs`
(this powers Go To Definition and Quick Info on an intercepted call):

```csharp
// Supported syntax points for interception in v1 are:
//
// Goo()
// X.Goo()
// X?.Goo()
var expression = simpleName.Parent switch
{
    MemberAccessExpressionSyntax memberAccess when memberAccess.Name == simpleName => memberAccess,
    MemberBindingExpressionSyntax memberBinding when memberBinding.Name == simpleName => memberBinding,
    _ => (ExpressionSyntax)simpleName,
};

if (expression.Parent is not InvocationExpressionSyntax)
    return null;

var contentHash = await document.GetContentHashAsync(cancellationToken).ConfigureAwait(false);
var interceptsLocationData = new InterceptsLocationData(contentHash, simpleName.FullSpan.Start);

// We only look for interceptors in generated source documents.  Interceptors cannot reasonably be written by
// hand (as they involve embedded an encoded version of a file's content hash, position, and other debugging
// information).  So the only realistic way to create them is by asking the compiler to create the attribute
// using SemanticModel.GetInterceptableLocation as part of a generator.
foreach (var generatedDocument in await document.Project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false))
{
    var syntaxIndex = await generatedDocument.GetSyntaxTreeIndexAsync(cancellationToken).ConfigureAwait(false);
    if (!syntaxIndex.TryGetInterceptsLocation(interceptsLocationData, out var methodDeclarationSpan))
        continue;
    ...
}
```

`simpleName.FullSpan.Start` here is the same value as `nameSyntax.Position` in the compiler, so the
two agree. But the IDE path searches **only source-generated documents**, so an interceptor written
by hand or contributed by any non-generator mechanism is invisible to Go To Definition and Quick
Info. The comment also states the design assumption plainly: interceptors are expected to come from
generators using `GetInterceptableLocation`, and nowhere else.

**7.2 The feature document's own statement about the editor:**

> "Interceptors are treated like a post-compilation step in this design. Diagnostics are given for
> misuse of interceptors, but some diagnostics are only given in the command-line build and not in
> the IDE. There is limited traceability in the editor for which calls in a compilation are actually
> being intercepted."
>
> "`GetInterceptorMethod(this SemanticModel, InvocationExpressionSyntax, CancellationToken)` enables
> analyzers to determine if a call is being intercepted, and if so, which method is intercepting the
> call."

---

## 8. Canonical generator pattern, from Roslyn's own tests

`src/Compilers/CSharp/Test/Semantic/SourceGeneration/GeneratorDriverTests.cs`,
`GetInterceptsLocationSpecifier_01` / `InterceptorGenerator1`:

```csharp
[Generator(LanguageNames.CSharp)]
private class InterceptorGenerator1 : IIncrementalGenerator
{
    record InterceptorInfo(InterceptableLocation locationSpecifier, object data);

    private static bool IsInterceptableCall(SyntaxNode node, CancellationToken token) => node is InvocationExpressionSyntax;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var interceptorInfos = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: IsInterceptableCall,
            transform: (GeneratorSyntaxContext context, CancellationToken token) =>
            {
                var model = context.SemanticModel;
                var locationSpecifier = model.GetInterceptableLocation((InvocationExpressionSyntax)context.Node, token);
                if (locationSpecifier is null)
                {
                    return null; // generator wants to intercept call, but host thinks call is not interceptable. bug.
                }

                // generator is careful to propagate only equatable data (i.e., not syntax nodes or symbols).
                return new InterceptorInfo(locationSpecifier, GetData(context));
            })
            .Where(info => info != null)
            .Collect();

        context.RegisterSourceOutput(interceptorInfos, (context, interceptorInfos) =>
        {
            ...
            builder.AppendLine($$"""
                    // {{locationSpecifier.GetDisplayLocation()}}
                    [InterceptsLocation({{locationSpecifier.Version}}, "{{locationSpecifier.Data}}")]
                    public static void Interceptor(this Program program, int param)
                    {
                        Console.Write(1);
                    }
                """);
            ...
            context.AddSource("MyInterceptors.cs", builder.ToString());
        });
    }
}
```

The comment "generator is careful to propagate only equatable data (i.e., not syntax nodes or
symbols)" is why `InterceptableLocation` implements `IEquatable<InterceptableLocation>` — it is
designed to be a value in an incremental state table.

The test's parse options are
`TestOptions.RegularPreview.WithFeature(Feature.InterceptorsNamespaces, "global")` and the driver is
created with `new GeneratorDriverOptions(baseDirectory: Path.Combine(projectDir, "obj"))`.

`GetDisplayLocation()` output shape (from the XML comment on `InterceptableLocation1`):

```
C:\project\src\Program.cs(12,34)
```

or, with a path map configured, `/_/src/Program.cs(12,34)`. The mapping goes through
`Compilation.Options.SourceReferenceResolver.NormalizePath(path, baseFilePath: null)` — added by
`Map the path in InterceptableLocation.GetDisplayPath (#76449)`.

Encoded `Data` values seen in real test baselines (base64, 28-40 characters):
`OC8Ntn0ZsekhqswDcyGy6ZgAAAA=`, `ugKu5/LV5oAEk8GTsnS0hJQAAAA=`,
`ZnP1PXDK5WDD07FTErR9eWUAAABQcm9ncmFtLmNz` (the tail of the last one decodes to `Program.cs`).

---

## 9. Open questions

1. Does replacing a *requires-unsafe* callee with a non-*requires-unsafe* interceptor, or the
   reverse, change the unsafe requirement of the call site under `updated-memory-safety-rules`?
   Unspecified; the substitution happens in lowering, after the binder has decided.
2. Can a runtime-async method's call be intercepted, and can the interceptor itself be compiled with
   runtime async? No tests, no specification text, no code path that forbids it.
3. Will a version 2 encoding land before .NET 11 GA in November 2026? No branch, no issue found; the
   code comments only reserve the possibility.
4. Will `InterceptsLocationAttribute` ever be added to the BCL? Still absent from
   `System.Private.CoreLib` in `dotnet/runtime` `main`; generators must keep declaring it, and the
   recommended form is `file class`.
5. Will `docs/features/interceptors.md` be updated for `extension` block interception before GA? It
   is 21 months stale on that point and still carries a "before releasing .NET 8" TODO.
6. Is emitting an interceptor from `RegisterPreCompilationSourceOutput` a supported scenario at all?
   The design document is silent and there are no tests, yet the phase has no `SemanticModel`, which
   makes the supported attribute-creation API unusable there.

---

## 10. Source URLs

Roslyn (`main`, unless a commit is named):

* <https://github.com/dotnet/roslyn/blob/main/docs/features/interceptors.md>
* <https://github.com/dotnet/roslyn/blob/main/docs/features/pre-compilation-source-outputs.md>
* <https://github.com/dotnet/roslyn/blob/main/docs/Language%20Feature%20Status.md>
* <https://github.com/dotnet/roslyn/blob/main/docs/compilers/CSharp/Compiler%20Breaking%20Changes%20-%20DotNet%2011.md>
* <https://github.com/dotnet/roslyn/blob/main/docs/compilers/CSharp/Runtime%20Async%20Design.md>
* <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Utilities/InterceptableLocation.cs>
* <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/CSharpExtensions.cs>
* <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Compilation/CSharpSemanticModel.cs>
* <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Compilation/CSharpCompilation.cs>
* <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Symbols/Source/SourceMethodSymbolWithAttributes.cs>
* <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Syntax/SyntaxNodeExtensions.cs>
* <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/CSharpParseOptions.cs>
* <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Errors/ErrorCode.cs>
* <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/CSharpResources.resx>
* <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/LanguageVersion.cs>
* <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/PublicAPI.Shipped.txt>
* <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/PublicAPI.Unshipped.txt>
* <https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/PublicAPI.Unshipped.txt>
* <https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/Text/SourceText.cs>
* <https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/CommandLine/Feature.cs>
* <https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/InternalUtilities/RoslynExperiments.cs>
* <https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/Symbols/Attributes/AttributeDescription.cs>
* <https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/SourceGeneration/Nodes/PreCompilationSourceOutputNode.cs>
* <https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/MSBuildTask/Csc.cs>
* <https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/MSBuildTask/Microsoft.CSharp.Core.targets>
* <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Test/Semantic/Semantics/InterceptorsTests.cs>
* <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Test/Semantic/SourceGeneration/GeneratorDriverTests.cs>
* <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Test/Semantic/SourceGeneration/GeneratorDriverTests_PreCompilation.cs>
* <https://github.com/dotnet/roslyn/blob/main/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Utilities/InterceptsLocationUtilities.cs>
* <https://github.com/dotnet/roslyn/blob/main/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/CSharp/Services/SemanticFacts/CSharpSemanticFacts.cs>
* <https://github.com/dotnet/roslyn/blob/main/eng/Versions.props>
* Commit `86c2b0f5a98afa1b1f7112f29f0dbcfc4929fc16` (PR #82172), 2026-02-03
* Pull requests referenced: #72814 (checksum-based interceptors), #72998 (conditional access),
  #74509 (implicit receiver capture), #74865 (InterceptorsNamespaces rename), #76312 (stable, and
  deprecate path-based attributes), #76344 (fix for #76341), #76449 (map path in GetDisplayLocation),
  #76642 (CS9270 into the .NET 9 warning wave), #77137 (IEquatable), #79010 (Extensions:
  interceptors), #81591 (centralize feature flags), #83088 (pre-compilation source outputs)
* Issues: <https://github.com/dotnet/roslyn/issues/72133> (InterceptableLocation API),
  <https://github.com/dotnet/roslyn/issues/72093> (GetInterceptorMethod),
  <https://github.com/dotnet/roslyn/issues/76341> (do not identify calls by path; closed),
  <https://github.com/dotnet/roslyn/issues/83089> (PreCompilation API review),
  <https://github.com/dotnet/roslyn/issues/81395> (two-phase generators),
  <https://github.com/dotnet/roslyn/issues/53632> (config options in post-init),
  <https://github.com/dotnet/roslyn/issues/75960> (runtime async test plan),
  <https://github.com/dotnet/roslyn/issues/81207> (unsafe evolution test plan)

csharplang:

* <https://github.com/dotnet/csharplang/blob/main/proposals/unsafe-evolution.md>
* <https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/unions.md>
* <https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/closed-hierarchies.md>
* <https://github.com/dotnet/csharplang/issues/7009> (interceptors championed issue)

sdk, docs, learn:

* <https://github.com/dotnet/sdk/blob/main/src/Cli/dotnet/Commands/Run/CSharpCompilerCommand.Generated.cs>
* <https://github.com/dotnet/docs/blob/main/docs/csharp/language-reference/compiler-messages/source-generator-errors.md>
* <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/source-generator-errors> (stale on the opt-in mechanism)
* <https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-generator>
* <https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12#interceptors>
