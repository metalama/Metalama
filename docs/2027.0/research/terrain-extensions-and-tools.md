# Terrain map: Extensions, tooling and introspection

Subsystem scope: `Metalama.Extensions/src/**` (DependencyInjection, DependencyInjection.ServiceLocator,
Metrics, Multicast), `Metalama.Framework/src/Metalama.Extensions.HtmlWriter/**`,
`Metalama.Framework/src/Metalama.Extensions.DiffEngine/**`,
`Metalama.Framework/src/Metalama.Framework.Workspaces/**`,
`Metalama.Framework/src/Metalama.Framework.Introspection/**`,
`Metalama.Framework/src/Metalama.Tool/**`, `Metalama.LinqPad/**`,
`Metalama.Framework/src/Metalama.Framework.Analyzers/**`.

Branch examined: `topic/2027.0/26-09-03-net11-impact` at `acce80e1ab`.
All paths below are repository-relative to `C:/src/Metalama-2027.0/Metalama`.

---

## 0. Executive summary

The subsystem carries almost no direct dependency on the *grammar* of C#. It depends on three abstractions
instead, and every language-shape sensitivity in it is a switch over one of those abstractions:

1. `Metalama.Framework.Code.DeclarationKind` and `Metalama.Framework.Code.TypeKind` (the code model), used by
   Multicast and DependencyInjection.
2. Roslyn's `ISymbol` / `IOperation` / `SyntaxNode` shapes, used by `Metalama.Framework.Analyzers` and by the
   HTML writer's member-path computation.
3. Roslyn's `Classifier.GetClassifiedSpansAsync` classification-type strings, used by the HTML writer.

The exception is `Metalama.Framework.Workspaces`, which is not language-sensitive at all but is the single
most platform-sensitive project in the whole repository: it is the only one that hosts MSBuild, that selects a
.NET SDK at run time, and that references `RoslynMaxVersion` rather than `RoslynApiMinVersion`.

The dominant risk in this subsystem is silence. Nine of the switches listed in section 5 have a `default`,
`_ =>` or fall-through arm that returns a *permissive or empty* answer rather than reporting. The C# 14 wave
demonstrated this exactly: two new enum values (`TypeKind.Extension`, `DeclarationKind.ExtensionBlock`) were
added to the code model and **not one line of this subsystem was changed to account for them**.

---

## 1. Files and types sensitive to the set of C# language constructs

### 1.1 `Metalama.Extensions.Multicast` — the densest concentration

This is the only part of the subsystem that enumerates the C# declaration space exhaustively, and it does so
three times over, in three different vocabularies.

**`Metalama.Extensions/src/Metalama.Extensions.Multicast/MulticastTargets.cs`** (lines 28-123)

A `[Flags]` enumeration of declaration kinds, inherited from PostSharp: `Class`=1, `Struct`=2, `Enum`=4,
`Delegate`=8, `Interface`=16, `Field`=32, `Method`=64, `InstanceConstructor`=128, `StaticConstructor`=256,
`Property`=512, `Event`=1024, `Assembly`=2048, `Parameter`=4096, `ReturnValue`=8192, plus the aggregates
`AnyType` (line 65), `AnyMember` (line 102) and `All` (line 122). `Default = 0` (line 34) means "inherited
from the parent attribute".

This enumeration is **the public surface** of the multicast extension. Adding a member to it is a public API
change; not adding one leaves the new construct unreachable by any multicast aspect. It has no value for an
extension block, and would have none for a union.

**`Metalama.Extensions/src/Metalama.Extensions.Multicast/MulticastTargetsHelper.cs`** (lines 13-68)

```csharp
switch ( declaration.DeclarationKind )
{
    case DeclarationKind.Compilation:   return MulticastTargets.Assembly;      // 17-18
    case DeclarationKind.NamedType:                                            // 20
        switch ( ((INamedType) declaration).TypeKind )
        {
            case TypeKind.Class:     return MulticastTargets.Class;            // 23-24
            case TypeKind.Interface: return MulticastTargets.Interface;        // 26-27
            case TypeKind.Struct:    return MulticastTargets.Struct;           // 29-30
            case TypeKind.Delegate:  return MulticastTargets.Delegate;         // 32-33
            case TypeKind.Enum:      return MulticastTargets.Enum;             // 35-36
        }
        break;                                                                 // 39
    case DeclarationKind.Method:        return MulticastTargets.Method;        // 41-42
    case DeclarationKind.Property:      return MulticastTargets.Property;      // 44-45
    case DeclarationKind.Indexer:       return MulticastTargets.Property;      // 47-48
    case DeclarationKind.Field:         return MulticastTargets.Field;         // 50-51
    case DeclarationKind.Event:         return MulticastTargets.Event;         // 53-54
    case DeclarationKind.Parameter:     ...                                    // 56-58
    case DeclarationKind.Constructor:   ...                                    // 60-64
}
return MulticastTargets.Default;                                               // 67
```

Missing today: `DeclarationKind.ExtensionBlock`, `DeclarationKind.TypeParameter`,
`DeclarationKind.Namespace`, `DeclarationKind.Attribute`, `DeclarationKind.AssemblyReference`,
`DeclarationKind.ManagedResource`, `DeclarationKind.Type`, and `TypeKind.Extension`, `TypeKind.Tuple`,
`TypeKind.Array`, `TypeKind.Pointer`, `TypeKind.FunctionPointer`, `TypeKind.Dynamic`, `TypeKind.Error`.
All of these return `MulticastTargets.Default`, that is, zero. See section 5.1 for why zero is dangerous.

**`Metalama.Extensions/src/Metalama.Extensions.Multicast/MulticastImplementation.cs`**

- Line 166-178, `MatchesTypeKind`: a second, independent `TypeKind` switch with `_ => false` (line 177).
  A named type whose kind is not one of the five listed is never a multicast target and nothing is reported.
- Line 131-153: `switch ( builder )` over `IAspectBuilder<ICompilation>`, `IAspectBuilder<IMethod>`,
  `IAspectBuilder<IHasAccessors>`, `IAspectBuilder<INamedType>`. A builder of any other shape silently
  multicasts to nothing (no `default` arm at all).
- Lines 190-320, `CreateEligibilityRule`: `AcceptClassOrStruct` (line 193-200 and 222-236) hard-codes
  `t.TypeKind is TypeKind.Class or TypeKind.Struct` with the message
  `"{t} is neither a class, struct or record"`. Extension blocks and unions are excluded by construction.
- Lines 329-541, `AddChildAspects` overloads: the walk of the compilation is written as one `if` per
  `MulticastTargets` flag over `c.AllTypes`, `t.MethodsAndAccessors()`, `t.Fields`, `t.Properties`,
  `t.Events`, `m.Parameters`. There is no traversal of `INamedType.ExtensionBlocks`, so members declared
  inside an extension block are never reached by multicasting.

**`Metalama.Extensions/src/Metalama.Extensions.Multicast/MulticastAttributes.cs`** (lines 40-199)

A `[Flags]` enumeration of *modifier* axes: visibility (`Private`, `Protected`, `Internal`,
`InternalAndProtected`, `InternalOrProtected`, `Public`, lines 51-81), scope (`Static`/`Instance`, 86-96),
abstraction (`Abstract`/`NonAbstract`, 101-111), virtuality (`Virtual`/`NonVirtual`, 116-126),
implementation (`Managed`/`NonManaged`, 131-141), literality (`Literal`/`NonLiteral`, 146-156), code
generation (`CompilerGenerated`/`UserGenerated`, 166-176) and parameter direction (`InParameter`,
`OutParameter`, `RefParameter`, 161-191).

**This is the extension point for a new modifier.** A `closed` modifier would need a new pair of flags here
plus a `DoesClosednessMatch` predicate, or it is simply not filterable.

**`Metalama.Extensions/src/Metalama.Extensions.Multicast/MulticastAttributeInfo.cs`**

- Lines 99-118, `IsMatchCore`: `switch ( declaration.DeclarationKind )` with cases `Compilation`,
  `NamedType`, `Parameter`, `Field`, `Method` and a `default` that casts to `IMember` (line 117). An
  `IDeclaration` that is not an `IMember` and is not one of the five listed kinds throws
  `InvalidCastException`. `DeclarationKind.ExtensionBlock` reaches the `default` arm; `IExtensionBlock`
  does not implement `IMember`, so this is an unhandled exception waiting for the day the walk reaches one.
- Lines 174-206, `DoesAccessibilityMatch`: an exhaustive switch over `Accessibility` whose `default`
  **throws** `ArgumentOutOfRangeException` (line 199). This is the one fail-loud switch of the subsystem
  and is the model the rest should follow.
- Lines 322-337, `DoesParameterDirectionMatch`: `RefKind.Out`, `RefKind.Ref`, `_ => InParameter`.
  `RefKind.RefReadOnly` and any future ref kind are silently reported as `in`.
- Lines 339-349, `DoesDeclarationKindMatch`: dead code today (`testDeclarationKind` is `false` at both call
  sites, lines 89 and 133) but a live trap if ever enabled; see section 5.1.

**`Metalama.Extensions/src/Metalama.Extensions.Multicast/EnumExtensions.cs`** (lines 12-16)

```csharp
public static bool HasFlagFast( this MulticastTargets targets, MulticastTargets flag ) => (targets & flag) == flag;
public static bool HasAnyFlag( this MulticastTargets targets, MulticastTargets flag )  => (targets & flag) != 0;
```

`HasFlagFast(anything, 0)` is `true`. This is what turns an unrecognised declaration kind into a permissive
answer rather than a restrictive one.

### 1.2 `Metalama.Extensions.DependencyInjection`

- `Implementation/DefaultDependencyInjectionStrategy.cs` line 47-63: `this.Properties.Kind switch` over
  `DeclarationKind.Field` and `DeclarationKind.Property`, `_ => throw new InvalidOperationException()`
  (line 62). Fail-loud but with no message.
- `Implementation/DefaultDependencyInjectionStrategy.cs` line 127-128, `GetConstructors`:
  `type.Constructors.Where( c => c.InitializerKind != ConstructorInitializerKind.This && !c.IsRecordCopyConstructor() )`.
  Two language-shape concepts, `ConstructorInitializerKind` and the record copy constructor, in one line.
  A new synthesised constructor form (a union case constructor, say) would be pulled into unless excluded here.
- `DependencyProperties.cs` lines 38-42: `if ( kind is not (DeclarationKind.Property or DeclarationKind.Field) ) throw new ArgumentOutOfRangeException`.
- `DependencyInjectionExtensions.cs` line 153-157: `dependencyType switch { INamedType namedType => namedType.Name, _ => throw ... }`;
  line 159: `dependencyType.TypeKind == TypeKind.Interface && baseName[0] == 'I'`; line 164:
  `options.MemberKind == DeclarationKind.Field`.
- `DependencyInjectionExtensions.cs` / `DependencyAttribute.cs` line 62-65 and `IntroduceDependencyAttribute.cs`
  lines 44-45 derive `IsStatic` and `MemberKind` from the template declaration; a new member kind on a
  template surfaces here.
- `Implementation/LazyDependencyInjectionStrategy.cs` line 107 and
  `Metalama.Extensions.DependencyInjection.ServiceLocator/LazyServiceLocatorDependencyInjectionStrategy.cs`
  lines 62 and 127: `Writeability.None` and `ConstructorInitializerKind.This` tests.

### 1.3 `Metalama.Extensions.Metrics`

- `StatementsCountMetricProvider.Visitor.cs`: derives from `SyntaxMetricProvider<T>.BaseVisitor`, which is
  `CSharpSyntaxVisitor<T>` (`Metalama.Framework/src/Metalama.Framework.Sdk/Metrics/SyntaxMetricProvider.cs`
  line 78). Overrides `DefaultVisit` (line 19, tests `node is StatementSyntax` at line 23), `VisitBlock`
  (36), `VisitForStatement` (48), `VisitLabeledStatement` (59), `VisitUnsafeStatement` (61),
  `VisitTryStatement` (63). A new statement node is counted by `DefaultVisit` because it derives from
  `StatementSyntax`; a new *expression* node is not counted, which is correct.
- `SyntaxNodesCountMetricProvider.Visitor.cs` line 44: `DefaultVisit` counts every node, so it is
  automatically correct for new node kinds. It is the only construct-agnostic metric.
- `LinesOfCodeMetricProvider.cs`:
  - Line 153, verbatim: `// TODO: Add support for partial properties (C# 13), events and constructors (C# 14).`
    This is the only C# 14 remark anywhere in the subsystem, and it records a gap that was never closed.
  - Lines 154-187, `GetAllSyntaxReferences`: `switch ( symbol )` with a case for `IMethodSymbol` (partial
    definition plus implementation part) and a `default` that returns `symbol.DeclaringSyntaxReferences`.
    Partial properties, partial events and partial constructors therefore count only one part.
  - Line 197: `token.IsKind( SyntaxKind.OpenBraceToken ) || token.IsKind( SyntaxKind.CloseBraceToken )`.
    The only literal `SyntaxKind` reference in the subsystem outside the analyzers.
- The visitors are marked `[CompileTime]` (`StatementsCountMetricProvider.Visitor.cs` line 16,
  `SyntaxNodesCountMetricProvider.Visitor.cs` line 41), so they are themselves compiled by the compile-time
  pipeline and are therefore subject to `SupportedCSharpVersions`.

### 1.4 `Metalama.Extensions.HtmlWriter`

`Metalama.Framework/src/Metalama.Extensions.HtmlWriter/HtmlCodeWriter.cs`:

- Lines 206-217, `GetMemberTextPair`, the member-path used for the `data-member` attribute:
  ```csharp
  MethodDeclarationSyntax method      => (method, method.Identifier.Text),      // 210
  BaseFieldDeclarationSyntax field    => (field, field.Declaration.Variables[0].Identifier.Text), // 211
  EventDeclarationSyntax @event       => (@event, @event.Identifier.Text),      // 212
  BaseTypeDeclarationSyntax type      => (type, type.Identifier.Text),          // 213
  PropertyDeclarationSyntax property  => (property, property.Identifier.Text),  // 214
  _                                   => (null, null)                          // 215
  ```
  This is the one place in the subsystem that switches on *syntax node types* rather than on the code model.
  `UnionDeclarationSyntax` derives from `TypeDeclarationSyntax` and therefore from
  `BaseTypeDeclarationSyntax`, so it is matched by line 213 and yields the union's identifier: correct
  by inheritance, with no edit. `ExtensionBlockDeclarationSyntax` also derives from `TypeDeclarationSyntax`
  but carries no identifier (see `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml` lines 2083-2103,
  which declares no `Identifier` field), so line 213 contributes an empty segment to the dotted member path.
  `IndexerDeclarationSyntax`, `ConstructorDeclarationSyntax`, `OperatorDeclarationSyntax` and
  `DestructorDeclarationSyntax` fall to line 215 and contribute nothing.
- Lines 34-35: a regular expression that recognises automatic property syntax,
  `@"\{(\s*(private|internal|protected|private protected|protected internal)?\s*[gs]et;\s*){1,2}\}"`.
  An enumeration of accessor modifiers in a regular expression. `file` and any future accessor modifier are
  not listed.
- Line 37: `_cleanReturnStatementRegex = new( @"(?<=^\s*)return(?=\s*[^\;])" )`. A keyword in a regular
  expression.
- Lines 286-305: the C# classification is consumed as an opaque string, split on `;` and `-`, and emitted as
  `cs-<token>` CSS classes (line 301). The strings come from Roslyn's
  `Classifier.GetClassifiedSpansAsync` through
  `Metalama.Framework/src/Metalama.Framework.Engine/Formatting/FormattedCodeWriter.cs` lines 116-149.
  A classification type that Roslyn adds for a new keyword produces a `cs-` class for which no stylesheet
  rule exists. See section 5.4.
- Lines 335-346: `classifiedSpan.Classification switch` over `TextSpanClassification`. Metalama's own
  classification, not the language's; `_ => null` at line 345.

### 1.5 `Metalama.Framework.Analyzers`

This project ships inside `Metalama.Framework` and is the only Roslyn analyzer in the subsystem. It is
language-shape sensitive throughout.

`ImmutabilityContext.cs`:
- Line 260: `type.TypeKind == TypeKind.Error || type is IErrorTypeSymbol` (Roslyn's `TypeKind`, not Metalama's).
- Lines 267-284: an exhaustive list of `SpecialType` intrinsics.
- Line 292: `type.TypeKind is TypeKind.Delegate or TypeKind.Enum or TypeKind.Pointer or TypeKind.FunctionPointer`.
- Line 299: `INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T }`.
- Line 309: `INamedTypeSymbol { IsTupleType: true }`.
- Line 325: `type is IArrayTypeSymbol`.
- Line 336: `type.TypeKind == TypeKind.TypeParameter`.
- Line 348: `type is not INamedTypeSymbol namedType` → `Immutable` (silent pass).
- Line 421: `namedType.IsValueType && containingNamespace.ToDisplayString() == "System"` → `Immutable`.
- Line 440: `namedType.TypeKind == TypeKind.Interface || namedType.IsAbstract`.

`DurabilityContext.cs` mirrors it: lines 218, 224-245, 247, 253, 259, 266, 272, 288, 296, 383.

`DurabilityContext.Expressions.cs`:
- Lines 64-110, `GetExpressionVerdict`: a switch over `IOperation` shapes — `ILiteralOperation`,
  `IDefaultValueOperation`, `IObjectCreationOperation`, `IDelegateCreationOperation`,
  `IAnonymousFunctionOperation`, `IMethodReferenceOperation`, `IParameterReferenceOperation`,
  `IFieldReferenceOperation`, `IPropertyReferenceOperation`, `IThrowOperation`, `ICoalesceOperation`,
  `IConditionalOperation`. Falls through to `new ExpressionVerdict( this.GetVerdict( value.Type ) )` (line 112).
- Lines 119-142, `Unwrap`: the transparent-wrapper list, `IConversionOperation { OperatorMethod: null }` and
  `IParenthesizedOperation`. **This is the extension point for a new expression form.**
- Line 198: `operation is IInstanceReferenceOperation`.

`ImmutableContractAnalyzer.WriteSites.cs` lines 61-70 and `DurableContractAnalyzer.UseSites.cs` lines 80-92:
the registered `OperationKind` list — `SimpleAssignment`, `CompoundAssignment`, `CoalesceAssignment`,
`DeconstructionAssignment`, `Increment`, `Decrement`, `FieldInitializer`, `PropertyInitializer`, `Argument`.
A new assignment or initialization form must be added here or its write sites are not analysed.

`ImmutableContractAnalyzer.WriteSites.cs` line 259-260:
`method.MethodKind == MethodKind.Constructor || (method.MethodKind == MethodKind.PropertySet && method.IsInitOnly)`.

`SymbolFacts.cs`:
- Lines 163-193, `IsAutomaticallyImplemented`: a switch over `PropertyDeclarationSyntax { ExpressionBody: not null }`
  (line 176) and `BasePropertyDeclarationSyntax { AccessorList.Accessors: { Count: > 0 } accessors }` (line 179),
  falling through to `return false` at line 192. The `<remarks>` at lines 154-162 explicitly names the C# 13
  semi-automatic property and explains why it is not handled here.
- Line 82-83: `IFieldSymbol field => !field.IsReadOnly && field.DeclaredAccessibility != Accessibility.Private`.
- Line 221: `definition.TypeKind == TypeKind.Interface || definition.IsAbstract`.

`ImmutableContractAnalyzer.cs` lines 211-274, `AnalyzeMembers`: walks `type.GetMembers()` twice, once for
`IFieldSymbol { IsStatic: false, IsConst: false }` (line 217) and once for
`IPropertySymbol { IsStatic: false, IsAbstract: false, IsExtern: false }` (line 257). A construct whose state
Roslyn does not expose as a field or a property is not examined at all.

`ImmutableContractAnalyzer.TemplateExemption.cs` lines 47-73: `switch ( member )` with `IPropertySymbol`,
`IFieldSymbol` and `default: return false`.

`WellKnownImmutableTypes.cs` lines 253-259 and `WellKnownDurableTypes.cs` lines 133-141 and 218-260 are
hand-maintained tables of Roslyn type names (`Microsoft.CodeAnalysis.SyntaxKind`,
`Microsoft.CodeAnalysis.SyntaxNode`, `Microsoft.CodeAnalysis.SyntaxList\`1`, and so on). A Roslyn type
introduced or renamed in a new version is not in these tables.

### 1.6 `Metalama.Framework.Workspaces` and `Metalama.Framework.Introspection`

Only one language-shape enumeration, and it is a public API surface.

`Metalama.Framework/src/Metalama.Framework.Workspaces/ICompilationSet.cs` and its implementation
`CompilationSet.cs` fix the set of member categories the workspace API exposes:

| Member | `ICompilationSet.cs` | `CompilationSet.cs` |
| --- | --- | --- |
| `Types` | 34 | 26 |
| `Methods` | 39 | 29 |
| `Fields` | 44 | 32 |
| `Properties` | 49 | 35 |
| `FieldsAndProperties` | 54 | 38 |
| `Constructors` | 59 | 41 |
| `Events` | 64 | 44 |

There is no `Indexers`, no `Operators`, no `Finalizers`, no `ExtensionBlocks`, no `Parameters`.

`Metalama.Framework/src/Metalama.Framework.Workspaces/Project.cs` line 85:
`this.Compilation.Types.SelectManyRecursive( t => t.Types, includeRoot: true )`.
Extension blocks are exposed by the code model as `INamedType.ExtensionBlocks`
(`Metalama.Framework/src/Metalama.Framework/Code/INamedType.cs` line 187), a collection distinct from
`INamedType.Types`, so **no extension block is reachable through the workspace API, the introspection model
or the LINQPad schema.**

`Metalama.Framework.Introspection` contains no language-shape switch. Its enumerations
(`IntrospectionTransformationKind.cs` lines 15-66, `IntrospectionChildKinds.cs` lines 77-97,
`IntrospectionAspectRelationship.cs`, `IntrospectionDiagnosticSource.cs`) describe Metalama's own
transformation vocabulary, not the language's. `IntrospectionReferenceDetail.cs` line 136 exposes
`Metalama.Framework.Code.ReferenceKinds`, which is where a new expression form would surface.

### 1.7 `Metalama.LinqPad`

- `SchemaFactory.cs` is entirely reflection-driven (`GetProperties`, lines 241-275; `GetIEnumerable`, lines
  185-199). A new property on `ICompilationSet` or on a code model interface appears in the LINQPad schema
  with no edit. It follows that a construct absent from `ICompilationSet` is absent from LINQPad.
- `FacadeType.cs` lines 30-140: reflection over the public interfaces of the code model. Same property.
- `PropertyComparer.cs` lines 19-32: a hard-coded display ordering by property name
  (`Index`, `Id`, `Severity`, `Position`, `ShortName`, `Name`, `DisplayName`, `FullName`, `_ => 10`).
  Cosmetic only.
- `Permalink.cs` lines 30-38: `try { this._declaration.ToRef().ToSerializableId().ToString(); } catch { serializedReference = null; }`
  with the comment `// This is not implemented everywhere, so skip exceptions.` See section 5.5.
- `linqpad-samples/Inheritance Depth.linq` line 35: `.Where(t => t.TypeKind == TypeKind.Class)`. The only
  language-kind test in the shipped samples.

### 1.8 `Metalama.Tool`

No language-construct sensitivity. `Divorce/DivorceService.cs` copies files listed in a
`TransformedFilesMap` and edits `.csproj` XML. `Program.cs` wires Spectre.Console commands.

### 1.9 `Metalama.Extensions.DiffEngine`

`DiffEngineRunner.cs` (26 lines) is a four-method adapter over the `DiffEngine` package. No language
sensitivity. Its only exposure is the target-framework literals of its props files (section 2.1).

---

## 2. Files and types sensitive to the runtime, the SDK, Roslyn or the host IDE

### 2.1 Target framework literals that must move with the Core flavour

`platform-support.md` states that the extension loader compares target framework names as strings and that
the literals must move when the Core flavour moves. In this subsystem the literals are:

| File | Lines | Content |
| --- | --- | --- |
| `Metalama.Framework/src/Metalama.Extensions.HtmlWriter/MetalamaExtensionAssemblies.props` | 5-9 | `net472` and `net10.0` for `DiffPlex.dll` and the extension |
| `Metalama.Framework/src/Metalama.Extensions.HtmlWriter/build/Metalama.Extensions.HtmlWriter.props` | 4-8 | the same, as package paths |
| `Metalama.Framework/src/Metalama.Extensions.HtmlWriter/buildTransitive/Metalama.Extensions.HtmlWriter.props` | 4-8 | duplicate of the previous file |
| `Metalama.Framework/src/Metalama.Extensions.DiffEngine/MetalamaExtensionAssemblies.props` | 5-11 | `net472` and `net10.0` for `EmptyFiles.dll`, `DiffEngine.dll` and the extension |
| `Metalama.Framework/src/Metalama.Extensions.DiffEngine/build/Metalama.Extensions.DiffEngine.props` | 4-10 | the same |
| `Metalama.Framework/src/Metalama.Extensions.DiffEngine/buildTransitive/Metalama.Extensions.DiffEngine.props` | 4-10 | duplicate of the previous file |

Each is duplicated three times because a project reference, a `build` folder and a `buildTransitive` folder
all need it. There is no shared property.

The matching side of the comparison is
`Metalama.Framework/src/Metalama.Framework.Engine/Options/TargetedAssemblyReference.cs` lines 19-24:

```csharp
private static readonly string _targetFramework =
    RuntimeInformation.FrameworkDescription.StartsWith( ".NET Framework", StringComparison.Ordinal ) ? "net472" : "net10.0";

public bool SatisfiesCurrentProcess
    => (this.TargetRoslynVersion == null || this.TargetRoslynVersion.Equals( RoslynApiVersion.Current.ToVersion() ))
       && (this.TargetFramework == null || this.TargetFramework == _targetFramework);
```

and `Metalama.Framework/src/Metalama.Framework.Engine/Extensibility/ExtensionLoaderBase.cs` line 31, which
repeats the same ternary (its `targetFramework` local is used only in the trace message at line 33; the real
filter is `a.SatisfiesCurrentProcess` at line 36).

### 2.2 Project target frameworks

| Project | File | Line | Value |
| --- | --- | --- | --- |
| `Metalama.Extensions.DependencyInjection` | `.../Metalama.Extensions.DependencyInjection.csproj` | 4 | `netstandard2.0` |
| `Metalama.Extensions.DependencyInjection.ServiceLocator` | `.../*.csproj` | 4 | `netstandard2.0` |
| `Metalama.Extensions.Metrics` | `.../Metalama.Extensions.Metrics.csproj` | 4 | `netstandard2.0` |
| `Metalama.Extensions.Multicast` | `.../Metalama.Extensions.Multicast.csproj` | 4 | `netstandard2.0;net10.0` |
| `Metalama.Extensions.HtmlWriter` | `.../Metalama.Extensions.HtmlWriter.csproj` | 4 | `net472;net10.0` |
| `Metalama.Extensions.DiffEngine` | `.../Metalama.Extensions.DiffEngine.csproj` | 4 | `net472;net10.0` |
| `Metalama.Framework.Introspection` | `.../Metalama.Framework.Introspection.csproj` | 4 | `net472;net10.0` |
| `Metalama.Framework.Workspaces` | `.../Metalama.Framework.Workspaces.csproj` | 18 | `net10.0` (plural element, deliberately) |
| `Metalama.Framework.Analyzers` | `.../Metalama.Framework.Analyzers.csproj` | 4 | `netstandard2.0` |
| `Metalama.Tool` | `.../Metalama.Tool.csproj` | 5 | `net10.0`, with `RollForward=Major` at line 37 |
| `Metalama.LinqPad` | `.../Metalama.LinqPad.csproj` | 6 | `net10.0-windows` |
| test projects | `Metalama.Extensions/src/tests/*/*.csproj` | 4 or 5 | `net10.0` |

### 2.3 Roslyn version bindings

- `Metalama.Framework/src/Metalama.Framework.Analyzers/Metalama.Framework.Analyzers.csproj` lines 16-26:
  a comment stating that the project **must** reference `RoslynApiMinVersion` (5.0.0) and nothing else,
  because one `netstandard2.0` assembly is loaded by every host. `VersionOverride` is deliberately absent.
  Consequence: the analyzer is compiled against Roslyn 5.0 but executes inside a host running Roslyn 5.10 or
  later, so it receives `IOperation` and `ISymbol` graphs containing shapes its own reference assembly does
  not declare. See section 5.3.
- `Metalama.Framework/src/Metalama.Framework.Introspection/Metalama.Framework.Introspection.csproj` line 18:
  `Microsoft.CodeAnalysis.CSharp` with `VersionOverride="$(RoslynApiMinVersion)"`.
  Lines 22-23: `InternalsVisibleTo Include="Metalama.Framework.Engine.5.0.0"` and
  `"Metalama.Framework.Engine.5.10.0"`. These names are the Roslyn variant project names and must be edited
  whenever the variant set changes.
- `Metalama.Framework/src/Metalama.Framework.Workspaces/Metalama.Framework.Workspaces.csproj` lines 51-54
  and 79: `Microsoft.CodeAnalysis.Common`, `.CSharp`, `.CSharp.Workspaces`, `.CSharp.Features` and
  `.Workspaces.MSBuild` all with `VersionOverride="$(RoslynMaxVersion)"`. **This is the only project in the
  subsystem that binds to the maximum Roslyn**, currently `5.10.0-1.26365.3`
  (`Directory.Packages.props` line 30). It therefore already parses the four C# 15 grammar additions today.
- `Metalama.LinqPad/src/Metalama.LinqPad/Metalama.LinqPad.csproj` line 3 imports
  `eng/RoslynVersions/Latest.props`, which imports `Roslyn.5.10.0.props`. Lines 34-37 stamp
  `AssemblyMetadata("Package:Microsoft.CodeAnalysis.Workspaces.MSBuild", "$(RoslynMaxVersion)")` into the
  assembly, which `MetalamaScratchpadDriver.OverrideDriverDependencies` uses to make LINQPad restore the
  matching package.

### 2.4 `Metalama.Framework.Workspaces` — the MSBuild and SDK dependency

`Metalama.Framework/src/Metalama.Framework.Workspaces/MSBuildInitializer.cs` is the single most
platform-sensitive file in the subsystem.

- Lines 33-42: refuses to run if `MSBuildLocator` is already registered or if MSBuild assemblies are loaded.
- Lines 59-68: shells out to `dotnet --list-sdks`, with the comment that `MSBuildLocator` does not find SDKs
  installed by `dotnet-installer.ps1` on Docker.
- Line 70: the SDK-list regular expression
  `@"^(?<version>[0-9]+(?:\.[0-9]+)*(?:-[A-Za-z0-9\.]+)?)\s+\[(?<directory>[^\]]+)\]$"`.
- **Line 83-87, the SDK selection rule:**
  ```csharp
  var highestSdk = sdks
      .Where( i => i.ParsedVersion != null && i.ParsedVersion.Major <= Environment.Version.Major )
      .OrderByDescending( i => i.ParsedVersion )
      .ThenBy( i => i.Version )
      .FirstOrDefault( x => HasMatchingProcessorArchitecture( x.Directory ) );
  ```
  The SDK major version must not exceed the runtime major version. A `net10.0` build of this assembly
  running on a .NET 10 runtime will **refuse the .NET 11 SDK**, and will therefore load a project that
  requires .NET 11 SDK targets with the .NET 10 SDK, or fail with the message at lines 91-94.
- Lines 97-112: reflection into the internal constructor of `Microsoft.Build.Locator.VisualStudioInstance`
  (`string, string, Version, DiscoveryType`). A signature change in `Microsoft.Build.Locator` breaks this at
  run time, with `AssertionFailedException` at line 104.
- Lines 123-154, `HasMatchingProcessorArchitecture`: parses line index 2 of the SDK's `.version` file and
  compares it to `RuntimeInformation.RuntimeIdentifier`. A change in the `.version` file layout would
  silently reject every SDK.

`Metalama.Framework.Workspaces.csproj`:
- Lines 5-17: a comment stating that the MSBuild version, and therefore the runtime version, must match the
  .NET SDK version, that only .NET 10 is supported, and why `TargetFrameworks` is plural for a single value.
- Lines 56-75: three `PackageReference` entries with `ExcludeAssets="runtime"` (`Microsoft.Build`,
  `Microsoft.Build.Framework`, `Microsoft.NET.StringTools`) whose comments record three distinct failures
  caused by shadowing the MSBuild the locator resolves: `TypeLoadException` on `IEventSource5`,
  `MissingMethodException` on `SpanBasedStringBuilder.Equals`.
- Lines 90-98: `_WorkspacesMSBuildAssetTargetFramework`, which falls back to a literal `net9.0` when the
  restored `Microsoft.CodeAnalysis.Workspaces.MSBuild` package has no folder named after the project's own
  target framework. The condition tests the package layout, not a version.
- Lines 115-118: an `<Error>` task that fails the build when the selected folder does not exist, with the
  comment that a folder name matching nothing "would produce a lib folder without the MSBuild build hosts,
  and the package would ship in that state without any error". This is a deliberate fail-loud guard and the
  model the rest of the subsystem lacks.

`Metalama.Framework/src/Metalama.Framework.Workspaces/buildTransitive/Metalama.Framework.Workspaces.targets`
lines 4-7 delete `Microsoft.Build*.dll` from the consumer's output directory for the same reason.

`Metalama.Framework/src/Metalama.Framework.Workspaces/Workspace.cs`:
- Line 254-258, `DotNetRestore`: runs `dotnet restore` in the project directory. The SDK selected is the one
  `global.json` resolves for that directory, not the one `MSBuildInitializer` chose.
- Line 300-301: `properties.Add( "MSBuildEnableWorkloadResolver", "false" )`.
- Lines 309-341: `switch ( Path.GetExtension( path ).ToLowerInvariant() )` over `.csproj`, `.sln`, `.slnf`,
  with `default: throw new ArgumentOutOfRangeException` (line 340). Notably absent: `.slnx`, the XML
  solution format that `Microsoft.VisualStudio.SolutionPersistence` (referenced at line 77 of the csproj)
  exists to read.
- Line 376: `WorkspaceProjectOptions.GetTargetFrameworkFromRoslynProject`, which parses the target framework
  out of the Roslyn project *name* by looking for a trailing parenthesis
  (`WorkspaceProjectOptions.cs` lines 41-54).

The effective language version for a workspace-loaded project comes from
`Metalama.Framework/src/Metalama.Framework.Engine/Options/MSBuildProjectOptions.cs` lines 167-183, which
`WorkspaceProjectOptions` inherits: it reads the `LangVersion` MSBuild property and falls back to
`SupportedCSharpVersions.Latest` when `LanguageVersionFacts.TryParse` fails, with the comment that this
happens when "the IDE runs a lower Roslyn version than the one required by the project".

### 2.5 Host IDE sensitivity

- `Metalama.LinqPad/src/Metalama.LinqPad/MetalamaScratchpadDriver.cs` lines 167-180, `Compile`: uses
  LINQPad's `CompileSource` helper, with the comment "CompileSource is a static helper method to compile C#
  source code using LINQPad's built-in Roslyn libraries". The host, not Metalama, supplies the compiler for
  the typed data context. A host whose Roslyn is older than the C# version used in the generated source
  fails with `AssertionFailedException` at line 178.
- `MetalamaScratchpadDriver.cs` lines 184-201, `InitializeContext`: throws if `Microsoft.Build.dll` is
  present in LINQPad's shadow directory.
- `MetalamaScratchpadDriver.cs` lines 203-214, `OverrideDriverDependencies`: forces LINQPad to restore
  `Metalama.Framework.Workspaces` at the version stamped into the assembly, so the MSBuild build hosts are
  copied to the shadow directory.
- `Metalama.LinqPad/src/Metalama.LinqPad/MetalamaWorkspaceDataContext.cs` lines 38-50: translates an
  architecture mismatch into a message naming `RuntimeInformation.ProcessArchitecture`, because LINQPad
  ships x86, x64 and Arm64 builds.
- `Metalama.LinqPad/src/Metalama.LinqPad/Metalama.LinqPad.csproj` line 6: `net10.0-windows` with
  `UseWpf` (line 9) for the connection dialog, and `EnableWindowsTargeting` (line 16).

### 2.6 Everything else

`Metalama.Tool` is `net10.0` with `RollForward=Major`, so it runs unchanged on .NET 11.
`Metalama.Extensions.DiffEngine` and `Metalama.Extensions.HtmlWriter` load through the extension mechanism
and are therefore governed entirely by section 2.1.

---

## 3. How the C# 14 wave was absorbed here

**Finding: it was not.** Of the nineteen tracked issues (#1034, #1035, #1036, #1094, #1105, #1108, #1109,
#1110, #1111, #1112, #1113, #1114, #1115, #1116, #1127, #1131, #1143, #1159, #1160), the commits produced by
those issues touched **zero files** in this subsystem. The verification is a walk of every commit whose
message names one of those issues, intersected with the subsystem's paths; the intersection is empty.

The subsystem was touched exactly once by the C# 14 work, and indirectly:

**`18f7ed78d0` "Deprecate DeclarationKind.Operator and DeclarationKind.Finalizer" (2026-02-02)**

```diff
--- a/Metalama.Extensions/src/Metalama.Extensions.Multicast/MulticastTargetsHelper.cs
+++ b/Metalama.Extensions/src/Metalama.Extensions.Multicast/MulticastTargetsHelper.cs
@@ -38,8 +38,6 @@ public static MulticastTargets GetMulticastTargets( IDeclaration declaration )
-            case DeclarationKind.Finalizer:
-            case DeclarationKind.Operator:
             case DeclarationKind.Method:
                 return MulticastTargets.Method;
```

That commit added `[Obsolete( ..., error: true )]` to `DeclarationKind.Finalizer` and
`DeclarationKind.Operator` (`Metalama.Framework/src/Metalama.Framework/Code/DeclarationKind.cs` lines 88-95).
Because `error: true` turns the use into a compile error, the switch in the Multicast extension had to be
edited. The edit was mechanical: delete the two arms.

Contrast that with the two *additive* changes of the same wave:

- **`88667a5265` "#1138 First-class support for tuple types"** added `TypeKind.Extension` and `TypeKind.Tuple`
  to `Metalama.Framework/src/Metalama.Framework/Code/TypeKind.cs` (lines 80-88 today).
- **`7df11b077c` "Adding DeclarationKind.ExtensionBlock and tuning MetaApi for consistency"** added
  `DeclarationKind.ExtensionBlock` (`DeclarationKind.cs` line 116).

Neither commit touched anything in this subsystem, and neither produced any compiler diagnostic here, because
adding a value to an enumeration does not break a `switch` that has a `default` or a `_ =>` arm.

`TypeKind.Extension` is handled in about twenty places in `Metalama.Framework.Engine` and
`Metalama.Framework` (`EligibilityExtensions.cs` 787 and 796, `EligibilityRuleFactory.cs` 47 and 121,
`IntroduceMemberAdvice.cs` 197, `NamedTypeBuilder.cs` 52, `CompilationElementVisitor.cs` 48,
`TypeVisitor.cs` 24, `ContextualSyntaxGenerator.cs` 142 and 167, `StructuralDeclarationComparer.cs` 693,
`MetaApi.cs` 205, and others). It is handled in **zero** places in Extensions, Workspaces, Introspection,
LinqPad, Tool or Analyzers.

### The pattern to expect for C# 15

1. The code model gains the new values. Whoever adds them is not obliged to visit this subsystem, and the
   compiler will not tell them to.
2. The one thing that would force an edit is a `[Obsolete(..., error: true)]` on an existing member, which is
   how the operator and finalizer fold reached `MulticastTargetsHelper`.
3. Otherwise the subsystem keeps compiling and starts producing quietly incomplete answers: multicast
   aspects that never reach the construct, metrics that count it as an opaque node, an HTML member path with
   an empty segment, a workspace `Types` collection that omits it, a LINQPad schema that never shows it.

There is one additive C# 14 change that did leave a written trace here, and it is a `TODO` rather than an
implementation: `Metalama.Extensions/src/Metalama.Extensions.Metrics/LinesOfCodeMetricProvider.cs` line 153,
`// TODO: Add support for partial properties (C# 13), events and constructors (C# 14).`

---

## 4. Extension points, by kind of language addition

### 4.1 A new kind of type declaration (`union`)

| What must change | Where | Effect if not changed |
| --- | --- | --- |
| `MulticastTargets` gains a `Union` flag, and `AnyType` and `All` are updated | `Metalama.Extensions/src/Metalama.Extensions.Multicast/MulticastTargets.cs` 28-123 | no multicast aspect can name a union as a target |
| `GetMulticastTargets` gains `case TypeKind.Union` | `.../MulticastTargetsHelper.cs` 20-39 | returns `MulticastTargets.Default` (0); see 5.1 |
| `MatchesTypeKind` gains an arm | `.../MulticastImplementation.cs` 170-178 | `_ => false`: a union is silently never a target |
| `AcceptClassOrStruct` and the eligibility messages | `.../MulticastImplementation.cs` 193-200, 222-236 | a union is declared ineligible with the message "is neither a class, struct or record" |
| `GetMemberTextPair` — nothing to do | `HtmlCodeWriter.cs` 213 | already correct: `UnionDeclarationSyntax` derives from `BaseTypeDeclarationSyntax` and has an `Identifier` field (`Syntax-5.10.0.xml` 1961-1963) |
| The immutability and durability type rules | `ImmutabilityContext.cs` 250-450, `DurabilityContext.cs` 209-400 | a union falls through to the closing rules; whether that answer is right depends on whether Roslyn reports its cases as fields |
| `ICompilationSet` / `CompilationSet` — only if unions are not `INamedType` | `ICompilationSet.cs` 34, `CompilationSet.cs` 26, `Project.cs` 85 | a union is invisible to Workspaces, Introspection and LINQPad, exactly as an extension block is today |

### 4.2 A new modifier (`closed`)

| What must change | Where |
| --- | --- |
| `MulticastAttributes` gains a `Closed`/`NonClosed` pair and an `AnyClosedness` aggregate, and `All` is updated | `.../MulticastAttributes.cs` 40-199 |
| `DoMemberOrNamedTypeAttributesMatch` gains a `DoesClosednessMatch` call | `.../MulticastAttributeInfo.cs` 152-156 |
| A new predicate beside `DoesAbstractionMatch` and `DoesVirtualityMatch` | `.../MulticastAttributeInfo.cs` 284-320 |
| The automatic-property regular expression, if the modifier can appear on an accessor | `HtmlCodeWriter.cs` 34-35 |
| The immutability rule for an abstract or interface type, if `closed` changes what may implement a type | `ImmutabilityContext.cs` 440, `SymbolFacts.cs` 221 |

A modifier is invisible to `MulticastAttributes` today, so the failure is under-filtering: an aspect that
asks for non-closed members gets closed ones too, silently.

### 4.3 A new expression form (`unsafe(expr)`)

| What must change | Where |
| --- | --- |
| `Unwrap`, if the new operation is a transparent wrapper | `Metalama.Framework/src/Metalama.Framework.Analyzers/DurabilityContext.Expressions.cs` 119-142 |
| `GetExpressionVerdict`, if the new form changes what a value reaches | `.../DurabilityContext.Expressions.cs` 64-113 |
| The registered `OperationKind` set, if the form is itself an assignment or an initialization | `DurableContractAnalyzer.UseSites.cs` 80-92, `ImmutableContractAnalyzer.WriteSites.cs` 61-70 |
| `Descendants`, used by the closure walk | `DurabilityContext.Expressions.cs` 316 |
| `Metalama.Framework.Code.ReferenceKinds`, surfaced by introspection | `IntrospectionReferenceDetail.cs` 136 (consumer only) |

`unsafe(expr)` is `UnsafeExpressionSyntax : ExpressionSyntax`
(`eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml` lines 496-506). The statement metric is unaffected
(it counts `StatementSyntax` only, `StatementsCountMetricProvider.Visitor.cs` line 23); the node metric
counts it automatically (`SyntaxNodesCountMetricProvider.Visitor.cs` line 44).

### 4.4 A new collection-expression element (`with(...)`)

`WithElementSyntax : CollectionElementSyntax` (`Syntax-5.10.0.xml` lines 816-822) carries an
`ArgumentListSyntax`.

| What must change | Where |
| --- | --- |
| Nothing in the metrics: the arguments are children and `DefaultVisit` recurses | `SyntaxNodesCountMetricProvider.Visitor.cs` 44-54, `StatementsCountMetricProvider.Visitor.cs` 19-34 |
| `AnalyzeArgument`, if Roslyn reports the arguments of a `with(...)` element as `OperationKind.Argument` | `DurableContractAnalyzer.UseSites.cs` 92, `ImmutableContractAnalyzer.WriteSites.cs` 70 |
| `GetExpressionVerdict`, if a collection expression containing a `with(...)` element retains what it is given | `DurabilityContext.Expressions.cs` 64-113 |

The realistic risk is that the collection-expression operation shape changes and the argument analysis stops
firing for the arguments inside `with(...)`, which is silent.

### 4.5 A new optional field on an existing statement (labeled `break` and `continue`)

`BreakStatementSyntax` and `ContinueStatementSyntax` gain an optional `Name` of type `IdentifierNameSyntax`.

| What must change | Where |
| --- | --- |
| Nothing structural | `StatementsCountMetricProvider.Visitor.cs` — a `BreakStatementSyntax` is a `StatementSyntax` and is counted at line 25 whether or not it carries a name |
| Nothing structural | `SyntaxNodesCountMetricProvider.Visitor.cs` — the extra `IdentifierNameSyntax` child is counted, so the node count of existing code is unchanged and of new code is one larger |
| `LinesOfCodeMetricProvider.ComputeForSyntaxNode` — no change; it walks `DescendantTokens()` | `LinesOfCodeMetricProvider.cs` 189-239 |
| Possibly `VisitLabeledStatement` | `StatementsCountMetricProvider.Visitor.cs` 59, which unwraps a label and returns the inner count. A labeled `break` is not a `LabeledStatementSyntax`, so nothing changes |
| The HTML classification | Roslyn will classify the name; the `cs-label-name` class is emitted with no stylesheet rule (section 5.4) |

This is the least invasive of the five, and the only one that costs no edit anywhere in the subsystem.

---

## 5. Where the subsystem would silently do the wrong thing

Ordered by how far the wrong answer travels.

### 5.1 `MulticastTargets.Default` is zero, and `HasFlagFast(x, 0)` is `true`

`MulticastTargetsHelper.GetMulticastTargets` returns `MulticastTargets.Default` (= 0) for any declaration
kind it does not recognise (`MulticastTargetsHelper.cs` line 67). Zero is then consumed by two predicates
with opposite semantics:

- `MulticastAttributeInfo.IsMatch` line 84 uses `HasAnyFlag`, and `(x & 0) != 0` is `false`, so an
  unrecognised kind is **excluded** whenever the attribute names any target.
- `MulticastAttributeInfo.DoesDeclarationKindMatch` line 348 uses `HasFlagFast`, and `(x & 0) == 0` is
  `true`, so an unrecognised kind **matches every filter**.

The second predicate is not reached today because `testDeclarationKind` is `false` at both call sites
(`MulticastAttributeInfo.cs` lines 89 and 133), which makes it dead code. Enabling it, or writing a third
caller, turns "I do not know this kind" into "this kind matches everything". The value `0` doing double duty
as "inherited from the parent attribute" (`MulticastTargets.cs` line 34) and as "unrecognised" is the root
cause, and it will not be revealed by any test that only exercises known kinds.

### 5.2 `MatchesTypeKind` returns `false` for anything new

`MulticastImplementation.cs` line 177, `_ => false`. An extension block today, a union tomorrow, is simply
never a multicast target. No diagnostic, no trace, no test failure: the aspect is applied to fewer
declarations than the user asked for and the output compiles.

The same shape appears at `MulticastImplementation.cs` lines 131-153, where `switch ( builder )` has no
`default` at all: an aspect builder of an unhandled shape multicasts to nothing.

### 5.3 The analyzer is compiled against Roslyn 5.0 and executes against Roslyn 5.10 or later

`Metalama.Framework.Analyzers.csproj` lines 16-26 fix the reference at `RoslynApiMinVersion`. The analyzer
therefore receives operation graphs built by a newer Roslyn and matches them against a `switch` written
against an older reference assembly. Two consequences:

- `DurabilityContext.Expressions.GetExpressionVerdict` (lines 64-110) falls through to
  `this.GetVerdict( value.Type )` for any operation shape it does not know, and
  `DurabilityContext.GetVerdict` returns `DurabilityVerdict.Durable` when the type is `null`
  (`DurabilityContext.cs` lines 188-193). An operation whose `Type` is `null`, which is what an unknown or
  invalid expression form frequently produces, is therefore **declared durable**.
- `ImmutabilityContext.GetVerdictCore` line 348, `if ( type is not INamedTypeSymbol namedType ) return ImmutabilityVerdict.Immutable;`
  is the same shape: an unrecognised type symbol is declared immutable.

Both are silent passes on a correctness analyzer. `ImmutabilityContext.cs` line 253 and
`DurabilityContext.cs` line 213 make the same choice explicitly and defend it in a comment
("Silence is preferable to a chain that was cut short and would mislead"), which is right for a depth cut-off
and is not obviously right for an unrecognised construct.

### 5.4 A new Roslyn classification type produces an unstyled HTML token

`HtmlCodeWriter.cs` lines 296-303 split `classifiedSpan.CSharpClassification` and emit `cs-<token>` classes
without validating the token against any list. The values come from Roslyn's
`Classifier.GetClassifiedSpansAsync` through `FormattedCodeWriter.cs` lines 116-149. When Roslyn adds a
classification for `union`, `closed`, or a labeled break target, the class is emitted, no stylesheet rule
matches it, and the token renders in the default colour. Nothing reports it.

There is one guard: the golden HTML baseline at
`Metalama.Extensions/src/tests/Metalama.Extensions.DependencyInjection.AspectTests/Html/EarlyRequired_Html.cs.html`,
which asserts the exact `cs-` class sequence for one file. It covers only the constructs that file uses
(`using`, `namespace`, `class`, `void`, `public`, `readonly`, `override`, `dynamic`, `return`). The
stylesheet it is compared against is generated by
`Metalama.Framework/src/Metalama.Testing.AspectTesting/HtmlGenerationTestRunner.cs` lines 24-70 and styles
only the `cr-` (Metalama classification) and `diag-` classes, never the `cs-` ones.

### 5.5 `Permalink.Format` swallows every exception

`Metalama.LinqPad/src/Metalama.LinqPad/Permalink.cs` lines 30-38:

```csharp
try { serializedReference = this._declaration.ToRef().ToSerializableId().ToString(); }
catch { /* This is not implemented everywhere, so skip exceptions. */ serializedReference = null; }
```

A declaration kind for which `ToSerializableId` is not implemented produces no permalink and no message.
The user sees a row with an empty link column and cannot tell it from a row for which a link was never
expected.

### 5.6 `LinesOfCodeMetricProvider` under-counts partial members

`LinesOfCodeMetricProvider.cs` lines 154-187: only `IMethodSymbol` aggregates its partial definition and
implementation parts. A partial property (C# 13), a partial event or a partial constructor (C# 14) returns
`symbol.DeclaringSyntaxReferences` from the `default` arm, which for a partial member is one part only. The
metric is silently low. The `TODO` at line 153 records this and it was never acted on.

### 5.7 The workspace API omits whole constructs

`ICompilationSet` exposes seven member categories (section 1.6) and `Project.Types` (line 85) recurses only
through `INamedType.Types`. Extension blocks live in `INamedType.ExtensionBlocks`
(`INamedType.cs` line 187) and are unreachable. A query such as "all public methods" over a workspace
therefore returns a wrong answer for any project that uses extension blocks, and the LINQPad schema, being
generated by reflection over the same interfaces (`SchemaFactory.cs` lines 127-183), shows no sign that
anything is missing.

### 5.8 `MSBuildInitializer` chooses an SDK older than the project needs

`MSBuildInitializer.cs` line 84, `i.ParsedVersion.Major <= Environment.Version.Major`. On a machine with the
.NET 10 runtime and both the .NET 10 and .NET 11 SDKs, the .NET 11 SDK is filtered out. The workspace then
evaluates a project that requires .NET 11 SDK targets with the .NET 10 SDK. MSBuild reports the failure as
workspace diagnostics, which `Workspace.LoadProjectSetCoreAsync` lines 357-369 log but do not throw on; the
comment at line 354 says "Throw an exception upon failure because otherwise it's too difficult to diagnose",
but the code below it only logs. `Metalama.Framework.Workspaces` is `net10.0` with no `RollForward`
declared, so this is reachable as soon as a project targets `net11.0`.

### 5.9 The extension loader is an exact string and version match with no report

`TargetedAssemblyReference.SatisfiesCurrentProcess` (lines 22-24) compares the target framework by string
equality and the Roslyn version by `Version.Equals`. A `MetalamaExtensionAssembly` whose
`TargetFramework` metadata says `net10.0` while the process runs a Core flavour named something else is
simply not returned by `ExtensionLoaderBase.GetExtensionAssemblyPaths` (line 35-37). The HTML writer and the
diff engine then do not load, and the only symptom is that HTML output is not produced and the diff tool does
not launch. This is the same class of failure that `platform-support.md` opens with, applied to extensions
rather than to the payload.

### 5.10 `Metalama.Framework.Analyzers` well-known tables drift

`WellKnownDurableTypes.cs` lines 133-141 and 218-260 and `WellKnownImmutableTypes.cs` lines 253-259 name
Roslyn types as strings (`Microsoft.CodeAnalysis.SyntaxKind`, `Microsoft.CodeAnalysis.SyntaxList\`1`, and so
on). A type that Roslyn adds or renames simply does not match, and the analyzer falls through to its closing
rule, which is `Durable` or `NotImmutable` depending on the table. There is a guard for user-declared names
(`DurableContractAnalyzer.UseSites.cs` lines 99-118, diagnostic `LAMA0879`, "A declared durable type name
matches no type"), but it applies only to names declared through the `MetalamaDurableType` and
`MetalamaNonDurableType` MSBuild items, never to the built-in tables.

### 5.11 The `.slnx` gap

`Workspace.cs` lines 309-341 accept `.csproj`, `.sln` and `.slnf` and throw on anything else. The project
references `Microsoft.VisualStudio.SolutionPersistence`
(`Metalama.Framework.Workspaces.csproj` line 77), which is the library that reads `.slnx`. This is a
fail-loud gap rather than a silent one, but it will bite as soon as the .NET 11 SDK makes `.slnx` the
default solution format.

---

## 6. Test coverage in this subsystem, and what it does not cover

| Suite | Path | What it asserts |
| --- | --- | --- |
| Multicast aspect tests | `Metalama.Extensions/src/tests/Metalama.Extensions.Multicast.AspectTests` | filters by accessibility, member kind and name; no test uses an extension block, a tuple type or any C# 14 construct |
| DependencyInjection aspect tests | `Metalama.Extensions/src/tests/Metalama.Extensions.DependencyInjection.AspectTests` | includes the single golden HTML baseline `Html/EarlyRequired_Html.cs.html` |
| Metrics unit tests | `Metalama.Extensions/src/tests/Metalama.Extensions.Metrics.UnitTests` | `AddMetricsTests.cs`, `LinesOfCodeTests.cs`, `StatementNumberTests.cs` |
| Packaging tests | `Metalama.Extensions/src/tests/Metalama.Extensions.PackagingTests` | consumes the packages from a `net10.0` project |
| LINQPad tests | `Metalama.LinqPad/src/tests/Metalama.LinqPad.Tests` | `FacadeObjectTests.cs`, `PropertyFormatterTests.cs`, `SchemaTests.cs` |

`SchemaTests.SchemaWithoutWorkspace` (lines 34-45) only writes the schema to the test output; it asserts
nothing. `SchemaTests.SchemaWithWorkspace` (line 47) is
`[Fact( Skip = "Cannot get MSBuildLocator to work." )]`. **There is therefore no automated test anywhere in
the repository that loads a project through `MSBuildWorkspace`,** which is the component with the deepest
SDK and MSBuild coupling in the subsystem.

`Metalama.Framework.Analyzers.Tests` exists (referenced by `InternalsVisibleTo` at
`Metalama.Framework.Analyzers.csproj` line 33) and includes `ImmutableTableCorrespondenceTests` and
`ImmutableTemplateExemptionTests`, both named in code comments as the guards against table drift.

---

## 7. Ordered list of edits a C# 15 wave would require here

1. `Metalama.Extensions/src/Metalama.Extensions.Multicast/MulticastTargets.cs` — a `Union` flag, if unions
   are to be multicast targets. Public API change.
2. `Metalama.Extensions/src/Metalama.Extensions.Multicast/MulticastTargetsHelper.cs` lines 20-39 and 67 —
   a `TypeKind.Union` arm, a `DeclarationKind.ExtensionBlock` arm, and a decision about what the fall-through
   should return. Consider making the fall-through report rather than return zero.
3. `Metalama.Extensions/src/Metalama.Extensions.Multicast/MulticastImplementation.cs` lines 170-178 and
   193-236 — the second `TypeKind` switch and the eligibility rules.
4. `Metalama.Extensions/src/Metalama.Extensions.Multicast/MulticastAttributes.cs` — a `Closed`/`NonClosed`
   pair, if the `closed` modifier is to be filterable.
5. `Metalama.Framework/src/Metalama.Framework.Analyzers/DurabilityContext.Expressions.cs` lines 119-142 —
   `unsafe(expr)` in `Unwrap`, once Roslyn's operation shape for it is known.
6. `Metalama.Extensions/src/Metalama.Extensions.Metrics/LinesOfCodeMetricProvider.cs` lines 153-187 — close
   the standing `TODO` before adding to it.
7. `Metalama.Framework/src/Metalama.Framework.Workspaces/MSBuildInitializer.cs` line 84 — decide whether a
   .NET 11 SDK may be used from a .NET 10 runtime, and whether the project should declare `RollForward`.
8. `Metalama.Framework/src/Metalama.Framework.Workspaces/Workspace.cs` lines 326-337 — `.slnx`.
9. `Metalama.Framework/src/Metalama.Framework.Introspection/Metalama.Framework.Introspection.csproj`
   lines 22-23 — the `InternalsVisibleTo` variant names, whenever the Roslyn variant set changes.
10. The twelve target framework literals of section 2.1, whenever the Core flavour moves.
11. `Metalama.Framework/src/Metalama.Framework.Workspaces/ICompilationSet.cs` and `CompilationSet.cs` —
    only if extension blocks or unions are to become visible to Workspaces, Introspection and LINQPad.
    This is the largest single gap in the subsystem and it predates C# 15.
