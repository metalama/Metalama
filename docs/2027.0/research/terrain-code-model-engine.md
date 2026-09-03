# Terrain map: code model implementation (`Metalama.Framework.Engine/CodeModel/**`)

Subsystem owner note: this map covers `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/**` (324 `.cs`
files) plus the two directly coupled satellites the task assigned to this subsystem:
`Metalama.Framework.Engine/SerializableIds/**` and the shared kind taxonomies in
`Metalama.Framework.Engine/Utilities/Roslyn/` that the code model dispatches on. Every path below is relative to
`C:/src/Metalama-2027.0/Metalama/` unless stated otherwise. Line numbers are from the working tree at
`topic/2027.0/26-09-03-net11-impact`.

The one-sentence summary: **the code model is a set of hand-written total functions over three closed
enumerations — Roslyn `SymbolKind`, Roslyn `TypeKind` and Metalama `DeclarationKind`/`TypeKind` — and a new C#
construct is absorbed by widening those functions in a fixed order.** The order is settled: it is exactly what
issue #1034 and issue #1159 did for extension blocks.

---

## Part 0. The shape of the subsystem, so the rest of the map reads

Four parallel families implement the same public interfaces (`INamedType`, `IMethod`, …):

| Family | Directory | Backing |
| --- | --- | --- |
| `Source*` | `CodeModel/Source/` | a Roslyn `ISymbol` |
| `Introduced*` | `CodeModel/Introductions/Introduced/` | a `*BuilderData` produced by an aspect |
| `*Builder` | `CodeModel/Introductions/Builders/` | the mutable form an aspect writes to |
| `Constructed*` | `CodeModel/Introductions/ConstructedTypes/`, `CodeModel/Source/ConstructedTypes/` | array / pointer / tuple over the two above |

Named types use a **facade + implementation pair**: `Source/SourceNamedType.cs` (facade, tracks usage through
`OnUsingDeclaration`) delegates to `Source/SourceNamedTypeImpl.cs` (implementation). `ExtensionBlock` /
`ExtensionBlockImpl` is the second instance of that pair and is the template for any further one.

References are the identity layer: `CodeModel/References/` holds `SymbolRef<T>`, `IntroducedRef<T>`,
`ConstructedTypeRef<T>`, `SyntaxRef<T>`, `DeclarationIdRef<T>`, `TypeIdRef<T>`. A reference is typed by the
*interface* (`IRef<INamedType>` versus `IRef<IExtensionBlock>`), so every new declaration kind that gets its own
public interface forces a new arm in the kind→interface tables.

---

## Part 1. Files and types sensitive to the set of C# language constructs

### 1.1 The closed enumerations themselves

| File | Lines | Note |
| --- | --- | --- |
| `Metalama.Framework/src/Metalama.Framework/Code/TypeKind.cs` | 17–89 | 15 members. `Extension` (83) and `Tuple` (88) were appended by the C# 14 wave. `RecordClass` (30) and `RecordStruct` (68) are `[Obsolete(…, true)]` **placeholders kept so the numeric values of later members do not shift** — proof that ordinal values are load-bearing. |
| `Metalama.Framework/src/Metalama.Framework/Code/DeclarationKind.cs` | 18–119 | `ExtensionBlock` (118) appended last, after `Type` (113). `Finalizer` (89) and `Operator` (95) are obsolete placeholders, same reason. |
| `CodeModel/References/RefTargetKind.cs` | 7–31 | Carries the comment `// WARNING! These values are serialized as strings and stored in compiled dlls. Do not rename.` `NamedType` (28) and `ExtensionBlock` (29) sit before `PrimaryConstructor` (30). |
| `CodeModel/Helpers/PropertyKind.cs` | 10–28 | `Default`, `Auto`, `SemiAuto`. `SemiAuto` is the C# 14 `field` keyword. |
| `CodeModel/Helpers/ModifierCategories.cs` | 10–24 | `[Flags]`, values 1…1024, `All` on line 23 is a hand-maintained union. **A new modifier category costs a new bit and an edit to `All`.** |
| `CodeModel/GenericContexts/GenericContextKind.cs` | — | `Null` / `Symbol` / `Introduced`; three-arm switches on it appear in `DeclarationFactory.Symbols.cs:349–371` and `SymbolRef.cs:104–124`. |
| `CodeModel/InternalSpecialType.cs` | 7–12 | one member; unlikely to move. |

Derived taxonomies (the "is this kind in that family" helpers), all in
`Metalama.Framework.Engine/Utilities/Roslyn/`:

- `TypeKindExtensions.cs:22–24` — `TypeKind.IsNamedType => kind is Class or Struct or Interface or Enum or Delegate`.
  **`Extension` and `Tuple` are deliberately not in it**, and `CompilationElementVisitor` relies on that
  (`{ IsNamedType: true }` is one arm, `TypeKind.Extension` another). A new named-type-like kind (a C# 15 `union`)
  must decide which side of this predicate it lands on, and the decision propagates everywhere.
- `TypeKindExtensions.cs:29` — `IsClassOrStruct`.
- `SymbolKindExtensions.cs:22–24` `IsType`, `:30` `IsMember`, `:36–38` `IsNonNamedType`.
- `SyntaxKindExtensions.cs:33–35` `IsTypeDeclaration`, `:41` `IsBaseTypeDeclaration`, `:47` `IsLambdaExpression`,
  `:53` `IsBaseFieldDeclaration`, `:59–63` `IsLiteralExpression`, `:69–72` `IsAccessorDeclaration`,
  `:78–81` `IsBaseMethodDeclaration`, `:86–88` `IsPropertyOrEventDeclaration`, `:94` `IsSimpleName`,
  `:100` `IsRecordDeclaration`, `:106–108` `IsName`, `:114` `IsNamespaceDeclaration`.
  **`IsTypeDeclaration` was never widened for `SyntaxKind.ExtensionBlockDeclaration`** — see §5.1.

### 1.2 Symbol → kind mapping (the single entry point)

`CodeModel/Helpers/DeclarationExtensions.cs:40–65`, `GetDeclarationKind( this ISymbol, CompilationContext )`.
This is *the* funnel; almost every dispatch downstream consumes its result.

```
SymbolKind.NamedType when symbol is INamedTypeSymbol { IsExtension: true } => DeclarationKind.ExtensionBlock,   // line 44
SymbolKind.NamedType                                                        => DeclarationKind.NamedType,        // line 45
…
{ IsNonNamedType: true }                                                    => DeclarationKind.Type,             // line 61
_ => throw new ArgumentException( $"Unexpected symbol: {symbol.GetType().Name}.", nameof(symbol) )               // line 64
```

Note line 44 is the *only* place in the code model that reads `INamedTypeSymbol.IsExtension` directly rather than
through `IsExtensionSafe()`.

### 1.3 Kind → implementation dispatch (the factory)

`CodeModel/Factories/DeclarationFactory.Symbols.cs`:

- `GetIType( ITypeSymbol, … )` — **lines 117–132**. Six arms on `SymbolKind`;
  `_ => throw new NotImplementedException( $"Types of kind {typeSymbol.Kind} are not implemented." )` at 130.
- `GetNamedType( INamedTypeSymbol, … )` — **lines 159–199**. The three-way construction at 185–198:
  `IsExtensionSafe()` → `new ExtensionBlock`, `IsTupleType` → `new TupleType`, else `new SourceNamedType`.
  This is the exact insertion point for a further named-type-like kind.
- `GetCompilationElementCore` — **lines 376–505**. Twelve `case SymbolKind.*` arms, each with an inner
  `RefTargetKind` switch; `default: throw new AssertionFailedException( $"Don't know how to resolve a '{mappedSymbol.Kind}'." )`
  at 503.
- `GetCompilationElement` — lines 310–338, with the early `return null` for `Local`/`Label`/`ErrorType` at 316–322.
- `MakeNullableType` — lines 649–709; line 659 special-cases the extension block (`throw` on `ToNullable`).
- `GetTupleTypeFromSymbol` — lines 507ff, keyed by underlying type + element names (issue #1844).

`CodeModel/Factories/DeclarationFactory.Builders.cs`:

- `GetDeclaration( DeclarationBuilderData, … )` — **lines 203–258**, a `DeclarationKind` switch.
  Line 219 is the canonical C# 14 widening:
  `ContainingDeclaration.DeclarationKind: DeclarationKind.NamedType or DeclarationKind.ExtensionBlock`.
  Line 251 is the `DeclarationKind.ExtensionBlock` arm. `_ => throw new AssertionFailedException` at 256.
- One `Get<Kind>` factory method per declaration kind: `GetAttribute` 70, `GetParameter` 76, `GetTypeParameter` 88,
  `GetMethod` 99, `GetAccessor` 107, `GetConstructor` 122, `GetField` 134, `GetProperty` 142, `GetIndexer` 150,
  `GetEvent` 158, `GetNamedType` 166, `GetNamespace` 180, **`GetExtensionBlock` 188** (the one #1159 added).

### 1.4 Kind → reference dispatch

- `CodeModel/References/RefFactory.cs:101–137`, `FromAnySymbol`. A 15-arm `DeclarationKind` switch producing
  `SymbolRef<TInterface>`; `DeclarationKind.ExtensionBlock => new SymbolRef<IExtensionBlock>` at 114–118;
  `_ => throw new ArgumentOutOfRangeException()` at 133. The tuple short-circuit is at 103–106.
- `CodeModel/References/RefExtensions.cs:140–154`, `ToRef( this INamedTypeSymbol, RefFactory )` — the
  extension/tuple/named three-way again. `ToExtensionBlockRef` 156, `ToTupleTypeRef` 159.
- `CodeModel/References/RefExtensions.cs:163–170`, `ToRef( this ITypeSymbol, … )` — **does not** replicate the
  extension check; see §5.2.
- `CodeModel/References/SymbolNormalizer.cs:95–103` — `GetCanonicalSymbolInfo`, four `SymbolKind` arms plus a
  permissive `_ =>`. The named-type overload at 39–69 short-circuits `IsExtensionSafe()` to
  `GenericContext.Empty` (line 44–47).
- `CodeModel/References/FullRef.cs:165–188`, `ApplyRefKind` — a `(RefTargetKind, SymbolKind)` tuple switch;
  line 170 pairs `RefTargetKind.NamedType or RefTargetKind.ExtensionBlock` with `SymbolKind.NamedType`.
  `FullRef.GetAttributes` at 74–120 is a `RefTargetKind` switch.
- `CodeModel/References/RefTargetKindExtensions.cs:31–55`, `ToDeclarationKind` — one arm per `RefTargetKind`,
  `RefTargetKind.ExtensionBlock => DeclarationKind.ExtensionBlock` at 52,
  `_ => throw new ArgumentOutOfRangeException` at 54. Lines 15–29 map Roslyn `MethodKind` → `RefTargetKind`.
- `CodeModel/References/SymbolRef.cs:60–94` — three `Invariant.Assert` blocks that encode the whole taxonomy.
  The one at 81–86 is the tightest: for any `SymbolKind.NamedType` the interface type **must** be exactly
  `IExtensionBlock`, `ITupleType` or `INamedType`. This is `#if DEBUG`-free (unlike line 60–64) and will fire on a
  new named-type-like kind that reaches a reference through the wrong constructor.
- `CodeModel/References/SymbolRef.Strategy.cs` — the member enumeration strategy:
  `GetMembersOfName` 99–131, `GetMembers` 133–175, then the per-kind predicates
  `IsEventSymbolIncluded` 215, `IsValidConstructor` 218, `IsValidField` 222, `IsValidIndexer` 224,
  `IsValidMethod` 227–237, `IsValidNamespace` 239, `IsValidProperty` 272, `IsValidNamedType` 276/279–313,
  **`IsValidExtensionBlock` 315–316**, and the table `GetSymbolPredicate` **318–330**.
  Line 298–301 inside `IsValidNamedType` is the exclusion that keeps extension blocks out of `INamedType.Types`:
  `if ( symbol.IsExtensionSafe() ) { return false; }`.
- `CodeModel/References/RefExtensions.cs:90–136`, `GetPossibleDeclarationInterfaceTypes` — a **`#if DEBUG`-only**
  table mapping `RefTargetKind` and `DeclarationKind` to the legal `IRef<T>` interface types.
  `RefTargetKind.ExtensionBlock => [typeof(IExtensionBlock)]` at 112, `DeclarationKind.ExtensionBlock` at 118,
  `_ => throw new ArgumentOutOfRangeException` at 131 and 134. It is consumed by the asserts in
  `SymbolRef.cs:62`, `IntroducedRef.cs:107` and `DeclarationFactory.Builders.cs:212`. **Being `#if DEBUG`, this
  whole guard is absent from release builds.**

### 1.5 Visitors (kind → virtual method)

- `CodeModel/Visitors/CompilationElementVisitor.cs` — `Visit` at 18–176. A `TypeKind` switch at 36–83
  (`TypeKind.Array` 38, `{ IsNamedType: true }` 43, `TypeKind.Extension` 48, `TypeKind.Tuple` 53,
  `Dynamic` 58, `TypeParameter` 63, `Pointer` 68, `FunctionPointer` 73, `Error` 78,
  `default: throw new ArgumentOutOfRangeException()` 81) followed by a 15-arm `DeclarationKind` switch at 88–167
  (`DeclarationKind.ExtensionBlock` 100, `default: throw` 165). The virtual methods are at 180–220;
  `VisitExtensionBlock` (210) and `VisitTupleType` (212) default to `VisitNamedType`, which is the compatibility
  device that kept every existing subclass working when the two kinds were added.
- `CodeModel/Visitors/CompilationElementVisitor{T}.cs:16–51` — the same two switches, generic form.
  Note it has **no `TypeKind.Tuple` arm**: tuples reach `{ IsNamedType: true }`? No — `Tuple` is not in
  `IsNamedType`, so a tuple hits `_ => throw new ArgumentOutOfRangeException()` at line 28. Latent gap.
- `CodeModel/Visitors/TypeVisitor.cs:15–27` — `TypeKind.Class or Delegate or Enum or Interface or Struct` written
  out longhand at line 20 rather than through `IsNamedType`; `Extension` 24, `Tuple` 25,
  `_ => throw new AssertionFailedException( $"Unexpected type: {type.GetType()}" )` 26.
- `CodeModel/Visitors/TypeSymbolRewriter.cs:38–52` — a switch on **Roslyn** `TypeKind`. Eleven arms,
  `_ => throw new ArgumentOutOfRangeException()` at 51. **`TypeKind.Extension` is missing**; see §5.3.
- `CodeModel/Visitors/TypeRewriter.cs:42` — `public virtual IType Visit( IType elementType ) => ((ITypeImpl) elementType).Accept( this );`
  Double dispatch through `ITypeImpl.Accept` (`CodeModel/Abstractions/ITypeImpl.cs:12`). This is the *only*
  type-level dispatch in the subsystem that does not enumerate kinds, and so the only one a new type class extends
  by implementing an interface member rather than by editing a switch.
- `CodeModel/Visitors/DisplayStringFormatter.cs` — subclass of `CompilationElementVisitor`.
  `VisitNamedType` 347–386, **`VisitExtensionBlock` 388–394**, `VisitTupleType` 396–425,
  `VisitTypeParameter` 427, `VisitArrayType` 458. `DefaultVisit` at 146 is
  `throw new AssertionFailedException()`, so an unhandled element kind fails loudly here.
  `VisitMethod` 256–340 switches on `MethodKind` (275) and `OperatorKind` (285).
- `CodeModel/Helpers/DependencyAnalysisHelper.FindDeclaredTypesVisitor.cs:47–57` — six explicit
  `VisitXDeclaration` overrides (interface, class, struct, record, enum, delegate) plus a manual nested-type walk
  at 35–44 gated on `IsTypeDeclaration`/`IsBaseTypeDeclaration`. See §5.1.
- `CodeModel/Helpers/DependencyAnalysisHelper.FindDeclaredAndAttributeTypesVisitor.cs:35` — same shape.
- `CodeModel/Helpers/IteratorHelper.FindYieldVisitor.cs:24–43` — `DefaultVisit` stops descending at
  `ExpressionSyntax` and `LocalFunctionStatementSyntax` (28–30).
- `CodeModel/CompilationModel.AttributeDiscoveryVisitor.cs:33–162` — walks attributes; the attribute-target
  keyword switch at 91–154 (`ModuleKeyword`, `AssemblyKeyword`, `FieldKeyword`, `ReturnKeyword`, `ParamKeyword`,
  `MethodKeyword`, `PropertyKeyword`, `EventKeyword`, `TypeKeyword`, `TypeVarKeyword`,
  `default: throw new AssertionFailedException( $"Unexpected attribute target: '{targetKind}'." )` 152–153).
  Line 127–131 is the C# 12 primary-constructor arm.

### 1.6 Comparers and the conversion rules

- `CodeModel/Comparers/DeclarationEqualityComparer.Conversions.cs`
  - `HasIdentityOrImplicitReferenceConversion` — **lines 72–140**. Switch on `left.TypeKind` at 90–136 with arms
    `Class` 92, `Interface` 100, `Delegate` 103, `TypeParameter` 121, `Array` 134, **and no `default`**; control
    falls out to `return false` at 139. Line 113 uses `right.TypeKind.IsNamedType`.
  - `IsClass` 70, `IsBaseClass` 142–158, `HasImplicitConversionToInterface` 160–…, boxing/unboxing 292, 318,
    370–380 (type-parameter constraints), 396 (`Class or Struct`), 420, 490 (`IsNamedType`).
- `CodeModel/Comparers/DeclarationEqualityComparer.cs` — `IsOfTypeDefinition( ITypeSymbol, INamedTypeSymbol )`
  250–280 (`SymbolKind.NamedType` 256, `SymbolKind.TypeParameter` 264, interface special case 269);
  `IsOfTypeDefinition( IType, INamedType )` 325–356 (`DeclarationKind.NamedType` 331 — **not widened for
  `ExtensionBlock`**, though an extension block is never a type definition to test against);
  the conversion-kind switch 240–247.
- `CodeModel/Comparers/SignatureTypeComparer.cs` — `Equals` 36–…: pairs of `SymbolKind`
  (`ArrayType` 36, `DynamicType` 40, `TypeParameter` 43, `PointerType` 50, `FunctionPointerType` 55,
  `Parameter` 60, `Method` 65, `Event` 73, `Property` 78, `NamedType` 84) and `GetHashCode` 103–130 with the same
  list. Two parallel lists that must stay in step.
- `CodeModel/Comparers/TypeOrderingComparer.cs:22–57` — the ordering used for deterministic output.
  **Line 39: `var kindDiff = (int) x.TypeKind - (int) y.TypeKind;`** — the sort order is the *ordinal* order of
  `Metalama.Framework.Code.TypeKind`. Lines 46–56 then dispatch, with
  `_ => this.CompareNamedTypes( (INamedType) x, (INamedType) y )` at 55 and the comment at 54
  "Class/Struct/Interface/Enum/Delegate/Tuple/Extension all implement INamedType — matches TypeVisitor<T>'s routing."
- `CodeModel/Comparers/SignatureOrderingComparer.cs`, `DeclarationOrderingComparer.cs`,
  `CompilationComparers.cs` — the remaining ordering surface; `DeclarationOrderingComparer` does not switch on
  kind, it compares containing declarations (48–53).

### 1.7 Collections

`CodeModel/UpdatableCollections/` holds one class per member kind, each declaring
`protected override DeclarationKind ItemsDeclarationKind`:
`ConstructorUpdatableCollection`, `EventUpdatableCollection`, **`ExtensionBlockUpdatableCollection.cs:17–25`**
(`ItemsDeclarationKind => DeclarationKind.ExtensionBlock`, line 24), `FieldUpdatableCollection`,
`IndexerUpdatableCollection`, `MemberUpdatableCollection`, `MethodUpdatableCollection`,
`NamespaceUpdatableCollection`, `PropertyUpdatableCollection`, `TypeUpdatableCollection.cs:11–24`.
`ExtensionBlockUpdatableCollection` derives from `NonUniquelyNamedUpdatableCollection<T>` rather than
`UniquelyNamedUpdatableCollection<T>` because extension blocks have no name — that base-class choice is the
substantive design decision a new declaration kind has to make.

`CodeModel/Collections/` holds the read-only façades; `ExtensionBlockCollection.cs:14–23` is the one added by the
wave, with `OfReceivingType` at 20 and 22.

`CodeModel/CompilationModel.Members.cs:24–40` — one `ImmutableDictionary<IFullRef<…>, …UpdatableCollection>`
field per kind; `_extensionBlocks` at line 40. Accessor `GetExtensionBlockCollection` at **188–193**.
Initialisation in `CodeModel/CompilationModel.cs:245` (`InitializeDictionary( out this._extensionBlocks )`) and
copy-on-write in `CompilationModel.cs:349`.

`CodeModel/CompilationModel.Members.cs:391–530`, `AddDeclaration( DeclarationBuilderData )` — a type-pattern
switch over builder-data classes: finalizer 397, method 410, instance constructor 416, static constructor 422,
field 435, property 441, indexer 447, event 453, parameter 459, attribute 465, named type 471,
**extension block 490–494**, namespace 496, `default: throw new AssertionFailedException` 527.
`AddReplaceMemberTransformation` at 291–342 is a second, smaller switch of the same shape.

### 1.8 Modifiers

`CodeModel/Helpers/ModifierHelper.cs` is the whole modifier surface of the code model.

- `GetSyntaxModifierList( this IDeclaration, ModifierCategories )` — **22–56**, a `DeclarationKind` switch;
  `DeclarationKind.NamedType` 50, `default: throw new AssertionFailedException( $"Unexpected declaration kind: {declaration.DeclarationKind}." )` 53.
  **No `DeclarationKind.ExtensionBlock` arm.**
- `GetAccessorSyntaxModifierList` 58–74.
- `GetMemberSyntaxModifierList` — **76–196**. One `if` per modifier: accessibility 87, `required` 93,
  `static` 100, `partial` 105, `extern` 110, `new`/`abstract`/`virtual`/`override`/`sealed` 115–162,
  `readonly` 164, `const` 171, `unsafe` 178, `volatile` 183, `async` 190.
- `GetTypeSyntaxModifierList` — **198–236**. Only accessibility, `static`, `new`, `abstract`, `sealed`.
- `AddAccessibilityTokens` 238–311, `GetParameterSyntaxModifierList` 313–335,
  `GetRefKindModifiers` 340–346, `AddRefKindModifiers` 348–385 (five `RefKind` arms plus
  `default: throw new AssertionFailedException( $"Unexpected parameter RefKind {refKind}." )` 382).

Roslyn `RefKind` → Metalama `RefKind` is mapped in `CodeModel/Source/SourceParameter.cs:50–59`, with
`_ => throw new InvalidOperationException( $"Roslyn RefKind {…} not recognized." )` at 58 — a loud failure, and
the model to imitate.

### 1.9 Expression- and statement-shape sensitivity reachable from the code model

The code model is mostly symbol-driven, but a handful of members read syntax:

- `CodeModel/Helpers/DeclarationExtensions.cs:292–378`, `GetPropertyKind( this IPropertySymbol )` — the C# 14
  `field` keyword. `ContainsFieldKeyword` 393–413 calls
  `SyntaxHelpers.ContainsFieldExpression` (`Utilities/Roslyn/SyntaxHelpers.cs:93–96`), which is
  `accessor.DescendantNodesAndSelf().OfType<FieldExpressionSyntax>().Any()`.
  The syntax-kind switch at 342–349 returns `_ => false`.
- `Utilities/Roslyn/SyntaxHelpers.cs:103–145`, `ContainsFieldAssignment` / `IsFieldAssignment` — an **exhaustive
  hand-written list of every assignment `SyntaxKind`** (113–126), the prefix/postfix increment kinds (129–136) and
  `SyntaxKind.Argument` (139). This is the single best example in the repository of what a new *expression form*
  costs.
- `CodeModel/Helpers/DeclarationExtensions.cs:380–391` `HasExplicitAccessorBody`,
  `415–425` `IsAutoAccessor`, `427–434` / `436–457` `HasBody`, `459–469` `IsEventField`,
  `471–477` / `479–485` `HasInitializer`. All switch on `SyntaxKind` and all fall to `false` or `null`.
  Line 448 and 451 use the `SyntaxKind.IsBaseMethodDeclaration` / `IsAccessorDeclaration` extension properties.
- `CodeModel/Source/SourceNamedTypeImpl.cs:329–352`, `IsPartial` — `{ SyntaxKind.IsTypeDeclaration: true }` 344,
  `EnumDeclaration` 345, `DelegateDeclaration` 346, `_ => default` **347**.
- `CodeModel/Helpers/IteratorHelper.cs:32–37`, `41–46`, `50–71` — yield detection over syntax.
- `CodeModel/Helpers/PrimarySyntaxNodeHelper.cs:17–22`, `24–35`, `37–51`, `53–…` — walks
  `ContainingSymbol` chains, stops at `SymbolKind.Namespace` (40, 55).
- `Utilities/Roslyn/SymbolExtensions.cs:283–…` `GetPrimarySyntaxReference` — line 289 uses
  `declarationSyntax?.SyntaxKind.IsTypeDeclaration == true` to pick the implementation part of a partial member.

### 1.10 Operators

`Utilities/Roslyn/OperatorData.cs` — a 287-line **table** of
`(OperatorKind Kind, string MemberName, LanguageVersion? MinimumLangVersion, SyntaxKind OperatorKeyword, bool IsChecked)`
(record declaration line 17). Entries run from `LanguageVersion.CSharp1` to the C# 14 compound-assignment block at
**151–265** (`// Compound assignment operators - user-definable since C# 14`). Lookup indexes at 276
(`_byMemberName`, filtered on `MinimumLangVersion != null`), `GetByKind` 278, `GetByName` 281.
Consumed from the code model at `Utilities/Roslyn/SymbolExtensions.cs:318–320` (`GetOperatorKind`, reached from
`CodeModel/Source/SourceMethod.cs:64`) and `CodeModel/Helpers/DeclarationExtensions.cs:270`, `272–276`.

**`OperatorData` is the template for "a new construct that is a table row rather than a switch arm".** It already
carries the language-version column that a C# 15 addition needs.

### 1.11 SerializableIds

`Metalama.Framework.Engine/SerializableIds/` — 3648 lines across 16 files. Metalama carries its **own fork of
Roslyn's `DocumentationCommentId`** (`DocumentationIdHelper*.cs`, 1608 lines), so every grammar change that changes
the shape of a declaration identifier lands here rather than being inherited from Roslyn.

- `SerializableDeclarationIdProvider.FromSymbol.cs:30–160`, `TryGetSerializableId` — a `SymbolKind` switch
  (Local 41, local/anonymous function 46, Parameter 51, TypeParameter 67, Assembly 83, NetModule 90,
  NamedType 97, the non-named types 107, and a nested `default:` switch 113–158 with
  `throw new ArgumentOutOfRangeException( … $"because it is a {symbol.Kind}." )` at 155).
- `SerializableDeclarationIdProvider.FromDeclaration.cs`, `.ToDeclaration.cs`, `.ToSymbol.cs`, `.Nullability.cs`.
- `DocumentationIdHelper.GeneratorOfDeclarationIdFromDeclaration.cs`,
  `DocumentationIdHelper.GeneratorOfReferenceIdFromDeclaration.cs:28`
  (`ContainingDeclaration?.DeclarationKind is DeclarationKind.NamedType or DeclarationKind.ExtensionBlock`),
  `DocumentationIdHelper.Parser.cs` at **336, 517, 544, 551, 607, 715, 764, 779** — eight sites where the C# 14
  wave widened `DeclarationKind.NamedType` to `DeclarationKind.NamedType or DeclarationKind.ExtensionBlock`.
  This eight-site list is the most mechanical evidence of the pattern in the whole subsystem.
- `SerializableTypeIdGenerator.cs` — `IsWrittenInAnnotatedContext( ITypeSymbol )` 93–113 and the code-model twin
  `IsWrittenInAnnotatedContext( IType )` **161–198** (`CodeTypeKind.Array` 176, `Pointer` 179,
  `Class or Struct or Interface or Delegate or Enum or Error or Tuple` 182–183 — **`Extension` absent** —,
  `default: return false` **195**). `GetSerializableTypeId( ITypeSymbol )` at 117 builds the id from
  `SyntaxGenerationContext.Contextless.SyntaxGenerator.TypeSyntax( symbol ).ToString()`.
- `SerializableTypeIdResolver.cs` — the parser side. `VisitArrayType` 249, `VisitPointerType` 272,
  `VisitNullableType` 284, name resolution 354–410, `VisitGenericName` 415, `VisitAliasQualifiedName` 417,
  `VisitQualifiedName` 419, `VisitIdentifierName` 421, `VisitTupleType` 423,
  `DefaultVisit` **441** (`throw new InvalidOperationException( $"Unexpected node {node.Kind()}." )`),
  predefined-type keyword table 447–463, `VisitPredefinedType` 466.
  Line 104 parses through `SyntaxFactoryEx.ParseExpressionSafe`, line 138 through `SyntaxFactory.ParseMemberDeclaration`.
- `SerializableTypeIdResolverForIType.cs:127–130` — `DeclarationKind.Namespace` / `DeclarationKind.NamedType` /
  `_ => throw new AssertionFailedException`; **not widened for `ExtensionBlock`**.
- `SerializableTypeIdResolverForSymbol.cs:53–54`, `99`, `133–144`, `153`.
- `SymbolId.cs`.

---

## Part 2. Files sensitive to the runtime, the SDK, the Roslyn version or the host IDE

### 2.1 Roslyn API version

There is currently **no `#if ROSLYN_*` in production source anywhere in this subsystem**. Confirmed by
`grep -rn "#if " CodeModel/` — the only conditionals are `#if DEBUG` (13 sites) and `#if NET5_0_OR_GREATER` /
`#if NET6_0_OR_GREATER` (4 sites). `eng/RoslynVersions/Roslyn.5.10.0.props:8–10` states this explicitly:

> `ROSLYN_5_10_0_OR_GREATER` is defined by this variant only. No production source branches on it. It exists
> for the two aspect tests whose expected output differs between Roslyn 5.0 and Roslyn 5.10.

`eng/RoslynVersions/Roslyn.5.0.0.props` defines no constant at all. The two tests that use the constant are
`Metalama.Framework.Tests.AspectTests/Tests/Aspects/DesignTimeInvalidCode/UnknownAccessorInTemplate.cs:7`
(`// @RequiredConstant(ROSLYN_5_10_0_OR_GREATER)`) and `…/UnknownAccessorInTemplate_Roslyn5_0.cs:7`
(`// @ForbiddenConstant(…)`).

**Consequence for C# 15.** The four new grammar nodes are `ExperimentalUrl`-marked in Roslyn 5.10 and absent from
Roslyn 5.0. Any code model code that names `UnionDeclarationSyntax`, `UnsafeExpressionSyntax`, `WithElementSyntax`
or `BreakStatementSyntax.Name` will not compile in the Roslyn 5.0 variant. This would be the **first production
source branch on `ROSLYN_5_10_0_OR_GREATER`** since the constant was introduced, and the props comment quoted above
becomes false and must be updated.

The established containment device is the `*Safe` wrapper in `Utilities/Roslyn/`, so that exactly one file carries
the `#if`:

- `Utilities/Roslyn/SymbolExtensions.cs:384–387`, `IsExtensionSafe( this INamedTypeSymbol )`. Git shows the
  original form (commit `16cc84ca1d`):
  ```csharp
  public static bool IsExtensionSafe( this INamedTypeSymbol namedType )
  {
  #if ROSLYN_5_0_0_OR_GREATER
      return namedType.IsExtension;
  #else
      return false;
  #endif
  }
  ```
  Commit `08d065a9f8` ("Replace the Roslyn 4.12 variant with a Roslyn 5.0 variant (#1881)") stripped the guard and
  left the wrapper. Commit `e247425d69` ("Strip always-true `#if ROSLYN_4_(4|8|12)_0_OR_GREATER` guards (#1603)")
  is the earlier instance of the same clean-up.
- Sibling wrappers: `SymbolExtensions.cs:36` `GetAttributesSafe`, `Utilities/Roslyn/ReflectionHelper.cs:58`
  `GetTypeByMetadataNameSafe`, `CodeModel/Helpers/AsyncHelper.cs:19` `IsAsyncSafe`,
  `Utilities/Roslyn/LanguageVersionExtensions.cs:12` `ToDisplayStringSafe`.

`Utilities/Roslyn/LanguageVersionExtensions.cs:16–40` is the other explicitly version-tolerant file:
```csharp
(LanguageVersion) 1300 => "13.0",
(LanguageVersion) 1400 => "14.0",
```
Numeric casts, because `LanguageVersion.ToDisplayString` throws for a version the bound Roslyn does not know.
C# 15 is `(LanguageVersion) 1500`. The `_ =>` at line 39 throws.

`Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs` — `Latest => LanguageVersion.CSharp14` (line 32),
the `All` set 38–43, `DefaultParseOptions` 50, and `ToLanguageVersion( this RoslynApiVersion )` 52–62 with
`RoslynApiVersion.V5_0_0 => CSharp14` (59) and `V5_10_0 => CSharp14` (60). The code model consumes this through
`CodeModel/LanguageOptions.cs:30` (`Default = new( SupportedCSharpVersions.Latest, … )`) and `:35`
(`ToParseOptions`).

Roslyn APIs the code model calls that arrived with a recent Roslyn and would break an older variant:
`INamedTypeSymbol.IsExtension` and `.ExtensionParameter` (`Source/ExtensionBlockImpl.cs:21, 24, 34`),
`IMethodSymbol.AssociatedExtensionImplementation` (`Source/SourceMethod.cs:174`),
`IMethodSymbol/IPropertySymbol/IEventSymbol.PartialDefinitionPart` and `.PartialImplementationPart`
(`Factories/DeclarationFactory.Symbols.cs:212, 224`; `References/SymbolNormalizer.cs:18, 20, 75, 77, 87, 89`;
`Source/SourceMethod.cs:195`), `IEventSymbol.IsPartialDefinition`
(`Helpers/DeclarationExtensions.cs:463`), `FieldExpressionSyntax` (`Utilities/Roslyn/SyntaxHelpers.cs:95` and the
`IsFieldAssignment` patterns), `RefKind.RefReadOnlyParameter` (`Source/SourceParameter.cs:57`),
`TypeKind.Extension` (`Utilities/Roslyn/SymbolExtensions.cs:479`).

### 2.2 .NET runtime version

- `CodeModel/Helpers/DeclarationCache.cs:29–33` and `:44–48` — `#if NET6_0_OR_GREATER`, an API-shape difference
  in the concurrent dictionary usage.
- `CodeModel/References/DurableRef.cs:160–164` — `#if NET5_0_OR_GREATER`.
- `CodeModel/ProjectModel.ProjectFeaturesImpl.cs:76–80` — `#if NET5_0_OR_GREATER` around
  `int.TryParse( tfm.AsSpan(…) )` versus `tfm.Substring(…)`.

These follow the Core/Desktop flavour split, not the user's target framework. Under PB-2027.0 the Core flavour is
`net10.0` and the Desktop flavour `net472`, so `NET5_0_OR_GREATER` and `NET6_0_OR_GREATER` are now both simply
"Core", and every one of these four conditionals is a candidate for the same strip-when-always-true clean-up that
`e247425d69` applied to the Roslyn constants.

### 2.3 .NET SDK / target framework of the *user's* project

`CodeModel/ProjectModel.ProjectFeaturesImpl.cs` is the only place in the subsystem that parses a target framework
moniker.

- `ComputeSupportsCovariantReturn` 23–49 — reads `options.LanguageVersion` (25) and
  `options.AllTargetFrameworks ?? options.TargetFramework` (30).
- `TargetFrameworkSupportsCovariantReturn( string tfm )` **55–86** — string surgery on the moniker:
  requires `net` + a digit (58–60), rejects dot-less monikers as .NET Framework (67–72), parses the major version
  between `net` and the first dot (77/79) and answers `major >= 5` (82).
  `net11.0` parses correctly. The file is nevertheless the one to check whenever the moniker grammar changes.
- `ProjectModel.cs:52`/`:70` `PreprocessorSymbols`, `:74` `TargetFramework`.

### 2.4 Host IDE

The code model has no direct IDE dependency; the host enters through
`CodeModel/ExecutionScenario.cs` and `CodeModel/CompilationModelOptions.cs`
(`ShowExternalPrivateMembers`, read at `References/SymbolRef.Strategy.cs:182`), and through
`SourceGeneratorHelper.IsGeneratedSymbol` at `References/SymbolRef.Strategy.cs:201` (issue #1752, pull request
#1784) — the rule that the design-time source generator must not read its own output back. The design-time /
compile-time distinction also drives `References/SymbolRef.Strategy.cs:189` (compile-time symbols hidden) and the
whole `IDurableRef` family (`References/DurableRef.cs`, `BoundDurableRef.cs`, `SerializedDurableRefFactory.cs`,
`BoundDurableRefFactory.cs`).

### 2.5 Reflection and the BCL

`CodeModel/Helpers/ReflectionMapper.cs` — `GetNamedTypeSymbolByMetadataName` 35–83,
`GetTypeSymbol` 88–100, `GetTypeSymbolCore` 102–134 (`IsGenericParameter` 104, `IsByRef` 114 → throws,
`IsArray` 119, `IsPointer` 126), `GetNamedTypeSymbol` 136ff.
`Utilities/Roslyn/SymbolExtensions.cs:50–75` `ToOurSpecialType` and `:77–104` `ToRoslynSpecialType` — two
hand-maintained 21-entry mappings between Roslyn `SpecialType` and Metalama `SpecialType`, both with a
permissive `_ => …None` fallback. `CodeModel/Source/SourceNamedTypeImpl.cs:84–129` `GetSpecialTypeCore` adds
Metalama-only special types (`Task`, `ValueTask`, `IAsyncEnumerable`, `Type`, `ValueTuple`) by **matching the
type name and namespace as strings** (100–127).

---

## Part 3. How the subsystem absorbed C# 14

### 3.1 What the tracked issues produced here

| Issue | Branch / commits | Footprint in this subsystem |
| --- | --- | --- |
| **#1034** extension members: code model | `topic/2026.0/1034-extensions-code-model`, PR #1123, commits `cdf076ee1a`, `bcdeb3a185` | `CompilationModel.cs` (+5), `References/IntroducedRef.cs`, `References/RefExtensions.cs`, `References/SymbolNormalizer.cs`, `References/SymbolRef.cs`, `References/SymbolRef.Strategy.cs` (+28/−…), `Source/Pseudo/PseudoParameter.cs`, `Source/SourceNamedType.cs`, **`Source/SourceNamedTypeImpl.cs` (+209/−…)**, `Source/SourceParameter.cs`, **new `Source/TypeExtension.cs` and `Source/TypeExtensionImpl.cs`** (later renamed `ExtensionBlock.cs` / `ExtensionBlockImpl.cs`), `Visitors/DisplayStringFormatter.cs`, `Helpers/DeclarationExtensions.cs` (+149), and the new test file `CodeModel/CodeModelTests.CSharp14.cs` |
| **#1159** introduce extension blocks | `topic/2026.1/1159-introduce-extension-blocks`, PR #1289, 16 commits starting `a9698fa1e8` "Add C# 14 extension block introduction infrastructure" | the whole builder half: `Introductions/Builders/ExtensionBlockBuilder.cs`, `Introductions/Builders/ExtensionReceiverParameterBuilder.cs`, `Introductions/BuilderData/ExtensionBlockBuilderData.cs`, `Introductions/Introduced/IntroducedExtensionBlock.cs`, `UpdatableCollections/ExtensionBlockUpdatableCollection.cs`, `Collections/ExtensionBlockCollection.cs`, `Factories/DeclarationFactory.Builders.cs` `GetExtensionBlock` |
| **#1036** extension member invokers | PR #1293, commit `22697b6ba5` | `Invokers/Invoker.cs:34` `IsExtensionMember`, `Introductions/Introduced/ExtensionImplementationLookup.cs`, `Source/SourceMethod.cs:169–180` `ExtensionImplementationMethod` |
| **#1127** extension member contracts | PR #1294, commit `30e21aea98` | receiver-parameter contracts, reaches `ExtensionReceiverParameterBuilder` |
| **#1114** `field` keyword in templates | PR #1297, 12 commits from `aea7b2e5a2` | `Helpers/PropertyKind.cs` (new enum), `Helpers/DeclarationExtensions.cs:292–378` `GetPropertyKind`, `:393–413` `ContainsFieldKeyword`, `:415–425` `IsAutoAccessor`; `Utilities/Roslyn/SyntaxHelpers.cs:93–145` |
| **#1094** field expression support | commits `70bd44a5e1`, `48541ada9b` ("Fix for older Roslyn") | same area; the second commit is the `#if`-guard pattern |
| **#1116** compound assignment operators | PR #1132, commits `5b121f3c21`, `6d8678e5d3` ("Refactored to not bypass the normal substitution mechanisms") | `Utilities/Roslyn/OperatorData.cs:151–265` |
| **#1110–#1113** partial constructors and events | commit `787ec4fcd8` | `PartialDefinitionPart`/`PartialImplementationPart` normalisation in `Factories/DeclarationFactory.Symbols.cs:212, 224` and `References/SymbolNormalizer.cs`; `Helpers/DeclarationExtensions.cs:463` `IsPartialDefinition` |
| **#1105** unsupported language features | PR #1117, commit `cf0861898b` | the diagnostics arm, not the code model |
| **#1109** null-conditional assignment | PR #1295, commit `e9edd7cacc` | templating, not the code model |

### 3.2 The pattern, stated as a checklist

For a **new kind of type declaration** (extension block) the wave did, in this order:

1. **Public interface.** `Metalama.Framework/Code/IExtensionBlock.cs` — `IExtensionBlock : INamedType`, adding
   only `ReceiverType`, `ReceiverParameter`, a covariant `ToRef()` and a narrowed `DeclaringType`. Plus
   `Code/Collections/IExtensionBlockCollection.cs` and `Code/DeclarationBuilders/IExtensionBlockBuilder.cs`.
   *The new interface derives from the closest existing one, so nothing that consumes `INamedType` breaks.*
2. **Enum members, appended.** `TypeKind.Extension`, `DeclarationKind.ExtensionBlock`, `RefTargetKind.ExtensionBlock`.
   Appended, never inserted, because `TypeOrderingComparer.cs:39` sorts by ordinal and `RefTargetKind` is
   serialised by name into compiled assemblies.
3. **A `*Safe` predicate.** `SymbolExtensions.IsExtensionSafe()`, originally `#if`-guarded, so exactly one file
   knew about the Roslyn version.
4. **The symbol→kind funnel.** One arm in `DeclarationExtensions.GetDeclarationKind` (line 44), placed *before*
   the general `SymbolKind.NamedType` arm.
5. **A Source facade + implementation pair.** `ExtensionBlockImpl : SourceNamedTypeImpl, IExtensionBlock`
   overriding `TypeKind` (30), `DeclarationKind` (39), `CheckSymbol` (32) and `CreateFullRef` (37);
   `ExtensionBlock : SourceNamedType, IExtensionBlock` forwarding through `OnUsingDeclaration`.
   The base class's `CheckSymbol` gained `Invariant.Assert( !this.NamedTypeSymbol.IsExtensionSafe() )`
   (`SourceNamedTypeImpl.cs:59`) so the two cannot be confused.
6. **The construction site.** `DeclarationFactory.GetNamedType` 185–198.
7. **Reference plumbing.** `RefFactory.FromAnySymbol` arm (114–118), `RefExtensions.ToRef(INamedTypeSymbol)`
   (142–145), `RefExtensions.ToExtensionBlockRef` (156), the `SymbolRef` assert (81–86),
   `RefTargetKindExtensions.ToDeclarationKind` (52), `FullRef.ApplyRefKind` (170),
   `GetPossibleDeclarationInterfaceTypes` (112, 118).
8. **Member enumeration.** `IsValidNamedType` excludes the new kind (`SymbolRef.Strategy.cs:298–301`),
   a new `IsValid<Kind>` predicate is added (315), and `GetSymbolPredicate` gains an arm (328).
9. **Collections.** A `*UpdatableCollection`, a read-only `*Collection`, a field and an accessor on
   `CompilationModel` (`_extensionBlocks` 40, `GetExtensionBlockCollection` 188), initialisation (245) and
   prototype copy (349), and the property on the owner (`SourceNamedTypeImpl.ExtensionBlocks` 317–327).
10. **Builder trio.** `*Builder` (deriving from the nearest existing builder — `ExtensionBlockBuilder : NamedTypeBuilder`),
    `*BuilderData`, `Introduced*`; the `AddDeclaration` arm (`CompilationModel.Members.cs:490`), the
    `DeclarationFactory.Builders` factory method (188) and switch arm (251).
11. **Visitors.** A `Visit<Kind>` virtual **defaulting to the nearest existing one**
    (`CompilationElementVisitor.cs:210`, `TypeVisitor.cs:39`) plus the dispatch arms
    (`CompilationElementVisitor.cs:48, 100`; `TypeVisitor.cs:24`), and the real override in
    `DisplayStringFormatter.cs:388`.
12. **Widen every `DeclarationKind.NamedType` test that also means "a thing that has members".** Mechanically:
    eight sites in `DocumentationIdHelper.Parser.cs`, one in
    `DocumentationIdHelper.GeneratorOfReferenceIdFromDeclaration.cs:28`, one in
    `DeclarationFactory.Builders.cs:219`.
13. **Tests.** `Metalama.Framework.Tests.UnitTests/CodeModel/CodeModelTests.CSharp14.cs` — a `partial class`
    file per language wave (`ExtensionMembers` 66, `ExtensionBlockAccessibility` 141,
    `ExtensionMemberAttributes_MirroredToImplementation` 173, `UnaryCompoundOperator` 22,
    `BinaryCompoundOperator` 44) plus a directory per feature under
    `Metalama.Framework.Tests.AspectTests/Tests/Aspects/CSharp14/` (`CompoundAssignmentOperator`,
    `ExtensionMembers`, `FieldKeyword`, `NullConditionalAssignment`, `PartialConstructor`, `PartialEvent`,
    `SimpleLambdaModifier`).

For a **new member-level construct** (compound-assignment operators, `field`, partial events) the wave instead
extended a **table** (`OperatorData.All`) or added a **derived-property helper**
(`GetPropertyKind`, `ContainsFieldExpression`) rather than a new declaration kind. That is the cheaper half of the
pattern and is the right shape for the C# 15 `closed` modifier and for indexers in extension blocks.

---

## Part 4. Extension points per kind of change

### 4.1 A new kind of type declaration (C# 15 `union`, `UnionDeclarationSyntax`)

Follow the 13-step list in §3.2 verbatim. The decisions that are not mechanical:

- **Does it get its own `TypeKind` value and its own public interface, or is it `TypeKind.Class` with a flag?**
  Records took the flag route (`TypeKind.RecordClass` was obsoleted in favour of `TypeKind.Class` +
  `INamedType.IsRecord`, see `TypeKind.cs:29–30` and `SourceNamedTypeImpl.cs:173`). Extension blocks took the
  new-kind route because they have no name and cannot be nullable. A union is a named, nameable, nullable type
  with members, so the record precedent (`TypeKind.Class` + `INamedType.IsUnion`) is the cheaper and more
  compatible answer, and it costs **zero** switch arms. Choosing a new `TypeKind` value costs, at minimum, the
  arms enumerated in §1.5 and §1.6 plus `TypeKindExtensions.IsNamedType`.
- **Whichever route: `SourceNamedTypeImpl.TypeKind` (lines 69–79) must map the Roslyn `TypeKind`.**
  It currently throws `InvalidOperationException` for anything outside `Class`/`Delegate`/`Enum`/`Interface`/
  `Struct`/`Error`. If Roslyn represents a union as `TypeKind.Class` this needs no change; if it introduces
  `TypeKind.Union`, this is the first thing that fails, and it fails loudly.
- `TypeKindExtensions.IsNamedType` (`TypeKindExtensions.cs:22–24`) — decide and change once.
- `NamedTypeBuilder`'s constructor assert (`Introductions/Builders/NamedTypeBuilder.cs:52`)
  gates which kinds an aspect may *introduce*.
- `Visitors/TypeSymbolRewriter.cs:43` — the Roslyn-side named-type arm.
- `SerializableTypeIdGenerator.cs:182–183` — the named-type arm of `IsWrittenInAnnotatedContext(IType)`.
- `Comparers/TypeOrderingComparer.cs:55` — already permissive (`_ => CompareNamedTypes`), so a named-type-like
  kind needs nothing; a *non*-named-type-like kind would throw `InvalidCastException` there.
- `SyntaxKindExtensions.IsTypeDeclaration` (`SyntaxKindExtensions.cs:33–35`) and
  `DependencyAnalysisHelper.FindDeclaredTypesVisitor` (`:47–57`) — see §5.1; these are already wrong for
  extension blocks and would be wrong again.

### 4.2 A new modifier (C# 15 `closed`)

- `CodeModel/Helpers/ModifierCategories.cs:12–23` — a new flag bit and an edit to `All`.
- `CodeModel/Helpers/ModifierHelper.cs` — a new `if` in `GetTypeSyntaxModifierList` (198–236) if it is a
  type modifier, or in `GetMemberSyntaxModifierList` (76–196) if it is a member modifier. The existing
  `unsafe` handling at 178 is the model for a modifier read back from syntax rather than from a symbol property:
  `member.GetSymbol() is { } symbol && symbol.HasModifier( SyntaxKind.UnsafeKeyword ) == true`.
- If the modifier is observable as a symbol property, the corresponding `Source*` and `Introduced*` and
  `*Builder` and `*BuilderData` members: for a type that is `SourceNamedTypeImpl.cs` (compare `IsReadOnly` 169,
  `IsRef` 171, `IsRecord` 173, `IsPartial` 329), `NamedTypeBuilder.cs`, `NamedTypeBuilderData.cs:31–35`
  (which currently hard-codes `IsReadOnly => false` and `IsRef => false`) and
  `IntroducedNamedType.cs`.
- The public interface (`Metalama.Framework/Code/INamedType.cs` or `IMember.cs`) — outside this subsystem, but
  the code model is what implements it four times over.
- **`closed` is a contextual modifier that Roslyn will surface as a symbol property.** If it is not surfaced,
  the read has to go through syntax and lands in `SourceNamedTypeImpl.IsPartial`'s shape (329–352), whose
  `_ => default` is a silent-false.

### 4.3 A new expression form (C# 15 `unsafe(expr)`, `UnsafeExpressionSyntax`)

The code model itself models declarations, not expressions, so the blast radius here is small and concentrated in
the syntax-reading helpers:

- `Utilities/Roslyn/SyntaxHelpers.cs:103–145` `ContainsFieldAssignment` / `IsFieldAssignment` — the exhaustive
  assignment/increment/argument list. `field` inside `unsafe(field = x)` would not be seen. This is the exact
  analogue of what `FieldExpressionSyntax` cost in #1114.
- `CodeModel/Helpers/IteratorHelper.FindYieldVisitor.cs:24–43` — stops descending at `ExpressionSyntax` (28), so
  an expression form that can contain statements would hide a `yield`.
- `CodeModel/Helpers/DeclarationExtensions.cs:436–457` `HasBody` and `:380–391` `HasExplicitAccessorBody` —
  both enumerate the syntax forms that carry a body, and both fall to `false`.
- `SerializableTypeIdResolver.cs:441` `DefaultVisit` — throws on an unexpected node, which is correct behaviour
  and needs no change unless the new form can appear inside a type syntax.

### 4.4 A new collection-expression element (C# 15 `with(...)`, `WithElementSyntax`)

Nothing in `CodeModel/**` parses collection expressions. The two places a collection-expression element can reach
the code model are:

- `CodeModel/Invokers/ValueArrayExpression.cs` — builds `params` arrays for invokers.
- `CodeModel/Helpers/TypedConstantExtensions.cs` and `CodeModel/StandaloneAttributeData.cs` — attribute argument
  values, which can be array-valued.

Neither reads a `CollectionExpressionSyntax`. The construct belongs to the templating and syntax-generation
subsystems; the code model's only exposure is that `SerializableTypeIdResolver` re-parses a generated type syntax
(`SerializableTypeIdResolver.cs:104`, `SyntaxFactoryEx.ParseExpressionSafe`, `SyntaxFactoryEx.cs:367–382`) with
`SyntaxFactory.ParseExpression` and **no explicit parse options**, so the ambient default language version applies.

### 4.5 A new optional field on an existing statement (labelled `break` / `continue`)

No exposure in `CodeModel/**`. Statements are never modelled here. The only statement-shaped code in the subsystem
is `IteratorHelper.FindYieldVisitor` (yield detection) and `SafeSyntaxWalker`'s generic descent, both of which
handle an unrecognised statement by walking its children. The risk lives in
`eng/src/GenerateMetaSyntaxRewriter` and the templating subsystem, not here.

---

## Part 5. Where the subsystem would silently do the wrong thing

Ranked by how likely the C# 15 wave is to hit them.

### 5.1 `SyntaxKindExtensions.IsTypeDeclaration` omits `ExtensionBlockDeclaration`, and will omit `UnionDeclaration`

`Utilities/Roslyn/SyntaxKindExtensions.cs:33–35`:
```csharp
public bool IsTypeDeclaration
    => kind is SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration or SyntaxKind.InterfaceDeclaration
        or SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration;
```
`SyntaxKind.ExtensionBlockDeclaration` exists (used at
`Linking/LinkerInjectionStep.Rewriter.cs:622`, `Linking/LinkerLinkingStep.LinkingRewriter.cs:79`,
`Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs:277, 662`) and is **not** in this predicate.

Three consumers degrade quietly rather than failing:

- `CodeModel/Source/SourceNamedTypeImpl.cs:342–348` — `IsPartial` matches `_ => default` (an empty
  `SyntaxTokenList`) and answers **false** for any declaration form not in the list.
- `CodeModel/Helpers/DependencyAnalysisHelper.FindDeclaredTypesVisitor.cs:35–44` — the nested-type walk is gated
  on `IsTypeDeclaration` / `IsBaseTypeDeclaration`, and there are only six `VisitXDeclaration` overrides (47–57).
  A `union` declaration is not visited, so the type is **omitted from the dependency graph**; the design-time
  incremental pipeline then does not invalidate the file when the union changes.
- `Utilities/Roslyn/SymbolExtensions.cs:289` — `GetPrimarySyntaxReference` picks a different declaring reference
  for a partial member, changing which file a diagnostic is reported in.

### 5.2 `RefExtensions.ToRef( this ITypeSymbol, … )` does not exclude extension blocks

`CodeModel/References/RefExtensions.cs:163–170`:
```csharp
SymbolKind.NamedType when symbol is INamedTypeSymbol { IsTupleType: true } => refFactory.FromSymbol<ITupleType>( … ),
SymbolKind.NamedType => refFactory.FromSymbol<INamedType>( symbol, genericContext ),
```
The `INamedTypeSymbol` overload three lines above (140–154) *does* branch on `IsExtensionSafe()`. An extension
block symbol reaching the `ITypeSymbol` overload yields a `SymbolRef<INamedType>` whose target interface violates
the invariant of `SymbolRef.cs:81–86` — an assert that is **not** `#if DEBUG`-guarded, so it throws; but the
`GetPossibleDeclarationInterfaceTypes` cross-check at `SymbolRef.cs:60–64` *is* `#if DEBUG`, so the release-build
behaviour of a similar mismatch elsewhere is an unchecked wrong-typed reference.

### 5.3 `TypeSymbolRewriter.Visit(ITypeSymbol)` has no `TypeKind.Extension` arm

`CodeModel/Visitors/TypeSymbolRewriter.cs:38–52`. Roslyn's `TypeKind.Extension` is real in the bound Roslyn
(`Utilities/Roslyn/SymbolExtensions.cs:479` reads it), so the `_ => throw new ArgumentOutOfRangeException()` at
line 51 fires with no message and no context. That is a hard failure rather than a silent one, but the
`ArgumentOutOfRangeException` carries neither the type nor the kind, which makes it read as a Metalama bug.

### 5.4 `CompilationElementVisitor<T>` has no `TypeKind.Tuple` arm

`CodeModel/Visitors/CompilationElementVisitor{T}.cs:19–29`. `TypeKind.Tuple` is not in
`TypeKindExtensions.IsNamedType`, so a tuple reaches `_ => throw new ArgumentOutOfRangeException()` at line 28.
The non-generic sibling handles it at `CompilationElementVisitor.cs:53`. Two files that should have identical
dispatch and do not — exactly the failure mode a new `TypeKind` value re-creates.

### 5.5 `SerializableTypeIdGenerator.IsWrittenInAnnotatedContext` returns `false` for unknown kinds

Two switches, `:93–113` over `SymbolKind` and `:174–195` over Metalama `TypeKind`, both ending in
`default: return false`. The result decides whether the generated `SerializableTypeId` carries the trailing `!`
that records "written in a nullable-annotated context". A wrong answer produces a **valid-looking identifier that
denotes a different type**, and the failure surfaces much later as a reference that resolves to the wrong
nullability or does not resolve at all. Note also that the `IType` switch at 182–183 lists
`Class or Struct or Interface or Delegate or Enum or Error or Tuple` and omits `Extension` — harmless today only
because an extension block cannot appear as a type reference.

### 5.6 `HasIdentityOrImplicitReferenceConversion` falls through to `false`

`CodeModel/Comparers/DeclarationEqualityComparer.Conversions.cs:90–139`. No `default` arm; an unrecognised
`left.TypeKind` yields "no conversion exists". This comparer backs `IType.Is()`, aspect eligibility
(`EligibilityRuleFactory.cs:47, 121`), contract applicability and advice validation. A new reference-type-like
kind would make aspects **silently skip** the declarations they were meant to apply to, with no diagnostic. This
is the highest-consequence silent failure in the subsystem.

### 5.7 `OperatorData.GetByName` returns `null` for an unknown operator name

`Utilities/Roslyn/OperatorData.cs:281`, consumed at `Utilities/Roslyn/SymbolExtensions.cs:318–320`:
```csharp
? OperatorData.GetByName( method.Name )?.Kind ?? OperatorKind.None
: OperatorKind.None
```
A C# 15 operator whose `WellKnownMemberNames` entry is not in the table is reported as
`OperatorKind.None` even though `MethodKind` is `UserDefinedOperator`. Downstream,
`DisplayStringFormatter.VisitMethod` (275–340) and `DeclarationExtensions.ToOperatorKeyword`
(`DeclarationExtensions.cs:270`) then either print the mangled metadata name or throw
`AssertionFailedException` from `ToOperatorMethodName` (276). The C# 14 wave added 19 rows to this table
(lines 151–265) and nothing warned that they were missing beforehand.

### 5.8 `ToOurSpecialType` / `ToRoslynSpecialType` fall back to `None`

`Utilities/Roslyn/SymbolExtensions.cs:74` and `:103`. `SpecialType.None` is a legitimate value, so a new Roslyn
special type is indistinguishable from "not special". Low risk for C# 15, high risk for a .NET 11 BCL that adds
special types.

### 5.9 `DeclarationExtensions.HasBody` / `IsEventField` / `HasInitializer` return `false` or `null`

`CodeModel/Helpers/DeclarationExtensions.cs:436–457`, `:459–469`, `:471–485`. Each enumerates the syntax forms it
knows and answers negatively otherwise. A member declared in a form the switch does not recognise is treated as
abstract-like, which changes whether the linker inlines it.

### 5.10 `GetPropertyKind` is `SemiAuto`-detected purely by syntax

`CodeModel/Helpers/DeclarationExtensions.cs:292–378` plus `:393–413`. Detection is
`accessor.DescendantNodesAndSelf().OfType<FieldExpressionSyntax>().Any()`
(`Utilities/Roslyn/SyntaxHelpers.cs:95`). Any new syntactic wrapper around a `field` expression that the
descendant walk does not reach (it does reach everything, today) would silently reclassify a semi-auto property as
`Default`, which changes whether Metalama transfers the property initialiser to the backing field
(commit `df4ae55b09`, issue #1114).

### 5.11 The `#if DEBUG` interface-type guard is absent in release builds

`CodeModel/References/RefExtensions.cs:90` and `:136` bracket `GetPossibleDeclarationInterfaceTypes` in
`#if DEBUG`. Its three call sites (`SymbolRef.cs:62`, `IntroducedRef.cs:107`,
`DeclarationFactory.Builders.cs:212`) are likewise `#if DEBUG`. A kind/interface mismatch introduced by the C# 15
work is caught in a debug test run and **not** in a shipped build, where it becomes an `InvalidCastException` far
from its cause.

### 5.12 `ReferenceValidationContext.GetInboundGranularity` in `Metalama.Premium` never learned about extension blocks

`C:/src/Metalama-2027.0/Metalama.Premium/src/Metalama.Extensions.Validation/ReferenceValidationContext.cs:124–134`:
```csharp
DeclarationKind.NamedType => ReferenceGranularity.Type,
…
_ => throw new ArgumentOutOfRangeException( nameof(kind), $"Unexpected kind: '{kind}'" )
```
`DeclarationKind.ExtensionBlock` is missing, so a reference validator applied to a declaration inside an extension
block throws. This is the one place in the Premium repository that this subsystem's taxonomy leaks into, and it
shows that widening `DeclarationKind` does not automatically get followed across repository boundaries. The other
two Premium sites (`Metalama.Extensions.Architecture/Aspects/ExperimentalAttribute.cs:47` and
`InternalOnlyImplementAttribute.cs:46`) are safe.

### 5.13 `SerializableTypeIdResolverForIType` does not accept an extension block as a container

`Metalama.Framework.Engine/SerializableIds/SerializableTypeIdResolverForIType.cs:127–130` —
`DeclarationKind.Namespace` and `DeclarationKind.NamedType` only, then
`_ => throw new AssertionFailedException`. One of the eight `NamedType or ExtensionBlock` widenings of
`DocumentationIdHelper.Parser.cs` was not applied here.

---

## Part 6. Practical starting points for the C# 15 wave in this subsystem

1. Decide the union representation first (new `TypeKind` versus `TypeKind.Class` + an `IsUnion` flag). Everything
   in §4.1 follows from that one decision, and the record precedent in `TypeKind.cs:29–30` argues for the flag.
2. Add `(LanguageVersion) 1500 => "15.0"` to `Utilities/Roslyn/LanguageVersionExtensions.cs:16–40` and bump
   `SupportedCSharpVersions.Latest` / `.All` / `.ToLanguageVersion` — `CodeModel/LanguageOptions.cs:30` picks it
   up for free.
3. Introduce the `*Safe` wrappers in `Utilities/Roslyn/SymbolExtensions.cs` for every new Roslyn API, guarded by
   `#if ROSLYN_5_10_0_OR_GREATER`, before touching anything in `CodeModel/**`. Update the comment in
   `eng/RoslynVersions/Roslyn.5.10.0.props:8–10`, which currently asserts that no production source branches on
   that constant.
4. Fix §5.1 (`IsTypeDeclaration`) and §5.3 (`TypeSymbolRewriter`) as prerequisites, because they are already wrong
   for extension blocks and a union declaration would hit them the same way.
5. Add `CodeModelTests.CSharp15.cs` beside
   `Metalama.Framework.Tests.UnitTests/CodeModel/CodeModelTests.CSharp14.cs` and a
   `Tests/Aspects/CSharp15/` directory, one sub-directory per feature, mirroring the C# 14 layout.
