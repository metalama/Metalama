# C# 15 / .NET 11 — Unions and Closed Hierarchies (deep dive)

Research date: 2026-09-03. Target: .NET 11 / C# 15 GA, November 2026.
All facts below were read from primary sources (dotnet/roslyn `main`, dotnet/runtime `main`,
dotnet/csharplang `main`, learn.microsoft.com). Every claim is followed by its source.

---

## 0. Status at .NET 11 GA — the headline

**Both features are stable, non-preview C# 15 language features.** Neither requires
`<LangVersion>preview</LangVersion>`.

### 0.1 Roslyn feature status table

Source: <https://github.com/dotnet/roslyn/blob/main/docs/Language%20Feature%20Status.md>
(raw: `https://raw.githubusercontent.com/dotnet/roslyn/main/docs/Language%20Feature%20Status.md`)

Both rows appear in the **`# C# 15.0`** section (line 37 onwards), *not* in `# Working Set C#`:

| Feature | Branch | State | Developer | Reviewer | IDE Buddy | LDM Champ |
| ------- | ------ | ----- | --------- | -------- | --------- | --------- |
| [Unions](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/unions.md) | [Unions](https://github.com/dotnet/roslyn/tree/features/Unions) | [C# 15](https://github.com/dotnet/roslyn/issues/81074) | AlekseyTs | RikkiGibson, jjonescz | TBD | MadsTorgersen |
| [Closed class hierarchies](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/closed-hierarchies.md) | [closed-class](https://github.com/dotnet/roslyn/tree/features/closed-class) | [C# 15](https://github.com/dotnet/roslyn/issues/81039) | RikkiGibson | AlekseyTs, jjonescz | TBD | mattwar |

Contrast with the same file's **Working Set** row for Unsafe evolution, which *is* explicitly a preview
feature: `[Merged as preview feature into .NET 11p2 and VS 18.6]`. Unions and closed classes carry no
such qualifier.

### 0.2 The decisive evidence — required LanguageVersion

`src/Compilers/CSharp/Portable/Errors/MessageID.cs`, `RequiredVersion(MessageID)`:

```csharp
// C# preview features.
case MessageID.IDS_FeatureUnsafeEvolution:
    return LanguageVersion.Preview;

// C# 15.0 features.
case MessageID.IDS_FeatureCollectionExpressionArguments:
case MessageID.IDS_FeatureUnions:
case MessageID.IDS_FeatureStaticMembersInInterfaces:
case MessageID.IDS_FeatureClosedClasses: // semantic check
case MessageID.IDS_FeatureLabeledBreakContinue:
case MessageID.IDS_FeatureExtensionIndexers:
    return LanguageVersion.CSharp15;
```

`IDS_FeatureUnions = MessageBase + 12860`, `IDS_FeatureClosedClasses = MessageBase + 12862`.
Resource strings: `IDS_FeatureUnions` = `"unions"`, `IDS_FeatureClosedClasses` = `"closed classes"`.

`LanguageVersion.CSharp15 = 1500` (`src/Compilers/CSharp/Portable/LanguageVersion.cs`).
`ERR_FeatureNotAvailableInVersion15 = 9399` exists in `ErrorCode.cs`.

### 0.3 Corroborating signals

* Neither `UnionKeyword` / `ClosedKeyword` (SyntaxKind) nor `ITypeSymbol.IsUnion` / `IsClosed` /
  `UnionCaseTypes` / `GetClosedDerivedTypeInfo` carry an `[Experimental]` / `[RSEXPERIMENTAL…]`
  marker in `PublicAPI.Unshipped.txt`. By contrast `SafeKeyword = 8454` carries
  `[Experimental("RSEXPERIMENTAL006", …)]`, `MemorySafetyRulesVersion` carries `RSEXPERIMENTAL006`,
  and `PreCompilationSourceProductionContext` carries `RSEXPERIMENTAL007`.
* `Syntax.xml` on `main` declares `UnionDeclarationSyntax` **without** an `ExperimentalUrl` attribute.
  (The `features/Unions` branch snapshot still carries
  `ExperimentalUrl="https://github.com/dotnet/roslyn/issues/82567"`; `main` is the newer state and has
  dropped it.)
* Tests live in `src/Compilers/CSharp/Test/CSharp15/UnionsTests.cs` (≈1.77 MB, 56 861 lines) and
  `src/Compilers/CSharp/Test/CSharp15/ClosedClassesTests.cs` (≈321 KB), in the project
  `Microsoft.CodeAnalysis.CSharp.CSharp15.UnitTests.csproj`.

### 0.4 The one caveat

learn.microsoft.com "What's new in C# 15" (`ms.date: 2026-08-14`, `updated_at: 2026-08-19`) says, of
unions:

> The runtime includes the `UnionAttribute` and `IUnion` types beginning with .NET 11 Preview 5.
> **Some features from the proposal specification aren't yet implemented. Those features are coming in
> future previews.**

The page does not enumerate which. So: the *feature* is C# 15 / stable, but as of mid-August 2026 the
implementation was not yet 100 % of the speclet. See Open Questions.

The same page frames C# 15 overall as: "C# 15 is the latest C# preview release. .NET 11 preview
versions support C# 15." — i.e. the *release* was in preview in August 2026, not the *feature*.

---

## 1. Union declaration syntax

### 1.1 The csharplang grammar

Source: <https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/unions.md>
(champion issue <https://github.com/dotnet/csharplang/issues/9662>)

```antlr
union_declaration
    : attributes? struct_modifier* 'partial'? 'union' identifier type_parameter_list?
      '(' case_types ')'  struct_interfaces? type_parameter_constraints_clause* 
      (`{` struct_member_declaration* `}` | ';')
    ;
case_types
    : type (',' type)*
    ;
```

Resolved design note in the same speclet:

> ### [Resolved] Union declaration syntax
> It looks like the proposed syntax is incomplete or unnecessarily limiting. For example, it looks like
> base clause is not permitted. … apart from the element-types-list the syntax should match regular
> `struct`/`record struct` declaration where the `struct` keyword is replaced with `union` keyword.
> **Resolution:** The restriction is removed.

### 1.2 Roslyn's `UnionDeclarationSyntax` — exact node shape

Source: `src/Compilers/CSharp/Portable/Syntax/Syntax.xml`, line 3513.

```xml
<Node Name="UnionDeclarationSyntax" Base="TypeDeclarationSyntax" SkipConvenienceFactories="true">
  <TypeComment><summary>Union type declaration syntax.</summary></TypeComment>
  <Kind Name="UnionDeclaration"/>
  <Field Name="AttributeLists"     Type="SyntaxList&lt;AttributeListSyntax&gt;" Override="true"/>
  <Field Name="Modifiers"          Type="SyntaxList&lt;SyntaxToken&gt;" Override="true"/>
  <Field Name="Keyword"            Type="SyntaxToken" Override="true">
    <PropertyComment><summary>Gets the union keyword token.</summary></PropertyComment>
    <Kind Name="UnionKeyword"/>
  </Field>
  <Field Name="Identifier"         Type="SyntaxToken" Override="true"><Kind Name="IdentifierToken"/></Field>
  <Field Name="TypeParameterList"  Type="TypeParameterListSyntax" Optional="true" Override="true"/>
  <Field Name="ParameterList"      Type="ParameterListSyntax" Optional="true" Override="true" />
  <Field Name="BaseList"           Type="BaseListSyntax" Optional="true" Override="true"/>
  <Field Name="ConstraintClauses"  Type="SyntaxList&lt;TypeParameterConstraintClauseSyntax&gt;" Override="true"/>
  <Field Name="OpenBraceToken"     Type="SyntaxToken" Override="true" Optional="true"><Kind Name="OpenBraceToken"/></Field>
  <Field Name="Members"            Type="SyntaxList&lt;MemberDeclarationSyntax&gt;" Override="true"/>
  <Field Name="CloseBraceToken"    Type="SyntaxToken" Override="true" Optional="true"><Kind Name="CloseBraceToken"/></Field>
  <Field Name="SemicolonToken"     Type="SyntaxToken" Optional="true" Override="true"><Kind Name="SemicolonToken"/></Field>
</Node>
```

This is **field-for-field identical to `ClassDeclarationSyntax` and `StructDeclarationSyntax`**, except
for `Keyword`'s kind. It derives from `TypeDeclarationSyntax`, so the abstract
`Keyword` / `TypeParameterList` / `ParameterList` / `ConstraintClauses` / `Members` and the
`BaseTypeDeclarationSyntax` `Identifier` / `BaseList` / braces / semicolon are all overridden.

`TypeDeclarationSyntax.Keyword`'s doc comment was updated to:
> Gets the type keyword token ("class", "struct", "interface", "record", "extension", "union").

`BaseTypeDeclarationSyntax` doc still reads "(class, struct, interface, record, extension)" — stale, but
harmless.

**The case-type list is `ParameterList`, a `ParameterListSyntax`.** There is no dedicated
`case_types` node. `union Pet(Cat, Dog)` produces a `ParameterListSyntax` whose two
`ParameterSyntax` nodes each have `Type` set and `Identifier` defaulted/missing —
`ParameterSyntax.Identifier` is `Optional="true"` in `Syntax.xml` (line 4436).

### 1.3 SyntaxKind values

`src/Compilers/CSharp/Portable/Syntax/SyntaxKind.cs`:

```csharp
/// <summary>Represents <see langword="extension"/>.</summary>
ExtensionKeyword = 8451,
/// <summary>Represents <see langword="union"/>.</summary>
UnionKeyword = 8452,
/// <summary>Represents <see langword="closed"/>.</summary>
ClosedKeyword = 8453,
/// <summary>Represents <see langword="safe"/>.</summary>
[Experimental("RSEXPERIMENTAL006", UrlFormat = "https://github.com/dotnet/roslyn/issues/82789")]
SafeKeyword = 8454,
```

```csharp
WithElement = 9081,
UnionDeclaration = 9082,      // last member of the enum
```

`UnionKeyword` and `ClosedKeyword` sit in the **contextual keyword** block (values below
`ElifKeyword`); the file's own comment says these must be reflected in
`SyntaxFacts.GetContextualKeywordKinds()`, `IsContextualKeyword`, `GetContextualKeywordKind(string)`
and `GetText`.

There is **no** `ClosedDeclaration` kind — `closed` is a modifier token in
`ClassDeclarationSyntax.Modifiers` / `RecordDeclarationSyntax.Modifiers`.

### 1.4 Public API surface (C#)

`src/Compilers/CSharp/Portable/PublicAPI.Unshipped.txt`:

* `Microsoft.CodeAnalysis.CSharp.SyntaxKind.UnionKeyword = 8452`
* `Microsoft.CodeAnalysis.CSharp.SyntaxKind.UnionDeclaration = 9082`
* `Microsoft.CodeAnalysis.CSharp.SyntaxKind.ClosedKeyword = 8453`
* `Microsoft.CodeAnalysis.CSharp.Syntax.UnionDeclarationSyntax` + all `With*` / `Add*` / `Update`
* `virtual CSharpSyntaxVisitor.VisitUnionDeclaration(UnionDeclarationSyntax! node) -> void`
* `virtual CSharpSyntaxVisitor<TResult>.VisitUnionDeclaration(UnionDeclarationSyntax! node) -> TResult?`
* `override CSharpSyntaxRewriter.VisitUnionDeclaration(UnionDeclarationSyntax! node) -> SyntaxNode?`
* `Microsoft.CodeAnalysis.CSharp.Conversion.IsUnion.get -> bool`

**Exactly one `SyntaxFactory.UnionDeclaration` overload** (because of
`SkipConvenienceFactories="true"`), the full 12-argument one:

```csharp
static SyntaxFactory.UnionDeclaration(
    SyntaxList<AttributeListSyntax> attributeLists,
    SyntaxTokenList modifiers,
    SyntaxToken keyword,
    SyntaxToken identifier,
    TypeParameterListSyntax? typeParameterList,
    ParameterListSyntax? parameterList,
    BaseListSyntax? baseList,
    SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
    SyntaxToken openBraceToken,
    SyntaxList<MemberDeclarationSyntax> members,
    SyntaxToken closeBraceToken,
    SyntaxToken semicolonToken) -> UnionDeclarationSyntax!
```

`ClassDeclarationSyntax`, `StructDeclarationSyntax` and `InterfaceDeclarationSyntax` are also
`SkipConvenienceFactories="true"`, so this matches their treatment.

### 1.5 Parsing (contextual-keyword behaviour — important)

`src/Compilers/CSharp/Portable/Parser/LanguageParser.cs`:

```csharp
private bool IsEnabledRecordOrUnionKeyword(SyntaxToken token)
{
    // Normally the parser recognizes unsupported features and binding reports a language-version
    // diagnostic. Record and union are contextual keywords, however, so treating them as type
    // declarations in every ambiguous context would break older code. Only recognize them here
    // when the corresponding feature is enabled.
    return token.ContextualKind switch
    {
        SyntaxKind.RecordKeyword => IsFeatureEnabled(MessageID.IDS_FeatureRecords),
        SyntaxKind.UnionKeyword  => IsFeatureEnabled(MessageID.IDS_FeatureUnions),
        _ => false,
    };
}
```

**`union` is only parsed as a type declaration when `LangVersion >= 15`.** Below that it is an
ordinary identifier. This makes the parse tree LangVersion-dependent, exactly like `record`.

`closed` is handled in `GetModifier`:

```csharp
case SyntaxKind.IdentifierToken:
    switch (contextualKind)
    {
        case SyntaxKind.PartialKeyword:  return DeclarationModifiers.Partial;
        case SyntaxKind.AsyncKeyword:    return DeclarationModifiers.Async;
        case SyntaxKind.RequiredKeyword: return DeclarationModifiers.Required;
        case SyntaxKind.FileKeyword:     return DeclarationModifiers.File;
        case SyntaxKind.ClosedKeyword:   return DeclarationModifiers.Closed;
        case SyntaxKind.SafeKeyword:     return DeclarationModifiers.Safe;
    }
```

Unions are parsed by `ParseMainTypeDeclaration`, the same routine as class/struct/interface/record/
extension, reached from the ordinary member-declaration path — so a union nests anywhere a struct does.

```csharp
bool isExtension = keyword.Kind == SyntaxKind.ExtensionKeyword;
bool isUnion     = keyword.Kind == SyntaxKind.UnionKeyword;
…
name = this.ParseIdentifierToken();
var typeParameters = this.ParseTypeParameterList();
var paramList = CurrentToken.Kind == SyntaxKind.OpenParenToken || isExtension
    ? ParseParenthesizedParameterList(forExtensionOrUnion: isExtension || isUnion) : null;
var baseList = isExtension ? null : this.ParseBaseList();
…
if (this.CurrentToken.ContextualKind == SyntaxKind.WhereKeyword) { … ParseTypeParameterConstraintClauses … }
```

`ParseParenthesizedParameterList(forExtensionOrUnion: true)` passes
`identifierIsOptional: true` and `requireOneElement: true` into `ParseParameterList` — i.e. for a
union, parameter *names* are optional and **at least one** case type is required.

`IsPartialType()` calls `IsClassStructInterfaceRecordOrUnionKeyword`, so `partial union` parses.

### 1.6 Worked examples of full declaration syntax

From the speclet and from `Test/CSharp15/UnionsTests.cs`:

```csharp
// Simplest form, semicolon body
public union Pet(Cat, Dog, Bird);

// Generic
union S1<T>(T);
public union OneOrMore<T>(T, IEnumerable<T>)
{
    public IEnumerable<T> AsEnumerable() => Value switch
    {
        IEnumerable<T> list => list,
        T value => [value],
    }
}

// "Discriminated" union with freshly declared case types
public record class None();
public record class Some<T>(T value);
public union Option<T>(None, Some<T>);

// With a base list (interfaces only — a union is a struct)
union S1(int, bool) : I1 { }
union S1(int, bool) : System.Runtime.CompilerServices.IUnion { }

// Partial: the case-type list may appear on exactly one part
partial union S1(int, bool) { }
partial union S1 { }
```

`UnionsTests.UnionDeclaration_03` / `_04` prove both orderings of the partial parts compile clean and
that `UnionCaseTypes` is `[System.Int32, System.Boolean]` either way.

Mixed-kind partials are an error:

```
error CS0261: Partial declarations of 'S1' must be all classes, all record classes, all structs,
              all unions, all record structs, or all interfaces
```
(`ERR_PartialTypeKindConflict`; tests `UnionDeclaration_05` mixing `partial struct` + `partial union`,
`UnionDeclaration_06` mixing `partial record` + `partial union`.)

### 1.7 Members allowed and forbidden in a union body

From the speclet, and enforced by these error codes:

* `ERR_InstanceFieldInUnion` (CS9373): *"Instance fields, auto-properties or field-like events are not
  permitted in a 'union' declaration."*
* `ERR_InstanceCtorWithOneParameterInUnion` (CS9374): *"Explicitly declared public constructors with a
  single parameter are not permitted in a 'union' declaration."*
* `ERR_UnionConstructorCallsDefaultConstructor` (CS9375): *"A constructor declared in a 'union'
  declaration must have a 'this' initializer that calls a synthesized constructor or an explicitly
  declared constructor."*
* `ERR_UnionDeclarationNeedsCaseTypes` (CS9370): *"A union declaration must specify at least one case
  type."*
* `ERR_MemberProviderInUnionDeclaration` (CS9387): *"A 'union' declaration cannot use a union member
  provider interface."* (i.e. `union S1(int, bool) : S1.IUnionMembers` is rejected.)

Otherwise a union body accepts anything a struct body accepts (methods, properties with bodies,
nested types, static fields, operators, `Deconstruct`, etc.).

### 1.8 Modifiers permitted on a union declaration

`SourceMemberContainerTypeSymbol.MakeModifiers` (`SourceMemberContainerSymbol.cs`, ~line 340):

```csharp
case TypeKind.Struct:
    allowedModifiers |= DeclarationModifiers.Partial | DeclarationModifiers.ReadOnly
                      | DeclarationModifiers.Unsafe | DeclarationModifiers.Safe;

    if (!this.IsRecordStruct && !this.IsUnionDeclaration)
    {
        allowedModifiers |= DeclarationModifiers.Ref;
    }
    break;
```

So a union allows: accessibility modifiers, `partial`, `readonly`, `unsafe`, `safe`, `new` (when
nested). It **forbids `ref`** (unlike a plain struct) and, being `TypeKind.Struct`, forbids `closed`,
`abstract`, `sealed` and `static` (it is implicitly `sealed`).

`record union` is **not supported**. Speclet resolution:

> ### [Resolved] Is union declaration a record?
> **Resolution:** A union declaration is a plain struct, not record struct.
> The ```record union ...``` isn't supported

Corroborated: `UnionsTests.cs` contains no `record union` test, and the parser reaches
`SyntaxKind.UnionKeyword` in `ParseMainTypeDeclaration` only after `tryScanRecordStart` has *failed*.

**Nested / generic / partial: all yes. Record: no.**
Nested is implied by the ordinary member-declaration parse path and by tests such as
`union S(int);` appearing inside test containers; generic proven by `union S1<T>(T);`; partial proven
by `UnionDeclaration_03`–`_06`; constraint clauses proven by
`SourceNamedTypeSymbol.GetConstraintClauses` and `MakeTypeParameters` both listing
`SyntaxKind.UnionDeclaration`.

---

## 2. What the compiler emits for a union

### 2.1 Lowering, per the speclet

> A union declaration is lowered to a struct declaration with
> * the same attributes, modifiers, name, type parameters and constraints,
> * implicit implementations of `IUnion`,
> * a `public object? Value { get; }` auto-property,
> * a public constructor for each of the *case_types*,
> * any members in the union declaration's body.
>
> It is an error for user-declared members to conflict with generated members.

```csharp
public union Pet(Cat, Dog){ ... }
```
lowers to
```csharp
[Union] public struct Pet : IUnion
{
    public Pet(Cat value) => Value = value;
    public Pet(Dog value) => Value = value;
    
    public object? Value { get; }
    
    ... // original body
}
```

**The compiler does NOT generate the case types.** Case types are pre-existing types named in the
parameter list. There are no synthesized nested case classes. (This is a "type union", not a
"discriminated/tagged union" — the speclet is explicit: *"The proposed unions in C# are unions of
*types* and not 'discriminated' or 'tagged'."*)

### 2.2 The actual emitted IL

From `UnionsTests.cs`, `verifier.VerifyTypeIL("S1", …)` for `union S1(bool, int)`:

```
.class public sequential ansi sealed beforefieldinit S1
    extends [netstandard]System.ValueType
    implements System.Runtime.CompilerServices.IUnion
{
    .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 02 00 00 )
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 )
    .custom instance void System.Runtime.CompilerServices.UnionAttribute::.ctor() = ( 01 00 00 00 )
    .interfaceimpl type System.Runtime.CompilerServices.IUnion
        .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 )
    // Fields
    .field private initonly object '<Value>k__BackingField'
    .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
    .custom instance void [netstandard]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [netstandard]System.Diagnostics.DebuggerBrowsableState) = ( 01 00 00 00 00 00 00 00 )
    // Methods
    .method public final hidebysig specialname newslot virtual 
        instance object get_Value () cil managed 
    {
        .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
        IL_0000: ldarg.0
        IL_0001: ldfld object S1::'<Value>k__BackingField'
        IL_0006: ret
    }
    .method public hidebysig specialname rtspecialname 
        instance void .ctor ( bool 'value' ) cil managed 
    {
        .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
        IL_0000: ldarg.0
        IL_0001: ldarg.1
        IL_0002: box [netstandard]System.Boolean
        IL_0007: stfld object S1::'<Value>k__BackingField'
        IL_000c: nop
        IL_000d: ret
    }
    .method public hidebysig specialname rtspecialname 
        instance void .ctor ( int32 'value' ) cil managed { /* same, box System.Int32 */ }
    // Properties
    .property instance object Value()
    {
        .get instance object S1::get_Value()
    }
}
```

Facts to note:

* The type is `sequential`, `sealed`, `beforefieldinit`, `extends System.ValueType`.
* `[UnionAttribute]` is synthesized onto the type unless the user already wrote it
  (`ShouldApplyUnionAttribute() => IsUnionDeclaration && !HasUnionAttribute`, `SourceNamedTypeSymbol.cs`).
* `IUnion` is implemented **implicitly**; `get_Value` is `public final … newslot virtual`.
  Listing `IUnion` explicitly in the base list does not duplicate it —
  `UnionDeclaration_22_IUnion_InBaseInterfaces` asserts
  `s1.InterfacesNoUseSiteDiagnostics().Single()` is `System.Runtime.CompilerServices.IUnion`.
* Exactly one private `initonly object` backing field, `<Value>k__BackingField`, with
  `[CompilerGenerated]` and `[DebuggerBrowsable(Never)]`.
* One `public` `[CompilerGenerated]` constructor per case type; **value-type cases are boxed** on entry.
* `Value` is get-only (no setter emitted).

### 2.3 Symbol-level synthesis

`SourceMemberContainerTypeSymbol.AddSynthesizedTypeMembersIfNecessary`:

```csharp
if (declaration.Kind is not (DeclarationKind.Record or DeclarationKind.RecordStruct or DeclarationKind.Union)
    && declaredMembersAndInitializers.PrimaryConstructor is null) { return; }
…
if (declaration.Kind is DeclarationKind.Union)
{
    // Synthesize Value property
    var valuePropertySyntax = (TypeDeclarationSyntax)declaration.Declarations[0].SyntaxReference.GetSyntax();
    var valueProperty = new SynthesizedUnionValuePropertySymbol(this, valuePropertySyntax, diagnostics);
    members.Add(valueProperty);
    Debug.Assert(valueProperty.GetMethod is object);
    Debug.Assert(valueProperty.SetMethod is null);
    members.Add(valueProperty.GetMethod);
    var backingField = valueProperty.DeclaredBackingField;
    members.Add(backingField);
    …
    if (declaredMembersAndInitializers.DeclarationWithParameters?.ParameterList is { } parameterList)
    {
        // Synthesize Union type constructors
        …
        foreach (var parameterSyntax in parameterList.Parameters)
        {
            report_ERR_UnionDeclarationNeedsCaseTypes = false;
            if (parameterSyntax.IsArgList) { diagnostics.Add(ErrorCode.ERR_IllegalVarArgs, …); continue; }
            TypeSyntax? typeSyntax = parameterSyntax.Type;
            typesBuilder.Add(typeSyntax);

            SyntaxToken syntaxToken = parameterSyntax.GetFirstToken();
            if (syntaxToken != typeSyntax.GetFirstToken())
                diagnostics.Add(ErrorCode.ERR_UnexpectedToken, …);   // no modifiers/attributes before the type
            syntaxToken = parameterSyntax.GetLastToken();
            if (syntaxToken != typeSyntax.GetLastToken())
                diagnostics.Add(ErrorCode.ERR_UnexpectedToken, …);   // no name/default after the type
        }
        // In order to keep the same relative order between constructors during emit
        // we assign offset in decreasing order
        int memberOffset = members.Count + typesBuilder.Count;
        …
    }
}
```

So although the case-type list is syntactically a `ParameterListSyntax`, **each entry must be a bare
type**: anything before or after the type in the `ParameterSyntax` produces `ERR_UnexpectedToken`.
Each case type must be implicitly convertible to `object` — otherwise `ERR_NoImplicitConversionToObject`
(CS9371): *"Cannot convert type '{0}' to 'object' via an implicit reference or boxing conversion"*.

`SynthesizedUnionValuePropertySymbol` — the class name; its backing field is exempted from the usual
"instance field in union" check:
```csharp
if (!field.IsStatic && field.AssociatedSymbol is not SynthesizedUnionValuePropertySymbol) { … }
```

### 2.4 SemanticModel behaviour on the case-type list (important)

From `UnionsTests.cs`:

```csharp
Assert.Same(s1, model.GetDeclaredSymbol(s1Decl).GetSymbol());
Assert.Null(model.GetDeclaredSymbol(s1Decl.ParameterList));
Assert.Null(model.GetDeclaredSymbol(s1Decl.ParameterList.Parameters[0]));
Assert.Null(model.GetDeclaredSymbol(s1Decl.ParameterList.Parameters[0].Type));
Assert.Null(model.GetDeclaredSymbol(s1Decl.ParameterList.Parameters[1]));
Assert.Null(model.GetDeclaredSymbol(s1Decl.ParameterList.Parameters[1].Type));

var typeInfo = model.GetTypeInfo(s1Decl.ParameterList.Parameters[0].Type);
Assert.Equal("System.Boolean", typeInfo.Type.ToTestDisplayString());
Assert.Equal("System.Boolean", typeInfo.ConvertedType.ToTestDisplayString());
```

`GetDeclaredSymbol` on a union's `ParameterList` or any of its `Parameter`s returns **null** — they are
not parameter symbols. Use `GetTypeInfo(parameter.Type)` to obtain the case type.

The synthesized constructors' `Locations` point at the case-type syntax:
```csharp
location = members[^3].Locations.Single();
Assert.Equal("bool", location.SourceTree.GetRoot().FindNode(location.SourceSpan).ToString());
location = members[^2].Locations.Single();
Assert.Equal("int",  location.SourceTree.GetRoot().FindNode(location.SourceSpan).ToString());
```
The `Value` property and its accessor point at the whole union declaration node.

### 2.5 Runtime types (dotnet/runtime, `System.Private.CoreLib`)

`src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/UnionAttribute.cs`:

```csharp
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Indicates that a class or struct is a union type, enabling compiler support for union behaviors.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class UnionAttribute : Attribute
    {
    }
}
```

`.../IUnion.cs`:

```csharp
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Provides a common interface for accessing the contents of a union type at runtime.
    /// </summary>
    public interface IUnion
    {
        /// <summary>
        /// Gets the value contained in the union, or <see langword="null" /> if the union has no value.
        /// </summary>
        object? Value { get; }
    }
}
```

`IUnion`'s doc adds: *"Implementing this interface is not required for union behaviors provided by the
compiler."*

Ship vehicle: learn.microsoft.com states *"The runtime includes these types beginning with .NET 11
Preview 5."*

Note the speclet's `UnionAttribute` sketch was `public class UnionAttribute : Attribute;` with only
`AttributeUsage(Class | Struct, AllowMultiple = false)`. The **shipped** type is additionally `sealed`
and `Inherited = false`. The runtime source is the more recent/authoritative form.

The compiler does **not** synthesize these types. Speclet resolution:
> **Resolution:** The compiler should not synthesize these types and users should provide them
> explicitly, either by referencing assemblies or defining them locally.

Roslyn well-known type IDs (`src/Compilers/Core/Portable/WellKnownTypes.cs`):
* `System_Runtime_CompilerServices_UnionAttribute` → `"System.Runtime.CompilerServices.UnionAttribute"`
* `System_Runtime_CompilerServices_IUnion` → `"System.Runtime.CompilerServices.IUnion"`
* `System_Runtime_CompilerServices_IsClosedTypeAttribute` → `"System.Runtime.CompilerServices.IsClosedTypeAttribute"`

Well-known members (`WellKnownMembers.cs`): `…UnionAttribute__ctor`,
`…IsClosedTypeAttribute__ctor`, `…IsClosedTypeAttribute__DerivedTypes`.

`IUnion<TUnion>` was proposed and then **removed**:
> ### [Resolved] Design of generic IUnion interface
> **Resolution:** The `IUnion<TUnion>` interface is removed for now.

---

## 3. How a union appears through the Roslyn symbol API

### 3.1 It is an `INamedTypeSymbol` with `TypeKind.Struct` — there is NO `TypeKind.Union`

`src/Compilers/Core/Portable/Symbols/TypeKind.cs` ends at:
```csharp
Submission = 12,
FunctionPointer = 13,
Extension = 14,
```
No `Union` member. (Compare: `extension` blocks *did* get `TypeKind.Extension = 14`.)

The internal `DeclarationKind` *does* gain a `Union` member
(`src/Compilers/CSharp/Portable/Declarations/DeclarationKind.cs`):
```csharp
internal enum DeclarationKind : byte
{
    Namespace, Class, Interface, Struct, Enum, Delegate, Script, Submission,
    ImplicitClass, Record, RecordStruct, Extension, Union,
}
…
case SyntaxKind.UnionDeclaration: return DeclarationKind.Union;
```

and it maps straight onto `TypeKind.Struct`
(`src/Compilers/CSharp/Portable/Symbols/EnumConversions.cs`):

```csharp
internal static TypeKind ToTypeKind(this DeclarationKind kind)
{
    switch (kind)
    {
        …
        case DeclarationKind.Struct:
        case DeclarationKind.Union:
        case DeclarationKind.RecordStruct:
            return TypeKind.Struct;
        case DeclarationKind.Extension:
            return TypeKind.Extension;
        …
    }
}
```

**Consequence:** a `union` declaration surfaces as an `INamedTypeSymbol` with
`TypeKind == TypeKind.Struct`, `IsValueType == true`, `IsSealed == true`, `IsReferenceType == false`.
Any existing `TypeKind` switch keeps working; the union-ness is a *flag*, not a kind.

### 3.2 The new public symbol API — on `ITypeSymbol`, not `INamedTypeSymbol`

`src/Compilers/Core/Portable/PublicAPI.Unshipped.txt` (language-agnostic `Microsoft.CodeAnalysis`):

```
Microsoft.CodeAnalysis.ITypeSymbol.IsUnion.get -> bool
Microsoft.CodeAnalysis.ITypeSymbol.UnionCaseTypes.get -> System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.ITypeSymbol!>
Microsoft.CodeAnalysis.ITypeSymbol.IsClosed.get -> bool
Microsoft.CodeAnalysis.ITypeSymbol.GetClosedDerivedTypeInfo(System.Threading.CancellationToken cancellationToken) -> Microsoft.CodeAnalysis.ClosedDerivedTypeInfo
Microsoft.CodeAnalysis.ClosedDerivedTypeInfo
Microsoft.CodeAnalysis.ClosedDerivedTypeInfo.ClosedDerivedTypeInfo() -> void
Microsoft.CodeAnalysis.ClosedDerivedTypeInfo.ClosedDerivedTypes.get -> System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.INamedTypeSymbol!>
Microsoft.CodeAnalysis.ClosedDerivedTypeInfo.IsComplete.get -> bool
Microsoft.CodeAnalysis.Operations.CommonConversion.IsUnion.get -> bool
const Microsoft.CodeAnalysis.WellKnownMemberNames.HasValuePropertyName = "HasValue" -> string!
const Microsoft.CodeAnalysis.WellKnownMemberNames.TryGetValueMethodName = "TryGetValue" -> string!
```

None of these carry an experimental marker.

XML docs, `src/Compilers/Core/Portable/Symbols/ITypeSymbol.cs`:

```csharp
/// <summary>
/// True if language treats the type as a Union.
/// </summary>
bool IsUnion { get; }

/// <summary>
/// When <see cref="IsUnion"/> is true, returns the case types of the union. Otherwise, returns an empty array.
/// </summary>
ImmutableArray<ITypeSymbol> UnionCaseTypes { get; }

/// <summary>
/// Indicates that the type is restricted from being inherited from outside its containing module.
/// </summary>
bool IsClosed { get; }

/// <summary>
/// Gets the direct derived types of a closed type.
/// </summary>
/// <exception cref="InvalidOperationException">If this is not a closed type.</exception>
ClosedDerivedTypeInfo GetClosedDerivedTypeInfo(CancellationToken cancellationToken);
```

Implementation, `src/Compilers/CSharp/Portable/Symbols/PublicModel/TypeSymbol.cs`:

```csharp
bool ITypeSymbol.IsRecord => UnderlyingTypeSymbol.IsRecord || UnderlyingTypeSymbol.IsRecordStruct;

bool ITypeSymbol.IsUnion => UnderlyingTypeSymbol is Symbols.NamedTypeSymbol { IsUnionType: true };

ImmutableArray<ITypeSymbol> ITypeSymbol.UnionCaseTypes
{
    get
    {
        if (UnderlyingTypeSymbol is not Symbols.NamedTypeSymbol { IsUnionType: true } namedType)
            return ImmutableArray<ITypeSymbol>.Empty;
        return namedType.UnionCaseTypesNoUseSiteDiagnostics.GetPublicSymbols();
    }
}

bool ITypeSymbol.IsClosed => UnderlyingTypeSymbol is Symbols.NamedTypeSymbol { IsClosed: true };

ClosedDerivedTypeInfo ITypeSymbol.GetClosedDerivedTypeInfo(CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    if (UnderlyingTypeSymbol is not Symbols.NamedTypeSymbol { IsClosed: true } namedType)
        throw new InvalidOperationException(CSharpResources.GetClosedDerivedTypeInfoMustBeClosed);
    var isComplete = namedType.TryGetClosedSubtypes(out var subtypes, cancellationToken);
    return new ClosedDerivedTypeInfo(subtypes.GetPublicSymbols(), isComplete);
}
```

So `IsUnion` / `IsClosed` are `false` for anything that is not a `NamedTypeSymbol`
(arrays, pointers, type parameters, dynamic).
`GetClosedDerivedTypeInfo` **throws `InvalidOperationException`** on a non-closed type — always guard
with `IsClosed`.

`ClosedDerivedTypeInfo` (`src/Compilers/Core/Portable/Compilation/ClosedDerivedTypeInfo.cs`):

```csharp
namespace Microsoft.CodeAnalysis
{
    /// <summary>Information about derived types of a closed type.</summary>
    public readonly struct ClosedDerivedTypeInfo
    {
        /// <summary>
        /// Possible direct derived types of the closed type.
        /// </summary>
        public ImmutableArray<INamedTypeSymbol> ClosedDerivedTypes { get; }

        /// <summary>
        /// Indicates whether <see cref="ClosedDerivedTypes" /> represents all possible derived types
        /// (i.e. it is a complete set).
        /// This will be false, for example, when a generic closed type has an unspeakable derived type.
        /// </summary>
        public bool IsComplete { get; }

        internal ClosedDerivedTypeInfo(ImmutableArray<INamedTypeSymbol> closedDerivedTypes, bool isComplete) { … }
    }
}
```

VB also implements these (`src/Compilers/VisualBasic/Portable/Symbols/TypeSymbol.vb` appears in the
search hits), as do `CodeGenerationTypeSymbol` and the MetadataAsSource `WrappedNamedTypeSymbol`
wrappers in the Workspaces/Features layers — so any custom `ITypeSymbol` implementation must add
these four members.

### 3.3 Internal symbol surface (compiler-internal, for reference)

* `NamedTypeSymbol.IsUnionType` / `IsUnionTypeCore`
* `NamedTypeSymbol.UnionCaseTypesNoUseSiteDiagnostics`
* `NamedTypeSymbol.IsClosed`
* `NamedTypeSymbol.CandidateClosedSubtypeDefinitions`
* `NamedTypeSymbol.TryGetClosedSubtypes(out …, CancellationToken)`
* `SourceMemberContainerTypeSymbol.IsUnionDeclaration` (true only for a `union` *declaration*)

`SourceNamedTypeSymbol.cs`:
```csharp
internal override bool IsUnionTypeCore
{
    get { return IsUnionDeclaration || HasUnionAttribute; }
}
private bool HasUnionAttribute
{
    get { return GetEarlyDecodedWellKnownAttributeData()?.HasUnionAttribute == true; }
}
```

**`IsUnion` is true for BOTH a `union` declaration and any hand-written class/struct carrying
`[Union]`.** `IsUnionDeclaration` (internal) distinguishes them; there is no public equivalent.

For metadata symbols (`PENamedTypeSymbol.cs`) union-ness is detected purely from the attribute:
```csharp
uncommon.lazyHasUnionAttribute =
    ContainingPEModule.Module.FindTargetAttribute(_handle, AttributeDescription.UnionAttribute)
        .HasValue.ToThreeState();
```
and closed-ness from `IsClosedTypeAttribute` (`lazyIsClosed`).

### 3.4 Conversions

* `Microsoft.CodeAnalysis.CSharp.Conversion.IsUnion` (C#)
* `Microsoft.CodeAnalysis.Operations.CommonConversion.IsUnion` (language-agnostic)

New in both public API surfaces; a union conversion is reported as its own conversion classification.

---

## 4. Union semantics — conversions, pattern matching, exhaustiveness, nullability

### 4.1 Recognising a union type at all

> Any class or struct type with a `System.Runtime.CompilerServices.UnionAttribute` attribute is
> considered a *union type*.

A union type must follow the **basic union pattern** (mandatory):

* **Union creation members.** If the union-defining type is the union type itself, *each public
  constructor with a single by-value or `in` parameter* is a *union constructor*, and the set of those
  parameter types is the set of **case types**. If a *union member provider* is used, *each public
  static `Create` method with a single parameter whose return type is identity-convertible to the
  union type* is a *union factory method*. At least one creation member is required.
* **`Value` property.** `public object? Value { get; }` or `object`. Must have a `get`; may have
  `init`/`set` of any accessibility, which the compiler ignores.

Optional **non-boxing union access pattern**:
* `public bool HasValue { get; }` — true iff `Value` is not null.
* `public bool TryGetValue(out T value)` per case type — returns true iff `Value` is a non-null value
  of that case type. If the case type is a nullable value type, the out-parameter type is the
  *underlying* type.

`WellKnownMemberNames.HasValuePropertyName = "HasValue"` and
`WellKnownMemberNames.TryGetValueMethodName = "TryGetValue"` are the new public constants.

**Union member providers**: if the union type *directly contains* a public interface declaration named
`IUnionMembers`, and the union type implements it, then union members are found **only** on that
interface. Errors: `ERR_MissingUnionValueProperty` (CS9386), `ERR_MissingUnionCaseTypes` (CS9385),
`ERR_MemberProviderInUnionDeclaration` (CS9387, on a `union` declaration).

**A type parameter is never a union type, even when constrained to one** (resolved open question).

**Well-formedness assumptions** (unchecked; violating them makes behaviour undefined):
*Soundness*, *Stability*, *Creation equivalence*, *Access pattern consistency*.

### 4.2 Union conversions

> There's a union conversion to a union type `U` from a type or expression `E` if there's a standard
> implicit conversion from `E` to a type `C` and `C` is a parameter type of a *union creation member*
> of `U`.
> If union type `U` is a struct, there's a union conversion to type `U?` from a type or expression `E`
> if there's a standard implicit conversion from `E` to a type `C` and `C` is a parameter type of a
> *union creation member* of `U`.

* A union conversion is **not itself a standard implicit conversion**; it cannot chain into a
  user-defined implicit conversion or another union conversion.
* There are **no explicit union conversions** beyond the implicit ones.
* Executed by calling the creation member:
  ```csharp
  Pet pet = dog;                    // becomes  Pet pet = new Pet(dog);
  Result<string> result = "Hello";  // becomes  Result<string>.IUnionMembers.Create("Hello");
  ```
* Ambiguity (no single best candidate, or the candidate is not a union member) is an error.

**Priority relative to user-defined conversions** (resolved, approved by the working group):
* An implicit user-defined conversion operator **shadows** (beats) a union conversion.
* Under an explicit cast, an explicit user-defined conversion beats a union conversion.
* Without an explicit cast, a union conversion beats an inapplicable explicit user-defined conversion.

```csharp
[Union] struct S1 { public S1(int x)…; public S1(string x)…; public object Value…;
                    public static implicit operator S1(int x) => …; }
[Union] struct S2 { public S2(int x)…; public S2(string x)…; public object Value…;
                    public static explicit operator S2(int x) => …; }

static S1 Test1() => 10;      // implicit operator S1(int x) is used
static S1 Test2() => (S1)20;  // implicit operator S1(int x) is used
static S2 Test3() => 10;      // Union conversion S2.S2(int) is used
static S2 Test4() => (S2)20;  // explicit operator S2(int x)
```

**Nullable conversions**: supported — `S1? x = someInt;` works (resolution "Approved"), evaluated as
the union conversion `S`→`T` followed by wrapping `T`→`T?`.

**Lifted union conversions**: **not** supported.
> **Resolution:** No lifted union conversions for now.
```csharp
static S1 Test1(int? x) => x;  // error CS0029: Cannot implicitly convert type 'int?' to 'S1'
static S1? Test2(int? y) => y; // error CS0029: Cannot implicitly convert type 'int?' to 'S1?'
```

**Union conversion from a base type or an interface type** is *allowed* (resolution: "Do nothing
special for now. Generic scenarios cannot be fully protected anyway."), even though a user-defined
conversion from a base type is illegal. Note the asymmetry this produces:
```csharp
[Union] struct S1 { public S1(System.ValueType x) {} public S1(string x)…; public object Value…; }
static S1 Test1(System.ValueType x) => x;      // Union conversion
static S1 Test2(System.ValueType y) => (S1)y;  // Unboxing conversion
```

**Expression trees**: `ERR_ExpressionTreeContainsUnionConversion` (CS9369) —
*"An expression tree may not contain a union conversion."*

**Default parameter values** cannot use a union conversion:
```csharp
union S(int) { static void M1(S v = 1) {} }
// error CS1750: A value of type 'int' cannot be used as a default parameter because
//               there are no standard conversions to type 'S'
```

An explicit cast that resolves to a union conversion does work: `s = (S)100;` for `union S(int);`.

### 4.3 Union pattern matching ("unwrapping")

When the pattern input is a union type **or a `Nullable<union struct>`**, the value is "unwrapped" and
the pattern is applied to `Value` — for *some* patterns.

Compiler codegen preference (guaranteed minimum):
1. For a pattern implying a check for a specific type `T`: if a `TryGetValue(S value)` exists and
   there is an identity / implicit reference / implicit boxing conversion from `T` to `S`, call it.
   Prefer a non-boxing conversion; ties broken in an implementation-defined manner.
2. Otherwise, for a pattern implying a null check: use `HasValue` if available.
3. Otherwise, apply the pattern to `Value`.

Note: only **identity, reference and boxing** implicit conversions are considered for `TryGetValue`
matching (resolved). Unsuitable/`Obsolete`/`Experimental` APIs are silently ignored, no diagnostic.

| Pattern | Unwraps to `Value`? | Notes |
|---|---|---|
| `var` pattern (all forms) | **No** | captures the union itself |
| Discard `_` | **No** | |
| List pattern | **No** | |
| Property pattern **without** a type (`{ … }`) | **No** | applied to the union instance |
| Positional pattern **without** a type (`( … )`) | **No** | applied to the union instance |
| Type pattern (`T`) | **Yes** | equivalent to `{ Value: T }`; output value is `Value` narrowed to `T` |
| Declaration pattern (`T x`) | **Yes** | equivalent to `T and var x` |
| Property pattern **with** a type (`T { … }`) | **Yes** | equivalent to `T and { … }` |
| Positional pattern **with** a type (`T ( … )`) | **Yes** | equivalent to `T and ( … )` |
| Constant pattern, non-null | **Yes** | `result is 1` ⇒ `result != null && result.Value is 1` |
| Constant pattern `null` | **Yes** (special) | see below |
| Relational pattern | **Yes** | `result is > 1` ⇒ `result != null && result.Value is > 1` |
| `not` | **No** | applies to its input value; output value is its input value |
| `and` / `or` | per-branch | see below |

The `is`-type operator (`x is T`) **has the same meaning as a type pattern** when applied to a union
(resolved: *"Should work as a type pattern."*).

*Pattern compatibility*: union `Value` is pattern compatible with `type` when at least one case type
is pattern compatible with `type`. Otherwise the compiler reports an error.

```csharp
union Pet(Cat, Dog);
Pet? p = new Cat(...);

p is Pet   // error, since p.Value.Value is not *pattern compatible* with *Pet*
p is Cat   // true, since p.Value.Value is a Cat, output value is (Cat)p.Value.Value
```

```csharp
record Cat(...) : ICat;
union Pet(Cat, Dog) : IPet;
Pet p = new Cat(...);

p is IPet ip   // error, since p.Value is not *pattern compatible* with *IPet*
p is ICat c    // true, since p.Value is a Cat and Cat implements ICat, c is (ICat)p.Value
```

```csharp
record Cat(string Name);
union Pet(Cat, Dog);
Pet p = new Cat(Name: "Fido");

p is { Name: "Fido" }     // error: Pet has no 'Name'; applied to p
p is { Value: Cat }       // true; applied to p
p is Pet { Value: Cat }   // error; p.Value is not *pattern compatible* with *Pet*
p is Cat { Name: "Fido" } // true; applied to p.Value
p is {}                   // true; applied to p and always true for struct union
```

```csharp
union Pet(Cat, Dog) { public void Deconstruct(out object value) { value = this.Value; }}
Pet p = new Cat(Name: "Fido");

p is ("Fido")       // false: applied to p
p is (Cat)          // true: applied to p
p is Pet (Cat)      // error: p.Value is not *pattern compatible* with *Pet
p is Cat ("Fido")   // true; applied to p.Value
```

**Logical patterns.** In `and`, the right pattern's input is the left pattern's *output*; union
unwrapping changes the *value source*:

```csharp
union Pet(Cat, Dog);

GetPet() switch
{
    var pet and not null    => ... // 'var pet' applies to the incoming 'Pet' as does 'not' and 'null' to its 'Value'
    not null and var value  => ... // 'not' applies to the incoming 'Pet', 'null' applies to the its 'Value'.
                                   // 'var value' applies to the incoming 'Pet' because the left branch in this case
                                   // (`not null`) does not change the incoming value for the right branch.
    var pet and Dog         => ... // 'var pet' applies to the incoming 'Pet' and 'Dog' to its 'Value'
    Dog and { Name: "Dog" } => ... // 'Dog' applies to the incoming 'Pet''s `Value` and it is changing the incoming value
                                   // for the right branch to the `Dog` instance. Therefore, the property pattern on the right
                                   // is applied to that `Dog` instance
}
```

In `or`, both branches take the *same* input; the output is the input.

**Null matching.**
* *Struct union*: `s is null` ⇒ `s.Value == null`.
* *Class union*: `u is null` ⇒ `u == null || u.Value == null` (resolved: the `null` pattern applies to
  both the reference and `Value`).
* *`Nullable<struct union>`*: `n is null` ⇒ `n.HasValue == false || n.GetValueOrDefault().Value == null`.

The output value of the `null` constant pattern is its input value.

Diagnostic `ERR_UnionMatchingWrongPattern` (CS9372):
*"An expression of type '{0}' cannot be handled by this pattern, see additional errors at this location."*

**List patterns** originally always failed over unions (`Value` is `object`); resolution:
*"No longer the case. Extension blocks can enable list pattern scenarios for union types by adding
missing APIs for `object`."*

### 4.4 Union exhaustiveness

> A union type is assumed to be "exhausted" by its case types. This means that a `switch` expression is
> exhaustive if it handles all of a union's case types

```csharp
var name = pet switch
{
    Dog dog => ...,
    Cat cat => ...,
    // No warning about non-exhaustive switch
};
```

No discard/`var` arm is required.

### 4.5 Nullability

* The default null state of a union's `Value` is "maybe null" if the default null state of **any** case
  type is "maybe null"; otherwise "not null". (The earlier rule keyed on "none of the case types are
  nullable" was **removed**; analysis now uses the `Value` property's annotations.)
* Calling a creation member (explicitly or via a union conversion) gives `Value` the incoming value's
  null state.
* `HasValue` / `TryGetValue` narrow `Value` to "not null" on the `true` branch, exactly as a direct
  check of `Value` would.
* Even an otherwise-exhaustive union switch warns on unhandled `null` when `Value` is "maybe null":

```csharp
Pet pet = GetNullableDog(); // 'pet.Value' is "maybe null"
var value = pet switch
{
    Dog dog => ...,
    Cat cat => ...,
    // Warning: 'null' not handled
}
```

Post-condition attributes (e.g. `[NotNull]`) do **not** affect this (resolved: "only look at types").

### 4.6 Docs-level summary of exceptions (slightly simplified vs. the speclet)

learn.microsoft.com `language-reference/builtin-types/union` says:

> Three patterns are exceptions to this rule: the discard `_` pattern, the `var` pattern, and the `not`
> pattern apply to the union value itself, not its `Value` property.

and

> Because patterns apply to `Value`, a pattern like `pet is Pet` typically doesn't match, since `Pet` is
> tested against the *contents* of the union, not the union itself.

The docs omit the list pattern and the untyped property/positional patterns from that list; the
speclet is the more complete source.

---

## 5. The `closed` contextual modifier

Source: <https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/closed-hierarchies.md>
(champion issue <https://github.com/dotnet/csharplang/issues/9499>) plus
<https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/closed>.

### 5.1 Declaration rules

* `closed` is a **class modifier only**.
* A `closed` class is **implicitly `abstract`**.
* It **cannot** combine with `sealed`, `static`, or an *explicit* `abstract`.
* It **is** compatible with `record class` (`public closed record class GateState;` is the canonical
  example in both the speclet and the docs), with `partial`, with generics, and with nesting.
* It is **not transitive**: *"A class deriving from a closed class is not itself closed unless
  explicitly declared to be."*

Enforced in `SourceMemberContainerTypeSymbol.MakeModifiers`:

```csharp
case TypeKind.Class:
case TypeKind.Submission:
    allowedModifiers |= DeclarationModifiers.Partial | DeclarationModifiers.Sealed | DeclarationModifiers.Abstract
        | DeclarationModifiers.Unsafe | DeclarationModifiers.Safe | DeclarationModifiers.Closed;
    if (!this.IsRecord) { allowedModifiers |= DeclarationModifiers.Static; }
    break;
case TypeKind.Struct:
    allowedModifiers |= DeclarationModifiers.Partial | DeclarationModifiers.ReadOnly
                      | DeclarationModifiers.Unsafe | DeclarationModifiers.Safe;
    …
```
```csharp
if (!modifierErrors && (mods & DeclarationModifiers.Closed) != 0)
{
    if ((mods & (DeclarationModifiers.Sealed | DeclarationModifiers.Static)) != 0)
        diagnostics.Add(ErrorCode.ERR_ClosedSealedStatic, GetFirstLocation(), this);
    if ((mods & DeclarationModifiers.Abstract) != 0)
        diagnostics.Add(ErrorCode.ERR_ClosedExplicitlyAbstract, GetFirstLocation(), this);
}
…
switch (typeKind)
{
    case TypeKind.Interface: mods |= DeclarationModifiers.Abstract; break;
    case TypeKind.Class:
        if ((mods & DeclarationModifiers.Closed) != 0)
            mods |= DeclarationModifiers.Abstract;   // implicitly abstract
        break;
    …
}
```

`DeclarationModifiers.Closed` is granted **only** for `TypeKind.Class` (and `Submission`). So:
**`closed` cannot be applied to a union** (a union is `TypeKind.Struct`), nor to a struct, interface,
enum or delegate.

`ClosedClassesTests.BadTypeKind_01` confirms:

```csharp
closed interface I { }        // error CS0106: The modifier 'closed' is not valid for this item
closed enum E { }             // error CS0106
closed delegate void D();     // error CS0106
closed struct S { }           // error CS0106
class C
{
    closed void M() { }               // error CS0106
    closed int P { get; set; }        // error CS0106
    closed event System.Action E;     // error CS0106
    closed string F;                  // error CS0106
}
```

Interfaces are listed in the speclet's "Optional features" as a possible *future* extension:
> - Interfaces could also be allowed to be closed. The rules would be very similar.

### 5.2 Same-assembly (and same-module) restriction

```csharp
// Assembly 1
public closed class CC { ... } 
public class CO : CC { ... }     // Ok, same assembly

// Assembly 2
public class C1 : CC { ... }     // Error, 'CC' is closed and in a different assembly
public class C2 : CO { ... }     // Ok, 'CO' is not closed
```

> The same restriction applies to modules. A subtype of a `closed` type must be located within the same
> module as the base type.

`ITypeSymbol.IsClosed`'s own XML doc says "outside its containing **module**".

Error: `ERR_ClosedBaseTypeBaseFromOtherAssembly` (CS9382) —
*"'{0}': cannot use a closed type '{1}' from another assembly as a base type."*

### 5.3 Type parameter restriction

> If a generic class directly derives from a closed class, then all of its type parameters must be used
> in the base class specification.

```csharp
closed class C<T> { ... }
class D1<U> : C<U> { ... }   // Ok, 'U' is used in base class
class D2<V> : C<V[]> { ... } // Ok, 'V' is used in base class
class D3<W> : C<int> { ... } // Error, 'W' is not used in base class
```

Error: `ERR_UnderspecifiedClosedSubtype` (CS9383) —
*"'{0}': The type parameter '{1}' must be referenced in the base type '{2}' because the base type is closed."*

Rationale: to guarantee a single generic instantiation of the derived type per instantiation of the
closed base type.

### 5.4 Exhaustiveness over a closed hierarchy

* A switch handling all **direct** descendants exhausts the closed class:
  ```csharp
  CC cc = ...;
  _ = cc switch
  {
      CO co => ...,
      // No warning about non-exhaustive switch
  };
  ```
* Consequently the base type as a *later* case becomes unreachable:
  ```csharp
  _ = cc switch
  {
      CO co => ...,
      CC cc => ..., // Error, case cannot be reached
  };
  ```
* **Impossible generic instantiations need no case.** For `closed class C<T>`, `class D1<U> : C<U>`,
  `class D2<V> : C<V[]>`, a switch over `C<string>` needs only `D1<string>`.
* **Unusable subtypes break exhaustiveness.** If a subtype is inaccessible or constraint-violating at
  the use site, the switch is *not* exhaustive:
  ```cs
  closed class C;
  class D1 : C;
  class Container { protected class D2 : C; }
  int M(C c) => c switch { D1 => 1, /* warning: non-exhaustive. Pattern 'C' is not handled. */ };
  ```
  Likewise for *unspeakable* generic subtypes:
  ```csharp
  int M<X>(C<X> c) => c switch { D1<X> => 1, /* warning: 'C' is not handled */ };
  ```
* **Subtype constraints are not analysed precisely.** Given
  `closed class C<T>; class D1<U1> : C<U1>; class D2<U2> : C<U2> where U2 : struct;`
  a switch over `C<X>` with `X : class` still warns, because the compiler does not prove that no
  `D2<X>` exists.
* **A closed class with no subtypes is not exhausted by an empty switch.** The speclet calls this a
  deliberate "quirk": *"despite 'all 0 subtypes being handled', the language still asks the user to
  handle the base type."*
* **A type parameter constrained to a closed class is treated like the closed class** for exhaustiveness:
  ```cs
  closed class C; class D1 : C; class D2 : C;
  int M1<X>(X x) where X : C => x switch { D1 => 1, D2 => 2 };            // exhaustive
  int M2<X>(X x) where X : C => x switch { D1 => 1, D2 => 2, C => 3 };    // error: 'C' is subsumed
  ```
  Docs add that this holds whether the type parameter is on the method or on the containing type.
* **Nullability**: a switch over `JobStatus?` must also handle `null` to be exhaustive.

**Determining the set of subtypes** (speclet):
> 1) For a given closed type `C`, let `C₀` be its original definition.
> 2) For each subtype declaration `S₀` whose base type has original definition `C₀`, determine if a
>    construction `S` exists which has base type `C`.
> 3) If such an `S` exists, it is included in the *set of subtypes*.

Roslyn's source-side candidate gathering walks the whole global namespace of the source module:

```csharp
internal sealed override ImmutableArray<NamedTypeSymbol> CandidateClosedSubtypeDefinitions
{
    get
    {
        if (!IsClosed) return [];
        if (_lazyClosedSubtypeCandidates.IsDefault)
            ImmutableInterlocked.InterlockedInitialize(ref _lazyClosedSubtypeCandidates, findClosedSubtypes());
        return _lazyClosedSubtypeCandidates;

        ImmutableArray<NamedTypeSymbol> findClosedSubtypes()
        {
            var stack = ArrayBuilder<NamespaceOrTypeSymbol>.GetInstance();
            stack.Add(DeclaringCompilation.SourceModule.GlobalNamespace);
            var subtypes = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            while (!stack.IsEmpty)
            {
                var namespaceOrType = stack.Pop();
                if (namespaceOrType is NamedTypeSymbol namedType)
                {
                    if (namedType.BaseTypeNoUseSiteDiagnostics is { } baseType
                        && baseType.OriginalDefinition.Equals(this, TypeCompareKind.AllIgnoreOptions))
                    {
                        subtypes.Add(namedType);
                    }
                    var nestedTypes = namedType.GetTypeMembers();
                    for (var i = nestedTypes.Length - 1; i >= 0; i--) stack.Add(nestedTypes[i]);
                }
                else { /* push child namespaces and types */ }
            }
            stack.Free();
            return subtypes.ToImmutableAndFree();
        }
    }
}
```

Note this is a **whole-source-module walk** — potentially expensive; it is cached in
`_lazyClosedSubtypeCandidates`.

### 5.5 Interface convertibility restriction

> A closed class is said to have a *sealed hierarchy*, if all its subtypes are either *sealed* or have
> a *sealed hierarchy*. … When a closed class has a *sealed hierarchy*, then an *interface
> convertibility* restriction is introduced.

```cs
var c = new C();
var i = (I)c; // error

closed class C { }
sealed class D1 : C { }
sealed class D2 : C { }
interface I { }
```

The explicit reference conversion `C` → `I` exists only if `I` is among the interfaces implemented by
`C` or any of its subtypes (recursively).

### 5.6 Metadata representation

`System.Private.CoreLib/src/System/Runtime/CompilerServices/IsClosedTypeAttribute.cs` (dotnet/runtime `main`):

```csharp
using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Reserved for use by a compiler for tracking metadata.
    /// This attribute should not be used by developers in source code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class IsClosedTypeAttribute : Attribute
    {
        private Type[] _derivedTypes = Type.EmptyTypes;

        /// <summary>Initializes the attribute.</summary>
        public IsClosedTypeAttribute() { }

        /// <summary>Gets or sets the derived types of the closed type.</summary>
        /// <value>An array of the derived types of the closed type.
        /// A <see langword="null" /> value is normalized to an empty array.</value>
        public Type[] DerivedTypes
        {
            get => _derivedTypes;
            set => _derivedTypes = value ?? Type.EmptyTypes;
        }
    }
}
```

**The shipped attribute differs from the csharplang speclet**, which showed a bare
`public sealed class IsClosedTypeAttribute : Attribute { }` with no members. The runtime version adds
the `DerivedTypes` property, and the compiler populates it. **The runtime + Roslyn source are more
recent and authoritative than the speclet here.**

`SourceNamedTypeSymbol.AddSynthesizedAttributes`:

```csharp
if (IsClosed)
{
    ImmutableArray<KeyValuePair<WellKnownMember, TypedConstant>> namedArguments;
    var derivedTypesProperty = (PropertySymbol)compilation.GetWellKnownTypeMember(
        WellKnownMember.System_Runtime_CompilerServices_IsClosedTypeAttribute__DerivedTypes);
    if (derivedTypesProperty is not null)
    {
        var propertyType = (ArrayTypeSymbol)derivedTypesProperty.Type;
        var derivedTypesConstant = new TypedConstant(
            propertyType,
            CandidateClosedSubtypeDefinitions.SelectAsArray(
                static (subtype, elementType) =>
                    new TypedConstant(elementType, TypedConstantKind.Type, subtype.GetUnboundGenericTypeOrSelf()),
                propertyType.ElementType));
        namedArguments = [new KeyValuePair<WellKnownMember, TypedConstant>(
            WellKnownMember.System_Runtime_CompilerServices_IsClosedTypeAttribute__DerivedTypes,
            derivedTypesConstant)];
    }
    else { namedArguments = default; }

    AddSynthesizedAttribute(ref attributes,
        compilation.TrySynthesizeAttribute(
            WellKnownMember.System_Runtime_CompilerServices_IsClosedTypeAttribute__ctor,
            namedArguments: namedArguments));
}
```

Derived types are recorded as **unbound generic types or the type itself**
(`GetUnboundGenericTypeOrSelf()`).

Validation error `ERR_ClosedBadDerivedTypesProperty` (CS9395):
*"'System.Runtime.CompilerServices.IsClosedTypeAttribute.DerivedTypes' must be an instance property
with public get and set accessors, no parameters, and type 'System.Type[]'."*

### 5.7 Emitted IL for a closed class

`ClosedClassesTests.Symbols_01`, `closed class C { }`:

```
.class private auto ansi abstract beforefieldinit C
    extends [System.Runtime]System.Object
{
    .custom instance void System.Runtime.CompilerServices.IsClosedTypeAttribute::.ctor() = (
        01 00 01 00 54 1d 50 0c 44 65 72 69 76 65 64 54
        79 70 65 73 00 00 00 00
    )                                        // named arg: Type[] DerivedTypes = {}
    .method family hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
            01 00 0d 43 6c 6f 73 65 64 43 6c 61 73 73 65 73
            00 00
        )                                    // "ClosedClasses"
        IL_0000: ldarg.0
        IL_0001: call instance void [System.Runtime]System.Object::.ctor()
        IL_0006: ret
    }
}
```

The class is emitted `abstract`. `Symbols_02` shows **every** constructor gets the attribute:

```csharp
closed class C
{
    public C() { }
    public C(int value) { }
}
// both .ctor()s carry [CompilerFeatureRequired("ClosedClasses")]
```

### 5.8 Attribute filtering — critical for tooling

`ClosedClassesTests.Symbols_01`:

```csharp
var classC = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
Assert.True(classC.IsClosed);
// IsClosedTypeAttribute is filtered out of source and metadata symbols.
Assert.Empty(classC.GetAttributes());

var ctor = classC.Constructors.Single();
// CompilerFeatureRequiredAttribute is filtered out
Assert.Empty(ctor.GetAttributes());

if (module is PEModuleSymbol peModule)
{
    var peType = (PENamedTypeSymbol)classC;
    // Get attributes from metadata without doing any filtering
    AssertEx.SetEqual(["System.Runtime.CompilerServices.IsClosedTypeAttribute(DerivedTypes = {})"],
        GetAttributeStrings(peModule.GetCustomAttributesForToken(peType.Handle)));
}
```

**`IsClosedTypeAttribute` and `CompilerFeatureRequiredAttribute` are invisible through
`ISymbol.GetAttributes()` on both source and metadata symbols.** The only supported way to detect a
closed type is `ITypeSymbol.IsClosed`.

(No equivalent filtering assertion was found for `UnionAttribute`; the union IL dump shows it emitted
on the type, and `PENamedTypeSymbol` reads it via `FindTargetAttribute`. Whether it is filtered from
`GetAttributes()` is listed as an open question.)

### 5.9 Blocking derivation from older compilers

> Closed classes shall not be inherited from languages that do not support closed classes. This is
> accomplished by adding `[CompilerFeatureRequired("ClosedClasses")]` to all constructors of closed
> classes.

```cs
// Consuming assembly, built with an older SDK
class C2 : C1
{
    public C2() { }            // error: 'C1.C1()' requires compiler feature "ClosedClasses"
    public C2() : base(42) { } // error: 'C1.C1(int)' requires compiler feature "ClosedClasses"
}
```

> Note that unlike for the "required members" feature, an ObsoleteAttribute is not emitted in addition
> to the CompilerFeatureRequiredAttribute. Only the latter is emitted.

Multiple features stack:

```cs
closed class C1
{
    public C() { }
    public required string P { get; set; }
}
// Metadata:
class C1
{
    [Obsolete("Types with required members are not supported in this version of your compiler")]
    [CompilerFeatureRequired("RequiredMembers")]
    [CompilerFeatureRequired("ClosedClasses")]
    public C1() { }
}
```

If `CompilerFeatureRequiredAttribute` is missing, compilation fails:
`error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute..ctor'`.

### 5.10 `closed` as an identifier

`ERR_ClosedTypeNameDisallowed` (CS9380): *"Types and aliases cannot be named 'closed'."*
Reported via `reportIfContextual(SyntaxKind.ClosedKeyword, MessageID.IDS_FeatureClosedClasses,
ErrorCode.ERR_ClosedTypeNameDisallowed)` in `SourceMemberContainerSymbol.cs`.

Use `@closed` to keep it as an identifier.

---

## 6. Breaking changes (dotnet/roslyn `docs/compilers/CSharp/Compiler Breaking Changes - DotNet 11.md`)

### `closed` is a contextual keyword in type declaration contexts
***Introduced in Visual Studio 2026 version 18.10***

> In C# 15, a type or alias declaration named `closed` without an `@` escape produces CS9380. In member
> declaration contexts, `closed` is treated as a modifier, so code that previously used `closed` as a
> type name may now be parsed as an incomplete declaration and produce CS1519.

```cs
class @closed { }

class C
{
    closed oldField;      // C# 14: field of type 'closed'; C# 15: parsed as an incomplete declaration
    @closed currentField; // field of type 'closed'
}
```

### `union` is a contextual keyword in type declaration contexts
***Introduced in Visual Studio 2026 version 18.10***

> In C# 15, `union` followed by a type name can be parsed as a union declaration. Code that previously
> used `union` as a type name in a declaration may therefore produce CS9370 instead of declaring a
> field.

```cs
class @union { }

class C
{
    union OldField;      // C# 14: field of type 'union'; C# 15: union declaration
    @union CurrentField; // field of type 'union'
}
```

Related open Roslyn issue found in search but not read in full:
[dotnet/roslyn#83055 — "[C# 15] Making an existing type a custom union type breaks existing pattern
matching"](https://github.com/dotnet/roslyn/issues/83055). Adding `[Union]` to an existing type changes
how patterns over it bind (they begin to unwrap to `Value`), which is a source-breaking change for the
type's consumers.

---

## 7. Complete diagnostic inventory

`src/Compilers/CSharp/Portable/Errors/ErrorCode.cs` and `CSharpResources.resx`:

| Code | Name | Message |
|---|---|---|
| CS9369 | `ERR_ExpressionTreeContainsUnionConversion` | An expression tree may not contain a union conversion. |
| CS9370 | `ERR_UnionDeclarationNeedsCaseTypes` | A union declaration must specify at least one case type. |
| CS9371 | `ERR_NoImplicitConversionToObject` | Cannot convert type '{0}' to 'object' via an implicit reference or boxing conversion |
| CS9372 | `ERR_UnionMatchingWrongPattern` | An expression of type '{0}' cannot be handled by this pattern, see additional errors at this location. |
| CS9373 | `ERR_InstanceFieldInUnion` | Instance fields, auto-properties or field-like events are not permitted in a 'union' declaration. |
| CS9374 | `ERR_InstanceCtorWithOneParameterInUnion` | Explicitly declared public constructors with a single parameter are not permitted in a 'union' declaration. |
| CS9375 | `ERR_UnionConstructorCallsDefaultConstructor` | A constructor declared in a 'union' declaration must have a 'this' initializer that calls a synthesized constructor or an explicitly declared constructor. |
| CS9380 | `ERR_ClosedTypeNameDisallowed` | Types and aliases cannot be named 'closed'. |
| CS9381 | `ERR_ClosedSealedStatic` | '{0}': a closed type cannot be sealed or static |
| CS9382 | `ERR_ClosedBaseTypeBaseFromOtherAssembly` | '{0}': cannot use a closed type '{1}' from another assembly as a base type. |
| CS9383 | `ERR_UnderspecifiedClosedSubtype` | '{0}': The type parameter '{1}' must be referenced in the base type '{2}' because the base type is closed. |
| CS9384 | `ERR_ClosedExplicitlyAbstract` | '{0}': a closed type cannot be marked abstract because it is always implicitly abstract. |
| CS9385 | `ERR_MissingUnionCaseTypes` | A union type must have at least one union creation member. |
| CS9386 | `ERR_MissingUnionValueProperty` | A union member provider type must have an instance 'Value' property of type 'object?' or 'object'. The property must have a public get accessor. |
| CS9387 | `ERR_MemberProviderInUnionDeclaration` | A 'union' declaration cannot use a union member provider interface. |
| CS9395 | `ERR_ClosedBadDerivedTypesProperty` | 'System.Runtime.CompilerServices.IsClosedTypeAttribute.DerivedTypes' must be an instance property with public get and set accessors, no parameters, and type 'System.Type[]'. |
| CS9399 | `ERR_FeatureNotAvailableInVersion15` | (C# 15 language-version gate) |

Reused existing codes: `CS0106` (`ERR_BadMemberFlag`) for `closed` on a non-class;
`CS0261` (`ERR_PartialTypeKindConflict`) for mixed partial kinds — its message was updated to
*"Partial declarations of '{0}' must be all classes, all record classes, all structs, **all unions**,
all record structs, or all interfaces"*; `CS1669` (`ERR_IllegalVarArgs`) for `__arglist` in a case-type
list; `ERR_UnexpectedToken` for anything but a bare type in the case-type list; `CS0656`
(`ERR_MissingPredefinedMember`) when `CompilerFeatureRequiredAttribute` is absent.

---

## 8. Quick answer sheet

| Question | Answer |
|---|---|
| Union declaration syntax | `attributes? struct_modifier* 'partial'? 'union' Ident type_parameter_list? '(' case_types ')' struct_interfaces? constraints* ('{' members '}' \| ';')` |
| Roslyn node | `UnionDeclarationSyntax : TypeDeclarationSyntax`, `SyntaxKind.UnionDeclaration = 9082` |
| Case types live in | `UnionDeclarationSyntax.ParameterList` (`ParameterListSyntax`); each `ParameterSyntax` has `Type` set, `Identifier` missing |
| Type parameters / base list / constraints / body | All present and all supported |
| Union `TypeKind` | `TypeKind.Struct` — **there is no `TypeKind.Union`** |
| Detect a union | `ITypeSymbol.IsUnion` (true for `union` decls *and* `[Union]` types) |
| Case types via API | `ITypeSymbol.UnionCaseTypes` → `ImmutableArray<ITypeSymbol>` |
| Emitted shape | `sealed struct : IUnion` + `[Union]` + `initonly object <Value>k__BackingField` + get-only `object? Value` + one boxing `.ctor` per case type |
| Union generates case types? | **No** — case types are pre-existing types |
| Union: partial / nested / generic | Yes / Yes / Yes |
| Union: record | **No** (`record union` not supported) |
| Union: `ref` modifier | **No** |
| `closed` applies to | Classes only (incl. `record class`); implicitly `abstract`; not `sealed`/`static`/explicit `abstract` |
| `closed` transitive? | **No** |
| `closed` metadata | `[IsClosedTypeAttribute(DerivedTypes = …)]` on the type + `[CompilerFeatureRequired("ClosedClasses")]` on every constructor |
| `closed` via `GetAttributes()` | **Filtered out** — use `ITypeSymbol.IsClosed` |
| Closed subtypes via API | `ITypeSymbol.GetClosedDerivedTypeInfo(ct)` → `ClosedDerivedTypeInfo { ClosedDerivedTypes, IsComplete }`; throws `InvalidOperationException` if not closed |
| Status at .NET 11 GA | Both **stable**, `LanguageVersion.CSharp15` (not `Preview`) |

---

## 9. Open questions / not established

1. **Which parts of the unions speclet were still unimplemented?** learn.microsoft.com (2026-08-14)
   says *"Some features from the proposal specification aren't yet implemented. Those features are
   coming in future previews."* but does not enumerate them, and I found no changelog that does.
   Candidates suggested by the speclet's still-open (unresolved) questions:
   * whether the compiler should error on a `[Union]` type in source that lacks a `Value` property or
     any creation member;
   * whether direct `Value`-property matching (`u is S1 { Value: long }`) should apply union rules;
   * the precise lookup rules for `HasValue` / `TryGetValue` (inheritance? read/write `HasValue`?).
2. **Is `UnionAttribute` filtered out of `ISymbol.GetAttributes()`?** The closed-classes tests assert
   filtering for `IsClosedTypeAttribute` and `CompilerFeatureRequiredAttribute`. I found no equivalent
   assertion for `UnionAttribute`, and the union IL dump shows it present in metadata. Unverified
   either way.
3. **Does a union in *metadata* expose `UnionCaseTypes` correctly?** `PENamedTypeSymbol` detects
   `[Union]` via `FindTargetAttribute`, and the case types would then be derived from the public
   single-parameter constructors, but I did not read the PE-side `UnionCaseTypesNoUseSiteDiagnostics`
   implementation to confirm.
4. **Exact `SyntaxFacts` behaviour** for `UnionKeyword` / `ClosedKeyword`: the SyntaxKind.cs comment
   requires `GetContextualKeywordKinds()`, `IsContextualKeyword`, `GetContextualKeywordKind(string)`
   and `GetText` to be updated, but I did not read `SyntaxFacts.cs` to confirm they were.
5. **`IOperation` surface.** `CommonConversion.IsUnion` exists, but I did not establish whether union
   conversions or union pattern matching introduce any new `OperationKind` or `IOperation` node.
   (The only new `OperationKind` in `PublicAPI.Unshipped.txt` is
   `CollectionExpressionElementsPlaceholder = 129`, unrelated.)
6. **Whether `closed` interacts with `union`** in any way beyond both being C# 15 exhaustiveness
   features. `closed` is rejected on a union (a struct), but I did not check whether a union's case
   types being a closed hierarchy composes for exhaustiveness.
7. **The dotnet/docs breaking-changes page URL.** The learn.microsoft.com link
   `breaking-changes/compiler breaking changes - dotnet 11` 404s under the slugs I tried; I read the
   Roslyn-repo copy instead (`docs/compilers/CSharp/Compiler Breaking Changes - DotNet 11.md`).
8. **VS/SDK version mapping.** The breaking changes note "Introduced in Visual Studio 2026 version
   18.10" for both contextual keywords; the corresponding .NET 11 preview number is not stated there.
   The runtime types landed in .NET 11 Preview 5 per learn.microsoft.com.

---

## 10. Source index

* csharplang unions speclet — <https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/unions.md> (1524 lines, read in full)
* csharplang closed-hierarchies speclet — <https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/closed-hierarchies.md> (364 lines, read in full)
* Roslyn feature status — <https://github.com/dotnet/roslyn/blob/main/docs/Language%20Feature%20Status.md>
* Roslyn `Syntax.xml` — `src/Compilers/CSharp/Portable/Syntax/Syntax.xml`
* Roslyn `SyntaxKind.cs` — `src/Compilers/CSharp/Portable/Syntax/SyntaxKind.cs`
* Roslyn `TypeKind.cs` — `src/Compilers/Core/Portable/Symbols/TypeKind.cs`
* Roslyn `DeclarationKind.cs` — `src/Compilers/CSharp/Portable/Declarations/DeclarationKind.cs`
* Roslyn `EnumConversions.cs` — `src/Compilers/CSharp/Portable/Symbols/EnumConversions.cs`
* Roslyn `ITypeSymbol.cs` — `src/Compilers/Core/Portable/Symbols/ITypeSymbol.cs`
* Roslyn `ClosedDerivedTypeInfo.cs` — `src/Compilers/Core/Portable/Compilation/ClosedDerivedTypeInfo.cs`
* Roslyn `PublicModel/TypeSymbol.cs` — `src/Compilers/CSharp/Portable/Symbols/PublicModel/TypeSymbol.cs`
* Roslyn `SourceMemberContainerSymbol.cs`, `SourceNamedTypeSymbol.cs`, `PENamedTypeSymbol.cs`
* Roslyn `LanguageParser.cs`, `LanguageVersion.cs`, `MessageID.cs`, `ErrorCode.cs`, `CSharpResources.resx`
* Roslyn `WellKnownTypes.cs`, `WellKnownMembers.cs`
* Roslyn `PublicAPI.Unshipped.txt` (Core and CSharp)
* Roslyn tests — `src/Compilers/CSharp/Test/CSharp15/UnionsTests.cs`, `.../ClosedClassesTests.cs`
* Roslyn breaking changes — `docs/compilers/CSharp/Compiler Breaking Changes - DotNet 11.md`
* dotnet/runtime — `src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/{UnionAttribute,IUnion,IsClosedTypeAttribute}.cs`
* learn.microsoft.com — <https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15>
* learn.microsoft.com — <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/union>
* learn.microsoft.com — <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/closed>
* Roslyn issue — <https://github.com/dotnet/roslyn/issues/83055> (union attribute breaks existing pattern matching)
