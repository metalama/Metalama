# Terrain map: public code model (`Metalama.Framework/src/Metalama.Framework/Code/**`)

Subsystem scope: `C:/src/Metalama-2027.0/Metalama/Metalama.Framework/src/Metalama.Framework/Code/`
including `Code/Collections/`, `Code/DeclarationBuilders/`, `Code/Types/`, `Code/Comparers/`,
`Code/Invokers/` and `Code/SyntaxBuilders/`.

Project file: `Metalama.Framework/src/Metalama.Framework/Metalama.Framework.csproj`
(`<TargetFrameworks>netstandard2.0;net10.0</TargetFrameworks>`, line 4).

All paths below are relative to
`C:/src/Metalama-2027.0/Metalama/Metalama.Framework/src/Metalama.Framework/Code/`
unless written in full.

---

## 0. Executive summary

This subsystem is the declarative surface of the code model. It is almost entirely interface
declarations and enumerations, plus a small number of extension-method files that contain the only
real logic. It has:

- **17 enumerations**, of which 5 describe the shape of the C# language directly
  (`DeclarationKind`, `TypeKind`, `MethodKind`, `OperatorKind`, `RefKind`) and 4 more describe it
  indirectly (`TypeKindConstraint`, `VarianceKind`, `Writeability`, `ConstructorInitializerKind`,
  `FieldKind`, `Accessibility`, `SpecialType`).
- **Exactly 9 places that switch over those enumerations**, listed in section 2. Six of them throw
  on an unknown value; three of them fall through to a default answer and are therefore
  silent-failure sites (section 6).
- **Zero direct dependency on Roslyn, on the .NET runtime version, on the .NET SDK, or on the host
  integrated development environment.** A grep for `LanguageVersion`, `SupportedCSharpVersions`,
  `RuntimeInformation`, `#if NET`, `TargetFramework` across `Code/**` returns nothing. The only
  Roslyn mentions are in prose documentation and in two `object`-typed escape hatches
  (`SourceReference.NodeOrTokenInternal`, `ISourceExpression.AsSyntaxNode`). See section 3.
- **`[InternalImplement]` on `ICompilationElement`** (`ICompilationElement.cs`), which every
  `IDeclaration` and `IType` inherits. Adding members to any code-model interface is therefore not a
  binary-breaking change for users, which is why the C# 14 wave added interface members freely.

---

## 1. Files and types sensitive to the set of C# language constructs

### 1.1 The enumerations that would gain a member

| File | Type | Line of the last member | Would gain a member for |
| --- | --- | --- | --- |
| `DeclarationKind.cs` | `DeclarationKind` | `ExtensionBlock` at line 118 | a new kind of declaration (a `union` is probably `NamedType`, but see 4.1) |
| `TypeKind.cs` | `TypeKind` | `Extension` at line 83, `Tuple` at line 88 | a new kind of type declaration (`union`) |
| `MethodKind.cs` | `MethodKind` | `DelegateInvoke` at line 77 | a new kind of method |
| `OperatorKind.cs` | `OperatorKind` | `CheckedDecrementAssignment` at line 324 | a new operator |
| `OperatorCategory.cs` | `OperatorCategory` | `UnaryAssignment` at line 24 | a new operator arity or form |
| `RefKind.cs` | `RefKind` | `Out` at line 41 | a new parameter or return passing mode |
| `TypeKindConstraint.cs` | `TypeKindConstraint` | `Default` at line 89 | a new generic type-kind constraint |
| `VarianceKind.cs` | `VarianceKind` | `Out` at line 159 of the file | a new variance form |
| `Writeability.cs` | `Writeability` | `All = 3` at line 129 | a new settability form. **Values are explicit and load-bearing**: the file says "IMPORTANT: Do not change values, comparison depends on these" (line 109) |
| `ConstructorInitializerKind.cs` | `ConstructorInitializerKind` | `This` | a new constructor initializer form |
| `FieldKind.cs` | `FieldKind` | `TupleElement` | a new field-like construct (added by the tuple wave, issue #1138) |
| `Accessibility.cs` | `Accessibility` | — | a new access modifier |
| `SpecialType.cs` | `SpecialType` | `Nullable_T` at line 177, then the sentinel `Count` at line 184 | a new well-known base-class-library type. **`Count` must stay last** (comment "Must be last." at line 179), so a new member is inserted before it, which shifts nothing but does change `Count` |
| `ITypeParameter.cs` | `TypeParameterKind` (lines 13-24) | `Method` | a type parameter on a construct that is neither a type nor a method |
| `ReferenceKinds.cs` | `ReferenceKinds` (`[Flags] : long`) | `IsType = 1 << 26` at line 165 | a new syntactic position in which a declaration can be referenced |
| `EnumerableKind.cs` | `EnumerableKind` | `IAsyncEnumerator` | a new iterator-producing interface |
| `ConversionKind.cs`, `ConversionFlags.cs`, `DerivedTypesOptions.cs`, `Comparers/TypeComparison.cs`, `DeclarationOriginKind.cs`, `ExecutionScope.cs`, `Invokers/InvokerOptions.cs`, `AccessibilityFlags.cs`, `RefComparison.cs` | — | not language-shape sensitive |

Observed ordering discipline, from the C# 14 wave (section 5): **new members are appended at the end
of the enumeration; obsolete members keep their ordinal and are marked `[Obsolete(..., true)]`
rather than deleted.** Examples: `DeclarationKind.Finalizer` (line 89) and `DeclarationKind.Operator`
(line 95) both remain as error-obsolete placeholders; `TypeKind.RecordClass` (line 149 of the file,
`TypeKind.cs:31`) and `TypeKind.RecordStruct` likewise; `OperatorKind.Multiply = Multiplication`
(line 124) is an obsolete alias.

### 1.2 The interfaces that would gain a property for a new modifier

Modifier-bearing properties, grouped by the interface that declares them:

| File | Interface | Modifier properties |
| --- | --- | --- |
| `IMemberOrNamedType.cs` | `IMemberOrNamedType` | `Accessibility` (25), `IsAbstract` (27), `IsStatic` (32), `IsSealed` (38), `IsNew` (43), `IsPartial` (101) |
| `IMember.cs` | `IMember` | `IsVirtual` (23), `IsAsync` (28), `IsOverride` (34), `IsExtern` (39), `IsExplicitInterfaceImplementation` (44), `HasImplementation` (52) |
| `INamedType.cs` | `INamedType` | `IsReadOnly` (192), `IsRef` (197), `IsRecord` (202), `HasDefaultConstructor` (51) |
| `IMethod.cs` | `IMethod` | `IsReadOnly` (87), `OperatorKind` (92), `MethodKind` (27) |
| `IField.cs` | `IField` | `FieldKind` (66), `ConstantValue` (46) |
| `IConstructor.cs` | `IConstructor` | `IsPrimary` (28), `InitializerKind` (33) |
| `IParameter.cs` | `IParameter` | `IsParams` (37), `IsThis` (42), `IsReturnParameter` (71) |
| `IHasType.cs` | `IHasType` | `RefKind` (28) |
| `ITypeParameter.cs` | `ITypeParameter` | `TypeKindConstraint` (66), `AllowsRefStruct` (71), `Variance` (76), `IsConstraintNullable` (83), `HasDefaultConstructorConstraint` (88) |
| `IFieldOrPropertyOrIndexer.cs` | `IFieldOrPropertyOrIndexer` | `Writeability` (20) |
| `IDeclaration.cs` | `IDeclaration` | `IsImplicitlyDeclared` (108), `BelongsToCurrentProject` (123) |

The C# 15 `closed` contextual modifier on a type would be a new `bool IsClosed { get; }` on
`INamedType.cs`, mirrored as `new bool IsClosed { get; set; }` on
`DeclarationBuilders/INamedTypeBuilder.cs` (which today declares only `IsPartial` at line 18 and
`BaseType` at line 35).

### 1.3 The interfaces that describe a kind of declaration

`ICompilation`, `IAssembly`, `INamespace`, `INamespaceOrNamedType`, `INamedType`, `IExtensionBlock`,
`ITupleType`, `ITypeParameter`, `IMethod`, `IMethodBase`, `IConstructor`, `IProperty`, `IIndexer`,
`IField`, `ITupleElement`, `IEvent`, `IParameter`, `IAttribute`, `IManagedResource`, and the
grouping interfaces `IMember`, `IMemberOrNamedType`, `IFieldOrProperty`,
`IFieldOrPropertyOrIndexer`, `IPropertyOrIndexer`, `IHasParameters`, `IHasAccessors`, `IHasType`,
`IGeneric`.

Type-shape interfaces live in `Types/`: `IArrayType.cs`, `IPointerType.cs`, `IDynamicType.cs`,
`IFunctionPointerType.cs`. A new *type form* that is not a named type would become a fifth file
here; a new *declaration form* of a named type (a union) is most likely a new `TypeKind` member on
the existing `INamedType`, following the `TypeKind.Extension` precedent.

### 1.4 The collections

`Collections/` mirrors the member kinds one for one:
`IMethodCollection`, `IPropertyCollection`, `IIndexerCollection`, `IFieldCollection`,
`IFieldOrPropertyCollection`, `IEventCollection`, `IConstructorCollection`, `INamedTypeCollection`,
`IExtensionBlockCollection`, `INamespaceCollection`, `IAssemblyCollection`,
`IImplementedInterfaceCollection`, `IMemberCollection<T>`, `IMemberOrNamedTypeCollection<T>`,
`INamedDeclarationCollection<T>`, `IParameterList`, `ITypeParameterList`, `IAttributeCollection`,
`INamedArgumentList`.

`INamedType.cs` exposes these as 17 collection properties (lines 80-187), each with an `All*`
counterpart where inheritance applies. A new kind of member requires a new collection interface, a
new pair of properties on `INamedType`, and a new arm in `DeclarationExtensions.GetMembers` (see
2.4).

### 1.5 The builders

`DeclarationBuilders/` mirrors the declaration kinds:
`INamedTypeBuilder`, `IExtensionBlockBuilder`, `IMethodBuilder`, `IMethodBaseBuilder`,
`IConstructorBuilder`, `IPropertyBuilder`, `IIndexerBuilder`, `IFieldBuilder`, `IEventBuilder`,
`IParameterBuilder`, `ITypeParameterBuilder`, plus the grouping builders
`IMemberOrNamedTypeBuilder`, `IMemberBuilder`, `IFieldOrPropertyBuilder`,
`IFieldOrPropertyOrIndexerBuilder`, `IPropertyOrIndexerBuilder`, `IHasParametersBuilder`,
`IHasTypeBuilder`, `IDeclarationBuilder`, `IParameterBuilderList`, and the value type
`AttributeConstruction`.

Modifier setters live on `IMemberOrNamedTypeBuilder.cs` (lines 116-141:
`Accessibility`, `Name`, `IsStatic`, `IsSealed`, `IsAbstract`, `IsPartial`) and `IMemberBuilder.cs`
(lines 161-166: `IsVirtual`, `IsExtern`).

`INamedTypeBuilder.cs` carries three commented-out `TODO` blocks (lines 20-42) for `IsReadOnly`,
`IsRef` and `PrimaryConstructor`, which is where a `closed` or `union` setter would land.

---

## 2. Every place that switches over those enumerations

Nine sites, exhaustively. Six throw on an unknown value; three do not (marked SILENT).

### 2.1 `DeclarationExtensions.cs:53-93` — `CanContain(DeclarationKind, DeclarationKind)`

The only exhaustive `switch` over `DeclarationKind` in the subsystem. Arms:
`None`/`Attribute`/`AssemblyReference` (57-60), `Compilation` (62-63), `Namespace` (65-66),
`NamedType` (68-69), `ExtensionBlock` (71-73), `Parameter`/`TypeParameter`/`Field` (75-78),
`Constructor` (80-81), `Method` (83-84), `Property`/`Event`/`Indexer` (86-90).

```
default:
    throw new ArgumentOutOfRangeException( nameof(containingDeclarationKind), $"Unexpected value: '{containingDeclarationKind}'." );
```
(lines 91-92). A new `DeclarationKind` that can contain anything must get an arm here, otherwise
`DeclarationExtensions.IsContainedIn` (line 32) throws.

The `ExtensionBlock` arm reads:
```
return containedDeclarationKind is DeclarationKind.Method or DeclarationKind.Indexer
    or DeclarationKind.Property or DeclarationKind.Parameter or DeclarationKind.Attribute or DeclarationKind.TypeParameter;
```
`Indexer` is already listed, so the C# 15 "indexers in extension blocks" feature needs no change
here. `Event` is deliberately absent.

### 2.2 `DeclarationExtensions.cs:105-143` — the `extension( DeclarationKind kind )` block

Five closed-world predicates, all written as `is ... or ...` patterns with no default:

- `IsMember` (105-112): `Event`, `Field`, `Property`, `Indexer`, `Constructor`, `Method`.
- `IsMemberOrNamedType` (120): `IsMember || NamedType || ExtensionBlock`.
- `IsType` (128): `NamedType`, `ExtensionBlock`, `TypeParameter`, `Type`.
- `IsAssembly` (134): `Compilation`, `AssemblyReference`.
- `IsNamedDeclaration` (140-143): `Method`, `Property`, `Field`, `Event`, `Parameter`,
  `TypeParameter`, `NamedType`, `Namespace`.

SILENT. All five return `false` for an unknown kind. `IsNamedDeclaration` is already wrong for
`ExtensionBlock`, `Indexer` and `Constructor` (see 6.1).

### 2.3 `DeclarationExtensions.cs:220-228` — `GetMembers(INamedType, DeclarationKind)`

```
DeclarationKind.Method => namedType.Methods,
DeclarationKind.Field => namedType.Fields,
DeclarationKind.Property => namedType.Properties,
DeclarationKind.Event => namedType.Events,
DeclarationKind.Constructor => namedType.Constructors,
_ => throw new ArgumentOutOfRangeException( nameof(kind) )
```
Note that `DeclarationKind.Indexer` is *already missing*, even though `Indexers` exists on
`INamedType` (line 106). A new member kind would need an arm here too.

### 2.4 `DeclarationExtensions.cs:406-437` — `GetEffectiveAccessibility(IType)`

Switches over the runtime type of `IType`: `IArrayType` (409-411), `IPointerType` (413-414),
`INamedType` (416-431), then
```
default:
    // For dynamic, type parameters, function pointers, etc.
    return Accessibility.Public;
```
SILENT. A new `IType` shape silently gets `Accessibility.Public`.

### 2.5 `GenericExtensions.cs:42-50` — `GetBase(IMemberOrNamedType)`

`NamedType`, `Method`, `Property`, `Event`, `Indexer`, then `_ => null`. SILENT.

### 2.6 `GenericExtensions.cs:56-62` — `GetDefinition(IDeclaration)`

`NamedType or Method or Property or Event or Field or Constructor or Indexer` mapped to
`memberOrNamedType.Definition`, then `_ => declaration`. SILENT: a new member kind would return
itself rather than its generic definition. Note `ExtensionBlock` is *not* listed even though an
extension block is an `IMemberOrNamedType` with a `Definition`.

### 2.7 `GenericExtensions.cs:299-334` — the `switch` inside the generic-instance resolver

Arms for `NamedType` (301), `Method` (306), `Field` (311), `Property` (316), `Event` (321),
`Constructor`+instance (326), `Constructor`+static (331), then
`default: throw new ArgumentOutOfRangeException( nameof(declaration) );` (333-334).
`Indexer` is missing here as well. Also `GenericExtensions.cs:32`
(`IsSelfOrDeclaringTypeGeneric`) enumerates `NamedType or ExtensionBlock or Method`.

### 2.8 `OperatorKindExtensions.cs:22-118` — `GetCategory(OperatorKind)`

The single largest switch in the subsystem: 60 arms mapping every `OperatorKind` to an
`OperatorCategory`, ending with
`_ => throw new ArgumentOutOfRangeException( nameof(operatorKind), operatorKind, null )` (line 117).
A new operator must be added both to `OperatorKind.cs` and here.

### 2.9 `AccessibilityExtensions.cs:27-41` — `ToAccessibilityFlags(Accessibility)`

Six arms plus `_ => throw new ArgumentOutOfRangeException( nameof(accessibility) )` (line 40).

### 2.10 `RefKindExtensions.cs:32-43` — `IsWritable(RefKind)`

Five arms plus `_ => throw new ArgumentOutOfRangeException( nameof(kind) )`.
Two sibling predicates are written as negations and are therefore SILENT:
`IsByRef` = `kind != RefKind.None` (line 23), `IsReadable` = `kind != RefKind.Out` (line 48).

### 2.11 Non-`switch` shape dependencies over `TypeKind`

- `MemberExtensions.cs:56`: `namedType is { TypeKind: TypeKind.Class, IsSealed: false }` inside
  `CanBeImplementedFromOutsideAssembly`.
- `SignatureMatcher.cs:293`: `INamedType { TypeKind: TypeKind.Struct or TypeKind.Class }` in
  `GetParamsElementType`.
- `SignatureMatcher.cs:298`: `TypeKind: TypeKind.Interface` in the same method.
- `TypedConstant.cs:106`: `IsArray => this.Type is { TypeKind: TypeKind.Array }`.
- `TypedConstant.cs:273` and `TypedConstant.cs:471`: `TypeKind.Enum`.

### 2.12 Hand-written closed-world enumerations (not switches, but equivalent)

- `NamedTypeExtensions.cs:40-65` — `MethodsAndAccessors`: iterates `Methods`, then property
  `GetMethod`/`SetMethod`, then event `AddMethod`/`RemoveMethod`. Omits indexer accessors and
  `IEvent.RaiseMethod`. SILENT.
- `NamedTypeExtensions.cs:72-102` — `Members`: `Methods`, `Properties`, `Fields`, `Events`,
  `Indexers`, `Constructors`. A new member kind must be appended here.
- `NamedTypeExtensions.cs:110-140` — `AllMembers`: same, with the `All*` collections.
- `DeclarationExtensions.cs:334-341` — `ContainedChildren`: `ICompilation => Types`,
  `INamespace => Namespaces + Types`, `INamedType => Members() + Types`, `_ => []`. SILENT, and it
  does **not** descend into `INamedType.ExtensionBlocks`, so `ContainedDescendants` misses every
  member declared in an extension block.
- `ReferenceKindsExtension.cs:46-69` — `ToDisplayString`: 24 hand-written `ConsiderKind` calls; see
  6.2.
- `TypedConstant.cs:478-492` — a `SpecialType` switch that maps element types to CLR array types,
  falling back to `object[]`. SILENT but benign.
- `TypedConstant.cs:223-243` — a closed `SpecialType is ... or ...` list of the constant-capable
  primitive types.

---

## 3. Sensitivity to the .NET runtime, the .NET SDK, Roslyn, or the host

**Essentially none, by construction.** This is the most important finding of section 3: the public
code model is a contract assembly with no Roslyn reference.

Verified by grep over `Code/**` for `LanguageVersion`, `SupportedCSharpVersions`, `Roslyn`,
`netstandard`, `RuntimeInformation`, `TargetFramework`, `#if NET`: four hits, all in prose
documentation (`ISourceExpression.cs:13`, `SourceReference.cs:13`, `SourceReference.cs:36`,
`SourceSpan.cs:54`).

The residual couplings are:

1. **`Metalama.Framework/src/Metalama.Framework/Metalama.Framework.csproj:4`** —
   `<TargetFrameworks>netstandard2.0;net10.0</TargetFrameworks>`. This is the .NET-runtime-version
   hotspot for the whole subsystem. It was `netstandard2.0;net8.0` before the PB-2027.0 work.
2. **`Metalama.Framework.csproj:17-33`** — the `InternalsVisibleTo` list encodes the Roslyn variant
   set literally: `Metalama.Framework.Engine.5.0.0`, `Metalama.Framework.Engine.5.10.0`, and the
   same suffixes for five test assemblies. Adding, renaming or removing a Roslyn variant requires
   editing this list. This is the only Roslyn-version-sensitive text in the subsystem.
3. **`SourceReference.cs:29-42`** — `SourceReference(object nodeOrToken, ISourceReferenceImpl)`,
   `NodeOrToken` (obsolete, line 40) and `NodeOrTokenInternal` (line 42) carry a Roslyn
   `SyntaxNode`/`SyntaxToken` as `object`. `SourceReference.Kind` (line 47) returns the Roslyn
   `SyntaxKind` **as a `string`**, through `ISourceReferenceImpl.GetKind`. A new Roslyn syntax kind
   therefore flows through this property without any code change here, and without any validation:
   `Kind` is a stringly-typed, open-world channel. See 6.3.
4. **`ISourceExpression.cs:85`** — `object AsSyntaxNode { get; }`, same pattern.
5. **`SyntaxBuilders/SyntaxBuilder.cs:39-41`** — `CurrentImplementation` resolves
   `ISyntaxBuilderImpl` from `MetalamaExecutionContext`. Every expression and statement produced by
   `ExpressionFactory`, `StatementFactory`, `ExpressionBuilder`, `StatementBuilder`, `ArrayBuilder`,
   `InterpolatedStringBuilder`, `SwitchStatementBuilder` is produced by *parsing a string* in the
   engine. `StatementFactory.Parse` (`SyntaxBuilders/StatementFactory.cs:48`) and
   `ExpressionFactory.Parse` (`SyntaxBuilders/ExpressionFactory.cs:160`) are the two entry points.
   The language version used for that parse is decided in the engine, not here, so C# 15 syntax
   inside a user's `ExpressionFactory.Parse( "unsafe(p)" )` is entirely an engine concern. The
   public surface needs no change.

No file in the subsystem depends on the host integrated development environment, the .NET SDK
version, or MSBuild.

---

## 4. Extension points that would have to change

### 4.1 A NEW kind of type declaration (`union`)

Minimum edit set, in order:

1. `TypeKind.cs` — append a `Union` member after `Tuple` (line 88). Do not reorder.
2. `INamedType.cs` — decide whether a union is an `INamedType` (as `TypeKind.Extension` and
   `TypeKind.Tuple` are) or a new interface. Precedent from C# 14 and from issue #1138 says: derive
   a new interface from `INamedType`, exactly as `IExtensionBlock.cs:11`
   (`public interface IExtensionBlock : INamedType`) and `ITupleType.cs:27`
   (`public interface ITupleType : INamedType`) do. The new interface would live beside them in
   `Code/`, expose its case members as a typed collection, and declare
   `new IRef<IUnionType> ToRef();`.
3. `DeclarationKind.cs` — only if the new type is *not* reachable as `DeclarationKind.NamedType`.
   The precedent is split: `ITupleType` reuses `DeclarationKind.NamedType` (see the doc comment at
   `DeclarationKind.cs:31`), whereas `IExtensionBlock` got its own member
   (`DeclarationKind.cs:118`). Adding a member costs the switch edits of 2.1, 2.2, 2.3, 2.5, 2.6,
   2.7 and roughly 35 files in the engine (see 5.3).
4. `Collections/` — a new `I<X>Collection` if the type owns a new kind of child, plus the paired
   `X`/`AllX` properties on `INamedType.cs` near lines 80-187.
5. `DeclarationBuilders/` — a new `I<X>Builder : INamedTypeBuilder, I<X>`, following
   `IExtensionBlockBuilder.cs:78` verbatim, including the `[InternalImplement]` attribute
   (`IExtensionBlockBuilder.cs:77`).
6. `IDeclarationFactory.cs` — a creation method if the type can be synthesised, following
   `CreateTupleType` (lines 98-120) and its `TypeFactory.cs:137-157` façade.
7. `DeclarationExtensions.cs:53-93` — a `CanContain` arm.
8. `DeclarationExtensions.cs:105-143` — the five predicates.
9. `NamedTypeExtensions.cs:72,110` — `Members` / `AllMembers` if the new type owns members.

### 4.2 A NEW modifier (`closed`)

1. `INamedType.cs` — `bool IsClosed { get; }` beside `IsRecord` (line 202).
2. `DeclarationBuilders/INamedTypeBuilder.cs` — `new bool IsClosed { get; set; }` beside `IsPartial`
   (line 18); the commented-out `IsReadOnly`/`IsRef` block at lines 22-30 shows the intended shape.
3. Nothing else in this subsystem. No switch enumerates modifiers; they are independent booleans.
   This is the cheapest of the five changes.

Precedent: `IMemberOrNamedType.IsPartial` (line 101) plus
`IMemberOrNamedTypeBuilder.IsPartial` (line 141) plus `INamedTypeBuilder.IsPartial` (line 18) is
exactly the three-line shape a modifier takes.

### 4.3 A NEW expression form (`unsafe(expr)`)

**No change in this subsystem.** `IExpression.cs` is `[InternalImplement]` and describes an
expression only by its `Type`, its `RefKind` and its `Value`; it does not model expression syntax.
`ISourceExpression.AsSyntaxNode` (line 85) hands the Roslyn node out as `object`. New expression
forms reach users through `ExpressionFactory.Parse`
(`SyntaxBuilders/ExpressionFactory.cs:160`) and `ExpressionBuilder.AppendVerbatim`
(`SyntaxBuilders/SyntaxBuilder.cs:59`), both of which are string-in, engine-parses.

The only thing to check is whether `ISourceExpression.AsTypedConstant` (line 101) must recognise the
new form; `TypedConstant.CheckAcceptableType` (`TypedConstant.cs:174-...`) is the closed list it
would consult.

### 4.4 A NEW collection-expression element (`with(...)`)

**No change in this subsystem.** Collection expressions are not modelled. The nearest things are:

- `SyntaxBuilders/ArrayBuilder.cs` — builds an array-creation expression from a list of items; it
  emits array-initializer syntax through the engine and has no notion of a collection-expression
  element.
- `SignatureMatcher.cs:282-304` — `GetParamsElementType`, which implements the C# 13
  `params`-collections rules by pattern-matching on `IArrayType`, `System.Span`,
  `System.ReadOnlySpan`, `CollectionBuilderAttribute` and the `IEnumerable`-family interfaces, and
  `SignatureMatcher.cs:307-...` — `GetIterationType`. These encode the *collection* rules of the
  language, not the *collection expression* rules, and would only change if C# 15 changed what may
  be a `params` collection.

### 4.5 A NEW optional field on an existing statement (labelled `break` / `continue`)

**No change in this subsystem.** Statements are modelled only as opaque `IStatement`
(`SyntaxBuilders/IStatement.cs`, `[InternalImplement]`) and `IStatementList`. The one structured
statement builder is `SyntaxBuilders/SwitchStatementBuilder.cs`, which builds `switch` sections
(`AddCase` at lines 57-95, `AddDefault` at lines 103-113, `ToStatement` at line 130) and has no
`break`/`continue` model at all. Labelled `break` reaches users only through
`StatementFactory.Parse` (`SyntaxBuilders/StatementFactory.cs:48`) and
`StatementBuilder.AppendVerbatim`.

`SwitchStatementBuilder.ToStatement` throws
`new InvalidOperationException( "The switch does not have any sections." )` at line 135 when empty;
that is the only validation in the file.

---

## 5. How the C# 14 wave was absorbed here

### 5.1 The commits that touched this subsystem

| Commit | Issue | Files in `Code/**` |
| --- | --- | --- |
| `cdf076ee1a` "#1034 C# 14 extension members: code model." | #1034 | `OperatorKind.cs` (+182 lines), `OperatorKindExtensions.cs` (+97), `OperatorCategory.cs` (+4) |
| `bcdeb3a185` "#1034 C# 14 extension members: code model" | #1034 | new `ITypeExtension.cs`, new `Collections/ITypeExtensionCollection.cs`, `INamedType.cs` (+2), `IParameter.cs` (nullability of `DeclaringMember`), `TypeKind.cs` (+`Extension`) |
| `16cc84ca1d` "Renaming and documenting APIs." | #1034 | `ITypeExtension.cs` → `IExtensionBlock.cs`, `ITypeExtensionCollection.cs` → `IExtensionBlockCollection.cs`, `TypeKind.cs`, `INamedType.cs`, `IParameter.cs` |
| `22697b6ba5` "Add invoker support for extension member implementation methods (#1036)" | #1036 | `IMethod.cs` (+8: `ExtensionImplementationMethod`, now at line 82) |
| `5b121f3c21` "#1116 C# 14 user-defined compound assignment operators: overriding" | #1116 | `OperatorKind.cs`, `OperatorKindExtensions.cs` |
| `787ec4fcd8` "#1110 … #1111 … #1113 … #1112" | #1110-#1113 | `Collections/IEventCollection.cs` (+7: the `this[string name]` indexer, now at line 18) |
| `a9698fa1e8` / `f776fd9af9` "Add C# 14 extension block introduction infrastructure (#1159)" | #1159 | new `DeclarationBuilders/IExtensionBlockBuilder.cs` (48 lines) |
| `7df11b077c` "Adding DeclarationKind.ExtensionBlock and tuning MetaApi for consistency." | #1034 follow-up | `DeclarationKind.cs`, `DeclarationExtensions.cs` |
| `88667a5265` "#1138 First-class support for tuple types" | #1138 (adjacent wave) | new `ITupleType.cs`, `IField.cs` (+9: `FieldKind`), `SpecialType.cs` (+5), `TypeFactory.cs` (+11), `IDeclarationFactory.cs` (+10), `TypeKind.cs` (+`Tuple`) |
| `18f7ed78d0` "Deprecate DeclarationKind.Operator and DeclarationKind.Finalizer" | — | `DeclarationKind.cs` |
| `b69925e37f` "Removing TypeKind.RecordClass and TypeKind.RecordStruct." | — | `TypeKind.cs` |

### 5.2 The pattern, stated

1. **Model the construct as an interface derived from the closest existing one.**
   `ITypeExtension : INamedType`, later renamed `IExtensionBlock : INamedType`
   (`IExtensionBlock.cs:11`). `ITupleType : INamedType` (`ITupleType.cs:27`).
   `ITupleElement : IField` (`ITupleElement.cs:68`). Never a new root.
2. **Append one enumeration member, at the end.** `TypeKind.Extension` (line 83), then
   `TypeKind.Tuple` (line 88); `DeclarationKind.ExtensionBlock` (line 118). Obsolete members keep
   their ordinals.
3. **Add a paired collection.** `IExtensionBlockCollection.cs` with domain-specific query methods
   (`OfReceivingType(IType)` line 18, `OfReceivingType(Type)` line 23), mirroring
   `IDeclarationFactory`'s `IType`/reflection-`Type` overload pairs.
4. **Add one property on `INamedType`.** `ExtensionBlocks` (line 187).
5. **Widen an existing member's contract rather than adding a new one where possible.**
   `IParameter.DeclaringMember` went from `IHasParameters` to `IHasParameters?`
   (`IParameter.cs:48`) so that the extension-block receiver parameter could have no declaring
   member. This is a *source*-breaking change under nullable reference types, taken deliberately.
6. **Add a builder that inherits and restricts.** `IExtensionBlockBuilder : INamedTypeBuilder,
   IExtensionBlock` (line 78), with `[InternalImplement]` (line 77) and the restrictions documented
   in `<remarks>` as a bullet list (lines 62-74) rather than expressed in the type system.
7. **Enumerate the new operators exhaustively and re-sort the mapping switch by category.**
   `cdf076ee1a` rewrote `GetCategory` from an alphabetical list to a category-grouped list with
   section comments (`OperatorKindExtensions.cs:27,32,39,45,51,56,64,67,75,80,84,90,103,107,113`),
   and added `OperatorCategory.BinaryAssignment` and `OperatorCategory.UnaryAssignment`
   (`OperatorCategory.cs:23-24`) at the same time.
8. **Rename before shipping.** The public names were revised once (`16cc84ca1d`) before release:
   `ITypeExtension` → `IExtensionBlock`, `ExtendedType` → `ReceiverType`, `ExtensionParameter` →
   `ReceiverParameter`, `Extensions` → `ExtensionBlocks`.
9. **Fix the consumers in the same commit.** `7df11b077c`, which added one `DeclarationKind`
   member, touched 35 files: `RefTargetKind.cs`, `RefTargetKindExtensions.cs`,
   `CompilationElementVisitor.cs`, `CompilationElementVisitor{T}.cs`,
   `MetalamaStringFormatterImpl.cs`, `DocumentationIdHelper.Parser.cs`, `MetaApi.cs` (119 lines),
   `TemplateExpansionContext.cs` (162 lines), and ten `Override*Transformation.cs` files.

### 5.3 The cost curve, measured

- A new modifier: 2 to 3 lines, 3 files, no switch.
- A new interface derived from an existing one: about 30 lines, 2 to 4 files, no switch.
- A new `TypeKind` member: 1 line here, plus the `TypeKind` consumers in the engine.
- A new `DeclarationKind` member: 1 line here, 6 switch edits in this subsystem, about 35 files in
  the engine.
- A new `OperatorKind` member: 2 lines (the enum and `GetCategory`), plus the engine's
  operator-syntax tables.

---

## 6. Places that would silently do the wrong thing

Ranked by how likely a C# 15 construct is to reach them.

### 6.1 `DeclarationExtensions.IsNamedDeclaration` is already wrong (`DeclarationExtensions.cs:140-143`)

```
public bool IsNamedDeclaration
    => kind is DeclarationKind.Method or DeclarationKind.Property or DeclarationKind.Field
        or DeclarationKind.Event or DeclarationKind.Parameter or DeclarationKind.TypeParameter
        or DeclarationKind.NamedType or DeclarationKind.Namespace;
```
`DeclarationKind.ExtensionBlock`, `DeclarationKind.Indexer` and `DeclarationKind.Constructor` are
missing, although `IExtensionBlock` is an `INamedType` (hence an `INamedDeclaration`), and both
`IIndexer` and `IConstructor` are `INamedDeclaration`. The property returns `false` with no
diagnostic. This is a C# 14 wave miss and the template for what a C# 15 miss looks like.

### 6.2 `ReferenceKindsExtension.ToDisplayString` degrades to an integer, and truncates

`ReferenceKindsExtension.cs:71-75`:
```
if ( consideredKinds != kinds )
{
    // If we forgot something, fallback to the integer value, this is at least deterministic.
    return ((int) kinds).ToString( CultureInfo.InvariantCulture );
}
```
Two defects. First, a new `ReferenceKinds` member that is not added to the 24 `ConsiderKind` calls
at lines 46-69 makes *every* combination containing it render as a number, not just the new flag.
Second, `ReferenceKinds` is declared `: long` (`ReferenceKinds.cs:16`) and the fallback casts to
`int`, so any flag at or above `1 << 31` truncates. The current maximum is `1 << 26`
(`ReferenceKinds.cs:165`), leaving five bits of headroom. A new syntactic reference position for a
C# 15 construct (a reference inside a `union` case list, or inside a `with(...)` element) lands
here.

### 6.3 `SourceReference.Kind` is a stringly-typed open channel (`SourceReference.cs:47`)

`public string Kind => this._sourceReferenceImpl.GetKind( this );` returns the Roslyn `SyntaxKind`
name as a string. User code that compares it against a literal (`"ClassDeclaration"`) keeps
compiling and silently stops matching when the construct becomes a `UnionDeclaration`. There is no
enumeration to extend and no compiler assistance.

### 6.4 `DeclarationExtensions.ContainedChildren` does not descend into extension blocks (`DeclarationExtensions.cs:334-341`)

```
INamedType type => type.Members().Concat<IDeclaration>( type.Types ),
_ => []
```
`INamedType.ExtensionBlocks` (line 187) is not visited, so `ContainedDescendants` and
`ContainedDescendantsAndSelf` (lines 350, 359) silently omit every member declared inside an
extension block. Any fabric or validator that walks the compilation with these methods misses them.
C# 15 indexers in extension blocks inherit this hole. The `_ => []` arm makes a new container type
silently empty.

### 6.5 `NamedTypeExtensions.MethodsAndAccessors` omits indexer accessors and event raisers (`NamedTypeExtensions.cs:40-65`)

Iterates `Methods`, then property `GetMethod`/`SetMethod`, then event `AddMethod`/`RemoveMethod`.
`INamedType.Indexers` (line 106) is skipped entirely, and `IEvent.RaiseMethod` (`IEvent.cs:57`) is
skipped. Since C# 15 adds indexers inside extension blocks, the set of indexer accessors this method
misses grows.

### 6.6 `DeclarationExtensions.GetMembers` throws for `DeclarationKind.Indexer` (`DeclarationExtensions.cs:220-228`)

Not silent, but wrong: the method's own documentation (lines 214-218) lists only Method, Field,
Property, Event and Constructor, so the omission is deliberate-looking. A caller passing
`DeclarationKind.Indexer` gets `ArgumentOutOfRangeException` rather than `namedType.Indexers`.

### 6.7 `GenericExtensions.GetDefinition` returns the declaration unchanged (`GenericExtensions.cs:56-62`)

`_ => declaration`. For a declaration kind not in the list, callers receive a generic *instance*
where they asked for the generic *definition*, then compare it for equality against a definition and
get `false`. `DeclarationKind.ExtensionBlock` is not in the list today. `IsContainedIn`
(`DeclarationExtensions.cs:34`) calls `GetDefinition` on both operands, so this feeds a wrong
containment answer.

### 6.8 `GenericExtensions.GetBase` returns null (`GenericExtensions.cs:42-50`)

`_ => null` for a new member kind that can be overridden. A validator that walks the override chain
stops one link early and reports nothing.

### 6.9 `DeclarationExtensions.GetEffectiveAccessibility(IType)` returns `Public` (`DeclarationExtensions.cs:433-435`)

A new `IType` shape whose components are `private` is reported as effectively `public`. In an
architecture-validation aspect this is a false negative, which is the dangerous direction.

### 6.10 `RefKindExtensions.IsByRef` and `IsReadable` are negations (`RefKindExtensions.cs:23,48`)

`IsByRef` = `kind != RefKind.None` and `IsReadable` = `kind != RefKind.Out`. A new `RefKind` is
silently classified as by-reference and readable. `IsWritable` (line 32) is the only one of the
three that throws.

### 6.11 There is no test that any of these switches is exhaustive

A search for `Enum.GetValues` combined with `DeclarationKind`, `TypeKind`, `OperatorKind`,
`MethodKind`, `SpecialType` or `RefKind` across `Metalama.Framework/src` and
`Metalama.Framework/tests` returns nothing. Nothing detects a missing arm at build time or at test
time. The C# compiler does not help either, because every one of these switches has a `default` or
a `_` arm.

### 6.12 Downstream consumer worth flagging (outside the subsystem)

`C:/src/Metalama-2027.0/Metalama.Premium/src/Metalama.Extensions.Validation/ReferenceValidationContext.cs:124-134`
— `GetInboundGranularity(DeclarationKind)` switches over `Constructor`, `Event`, `Method`, `Field`,
`Property`, `Indexer`, `Compilation`, `AssemblyReference`, `Namespace`, `NamedType`, `Parameter`,
`TypeParameter`, `Attribute`, then
`_ => throw new ArgumentOutOfRangeException( nameof(kind), $"Unexpected kind: '{kind}'" )`.
`DeclarationKind.ExtensionBlock` is missing, so a validated reference to an extension block throws
today. This is the clearest evidence of how far a `DeclarationKind` addition propagates.

---

## 7. Checklist for the C# 15 wave, derived from the above

For **`union` (new type declaration)**:
`TypeKind.cs` (append), new `IUnionType.cs` (derive from `INamedType`), possibly
`DeclarationKind.cs` (append) and then 2.1, 2.2, 2.3, 2.5, 2.6, 2.7,
`Collections/IUnionCaseCollection.cs`, `INamedType.cs` (paired properties),
`DeclarationBuilders/IUnionTypeBuilder.cs`, `IDeclarationFactory.cs` + `TypeFactory.cs` if
synthesisable, `NamedTypeExtensions.cs:72,110`, `DeclarationExtensions.cs:334`.

For **`closed` (new modifier)**:
`INamedType.cs` (beside line 202), `DeclarationBuilders/INamedTypeBuilder.cs` (beside line 18).

For **indexers in extension blocks**:
nothing new is required (`DeclarationExtensions.cs:71-73` already lists
`DeclarationKind.Indexer`, and `INamedType.Indexers` is inherited by `IExtensionBlock`), but
`NamedTypeExtensions.MethodsAndAccessors` (6.5) and `DeclarationExtensions.ContainedChildren` (6.4)
should be fixed so the new indexers are actually enumerated.

For **`unsafe(expr)`, `with(...)` elements, labelled `break`/`continue`**:
no change in this subsystem; verify only that `SignatureMatcher.GetParamsElementType`
(`SignatureMatcher.cs:282`) still matches the language rules for `params` collections.

Cross-cutting, regardless of feature: add an exhaustiveness test over
`DeclarationKind`, `TypeKind`, `MethodKind`, `OperatorKind` and `RefKind` that drives every
`switch` listed in section 2 and asserts it does not throw and does not fall to the default arm
(6.11).
