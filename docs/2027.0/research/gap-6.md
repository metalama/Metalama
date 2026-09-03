# Gap 6 — Symbol display, documentation comment IDs, CREF and SyntaxGenerator for unions, closed classes and extension indexers

Research date: **2026-09-03**. Target: **.NET 11 / C# 15 GA, November 2026**.
All statements below are verified against **`dotnet/roslyn` `main`** (the commit reachable on 2026-09-03) and the
`dotnet/csharplang` C# 15 proposals. Every file path is repo-relative to `dotnet/roslyn` unless stated otherwise.

---

## 0. Baseline: what is actually in `main`

The three features in scope are **already merged into `main`**. This was verified with the GitHub compare API:

```
gh api repos/dotnet/roslyn/compare/main...features/Unions        -> ahead_by = 0, behind_by = 2042
gh api repos/dotnet/roslyn/compare/main...features/closed-class  -> ahead_by = 0, behind_by = 2914
gh api repos/dotnet/roslyn/compare/main...features/extensions    -> ahead_by = 0, behind_by = 2270
```

`ahead_by = 0` means the feature branch contains nothing `main` does not already have.
`docs/Language Feature Status.md` still lists them under the "C# 15.0" table with their branch links
(<https://github.com/dotnet/roslyn/blob/main/docs/Language%20Feature%20Status.md>), which is a bookkeeping
artefact, not evidence that they are unmerged. Corroborating evidence in `main`:

| Feature | Evidence in `main` |
|---|---|
| Unions | `src/Compilers/CSharp/Portable/Symbols/Synthesized/SynthesizedUnionCtor.cs`, `SynthesizedUnionValuePropertySymbol.cs`, `Binder/UnionMatchingRewriter.cs`, `BoundTree/BoundPatternWithUnionMatching.cs`, `Utilities/TypeUnionValueSet.cs`, `Test/CSharp15/UnionsTests.cs` (56 861 lines) |
| Closed classes | `Symbols/Source/SourceMemberContainerSymbol.cs` (`IsClosed`), `Core/Portable/Compilation/ClosedDerivedTypeInfo.cs`, `Test/CSharp15/ClosedClassesTests.cs`, `Test/Syntax/Parsing/ClosedModifierParsingTests.cs` |
| Extension indexers | `Test/CSharp15/ExtensionIndexersTests.cs` (42 306 lines), `Binder/Binder_Crefs.cs` (`IndexerMemberCrefSyntax` inside `ExtensionMemberCrefSyntax`), `MessageID.IDS_FeatureExtensionIndexers` |

Language version: `Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp15 = 1500`
(`src/Compilers/CSharp/Portable/PublicAPI.Unshipped.txt`).
Extension indexers report `CS9327` / `CS9260` "Feature 'extension indexers' is not available in C# 14.0.
Please use language version 15.0 or greater." (`ExtensionIndexersTests.Cref_01`, `ExtensionTests.Member_InstanceIndexer`).

### New public API relevant to this gap

From `src/Compilers/Core/Portable/PublicAPI.Unshipped.txt`:

```
Microsoft.CodeAnalysis.ITypeSymbol.IsUnion.get -> bool
Microsoft.CodeAnalysis.ITypeSymbol.UnionCaseTypes.get -> System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.ITypeSymbol!>
Microsoft.CodeAnalysis.ITypeSymbol.IsClosed.get -> bool
Microsoft.CodeAnalysis.ITypeSymbol.GetClosedDerivedTypeInfo(System.Threading.CancellationToken cancellationToken) -> Microsoft.CodeAnalysis.ClosedDerivedTypeInfo
Microsoft.CodeAnalysis.ClosedDerivedTypeInfo
Microsoft.CodeAnalysis.ClosedDerivedTypeInfo.ClosedDerivedTypes.get -> System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.INamedTypeSymbol!>
Microsoft.CodeAnalysis.ClosedDerivedTypeInfo.IsComplete.get -> bool
Microsoft.CodeAnalysis.Operations.CommonConversion.IsUnion.get -> bool
const Microsoft.CodeAnalysis.WellKnownMemberNames.HasValuePropertyName = "HasValue" -> string!
const Microsoft.CodeAnalysis.WellKnownMemberNames.TryGetValueMethodName = "TryGetValue" -> string!
```

From `src/Compilers/CSharp/Portable/PublicAPI.Unshipped.txt`:

```
Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp15 = 1500
Microsoft.CodeAnalysis.CSharp.SyntaxKind.UnionKeyword = 8452
Microsoft.CodeAnalysis.CSharp.SyntaxKind.ClosedKeyword = 8453
[RSEXPERIMENTAL006]Microsoft.CodeAnalysis.CSharp.SyntaxKind.SafeKeyword = 8454
Microsoft.CodeAnalysis.CSharp.SyntaxKind.UnionDeclaration = 9082
Microsoft.CodeAnalysis.CSharp.Syntax.UnionDeclarationSyntax        (Base = TypeDeclarationSyntax)
Microsoft.CodeAnalysis.CSharp.Conversion.IsUnion.get -> bool
static Microsoft.CodeAnalysis.CSharp.SyntaxFactory.UnionDeclaration(...)   // single full-arity overload only
```

From `src/Compilers/Core/Portable/PublicAPI.Unshipped.txt` **there is no** new
`SymbolDisplayPartKind`, `SymbolDisplayKindOptions`, `SymbolDisplayMemberOptions`,
`SymbolDisplayMiscellaneousOptions`, `SymbolDisplayTypeQualificationStyle`,
`SymbolDisplayGenericsOptions` or `SymbolDisplayFormat` member.

From `src/Workspaces/Core/Portable/PublicAPI.Unshipped.txt` — the file contains **exactly three lines**:

```
Microsoft.CodeAnalysis.Editing.DeclarationModifiers.IsClosed.get -> bool
Microsoft.CodeAnalysis.Editing.DeclarationModifiers.WithIsClosed(bool isClosed) -> Microsoft.CodeAnalysis.Editing.DeclarationModifiers
static Microsoft.CodeAnalysis.Editing.DeclarationModifiers.Closed.get -> Microsoft.CodeAnalysis.Editing.DeclarationModifiers
```

`src/Workspaces/CSharp/Portable/PublicAPI.Unshipped.txt` is empty (1 blank line).

### Underlying symbol shape (the fact everything else follows from)

`src/Compilers/CSharp/Portable/Declarations/DeclarationKind.cs` adds `DeclarationKind.Union`, and
`src/Compilers/CSharp/Portable/Symbols/EnumConversions.cs` maps it:

```csharp
case DeclarationKind.Struct:
case DeclarationKind.Union:          // <-- union
case DeclarationKind.RecordStruct:
    return TypeKind.Struct;

case DeclarationKind.Extension:
    return TypeKind.Extension;
```

So:

* A `union` declaration is `TypeKind.Struct`, `IsRecord == false`
  (csharplang `unions.md`, "[Resolved] Is union declaration a record?": *"A union declaration is a plain struct,
  not record struct. The `record union ...` isn't supported"*).
* A `closed` class is `TypeKind.Class` with `SourceMemberContainerSymbol.IsClosed => HasFlag(DeclarationModifiers.Closed)`
  (`SourceMemberContainerSymbol.cs:902`), and gets a `[System.Runtime.CompilerServices.IsClosedTypeAttribute]`
  with a `DerivedTypes` property emitted (`SourceMemberContainerSymbol.cs:1978-2005`).
* `NamedTypeSymbol.IsUnionType` (`Symbols/NamedTypeSymbol.cs:1944`) is
  `TypeKind is TypeKind.Class or TypeKind.Struct && IsUnionTypeCore`, so a **hand-written `[Union] class` is also a
  union type**. The public `ITypeSymbol.IsUnion` is
  `UnderlyingTypeSymbol is Symbols.NamedTypeSymbol { IsUnionType: true }`
  (`Symbols/PublicModel/TypeSymbol.cs:205`).
* There is **no public way to tell a `union` *declaration* from a hand-written `[Union] struct`** except by
  inspecting `DeclaringSyntaxReferences` for a `UnionDeclarationSyntax`. The distinguishing predicate
  `SourceMemberContainerSymbol.IsUnionDeclaration` (line 1054) is `internal`.
* `TypeKind.Extension = 14` (`src/Compilers/Core/Portable/Symbols/TypeKind.cs`). There is **no `TypeKind.Union`**
  and no `TypeKind.ClosedClass`.

### Union lowering (csharplang `proposals/csharp-15.0/unions.md`, "#### Lowering")

```csharp
public union Pet(Cat, Dog){ ... }
```

is lowered to

```csharp
[Union] public struct Pet : IUnion
{
    public Pet(Cat value) => Value = value;
    public Pet(Dog value) => Value = value;

    public object? Value { get; }

    ... // original body
}
```

Implementation, verified in source:

* `SynthesizedUnionValuePropertySymbol` (`Symbols/Synthesized/SynthesizedUnionValuePropertySymbol.cs`)
  — a `SourcePropertySymbolBase` named `WellKnownMemberNames.ValuePropertyName` (`"Value"`),
  `modifiers: DeclarationModifiers.Public`, `hasGetAccessor: true`, `hasSetAccessor: false`,
  `hasAutoPropertyGet: true`, type `TypeWithAnnotations.Create(System_Object, NullableAnnotation.Annotated)`
  (that is, `object?`), and **`public override bool IsImplicitlyDeclared => true;`** (line 44).
* `SynthesizedUnionCtor` (`Symbols/Synthesized/SynthesizedUnionCtor.cs`)
  — derives from `SynthesizedInstanceConstructor`; one parameter, ordinal 0, `RefKind.None`, named
  `ParameterSymbol.ValueParameterName` (`"value"`); `DeclaredAccessibility => Accessibility.Public`;
  `IsImplicitlyDeclared` (asserted at line 74); emits `[CompilerGenerated]`. Its body assigns
  `valueProperty.DeclaredBackingField`, so a backing field `<Value>k__BackingField` exists.
* The *non-boxing access pattern* members `HasValue` / `TryGetValue` are **optional** and are **not** synthesized
  by a `union` declaration; the two new `WellKnownMemberNames` constants exist for recognising hand-written
  union types (unions.md, "#### Non-boxing access members").

### Extension lowering names (C# 14, unchanged in C# 15)

`src/Compilers/Core/Portable/Symbols/WellKnownMemberNames.cs`:

```csharp
internal const string ExtensionGroupingTypePrefix = "<G>$";
internal const string ExtensionMarkerTypePrefix   = "<M>$";
```

`src/Compilers/CSharp/Portable/Symbols/Source/SourceNamedTypeSymbol_Extension.cs:1098-1165`:

```csharp
LazyExtensionGroupingName = WellKnownMemberNames.ExtensionGroupingTypePrefix + RawNameToHashString(ComputeExtensionGroupingRawName());
LazyExtensionMarkerName   = WellKnownMemberNames.ExtensionMarkerTypePrefix   + RawNameToHashString(ComputeExtensionMarkerRawName());
```

`RawNameToHashString` is `XxHash128` over the UTF-16 code units (endianness-normalised), rendered by
`HexUtilities.ToHexString` — 16 bytes → **32 upper-case hex characters**. Hence names of the form
`<G>$8B58B811E742D8E9EA7E14F878F87B0F` and `<M>$2C37A6F24442AF359D03A7723186221C`.

**Important:** the C# 14 speclet `proposals/csharp-14.0/extensions.md` still shows the *old* placeholder names
(`<>E__MarkerContentName_For_ExtensionOfT`) in its XML-doc example. The speclet is stale on this point; the
implementation and the C# 15 `extension-indexers.md` metadata section use the `<G>$` / `<M>$` + hash form.
Use the implementation.

---

## 1. Symbol display

### 1.1 There is no union or closed awareness in SymbolDisplay at all

Every file in `src/Compilers/CSharp/Portable/SymbolDisplay/` was downloaded and grepped:

```
ObjectDisplay.cs, SymbolDisplay.cs, SymbolDisplayVisitor.cs,
SymbolDisplayVisitor.Members.cs, SymbolDisplayVisitor.Types.cs,
SymbolDisplayVisitor_Constants.cs, SymbolDisplayVisitor_Minimal.cs
```

`grep -i -E "union|closed"` over all seven returns **zero matches**.
Likewise, a repo-wide code search for `"SyntaxKind.UnionKeyword"` and `"SyntaxKind.ClosedKeyword"` returns no
hit under `SymbolDisplay/` in either the C# or the Visual Basic compiler.

### 1.2 The type-keyword switch: a union prints as `struct`

`src/Compilers/CSharp/Portable/SymbolDisplay/SymbolDisplayVisitor.Types.cs`, `AddTypeKind`, lines 727-804:

```csharp
if (IsFirstSymbolVisited && Format.KindOptions.IncludesOption(SymbolDisplayKindOptions.IncludeTypeKeyword))
{
    ...
    switch (symbol.TypeKind)
    {
        case TypeKind.Class when symbol.IsRecord:   AddKeyword(SyntaxKind.RecordKeyword); ...
        case TypeKind.Struct when symbol.IsRecord:  ... RecordKeyword + StructKeyword ...
        case TypeKind.Module:
        case TypeKind.Class:                        AddKeyword(SyntaxKind.ClassKeyword); ...
        case TypeKind.Enum:                         AddKeyword(SyntaxKind.EnumKeyword); ...
        case TypeKind.Delegate:                     AddKeyword(SyntaxKind.DelegateKeyword); ...
        case TypeKind.Interface:                    AddKeyword(SyntaxKind.InterfaceKeyword); ...
        case TypeKind.Struct:
            if (symbol.IsReadOnly) { AddKeyword(SyntaxKind.ReadOnlyKeyword); AddSpace(); }
            if (symbol.IsRefLikeType) { AddKeyword(SyntaxKind.RefKeyword); AddSpace(); }
            AddKeyword(SyntaxKind.StructKeyword);
            AddSpace();
            break;
    }
}
```

There is no `case TypeKind.Struct when symbol.IsUnion`. A `union` declaration is `TypeKind.Struct`, so with
`SymbolDisplayKindOptions.IncludeTypeKeyword` a union **renders as `struct Pet`, never `union Pet`**.

`TypeKind.Extension` is absent from the switch too, so no keyword is added there; the word `extension` for an
extension block comes from `AddNameAndTypeArgumentsOrParameters`, not from `AddTypeKind`.

### 1.3 `SymbolDisplayPartKind`: a union type name is `StructName`

Same file, `GetPartKind`, lines 639-663:

```csharp
case TypeKind.Class when symbol.IsRecord:  return SymbolDisplayPartKind.RecordClassName;
case TypeKind.Struct when symbol.IsRecord: return SymbolDisplayPartKind.RecordStructName;
case TypeKind.Submission:
case TypeKind.Module:
case TypeKind.Class:                       return SymbolDisplayPartKind.ClassName;
case TypeKind.Delegate:                    return SymbolDisplayPartKind.DelegateName;
case TypeKind.Enum:                        return SymbolDisplayPartKind.EnumName;
case TypeKind.Error:                       return SymbolDisplayPartKind.ErrorTypeName;
case TypeKind.Interface:                   return SymbolDisplayPartKind.InterfaceName;
case TypeKind.Struct:                      return SymbolDisplayPartKind.StructName;
default:                                   throw ExceptionUtilities.UnexpectedValue(symbol.TypeKind);
```

So:

* union type name → `SymbolDisplayPartKind.StructName` (= 23)
* closed class name → `SymbolDisplayPartKind.ClassName` (= 2)
* `closed record class` name → `SymbolDisplayPartKind.RecordClassName` (= 31)
* extension block name → `SymbolDisplayPartKind.ClassName`, added explicitly at
  `SymbolDisplayVisitor.Types.cs:345` and `:1004`.

### 1.4 The full `SymbolDisplayPartKind` member list is unchanged

`src/Compilers/Core/Portable/SymbolDisplay/SymbolDisplayPartKind.cs` ends at

```csharp
RecordClassName = 31,
RecordStructName = 32,
```

with `InternalSymbolDisplayPartKind.Arity = 33`, `Other = 34` (internal). No `UnionName`, no `ClosedClassName`,
no `ExtensionBlockName`, no `IndexerName`. The enum bounds check `IsValid` still tops out at `RecordStructName`.

`SymbolDisplayKindOptions` (`.../SymbolDisplayKindOptions.cs`) still has exactly
`None = 0`, `IncludeNamespaceKeyword = 1<<0`, `IncludeTypeKeyword = 1<<1`, `IncludeMemberKeyword = 1<<2`.

`SymbolDisplayMemberOptions` still has `None`, `IncludeType` (1<<0), `IncludeModifiers` (1<<1),
`IncludeAccessibility` (1<<2), `IncludeExplicitInterface` (1<<3), `IncludeParameters` (1<<4),
`IncludeContainingType` (1<<5), `IncludeConstantValue` (1<<6), `IncludeRef` (1<<7).

`SymbolDisplayMiscellaneousOptions` still has `None`, `UseSpecialTypes` (1<<0), `EscapeKeywordIdentifiers` (1<<1),
`UseAsterisksInMultiDimensionalArrays` (1<<2), `UseErrorTypeSymbolName` (1<<3), `RemoveAttributeSuffix` (1<<4),
`ExpandNullable` (1<<5), `IncludeNullableReferenceTypeModifier` (1<<6), `AllowDefaultLiteral` (1<<7),
`IncludeNotNullableReferenceTypeModifier` (1<<8), `CollapseTupleTypes` (1<<9), `ExpandValueTuple` (1<<10).

**The PublicAPI diff that reported "no new SymbolDisplay enum members" was correct, and this section shows the
behavioural consequence: the existing enum members simply describe unions as structs.**

### 1.5 `closed` is never rendered, because SymbolDisplay renders no type modifiers at all

`VisitNamedType` (`SymbolDisplayVisitor.Types.cs:198-306`) calls, in order: alias substitution, special-type
keyword, nullable unwrapping, minimal qualification, `AddTypeKind(symbol)`, delegate signature, namespace,
containing types, `AddNameAndTypeArgumentsOrParameters`.

It never calls `AddAccessibilityIfNeeded` or `AddMemberModifiersIfNeeded` (those are member-only paths in
`SymbolDisplayVisitor.Members.cs`). The only type-level modifiers ever emitted are `readonly` and `ref` inside
the `TypeKind.Struct` branch of `AddTypeKind`. `abstract`, `sealed`, `static`, `partial`, `file` and now
`closed` are **all** invisible to `ISymbol.ToDisplayString` for a named type. So dropping `closed` is consistent
with existing behaviour for `abstract`/`sealed`, not a new regression — but it does mean a closed class is
indistinguishable from an ordinary class in every display string.

### 1.6 Concrete outputs for the three named formats

The three formats are defined in `src/Compilers/Core/Portable/SymbolDisplay/SymbolDisplayFormat.cs`:

```csharp
public static SymbolDisplayFormat CSharpErrorMessageFormat { get; } = new SymbolDisplayFormat(
    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
    propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
    genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
    memberOptions: IncludeParameters | IncludeContainingType | IncludeExplicitInterface,
    parameterOptions: IncludeParamsRefOut | IncludeType,
    miscellaneousOptions: EscapeKeywordIdentifiers | UseSpecialTypes |
                          UseAsterisksInMultiDimensionalArrays | UseErrorTypeSymbolName |
                          IncludeNullableReferenceTypeModifier);
    // NOTE: kindOptions is NOT set  => SymbolDisplayKindOptions.None

public static SymbolDisplayFormat FullyQualifiedFormat { get; } = new SymbolDisplayFormat(
    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
    genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
    miscellaneousOptions: EscapeKeywordIdentifiers | UseSpecialTypes);
    // kindOptions NOT set

public static SymbolDisplayFormat MinimallyQualifiedFormat { get; } = new SymbolDisplayFormat(
    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
    genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
    memberOptions: IncludeParameters | IncludeType | IncludeRef | IncludeContainingType,
    kindOptions: SymbolDisplayKindOptions.IncludeMemberKeyword,     // member keyword only
    parameterOptions: IncludeName | IncludeType | IncludeParamsRefOut | IncludeDefaultValue,
    localOptions: SymbolDisplayLocalOptions.IncludeType,
    miscellaneousOptions: EscapeKeywordIdentifiers | UseSpecialTypes | IncludeNullableReferenceTypeModifier);
```

**None of the three sets `SymbolDisplayKindOptions.IncludeTypeKeyword`.** Consequently no type keyword is emitted
by any of them, and the "union prints as struct" problem is latent rather than immediately visible in these three:

Given `namespace N; public union Pet(Cat, Dog);` and `namespace N; public closed class GateState;`:

| Symbol | `CSharpErrorMessageFormat` | `FullyQualifiedFormat` | `MinimallyQualifiedFormat` |
|---|---|---|---|
| union `Pet` | `N.Pet` | `global::N.Pet` | `Pet` |
| closed class `GateState` | `N.GateState` | `global::N.GateState` | `GateState` |
| union ctor | `N.Pet.Pet(N.Cat)` | (types only) | `Pet.Pet(Cat value)` |
| union `Value` property | `N.Pet.Value` (`PropertyStyle.NameOnly`) | (types only) | `object Pet.Value` |

With a format that *does* set `IncludeTypeKeyword` — for example
`SymbolDisplayFormat.MinimallyQualifiedFormat.AddKindOptions(SymbolDisplayKindOptions.IncludeTypeKeyword)`,
which is what the IDE QuickInfo/`ISymbolDisplayService` layer uses — the union renders as **`struct Pet`** and the
closed class as **`class GateState`**.

### 1.7 Contextual-keyword escaping: `union` and `closed` are not special-cased

`src/Compilers/CSharp/Portable/Syntax/SyntaxKindFacts.cs` puts `UnionKeyword` and `ClosedKeyword` in
`IsContextualKeyword` / `GetContextualKeywordKind`, so a *type named* `union` or `closed` remains legal
(this is unlike the C# 14 `extension` keyword, whose speclet explicitly says
*"Types and aliases may not be named 'extension'."*).

`SymbolDisplayVisitor.EscapeIdentifier` (`SymbolDisplayVisitor.cs:138-148`):

```csharp
private static string EscapeIdentifier(string identifier, bool isNamedTypeOrAliasName)
{
    SyntaxKind kind = SyntaxFacts.GetKeywordKind(identifier);

    if (kind is SyntaxKind.None && isNamedTypeOrAliasName && StringComparer.Ordinal.Equals(identifier, "record"))
    {
        kind = SyntaxKind.RecordKeyword;
    }

    return kind == SyntaxKind.None ? identifier : $"@{identifier}";
}
```

Only `"record"` gets the named-type special case. A type named `union` or `closed` is therefore rendered
**unescaped** even under `SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers`, whereas a type named
`record` is rendered as `@record`. This is an asymmetry in `main` today. It is only harmful when the resulting
string is reparsed in a type-declaration position.

### 1.8 Extension blocks and extension indexers *do* display specially

`SymbolDisplayVisitor.Types.cs:294-298` — the containing type is always visited for an extension, regardless of
`TypeQualificationStyle`:

```csharp
if (Format.TypeQualificationStyle == SymbolDisplayTypeQualificationStyle.NameAndContainingTypes ||
    Format.TypeQualificationStyle == SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces ||
    symbol.IsExtension)                                   // <-- always for extensions
{ ... }
```

`SymbolDisplayVisitor.Types.cs:339-352, 466-478`:

```csharp
if (symbol.IsExtension)
{
    if (Format.CompilerInternalOptions.HasFlag(SymbolDisplayCompilerInternalOptions.UseMetadataMemberNames))
        Builder.Add(CreatePart(SymbolDisplayPartKind.ClassName, symbol, symbol.ExtensionGroupingName));
    else
        AddKeyword(SyntaxKind.ExtensionKeyword);
}
...
void addExtensionParameter(INamedTypeSymbol symbol)
{
    if (!Format.CompilerInternalOptions.HasFlag(SymbolDisplayCompilerInternalOptions.UseMetadataMemberNames)
        && symbol.ExtensionParameter is { } extensionParameter)
    {
        AddPunctuation(SyntaxKind.OpenParenToken);
        AddParameterModifiersAndType(extensionParameter);
        AddPunctuation(SyntaxKind.CloseParenToken);
    }
}
```

The marker name is appended only under the internal option, from `SymbolDisplay.cs:292-295`:

```csharp
if (symbol is INamedTypeSymbol { IsExtension: true } extension
    && format.CompilerInternalOptions.HasFlag(SymbolDisplayCompilerInternalOptions.UseMetadataMemberNames))
{
    visitor.AddExtensionMarkerName(extension);
}
```

`SymbolDisplayCompilerInternalOptions` is `internal`, so through the public API an extension block always shows as
`E.extension(int)` and never as `E.<G>$…`.

**Test that pins this — `ExtensionTests.EmptyExtension`**
(`src/Compilers/CSharp/Test/Emit3/Semantics/ExtensionTests.cs`, test at line 91, assertions at lines 161-179):

```csharp
AssertEx.Equal("<M>$C43E2675C7BBF9284AF22FB8A9BF0280", symbol.MetadataName);

var format = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
AssertEx.Equal("Extensions.extension(System.Object)", symbol.ToDisplayString(format));

format = new SymbolDisplayFormat(kindOptions: SymbolDisplayKindOptions.IncludeTypeKeyword);
AssertEx.Equal("Extensions.extension(Object)", symbol.ToDisplayString(format));   // no "class"/"struct" keyword

format = new SymbolDisplayFormat();
AssertEx.Equal("Extensions.extension(Object)", symbol.ToDisplayString(format));   // containing type always shown

format = new SymbolDisplayFormat(compilerInternalOptions: SymbolDisplayCompilerInternalOptions.UseMetadataMemberNames);
AssertEx.Equal("Extensions.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.<M>$C43E2675C7BBF9284AF22FB8A9BF0280", symbol.ToDisplayString(format));
```

Other pinned display strings for extension blocks (same file):

```csharp
// line 30339, format = IncludeTypeParameters | IncludeTypeConstraints
AssertEx.Equal("E.extension<T>(T) where T : struct", symbol.ToDisplayString(format));
// line 30362, format = IncludeType | IncludeModifiers on parameters
AssertEx.Equal("E.extension(ref readonly Int32)", symbol.ToDisplayString(format));
// line 9396
AssertEx.Equal("Extensions.extension(object).M(object, string)", m1.ToDisplayString());
// line 36565
AssertEx.Equal("E.extension(I<string>).P", model.GetSymbolInfo(memberAccess).Symbol.ToDisplayString());
```

#### Extension indexer display

An extension indexer is an ordinary `IPropertySymbol` with `IsIndexer == true` whose `ContainingType` is the
`TypeKind.Extension` block. `SymbolDisplayVisitor.Members.cs`, `AddPropertyNameAndParameters` (lines 198-227):

```csharp
if (symbol.IsIndexer)
{
    AddKeyword(SyntaxKind.ThisKeyword);
}
...
if (this.Format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeParameters) && symbol.Parameters.Any())
{
    AddPunctuation(SyntaxKind.OpenBracketToken);
    AddParametersIfNeeded(hasThisParameter: false, isVarargs: false, parameters: symbol.Parameters);
    AddPunctuation(SyntaxKind.CloseBracketToken);
}
```

The receiver is **not** part of the indexer's own parameter list; it appears only through the containing
extension block. So under `CSharpErrorMessageFormat` (which has `IncludeContainingType | IncludeParameters`)
an extension indexer renders as **`E.extension(int).this[int]`**.

Pinned by diagnostic-argument tests in `ExtensionTests.cs` (diagnostic arguments are formatted with
`CSharpErrorMessageFormat`):

```
// ExtensionTests.cs:4681
Diagnostic(ErrorCode.ERR_BadVisIndexerParam, "this").WithArguments("Extensions.extension(C).this[int]", "C")
// ExtensionTests.cs:43326
Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("E.extension(int).this[int].get")
// ExtensionTests.cs:44753
Diagnostic(ErrorCode.ERR_ProtectedInExtension, "this").WithArguments("E.extension(int).this[int]")
// ExtensionTests.cs:44756
Diagnostic(ErrorCode.ERR_ProtectedInExtension, "get").WithArguments("E.extension(int).this[int, int].get")
```

`ExtensionTests.Member_InstanceIndexer` (line 2395; assertions at 2424-2431) pins the member names and the
test-format display:

```csharp
AssertEx.SequenceEqual(["this[]"], symbol.MemberNames);
AssertEx.Equal("System.Int32 Extensions.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.this[System.Int32 i] { get; set; }",
    symbol.GetMember("this[]").ToTestDisplayString());
AssertEx.Equal([
    "System.Int32 Extensions.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.this[System.Int32 i] { get; set; }",
    "System.Int32 Extensions.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.this[System.Int32 i].get",
    "void Extensions.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.this[System.Int32 i].set"],
    symbol.GetMembers().ToTestDisplayStrings());
```

So **`IPropertySymbol.Name == "this[]"`** for an extension indexer, exactly as for an ordinary one.

`ExtensionTests2.ReduceExtensionMember_07` (line 37607, `WorkItem` roslyn#80273) pins the reduced form:

```csharp
var indexer = extension.GetMember<PropertySymbol>("this[]").GetPublicSymbol();
Assert.Equal("E.extension<object>(object).this[int]",
    indexer.ReduceExtensionMember(comp.GetSpecialType(SpecialType.System_Object).GetPublicSymbol()).ToDisplayString());
```

### 1.9 Tests that pin (or fail to pin) union and closed display

* `src/Compilers/CSharp/Test/Symbol/SymbolDisplay/SymbolDisplayTests.cs` (9 286 lines):
  `grep -ci "union\|closed"` → **0**. There is **no** SymbolDisplay test for unions or closed classes.
* `src/Compilers/CSharp/Test/CSharp15/UnionsTests.cs` (56 861 lines): 122 occurrences of
  `ToTestDisplayString`/`ToDisplayString`, but **zero** `SymbolDisplay…` format assertions and **zero**
  `GetDocumentationCommentId` / `CreateDeclarationId` / `GetDocumentationCommentXml` assertions.
* `src/Compilers/CSharp/Test/CSharp15/ClosedClassesTests.cs` (7 823 lines): same — 70 display-string
  assertions, zero display-format and zero documentation-comment-ID assertions.

**Conclusion for (1):** no new `SymbolDisplayPartKind`, `SymbolDisplayKindOptions`, `SymbolDisplayMemberOptions`
or `SymbolDisplayMiscellaneousOptions` member exists. A union is displayed exactly like the struct it is; the
`union` keyword never appears in any display string. The `closed` modifier never appears either. Extension
blocks and extension indexers *are* handled, and are displayed as `E.extension(receiverType).member`.

---

## 2. Documentation comment identifiers

There are **two** independent implementations, and they must be considered separately.

| | Used by | Source |
|---|---|---|
| `Microsoft.CodeAnalysis.DocumentationCommentId` (public, language-agnostic) | Workspaces, `GetSymbolsForDeclarationId`, `GetFirstSymbolForDeclarationId`, `CreateDeclarationId`, `CreateReferenceId` | `src/Compilers/Core/Portable/DocumentationCommentId.cs` (1 646 lines) |
| `Microsoft.CodeAnalysis.CSharp.DocumentationCommentIDVisitor` (internal, C#-specific) | `ISymbol.GetDocumentationCommentId()`, `Symbol.GetEscapedDocumentationCommentId()`, and the XML documentation file the compiler emits | `src/Compilers/CSharp/Portable/DocumentationComments/DocumentationCommentIDVisitor.cs` + `.PartVisitor.cs` |

### 2.1 Neither generator has any union or closed handling

`grep -i -E "union|closed"` over `DocumentationCommentId.cs`,
`DocumentationCommentIDVisitor.cs` and `DocumentationCommentIDVisitor.PartVisitor.cs` returns **zero matches**.
Both files contain extension handling only.

This is correct behaviour, and it is not a defect: a `union` declaration produces an ordinary named struct with
an ordinary, speakable name, and a `closed` class produces an ordinary named class. Neither introduces an
unspeakable name or a new member kind, so the existing ID grammar covers them with no change.

### 2.2 Exact ID strings

Take

```csharp
namespace N;
public record class Cat();
public record class Dog();
public union Pet(Cat, Dog);
public closed class GateState;
public class Open : GateState;
```

| Symbol | Documentation comment ID |
|---|---|
| union type `Pet` | `T:N.Pet` |
| union constructor `Pet(Cat)` | `M:N.Pet.#ctor(N.Cat)` |
| union constructor `Pet(Dog)` | `M:N.Pet.#ctor(N.Dog)` |
| union `Value` property | `P:N.Pet.Value` |
| union `Value` backing field | `F:N.Pet.<Value>k__BackingField` (C# generator) / `F:N.Pet.<Value>k__BackingField` (core generator) — see 2.5 for the `<`/`>` divergence |
| union `Value` getter | `M:N.Pet.get_Value` |
| generic union `Option<T>` | `T:N.Option`1` |
| closed class `GateState` | `T:N.GateState` |
| derived `Open` | `T:N.Open` |

Derivations:

* `T:` prefix from `DocumentationCommentIDVisitor.VisitNamedType` / `PrefixAndDeclarationGenerator.VisitNamedType`.
* `#ctor` because the C# generator uses `GetEscapedMetadataName(symbol)` and the metadata name of a constructor
  is `.ctor`, and `GetEscapedMetadataName` replaces `'.'` with `'#'`
  (`DocumentationCommentIDVisitor.PartVisitor.cs:268-289`). The core generator does the same through
  `EncodeName` (`DocumentationCommentId.cs:280-288`), which also replaces `'.'` with `'#'`.
* `P:N.Pet.Value` because `SynthesizedUnionValuePropertySymbol.Name == "Value"` and the property has no
  parameters, so `AppendParameters` adds nothing.
* Arity suffix `` `1 `` from `VisitNamedType`'s `symbol.TypeParameters.Length` branch.

There is **nothing union-specific** anywhere in the ID grammar. The set of case types is not represented in any
identifier; a union is addressed exactly like a struct, and its case types are addressed as the independent
types that they are.

### 2.3 Extension grouping and marker types

`DocumentationCommentIDVisitor.cs:68-78`:

```csharp
public override object VisitNamedType(NamedTypeSymbol symbol, StringBuilder builder)
{
    builder.Append("T:");
    PartVisitor.Instance.Visit(symbol, builder);
    if (symbol.IsExtension)
    {
        builder.Append('.');
        builder.Append(symbol.ExtensionMarkerName);
    }
    return null;
}
```

`DocumentationCommentIDVisitor.PartVisitor.cs:174-184`:

```csharp
public override object VisitNamedType(NamedTypeSymbol symbol, StringBuilder builder)
{
    Symbol containingSymbol = symbol.ContainingSymbol;
    if ((object)containingSymbol != null &&
        (containingSymbol.Name.Length != 0 || containingSymbol is NamedTypeSymbol { IsExtension: true }))
    {
        Visit(containingSymbol, builder);
        builder.Append('.');
    }
    builder.Append(symbol.IsExtension ? symbol.ExtensionGroupingName : symbol.Name);
    ...
}
```

The core generator mirrors this at `DocumentationCommentId.cs:386-397` (`PrefixAndDeclarationGenerator.VisitNamedType`),
`:534-546` (`DeclarationGenerator.VisitNamedType`), `:571-586` (`ReferenceGenerator.BuildDottedName`)
and `:601-614` (`ReferenceGenerator.VisitNamedType`).

Note that the grouping name is passed through `EncodeName` (which only maps `'.'`→`'#'`) and the marker name is
appended **raw**. Neither escapes `<`, `>` or `$`. The angle brackets survive into the ID and are XML-escaped
only when written into an XML attribute, by `Symbol.GetEscapedDocumentationCommentId`
(`src/Compilers/CSharp/Portable/Symbols/Symbol.cs:1001-1011`):

```csharp
public string? GetEscapedDocumentationCommentId()
{
    var documentationCommentId = GetDocumentationCommentId();
    return documentationCommentId is null ? null : escape(documentationCommentId);

    static string escape(string s)
    {
        Debug.Assert(!s.Contains("&"));
        return s.Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
```

#### Pinned test — `ExtensionTests2.DocumentationCommentId_01`

`src/Compilers/CSharp/Test/Emit3/Semantics/ExtensionTests2.cs:35977`,
`[Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78606")]`, source
`public static class E { extension(int i1) { public void M() { } } }`:

```csharp
// extension block (grouping + marker)
AssertEx.Equal("E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.<M>$531E7AC45D443AE2243E7FFAB9455D60",
    DocumentationCommentId.CreateReferenceId(extension));
AssertEx.Equal("T:E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.<M>$531E7AC45D443AE2243E7FFAB9455D60",
    DocumentationCommentId.CreateDeclarationId(extension));
Assert.Equal("<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69", extension.ExtensionGroupingName);
Assert.Equal("<M>$531E7AC45D443AE2243E7FFAB9455D60", extension.ExtensionMarkerName);
AssertEx.Equal("T:E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.<M>$531E7AC45D443AE2243E7FFAB9455D60",
    extension.GetDocumentationCommentId());

// ROUND TRIP
var found = (INamedTypeSymbol)DocumentationCommentId
    .GetSymbolsForDeclarationId(DocumentationCommentId.CreateDeclarationId(extension), comp).Single();
Assert.True(found.IsExtension);
AssertEx.Equal("E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.<M>$531E7AC45D443AE2243E7FFAB9455D60",
    found.ToTestDisplayString());

// extension member
var m = e.GetTypeMembers().Single().GetMember<MethodSymbol>("M").GetPublicSymbol();
Assert.Equal("", DocumentationCommentId.CreateReferenceId(m));            // <-- empty string!
Assert.Equal("M:E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M", m.GetDocumentationCommentId());

var declarationId = DocumentationCommentId.CreateDeclarationId(m);
AssertEx.Equal("M:E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M", declarationId);
AssertEx.Equal("void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M()",
    DocumentationCommentId.GetFirstSymbolForDeclarationId(declarationId, comp).ToTestDisplayString());

// implementation method on the static class
var mImplementation = e.GetMember<MethodSymbol>("M").GetPublicSymbol();
declarationId = DocumentationCommentId.CreateDeclarationId(mImplementation);
AssertEx.Equal("M:E.M(System.Int32)", declarationId);
AssertEx.Equal("void E.M(this System.Int32 i1)",
    DocumentationCommentId.GetFirstSymbolForDeclarationId(declarationId, comp).ToTestDisplayString());
```

The whole block is run three times: on the source compilation, on a compilation referencing the emitted image,
and (for the metadata shapes only) from a Visual Basic compilation. `DocumentationCommentId_02` repeats it for
the generic case, producing
`T:E.<G>$8048A6C8BE30A622530249B904B537EB`1.<M>$D1693D81A12E8DED4ED68FE22D9E856F` and
`M:E.<G>$8048A6C8BE30A622530249B904B537EB`1.M`, and `DocumentationCommentId_03` for a hand-authored IL grouping
type named `GroupingType`/`MarkerType`, producing `T:E.GroupingType`1.MarkerType`.

Note the important asymmetry that these tests pin:

* `CreateDeclarationId(extensionBlock)` uses **grouping name + `.` + marker name**.
* `CreateDeclarationId(extensionMember)` uses **grouping name only** — no marker segment.
* `CreateReferenceId(extensionMember)` returns **`""`** (empty string), not `null`, for an extension member.
  That is a quirk worth knowing when persisting identities.

#### Round-trip machinery in the parser

`DocumentationCommentId.cs:1191-1265`:

```csharp
/// <param name="isTerminal">Indicates that we're looking at the last segment in a dotted chain.
/// If we're in terminal position, we need to recognize the extension marker name so that
/// `ContainingType.ExtensionGroupingName.ExtensionMarkerName` can be matched to the extension type.
/// </param>
private static void GetMatchingTypes(INamespaceOrTypeSymbol container, string memberName, int arity, bool isTerminal, List<ISymbol> results)
{
    if (isTerminal
        && container is INamedTypeSymbol { IsExtension: true } extension
        && extension.ExtensionMarkerName == memberName
        && arity == 0)
    {
        results.Add(extension);
        return;
    }
    ...
    GetMatchingExtensions(container, memberName, arity, results);
}

private static void GetMatchingExtensions(INamespaceOrTypeSymbol container, string memberName, int arity, List<ISymbol> results)
{
    if (container.IsNamespace) return;

    ImmutableArray<INamedTypeSymbol> unnamedNamedTypes = container.GetTypeMembers("");
    foreach (var namedType in unnamedNamedTypes)
    {
        if (namedType.IsExtension && namedType.Arity == arity && namedType.ExtensionGroupingName == memberName)
            results.Add(namedType);
    }
}
```

An extension block appears in the source symbol table as a nested type with the **empty name**; the parser finds
it by matching `ExtensionGroupingName`, then recognises the marker name in terminal position. This is the only
special-casing in the parser, and it is what makes the round-trip work.

### 2.4 Extension indexers

An extension indexer is a `PropertySymbol` with `Name == "this[]"` inside the extension block type.

**C# generator** (`DocumentationCommentIDVisitor.PartVisitor.cs:120-133`):

```csharp
public override object VisitProperty(PropertySymbol symbol, StringBuilder builder)
{
    Visit(symbol.ContainingType, builder);              // -> "E.<G>$HASH`1"
    builder.Append('.');
    builder.Append(GetEscapedMetadataName(symbol));     // -> "Item" (or the IndexerName value)
    if (symbol.Parameters.Any())
        s_parameterOrReturnTypeInstance.VisitParameters(symbol.Parameters, false, builder);
    return null;
}
```

**Core generator** (`DocumentationCommentId.cs:452-465`):

```csharp
public override bool VisitProperty(IPropertySymbol symbol)
{
    if (this.Visit(symbol.ContainingSymbol)) _builder.Append('.');
    var name = EncodePropertyName(symbol.Name);        // "this[]" -> "Item"
    _builder.Append(EncodeName(name));
    AppendParameters(symbol.Parameters);
    return true;
}
```

with

```csharp
private static string EncodePropertyName(string name)
{
    // convert C# indexer names to 'Item'
    if (name == "this[]") name = "Item";
    else if (name.EndsWith(".this[]")) name = name.Substring(0, name.Length - 6) + "Item";
    return name;
}
```

**Pinned test — `ExtensionIndexersTests.XmlDoc_01`**
(`src/Compilers/CSharp/Test/CSharp15/ExtensionIndexersTests.cs:15285`). Source:

```csharp
static class E
{
    /// <summary>Summary for extension block</summary>
    /// <typeparam name="T">Description for T</typeparam>
    /// <param name="t">Description for t</param>
    extension<T>(T t)
    {
        /// <summary>Summary for indexer with references to <typeparamref name="T"/> and <paramref name="t"/> and <paramref name="s"/>.</summary>
        /// <param name="s">Description for s</param>
        public int this[string s] { get => throw null; set => throw null; }
    }
}
```

Emitted XML documentation file (assembly name `assembly`):

```xml
<?xml version="1.0"?>
<doc>
    <assembly>
        <name>assembly</name>
    </assembly>
    <members>
        <member name="M:E.get_Item``1(``0,System.String)">
            <inheritdoc cref="P:E.&lt;G&gt;$8048A6C8BE30A622530249B904B537EB`1.Item(System.String)"/>
        </member>
        <member name="M:E.set_Item``1(``0,System.String,System.Int32)">
            <inheritdoc cref="P:E.&lt;G&gt;$8048A6C8BE30A622530249B904B537EB`1.Item(System.String)"/>
        </member>
        <member name="T:E.&lt;G&gt;$8048A6C8BE30A622530249B904B537EB`1.&lt;M&gt;$D1693D81A12E8DED4ED68FE22D9E856F">
            <summary>Summary for extension block</summary>
            <typeparam name="T">Description for T</typeparam>
            <param name="t">Description for t</param>
        </member>
        <member name="P:E.&lt;G&gt;$8048A6C8BE30A622530249B904B537EB`1.Item(System.String)">
            <summary>Summary for indexer with references to <typeparamref name="T"/> and <paramref name="t"/> and <paramref name="s"/>.</summary>
            <param name="s">Description for s</param>
        </member>
    </members>
</doc>
```

Unescaped, the identifiers are:

| Symbol | Documentation comment ID |
|---|---|
| the extension indexer (skeleton property in the grouping type) | ``P:E.<G>$8048A6C8BE30A622530249B904B537EB`1.Item(System.String)`` |
| the extension block | ``T:E.<G>$8048A6C8BE30A622530249B904B537EB`1.<M>$D1693D81A12E8DED4ED68FE22D9E856F`` |
| the `get` implementation method on `E` | ``M:E.get_Item``1(``0,System.String)`` |
| the `set` implementation method on `E` | ``M:E.set_Item``1(``0,System.String,System.Int32)`` |

Observations:

* The indexer's ID uses **`Item`**, the metadata name — the `this[]` spelling never appears in an ID.
* The indexer's parameter list holds only the **indexer's own** parameters. The receiver is not there.
* The **implementation methods** prepend the receiver (`` ``0 ``, the extension block's type parameter
  reprojected as a *method* type parameter, hence the double backtick) and, for the setter, append the
  `value` parameter last.
* No `<member>` element is emitted for the skeleton indexer's `get`/`set` accessors inside the grouping type;
  only the property gets one.
* The extension block's own documentation lands on the **marker** type ID, while the members land under the
  **grouping** type ID.

`ExtensionIndexersTests.Cref_03` (assertions at lines 15205-15228) shows that `[IndexerName("MyIndexer")]`
changes the accessor names to `get_MyIndexer`/`set_MyIndexer` and therefore the implementation-method IDs;
by the same code path it changes the property's metadata name and therefore the `P:` ID to `…​.MyIndexer(System.String)`.

### 2.5 A pre-existing divergence between the two generators, now reaching extension members

The two generators escape differently:

| | `<` | `>` | `.` | `::` |
|---|---|---|---|---|
| C# `PartVisitor.GetEscapedMetadataName` (members only) | `{` | `}` | `#` | prefix stripped |
| Core `DocumentationCommentId.EncodeName` | *unchanged* | *unchanged* | `#` | unchanged |

`GetEscapedMetadataName` (`DocumentationCommentIDVisitor.PartVisitor.cs:268-289`):

```csharp
private static string GetEscapedMetadataName(Symbol symbol)
{
    string metadataName = symbol.MetadataName;
    if (metadataName.IndexOfAny(s_escapedMetadataNameChars) == -1)   // [':', '.', '<', '>']
        return metadataName;

    int colonColonIndex = metadataName.IndexOf("::", StringComparison.Ordinal);
    int startIndex = colonColonIndex < 0 ? 0 : colonColonIndex + 2;

    PooledStringBuilder pooled = PooledStringBuilder.GetInstance();
    pooled.Builder.Append(metadataName, startIndex, metadataName.Length - startIndex);
    pooled.Builder.Replace('.', '#').Replace('<', '{').Replace('>', '}');
    return pooled.ToStringAndFree();
}
```

This applies only to **members** (methods, properties, events). Type names — including
`ExtensionGroupingName` and `ExtensionMarkerName` — bypass it in **both** generators, so `<G>$`/`<M>$` keep
their raw angle brackets in both. The divergence therefore does **not** affect the extension type IDs.

It *does* affect a compiler-generated member such as the union `Value` backing field:

* C# generator: `F:N.Pet.{Value}k__BackingField`
* core generator: `F:N.Pet.<Value>k__BackingField`

This is pre-existing behaviour, unchanged by C# 15, and only reachable for symbols that are not normally
addressed by documentation comment IDs.

A second, more consequential divergence for indexers: the core generator's `EncodePropertyName` maps
`"this[]"` to `"Item"` **unconditionally**, while the C# generator uses `MetadataName` and therefore honours
`[IndexerName]`. For an extension indexer declared with `[IndexerName("MyIndexer")]`:

* `ISymbol.GetDocumentationCommentId()` → ``P:E.<G>$HASH.MyIndexer(System.String)``
* `DocumentationCommentId.CreateDeclarationId(symbol)` → ``P:E.<G>$HASH.Item(System.String)``

These do not agree, and only the first matches the identifier the compiler writes to the XML file.
This is pre-existing for ordinary indexers; extension indexers inherit it.

### 2.6 Test coverage gaps

| Test file | `DocumentationCommentId` / `GetDocumentationCommentId` assertions |
|---|---|
| `src/Compilers/Core/CodeAnalysisTest/Symbols/DocumentationCommentIdTests.cs` (192 lines) | none for extensions, unions or closed classes (tuples, `dynamic`, `nint`, invalid type-parameter indices only) |
| `src/Compilers/CSharp/Test/Symbol/DocumentationComments/DocumentationCommentIDTests.cs` (440 lines) | none |
| `src/Workspaces/CoreTest/UtilityTest/DocumentationCommentIdTests.cs` (354 lines) | none |
| `src/Compilers/CSharp/Test/Emit3/Semantics/ExtensionTests2.cs` | `DocumentationCommentId_01/_02/_03` — extension blocks and extension **methods** only |
| `src/Compilers/CSharp/Test/CSharp15/ExtensionIndexersTests.cs` | **zero** occurrences of `DocumentationCommentId` — only the emitted-XML tests above |
| `src/Compilers/CSharp/Test/CSharp15/UnionsTests.cs` | **zero** |
| `src/Compilers/CSharp/Test/CSharp15/ClosedClassesTests.cs` | **zero** |

So `DocumentationCommentId.CreateDeclarationId` → `GetFirstSymbolForDeclarationId` round-tripping is
**proved by test** for an extension block and an extension method, and is **untested** for an extension
indexer, an extension property, a union, a union's synthesized members, and a closed class. For the union and
closed cases the untested path is the ordinary type/member path, so the risk is low; for the extension indexer,
the untested path goes through `EncodePropertyName` and the `GetMatchingExtensions` lookup, so the risk is real.

---

## 3. CREF grammar

### 3.1 Grammar

`proposals/csharp-14.0/extensions.md`, "### CREF references":

```antlr
member_cref
  : conversion_operator_member_cref
  | extension_member_cref // added
  | indexer_member_cref
  | name_member_cref
  | operator_member_cref
  ;

extension_member_cref // added
 : 'extension' type_argument_list? cref_parameter_list '.' member_cref
 ;

qualified_cref
  : type '.' member_cref
  ;

cref
  : member_cref
  | qualified_cref
  | type_cref
  ;
```

Constraints from the same speclet:

* *"a cref cannot address an extension block itself. `E.extension(int)` could refer to a method named
  "extension" in type `E`."*
* *"It is an error to use `extension_member_cref` at top-level (`extension(int).M`) or nested in another
  extension (`E.extension(int).extension(string).M`)."*
* *"As we disallow unqualified references to extension members, cref would also disallow them."*

The C# 15 extension-indexer proposal (`proposals/csharp-15.0/extension-indexers.md`) simply lets
`indexer_member_cref` appear as the `member_cref` of an `extension_member_cref`.

### 3.2 Binder implementation

`src/Compilers/CSharp/Portable/Binder/Binder_Crefs.cs:224-270`:

```csharp
private ImmutableArray<Symbol> BindExtensionMemberCref(ExtensionMemberCrefSyntax syntax, ...)
{
    ...
    if (syntax.Member is NameMemberCrefSyntax { Name: SimpleNameSyntax simpleName } nameMember)
    {
        CheckFeatureAvailability(syntax, MessageID.IDS_FeatureExtensions, diagnostics);
        arity = simpleName.Arity;
        typeArgumentListSyntax = simpleName is GenericNameSyntax genericName ? genericName.TypeArgumentList : null;
        parameters = nameMember.Parameters;
        memberName = simpleName.Identifier.ValueText;
    }
    else if (syntax.Member is OperatorMemberCrefSyntax operatorSyntax)
    {
        CheckFeatureAvailability(syntax, MessageID.IDS_FeatureExtensions, diagnostics);
        memberName = GetOperatorMethodName(operatorSyntax);
        parameters = operatorSyntax.Parameters;
    }
    else if (syntax.Member is IndexerMemberCrefSyntax indexerSyntax)                  // C# 15
    {
        CheckFeatureAvailability(syntax, MessageID.IDS_FeatureExtensionIndexers, diagnostics);
        memberName = WellKnownMemberNames.Indexer;                                   // "this[]"
        parameters = indexerSyntax.Parameters;
    }
    ...
    Debug.Assert(sortedSymbols.All(s => s.IsExtensionBlockMember()));
    return ProcessCrefMemberLookupResults(sortedSymbols, arity, syntax, typeArgumentListSyntax, parameters, out ambiguityWinner, diagnostics);
}
```

`SyntaxKind.ExtensionMemberCref` and `ExtensionMemberCrefSyntax` shipped with C# 14; the only C# 15 addition is
the `IndexerMemberCrefSyntax` branch, gated on `MessageID.IDS_FeatureExtensionIndexers`.

### 3.3 The definitive extension-indexer CREF test

`src/Compilers/CSharp/Test/CSharp15/ExtensionIndexersTests.cs`, `Cref_01` (line 15031). Source:

```csharp
/// <see cref="E.extension(int).this[string]"/>
/// <see cref="E.extension(int).get_Item(string)"/>
/// <see cref="E.extension(int).get_Item"/>
/// <see cref="E.extension(int).set_Item(string, int)"/>
/// <see cref="E.extension(int).set_Item"/>
/// <see cref="E.get_Item(int, string)"/>
/// <see cref="E.get_Item"/>
/// <see cref="E.set_Item(int, string, int)"/>
/// <see cref="E.set_Item"/>
/// <see cref="E.extension(int).this[]"/>
/// <see cref="E.extension(int).Item(string)"/>
public static class E
{
    extension(int i)
    {
        /// <summary></summary>
        public int this[string s] { get => throw null; set => throw null; }
    }
}
```

Resolution (`PrintXmlCrefSymbols`):

```
(E.extension(int).this[string],          System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s] { get; set; })
(E.extension(int).get_Item(string),      System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s].get)
(E.extension(int).get_Item,              System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s].get)
(E.extension(int).set_Item(string, int), void         E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s].set)
(E.extension(int).set_Item,              void         E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.this[System.String s].set)
(E.get_Item(int, string),                System.Int32 E.get_Item(System.Int32 i, System.String s))
(E.get_Item,                             System.Int32 E.get_Item(System.Int32 i, System.String s))
(E.set_Item(int, string, int),           void         E.set_Item(System.Int32 i, System.String s, System.Int32 value))
(E.set_Item,                             void         E.set_Item(System.Int32 i, System.String s, System.Int32 value))
(E.extension(int).this[],                null)                                  // CS1574
(E.extension(int).Item(string),          null)                                  // CS1574
```

Diagnostics:

```
// (10,16): warning CS1574: XML comment has cref attribute 'extension(int).this[]' that could not be resolved
// (11,16): warning CS1574: XML comment has cref attribute 'extension(int).Item(string)' that could not be resolved
```

Key facts:

* `E.extension(int).this[string]` → the **skeleton indexer property** in the grouping type.
* `E.extension(int).get_Item(string)` (and the parameterless form) → the **accessor of the skeleton indexer**,
  not the implementation method.
* `E.get_Item(int, string)` → the **implementation method** on the static class, with the receiver prepended.
* `E.set_Item(int, string, int)` → the setter implementation method, receiver first and `value` last.
* `E.extension(int).this[]` (empty bracket list) does **not** resolve.
* `E.extension(int).Item(string)` — using the metadata name — does **not** resolve. Only the `this[...]`
  spelling addresses the indexer itself.

Generic form, `ExtensionIndexersTests.Cref_04` (line ~15240), for `extension<T>(int) { public int this[T t] … }`:

```
(E.extension{U}(int).this[U],          System.Int32 E.<G>$B8D310208B4544F25EEBACB9990FC73B<U>.this[U t] { get; set; })
(E.extension{U}(int).get_Item(U),      System.Int32 E.<G>$B8D310208B4544F25EEBACB9990FC73B<U>.this[U t].get)
(E.extension{U}(int).set_Item(U, int), void         E.<G>$B8D310208B4544F25EEBACB9990FC73B<U>.this[U t].set)
(E.get_Item{U}(int, U),                System.Int32 E.get_Item<U>(System.Int32 i, U t))
(E.set_Item{U}(int, U, int),           void         E.set_Item<U>(System.Int32 i, U t, System.Int32 value))
```

The `{U}` brace form is the standard CREF spelling of a type-argument list.

`[IndexerName]` variant, `ExtensionIndexersTests.Cref_03` (lines 15195-15228): with
`[System.Runtime.CompilerServices.IndexerName("MyIndexer")]` the accessor crefs become
`E.extension(int).get_MyIndexer(string)` / `set_MyIndexer(string, int)` and the implementation crefs
`E.get_MyIndexer(int, string)` / `E.set_MyIndexer(int, string, int)`, while `E.extension(int).this[string]`
continues to work unchanged.

### 3.4 Extension member crefs generally

`ExtensionTests2.Cref_01` (line 6483) shows how a cref is *serialised* into the XML file:

```csharp
/// <see cref="E.extension(int).M(string)"/>
/// <see cref="E.M(int, string)"/>
/// <see cref="E.extension(int).M"/>
/// <see cref="E.M"/>
static class E
{
    extension(int i)
    {
        /// <see cref="M(int, string)"/>
        /// <see cref="M(string)"/>       // CS1574: cannot be resolved unqualified
        /// <see cref="M"/>
        public void M(string s) => throw null!;
    }
}
```

`e.GetDocumentationCommentXml()`:

```xml
<member name="T:E">
    <see cref="M:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M(System.String)"/>
    <see cref="M:E.M(System.Int32,System.String)"/>
    <see cref="M:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M(System.String)"/>
    <see cref="M:E.M(System.Int32,System.String)"/>
</member>
```

and for the skeleton method:

```xml
<member name="M:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M(System.String)">
    <see cref="M:E.M(System.Int32,System.String)"/>
    <see cref="!:M(string)"/>
    <see cref="M:E.M(System.Int32,System.String)"/>
</member>
```

There are **70** `Cref_NN` tests in `ExtensionTests2.cs` (lines 6483-8664). None of them involves an indexer;
the indexer cases live entirely in `ExtensionIndexersTests.cs`.

### 3.5 Can a cref address a union or a union case type?

Yes, and with no new syntax. A union declaration produces an ordinary named type, so:

* `cref="Pet"` → `T:N.Pet`
* `cref="Pet.Value"` → `P:N.Pet.Value`
* `cref="Pet.Pet(Cat)"` → `M:N.Pet.#ctor(N.Cat)`
* `cref="Option{T}"` → `` T:N.Option`1 ``

Union case types are ordinary independent types (csharplang `unions.md`: *"The proposed unions in C# are unions
of *types* and not 'discriminated' or 'tagged'."*) and are addressed as themselves; there is no
`Pet.Cat`-style nested case name and no CREF form for "the `Cat` case of `Pet`".

There is **no new cref production for unions and none for closed classes**. `SyntaxKind.UnionKeyword` and
`SyntaxKind.ClosedKeyword` appear nowhere in `Binder_Crefs.cs`.

---

## 4. The Workspaces `SyntaxGenerator`

### 4.1 `SyntaxGenerator.Declaration(ISymbol)` — complete current dispatch

`src/Workspaces/Core/Portable/Editing/SyntaxGenerator.cs:726-857`:

```csharp
public SyntaxNode Declaration(ISymbol symbol)
{
    switch (symbol.Kind)
    {
        case SymbolKind.Field:    return FieldDeclaration((IFieldSymbol)symbol);
        case SymbolKind.Property:
            var property = (IPropertySymbol)symbol;
            return property.IsIndexer ? IndexerDeclaration(property) : PropertyDeclaration(property);
        case SymbolKind.Event:    return EventDeclaration((IEventSymbol)symbol);
        case SymbolKind.Method:
            var method = (IMethodSymbol)symbol;
            switch (method.MethodKind)
            {
                case MethodKind.Constructor:
                case MethodKind.SharedConstructor:                    return ConstructorDeclaration(method);
                case MethodKind.Destructor:                           return DestructorDeclaration(method);
                case MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation:
                                                                      return MethodDeclaration(method);
                case MethodKind.UserDefinedOperator or MethodKind.Conversion:
                                                                      return OperatorDeclaration(method);
            }
            break;
        case SymbolKind.Parameter: return ParameterDeclaration((IParameterSymbol)symbol);
        case SymbolKind.NamedType:
            var type = (INamedTypeSymbol)symbol;
            var declaration = type.TypeKind switch
            {
                TypeKind.Class => ClassDeclaration(
                    type.IsRecord, type.Name, type.TypeParameters.Select(TypeParameter),
                    accessibility: type.DeclaredAccessibility,
                    modifiers: DeclarationModifiers.From(type),
                    baseType: type.BaseType != null ? TypeExpression(type.BaseType) : null,
                    interfaceTypes: type.Interfaces.Select(TypeExpression),
                    members: GetMembersExceptExtensionImplementations(type).Where(CanBeDeclared).Select(Declaration)),
                TypeKind.Struct => StructDeclaration(
                    type.IsRecord, type.Name, type.TypeParameters.Select(TypeParameter),
                    accessibility: type.DeclaredAccessibility,
                    modifiers: DeclarationModifiers.From(type),
                    interfaceTypes: type.Interfaces.Select(TypeExpression),
                    members: type.GetMembers().SelectAsArray(CanBeDeclared, Declaration)),
                TypeKind.Interface => InterfaceDeclaration(...),
                TypeKind.Enum => EnumDeclaration(...),
                TypeKind.Delegate => ... DelegateDeclaration(...) : null,
                TypeKind.Extension when type.ExtensionParameter is { } extensionParameter => ExtensionBlockDeclaration(
                    ParameterDeclaration(extensionParameter),
                    typeParameters: type.TypeParameters.Select(TypeParameter),
                    members: type.GetMembers().Where(CanBeDeclared).Select(Declaration)),
                _ => null,
            };

            if (declaration != null)
                return WithTypeParametersAndConstraints(declaration, type.TypeParameters);
            break;
    }

    throw new ArgumentException("Symbol cannot be converted to a declaration");
}
```

### 4.2 What it does for each construct

#### Union

A union is `TypeKind.Struct` with `IsRecord == false`. It therefore takes the
`TypeKind.Struct => StructDeclaration(isRecord: false, …)` branch and produces an ordinary **`struct`
declaration**. There is **no `UnionDeclaration` factory** anywhere on `SyntaxGenerator` (grep for
`Union` across `SyntaxGenerator.cs` returns zero hits), and none on `CSharpSyntaxGenerator`.

What the regenerated declaration looks like, and what it loses:

* the keyword becomes `struct`, not `union`;
* the case-type list `(Cat, Dog)` is gone; instead the synthesized constructors are regenerated individually,
  because `CanBeDeclared` (see 4.3) only skips implicitly declared members when
  `symbol.ContainingType?.IsRecord is true`, and a union is not a record;
* the synthesized `Value` property is regenerated as an ordinary auto-property;
* `interfaceTypes: type.Interfaces.Select(TypeExpression)` re-emits the implicit
  `System.Runtime.CompilerServices.IUnion` implementation;
* `SyntaxGenerator.Declaration` never emits attributes, so the `[Union]` attribute is **lost**.

The result is a plain struct that is no longer a union type. There is no exception and no diagnostic; the
union-ness is silently dropped.

#### Closed class

A closed class is `TypeKind.Class`, so it takes `ClassDeclaration(type.IsRecord, …,
modifiers: DeclarationModifiers.From(type), …)`.

`DeclarationModifiers.From` (`src/Workspaces/Core/Portable/Editing/DeclarationModifiers.cs:63-99`):

```csharp
var type = symbol as INamedTypeSymbol;
var isClosed = type?.IsClosed == true;

return new DeclarationModifiers(
    isStatic: symbol.IsStatic && !isConst,
    isAbstract: symbol.IsAbstract && !isClosed,   // <-- 'closed' implies abstract; suppress the redundant word
    ...
    isClosed: isClosed);
```

`CSharpSyntaxGenerator.AsModifierList` (`src/Workspaces/CSharp/Portable/CodeGeneration/CSharpSyntaxGenerator.cs:1697-1717`):

```csharp
AddIf(modifiers.IsFile, FileKeyword);
AddIf(modifiers.IsAbstract, AbstractKeyword);
AddIf(modifiers.IsNew, NewKeyword);
AddIf(modifiers.IsSealed, SealedKeyword);
AddIf(modifiers.IsOverride, OverrideKeyword);
AddIf(modifiers.IsVirtual, VirtualKeyword);
AddIf(modifiers.IsStatic, StaticKeyword);
AddIf(modifiers.IsAsync, AsyncKeyword);
AddIf(modifiers.IsConst, ConstKeyword);
AddIf(modifiers.IsReadOnly, ReadOnlyKeyword);
AddIf(modifiers.IsUnsafe, UnsafeKeyword);
AddIf(modifiers.IsVolatile, VolatileKeyword);
AddIf(modifiers.IsExtern, ExternKeyword);
AddIf(modifiers.IsRequired, RequiredKeyword);
AddIf(modifiers.IsFixed, FixedKeyword);
AddIf(modifiers.IsClosed, ClosedKeyword);          // <-- new

// partial and ref must be last
AddIf(modifiers.IsRef, RefKeyword);
AddIf(modifiers.IsPartial, PartialKeyword);
```

and `WithModifiers` (same file, 1641-1645):

```csharp
// 'closed' implies abstract on classes and 'closed abstract' can't be explicitly combined.
if (modifiers.IsClosed && modifiers.IsAbstract)
    modifiers = modifiers.WithIsAbstract(false);
```

The permitted-modifier filter, `GetAllowedModifiers` (line 1539) → `s_classModifiers` (line 1499):

```csharp
private static readonly DeclarationModifiers s_classModifiers =
    DeclarationModifiers.Abstract | DeclarationModifiers.New | DeclarationModifiers.Partial |
    DeclarationModifiers.Sealed | DeclarationModifiers.Static | DeclarationModifiers.Unsafe |
    DeclarationModifiers.File | DeclarationModifiers.Closed;          // <-- Closed allowed on class

private static readonly DeclarationModifiers s_recordModifiers =
    DeclarationModifiers.Abstract | DeclarationModifiers.New | DeclarationModifiers.Partial |
    DeclarationModifiers.Sealed | DeclarationModifiers.Unsafe | DeclarationModifiers.File;
    // NOTE: no Closed

private static readonly DeclarationModifiers s_structModifiers =
    DeclarationModifiers.New | DeclarationModifiers.Partial | DeclarationModifiers.ReadOnly |
    DeclarationModifiers.Ref | DeclarationModifiers.Unsafe | DeclarationModifiers.File;

private static readonly DeclarationModifiers s_interfaceModifiers =
    DeclarationModifiers.New | DeclarationModifiers.Partial | DeclarationModifiers.Unsafe | DeclarationModifiers.File;
```

So `SyntaxGenerator.Declaration(closedClassSymbol)` emits **`public closed class C`**, correctly.

**But there is a gap.** `ClassDeclaration(isRecord: true, …)` sets `kind = SyntaxKind.RecordDeclaration`
(`CSharpSyntaxGenerator.cs:712`), so the allowed set becomes `s_recordModifiers`, which does **not** contain
`DeclarationModifiers.Closed`. The closed-hierarchies speclet's own opening example is

```csharp
// Assembly 1
public closed record class GateState;
public record class Closed : GateState;
public record class Open(float Percent) : GateState;
```

so `closed record class` is valid C# 15. For such a symbol,
`SyntaxGenerator.Declaration` produces `public record GateState` and **silently drops the `closed` modifier**.
The `Closed` flag is also unavailable on struct and interface declarations, which matters if the feature is
later widened (the speclet notes interfaces as a possible future extension).

#### Extension block

`TypeKind.Extension when type.ExtensionParameter is { } extensionParameter => ExtensionBlockDeclaration(...)`.

`ExtensionBlockDeclaration` is declared **`internal abstract`** on `SyntaxGenerator`
(`SyntaxGenerator.cs:687-693`):

```csharp
/// <summary>
/// Creates an extension block declaration
/// </summary>
internal abstract SyntaxNode ExtensionBlockDeclaration(
    SyntaxNode extensionParameter,
    IEnumerable<SyntaxNode>? typeParameters,
    IEnumerable<SyntaxNode> members);
```

It is therefore **not reachable from outside `Microsoft.CodeAnalysis.Workspaces`** except through
`Declaration(ISymbol)`. The C# override (`CSharpSyntaxGenerator.cs:3741-3752`):

```csharp
internal override SyntaxNode ExtensionBlockDeclaration(
    SyntaxNode extensionParameter, IEnumerable<SyntaxNode>? typeParameters, IEnumerable<SyntaxNode> members)
{
    SyntaxList<MemberDeclarationSyntax> extensionMembers = [.. members.OfType<MemberDeclarationSyntax>().WhereNotNull()];
    var typeParameterList = AsTypeParameterList(typeParameters);

    return SyntaxFactory.ExtensionBlockDeclaration(attributeLists: default, modifiers: default, ExtensionKeyword,
        typeParameterList, parameterList: AsParameterList([extensionParameter]),
        constraintClauses: default, OpenBraceToken, extensionMembers, CloseBraceToken, default);
}
```

If `ExtensionParameter` is `null`, the `when` clause fails, `declaration` stays `null`, and
`Declaration(ISymbol)` **throws `ArgumentException("Symbol cannot be converted to a declaration")`**.
This is pinned by `SyntaxGeneratorTests.TestExtensionDeclaration_04`:

```csharp
[Fact]
public void TestExtensionDeclaration_04()
{
    // null extension parameter
    var compilation = Compile("""
        public static class E
        {
            extension(__arglist)
            {
            }
        }
        """);

    var symbol = compilation.GlobalNamespace.GetTypeMembers("E").Single();
    Assert.Throws<ArgumentException>(() => Generator.Declaration(symbol));
}
```

Extension implementation methods are hidden from the containing class's regenerated member list by
`GetMembersExceptExtensionImplementations` (`SyntaxGenerator.cs:823-857`), which walks
`type.GetTypeMembers("")` looking for `IsExtension` nested types and collects
`IMethodSymbol.AssociatedExtensionImplementation` to suppress.

#### Extension indexer

`SymbolKind.Property` with `IsIndexer == true` → `IndexerDeclaration(property)`. This is the ordinary indexer
path; it works, and produces `public int this[string s] { get; set; }` inside the regenerated extension block.
`s_indexerModifiers` (`CSharpSyntaxGenerator.cs:1488-1497`) is unchanged:
`Abstract | Extern | New | Override | ReadOnly | Sealed | Static | Virtual | Unsafe`.

There is **no** extension-indexer-specific factory and no extension-indexer test in
`src/Workspaces/CSharpTest/CodeGeneration/SyntaxGeneratorTests.cs` — grep for `this[` and
`IndexerDeclaration` in the `TestExtensionDeclaration_01..12` range (lines 5353-5785) returns nothing.

### 4.3 `CanBeDeclared` — why union synthesized members are regenerated

`SyntaxGenerator.cs:861-910`:

```csharp
private static bool CanBeDeclared(ISymbol symbol)
{
    // Skip implicitly declared members from a record.  No need to synthesize those as the compiler will do it
    // anyways.
    if (symbol.ContainingType?.IsRecord is true)
    {
        if (symbol.IsImplicitlyDeclared)
            return false;
    }

    switch (symbol.Kind)
    {
        case SymbolKind.Field:
        case SymbolKind.Property:
        case SymbolKind.Event:
        case SymbolKind.Parameter:
            return symbol.CanBeReferencedByName;

        case SymbolKind.Method:
            var method = (IMethodSymbol)symbol;
            switch (method.MethodKind)
            {
                case MethodKind.Constructor:
                case MethodKind.SharedConstructor:
                case MethodKind.UserDefinedOperator:
                    return true;
                case MethodKind.Ordinary:
                    return method.CanBeReferencedByName;
            }
            break;

        case SymbolKind.NamedType:
            var type = (INamedTypeSymbol)symbol;
            switch (type.TypeKind)
            {
                case TypeKind.Class:
                case TypeKind.Struct:
                case TypeKind.Interface:
                case TypeKind.Enum:
                case TypeKind.Delegate:
                    return type.CanBeReferencedByName;
                case TypeKind.Extension:
                    return true;
            }
            break;
    }

    return false;
}
```

The implicit-member skip is keyed on `IsRecord`, and a union declaration is deliberately **not** a record.
Therefore for a union struct:

* `SynthesizedUnionCtor` — `MethodKind.Constructor` → `return true` → **regenerated**;
* `SynthesizedUnionValuePropertySymbol` — `SymbolKind.Property`, `CanBeReferencedByName == true` →
  **regenerated**;
* `<Value>k__BackingField` — `CanBeReferencedByName == false` → skipped;
* `get_Value` — `MethodKind.PropertyGet` falls through the inner switch to `break`, then `return false` → skipped.

### 4.4 The complete current `DeclarationModifiers` member list

`src/Workspaces/Core/Portable/Editing/DeclarationModifiers.cs`. `public readonly record struct`.

**Predicates**: `IsStatic`, `IsAbstract`, `IsNew`, `IsUnsafe`, `IsReadOnly`, `IsVirtual`, `IsOverride`,
`IsSealed`, `IsConst`, `IsWithEvents`, `IsPartial`, `IsAsync`, `IsWriteOnly`, `IsRef`, `IsVolatile`,
`IsExtern`, `IsRequired`, `IsFile`, **`IsClosed`**; plus `internal bool IsFixed`.

**Withers**: `WithIsStatic`, `WithIsAbstract`, `WithIsNew`, `WithIsUnsafe`, `WithIsReadOnly`, `WithIsVirtual`,
`WithIsOverride`, `WithIsSealed`, `WithIsConst`, `WithWithEvents`, `WithPartial`, `WithAsync`,
`WithIsWriteOnly`, `WithIsRef`, `WithIsVolatile`, `WithIsExtern`, `WithIsRequired`, `WithIsFile`,
**`WithIsClosed`**.

**Static factories**: `None`, `Static`, `Abstract`, `New`, `Unsafe`, `ReadOnly`, `Virtual`, `Override`,
`Sealed`, `Const`, `WithEvents`, `Partial`, `Async`, `WriteOnly`, `Ref`, `Volatile`, `Extern`, `Required`,
`File`, **`Closed`**; plus `internal static DeclarationModifiers Fixed`.

**Other members**: `From(ISymbol)`, `operator |`, `operator &`, `operator +`, `operator -`,
`ToString()`, `TryParse(string, out DeclarationModifiers)`.

**There is no `Union` member and no `Safe` member.** The `safe` keyword
(`[RSEXPERIMENTAL006]Microsoft.CodeAnalysis.CSharp.SyntaxKind.SafeKeyword = 8454`) belongs to the separate
"Unsafe evolution" feature, which is merged as a preview feature into .NET 11 preview 2 / VS 18.6 and is
guarded by the `RSEXPERIMENTAL006` diagnostic; it is not represented in `DeclarationModifiers` at all.
`TryParse` delegates to `Enum.TryParse` over the internal `Modifiers` enum, so `"Closed"` parses and
`"Union"`/`"Safe"` do not.

### 4.5 SyntaxGenerator tests

`src/Workspaces/CSharpTest/CodeGeneration/SyntaxGeneratorTests.cs` (5 785 lines):

| Test | Line | What it pins |
|---|---|---|
| `TestAddAbstractToClosedClass` | 3317 | `WithIsAbstract(true)` on `closed class C` leaves `closed class C` (abstract suppressed) |
| `TestAddPublicToClosedClass` | 3330 | `WithAccessibility(Public)` on `closed class C` → `public closed class C` |
| `TestAddClosedModifierToAbstractClass` | 3342 | `WithIsClosed(true)` on `abstract class C` → `closed class C` |
| `TestAddClosedModifierToPublicClass` | 3355 | `WithIsClosed(true)` on `public class C` → `public closed class C` |
| `TestClassModifiers2` | 5162, `WorkItem` roslyn#65834 | `DeclarationModifiers.From` on `public closed class [\|C\|]` → `DeclarationModifiers.Closed` |
| `TestExtensionDeclaration_01` | 5353 | `extension(int i) { public void M() { } }` round-trips through `Declaration(ISymbol)` |
| `TestExtensionDeclaration_02` | 5383 | unnamed extension parameter `extension(int)` with a static property |
| `TestExtensionDeclaration_03` | 5410 | generic `extension<T>(int)` |
| `TestExtensionDeclaration_04` | 5437 | `extension(__arglist)` (null `ExtensionParameter`) → `Assert.Throws<ArgumentException>` |
| `TestExtensionDeclaration_05` | 5454 | extension operator |
| `TestExtensionDeclaration_06..12` | 5485-5679 | further extension cases |

`grep -i union SyntaxGeneratorTests.cs` → **zero** hits. There is **no SyntaxGenerator test for a union type,
no test for a `closed record class`, and no test for an extension indexer**.

Example of the extension shape produced (`TestExtensionDeclaration_01`):

```csharp
public static class E : global::System.Object
{
    extension(global::System.Int32 i)
    {
        public void M()
        {
        }
    }
}
```

---

## 5. `GetDocumentationCommentXml`, `GetDocumentationCommentId` and the emitted XML file

### 5.1 `ISymbol.GetDocumentationCommentId()`

`src/Compilers/CSharp/Portable/Symbols/Symbol.cs:982-999`:

```csharp
public virtual string? GetDocumentationCommentId()
{
    var pool = PooledStringBuilder.GetInstance();
    try
    {
        StringBuilder builder = pool.Builder;
        DocumentationCommentIDVisitor.Instance.Visit(this, builder);
        return builder.Length == 0 ? null : builder.ToString();
    }
    finally { pool.Free(); }
}
```

The only C# 15-era change to this path is the extension branch already quoted in 2.3. No union or closed
handling was added, and none is needed.

### 5.2 `ISymbol.GetDocumentationCommentXml()`

The base implementation returns `""` (`Symbol.cs:1020-1025`); source symbols override it through
`SourceDocumentationCommentUtils`. The C# 15 changes are all in `DocumentationCommentCompiler`
(`src/Compilers/CSharp/Portable/Compiler/DocumentationCommentCompiler.cs`, 1 526 lines) and are extension-only:

`VisitNamedType` (lines 217-333) merges the doc comments of extension blocks that share a marker type, and
writes them under the **marker** type's identifier:

```csharp
if (symbol.IsExtension && (SourceNamedTypeSymbol)symbol.ContainingType is { } containingType)
{
    // We've been asked to generate the docs for a given extension block. We'll produce the merged docs for the merged blocks.
    ImmutableArray<SourceNamedTypeSymbol> extensions = containingType.GetExtensionGroupingInfo().GetMergedExtensions((SourceNamedTypeSymbol)symbol);
    appendMergedExtensionBlocks(extensions);
}
```

`VisitMethod` (lines 334-372) emits `<inheritdoc>` on the implementation methods:

```csharp
if (symbol is SourceExtensionImplementationMethodSymbol implementation)
{
    MethodSymbol underlyingMethod = implementation.UnderlyingMethod;
    Symbol symbolForDocComment = underlyingMethod.IsAccessor()
        ? underlyingMethod.AssociatedSymbol      // <-- for get_Item/set_Item, the indexer property
        : underlyingMethod;

    if (!hasDocumentationTrivia(symbolForDocComment)) return;

    WriteLine("<member name=\"{0}\">", symbol.GetEscapedDocumentationCommentId());
    Indent();
    WriteLine("<inheritdoc cref=\"{0}\"/>", symbolForDocComment.GetEscapedDocumentationCommentId());
    Unindent();
    WriteLine("</member>");
    return;
}
```

This is exactly what produces the `<inheritdoc cref="P:E.&lt;G&gt;$…​.Item(System.String)"/>` entries in
`ExtensionIndexersTests.XmlDoc_01` above. Note the accessor implementation methods point at the **property**,
not at the accessor.

### 5.3 Which symbols get a `<member>` element

`DocumentationCommentCompiler.ShouldSkip` (lines 462-467):

```csharp
private static bool ShouldSkip(Symbol symbol)
{
    return symbol.IsImplicitlyDeclared ||
        symbol.IsAccessor() ||
        symbol is SynthesizedSimpleProgramEntryPointSymbol;
}
```

Consequences for unions:

* `SynthesizedUnionValuePropertySymbol.IsImplicitlyDeclared == true` → **no `<member name="P:N.Pet.Value">`
  element is emitted**, and no `CS1591` "Missing XML comment for publicly visible type or member" warning.
* `SynthesizedUnionCtor.IsImplicitlyDeclared == true` → same, no `<member name="M:N.Pet.#ctor(N.Cat)">`.
* The union type itself is a source declaration, so it gets `<member name="T:N.Pet">` with whatever
  `///` comment the author wrote on the `union` declaration.

There is **no** union analogue of `DocumentationCommentCompiler.TryProcessRecordPropertyDocumentation`
(line 469), which copies `<param>` tags from a record declaration onto the synthesized positional properties.
So documentation written on the union declaration's case-type list has nowhere to go, and the synthesized
`Value` property and constructors are simply undocumented. A `union Pet(Cat, Dog)` also has no `<param>`
slot for its case types: the case-type list is not a parameter list in the doc-comment sense.

For closed classes there is no change of any kind in `DocumentationCommentCompiler`;
`grep -i "closed"` over the file returns nothing.

### 5.4 The `closed` modifier and code-style modifier ordering

`src/Workspaces/SharedUtilitiesAndExtensions/Compiler/CSharp/CodeStyle/CSharpCodeStyleOptions.cs:158-171`
places `SyntaxKind.ClosedKeyword` in the default `preferred_modifier_order`, immediately after
`AbstractKeyword`:

```csharp
[
    SyntaxKind.PublicKeyword, SyntaxKind.PrivateKeyword, SyntaxKind.ProtectedKeyword,
    SyntaxKind.InternalKeyword, SyntaxKind.FileKeyword, SyntaxKind.StaticKeyword,
    SyntaxKind.ExternKeyword, SyntaxKind.NewKeyword, SyntaxKind.VirtualKeyword,
    SyntaxKind.AbstractKeyword,
#if !OLDER_ROSLYN
    SyntaxKind.ClosedKeyword,
#endif
    ...
]
```

`SyntaxKind.UnionKeyword` does not appear there, because `union` is a type-declaration keyword rather than a
modifier.

---

## 6. Summary of the concrete answers

1. **`ToDisplayString`.** `CSharpErrorMessageFormat`, `FullyQualifiedFormat` and `MinimallyQualifiedFormat`
   none of them set `SymbolDisplayKindOptions.IncludeTypeKeyword`, so none of them prints any type keyword.
   With a format that does set it, a union prints as **`struct Pet`** — the `union` keyword is never emitted,
   and the part kind is `SymbolDisplayPartKind.StructName`. The `closed` modifier is never emitted by any
   format, because `VisitNamedType` emits no type modifiers at all beyond `readonly` and `ref` on structs.
   An extension block prints as `E.extension(int)` and an extension indexer as `E.extension(int).this[int]`.
   **No new `SymbolDisplayPartKind`, `SymbolDisplayKindOptions`, `SymbolDisplayMemberOptions` or
   `SymbolDisplayMiscellaneousOptions` member was added in Roslyn 5.x.** The pinning tests are
   `ExtensionTests.EmptyExtension`, `ExtensionTests.Member_InstanceIndexer` and
   `ExtensionTests2.ReduceExtensionMember_07`; there is **no** SymbolDisplay test for a union or a closed class.

2. **Documentation comment IDs.** A union type is `T:N.Pet`; its synthesized constructors are
   `M:N.Pet.#ctor(N.Cat)`; its `Value` property is `P:N.Pet.Value`; a closed class is `T:N.GateState`. All of
   these use the pre-existing grammar unchanged and round-trip through
   `CreateDeclarationId`/`GetFirstSymbolForDeclarationId` on the ordinary type and member paths, but **no test
   proves it**. An extension block is
   ``T:E.<G>$HASH.<M>$HASH``, an extension member is ``M:E.<G>$HASH.M`` (grouping only, no marker), an
   extension indexer is ``P:E.<G>$HASH`1.Item(System.String)``, and its implementation methods are
   ``M:E.get_Item``1(``0,System.String)`` / ``M:E.set_Item``1(``0,System.String,System.Int32)``. The
   extension-block and extension-method round-trip is proved by `ExtensionTests2.DocumentationCommentId_01/_02/_03`
   (roslyn#78606); the extension-indexer round-trip is not tested. `CreateReferenceId` on an extension member
   returns the empty string.

3. **CREF.** `extension_member_cref : 'extension' type_argument_list? cref_parameter_list '.' member_cref`,
   with `member_cref` now allowed to be an `indexer_member_cref`. `E.extension(int).this[string]` addresses the
   indexer; `E.extension(int).get_Item(string)` addresses its **getter**; `E.get_Item(int, string)` addresses
   the **implementation method** on the static class. `E.extension(int).this[]` and
   `E.extension(int).Item(string)` do **not** resolve (CS1574). A cref cannot address an extension block
   itself. A union type and a union case type are addressed by ordinary type crefs; there is no new cref
   production for unions or closed classes.

4. **SyntaxGenerator.** `Declaration(ISymbol)` on a union produces an ordinary `struct` declaration,
   regenerating the synthesized `Value` property and constructors (because the implicit-member skip is keyed on
   `IsRecord`) and dropping the `[Union]` attribute; there is no `UnionDeclaration` factory. On a closed class
   it produces `closed class C` correctly, but on a **`closed record class` it silently drops `closed`**,
   because `s_recordModifiers` omits `DeclarationModifiers.Closed`. On an extension indexer it takes the
   ordinary `IndexerDeclaration` path and works. On an extension block whose `ExtensionParameter` is `null` it
   throws `ArgumentException("Symbol cannot be converted to a declaration")`
   (`TestExtensionDeclaration_04`). `ExtensionBlockDeclaration` is `internal abstract`. `DeclarationModifiers`
   gained exactly `Closed`, `IsClosed` and `WithIsClosed`; there is **no `Union` and no `Safe` member**.

5. **XML documentation.** `ISymbol.GetDocumentationCommentId` and `GetDocumentationCommentXml` changed only for
   extensions. The union's synthesized `Value` property and constructors are `IsImplicitlyDeclared`, so
   `DocumentationCommentCompiler.ShouldSkip` omits them from the emitted XML file (and from `CS1591`), even
   though `GetDocumentationCommentId()` still returns identifiers for them. There is no union analogue of the
   record `<param>`-copying logic. Nothing changed for closed classes.

---

## 7. Open questions and untested surfaces

1. **Extension-indexer documentation-comment-ID round-trip is unverified.**
   `DocumentationCommentId.CreateDeclarationId` on an extension indexer goes through `EncodePropertyName`
   (`"this[]"` → `"Item"`) and `GetMatchingExtensions`; no test exercises
   `GetFirstSymbolForDeclarationId` on the resulting ``P:E.<G>$HASH.Item(System.String)``.
2. **`[IndexerName]` divergence.** `DocumentationCommentId.CreateDeclarationId` hard-codes `"Item"` while
   `ISymbol.GetDocumentationCommentId()` uses `MetadataName`. For an extension indexer with
   `[IndexerName("MyIndexer")]` the two disagree, and only the second matches the emitted XML file. Is this a
   known Roslyn issue, and is it in scope for GA?
3. **`closed record class` loses its modifier through `SyntaxGenerator`.** `s_recordModifiers` in
   `CSharpSyntaxGenerator` omits `DeclarationModifiers.Closed`, yet the closed-hierarchies speclet's own
   opening example is `public closed record class GateState`. Confirm whether this is a known gap or whether
   `closed record` was disallowed after the speclet was written.
4. **No `EscapeIdentifier` special case for `union`/`closed`.** `record` is special-cased for named types;
   `union` and `closed` are contextual keywords and legal type names but are not special-cased, so
   `EscapeKeywordIdentifiers` will not escape a type named `union`.
5. **Whether the IDE's `ISymbolDisplayService`/QuickInfo formats set `IncludeTypeKeyword`.** They do in
   general, which is where "a union looks like a struct" becomes user-visible; the exact Features-layer format
   was not traced in this pass.
6. **Whether unions and closed classes appear in `SymbolKey`** (the Workspaces symbol-persistence format,
   `src/Workspaces/Core/Portable/Workspace/Solution/SymbolKey*`). Not examined here; adjacent to this gap and
   relevant to any framework that persists symbol identities.
7. **The union `Value` backing field ID divergence** (`F:N.Pet.{Value}k__BackingField` from the C# generator
   versus `F:N.Pet.<Value>k__BackingField` from the core generator) is pre-existing and only reachable for
   compiler-generated fields, but it is a real inconsistency between two APIs that are usually assumed
   interchangeable.
8. **The C# 14 extensions speclet is stale on the grouping/marker naming.** It still shows
   `<>E__MarkerContentName_For_ExtensionOfT`; the implementation uses `<G>$`/`<M>$` plus a 32-hex-character
   XxHash128. Any consumer that hard-codes the speclet's form is wrong.

---

## 8. Sources

Roslyn source (branch `main`, all under `https://github.com/dotnet/roslyn/blob/main/`):

* `docs/Language Feature Status.md`
* `src/Compilers/Core/Portable/PublicAPI.Unshipped.txt`
* `src/Compilers/CSharp/Portable/PublicAPI.Unshipped.txt`
* `src/Workspaces/Core/Portable/PublicAPI.Unshipped.txt`
* `src/Compilers/Core/Portable/SymbolDisplay/SymbolDisplayPartKind.cs`
* `src/Compilers/Core/Portable/SymbolDisplay/SymbolDisplayKindOptions.cs`
* `src/Compilers/Core/Portable/SymbolDisplay/SymbolDisplayMemberOptions.cs`
* `src/Compilers/Core/Portable/SymbolDisplay/SymbolDisplayMiscellaneousOptions.cs`
* `src/Compilers/Core/Portable/SymbolDisplay/SymbolDisplayFormat.cs`
* `src/Compilers/CSharp/Portable/SymbolDisplay/SymbolDisplay.cs`
* `src/Compilers/CSharp/Portable/SymbolDisplay/SymbolDisplayVisitor.cs`
* `src/Compilers/CSharp/Portable/SymbolDisplay/SymbolDisplayVisitor.Types.cs`
* `src/Compilers/CSharp/Portable/SymbolDisplay/SymbolDisplayVisitor.Members.cs`
* `src/Compilers/Core/Portable/DocumentationCommentId.cs`
* `src/Compilers/CSharp/Portable/DocumentationComments/DocumentationCommentIDVisitor.cs`
* `src/Compilers/CSharp/Portable/DocumentationComments/DocumentationCommentIDVisitor.PartVisitor.cs`
* `src/Compilers/CSharp/Portable/Compiler/DocumentationCommentCompiler.cs`
* `src/Compilers/CSharp/Portable/Binder/Binder_Crefs.cs`
* `src/Compilers/CSharp/Portable/Declarations/DeclarationKind.cs`
* `src/Compilers/CSharp/Portable/Symbols/EnumConversions.cs`
* `src/Compilers/CSharp/Portable/Symbols/NamedTypeSymbol.cs`
* `src/Compilers/CSharp/Portable/Symbols/PublicModel/TypeSymbol.cs`
* `src/Compilers/CSharp/Portable/Symbols/Symbol.cs`
* `src/Compilers/CSharp/Portable/Symbols/Source/SourceMemberContainerSymbol.cs`
* `src/Compilers/CSharp/Portable/Symbols/Source/SourceNamedTypeSymbol_Extension.cs`
* `src/Compilers/CSharp/Portable/Symbols/Source/ExtensionGroupingInfo.cs`
* `src/Compilers/CSharp/Portable/Symbols/Synthesized/SynthesizedUnionCtor.cs`
* `src/Compilers/CSharp/Portable/Symbols/Synthesized/SynthesizedUnionValuePropertySymbol.cs`
* `src/Compilers/Core/Portable/Symbols/WellKnownMemberNames.cs`
* `src/Compilers/Core/Portable/Symbols/TypeKind.cs`
* `src/Compilers/Core/Portable/Compilation/ClosedDerivedTypeInfo.cs`
* `src/Compilers/CSharp/Portable/Syntax/Syntax.xml`
* `src/Compilers/CSharp/Portable/Syntax/SyntaxKindFacts.cs`
* `src/Workspaces/Core/Portable/Editing/SyntaxGenerator.cs`
* `src/Workspaces/Core/Portable/Editing/DeclarationModifiers.cs`
* `src/Workspaces/CSharp/Portable/CodeGeneration/CSharpSyntaxGenerator.cs`
* `src/Workspaces/SharedUtilitiesAndExtensions/Compiler/CSharp/CodeStyle/CSharpCodeStyleOptions.cs`

Roslyn tests:

* `src/Compilers/CSharp/Test/Emit3/Semantics/ExtensionTests.cs`
* `src/Compilers/CSharp/Test/Emit3/Semantics/ExtensionTests2.cs`
* `src/Compilers/CSharp/Test/CSharp15/UnionsTests.cs`
* `src/Compilers/CSharp/Test/CSharp15/ClosedClassesTests.cs`
* `src/Compilers/CSharp/Test/CSharp15/ExtensionIndexersTests.cs`
* `src/Compilers/CSharp/Test/Symbol/SymbolDisplay/SymbolDisplayTests.cs`
* `src/Compilers/CSharp/Test/Symbol/DocumentationComments/CrefTests.cs`
* `src/Compilers/CSharp/Test/Symbol/DocumentationComments/DocumentationCommentIDTests.cs`
* `src/Compilers/Core/CodeAnalysisTest/Symbols/DocumentationCommentIdTests.cs`
* `src/Workspaces/CoreTest/UtilityTest/DocumentationCommentIdTests.cs`
* `src/Workspaces/CSharpTest/CodeGeneration/SyntaxGeneratorTests.cs`

csharplang specifications (`https://github.com/dotnet/csharplang/blob/main/proposals/`):

* `csharp-15.0/unions.md` (champion <https://github.com/dotnet/csharplang/issues/9662>)
* `csharp-15.0/closed-hierarchies.md` (champion <https://github.com/dotnet/csharplang/issues/9499>)
* `csharp-15.0/extension-indexers.md` (champion <https://github.com/dotnet/csharplang/issues/9856>)
* `csharp-14.0/extensions.md` (champion <https://github.com/dotnet/csharplang/issues/8697>)

Roslyn test plan issues: unions <https://github.com/dotnet/roslyn/issues/81074>,
closed class hierarchies <https://github.com/dotnet/roslyn/issues/81039>,
extension indexers <https://github.com/dotnet/roslyn/issues/81505>,
extension documentation comment IDs <https://github.com/dotnet/roslyn/issues/78606>.
