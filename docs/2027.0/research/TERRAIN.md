# TERRAIN: the Metalama source tree under C# 15 and .NET 11

Consolidated from fourteen subsystem maps made on branch `topic/2027.0/26-09-03-net11-impact`
(based on `develop/2027.0`), with the companion repository `C:/src/Metalama-2027.0/Metalama.Premium`
on `topic/2027.0/1829-durable-and-immutable-contracts`.

Repository roots used throughout:

- `C:/src/Metalama-2027.0/Metalama` — paths written without a repository prefix are relative to it.
- `C:/src/Metalama-2027.0/Metalama.Premium` — always written in full.

Subsystem keys used in the hotspot table:

| Key | Subsystem | Scope |
| --- | --- | --- |
| CM-PUB | Public code model | `Metalama.Framework/src/Metalama.Framework/Code/**` |
| CM-ENG | Code model implementation | `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/**`, `SerializableIds/**`, `Utilities/Roslyn/**` |
| TMPL | Templating (T#) and the grammar generator | `Metalama.Framework.Engine/Templating/**`, `eng/src/GenerateMetaSyntaxRewriter/**` |
| ADV | Advising and advice implementation | `Metalama.Framework.Engine/Advising/**`, `AdviceImpl/**`, `Metalama.Framework/Advising/**` |
| LINK | Aspect linker | `Metalama.Framework.Engine/Linking/**`, `Transformations/**` |
| SYNGEN | Syntax generation, serialisation, formatting | `Metalama.Framework.Engine/{SyntaxGeneration,SyntaxSerialization,Formatting,Serialization}/**` |
| CT | Compile-time compilation, pipeline, options | `Metalama.Framework.Engine/{CompileTime,Pipeline,Options}/**` and the four `Utilities/` version files |
| DT | Design time and cross-process | `Metalama.Framework.DesignTime{,.Contracts,.Rpc}/**`, `Metalama.Framework.CompilerExtensions/**` |
| BUILD | Build, packaging, target frameworks | `eng/**`, `Directory.*.props`, `global.json`, the Package projects |
| BACK | Backstage | `Metalama.Backstage/src/**` |
| PAT | Patterns | `Metalama.Patterns/src/**` |
| EXT | Extensions, tooling, introspection | `Metalama.Extensions/src/**`, HtmlWriter, DiffEngine, Workspaces, Introspection, Tool, LinqPad, Analyzers |
| TEST | Test infrastructure and suites | `Metalama.Testing.*/**`, `Metalama.Framework/src/tests/**` |
| PREM | Premium | `Metalama.Premium/src/**` |

The five C# 15 grammar additions referred to throughout, all declared in
`eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml` and all carrying `ExperimentalUrl`:

| Grammar element | Line | Roslyn issue |
| --- | --- | --- |
| `UnsafeExpressionSyntax` (`unsafe(expr)`), `Base="ExpressionSyntax"` | 496 | dotnet/roslyn#82789 |
| `WithElementSyntax` (`with(...)`), `Base="CollectionElementSyntax"` | 816 | dotnet/roslyn#82210 |
| `BreakStatementSyntax.Name`, `Optional="true"`, `IdentifierNameSyntax` | 1296 | dotnet/roslyn#83266 |
| `ContinueStatementSyntax.Name`, same | 1307 | dotnet/roslyn#83266 |
| `UnionDeclarationSyntax`, `Base="TypeDeclarationSyntax"`, `SkipConvenienceFactories="true"` | 1954 | dotnet/roslyn#82567 |

Two further C# 15 features add no grammar node: the `closed` contextual modifier, and indexers declared
inside an extension block.

---

## 1. Hotspot table

| # | Path (and line) | What it is | Sensitive to | Subsystem |
| --- | --- | --- | --- | --- |
| 1 | `Metalama.Framework/src/Metalama.Framework/Code/DeclarationKind.cs` (last member `ExtensionBlock` 118; obsolete placeholders `Finalizer` 89, `Operator` 95) | The declaration-kind enumeration | A new kind of declaration; ordinals are load-bearing | CM-PUB |
| 2 | `Code/TypeKind.cs` (`Extension` 83, `Tuple` 88; obsolete `RecordClass` 30, `RecordStruct` 68) | The type-kind enumeration | A new kind of type declaration; ordinal order is the sort order used by `TypeOrderingComparer` | CM-PUB |
| 3 | `Code/MethodKind.cs` (last `DelegateInvoke` 77) | Method-kind enumeration | A new kind of method | CM-PUB |
| 4 | `Code/OperatorKind.cs` (last `CheckedDecrementAssignment` 324) | Operator enumeration | A new operator | CM-PUB |
| 5 | `Code/OperatorCategory.cs` (last `UnaryAssignment` 24) | Operator arity or form | A new operator form | CM-PUB |
| 6 | `Code/RefKind.cs` (last `Out` 41) | Parameter and return passing modes | A new passing mode | CM-PUB |
| 7 | `Code/Writeability.cs` (109 "IMPORTANT: Do not change values"; `All = 3` 129) | Settability, with explicit load-bearing values | A new settability form | CM-PUB |
| 8 | `Code/SpecialType.cs` (`Nullable_T` 177; sentinel `Count` 184, "Must be last." 179) | Well-known base-class-library types | A new well-known type; `Count` must stay last | CM-PUB |
| 9 | `Code/ReferenceKinds.cs` (`[Flags] : long` 16; `IsType = 1 << 26` 165; `All = -1` 23) | Syntactic reference positions | A new syntactic position in which a declaration can be referenced | CM-PUB |
| 10 | `Code/TypeKindConstraint.cs`, `VarianceKind.cs`, `ConstructorInitializerKind.cs`, `FieldKind.cs` (`TupleElement`), `Accessibility.cs`, `EnumerableKind.cs`, `ITypeParameter.cs` (`TypeParameterKind` 13-24) | Six further language-shape enumerations | Generic constraints, variance, constructor initializers, field-like constructs, access modifiers, iterator interfaces, type-parameter owners | CM-PUB |
| 11 | `Code/DeclarationExtensions.cs:53-93` `CanContain` (`ExtensionBlock` arm 71-73 already lists `Indexer`; `default: throw` 91-92) | The only exhaustive `DeclarationKind` switch in the public model | A new container declaration kind | CM-PUB |
| 12 | `Code/DeclarationExtensions.cs:105-143` — `IsMember` 105, `IsMemberOrNamedType` 120, `IsType` 128, `IsAssembly` 134, `IsNamedDeclaration` 140-143 | Five closed-world predicates with no default | A new declaration kind: all five silently answer `false` | CM-PUB |
| 13 | `Code/DeclarationExtensions.cs:220-228` `GetMembers(INamedType, DeclarationKind)` (`Indexer` already missing) | Kind to collection map | A new member kind; throws `ArgumentOutOfRangeException` | CM-PUB |
| 14 | `Code/DeclarationExtensions.cs:334-341` `ContainedChildren` (`_ => []`; does not visit `INamedType.ExtensionBlocks`), consumed at `:350,359` | The compilation walk | A new container: silently empty | CM-PUB |
| 15 | `Code/DeclarationExtensions.cs:406-437` `GetEffectiveAccessibility(IType)` (`default: return Accessibility.Public` 433-435) | Effective accessibility of a type | A new `IType` shape: silently public | CM-PUB |
| 16 | `Code/GenericExtensions.cs:42-50` `GetBase`, `:56-62` `GetDefinition` (`_ => null`, `_ => declaration`), `:299-334` the generic-instance resolver (`default: throw`), `:32` `IsSelfOrDeclaringTypeGeneric` | Generic-definition plumbing | A new member kind; two of the four are silent | CM-PUB |
| 17 | `Code/OperatorKindExtensions.cs:22-118` `GetCategory` (60 arms, `_ => throw` 117; section comments at 27,32,39,45,51,56,64,67,75,80,84,90,103,107,113) | Operator to category map | A new operator | CM-PUB |
| 18 | `Code/AccessibilityExtensions.cs:27-41`; `Code/RefKindExtensions.cs:23,32-43,48` | Accessibility flags; `IsByRef`, `IsWritable`, `IsReadable` | A new accessibility or ref kind; `IsByRef` and `IsReadable` are silent negations | CM-PUB |
| 19 | `Code/NamedTypeExtensions.cs:40-65` `MethodsAndAccessors`, `:72-102` `Members`, `:110-140` `AllMembers` | Hand-written member enumerations | A new member kind; `MethodsAndAccessors` already omits indexer accessors and `IEvent.RaiseMethod` | CM-PUB |
| 20 | `Code/ReferenceKindsExtension.cs:46-69` `ToDisplayString` (24 `ConsiderKind` calls; integer fallback 71-75 casting a `long` enum to `int`) | Reference-kind display | A new `ReferenceKinds` member; whole combinations degrade to a number, and flags at or above `1 << 31` truncate | CM-PUB |
| 21 | `Code/SignatureMatcher.cs:282-304` `GetParamsElementType`, `:293,298` `TypeKind` tests, `:307-` `GetIterationType` | The C# 13 `params`-collections rules | A change to what may be a `params` collection | CM-PUB |
| 22 | `Code/TypedConstant.cs:106,174-,223-243,273,471,478-492` | Constant-capable type list and the CLR array mapping | A new constant-capable type | CM-PUB |
| 23 | `Code/SourceReference.cs:29-47` (`Kind` returns the Roslyn `SyntaxKind` **as a `string`**), `Code/ISourceExpression.cs:85` `object AsSyntaxNode` | Stringly-typed, open-world Roslyn escape hatches | Any new Roslyn syntax kind, with no compiler assistance | CM-PUB |
| 24 | `Code/SyntaxBuilders/SyntaxBuilder.cs:39-41`, `ExpressionFactory.cs:160` `Parse`, `StatementFactory.cs:48` `Parse`, `ArrayBuilder.cs`, `SwitchStatementBuilder.cs:57-135` | String-in, engine-parses expression and statement builders | The language version used by the engine's parser, not by this assembly | CM-PUB |
| 25 | `Metalama.Framework/src/Metalama.Framework/Metalama.Framework.csproj:4` `<TargetFrameworks>netstandard2.0;net10.0</TargetFrameworks>`, `:17-33` the `InternalsVisibleTo` list naming `Metalama.Framework.Engine.5.0.0` and `.5.10.0` | The public model's only platform coupling | The .NET runtime version; the Roslyn variant set | CM-PUB |
| 26 | `Code/DeclarationBuilders/INamedTypeBuilder.cs:18` (`IsPartial`), `:20-42` three commented-out `TODO` blocks for `IsReadOnly`, `IsRef`, `PrimaryConstructor` | Where a `closed` or `union` setter lands | A new modifier or type form | CM-PUB |
| 27 | `Code/IMemberOrNamedType.cs:25-101`, `IMember.cs:23-52`, `INamedType.cs:51,192-202`, `IMethod.cs:27-92`, `IField.cs:46-66`, `IConstructor.cs:28-33`, `IParameter.cs:37-71`, `IHasType.cs:28`, `ITypeParameter.cs:66-88`, `IFieldOrPropertyOrIndexer.cs:20`, `IDeclaration.cs:108-123` | Every modifier-bearing property in the public model | A new modifier; each is an independent boolean and no switch enumerates them | CM-PUB |
| 28 | `CodeModel/Helpers/DeclarationExtensions.cs:40-65` `GetDeclarationKind( ISymbol, CompilationContext )` (line 44 is the only direct `INamedTypeSymbol.IsExtension` read; `_ => throw` 64) | The symbol to kind funnel that almost every downstream dispatch consumes | A new symbol shape or declaration kind | CM-ENG |
| 29 | `CodeModel/Factories/DeclarationFactory.Symbols.cs:117-132` `GetIType` (`_ => throw NotImplementedException` 130), `:159-199` `GetNamedType` (the three-way construction at 185-198), `:310-338`, `:376-505` `GetCompilationElementCore` (twelve `SymbolKind` arms, `default: throw` 503), `:507-` `GetTupleTypeFromSymbol`, `:649-709` `MakeNullableType` (659) | Kind to implementation dispatch | A new named-type-like kind; 185-198 is the insertion point | CM-ENG |
| 30 | `CodeModel/Factories/DeclarationFactory.Builders.cs:203-258` (219 the canonical `NamedType or ExtensionBlock` widening; 251 the extension-block arm; `_ => throw` 256) and one `Get<Kind>` per kind at 70,76,88,99,107,122,134,142,150,158,166,180,188 | Builder-data to declaration dispatch | A new introducible declaration kind | CM-ENG |
| 31 | `CodeModel/References/RefFactory.cs:101-137` `FromAnySymbol` (fifteen arms; `ExtensionBlock` 114-118; tuple short-circuit 103-106; `_ => throw` 133) | Kind to typed reference | A new declaration kind with its own public interface | CM-ENG |
| 32 | `CodeModel/References/RefExtensions.cs:140-154` `ToRef(INamedTypeSymbol)`, `:156`, `:159`, `:163-170` `ToRef(ITypeSymbol)` which does **not** replicate the extension check | Reference construction | A new named-type-like kind; the two overloads already disagree | CM-ENG |
| 33 | `CodeModel/References/RefExtensions.cs:90-136` `GetPossibleDeclarationInterfaceTypes`, **`#if DEBUG` only**; consumed by `SymbolRef.cs:62`, `IntroducedRef.cs:107`, `DeclarationFactory.Builders.cs:212` | The kind to legal `IRef<T>` interface table | A new kind and interface pair; the guard is absent from release builds | CM-ENG |
| 34 | `CodeModel/References/SymbolRef.cs:60-94` — three `Invariant.Assert` blocks; the one at 81-86 requires a `SymbolKind.NamedType` reference to be exactly `IExtensionBlock`, `ITupleType` or `INamedType`, and is not `#if DEBUG` | The reference-typing invariant | A new named-type-like kind reaching a reference through the wrong constructor | CM-ENG |
| 35 | `CodeModel/References/SymbolRef.Strategy.cs:99-175`; per-kind predicates at 215,218,222,224,227-237,239,272,276-313 (the `IsExtensionSafe` exclusion 298-301), 315-316, and the table `GetSymbolPredicate` 318-330; `:182,189,201` | Which symbols a member collection yields | A new member or type kind; also the design-time host | CM-ENG |
| 36 | `CodeModel/References/RefTargetKind.cs:7-31` ("WARNING! These values are serialized as strings and stored in compiled dlls. Do not rename."; `NamedType` 28, `ExtensionBlock` 29, `PrimaryConstructor` 30) and `RefTargetKindExtensions.cs:15-29,31-55` (`_ => throw` 54) | The serialized reference-target vocabulary | A new declaration kind; the names are a wire format | CM-ENG |
| 37 | `CodeModel/References/FullRef.cs:74-120` `GetAttributes`, `:165-188` `ApplyRefKind` (170 pairs `NamedType or ExtensionBlock` with `SymbolKind.NamedType`) | Reference-target resolution | A new reference target | CM-ENG |
| 38 | `CodeModel/References/SymbolNormalizer.cs:18,20,39-69,75-103` (the `IsExtensionSafe` short-circuit 44-47; permissive `_ =>` 103) | Symbol canonicalisation, including partial members | A new kind; a new partial-able member | CM-ENG |
| 39 | `CodeModel/Visitors/CompilationElementVisitor.cs:18-176` — the `TypeKind` switch 36-83 (`{ IsNamedType: true }` 43, `Extension` 48, `Tuple` 53, `default: throw` 81), the `DeclarationKind` switch 88-167 (`ExtensionBlock` 100, `default: throw` 165), virtuals 180-220 with `VisitExtensionBlock` 210 and `VisitTupleType` 212 defaulting to `VisitNamedType` | The compatibility device that kept every subclass working when two kinds were added | A new `TypeKind` or `DeclarationKind` | CM-ENG |
| 40 | `CodeModel/Visitors/CompilationElementVisitor{T}.cs:16-51` — the same two switches, but **no `TypeKind.Tuple` arm**; a tuple reaches `_ => throw` at 28 | The generic sibling of the previous row | A new `TypeKind`; already wrong for tuples | CM-ENG |
| 41 | `CodeModel/Visitors/TypeVisitor.cs:15-27` (the five kinds written longhand at 20; `Extension` 24, `Tuple` 25, `_ => throw` 26; virtual `VisitExtensionBlock` 39) | `IType` dispatch | A new `TypeKind` | CM-ENG |
| 42 | `CodeModel/Visitors/TypeSymbolRewriter.cs:38-52` — a switch on Roslyn `TypeKind`, eleven arms, `_ => throw ArgumentOutOfRangeException` 51, **no `TypeKind.Extension` arm** | Roslyn-side type rewriting | A new Roslyn `TypeKind`; already broken for extension blocks | CM-ENG |
| 43 | `CodeModel/Visitors/TypeRewriter.cs:42` with `CodeModel/Abstractions/ITypeImpl.cs:12` `Accept` | The only type dispatch that does not enumerate kinds | A new type class extends it by implementing an interface member | CM-ENG |
| 44 | `CodeModel/Visitors/DisplayStringFormatter.cs:146` (`DefaultVisit` throws), `:256-340` `VisitMethod` (`MethodKind` 275, `OperatorKind` 285), `:347-386`, `:388-394`, `:396-425`, `:427`, `:458` | Display strings | A new kind; fails loudly | CM-ENG |
| 45 | `CodeModel/Comparers/DeclarationEqualityComparer.Conversions.cs:72-140` — the switch at 90-136 with arms `Class` 92, `Interface` 100, `Delegate` 103, `TypeParameter` 121, `Array` 134, **no default**, falling out to `return false` 139; also 142-158, 160-, 292, 318, 370-380, 396, 420, 490 | The conversion rules behind `IType.Is()`, aspect eligibility, contract applicability and advice validation | A new reference-type-like kind: aspects silently skip their targets | CM-ENG |
| 46 | `CodeModel/Comparers/DeclarationEqualityComparer.cs:240-247,250-280,325-356` (`DeclarationKind.NamedType` at 331, not widened for `ExtensionBlock`) | Type-definition identity | A new type-like kind | CM-ENG |
| 47 | `CodeModel/Comparers/SignatureTypeComparer.cs:36-` `Equals` and `:103-130` `GetHashCode` — two parallel `SymbolKind` lists that must stay in step | Signature comparison | A new symbol kind | CM-ENG |
| 48 | `CodeModel/Comparers/TypeOrderingComparer.cs:22-57` — **line 39 `(int) x.TypeKind - (int) y.TypeKind`**; dispatch 46-56 with `_ => CompareNamedTypes` 55 | Deterministic output ordering by `TypeKind` ordinal | Inserting rather than appending a `TypeKind`; a non-named-type-like kind throws `InvalidCastException` at 55 | CM-ENG |
| 49 | `CodeModel/UpdatableCollections/*.cs` (one per member kind; `ExtensionBlockUpdatableCollection.cs:17-25` derives from `NonUniquelyNamedUpdatableCollection<T>`), `CodeModel/Collections/ExtensionBlockCollection.cs:14-23` | Per-kind collections | A new member or type kind; the base-class choice is the substantive decision | CM-ENG |
| 50 | `CodeModel/CompilationModel.Members.cs:24-40` (one dictionary field per kind, `_extensionBlocks` 40), `:188-193`, `CompilationModel.cs:245`, `:349` | The compilation's member storage | A new member or type kind | CM-ENG |
| 51 | `CodeModel/CompilationModel.Members.cs:391-530` `AddDeclaration( DeclarationBuilderData )` (finalizer 397 through extension block 490-494 and namespace 496; `default: throw` 527) and `:291-342` | Builder-data type-pattern dispatch | A new introducible kind | CM-ENG |
| 52 | `CodeModel/Helpers/ModifierCategories.cs:10-24` — `[Flags]` 1 to 1024 with a hand-maintained `All` at 23 | The modifier-category vocabulary | A new modifier: a new bit **and** an edit to `All` | CM-ENG |
| 53 | `CodeModel/Helpers/ModifierHelper.cs:22-56` `GetSyntaxModifierList` (**no `ExtensionBlock` arm**, `default: throw` 53), `:58-74`, `:76-196` `GetMemberSyntaxModifierList` (accessibility 87, `required` 93, `static` 100, `partial` 105, `extern` 110, inheritance 115-162, `readonly` 164, `const` 171, `unsafe` 178, `volatile` 183, `async` 190), `:198-236` `GetTypeSyntaxModifierList` (accessibility, `static`, `new`, `abstract`, `sealed` only), `:238-311`, `:313-335`, `:340-385` (`default: throw` 382) | The whole modifier surface | A new modifier; `closed` lands at 198-236 | CM-ENG |
| 54 | `CodeModel/Source/SourceParameter.cs:50-59` Roslyn `RefKind` to Metalama `RefKind` (`_ => throw InvalidOperationException` 58) | The model for a loud kind mapping | A new `RefKind` | CM-ENG |
| 55 | `CodeModel/Source/SourceNamedTypeImpl.cs:59`, `:69-79` (Roslyn to Metalama `TypeKind`; `InvalidOperationException` outside Class/Delegate/Enum/Interface/Struct/Error), `:84-129` `GetSpecialTypeCore` (name and namespace string matching 100-127), `:169-173`, `:317-327`, `:329-352` `IsPartial` (`_ => default` 347) | The source named-type implementation | A new Roslyn `TypeKind`; a new modifier read from syntax | CM-ENG |
| 56 | `CodeModel/Source/ExtensionBlock.cs` and `ExtensionBlockImpl.cs:21,24,30,32,34,37,39` | The facade plus implementation pair that is the template for a new type kind | A new type kind | CM-ENG |
| 57 | `CodeModel/Helpers/PropertyKind.cs:10-28`; `CodeModel/Helpers/DeclarationExtensions.cs:292-378` `GetPropertyKind` (the syntax switch at 342-349 returns `_ => false`), `:393-413` `ContainsFieldKeyword` | The C# 14 `field` keyword detection | A new syntactic wrapper around a `field` expression | CM-ENG |
| 58 | `Utilities/Roslyn/SyntaxHelpers.cs:93-96` `ContainsFieldExpression`, `:103-145` `ContainsFieldAssignment` and `IsFieldAssignment` — an exhaustive hand-written list of every assignment `SyntaxKind` (113-126), the increment kinds (129-136) and `SyntaxKind.Argument` (139) | The best example in the repository of what a new expression form costs | A new assignment or expression form | CM-ENG |
| 59 | `CodeModel/Helpers/DeclarationExtensions.cs:380-391`, `:415-425`, `:427-457`, `:459-469`, `:471-485` | Syntax-form predicates, all falling to `false` or `null` | A new member declaration form | CM-ENG |
| 60 | `CodeModel/Helpers/IteratorHelper.cs:32-71` (`_ => false` 64; `IAsyncEnumerable` matched by name and namespace string at 99-106) and `IteratorHelper.FindYieldVisitor.cs:16-44` (stops at `ExpressionSyntax` and `LocalFunctionStatementSyntax` 28-30) | Iterator detection | A new declaration form; a new expression form that can contain statements | CM-ENG |
| 61 | `Utilities/Roslyn/SyntaxKindExtensions.cs:33-35` `IsTypeDeclaration` (**`ExtensionBlockDeclaration` absent**), `:41`, `:47`, `:53`, `:59-63`, `:69-72`, `:78-81`, `:86-88`, `:94`, `:100`, `:106-108`, `:114` | The shared syntax-kind family predicates | A new type declaration syntax kind; already wrong for extension blocks | CM-ENG |
| 62 | `Utilities/Roslyn/TypeKindExtensions.cs:22-24` `IsNamedType` (`Extension` and `Tuple` deliberately excluded), `:29`; `Utilities/Roslyn/SymbolKindExtensions.cs:22-38` | The "is this kind in that family" helpers | A new named-type-like kind must decide which side it lands on, and the decision propagates everywhere | CM-ENG |
| 63 | `Utilities/Roslyn/OperatorData.cs` (287 lines; the record at 17; the C# 14 compound-assignment block 151-265; `_byMemberName` 276, `GetByKind` 278, `GetByName` 281) with its `MinimumLangVersion` column | The template for "a new construct that is a table row rather than a switch arm" | A new operator and its minimum language version | CM-ENG |
| 64 | `Utilities/Roslyn/SymbolExtensions.cs:36`, `:50-104` (`ToOurSpecialType` and `ToRoslynSpecialType`, 21 entries each, permissive `_ => None`), `:283-` (289 uses `IsTypeDeclaration`), `:318-320`, `:384-387` `IsExtensionSafe`, `:479` | The containment device for Roslyn-version differences, and the special-type mapping | A new Roslyn API; a new `SpecialType` | CM-ENG |
| 65 | `Utilities/Roslyn/LanguageVersionExtensions.cs:12-40` `ToDisplayStringSafe` (`(LanguageVersion) 1300 => "13.0"` 33, `1400 => "14.0"` 34, `_ => throw` 39) | Diagnostic formatting of a language version | A new language version; an unmapped version makes LAMA0051, LAMA0052, LAMA0232 and LAMA0282 formatting throw | CM-ENG |
| 66 | `Metalama.Framework.Engine/SerializableIds/**` (3648 lines, sixteen files) — Metalama's own fork of Roslyn's `DocumentationCommentId` | Every grammar change that changes the shape of a declaration identifier | A new declaration form | CM-ENG |
| 67 | `SerializableIds/SerializableDeclarationIdProvider.FromSymbol.cs:30-160` (a `SymbolKind` switch; the nested `default:` switch 113-158 with `throw` at 155) | Declaration identifier generation | A new symbol kind | CM-ENG |
| 68 | `SerializableIds/DocumentationIdHelper.Parser.cs:336,517,544,551,607,715,764,779` — the eight sites the C# 14 wave widened from `NamedType` to `NamedType or ExtensionBlock`; plus `DocumentationIdHelper.GeneratorOfReferenceIdFromDeclaration.cs:28` | The most mechanical evidence of the widening pattern | A new member-carrying declaration kind | CM-ENG |
| 69 | `SerializableIds/SerializableTypeIdGenerator.cs:93-113` and `:161-198` `IsWrittenInAnnotatedContext` (`Class or Struct or Interface or Delegate or Enum or Error or Tuple` 182-183, **`Extension` absent**; `default: return false` 195), `:117` | Whether a serialized type identifier carries the nullable-annotated marker | A new `TypeKind`; a wrong answer yields a valid-looking identifier denoting a different type | CM-ENG |
| 70 | `SerializableIds/SerializableTypeIdResolver.cs:104,138,249,272,284,354-410,415-423,441 (DefaultVisit throws),447-466` | The type-identifier parser; parses through `SyntaxFactoryEx.ParseExpressionSafe` with no explicit parse options | A new type syntax form; the ambient default language version | CM-ENG |
| 71 | `SerializableIds/SerializableTypeIdResolverForIType.cs:127-130` — `Namespace` and `NamedType` only, then `_ => throw AssertionFailedException` | One of the eight widenings that was not applied | A new container kind | CM-ENG |
| 72 | `CodeModel/Helpers/DeclarationCache.cs:29-33,44-48`, `CodeModel/References/DurableRef.cs:160-164`, `CodeModel/ProjectModel.ProjectFeaturesImpl.cs:76-80` | The only target-framework conditionals in the code model | The Core and Desktop flavour split; all four are now always-true candidates | CM-ENG |
| 73 | `CodeModel/ProjectModel.ProjectFeaturesImpl.cs:23-49` and `:55-86` `TargetFrameworkSupportsCovariantReturn` (string surgery: `net` plus digit 58-60, dot-less rejection 67-72, major parse 77 and 79, `major >= 5` 82) | The only target-framework-moniker parser in the code model | The moniker grammar; `net11.0` parses correctly today | CM-ENG |
| 74 | `eng/RoslynVersions/Roslyn.5.10.0.props:8-10` and `:22-24` — "`ROSLYN_5_10_0_OR_GREATER` is defined by this variant only. No production source branches on it." | The assertion that the C# 15 work will falsify | The first production `#if` on the constant | CM-ENG, BUILD |
| 75 | `eng/src/GenerateMetaSyntaxRewriter/GenerateMetaSyntaxRewriter.cs:16-18` (`deprecatedVersionNames`; `legacyVersionNames = ["4.0.1","4.4.0","4.8.0","4.12.0"]`; `versionNames = [..legacy, "5.0.0", "5.10.0"]`), `:37`, `:39-48` | The hard-coded Roslyn grammar version list and the five generated artefacts | Adding or dropping a Roslyn variant; `RoslynApiVersion` ordinals shift if a legacy entry is removed rather than deprecated | TMPL, BUILD |
| 76 | `eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs:70-78` `RemoveExperimentalDeclarations`, `:90-109` `RemoveExperimentalChildren` (rationale 60-69: `RSEXPERIMENTAL`) | **The single gate that hides all five C# 15 grammar additions today** | The `ExperimentalUrl` attribute in the grammar snapshot | TMPL, BUILD |
| 77 | `eng/src/GenerateMetaSyntaxRewriter/Model/VersionDetector.cs:11-57` (node minimum 36-48; per-field minimum and maximum, and per-kind minimum, 50-75) | Per-node, per-field and per-kind Roslyn version detection | A new node, field or field kind | TMPL, BUILD |
| 78 | `eng/src/GenerateMetaSyntaxRewriter/Generator.cs:64-98`, `:100-174` (`IsVersionSpecificType` 160, `IsVersionSpecificField` 162, `GetVersionSpecificKinds` 164-173), `:396-525` (the `switch ( this.TargetApiVersion )` at 432-479, `default: throw` 476-477), `:527-613`, `:615-712`, `:714-723`, `:725-735`, `:737-803` | The whole generated layer | A new grammar element; no generator edit is needed for any of the five kinds of addition | TMPL, BUILD |
| 79 | `eng/src/GenerateMetaSyntaxRewriter/Generator.cs:301-389` `IsKeyword` — a hand-written list of 80 C# keywords used to escape generated parameter names (`unsafe` 381, `required` 382, `file` 383; **`union` and `closed` absent**) | Identifier escaping for generated parameter names | A grammar field named after a reserved word | TMPL, BUILD |
| 80 | `eng/src/GenerateMetaSyntaxRewriter/Model/TreeFlattening.cs:292-324` (`Choice` children forced optional 311; `default: throw InvalidOperationException` 321) | Grammar flattening | A new kind of grammar child element; fails loudly | BUILD |
| 81 | `eng/src/GenerateMetaSyntaxRewriter/Syntax-{4.0.1,4.4.0,4.8.0,4.12.0,5.0.0,5.10.0}.xml` (3008, 3067, 3103, 3120, 3199 and 3245 lines; 237, 240, 243, 245, 249 and 252 `<Node>` elements) | The grammar snapshots, copied verbatim from Roslyn | A new Roslyn version | BUILD |
| 82 | `Metalama.Framework/.generated/<version>/**` (git-ignored, `.gitignore:62`; `MetaSyntaxRewriter.g.cs` is 10 436 lines for 5.0.0) with its wiring at `Metalama.Framework.Engine.csproj:37-38` and `Metalama.Framework.DesignTime.csproj:34-35`, and a `-stubs` fallback directory that does not exist | The generated layer and its build wiring | The Roslyn variant name; a missing directory compiles the variant with no hasher at all | TMPL, DT |
| 83 | `Templating/TemplateAnnotator.cs` (3516 lines): `VisitCore` 627, `DefaultVisitImpl` 630, `AddScopeAnnotationToVisitedNode` 648-698, `GetExpressionScope` 446-590, `GetExpressionTypeScope` 434-444, `GetSymbolScope` 181-297, `ReportUnsupportedLanguageFeature` 2591, the `#region Unsupported Features` 2589-2719, and the sixty-odd per-construct overrides | The classifier that decides compile-time versus run-time for every syntax node | Every new syntax node; there is **no** "I do not know this construct" branch | TMPL |
| 84 | `Templating/TemplateAnnotator.cs:743-776` — `VisitClassDeclaration` 743, `VisitStructDeclaration` 745, `VisitRecordDeclaration` 748, `VisitDelegateDeclaration` 751, `VisitEnumDeclaration` 754, shared `VisitTypeDeclaration<T>` 756 with the run-time early exit at 765-774. **No `VisitInterfaceDeclaration`, no `VisitExtensionBlockDeclaration`** | Type-declaration classification | A new type declaration kind | TMPL |
| 85 | `Templating/TemplateAnnotator.cs:1375-1379` (`VisitBreakStatement` and `VisitContinueStatement` annotating with `CurrentBreakOrContinueScope`) and `TemplateAnnotator.ScopeContext.cs:21,123` — a single scope, no label map | Break and continue scope | A labelled `break`, which targets an *outer* construct | TMPL |
| 86 | `Templating/TemplateAnnotator.cs:3495-3503` `VisitCollectionExpression`, with the child filter at `:693` (`n is ExpressionSyntax or InterpolationSyntax`) and the empty-child rule at `:448-451` (`RunTimeOrCompileTime`) | Collection-expression classification | A collection element with no `ExpressionSyntax` child, such as `WithElementSyntax` | TMPL |
| 87 | `Templating/TemplateAnnotator.cs:2594-2599` `VisitUnsafeStatement`, `:2601-2606` `VisitGotoStatement`, `:2668` `VisitQueryExpression` — all LAMA0101 | The refusal shape a new construct copies | A new construct to be refused | TMPL |
| 88 | `Templating/TemplateAnnotator.cs:2710` `VisitFieldExpression`, `:3450-3483` `VisitInterpolatedStringExpression` (`AssertionFailedException` 3483), `:2041-2099` `VisitAssignmentExpression` | The C# 14 support that already landed here | Precedent for a new expression form | TMPL |
| 89 | `Templating/TemplateCompilerRewriter.cs` (3299 lines, 138 `SyntaxKind.` references): `GetTransformationKind` 196, `IsCompileTimeCode` 199-264 (`GetFromParent` 236-263, `AssertionFailedException` 258), `VisitFieldExpression` 310-316, `Transform(SyntaxToken)` 394, `TransformIdentifierName` 476-501, `CreateRunTimeExpression` 641- (expression-kind switch 649-731, type-name switch 800-853), `VisitInvocationExpression` 1182, `VisitBlock` 1952, `BuildRunTimeBlock` 1981-2055, `GetFunctionLikeRunTimeBlockInfo` 2011-2046, `ToMetaStatement` 2212-2221, `ToMetaStatements` 2232-2347, `TransformInterpolatedStringExpression` 2444-2492, `VisitSwitchStatement` 2548-2598 (control-transfer list 2568-2575), `VisitAssignmentExpression` 3245, `TransformAssignmentExpression` 3293-3299 | The T# to C# rewriter | A new statement or expression form | TMPL |
| 90 | `Templating/TemplateCompilerRewriter.BuildTimeOnlyRewriter.cs:52-176`; `TemplateCompilerRewriter.StatementCompileTimeVariableFinder.cs:55-169` (a plain `CSharpSyntaxWalker`, not `SafeSyntaxWalker`) | The compile-time-only rewriter and the variable finder | New assignment or designation forms | TMPL |
| 91 | `Templating/MetaSyntaxRewriter.cs:42-54` (`TargetApiVersion`), `:59`, `:106-139` `Transform<T>` (**`AssertionFailedException( $"Unexpected node kind: {node.Kind()}." )` at 132** — the arm a `CollectionElementSyntax` would hit), `:144-237`, `:239-294` (kind-generic token transform), `:300` | The hand-written half of the generated rewriter | A new abstract node category | TMPL |
| 92 | `Templating/MetaSyntaxRewriter.MetaSyntaxFactoryImpl.cs:78-82` `Kind( SyntaxKind )` — emits `SyntaxKind.<name>` by `ToString()` | Kind emission | A new `SyntaxKind` needs no change here | TMPL |
| 93 | `Templating/RoslynVersionSyntaxVerifier.cs:17-89` — `MaximalAcceptableLanguageVersion` 22, `MaximalUsedVersion` 24, `OnForbiddenSyntaxUsed` 32-38 (LAMA0232), `VisitVersionSpecificNode` 41-52, `VisitVersionSpecificField` 55-75 (with the recorded generalising-field defect at 57-61), `VisitVersionSpecificFieldKind` 78-89; none calls `base.Visit` | The gate that refuses a template written in too new a language | The Roslyn to language version map; the experimental filter | TMPL |
| 94 | `Templating/TemplatingCodeValidator.Visitor.cs:34,40,84,95-243` (early return 134-137), `:284-813` (per-declaration overrides; **no `VisitEnumDeclaration`, no `VisitExtensionBlockDeclaration`**), `:1112-1129` (`_ => throw AssertionFailedException` 1128) | Reference validation inside compile-time code | A new declaration kind: the body is never validated | TMPL |
| 95 | `Templating/TemplateExpansionContext.cs:606-657`, `:799-846` `HasAnyYieldVisitor.DefaultVisit` (an **allow-list of 24 statement kinds** at 807-836), `:861-878` `CheckTemplateLanguageVersion` (LAMA0282), `BackingFieldName`; `TemplateExpansionContext.HasYieldInTryCatchVisitor.cs:22-35` | Template expansion, iterator detection, and the consumer-side version warning | A new statement kind; the language version of the consuming project | TMPL |
| 96 | `Templating/TemplateSyntaxFactoryImpl.cs:105-205,206-254,365-425,430-445,501-519,704-740,933-942` (`EscapeIdentifier`, the `field` contextual-keyword hook), `:944-980` (`RewriteAssignmentExpression`, whose comment names the Roslyn 5 parse shape), `:982-989`, `:991-999` | The run-time half of `ITemplateSyntaxFactory` | New expression forms; a Roslyn parse-shape change | TMPL |
| 97 | `Metalama.Framework/src/Metalama.Framework.CompileTimeContracts/ITemplateSyntaxFactory.cs:132,137` and `Metalama.Framework/Aspects/CompiledTemplateAttribute.cs:43,49` | The contract between compiled templates and the engine | A new construct whose behaviour must be carried on the template | TMPL, ADV |
| 98 | `Templating/TemplatingDiagnosticDescriptors.cs:20,24-30` (LAMA0101), `:247-253` (LAMA0232), `:618-625` (LAMA0282) | The three descriptors carrying the language-version contract | Refusing or gating a construct costs no new descriptor | TMPL |
| 99 | `Templating/Expressions/SyntaxBuilderImpl.cs:69-80,308-330` — `ParseExpression` and `ParseStatement` with **no `CSharpParseOptions`**; the same shape at `TemplateSyntaxFactoryImpl.cs:79,646` and `Expressions/DurableExpression.cs:66,99`, through `SyntaxGeneration/SyntaxFactoryEx.cs:367-382` | User-supplied source text parsed at the running Roslyn's default language version | Any construct the running Roslyn parses but the project does not accept | TMPL |
| 100 | `Templating/Statements/SwitchStatement.cs:213-294` (appends a bare `BreakStatement()` at 277), `Statements/StatementList.cs:85-109`, `Statements/UnwrappedBlockStatementList.cs:134` | The statement builder API | A labelled `break`; nothing here knows about one | TMPL |
| 101 | `Templating/CompileTimeSideEffectDetector.cs:104-193` — returns **`true`** (report LAMA0288) for anything it does not recognise (116-118) | Side-effect detection that errs toward a false positive | A new expression form | TMPL |
| 102 | `Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:31-32` `Latest => CSharp14`, `:38-43` `All`, `:45`, `:50` `DefaultParseOptions`, `:52-62` `ToLanguageVersion` (**both `V5_0_0` and `V5_10_0` map to `CSharp14`**), `:77-87` `ToNuGetVersionString` (85), `:93,99,104`, `:117-132`, `:134-144`, `:149-159` `GetMaxLanguageVersion` (`(>= 5, _) => CSharp14` 152) | The single most-referenced version table in the repository | Every language, Roslyn and package-version decision | CT, BUILD, TMPL, SYNGEN, TEST |
| 103 | `Metalama.Framework.Engine/Utilities/AllLanguageVersions.cs:14-18` — `CSharp10 = 1000` through `CSharp14 = 1400`; a `CSharp15 = 1500` constant does not exist | Roslyn-independent `LanguageVersion` constants | Naming a language version the compiling Roslyn does not declare | CT |
| 104 | `Metalama.Framework.Engine/Utilities/LanguageVersionProvider.cs:29`, `:35`, `:45-72` `GetLanguageVersionFromDotNetSdk` (**`>= 10 => CSharp14`** at 56; `_ => throw PlatformNotSupportedException` 59; minimum with the project version 62-71), `:74-123` `GetLanguageVersionFromMSBuild` (88, 107, 111) | The one place where the .NET SDK major version becomes a C# version | The .NET 11 SDK matches `>= 10` and yields C# 14, silently | CT, BUILD, DT |
| 105 | `Metalama.Framework.Engine/Utilities/ILanguageVersionProvider.cs:10-17`; the test double `Metalama.Testing.UnitTesting/TestLanguageVersionProvider.cs:12` returning `SupportedCSharpVersions.Latest` unconditionally | The service contract and its test double | The test double hides the SDK-derived path | CT, TEST |
| 106 | `Options/MSBuildProjectOptions.cs:167-183` — the silent fallback to `SupportedCSharpVersions.Latest` when `LanguageVersionFacts.TryParse` fails (comment 174-178) | The project language version | A `LangVersion` the hosting Roslyn cannot parse | CT, DT |
| 107 | `Templating/TemplateCompiler.cs:33,51,55-79` (LAMA0052 at 68-73), `:106`, `:232` | The template language version | The MSBuild property and the compile-time version | CT, TMPL |
| 108 | `Directory.Build.props:16` `<MetalamaTemplateLanguageVersion>14.0</MetalamaTemplateLanguageVersion>` (comment 11-15 ties the ceiling to `RoslynApiMinVersion`), with plumbing at `Options/MSBuildPropertyNames.cs:44,93`, `Options/IProjectOptions.cs:215`, `MSBuildProjectOptions.cs:153`, `DefaultProjectOptions.cs:113`, `ProjectOptionsWrapper.cs:99`, `Metalama.Framework.Package/build/Metalama.CompilerVisibleProperties.props:32` | The repository-wide template language ceiling | `RoslynApiMinVersion` | CT, BUILD, TEST |
| 109 | `Pipeline/CompileTime/CompileTimeAspectPipeline.cs:62-93` `VerifyLanguageVersion` (LAMA0051 at 75, LAMA0052 at 85), called at `:177`; the comment at 64-65 | The only language-version gate | There is no equivalent in the design-time, preview or live-template pipelines | CT, DT |
| 110 | `CompileTime/CompileTimeCompilationBuilder.ProduceCompileTimeCodeRewriter.cs:204-252` (**no `VisitExtensionBlockDeclaration`**), `:254-373` (272, 356-357, `default: break` 369-372), `:417-447`, `:508-563` (indexer `NotImplementedException` 515-516; `default:` copies through 555-558), `:642`, `:936-1170`, `:1248`, `:328`, `:1452-1478`, `:1526-1535`, `:1562-1580`, `:1584,1602,1637,1684,1701,1718,1778-1790` | The largest construct-shaped switch in the compile-time subsystem | A new type declaration or member form inside compile-time code | CT |
| 111 | `CompileTime/CompileTimeCompilationBuilder.FindCompileTimeCodeVisitor.cs:58-99` — six `Visit*Declaration` overrides, no fallback classifying an unknown type declaration | Whether a file contains compile-time code at all | A new type declaration kind: the file is excluded with no diagnostic | CT |
| 112 | `CompileTime/CompileTimeCompilationBuilder.cs:123-167` `ComputeSourceHash`, `:169-247` `ComputeProjectHash` (`TemplateLanguageVersion` 239, `RoslynApiVersion.Current` 243; **neither `SdkVersion` nor the resolved compile-time language version**), `:360`, `:411-454` (the `languageVersion >= CSharp14` guard on `EMBED_SYSTEM_TYPES` 425), `:1282`, `:1355` | The compile-time assembly cache key | An SDK upgrade that changes the compile-time language version does not change the hash | CT |
| 113 | `CompileTime/CompileTimeAssemblyLocator.cs:43` `_defaultCompileTimeTargetFrameworks = "netstandard2.0;net8.0;net48"`, `:194`, `:209-224` (LAMA0084), `:234-243`, `:261-296`, `:389-430`, `:664`, `:705`, `:735-775` (`<LangVersion>latest</LangVersion>` 751; the Roslyn package reference 756), `:777-830`, `:838-848` | The reference-assembly project built on the user's machine | The .NET SDK; the target-framework set; the prerelease Roslyn feed | CT, BUILD |
| 114 | `CompileTime/ReferenceAssemblyBuildFailureClassifier.cs:83,115-152 (NU1101, NETSDK1045),177-208,302-306` | Explanations for the nested build's failures | The SDK version; the prerelease feed | CT |
| 115 | `CompileTime/Manifest/CompileTimeProjectManifest.cs:59-62,88,90,92,96-97,99-101` (`ResolvedLanguageVersion`, which has no callers) | The compile-time project manifest | A language version written by a newer Metalama and read by an older one | CT |
| 116 | `CompileTime/Manifest/TemplateSymbolManifest.cs:31,43,49,59,61,73` — `RoslynApiVersion? UsedApiVersion`, serialized as a bare **ordinal** whose value is positional in `GenerateMetaSyntaxRewriter.cs:17-18` | The wire form of the Roslyn API version | Removing a legacy grammar version shifts every ordinal | CT, BUILD |
| 117 | `CompileTime/CompileTimeProjectRepository.Builder.cs:62`, `:526-561` (LAMA0081, LAMA0078), `:581-590` (LAMA0061), `:596-604` | Cross-version guards and manifest parse options | A manifest without a language version | CT |
| 118 | `CompileTime/SymbolClassifier.cs:245,256,276,347,463,514,615,623,673,958,1093,1129,1151,1206-1210`; `CompileTime/CompileTimeTypeResolver.cs:70,89,110,113,116,130` | Symbol-kind and type-shape reasoning in the compile-time classifier | A new `SymbolKind` or type-symbol shape | CT |
| 119 | `CompileTime/RunTimeAssemblyRewriter.cs:88,140,191,208,213,255,273,308,339,398,444,468-472`; `CompileTime/RewriterHelper.cs:50-222` | The run-time assembly rewriter and the "make this member abstract-bodied" switch | A new member form; the C# 14 `field` keyword | CT |
| 120 | `CompileTime/CompileTimeCodeFastDetector.cs:41-84` — recurses only into `CompilationUnit`, `NamespaceDeclaration` and `FileScopedNamespaceDeclaration`; `DefaultVisit` returns `false` (83) | The syntactic pre-filter that chooses which code hasher runs | A new using-directive container | CT, DT |
| 121 | `CompileTime/UnloadableCompileTimeDomain.cs:5,38,51` — the only target-framework conditional in the compile-time subsystem | Assembly load contexts | The Core and Desktop split | CT |
| 122 | `CompileTime/OutputPathHelper.cs:27-96` and `Metalama.Framework/docs/compile-time-target-frameworks.md` | The run-time `FrameworkName` as a path segment of the compile-time output directory | The user's target framework | CT |
| 123 | `CompileTime/DesignTimeCompatibility.cs:33,42-48` (`MinimumSupportedVersion = new( 2026, 1 )`), consumed at `CompileTimeProjectRepository.Builder.cs:548-554` | The design-time generation floor | The Metalama version of a referenced project | CT, DT |
| 124 | `Options/IProjectOptions.cs:75-88,123,197-205,215,261-270`; `Options/MSBuildProjectOptions.cs:87-93,108,144-153,186-191`; `Options/DefaultProjectOptions.cs:56` (`TargetFramework => "net8.0"`), `:77,101-113,127-131`; `Options/MSBuildPropertyNames.cs:24,31,41-44,54-58` | The project-option surface and its MSBuild binding | The target framework, the SDK version, `MSBuildBinPath` | CT, BUILD |
| 125 | `Options/TargetedAssemblyReference.cs:19-20` (`… ? "net472" : "net10.0"`), `:22-24` `SatisfiesCurrentProcess` — **two exact-equality comparisons**; consumed at `Extensibility/ExtensionLoaderBase.cs:29-38` (whose own copy of the ternary at 31 is used only in a trace message) | Extension-assembly selection | The Core flavour's target-framework name; the Roslyn variant version. A mismatch produces an empty list and no diagnostic | CT, EXT, DT, PREM |
| 126 | `Metalama.Framework.CompilerExtensions/RoslynVariantPolicy.cs:22,30-54`; tests at `Metalama.Framework.Tests.UnitTests/Utilities/RoslynVariantPolicyTests.cs:21-79` including `LatestVersionSelectsThe5100Variant` | The run-time Roslyn variant table | Adding or dropping a variant; the latest variant is the catch-all for any future Roslyn | DT, BUILD |
| 127 | `Metalama.Framework.CompilerExtensions/ResourceExtractor.cs:31,35-36,54,77-79,83,89-108,157-172,180-211,244,466-485,539-603,605-631,633-656` | Payload extraction and host detection | The .NET flavour, the host Roslyn version, the host process | DT |
| 128 | `Metalama.Framework.CompilerExtensions/AssemblyResolutionPolicy.cs:24-25,31-35,51-60,61-89` | Exact-version binding for embedded assemblies (issue #1833) | Several Metalama builds in one process | DT |
| 129 | `Metalama.Framework.CompilerExtensions/ProcessKindHelper.cs:19-58,62-70` (the C# Dev Kit language server is absent and falls to `ProcessKind.Other`), duplicated in `Metalama.Backstage/src/Metalama.Backstage/Utilities/ProcessUtilities.cs:34-138` which says so at 36-37 | Host detection, in two independent copies | A new or renamed host process | DT, BACK |
| 130 | The entry-point shims `Metalama.Framework.CompilerExtensions/{MetalamaDiagnosticAnalyzer.cs:22-58,MetalamaSourceGenerator.cs:18-60,MetalamaDiagnosticSuppressor.cs:19-51,MetalamaGeneratedCodeAnalyzer.cs:20-35,AdditionalDiagnosticAnalyzer.cs:22-28,MetalamaSourceTransformer.cs:23-63}` and `Metalama.Framework.EditorExtensions/{MetalamaCodeFixProvider.cs:22-58,MetalamaCodeRefactoringProvider.cs:87-123}`; names in `Metalama.Framework.DesignTime/RoslynEntryPointTypeNames.cs:17-32` | Per-process-kind dispatch; only `MetalamaSourceTransformer` fails loudly (LAMA0087 at 23-31, reported at 52) | A host with no loadable payload variant | DT |
| 131 | `Metalama.Framework.DesignTime/Pipeline/Diff/PartialTypesVisitor.cs:12,18-67` and `Pipeline/Diff/PartialTypesHasher.cs:15,43-67` — the same `{class, struct, record}` enumeration twice; consumed at `Pipeline/Diff/DiffStrategy.cs:112,129` | Which type declarations can be partial | A new partial-able type declaration; the two copies must agree | DT |
| 132 | `Metalama.Framework.DesignTime/Refactoring/CSharpAttributeHelper.cs:28,71,74-191` (nineteen `SyntaxKind` cases, `default: return null` 189-190; already missing `RecordDeclaration`, `RecordStructDeclaration`, `ExtensionBlockDeclaration`, `InitAccessorDeclaration`, `FileScopedNamespaceDeclaration`), `:266,272` | Where the "Add aspect" refactoring inserts an attribute | A new declaration kind: the code action produces no edit | DT |
| 133 | `Metalama.Framework.DesignTime/CodeFixes/TheCodeFixProvider.cs:173,187-193` — matches `BaseTypeDeclarationSyntax`, so it is already correct for a union | The model to follow: match the base type, not the kind | Modifier ordering for a new modifier | DT |
| 134 | `Metalama.Framework.DesignTime/DiagnosticAnalysis/TheDiagnosticAnalyzer.cs:420-486` `TryMapLocation` — matches a token **by its text** among the direct child tokens (459-460), warns and drops on ambiguity (451, 465) | Remapping a diagnostic onto a newer syntax tree | A node that gains a second child token with the same text | DT |
| 135 | `Metalama.Framework.DesignTime/Pipeline/Diff/BaseCodeHasher.cs:19-80` over `SafeSyntaxWalker`, with the generated `RunTimeCodeHasher.g.cs` and `CompileTimeCodeHasher.g.cs`; every generated `Visit<Node>` **overrides** the base and never calls it | The design-time incremental invalidation hash | A node or field absent from the variant's grammar snapshot contributes nothing to the hash | DT |
| 136 | `Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs:55-57,65-78,84-106 (LAMA0049, issue #1767),113-127 (the C# 14 edit at 115; `default: throw` 125),247-280,381-388,456-504,506-523,662-695,697-790 (`_ => throw` 788),792-815,817-823` | What the editor sees; where the C# 14 wave landed at design time | A new declaration kind, a new `TypeKind`, a modifier that must be repeated on every partial declaration | DT |
| 137 | `Metalama.Framework.DesignTime/Services/RemoteWorkspaceProvider.cs:26-89` (the Roslyn 4 field versus Roslyn 5 property accommodation at 50-58), `Services/AnalysisProcessInvalidationService.cs:36,44-84`, `Services/UserProcessInvalidationService.cs:50,58-68` | Three private-reflection bridges into Roslyn internals, all null-tolerant | A Roslyn internal that moves or is renamed | DT |
| 138 | `Metalama.Framework.DesignTime/VisualStudio/ServiceHub/ServiceHubClientEndpoint.cs:35-83` (VS 2022 at 59-66, VS 2026 at 67-71, else log and `false` 72-78) and `VisualStudio/Rpc/PipeNameProvider.cs:13-19` | Cross-process discovery | A new Visual Studio generation (precedent: commit `5146c0a252`, issue #1096) | DT |
| 139 | `Metalama.Framework.DesignTime.Contracts/EntryPoint/DesignTimeEntryPointManager.Consumer.cs:30-43`, `EntryPoint/CurrentContractVersions.cs:22`, `DesignTimeEntryPointManager.cs:23,38-39,107`, `AssemblyInfo.cs:7` | The frozen cross-version contract surface | A new contract version added on one side only | DT |
| 140 | `Metalama.Framework.DesignTime.Contracts/Metalama.Framework.DesignTime.Contracts.csproj:16-27,30-33,35,38-43` — Roslyn pinned by `VersionOverride="4.0.1"`, with a stale comment | The binary-frozen contract assembly | Nothing; the pin is about the contract, not the host | DT |
| 141 | `Metalama.Framework.DesignTime/VisualStudio/AspectExplorer/AspectDatabaseService.cs:140-186` and the frozen `AspectExplorerDeclarationKind` (`Contracts/AspectExplorer/AspectExplorerAspectInstance.cs:60-66`, two members, `[Guid("96F4689F-…")]`) | The aspect explorer's wire vocabulary | A third declaration kind requires a new type with a new GUID | DT |
| 142 | `Metalama.Framework.DesignTime/VisualStudio/Classification/DesignTimeTextSpanClassificationHelper.cs:12-27` (`ArgumentOutOfRangeException` 26) | The one contract-crossing enum mapping that fails loudly | A new `TextSpanClassification` | DT |
| 143 | `Metalama.Framework.DesignTime/SourceGeneration/{BaseSourceGenerator.cs:28-228,ProjectSourceGenerator.cs:18,49}` and `ProjectKeyFactory.cs:30-96` (hashes only the `METALAMA_PROJECT_*` symbols, issue #1749) | The design-time source generator pipeline | A `LangVersion` change alone does not change the project key | DT |
| 144 | `Metalama.Framework.Engine/Advising/AdviceFactory.cs` (2254 lines): `Validate` 404-422, `ValidateTarget` 441-475, `ValidateNotExplicitInterfaceImplementation` 477-486, `ValidateIntroduceAttributeTarget` 488-525, **`ValidateNotExtensionBlock` 527-534**, **`ValidateNotExtensionBlockReceiver` 536-544**, `Override(IMethod)` 564-698 (event raise `NotImplementedException` 619), `IntroduceMethod` 700-728, `IntroduceFinalizer` 730-751, `IntroduceUnaryOperator` 753-804, `IntroduceBinaryOperator` 806-867, `IntroduceConversionOperator` 869-915, `IntroduceConstructor` 945-970, `OverrideAccessors` 1015-1098, `IntroduceField` 1116-1191, `IntroduceAutomaticProperty` 1193-1239, `IntroduceProperty` 1241-1316, **`IntroduceIndexer` 1318-1426 with the gate at 1406**, `OverrideAccessors(IEvent)` 1428-1476, `IntroduceEvent` 1478-1511 and 1513-1562, `IntroduceAttribute` 1871-1887, `IntroduceClass` 2050-2071 (`TypeKind.Class` 2068), `IntroduceInterface` 2073-2093 (2090), `IntroduceExtensionBlock` 2095-2126, `RequireAspect` 2172-2180 | The API surface where the shape of the language is encoded, and the eleven `ValidateNotExtensionBlock` call sites that are the whole "what may live inside an extension block" policy | A new introducible construct; extension-block indexers (delete line 1406) | ADV |
| 145 | `Advising/TemplateBindingHelper.cs:103-121` (including `UnaryAssignment` 119 and `BinaryAssignment` 120; `_ => throw`), `:443-501` (indexer special case 452-461), `:770-805` | Template binding | A new operator category; a new accessor kind | ADV |
| 146 | `Advising/TemplateMember.cs:61-70,160-165,184-236,243-313` (the same-project syntax fallback at 293-310) | Carrying a construct's behaviour on the template rather than inferring it at the use site | A new construct that changes what a member means | ADV |
| 147 | `Advising/TemplateExtensions.cs:16-59` (32), `:61-94`, `:96-119` | Accessor template selection | Semi-auto properties; async and iterator template variants | ADV |
| 148 | `Advising/DeclarationExtensions.cs:16-20` — a `(DeclarationKind, DeclarationKind)` tuple switch pairing Method, Indexer, Field, Property and Event | Signature equality | A new introducible member kind: silent fall-through | ADV |
| 149 | `AdviceImpl/AdviceSyntaxGenerator.cs:40-63` (two arms, **no default**; `// TODO: field-level attributes` 61; `MethodKind` list 49), `:100-209` (172), `:121-131`, `:140-147` | Attribute and initializer emission | A new declaration kind; a new statement form that can substitute for `return` | ADV |
| 150 | `AdviceImpl/Introduction/IntroduceMethodTransformation.cs:45,46-224` (`Finalizer` 49, `Operator` 64, `OperatorData` and `IsChecked` at 67, 86, 97, 116), `:227-266` | Method emission and the extension-implementation hook | A new method kind or modifier | ADV |
| 151 | `AdviceImpl/Introduction/IntroducePropertyTransformation.cs:60,217,225-277`; `AdviceImpl/Introduction/IntroduceIndexerTransformation.cs` (139 lines, **no `GetImplicitDeclarations` override**; `BaseTransformation.GetImplicitDeclarations()` at line 63 returns empty) | Property and indexer emission | Extension-block indexers: the static implementation methods would never reach the code model | ADV |
| 152 | `AdviceImpl/Introduction/IntroduceNamedTypeTransformation.cs:45-59,62-92` (`Class`, `Struct`, `Interface`, `_ => throw AssertionFailedException`), `:94-124` | The closed world of introducible type declarations | A new type declaration kind | ADV |
| 153 | `AdviceImpl/Introduction/IntroduceExtensionBlockTransformation.cs:56-66` | The template a `UnionDeclaration` introduction would copy | A new type declaration kind | ADV |
| 154 | `AdviceImpl/Introduction/ExtensionImplementationHelper.cs:38-131,89-98,163-,177` (`"set_"` or `"get_"` plus the property name, which cannot express an indexer) | The mapping from an extension member to its static implementation method | Extension-block indexers need an accessor overload with index parameters | ADV |
| 155 | `AdviceImpl/Introduction/IntroduceMemberAdvice.cs:88,91-134,136-141 and 237-243 (the silent `IsVirtual` downgrade),145-151,168-244,197 (the `TypeKind.Extension` carve-out),253-306` | Member introduction validation and modifier derivation | A new modifier; a new value-like type kind | ADV |
| 156 | `AdviceImpl/Contracts/ParameterContractAdvice.cs:25-77` (`IExtensionBlock` 27, `IIndexer` 39, `IMethod` 51, `IConstructor` 63, `default: throw` 75-76); `FieldOrPropertyOrIndexerContractAdvice.cs:29-58`; **`ContractExtensionBlockTransformation.cs:45-107`** with `Methods` 75, `Properties` 80 and **`Indexers` 93 (speculative since commit `30e21aea98`)**, `RefKind` mapping 59-69; `ContractBaseTransformation.cs:114-121`; `ContractIndexerTransformation.cs:47,122,193`; `ContractMethodTransformation.cs:47,68`; `ContractConstructorTransformation.cs:47` | Contracts, including the extension-block receiver fan-out | Extension-block indexers make the loop at 93 live; there is no loop for events, fields, constructors or nested types | ADV |
| 157 | `AdviceImpl/InterfaceImplementation/ImplementInterfaceAdvice.cs:105,154-155,184-211 (196 "Indexers are ignored, because there are no indexer templates."),362,373,517,779-780,782,949-950,608,869` | Interface implementation | A new interface member kind; interface indexers are a documented gap | ADV |
| 158 | `AdviceImpl/Override/OverrideHelper.cs:33-40,49-54,61-134,143-171 (the collision check omits `AllIndexers` and `AllTypes`),187-207,209-223`; `OverrideMethodBaseTransformation.cs:113-131`; `OverrideEventTransformation.cs:218-219,260`; `OverridePropertyBaseTransformation.cs:108-113`; `OverrideIndexerBaseTransformation.cs:109-114`; `OverridePropertyTransformation.cs:29-38` | Override emission and backing-field naming | A new member kind; a name collision with a nested type | ADV |
| 159 | The six `GetSyntaxModifierList( ModifierCategories… )` masks at `AdviceImpl/Override/{OverrideConstructorTransformation.cs:79,OverrideEventTransformation.cs:119,OverrideFinalizerTransformation.cs:79,OverrideIndexerBaseTransformation.cs:50,OverrideMethodBaseTransformation.cs:51,OverridePropertyBaseTransformation.cs:59}` | Explicit allow-lists deciding which modifiers propagate to a generated override | A new modifier is silently dropped from every override until each is revisited | ADV |
| 160 | `Metalama.Framework/src/Metalama.Framework/Advising/AdviceKind.cs:18-51` and `Metalama.Framework/Eligibility/EligibilityRuleFactory.cs:242-266` | The advice-kind vocabulary | A new advice kind | ADV |
| 161 | `Metalama.Framework/Eligibility/EligibilityRuleFactory.cs:47,89-92,117-125` (`_introduceRule` admits exactly `Class or Struct or Interface or Extension`), `:141,170-172`; `EligibilityRuleFactory.Contracts.cs:111` | The eligibility rules the advice factory consults | A new `TypeKind`; `RecordClass` and `RecordStruct` are a latent trap | ADV |
| 162 | `Metalama.Framework/Advising/IAdviceFactory.cs` (1108 lines, 54 members; `IntroduceIndexer` at 482, 515, 548, 581; `IntroduceClass` 1015; `IntroduceInterface` 1031; `IntroduceExtensionBlock` 1053 and 1068) and `Metalama.Framework/Aspects/AdviserExtensions.cs` (near 1702-1740) | The public advising surface; there is no `IntroduceStruct`, `IntroduceRecord`, `IntroduceEnum` or `IntroduceDelegate` | A new introducible type kind | ADV |
| 163 | `Advising/AdviceDiagnosticDescriptors.cs:61,77,85,216,224,280,288,302-319` (LAMA0540 at 305, LAMA0541 at 313) and `Metalama.Framework.Engine/Diagnostics/Ranges.md:14` | Language-shape diagnostics and their reserved ranges | A new refusal needs a range entry | ADV |
| 164 | `Metalama.Framework.Engine.Analyzers/KindCheckOptimizationAnalyzer.cs:26,626,689,723,745,772` (the hard-coded recognised-name list at 723) | The analyzer requiring `case SyntaxKind.X when node is XSyntax x:` | A new `SyntaxKindExtensions` property must be added to its list | ADV, SYNGEN |
| 165 | `Linking/LinkerInjectionStep.Rewriter.cs:316-358` (one `Visit*Declaration` per concrete type syntax; `ExtensionBlockDeclaration` 324), `:359-452` `VisitTypeDeclaration<T>` (primary constructor 366, record `Deconstruct` 377-387, `AddInjectionsOnPosition` 390-404, brace insertion 407, base-list append 418-441) | Step 1 injection, per type declaration | A new type declaration kind: none of this happens for its members | LINK |
| 166 | `Linking/LinkerInjectionStep.Rewriter.cs:578-670` — the injected-node post-processing switch (`ConstructorDeclaration` 580, `PropertyDeclaration` 595, `ExtensionBlockDeclaration` 621, the type-declaration arm 639, `NamespaceDeclaration` 657); **no `default`**, so an unknown node is added verbatim at 673 | Nested injections into an injected member | A new injected node kind | LINK |
| 167 | `Linking/LinkerInjectionStep.Rewriter.cs:1113-1133` `VisitMember` (eight arms, `_ =>` passthrough 1132) and the `*Core` methods at 1393, 1471, 1482, 1553, 1582, 1606, 1629, 1652, 1712, 1760, 1783, 1795, 1836, 1874, 1894 | Per-member injection | A new member declaration kind; `ConversionOperatorDeclaration` and `DestructorDeclaration` never receive inserted statements | LINK |
| 168 | `Linking/LinkerInjectionStep.Rewriter.cs:766,792,1212`, `:1491-1497,1495,1504,1517,1562,1664,1723` | Modifier, `params` and partial-member handling at injection time | A new modifier; a new partial-able member | LINK |
| 169 | `Linking/LinkerLinkingStep.LinkingRewriter.cs:37-79` (`ExtensionBlockDeclaration` 79), `:88`, `:111-129` (with `_ => []`) | Step 3 linking, per type declaration | A new type declaration kind: its members are never linked | LINK |
| 170 | `Linking/LinkerRewritingDriver.cs:466-496` `RewriteMember` (`_ => throw AssertionFailedException` at 478 and 495), `:449-452` | Step 3 member dispatch | A new member kind; fails loudly | LINK |
| 171 | `Linking/LinkerRewritingDriver.Types.cs:18,46,64` with the primary-constructor undo and `PrimaryConstructorBaseTypeSyntax` rewriting at 34-38 and 80-84 | Type-level rewriting | A new type declaration that can carry a primary constructor | LINK |
| 172 | `Linking/LinkerLateTransformationRegistry.cs:147-150` and `:189-191` — the primary-constructor type-kind predicate, twice, each followed by `.Single(...)`; `:77-125`, `:168` | Primary-constructor bookkeeping | A new type declaration with a parameter list: `InvalidOperationException` from LINQ, not a diagnostic | LINK |
| 173 | `Linking/SymbolExtensions.cs:23-64` `GetDeclarationFlags` — an explicit list of every declaration syntax kind that may carry a linker annotation, `default: throw AssertionFailedException` 62-63; **`SyntaxKind.ExtensionBlockDeclaration` is absent** | Linker annotations | A new declaration syntax kind; a required edit | LINK |
| 174 | `Linking/LinkerSyntaxHandler.cs:17-149` (two switches, 30-113 and 118-145, both throwing; the body-less accessor arm at 68) | Which node is the body of an overridden member | A new member form | LINK |
| 175 | `Linking/LinkerSyntaxHelper.cs:14-24` `IsUnsupportedMemberSyntax` — recognises only `UnknownAccessorDeclaration` inside a property or an indexer | The linker's only graceful-skip gate | Any other malformed member proceeds and throws | LINK |
| 176 | `Linking/LinkerAnalysisStep.SemanticBodyAnalyzer.cs:121-252` (`default: throw` 250), `:146-195` (the `default` records "not an unconditional end point" 191), `:254-391` `DiscoverExitFlowingStatements` (**no `default` arm at all**), `:367-390`, `:393-422`, `:425-441`, `:443-508` | Control-flow analysis, the most statement-shape-dependent file in the linker | A new statement wrapper; a labelled `break` changes what "exit-flowing" means | LINK |
| 177 | `Linking/Substitution/ReturnStatementSubstitution.cs:48-170` (emits an **unlabelled** `break;` at 86-92, 104-110 and 154-159) and `:213-223` | The T1 to T4 return-statement transformations | A labelled `break` | LINK |
| 178 | `Linking/LinkerLinkingStep.CountLabelUsesWalker.cs:15-32` — counts **only** `GotoStatementSyntax`; consumed by `LinkerLinkingStep.RemoveTrivialLabelRewriter.cs:51-131` | Label-use counting before label removal | A labelled `break` referencing a label: the label is deleted and the `break` no longer resolves | LINK |
| 179 | `Linking/LinkerLinkingStep.CleanupBodyRewriter.cs:110,138,165-197,235,255` and `LinkerLinkingStep.CleanupRewriter.cs:29-60,68` (gated on `CodeFormattingOptions.Formatted`) | Block flattening and marker removal, design-time only | A new statement-list carrier | LINK |
| 180 | `Linking/ConstructorEpilogueRewriter.cs:27-74` — returns unchanged for the three anonymous-function forms and `LocalFunctionStatementSyntax` (67-73) | Constructor epilogue rewriting | A new nested-function form | LINK |
| 181 | `Linking/AspectReferenceResolver.cs:828-864` `ResolveExpressionTarget` — thirteen assignment `SyntaxKind`s enumerated **twice** (property 832-842, field 843-852), events 853-861, fall-through to `PropertyGetAccessor` at 842 and 852; `:612-816`; `:401`; `:930-954` | Whether an aspect reference is a read or a write | A new assignment operator kind; an intervening wrapper expression | LINK |
| 182 | `Linking/LinkerAnalysisStep.AspectReferenceWalker.cs:45-49,63-160` (`ConditionalAccessExpression` 99-104; `_ => null` 108-119 with the comment at 117), `:139` | Aspect-reference discovery | A new expression form that changes `GetSymbolInfo` results | LINK |
| 183 | `Linking/Inlining/InlinerProvider.cs:17-58` — the static array of twenty inliners (two expression-body inliners commented out at 39 and 42); each `CanInline` walks a fixed ancestor chain, for example `Inlining/MethodReturnStatementInliner.cs:31-50` | The extension point for a new statement or expression form around an aspect reference | A new expression or statement shape | LINK |
| 184 | `Linking/Inlining/InlinerHelper.cs:99-108` `SkipParenthesizedExpressionAncestors` (`ParenthesizedExpressionSyntax` and `SuppressNullableWarningExpression` only), `:42-91`; downward twins at `Utilities/Roslyn/SyntaxExtensions.cs:92-97,103-111` | The transparent-wrapper list | A new transparent wrapper such as `unsafe(expr)` | LINK |
| 185 | `Linking/Inlining/MethodInliner.cs:15-20` and `Inlining/AsyncMethodInliner.cs:86-105` | Async and iterator gating of inlining | A method whose iterator-ness `IteratorHelper` misjudges | LINK |
| 186 | `Linking/LinkerAnalysisStep.SubstitutionGenerator.cs:861-911` `CreateOriginalBodySubstitution` (`ArrowExpressionClause` 865, accessors 872 and 891, `MethodDeclaration` 884, record `Parameter` 900, record declarations 903 with LAMA0651, `_ => throw` 906), `:918-930` | The registry mapping a body root node kind to a substitution class | A new body form | LINK |
| 187 | `Linking/Substitution/**` (34 classes), notably `PropertyBackingFieldReferenceSubstitution.cs:35-46`, `PropertyImplicitAccessorSubstitution.cs:83-110`, `EmptyPartialMethodSubstitution.cs:93-98`, `EmptyPartialAccessorSubstitution.cs:130-135`, `ExpressionBodySubstitution.cs:56,70`, `RecordParameterSubstitution.cs`, `ForcedInitializationSubstitution.cs:44` | One node kind, one substitution class | A new expression form that denotes a member | LINK |
| 188 | `Linking/LinkerAnalysisStep.SymbolReferenceFinder.cs:23-30,196-230` — `BodyWalker` indexes only `IdentifierNameSyntax` (209) and `InvocationExpressionSyntax` (220) | The index behind three fix-up analyses | A member reference expressed through a new syntax form is invisible | LINK |
| 189 | `Linking/LexicalScopeFactory.Visitor.cs:31-108` — thirteen name-introducing constructs; `LexicalScopeFactory.cs:190-197` | Unique-identifier generation | A new binding form: generated names may collide | LINK |
| 190 | `Metalama.Framework.Engine/Utilities/Roslyn/SyntaxExtensions.cs:113-120` `GetDeclaringType` — Class, Struct, Interface, Record, RecordStruct, Enum only (the `Enum` arm is already dead), then falls through to the parent | Which type declaration a node belongs to, for lexical scoping | `ExtensionBlockDeclaration` and a future `UnionDeclaration` walk to the wrong type | LINK, CM-ENG |
| 191 | `Linking/LinkerInjectionStep.LinkerInjectedMemberComparer.cs:21-30,73,194` (`GetKindOrder` returns `10` for anything unlisted) | Deterministic output member ordering | A new `DeclarationKind`: two new kinds compare equal | LINK |
| 192 | `Linking/LinkerInjectionStep.AuxiliaryMemberFactory.cs:100,156-168,170-205,411,524` | Auxiliary member generation, the async and iterator tuple switch, and `SupportsInitAccessors` | Async and iterator shapes; the reference set | LINK |
| 193 | `Transformations/ProceedHelper.cs:26-145` (`Buffer`/`BufferAsync` 37-64, `await` 67-96), `:146-179`, `:234-235,252-253` | `meta.Proceed()` emission, including the extension-member receiver forms | Async, iterator and extension-member shapes | LINK |
| 194 | `Linking/LinkerAnalysisStep.cs:198-201,218-222,248,553` (`LanguageVersion < AllLanguageVersions.CSharp14`), `:838-909` (LAMA0699), `:1065-1140` | The analysis step, including the only language-version test in the linker | The language version; hybrid auto properties | LINK |
| 195 | `Linking/LinkerInjectionHelperProvider.cs:51,214,219` (`options.Version is LanguageVersion.CSharp9 or CSharp10`, an equality test now permanently false), `:230-243`, and the cache at `:44` | The linker helper syntax tree, cached per `LanguageOptions` | The language version; raising `SupportedCSharpVersions.Latest` invalidates the cache key | LINK |
| 196 | `Linking/AspectLinkerDiagnosticDescriptors.cs` — only LAMA0650, LAMA0651 and LAMA0699 in the reserved range 650-699 | The linker reports almost nothing to the user | Any construct it does not understand crashes or is ignored | LINK |
| 197 | `Metalama.Framework/docs/{linker-overview,linker-architecture,linker-inlining,linker-callsite}.md` — none updated by any C# 14 commit | The linker design documents | Any language wave | LINK |
| 198 | `SyntaxGeneration/ContextualSyntaxGenerator.cs:41-48` (reflection into `CSharpSyntaxGenerator.Instance`), `:142,167`, `:205-211`, `:283-342` (no default; `AllowsRefStruct` 343), `:505-547` (no default), `:640,658`, `:704-741`, `:780-817` `AddAttribute` (twenty cases, `_ => throw AssertionFailedException` 815), `:819-882`, `:948-962`, `:964-1024`, `:1026-1072` `SafeCastExpression` (`_ => true` 1060; `CollectionExpression => false` 1052), `:1113-1167` | The dominant syntax-generation hotspot | A new type kind; a new declaration kind; a new expression form; a new generic constraint | SYNGEN |
| 199 | `SyntaxGeneration/SyntaxGeneratorForIType.cs:23-28,41,49-58,71-80,86`; `SyntaxGeneratorForIType.AbstractGeneratorVisitor.cs:111,118,125`; `SyntaxGeneratorForIType.TypeSyntaxGeneratorVisitor.cs:22,81,99,126-145,151,160-161,175,197-230`; `SyntaxGeneratorForIType.ExpressionSyntaxGeneratorVisitor.cs:37` | The `IType` to `TypeSyntax` visitor pair | A new `IType` kind; handled for free if `TypeVisitor` gains a virtual defaulting to `VisitNamedType` | SYNGEN |
| 200 | `SyntaxGeneration/SyntaxGeneratorForIType.NullableSyntaxAnnotationEx.cs:18-33` (`throwOnError: false`, `?.GetField`, `?.GetValue(null)`) with consumers at `SyntaxGeneratorForIType.cs:49-58,71-80` | The nullable-annotation bridge into a Roslyn internal | A Roslyn internal that moves: no diagnostic, no log, no assertion | SYNGEN |
| 201 | `SyntaxGeneration/ObjectDisplayOptions.cs:234-266` — a hand-copy of a Roslyn internal enum whose values are cast numerically at `SyntaxFactoryEx.LiteralFormatter.cs:39,61` | Literal formatting | Roslyn renumbering the enum: every literal in every generated file is formatted wrongly while still parsing | SYNGEN |
| 202 | `SyntaxGeneration/SyntaxFactoryEx.cs:27,41,49-83,85-93,99-135 and 160-182 `SafeIdentifier` (reserved keywords only),189-236,329-356` (`_ => null` 355) | Identifier escaping and literal emission | A new contextual keyword (`union`, `closed`) is emitted unescaped; a new literal type | SYNGEN |
| 203 | `SyntaxGeneration/SyntaxFactoryEx.LiteralFormatter.cs:30-58` — reflection into `Microsoft.CodeAnalysis.CSharp.ObjectDisplay`, picking `FormatLiteral` by first-parameter type via `.Single(…)` (33) and tolerating two shapes (41) | A literal-formatting bridge that already carries one signature-change shim | A Roslyn signature change; fails loudly | SYNGEN |
| 204 | `SyntaxGeneration/SyntaxFactoryDebugHelper.NormalizeRewriter.cs:128-171` — an explicitly incomplete allow-list of fifteen parent contexts (comment at 135); `SyntaxFactoryDebugHelper.cs:210-225` swallows every exception | The `ToSyntaxFactoryDebug` rendering used by the meta-syntax round trip | A new context in which a `QualifiedNameSyntax` must stay a name | SYNGEN, TEST |
| 205 | `SyntaxGeneration/ContextualSyntaxGenerator.{RemoveReferenceNullableAnnotationsRewriter.cs:19-270,DynamicToVarRewriter.cs:16-26,RemoveTypeArgumentsRewriter.cs:45-55,SubstitutionRewriter.cs:80-90,NormalizeSpaceRewriter.cs:107}` | Four `SafeSyntaxRewriter` derivations, each overriding a fixed set of type-syntax visits | A new type syntax form is walked by the base rewriter with stale state | SYNGEN |
| 206 | `SyntaxGeneration/SyntaxGenerationContext.cs:39,41` (`RequiresStructFieldInitialization`, a numeric language-version test), `:44` (`SupportsInitAccessors`, a **reference-set** probe); consumers at `AdviceImpl/Introduction/Constructors/IntroduceConstructorTransformation.cs:131`, `IntroduceEventTransformation.cs:61`, `IntroduceFieldTransformation.cs:52`, `IntroducePropertyTransformation.cs:61`, `AdviceImpl/Override/OverridePropertyBaseTransformation.cs:52`, `Linking/LinkerInjectionStep.AuxiliaryMemberFactory.cs:411,524`, `Linking/RewriterExtensions.cs:68` | The only language-version reads in syntax generation, and the two different techniques used | The language version; the reference set | SYNGEN |
| 207 | `SyntaxGeneration/SyntaxGenerationOptions.cs:33,43,53,55` — a formatting switch, **not** a language switch; and `Metalama.Framework.Engine/Services/CompilationContext.cs:146-174`, whose cache key does not include the language version | The syntax-generation option surface and its cache | A language-version predicate added here rather than to `SyntaxGenerationContext` would be cached across language versions | SYNGEN |
| 208 | `Utilities/Roslyn/CompilationExtensions.cs:114-124` `GetLanguageVersion` — reads the **first syntax tree only**, falling back to `LanguageVersion.Default.MapSpecifiedToEffectiveVersion()` | How the engine learns the compilation's language version | A compilation with mixed or absent parse options | SYNGEN, CT |
| 209 | `Formatting/TextSpanClassifier.cs:46-52,77-111,113-259` (**no `VisitInterfaceDeclaration`, `VisitExtensionBlockDeclaration`, `VisitIndexerDeclaration`, `VisitConstructorDeclaration` or `VisitOperatorDeclaration`**), `:278-298`, `:325-372` | The design-time compile-time and run-time colouring | A new type declaration kind: no colouring, silently | SYNGEN |
| 210 | `Formatting/CodeFormatter.cs:108-215` (the five passes, including `ImportAdder` 155, `Simplifier.ReduceAsync` 164-174 and 182, `Formatter.FormatAsync` 191 and 209) and `Formatting/CodeFormatter.SimplifierFixer.cs:99-145` | The formatting pipeline, whose behaviour is the host's Roslyn | The Roslyn version | SYNGEN |
| 211 | `Formatting/CodeFormatter.CustomSimplifier.cs:42-142` (parent gate 51, grandparent gate 60 and 79), `:144-210`, `:212-234` | Metalama-specific simplification | A new expression form that can host a target-typed delegate creation | SYNGEN |
| 212 | `Formatting/FormattedCodeWriter.cs:116-149` — consumes `Classifier.GetClassifiedSpansAsync` and matches classification-type **strings** (`"comment"` 129), passing unknown strings through at 147 | The classification bridge | A Roslyn classification type added for a new keyword | SYNGEN, EXT |
| 213 | `Formatting/ClassifierBase.cs:17-21,33-58` (no default), `:77-81`; `Formatting/FormattedCodeWriter.FormattingVisitor.cs:77`; `Formatting/XmlDocumentationReader.cs:57-69,80,166-172` | Trivia classification and documentation display | New trivia kinds; a new `TypeKind` | SYNGEN |
| 214 | `SyntaxSerialization/**` — `CompileTimePropertyInfoSerializer.cs:38-111` (throws 110), `CompileTimeFieldOrPropertyInfoSerializer.cs:25-29`, `CompileTimeParameterInfoSerializer.cs:28-36`, `CompileTimeReturnParameterInfoSerializer.cs:21-26`, `MetalamaMethodBaseSerializer.cs:81,161`, `TypeSerializationHelper.cs:32-42`, `SerializableTypes.cs:62,79,106-109,123-146`, `ReflectionSignatureBuilder.cs:45-62,64-236` (thirteen hard-coded `SpecialType` values at 161-176), `SyntaxSerializationService.cs:161-200,281,328-356` | Compile-time reflection-object serialisation | A new declaration kind; a new `SpecialType` | SYNGEN |
| 215 | `Serialization/LanguageVersionJsonConverter.cs:16-43` (`Read` is `(LanguageVersion) reader.GetInt32()` with no validation), `Serialization/ManifestSerializer.cs:145,158-172`, `Serialization/ManifestJsonContext.cs:64-84,112` | The manifest's language-version wire form | A manifest written by a Metalama that knows C# 15 and read by one that does not | SYNGEN, CT |
| 216 | `Metalama.Framework.Sdk/Utilities/Roslyn/{SafeSyntaxRewriter.cs:35,44-64,SafeSyntaxWalker.cs:35-73,111-139,SafeSyntaxVisitor.cs,SafeSyntaxVisitor{T}.cs}` | The rewriter and walker bases: exception wrapping and a recursion guard, **no unknown-node detection** | Every new node falls to Roslyn's generic recursion and is returned unchanged | LINK, TMPL, SYNGEN, DT |
| 217 | `Metalama.Framework.Sdk/Formatting/FormattingAnnotations.cs:49,62,67,79,82-101` and `Metalama.Framework.Sdk/Formatting/TextSpanClassification.cs` | The annotation surface, with the simplifier injected by the engine | Formatting and simplification behaviour | SYNGEN |
| 218 | `Metalama.Framework.Engine.Analyzers/{UnsafeIdentifierAnalyzer.cs:23,MetalamaPerformanceAnalyzer.cs:24,41,KindCheckOptimizationAnalyzer.cs:26}` (LAMA0850, LAMA0830, LAMA0832, LAMA0860) | The analyzers policing the engine's own conventions | A new construct needs both a `SyntaxKind` case and a type pattern; only the missing kind is caught | SYNGEN |
| 219 | `Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:118-121` — the implicit-`LangVersion` clamp, whose whitelist is `'12.0'`, `'13.0'`, `'14.0'`, `'default'`, `'latest'`, `'latestMajor'`, `'preview'`, and whose else-branch sets `<LangVersion>12.0</LangVersion>`; the warning at `:243-247` describes a floor action | **The single most dangerous line for .NET 11**: a `net11.0` project whose SDK implicitly sets `LangVersion` to `15.0` is silently compiled as C# 12 | BUILD |
| 220 | `Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.props:11-12,21-44` (`MinimumNETCoreAppVersion` 10.0 and **`MaximumNETCoreAppVersion` 11.0** at 30-31; `MinimumSdkVersion` 10.0 and **`MaximumSdkVersion` 11.0** at 32-33; `MinimumVisualStudioVersion` 18.0 at 37; the four sentences 38-41), `:66-78` | The platform requirement matrix the user sees; already .NET 11 aware | Adding a target framework or an SDK version | BUILD |
| 221 | `Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:286-302,309-311,315-370` (LAMA0600), `:378-380,392-421` (LAMA0601, LAMA0602; prerelease-suffix stripping at 397) | The platform checks | The target framework, the SDK version, MSBuild | BUILD |
| 222 | `Directory.Packages.props:7,12,16,23` (`RoslynApiMinVersion` 5.0.0), `:28` (`RoslynApiMaxVersion` 5.10.0-1.26365.3), `:30,33,50` (`MicrosoftBuildVersion` 18.0.2), `:53,55,59-60,63,66,70,84-85,114,121,132,168,179,217-219,229` | Every package pin, with its rationale comment | The Roslyn axis, the MSBuild axis, the out-of-band package ceilings | BUILD |
| 223 | `global.json:2,4,5,6,9` — generated from `eng/src/Program.cs:52` | The SDK pin | The SDK version; not to be hand-edited | BUILD |
| 224 | `nuget.base.config:8,14-18` | The `roslyn-consolidated` prerelease source and its package-source mapping | `RoslynApiMaxVersion` being a prerelease | BUILD |
| 225 | `Directory.Build.props:9,16`; `Metalama.Framework/Directory.Build.props:31,45-46` (`LangMaxVersion` 14.0); consumed by `Metalama.Extensions/Directory.Build.props:23`, `Metalama.Patterns/Directory.Build.props:26`, `Metalama.Migration/Directory.Build.props:18`; exported by `eng/src/Program.cs:142` | The repository-wide language settings | The language version of every project and every test payload | BUILD, PAT, TEST |
| 226 | `eng/RoslynVersions/{Latest.props:2,5,Roslyn.5.0.0.props:3,5,7,8-10,12,Roslyn.5.10.0.props:3,5,7,10,12,22-24}` | The two Roslyn variant declarations | Adding, renaming or dropping a variant | BUILD |
| 227 | The variant shim projects `Metalama.Framework/src/{Metalama.Framework.Engine.5.0.0,Metalama.Framework.DesignTime.5.0.0,Metalama.Framework.Implementation.Package.5.0.0,Metalama.Testing.AspectTesting.5.0.0,Metalama.Testing.UnitTesting.5.0.0}` plus six under `src/tests`, and the ten empty `*.4.12.0` directories left over from the dropped variant | The per-variant compilation | Adding or dropping a variant | BUILD, TEST |
| 228 | `Metalama.Framework/src/Metalama.Framework.Engine/Metalama.Framework.Engine.csproj:5,8,12,37-38,43-49,53-55`; `Metalama.Framework.DesignTime.csproj:3,6,9,16-17,23,33-36`; `Metalama.Framework.Implementation.Package.csproj:3,6,12,33-34,46-48,84,87,108,111` | How the Roslyn variant is threaded into assembly names, package identities, `InternalsVisibleTo` and `VersionOverride` | The variant set | BUILD |
| 229 | `Metalama.Framework/src/Metalama.Framework.CompilerExtensions.Resources/Metalama.Framework.CompilerExtensions.Resources.csproj:5-6` (`net10.0;net472`), `:25-26` (one `ProjectReference` per shipped Roslyn variant) | The Desktop and Core embedding pair, and the variant list | The Core flavour's target framework; the variant set | BUILD, DT |
| 230 | `Metalama.Framework/src/Metalama.Framework.CompilerExtensions/Metalama.Framework.CompilerExtensions.csproj:4,38-72` (ten unguarded `Include` globs whose paths contain `net472` or `net10.0`), `:65-70`, `:88-89` (a signing step hard-coded to `eng/src/bin/Debug/net9.0`) | The embedding globs | The Core flavour's target framework; a glob matching nothing produces no error | BUILD, DT |
| 231 | `eng/src/Program.cs:17,21,26-31,34-46,52,54,55-100,140-143,203-220,224,228-251` and `eng/src/BuildMetalama.csproj:5-6` (`net9.0`) | The build orchestrator and the contract with dependent repositories | The SDK set, the Visual Studio version, the Roslyn and language ceilings | BUILD |
| 232 | `eng/docker/vs17.Dockerfile:5,33-36` and `eng/docker/build.Dockerfile:44,48,52` — both auto-generated; the .NET 8 line is no longer requested by `Program.cs` | The continuous-integration containers | The SDK and Visual Studio set | BUILD |
| 233 | `Metalama.Framework/docs/platform-support.md:22-28,53-54,76-82,114-116,199-206,216-266,241-246,268-281,294-300,302-313,344-364` | The doctrine every platform decision is measured against | Any platform change | BUILD |
| 234 | `Metalama.Framework/docs/updating-roslyn.md:10-12,29-36,38-54` | The twelve-step procedure that the C# 15 wave follows | Any Roslyn change | BUILD |
| 235 | `Directory.Packages.md:61-79,161-172,189,193-221,381-391` | The package-version and preprocessor-symbol policy (177 conditional blocks and 69 test directives removed for one symbol) | Any variant change | BUILD |
| 236 | `Metalama.Framework/src/Metalama.Framework.Workspaces/Metalama.Framework.Workspaces.csproj:5-17,18,51-54,56-75,77,79,90-98,115-118` | The only project binding to the **maximum** Roslyn, and the only one that guards its asset selection with an `<Error>` | The Roslyn version; the MSBuild asset layout | EXT, BUILD |
| 237 | `Metalama.Framework/src/Metalama.Framework.Workspaces/MSBuildInitializer.cs:33-42,59-68,70,83-87` (**`i.ParsedVersion.Major <= Environment.Version.Major`**), `:91-94,97-112,123-154` | The most platform-sensitive file in the repository | The .NET SDK set; the runtime major version; the `Microsoft.Build.Locator` signature | EXT |
| 238 | `Metalama.Framework/src/Metalama.Framework.Workspaces/Workspace.cs:254-258,300-301,309-341` (**no `.slnx`** although `Microsoft.VisualStudio.SolutionPersistence` is referenced), `:354-369,376`; `WorkspaceProjectOptions.cs:41-54` | Project and solution loading | The .NET 11 SDK making `.slnx` the default solution format | EXT |
| 239 | `Metalama.Framework/src/Metalama.Framework.Workspaces/{ICompilationSet.cs:34,39,44,49,54,59,64,CompilationSet.cs:26,29,32,35,38,41,44,Project.cs:85}` — seven member categories; **no `Indexers`, `Operators`, `Finalizers`, `ExtensionBlocks` or `Parameters`** | The workspace, introspection and LINQPad view of a compilation | A new member or type container is invisible; extension blocks already are | EXT |
| 240 | `Metalama.Extensions/src/Metalama.Extensions.Multicast/MulticastTargets.cs:28-123` (`Default = 0` at 34; aggregates `AnyType` 65, `AnyMember` 102, `All` 122) | The public multicast target vocabulary | A new declaration kind must be named here or is unreachable | EXT |
| 241 | `Metalama.Extensions/src/Metalama.Extensions.Multicast/MulticastTargetsHelper.cs:13-68` — the `DeclarationKind` and `TypeKind` switch, falling through to `MulticastTargets.Default` (0) at 67 | Declaration to multicast target | A new kind returns zero, whose meaning depends on which predicate reads it | EXT |
| 242 | `Metalama.Extensions/src/Metalama.Extensions.Multicast/MulticastImplementation.cs:131-153` (no default), `:166-178` (`_ => false` 177), `:190-320`, `:329-541` (no traversal of `ExtensionBlocks`) | The multicast walk | A new type kind is silently never a target; extension-block members are never reached | EXT |
| 243 | `Metalama.Extensions/src/Metalama.Extensions.Multicast/MulticastAttributes.cs:40-199` — the `[Flags]` modifier-axis enumeration | **The extension point for a new modifier** | `closed` is not filterable until a flag pair and a predicate are added | EXT |
| 244 | `Metalama.Extensions/src/Metalama.Extensions.Multicast/MulticastAttributeInfo.cs:84,89,99-118,133,152-156,174-206` (the one fail-loud switch, throwing at 199), `:284-320,322-337,339-349`; `EnumExtensions.cs:12-16` (`HasFlagFast(x, 0)` is `true`) | Multicast filtering | A new declaration kind (`InvalidCastException` at 117); a new `RefKind` (silently `in`) | EXT |
| 245 | `Metalama.Extensions/src/Metalama.Extensions.DependencyInjection/{Implementation/DefaultDependencyInjectionStrategy.cs:47-63,127-128,DependencyProperties.cs:38-42,DependencyInjectionExtensions.cs:153-164,DependencyAttribute.cs:62-65,IntroduceDependencyAttribute.cs:44-45,Implementation/LazyDependencyInjectionStrategy.cs:107}` and `Metalama.Extensions.DependencyInjection.ServiceLocator/LazyServiceLocatorDependencyInjectionStrategy.cs:62,127` | Dependency-injection strategies | `DeclarationKind`, `TypeKind`, `ConstructorInitializerKind`, `Writeability`; a new synthesised constructor form | EXT |
| 246 | `Metalama.Extensions/src/Metalama.Extensions.Metrics/{StatementsCountMetricProvider.Visitor.cs:16,19-34,36,48,59,61,63,SyntaxNodesCountMetricProvider.Visitor.cs:41,44-54,LinesOfCodeMetricProvider.cs:153,154-187,189-239,197}` | The three metric providers, all `[CompileTime]` | A new statement node is counted automatically; partial members are under-counted (the `TODO` at 153) | EXT |
| 247 | `Metalama.Framework/src/Metalama.Extensions.HtmlWriter/HtmlCodeWriter.cs:34-35,37,206-217` (`_ => (null, null)` 215), `:286-305` (emits unvalidated `cs-<token>` classes), `:335-346` | The HTML writer's member path and token classes | A new declaration kind; a new Roslyn classification type | EXT |
| 248 | `Metalama.Framework/src/Metalama.Framework.Analyzers/{ImmutabilityContext.cs:250-450,DurabilityContext.cs:188-193,209-400,DurabilityContext.Expressions.cs:64-113,119-142,198,316,ImmutableContractAnalyzer.WriteSites.cs:61-70,211-274,259-260,ImmutableContractAnalyzer.TemplateExemption.cs:47-73,DurableContractAnalyzer.UseSites.cs:80-92,99-118,SymbolFacts.cs:82-83,154-193,221,WellKnownImmutableTypes.cs:253-259,WellKnownDurableTypes.cs:133-141,218-260}` | The immutability and durability analyzers | A new type shape, expression form, `OperationKind`, or Roslyn type name | EXT |
| 249 | `Metalama.Framework/src/Metalama.Framework.Analyzers/Metalama.Framework.Analyzers.csproj:4,16-26,33` — one `netstandard2.0` assembly referencing `RoslynApiMinVersion` deliberately | The analyzer's Roslyn binding | It receives graphs from a newer Roslyn than its reference assembly declares | EXT |
| 250 | `Metalama.Framework/src/Metalama.Framework.Introspection/**` (`IntrospectionTransformationKind.cs:15-66`, `IntrospectionChildKinds.cs:77-97`, `IntrospectionReferenceDetail.cs:136`) and `Metalama.Framework.Introspection.csproj:18,22-23` | The introspection model, which describes Metalama's vocabulary rather than the language's | A new `ReferenceKinds` member; the variant set | EXT |
| 251 | `Metalama.LinqPad/src/Metalama.LinqPad/{SchemaFactory.cs:127-183,185-199,241-275,FacadeType.cs:30-140,PropertyComparer.cs:19-32,Permalink.cs:30-38,MetalamaScratchpadDriver.cs:167-180,184-201,203-214,MetalamaWorkspaceDataContext.cs:38-50,Metalama.LinqPad.csproj:3,6,9,16,34-37}` | The LINQPad driver, entirely reflection-driven over `ICompilationSet` and the code model | Whatever the workspace API exposes; the host's Roslyn and architecture | EXT |
| 252 | The twelve target-framework literals in `Metalama.Framework/src/Metalama.Extensions.HtmlWriter/{MetalamaExtensionAssemblies.props:5-9,build/Metalama.Extensions.HtmlWriter.props:4-8,buildTransitive/Metalama.Extensions.HtmlWriter.props:4-8}` and `Metalama.Extensions.DiffEngine/{MetalamaExtensionAssemblies.props:5-11,build/…props:4-10,buildTransitive/…props:4-10}` | The extension-assembly manifests, each duplicated three times with no shared property | The Core flavour's target-framework name, matched by exact string equality | EXT |
| 253 | `Metalama.Backstage/src/Metalama.Backstage/Metalama.Backstage.csproj:5,23,27,38-49` and `Metalama.Backstage.{Commands,Worker,DotNetTool,Desktop.Windows}` with `RollForward=Major` | Backstage's target frameworks; the three executables roll forward onto .NET 11 unchanged | The .NET runtime version | BACK |
| 254 | `postsharp.engineering.sdk/2023.2.412/sdk/BuildOptions.props:4` `<LangVersion>latest</LangVersion>` (imported by `Metalama.Backstage/Directory.Build.props`) and `CodeQuality.targets:17-19` | Every Backstage project silently compiles at C# 15 under the .NET 11 SDK, with no gate | The .NET SDK version; the `AnalysisLevel` that follows the target framework | BACK |
| 255 | `Metalama.Backstage/src/Metalama.Backstage/Tools/DevBackstageToolsLocator.cs:235` — a hard-coded `"net10.0"` path segment | A target-framework name in a path (the one C# line issue #1876 had to change) | The Worker's target framework | BACK, BUILD |
| 256 | `Metalama.Backstage/src/Metalama.Backstage/Extensibility/RegisterServiceExtensions.cs:152-169,216-219,311-322,377-392,378-395` and `Utilities/ProcessUtilities.cs:274-289` | The four operating-system dispatch points; three fall back silently, one throws | A fifth platform, or a platform that stops reporting itself the same way | BACK |
| 257 | `Metalama.Backstage/src/Metalama.Backstage/Licensing/Licenses/{LicenseKeyData.Validation.cs:31-39,46-68,License.cs:99-108,LicensingAuthority.cs:24,27,29-33,40,47-53,61-67,72-78,LicensingAuthorityProvider.cs:102-146,ProductionLicensingAuthorityProvider.cs:190-198,ILicensingAuthorityObserver.cs:164-175,CryptographyHelper.cs:16,20,36-149}` | Licence signature verification, made lazy by issue #1861 | .NET 11 on macOS removing finite-field DSA (issue #1860): `DSA.Create` throws `PlatformNotSupportedException`, which is not a `CryptographicException` | BACK |
| 258 | `Metalama.Backstage/src/Metalama.Backstage/Infrastructure/StandardDirectories.cs:83,98-124` | The only explicit runtime-version comparison in Backstage; dead for `net10.0`, live for `netstandard2.0` | The runtime version of the host | BACK |
| 259 | `Metalama.Backstage/src/Metalama.Backstage/UserInterface/{SetupWebServerToken.cs:142-165,WindowsUserDeviceDetectionService.cs:9,42-51,71-99,102-123,125-145,151,153-166,WindowsUserInterfaceService.cs:11,68}` | The user-interface services | .NET 11 moving `System.IO.UnixFileMode`; Visual Studio 2026 being version 18 | BACK |
| 260 | `Metalama.Backstage/src/Metalama.Backstage/Threading/NamedLockService.cs:82-93,397,432,564` and `Threading/MutexAcl.cs:40,213-235` | Machine-wide named locks, with a documented degradation to a process-local dictionary | A change in the exception a named mutex raises | BACK |
| 261 | `Metalama.Backstage/src/Metalama.Backstage/Serialization/BackstageJsonContext.cs:24-61` and the recorded generator defects at `Diagnostics/DiagnosticsConfiguration.cs:24-31` (#1777) and `:39-53` (#1778) | Configuration serialisation through a source-generated JSON context | The SDK version, because the generator ships with the SDK | BACK |
| 262 | `Metalama.Backstage/src/Metalama.Backstage/Infrastructure/PlatformInfo.cs:39-52,68,167-200,245-274` | .NET installation discovery, with the Rider carve-out (#1627) and the ARM64 case (#1745) | The .NET SDK layout; the host | BACK |
| 263 | `Metalama.Backstage/src/Metalama.Backstage/Licensing/Licenses/LicenseFields/{LicenseFieldIndex.cs:12-42,LicenseFieldsExtensions.cs:23-33,47-57}` and `LicenseKeyDataSerializer.cs:12` | The licence-key field vocabulary: a forward-compatibility mechanism where the identifier itself says whether an unknown item may be skipped | The structural analogue of a grammar extension | BACK |
| 264 | `Metalama.Patterns/src/Metalama.Patterns.Observability/Implementation/DependencyAnalysis/DependencyGraphBuilder.Visitor.cs:22-50` (**`Cast<PropertyDeclarationSyntax>()` 34, `SingleOrDefault()` 36**), `:53,112-117,144-212` (a `SymbolKind` switch with **no default**), `:197,200,227,237-241,280-291,292-302,305,313,331-335,338,361-393,395,402,409-437` | The only C# syntax walker in Patterns | A partial property (already); a labelled `break` inside a getter; a new transparent wrapper | PAT |
| 265 | `Metalama.Patterns/src/Metalama.Patterns.Observability/Implementation/RoslynHelper.cs:24-76` — the transparent-wrapper list is `ParenthesizedExpressionSyntax` only (69); the fallback is `AccessKind.Read` (75) | Read and write classification | `unsafe(expr)` and any new wrapper | PAT |
| 266 | `Metalama.Patterns/src/Metalama.Patterns.Observability/Implementation/DependencyAnalysis/RoslynExtensions.cs:19-54` (`_ => throw NotSupportedException` 45, `default: throw` 53), `:84-107,117-135` | Effective accessibility and getter-body extraction | A new `TypeKind` reached through a property chain: a hard failure inside the aspect | PAT |
| 267 | `Metalama.Patterns/src/Metalama.Patterns.Observability/Implementation/{InpcInstrumentationKindLookup.cs:26-84,30,47-61,DependencyAnalysis/GraphBuildingContext.cs:37-45,66,74,82}` | `INotifyPropertyChanged` detection and deep-immutability classification | A new `IType` shape: silently "does not implement" | PAT |
| 268 | `Metalama.Patterns/src/Metalama.Patterns.Immutability/{ImmutabilityExtensions.cs:40-55,88-93,ImmutableAttribute.cs:38,57-93}` — checks only `Fields` and `Properties` | Immutability classification, trusted by the Observability analyser | A new type kind; a mutable indexer or new member kind on an `[Immutable]` type | PAT |
| 269 | `Metalama.Patterns/src/Metalama.Patterns.Contracts/{ContractContext.cs:62-73,78-86,ContractExtensions.cs:70,93-108,100,165-172,CompileTimeHelpers.cs:18-23,33,73,CheckInvariantsAspect.cs:27-33}` | Contract targets and the fabric's declaration walk | A new member kind; a new `TypeKind` | PAT |
| 270 | `Metalama.Patterns/src/Metalama.Patterns.Contracts/Numeric/{NumericRange.cs:11,306-465,382-419 (`#if NET8_0_OR_GREATER`),481,513-528,NumericBound.cs:120-133,140-300,310,312-440}`; `RangeAttribute.cs:170-183`; `EnumDataTypeAttribute.cs:63-74,85`; `NotEmptyAttribute.cs:53-60,92,171-226`; `RequiredAttribute.cs:75`; `InvariantAttribute.cs:32` | The `SpecialType` enumerations and the only pattern-syntax generation in Patterns | Which asset of `Metalama.Patterns.Contracts` the pipeline loads decides whether a generic-math range check is generated at all | PAT |
| 271 | `Metalama.Patterns/src/Metalama.Patterns.Wpf/{Metalama.Patterns.Wpf.csproj:4,5,20,29,CommandAttribute.cs:128,CommandAttribute.DiagnosticReporter.cs:19-21,Implementation/DependencyPropertyAspectBuilder.cs:43-45,76-107,112,199,237-239,Implementation/CommandNamingConvention/CommandNamingConventionMatcher.cs:27-52,127-134,Implementation/DependencyPropertyNamingConvention/DependencyPropertyNamingConventionMatcher.cs:30-50,79-203}` | The WPF package, the one with a user-visible target-framework consequence | A `net11.0-windows` application finds no compatible asset if the floor moves | PAT |
| 272 | `Metalama.Patterns/src/Metalama.Patterns.{Observability,Immutability}.csproj:27,31` — a private `Metalama.Framework.Sdk` reference and a `NuGetAuditSuppress` comment naming Roslyn 4.12 and the `net8.0` shared framework | The only Roslyn coupling in Patterns; both compile against a single Roslyn with no variant mechanism | Any C# 15 handling written against a Roslyn 5.10 API would not compile here | PAT |
| 273 | The `NET6_0_OR_GREATER`, `NETCOREAPP3_0_OR_GREATER` and `NETFRAMEWORK` guards across Flashtrace, Caching and Wpf | Runtime-conditional compilation whose `#else` arms still serve `netstandard2.0` and `net472` | These do **not** become removable when `net8.0` becomes `net10.0` | PAT |
| 274 | `Metalama.Framework/src/Metalama.Testing.AspectTesting/TestOptions.cs:40,198,204,212,220,256,262,268,289,407-501,508-528,540,555,609,614,619,624,681-720` (the **silent skip** for an unrecognised `@LanguageVersion` at 687-694 and 708-715), `:723` (`@LanguageFeature`, unvalidated), `:835,841,857-872` | The test-directive vocabulary | A new language version; a new preprocessor constant; a new target framework | TEST |
| 275 | `Metalama.Framework/src/Metalama.Testing.AspectTesting/TestInput.cs:76-83,87-95,97-117` — `@RequiredConstant`, `@ForbiddenConstant` and `@TargetFrameworks` all **skip** rather than fail on an unmatched value | Test-selection directives | A misspelled constant or target framework silently disables a test forever | TEST |
| 276 | `Metalama.Framework/src/Metalama.Testing.AspectTesting/BaseTestRunner.cs:186-191,211-213,218,220-230,255,259,385-399,417-423,575,578,592,698,701,713,843-861` (the "all expected files were written" loop, which does **not** cover `.t.txt`) | The aspect-test runner | The default language version; the target framework | TEST |
| 277 | `Metalama.Framework/src/Metalama.Testing.AspectTesting/TestSyntaxTree.cs:187-231` — the 22-kind `MemberDeclarationSyntax` switch, `default: throw ArgumentOutOfRangeException` 225-226; **`ExtensionBlockDeclaration` absent** | The document-root kind check | A new type declaration kind used as a test document root | TEST |
| 278 | `Metalama.Framework/src/Metalama.Testing.AspectTesting/TestResult.cs:490-499,518-585` — the same 22-kind list, `default: throw InvalidOperationException` 583-584 | The `// <target>` consolidation | A new declaration kind marked `// <target>`: found, then rejected | TEST |
| 279 | `Metalama.Framework/src/Metalama.Testing.AspectTesting/TestOutputNormalizer.cs:21-22,33` — `CSharpSyntaxTree.ParseText( s )` with no parse options, diagnostics never inspected | The golden-file comparison path for `.t.cs`, `.i.cs` and `.ct.cs` | Any construct the default parse options reject: both sides are mangled identically and the comparison passes | TEST |
| 280 | `Metalama.Framework/src/Metalama.Testing.AspectTesting/SyntaxTreeStructureVerifier.cs:26-50` (`VerifyMetaSyntax`, reparsing with `SupportedCSharpVersions.DefaultParseOptions` rather than the tree's own), `:52-85` (`Verify`, which does use the tree's options); callers `AspectTestRunner.cs:211`, `Metalama.Framework.Tests.LinkerTests/Runner/LinkerTestRunner.cs:99`, `Metalama.AspectWorkbench/ViewModels/MainViewModel.cs:259` | The single most valuable structural check for a new construct | A construct the syntax generator emits incorrectly | TEST |
| 281 | `Metalama.Framework/src/Metalama.Testing.AspectTesting/AspectTestRunner.cs:22,42,239-241,294-455,385-453,479-519,525-540,546-566` | Transformed-program execution and its snapshot | The `net48` leg never executes; a renamed `Program` or main method silently disables execution | TEST |
| 282 | `Metalama.Framework/src/Metalama.Testing.AspectTesting/Metalama.Testing.AspectTesting.targets:46,52-55` (`ThisRoslynVersionNoPreview` fallback `5.0.0`, described as "the latest version of Roslyn"), `:58-129`; `TestAssemblyMetadataReader.cs:28-128`; `XunitFramework/TestDiscoverer.cs:56-80`; `TestRunnerFactory.cs:84-115` | The MSBuild to test bridge and the per-variant assembly renaming | The variant set; every external consumer of the package binds the `5.0.0`-suffixed name by default | TEST |
| 283 | `Metalama.Framework/src/tests/Metalama.Framework.Tests.LinkerTests/Runner/LinkerTestInputBuilder.TestTypeRewriter.cs:49,65,81,119-152,341-350,501-502,555-568,641-696` and `LinkerTestInputBuilder.TestRewriter.cs:112,126,140,154,183` | The linker test input builder | A new type declaration kind: members are relocated to the wrong type, or `Stack<T>.Peek` throws | TEST |
| 284 | `Metalama.Framework/src/tests/Metalama.Framework.Tests.TemplateTests/Runner/{TestTemplateCompiler.cs:85,TemplatingTestRunner.cs:184,194,328-340,393-402}` | The template test runner | A new `RefKind`; a template that is not a method | TEST |
| 285 | `Metalama.Framework/src/tests/Utilities/SyntaxCover/{Program.cs:21,51-55,SyntaxCover.csproj:5}` — the only tool that enumerates the whole C# grammar, reading files nothing writes any more | Grammar coverage of the aspect-test corpus | There is **no** automated signal about which syntax kinds the corpus exercises | TEST |
| 286 | `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/CSharp14/**` (61 files in six directories; twenty carry `@RequiredConstant(NET8_0_OR_GREATER)`, three carry `@TestScenario(DesignTime)`, **none carries `@LanguageVersion`**) and the `CSharp11`, `CSharp12`, `CSharp13` suites | The per-wave test convention | The default language version, not a directive | TEST |
| 287 | `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/{Misc/LanguageVersion.t.cs,LanguageVersion/LanguageVersionPreview.t.cs}` | Two checked-in expected files that render `SupportedCSharpVersions.All` verbatim | `SupportedCSharpVersions.All` | TEST |
| 288 | `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/DesignTimeInvalidCode/{UnknownAccessorInTemplate.cs:7,UnknownAccessorInTemplate_Roslyn5_0.cs:7}` | The only two consumers of `ROSLYN_5_10_0_OR_GREATER`; the canonical per-variant expected-output pattern | A construct whose diagnostic differs between variants | TEST |
| 289 | `Metalama.Framework/src/tests/Standalone/{TemplateLanguageVersion14/,DefaultLanguageVersion/,Issue1757/OldAspects/,CSharp10/,Issue1585b/,Issue31024/,Issue1789/}` and `DesignTimeStandalone/Issue1749.FrameworkVersions/OldAspects/` | The standalone scenarios that pin a language version deliberately | The repository defaults; a raised default must not change what they measure | TEST |
| 290 | `Metalama.Framework/src/tests/Standalone/SupportedPlatform.*` — `TestedTargetFrameworks:8-10,13`, `MultiTargeting:13`, `UntestedTargetFramework:8`, `Exclusion:9`, `NoWarn:12`, `CheckDisabled:9`, `MetalamaDisabled:9`, `ContributedRequirements:15` | The hand-maintained platform matrix assertions | `MetalamaPlatformRequirement`; nothing verifies the two stay in step | TEST, BUILD |
| 291 | `Metalama.Framework/src/tests/Metalama.DesignTime.HostSimulator/{MSBuildEnvironment.cs:27-99,DesignTimeHost.cs:34-47,IsolatedAnalyzerAssemblyLoader.cs:15-111,ProjectDesignTimeSession.cs:130-136}` and `eng/src/DesignTimeSolution.cs:42,83,104-107` | The only design-time host simulation; the only place where the language version comes from a real project evaluation | The .NET SDK; the analyzer load contexts | TEST, DT |
| 292 | `Metalama.Framework/src/tests/docker/**` (`ARG DOTNET_VERSION=10.0.302`, per-scenario `global.json`, `ubuntu:24.04`, `servercore:ltsc2025`, `SDK_X86=8.0.423`) and `eng/src/Program.cs:203-220` | The container test scenarios | The SDK versions and base images | TEST, BUILD |
| 293 | `Metalama.Framework/docs/testing.md:12,41,62,147,179-185,244,252` | The testing doctrine, which documents two test projects that no longer have a `.csproj` and a stale `@TargetFrameworks` example | Drift against the projects it describes | TEST |
| 294 | `Metalama.Premium/src/Metalama.Extensions.CodeFixes.Engine/Implementations/ChangeVisibilityCodeAction.cs:21,33-50` (`UpdateTree` called unconditionally at 48), `:52,65-118` (eighteen `Visit*` overrides; **`InterfaceDeclarationSyntax`, `IndexerDeclarationSyntax` and the extension block absent**), `:124-189` (the `Accessibility` switch 142, `default: throw` 177, non-accessibility modifiers copied at 181), `:191-199` | The one genuine syntax rewriter in Premium | A new declaration form: the code fix reports success and changes nothing | PREM |
| 295 | `Metalama.Premium/src/Metalama.Extensions.Validation/ReferenceValidationContext.cs:57,124-134` — omits `None`, `ManagedResource`, `Type` and **`ExtensionBlock`**, then `_ => throw ArgumentOutOfRangeException` | The clearest evidence of how far a `DeclarationKind` addition propagates across repositories | A new `DeclarationKind`; validating a reference into an extension block throws today | PREM, CM-PUB, CM-ENG |
| 296 | `Metalama.Premium/src/Metalama.Extensions.Validation/{ReferenceEnd.cs:119,125,150-178,ReferenceGranularity.cs:15-53,ReferenceGranularityExtension.cs:69}` | Reference-granularity resolution; the `Type` and `TopLevelType` arms cast to `INamedType` | A type-like declaration that is not an `INamedType` (an extension block): `InvalidCastException` | PREM |
| 297 | `Metalama.Premium/src/Metalama.Extensions.Validation.Engine/{Queries/ReferenceValidatorQuerySource.cs:56-73,Queries/DynamicReferenceValidatorQuerySource.cs:53-67}` — two `MethodKind` switch statements with no default, neither handling indexer accessors nor `EventRaise` | Accessor-to-member validator translation | A new accessor form: the validator is registered and never runs | PREM |
| 298 | `Metalama.Premium/src/Metalama.Extensions.Architecture/{ArchitectureExtensions.cs:38-263,130-175,152-174,Aspects/InternalsUsageValidationAttribute.cs:34-48,144-152,Aspects/ExperimentalAttribute.cs:62,Aspects/InternalOnlyImplementAttribute.cs:110,Predicates/HasFamilyAccessPredicate.cs:26}` | The architecture rules and the internal-surface enumeration, in duplicate; `t.Indexers` is missing from both | A new member container; an internal accessor of a public indexer is already unprotected | PREM |
| 299 | `Metalama.Premium/src/Metalama.Extensions.Validation.Engine/TransitiveValidatorInstance.cs:66,77-78,103-121` — the serializer writes five fields and **omits `Granularity` and `IncludeDerivedTypes`** | The cross-project validator wire form | A validator crossing a project boundary silently returns to `SyntaxNode` granularity and loses `IncludeDerivedTypes` | PREM |
| 300 | `Metalama.Premium/src/Metalama.Extensions.Validation.Engine/ReferenceValidatorRunner.cs:43-48,68-79,135-143` (`_ => GetDeclaration`), `:158-191` | The runner and its grouping-key switch | A `ReferenceGranularity` the switch does not name degrades silently | PREM |
| 301 | The four Premium extension-assembly manifests: `Metalama.Premium/src/Metalama.Extensions.CodeFixes.Package/build/Metalama.Extensions.CodeFixes.props:5-20`, `Metalama.Extensions.Validation.Package/build/Metalama.Extensions.Validation.props:5-14`, `Metalama.Extensions.CodeFixes/MetalamaExtensionAssemblies.props:8-20`, `Metalama.Extensions.Validation/MetalamaExtensionAssemblies.props:8-15` — all naming `net8.0` and the variant versions `4.12.0` and `5.0.0` | Premium's half of the exact-string extension-loader contract | **No premium extension assembly satisfies the current process on .NET today**, with no diagnostic | PREM |
| 302 | `Metalama.Premium/src/{Metalama.Extensions.CodeFixes.Package/Metalama.Extensions.CodeFixes.Package.csproj:47-64,Metalama.Extensions.Validation.Package/Metalama.Extensions.Validation.Package.csproj:40-53}` — sixteen `TfmSpecificPackageFile` `Include` globs over build outputs, with no existence check | The packaging copy lists | A half-renamed variant produces a package missing an assembly, with no error | PREM |
| 303 | `Metalama.Premium/src/Metalama.Licensing/build/Metalama.Licensing.targets:11` (a runtime version computed and discarded), `:12-14,18` | MSBuild-task runtime selection, with no version guard and no `LAMA`-numbered diagnostic | Below the SDK floor, a raw assembly-load error | PREM |
| 304 | `Metalama.Premium/eng/RoslynVersions/{Latest.props:2,Roslyn.4.12.0.props,Roslyn.5.0.0.props:3}` (which reads `$(RoslynApiMaxVersion)`, **undefined in that repository**) and the three `*.4.12.0` shim projects | Premium's variant machinery, a whole wave behind | Renaming the latest variant inverts the meaning of `5.0.0` | PREM |
| 305 | `Metalama.Premium/Directory.Build.props:16,19-20` `<MetalamaTemplateLanguageVersion>13.0</MetalamaTemplateLanguageVersion>` with the comment "must be compatible with VS 2022", which PB-2027.0 falsifies | Premium's deliberate template-language ceiling | The supported Visual Studio set | PREM |
| 306 | `Metalama.Premium/src/tests/Metalama.Extensions.Validation.AspectTests/{AllReferences.cs,AllReferences.t.cs,AllReferences_Derived.cs,AllReferences_NotDerived.cs}` and `src/tests/Metalama.Extensions.Validation.UnitTests/SideBySideVersionTests.cs` | The test evidence a language wave produces in Premium; absent from it are extension blocks, extension indexers, `union`, `unsafe(expr)`, `with(...)`, labelled `break`/`continue` and `closed` | Every new construct that can be referenced | PREM |
| 307 | `Metalama.Premium/src/Metalama.Extensions.CodeFixes.DesignTime/CodeFixesDesignTimeExtension.cs:328-357` and `Metalama.Framework.DesignTime/Services/DesignTimeServiceProviderFactory.cs:26-44` (`DesignTimeProcessKind` has exactly three members) | Host-process dispatch for code fixes | A new host that splits its processes | PREM, DT |
| 308 | `Metalama.Premium/src/Metalama.Patterns.Caching.Backends.{Azure,Redis}/…csproj:4` (`net471;net8.0;netstandard2.0`) and `Metalama.Licensing.BuildTasks/…csproj:4` (`net8.0;net472`) | Target frameworks below the PB-2027.0 floor | The .NET Framework floor of 4.7.2; the dropped `net8.0` | PREM, BACK |

Hotspot count: **308**.

---

## 2. Subsystem detail

### 2.1 CM-PUB — the public code model

Scope: `Metalama.Framework/src/Metalama.Framework/Code/`, including `Code/Collections/`,
`Code/DeclarationBuilders/`, `Code/Types/`, `Code/Comparers/`, `Code/Invokers/` and `Code/SyntaxBuilders/`.
Project file `Metalama.Framework/src/Metalama.Framework/Metalama.Framework.csproj`
(`<TargetFrameworks>netstandard2.0;net10.0</TargetFrameworks>` at line 4).

This subsystem is the declarative surface of the code model: almost entirely interface declarations and
enumerations, plus a small number of extension-method files that contain the only real logic.

- **Seventeen enumerations**, of which five describe the shape of the C# language directly
  (`DeclarationKind`, `TypeKind`, `MethodKind`, `OperatorKind`, `RefKind`) and several more do so
  indirectly (`TypeKindConstraint`, `VarianceKind`, `Writeability`, `ConstructorInitializerKind`,
  `FieldKind`, `Accessibility`, `SpecialType`).
- **Exactly nine places switch over those enumerations.** Six throw on an unknown value; three fall
  through to a default answer.
- **Zero direct dependency on Roslyn, on the .NET runtime version, on the .NET SDK, or on the host
  integrated development environment.** A grep over `Code/**` for `LanguageVersion`,
  `SupportedCSharpVersions`, `RuntimeInformation`, `#if NET` and `TargetFramework` returns nothing; the
  only Roslyn mentions are four prose comments (`ISourceExpression.cs:13`, `SourceReference.cs:13,36`,
  `SourceSpan.cs:54`) and two `object`-typed escape hatches.
- **`[InternalImplement]` on `ICompilationElement`** (`ICompilationElement.cs`), inherited by every
  `IDeclaration` and `IType`, is why adding members to a code-model interface is not binary-breaking for
  users, and why the C# 14 wave added interface members freely.

The residual platform couplings are exactly five: the `TargetFrameworks` element and the
`InternalsVisibleTo` list in the project file; `SourceReference.NodeOrTokenInternal` and
`SourceReference.Kind`; `ISourceExpression.AsSyntaxNode`; and `SyntaxBuilders/SyntaxBuilder.cs:39-41`,
whose `CurrentImplementation` resolves `ISyntaxBuilderImpl` from `MetalamaExecutionContext` so that every
expression and statement the user builds is produced by parsing a string **in the engine**.

Observed ordering discipline, from the C# 14 wave: new members are appended at the end of an enumeration;
obsolete members keep their ordinal and are marked `[Obsolete(..., true)]` rather than deleted.
`DeclarationKind.Finalizer` (89) and `DeclarationKind.Operator` (95), `TypeKind.RecordClass` (30) and
`TypeKind.RecordStruct` (68), and the obsolete alias `OperatorKind.Multiply = Multiplication` (124) all
remain as placeholders.

**The nine switch sites**, in full:

1. `DeclarationExtensions.cs:53-93` `CanContain` — the only exhaustive `DeclarationKind` switch; arms at
   57-60, 62-63, 65-66, 68-69, 71-73, 75-78, 80-81, 83-84, 86-90, then
   `throw new ArgumentOutOfRangeException( nameof(containingDeclarationKind), … )` at 91-92. The
   `ExtensionBlock` arm already lists `DeclarationKind.Indexer`, so C# 15 indexers in extension blocks
   need no change here; `Event` is deliberately absent.
2. `DeclarationExtensions.cs:105-143` — the five predicates, all written as `is … or …` with no default,
   all silently `false` for an unknown kind.
3. `DeclarationExtensions.cs:220-228` `GetMembers` — five arms, `_ => throw`, `Indexer` already missing.
4. `DeclarationExtensions.cs:406-437` `GetEffectiveAccessibility(IType)` — `IArrayType` 409-411,
   `IPointerType` 413-414, `INamedType` 416-431, then `default: return Accessibility.Public` with the
   comment "For dynamic, type parameters, function pointers, etc."
5. `GenericExtensions.cs:42-50` `GetBase` — `_ => null`.
6. `GenericExtensions.cs:56-62` `GetDefinition` — `_ => declaration`; `ExtensionBlock` is not listed.
7. `GenericExtensions.cs:299-334` — arms at 301, 306, 311, 316, 321, 326, 331, `default: throw` 333-334;
   `Indexer` missing.
8. `OperatorKindExtensions.cs:22-118` `GetCategory` — the single largest switch, 60 arms, `_ => throw` 117.
9. `AccessibilityExtensions.cs:27-41` — six arms, `_ => throw` 40. `RefKindExtensions.cs:32-43`
   `IsWritable` is a tenth of the same shape, with the two silent negations at 23 and 48 beside it.

Hand-written closed-world enumerations that are equivalent to switches: `NamedTypeExtensions.cs:40-65`,
`:72-102`, `:110-140`; `DeclarationExtensions.cs:334-341`; `ReferenceKindsExtension.cs:46-69`;
`TypedConstant.cs:478-492` and `:223-243`.

Non-`switch` `TypeKind` dependencies: `MemberExtensions.cs:56`, `SignatureMatcher.cs:293` and `:298`,
`TypedConstant.cs:106`, `:273` and `:471`.

**How the C# 14 wave was absorbed here**, from the commit record:

| Commit | Issue | Files in `Code/**` |
| --- | --- | --- |
| `cdf076ee1a` | #1034 | `OperatorKind.cs` (+182), `OperatorKindExtensions.cs` (+97), `OperatorCategory.cs` (+4) |
| `bcdeb3a185` | #1034 | new `ITypeExtension.cs`, new `Collections/ITypeExtensionCollection.cs`, `INamedType.cs` (+2), `IParameter.cs` (nullability of `DeclaringMember`), `TypeKind.cs` (+`Extension`) |
| `16cc84ca1d` | #1034 | the rename to `IExtensionBlock` / `IExtensionBlockCollection`, plus `ExtendedType` → `ReceiverType`, `ExtensionParameter` → `ReceiverParameter`, `Extensions` → `ExtensionBlocks` |
| `22697b6ba5` | #1036 | `IMethod.cs` (+8: `ExtensionImplementationMethod`, now line 82) |
| `5b121f3c21` | #1116 | `OperatorKind.cs`, `OperatorKindExtensions.cs` |
| `787ec4fcd8` | #1110-#1113 | `Collections/IEventCollection.cs` (+7: the `this[string name]` indexer, now line 18) |
| `a9698fa1e8` / `f776fd9af9` | #1159 | new `DeclarationBuilders/IExtensionBlockBuilder.cs` (48 lines) |
| `7df11b077c` | #1034 follow-up | `DeclarationKind.cs`, `DeclarationExtensions.cs` — **and 35 files elsewhere** |
| `88667a5265` | #1138 | new `ITupleType.cs`, `IField.cs` (+9: `FieldKind`), `SpecialType.cs` (+5), `TypeFactory.cs` (+11), `IDeclarationFactory.cs` (+10), `TypeKind.cs` (+`Tuple`) |
| `18f7ed78d0`, `b69925e37f` | — | deprecations in `DeclarationKind.cs` and `TypeKind.cs` |

The nine-step pattern the wave established: (1) model the construct as an interface derived from the
closest existing one, never a new root (`IExtensionBlock : INamedType` at `IExtensionBlock.cs:11`,
`ITupleType : INamedType` at `ITupleType.cs:27`, `ITupleElement : IField` at `ITupleElement.cs:68`);
(2) append one enumeration member at the end; (3) add a paired collection with domain-specific query
methods (`IExtensionBlockCollection.cs:18,23`); (4) add one property on `INamedType`
(`ExtensionBlocks` 187); (5) widen an existing member's contract rather than adding a new one where
possible (`IParameter.DeclaringMember` became nullable at `IParameter.cs:48`, a deliberate source-breaking
change under nullable reference types); (6) add a builder that inherits and restricts, with
`[InternalImplement]` and the restrictions documented in `<remarks>` rather than expressed in the type
system (`IExtensionBlockBuilder.cs:62-78`); (7) enumerate new operators exhaustively and re-sort the
mapping switch by category; (8) rename before shipping; (9) fix the consumers in the same commit.

Measured cost curve: a new modifier is 2 to 3 lines in 3 files with no switch; a new interface derived
from an existing one is about 30 lines in 2 to 4 files with no switch; a new `TypeKind` member is one line
here plus the engine's `TypeKind` consumers; **a new `DeclarationKind` member is one line here, six switch
edits in this subsystem, and about 35 files in the engine**; a new `OperatorKind` member is two lines plus
the engine's operator-syntax tables.

For the specific C# 15 features: `union` most likely follows the `IExtensionBlock` / `ITupleType`
precedent (a new interface derived from `INamedType`, a new `TypeKind` member, and a decision about
whether it also needs a `DeclarationKind` member — the precedent is split, since `ITupleType` reuses
`DeclarationKind.NamedType` while `IExtensionBlock` got its own). `closed` is the three-line modifier
shape: `INamedType.cs` beside line 202, `INamedTypeBuilder.cs` beside line 18, nothing else.
`unsafe(expr)`, `with(...)` elements and labelled `break`/`continue` require **no change in this
subsystem**: `IExpression` is described only by `Type`, `RefKind` and `Value`, statements are opaque
`IStatement`, and collection expressions are not modelled at all.

### 2.2 CM-ENG — the code model implementation

Scope: `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/**` (324 `.cs` files), plus
`Metalama.Framework.Engine/SerializableIds/**` and the shared kind taxonomies in
`Metalama.Framework.Engine/Utilities/Roslyn/`.

The one-sentence summary: the code model is a set of hand-written total functions over three closed
enumerations — Roslyn `SymbolKind`, Roslyn `TypeKind` and Metalama `DeclarationKind`/`TypeKind` — and a
new C# construct is absorbed by widening those functions in a fixed order, which is exactly what issues
#1034 and #1159 did for extension blocks.

Four parallel families implement the same public interfaces:

| Family | Directory | Backing |
| --- | --- | --- |
| `Source*` | `CodeModel/Source/` | a Roslyn `ISymbol` |
| `Introduced*` | `CodeModel/Introductions/Introduced/` | a `*BuilderData` produced by an aspect |
| `*Builder` | `CodeModel/Introductions/Builders/` | the mutable form an aspect writes to |
| `Constructed*` | `CodeModel/Introductions/ConstructedTypes/`, `CodeModel/Source/ConstructedTypes/` | array, pointer and tuple over the two above |

Named types use a **facade plus implementation pair**: `Source/SourceNamedType.cs` (the facade, tracking
usage through `OnUsingDeclaration`) delegates to `Source/SourceNamedTypeImpl.cs`. `ExtensionBlock` and
`ExtensionBlockImpl` is the second instance of that pair and the template for any further one.

References are the identity layer (`CodeModel/References/`: `SymbolRef<T>`, `IntroducedRef<T>`,
`ConstructedTypeRef<T>`, `SyntaxRef<T>`, `DeclarationIdRef<T>`, `TypeIdRef<T>`). A reference is typed by
the *interface*, so every new declaration kind with its own public interface forces a new arm in the
kind-to-interface tables.

**Roslyn-version sensitivity.** There is currently **no `#if ROSLYN_*` in production source anywhere in
this subsystem**; a grep over `CodeModel/` finds only `#if DEBUG` (thirteen sites) and
`#if NET5_0_OR_GREATER` / `#if NET6_0_OR_GREATER` (four sites). `eng/RoslynVersions/Roslyn.5.10.0.props:8-10`
states this explicitly. The four new Roslyn 5.10 grammar nodes are `ExperimentalUrl`-marked and absent from
Roslyn 5.0, so any code model code naming `UnionDeclarationSyntax`, `UnsafeExpressionSyntax`,
`WithElementSyntax` or `BreakStatementSyntax.Name` will not compile in the Roslyn 5.0 variant. That would
be the **first production source branch on `ROSLYN_5_10_0_OR_GREATER`** and would falsify the props
comment.

The established containment device is the `*Safe` wrapper in `Utilities/Roslyn/`, so exactly one file
carries the `#if`. `SymbolExtensions.IsExtensionSafe` (`:384-387`) is the model; git shows its original
guarded form, stripped by commit `08d065a9f8` once the floor moved, and commit `e247425d69` is the earlier
instance of the same clean-up. Sibling wrappers: `SymbolExtensions.GetAttributesSafe` (36),
`ReflectionHelper.GetTypeByMetadataNameSafe` (58), `AsyncHelper.IsAsyncSafe` (19),
`LanguageVersionExtensions.ToDisplayStringSafe` (12).

Roslyn APIs the code model already calls unconditionally that would break an older variant:
`INamedTypeSymbol.IsExtension` and `.ExtensionParameter` (`Source/ExtensionBlockImpl.cs:21,24,34`),
`IMethodSymbol.AssociatedExtensionImplementation` (`Source/SourceMethod.cs:174`), the
`PartialDefinitionPart` / `PartialImplementationPart` family
(`Factories/DeclarationFactory.Symbols.cs:212,224`; `References/SymbolNormalizer.cs:18,20,75,77,87,89`;
`Source/SourceMethod.cs:195`), `IEventSymbol.IsPartialDefinition` (`Helpers/DeclarationExtensions.cs:463`),
`FieldExpressionSyntax` (`Utilities/Roslyn/SyntaxHelpers.cs:95`),
`RefKind.RefReadOnlyParameter` (`Source/SourceParameter.cs:57`),
`TypeKind.Extension` (`Utilities/Roslyn/SymbolExtensions.cs:479`).

**The thirteen-step pattern the C# 14 wave used for a new kind of type declaration**, in order:

1. Public interface derived from the closest existing one, plus its collection and builder interfaces.
2. Enum members, appended: `TypeKind.Extension`, `DeclarationKind.ExtensionBlock`,
   `RefTargetKind.ExtensionBlock`. Appended, never inserted, because `TypeOrderingComparer.cs:39` sorts by
   ordinal and `RefTargetKind` is serialised by name into compiled assemblies.
3. A `*Safe` predicate, so exactly one file knows about the Roslyn version.
4. One arm in the symbol-to-kind funnel `DeclarationExtensions.GetDeclarationKind` (line 44), placed
   *before* the general `SymbolKind.NamedType` arm.
5. A `Source` facade plus implementation pair, with the base class's `CheckSymbol` gaining
   `Invariant.Assert( !this.NamedTypeSymbol.IsExtensionSafe() )` (`SourceNamedTypeImpl.cs:59`) so the two
   cannot be confused.
6. The construction site, `DeclarationFactory.GetNamedType` 185-198.
7. Reference plumbing: `RefFactory.FromAnySymbol` 114-118, `RefExtensions.ToRef(INamedTypeSymbol)` 142-145,
   `RefExtensions.ToExtensionBlockRef` 156, the `SymbolRef` assert 81-86,
   `RefTargetKindExtensions.ToDeclarationKind` 52, `FullRef.ApplyRefKind` 170,
   `GetPossibleDeclarationInterfaceTypes` 112 and 118.
8. Member enumeration: `IsValidNamedType` excludes the new kind (`SymbolRef.Strategy.cs:298-301`), a new
   `IsValid<Kind>` predicate is added (315), and `GetSymbolPredicate` gains an arm (328).
9. Collections: a `*UpdatableCollection`, a read-only `*Collection`, a field and an accessor on
   `CompilationModel` (`_extensionBlocks` 40, `GetExtensionBlockCollection` 188), initialisation (245),
   prototype copy (349), and the property on the owner (`SourceNamedTypeImpl.ExtensionBlocks` 317-327).
10. Builder trio: `*Builder` deriving from the nearest existing builder, `*BuilderData`, `Introduced*`;
    plus the `AddDeclaration` arm (`CompilationModel.Members.cs:490`), the factory method
    (`DeclarationFactory.Builders.cs:188`) and its switch arm (251).
11. Visitors: a `Visit<Kind>` virtual **defaulting to the nearest existing one**
    (`CompilationElementVisitor.cs:210`, `TypeVisitor.cs:39`), the dispatch arms
    (`CompilationElementVisitor.cs:48,100`; `TypeVisitor.cs:24`), and the real override in
    `DisplayStringFormatter.cs:388`.
12. Widen every `DeclarationKind.NamedType` test that also means "a thing that has members": eight sites
    in `DocumentationIdHelper.Parser.cs`, one in
    `DocumentationIdHelper.GeneratorOfReferenceIdFromDeclaration.cs:28`, one in
    `DeclarationFactory.Builders.cs:219`.
13. Tests: `Metalama.Framework.Tests.UnitTests/CodeModel/CodeModelTests.CSharp14.cs` (a `partial class`
    file per language wave) plus a directory per feature under
    `Metalama.Framework.Tests.AspectTests/Tests/Aspects/CSharp14/`.

For a **new member-level construct** the wave instead extended a **table** (`OperatorData.All`) or added a
**derived-property helper** (`GetPropertyKind`, `ContainsFieldExpression`). That is the cheaper half of the
pattern and the right shape for `closed` and for indexers in extension blocks.

The decision that is not mechanical, for `union`: **does it get its own `TypeKind` value and public
interface, or is it `TypeKind.Class` plus a flag?** Records took the flag route (`TypeKind.RecordClass` was
obsoleted in favour of `TypeKind.Class` plus `INamedType.IsRecord`; see `TypeKind.cs:29-30` and
`SourceNamedTypeImpl.cs:173`). Extension blocks took the new-kind route because they have no name and
cannot be nullable. A union is a named, nameable, nullable type with members, so the record precedent costs
**zero** switch arms, while a new `TypeKind` value costs at minimum the arms in rows 39-48 of the hotspot
table plus `TypeKindExtensions.IsNamedType`. Either way, `SourceNamedTypeImpl.TypeKind` (69-79) must map
the Roslyn `TypeKind`: if Roslyn represents a union as `TypeKind.Class` nothing changes; if it introduces
`TypeKind.Union`, this is the first thing that fails, and it fails loudly.

`SerializableIds/**` deserves separate mention: 3648 lines across sixteen files, carrying Metalama's own
fork of Roslyn's `DocumentationCommentId` (`DocumentationIdHelper*.cs`, 1608 lines). Every grammar change
that changes the shape of a declaration identifier lands here rather than being inherited from Roslyn.

### 2.3 TMPL — the template compiler and the grammar generator

Scope: `Metalama.Framework/src/Metalama.Framework.Engine/Templating/**`,
`eng/src/GenerateMetaSyntaxRewriter/**`, and the generated output in
`Metalama.Framework/.generated/<roslyn version>/**` (git-ignored, produced by `Build.ps1 prepare`).

**What the generator produces, per non-legacy version** (`GenerateMetaSyntaxRewriter.cs:30-49`):

| Generator method | Output file | Target project | Line |
| --- | --- | --- | --- |
| `GenerateRoslynApiVersionEnum` | `Metalama.Framework.Engine/RoslynApiVersion.g.cs` | Engine | 39 |
| `GenerateTemplateFiles` | `Metalama.Framework.Engine/MetaSyntaxRewriter.g.cs` | Engine | 44 |
| `GenerateVersionChecker` | `Metalama.Framework.Engine/RoslynVersionSyntaxVerifier.g.cs` | Engine | 45 |
| `GenerateHasher` (run time) | `Metalama.Framework.DesignTime/RunTimeCodeHasher.g.cs` | DesignTime | 46 |
| `GenerateHasher` (compile time) | `Metalama.Framework.DesignTime/CompileTimeCodeHasher.g.cs` | DesignTime | 47 |
| `GeneratePartialUpdate` | `Metalama.Framework.Engine/SyntaxNodePartialUpdateExtensions.g.cs` | Engine | 48 |

**A new syntax node** produces: `VisitFoo` and `TransformFoo` in `MetaSyntaxRewriter.g.cs`
(`Generator.cs:396-525`; a leading `Argument(this.Transform(node.Kind()))` when the node has more than one
kind, 497-501); a `Foo(...)` factory in `MetaSyntaxFactoryImpl` (527-613, with a minimal overload when the
node has auto-creatable token fields, 597-602); a `VisitFoo` in `RoslynVersionSyntaxVerifier.g.cs` pinned
to the version at which the node appears (100-174, `IsVersionSpecificType` at 160); a hashing `VisitFoo` in
both hashers (615-712); and a `PartialUpdate` overload (737-803). Nothing else. In particular the
generator emits **no** `TemplateAnnotator` override, **no** `TemplateCompilerRewriter` override and **no**
diagnostic; those are all hand-written.

**A new optional field on an existing node** makes `TransformBreakStatement` version-switched
(`Generator.cs:432-479`, because `node.Fields.Select( f => f.MinimalRoslynVersion ).Distinct()` now has
more than one element), adds a parameter to the factory (594), adds
`VisitVersionSpecificField( node.Name, RoslynApiVersion.V5_10_0 )` to the verifier (127-156), adds
`this.Visit( node.Name );` to both hashers, and adds an `Option<IdentifierNameSyntax?> name = default`
parameter to `PartialUpdate`. The existing example of the version-switched shape is
`TransformClassDeclaration` in `.generated/5.0.0/.../MetaSyntaxRewriter.g.cs:4441-4501`, split on
`ClassDeclarationSyntax.ParameterList` which Roslyn 4.8 added.

**A new `SyntaxKind` on an existing field** changes only `RoslynVersionSyntaxVerifier`
(`Generator.cs:135-153`, helper `GetVersionSpecificKinds` 164-173, with the guard at 166-170 that if
*every* kind of the field is version-specific none is reported). The rewriter is unaffected, because
`MetaSyntaxRewriter.Transform( SyntaxToken )` (`MetaSyntaxRewriter.cs:239-294`) is kind-generic.

**The experimental filter is the single switch for C# 15.** `TreeReader.RemoveExperimentalDeclarations`
deletes all five additions before any code is generated. Removing an `ExperimentalUrl` attribute from the
grammar file is the act that turns generated support on. `Metalama.Framework/docs/updating-roslyn.md:11`
states the standing policy: "Study the new C# syntax features. We IGNORE any experimental feature. They are
not supported."

**The classification algorithm**, for context. Two annotations drive everything, both defined in
`Templating/SyntaxAnnotationExtensions.cs`: the **scope** annotation (`TemplatingScope`: `RunTimeOnly`,
`CompileTimeOnly`, `RunTimeOrCompileTime`, `CompileTimeOnlyReturningBoth`,
`CompileTimeOnlyReturningRuntimeOnly`, `RunTimeTemplateParameter`, `LateBound`, `Conflict`,
`TypeOfRunTimeType`, `DynamicTypeConstruction`) and the **target scope** annotation, whose only interesting
value is `MustFollowParent`. `TemplateAnnotator` computes them bottom-up: `VisitCore` → `DefaultVisitImpl`
(627-643) visits children then calls `AddScopeAnnotationToVisitedNode` (648-698), which either reports
LAMA0104 for a scope mismatch, keeps an existing annotation or a `StatementSyntax` (685-690), or combines
the scopes of the node's `ExpressionSyntax` and `InterpolationSyntax` children (693-697).
`GetExpressionScope` (446-590) combines child execution and value scopes with the node's own expression
type scope and maps the pair through the table at 559-571; **an empty child list returns
`RunTimeOrCompileTime`** (448-451). `TemplateCompilerRewriter.IsCompileTimeCode` (199-264) then reads those
annotations: compile-time code is copied through, run-time code is transformed into the syntax-building
expression by the generated `Transform*` methods.

**How C# 14 was absorbed**, in four steps:

1. **Refuse the feature, loudly, in the annotator.** Commit `cf0861898b` (#1105) added exactly two things
   to `TemplateAnnotator.cs`: a `VisitFieldExpression` override inside `#if ROSLYN_5_0_0_OR_GREATER`
   calling `ReportUnsupportedLanguageFeature( node, "field keyword" )`, and a check inside
   `VisitAssignmentExpression` for `node.Parent.IsKind( SyntaxKind.ConditionalAccessExpression )`. Both
   used the existing LAMA0101 descriptor, and the commit added the test files and baselines in the same
   change.
2. **Implement the feature end to end, and delete the refusal.** Null-conditional assignment: commit
   `b4da958605` added `ITemplateSyntaxFactory.RewriteAssignmentExpression`, its implementation, and the
   `TransformAssignmentExpression` override that wraps every transformed assignment; commit `e9edd7cacc`
   then removed the eight lines of refusal. The `field` keyword: commit `aea7b2e5a2` replaced the refusal
   with `return node.AddScopeAnnotation( RunTimeOnly );`, added `TemplateCompilerRewriter.VisitFieldExpression`
   emitting a call to `ITemplateSyntaxFactory.GetPropertyBackingField()`, added that member to the interface
   and its implementation, and added `CompiledTemplateAttribute.IntroducesBackingField` /
   `IsBackingFieldAssigned` so the advice layer knows to introduce the field.
3. **Guard the new API with `#if ROSLYN_<version>_OR_GREATER` while an older variant still ships, then
   delete the guard when it is dropped.** `Templating/` contains no `#if ROSLYN_*` today; the only
   conditional compilation left is `#if DEBUG` at `MetaSyntaxRewriter.cs:308`,
   `SyntaxAnnotationExtensions.cs:121,315` and `SyntaxTreeAnnotationMap.cs:68`.
4. **A test folder per feature, with committed baselines.** `Tests/Aspects/CSharp14/` has one subfolder per
   feature; `FieldKeyword/` alone holds 21 test pairs. Templating-level refusals live in
   `Metalama.Framework.Tests.TemplateTests/Tests/UnsupportedSyntax/`.

The grammar side of the same wave: `b46f9218a8` replaced a hand-edited `Syntax-5.10.0.xml` with the real
one from `Metalama.Compiler`; `e1cbb88a77` added `ExperimentalUrl` and the removal pass; `08d065a9f8` is
the model for renumbering or dropping a variant.

**What C# 14 did not do, and should be read as a gap rather than as precedent.** C# 14 introduced a
genuinely new type declaration, the extension block, and the templating subsystem gained **no**
`VisitExtensionBlockDeclaration` in `TemplateAnnotator` and **none** in `TemplatingCodeValidator.Visitor`.
The only hand-written handling is in the linker (`LinkerInjectionStep.Rewriter.cs:324`,
`LinkerLinkingStep.LinkingRewriter.cs:79`) and the design-time generator
(`DesignTimeSyntaxTreeGenerator.cs:277,662,672`). So the C# 15 `union` declaration will not be covered by
following the C# 14 precedent.

Note on `RoslynApiVersion`: the generated enum in `.generated/5.0.0/` still has `V4_0_1 = 0` through
`V5_0_0 = 4` with `Lowest = V4_0_1`, because the four 4.x grammars remain in `legacyVersionNames` even
though no 4.x variant ships. The 5.10 generated enum will additionally have `V5_10_0 = 5`,
`Current = V5_10_0`, `Highest = V5_10_0`.

### 2.4 ADV — advising and advice implementation

Scope: `Metalama.Framework.Engine/Advising/**` (32 files), `Metalama.Framework.Engine/AdviceImpl/**`
(six folders, 74 files), `Metalama.Framework/Advising/**`. Adjacent files this subsystem cannot be changed
without: `Metalama.Framework/Aspects/AdviserExtensions.cs`, `Metalama.Framework/Eligibility/EligibilityRuleFactory.cs`
and `.Contracts.cs`, `CodeModel/Helpers/ModifierHelper.cs` and `ModifierCategories.cs`,
`CodeModel/Introductions/Builders/ExtensionBlockBuilder.cs` and `ExtensionReceiverParameterBuilder.cs`.

`Metalama.Premium` contains **no** advice implementation: a grep for `IAdviceFactory` or `Advising` there
matches only four test files. Nothing in that repository has to change for a language wave in this
subsystem.

**Version sensitivity is almost nil.** This is target-framework-agnostic `netstandard2.0` code consuming
the Roslyn syntax API through `SyntaxFactory`. There is not a single `#if` on a Roslyn constant, not a
single `LanguageVersion` reference, and not a single `RuntimeInformation` or `Environment.Version`
reference anywhere under `Advising/` or `AdviceImpl/`. The version-recent Roslyn APIs it calls are
`SyntaxKind.ExtensionKeyword` and `ExtensionBlockDeclaration(…)`
(`IntroduceExtensionBlockTransformation.cs:56-66`), `SyntaxKind.FieldKeyword`
(`IntroduceEventTransformation.cs:201`, `IntroducePropertyTransformation.cs:217`), and `WithCheckedKeyword`
(`IntroduceMethodTransformation.cs:86,116`) — all satisfied by the Roslyn 5.0 floor. Consequently **the
first C# 15 syntax node this subsystem has to emit will be the first production `#if` in it**, and there is
no precedent to copy inside `Advising/` or `AdviceImpl/`.

There is **no IDE-host sensitivity at all**: the design-time distinction reaches this subsystem only
through `context.SyntaxGenerationContext.IsPartial` (`AdviceSyntaxGenerator.cs:121-131`) and
`AdviceFactoryState.ExecutionScenario` (`AdviceFactoryState.cs:63`).

**How C# 14 was absorbed**, as ten observations:

1. **A blanket refusal first.** The original code refused to advise anything inside an extension block, in
   `AdviceFactory.ValidateTarget.ValidateOneTarget`. Commit `737e0347a9` (#1035) deleted it.
2. **Replaced by narrow, per-advice refusals.** The blanket check became one `ValidateNotExtensionBlock`
   call per `Introduce*` method that C# 14 does not allow. Commit `5e65ceb149` (#1159) is the canonical
   example and is a two-line diff adding `ValidateNotExtensionBlock( targetType, "an indexer" );` with the
   message "C# 14 extension blocks don't support indexers with the this[] syntax (CS9282)."
3. **Each refusal is pinned by an error aspect test** under
   `Tests/Aspects/Introductions/ExtensionBlocks/`: `ErrorIndexerIntoExtensionBlock`, `ErrorFieldIntoExtensionBlock`,
   `ErrorEventIntoExtensionBlock`, `ErrorConstructorIntoExtensionBlock`, `ErrorAutoPropertyIntoExtensionBlock`,
   `ErrorNestedTypeIntoExtensionBlock`, `ErrorExtensionBlockIntoExtensionBlock`, plus target-shape and
   builder-restriction errors. The expected output is the raw `LAMA0041` wrapper around the
   `InvalidOperationException` message, so the message text is part of the baseline.
4. **When support lands, the refusal is deleted and a fan-out is added.** Commit `30e21aea98` (#1127)
   removed `ValidateNotExtensionBlockReceiver( targetParameter, "a contract" )` and added, in the same
   commit, `ContractExtensionBlockTransformation.cs` (145 lines), the
   `case DeclarationKind.Parameter when … IExtensionBlock` arm in `ParameterContractAdvice.cs`, a new
   eligibility clause (`EligibilityRuleFactory.Contracts.cs:111`), and seven new aspect tests.
5. **New syntax kinds get an implicit-declaration hook, not a special case.** #1036 introduced
   `ExtensionImplementationHelper.cs` and overrode `GetImplicitDeclarations()` in
   `IntroduceMethodTransformation` and `IntroducePropertyTransformation`, wired into
   `AdviceFactoryState.AddTransformations` (75-80). The virtual
   `BaseTransformation.GetImplicitDeclarations()` (line 63) returns empty, so a transformation that forgets
   to override it simply produces nothing.
6. **Behaviour that depends on a new construct is carried on the template, not inferred at the use site.**
   #1114 added two booleans to `TemplateMember`, read from `CompiledTemplateAttribute` for cross-project
   templates and from `DeclaringSyntaxReferences` for same-project templates, consumed at exactly two sites.
7. **New operator forms are absorbed by widening a category enum and delegating to a data table.** #1116
   replaced `finalMethod.OperatorKind.ToOperatorKeyword()` with `OperatorData.GetByKind(…)`, added
   `operatorData.IsChecked` handling, and added the `!OperatorData.IsUserDefinable( kind )` gate. The
   resulting asymmetry: overriding a compound assignment operator works, but there is no
   `IntroduceCompoundOperator` API and `IntroduceUnaryOperator` / `IntroduceBinaryOperator` reject those
   categories.
8. **Partial members were absorbed by an existing boolean.** #1110-#1113 touched only
   `IntroduceConstructorAdvice.cs` (+22/-4) and `IntroduceEventTransformation.cs` (+2/-1) here.
9. **Tests are foldered by language version**, with introduction-side extension-block tests kept separately
   under `Tests/Aspects/Introductions/ExtensionBlocks/`.
10. **Diagnostics get a reserved sub-range recorded in `Ranges.md`.** #1159 claimed 0540-0549 and recorded
    it in `Metalama.Framework.Engine/Diagnostics/Ranges.md:14` in the same pull request.

The quick index of the highest-value line numbers, kept verbatim from the map:

```
AdviceFactory.cs:527   ValidateNotExtensionBlock          (the gate)
AdviceFactory.cs:536   ValidateNotExtensionBlockReceiver
AdviceFactory.cs:1406  ValidateNotExtensionBlock(targetType, "an indexer")     <- delete for C# 15
AdviceFactory.cs:730   IntroduceFinalizer                 (missing gate)
AdviceFactory.cs:1513  IntroduceEvent add/remove overload (missing gate)
AdviceFactory.cs:2068  TypeKind.Class literal (IntroduceClass)
AdviceFactory.cs:2090  TypeKind.Interface literal (IntroduceInterface)
ContractExtensionBlockTransformation.cs:93   foreach extensionBlock.Indexers   <- already speculative
IntroduceMemberAdvice.cs:197                 TypeKind.Extension carve-out
IntroduceNamedTypeTransformation.cs:62-92    TypeKind -> declaration syntax
IntroduceMethodTransformation.cs:228         GetImplicitDeclarations (methods)
IntroducePropertyTransformation.cs:226       GetImplicitDeclarations (properties)
IntroduceIndexerTransformation.cs            (no GetImplicitDeclarations)      <- add for C# 15
ExtensionImplementationHelper.cs:177         "set_"/"get_" + propertyName      <- no indexer form
ImplementInterfaceAdvice.cs:196              "Indexers are ignored"
ImplementInterfaceAdvice.cs:780              NotImplementedException, interface indexers
TemplateMember.cs:61-70                      IntroducesBackingField / IsBackingFieldAssigned
TemplateBindingHelper.cs:119-120             OperatorCategory.UnaryAssignment / BinaryAssignment
OverrideHelper.cs:143                        ComputeBackingFieldName
AdviceKind.cs:50                             IntroduceExtensionBlock (last enum value)
EligibilityRuleFactory.cs:121                _introduceRule TypeKind allow-list
AdviceDiagnosticDescriptors.cs:302-319       LAMA0540 / LAMA0541
```

### 2.5 LINK — the aspect linker

Scope: `Metalama.Framework.Engine/Linking/**` (167 files) and `Metalama.Framework.Engine/Transformations/**`
(31 files), with the design documents `Metalama.Framework/docs/{linker-overview,linker-architecture,linker-inlining,linker-callsite}.md`
and the test harness `Metalama.Framework/src/tests/Metalama.Framework.Tests.LinkerTests/**` plus
`Metalama.Framework.Tests.UnitTests/Linker/LinkerTriviaPreservationTests.cs`. `Metalama.Premium` contains
**no** linker or transformation code, so the C# 15 linker work is confined to `Metalama.Framework`.

The three steps and where language shape enters each:

| Step | Class | Where language shape enters |
| --- | --- | --- |
| 1. Injection | `LinkerInjectionStep` | A `CSharpSyntaxRewriter` overriding one `Visit*Declaration` per concrete type and member syntax kind |
| 2. Analysis | `LinkerAnalysisStep` | Control-flow shape of statements (`BodyAnalyzer`), expression shape around aspect references (`AspectReferenceResolver`, the inliners) |
| 3. Linking | `LinkerLinkingStep` | A second `CSharpSyntaxRewriter` plus `LinkerRewritingDriver`, dispatching on `SymbolKind`, `MethodKind` and `SyntaxKind` |

Every rewriter derives from `SafeSyntaxRewriter`, which adds a recursion guard and wraps exceptions in
`SyntaxProcessingException`. It does **not** add any unknown-node detection: an unrecognised node falls
through to `CSharpSyntaxRewriter.Visit`, which recurses generically and returns the node unchanged. That
default is the root cause of most of the silent-failure risks in section 5.

**Language-version sensitivity is exactly two tests.** `LinkerAnalysisStep.cs:553`
(`context.CompilationContext.LanguageVersion < AllLanguageVersions.CSharp14`, with the comment "Before
C# 14, we must specify the type of all lambda parameters because of `in`") and
`LinkerInjectionHelperProvider.cs:219` (`options.Version is LanguageVersion.CSharp9 or LanguageVersion.CSharp10`),
which is an equality test against two specific versions and is now permanently false, so `#nullable enable`
is never emitted in the helper tree.

**Roslyn-version sensitivity is zero conditional compilation.** `grep -rn "#if ROSLYN" Linking/ Transformations/`
returns nothing. Roslyn differences are absorbed by *not building* the older variant:
`ExtensionBlockDeclarationSyntax`, `FieldExpressionSyntax`, `IsPartialDefinition` on properties and events,
and `PartialDefinitionPart` are all used unconditionally. The one remaining historical comment is at
`LinkerAnalysisStep.AspectReferenceWalker.cs:45-49`, now dead weight with a `#pragma warning disable IDE0004`
that would fail a `-p:ContinuousIntegrationBuild=True` build if the pragma were removed.

The linker test project targets `net48;net10.0`, not the `net472` desktop flavour stated for PB-2027.0;
that is worth reconciling. `src/tests/Metalama.Framework.Tests.LinkerTests.4.12.0/` remains on disk with
only `obj/` and no `.csproj`.

**How C# 14 was absorbed.** Every C# 14 change in the linker is a *widening* of an existing enumeration,
plus (where a genuinely new node type appeared) one new `SyntaxNodeSubstitution` class and one new entry in
`CreateOriginalBodySubstitution`. No new abstraction was introduced; no `#if` was added.

Extension members (#1034, #1035, #1036, #1127, #1159) — commits `cdf076ee1a` (nothing here),
`737e0347a9`, `22697b6ba5` (`Transformations/BaseTransformation.cs` +6, `Transformations/ITransformation.cs` +9),
`30e21aea98` (163 insertions across `LinkerInjectionRegistry.cs`,
`LinkerInjectionStep.InsertStatementTransformationContextImpl.cs`, `LinkerInjectionStep.Rewriter.cs`,
`LinkerInjectionStep.TransformationCollection.cs`, `LinkerInjectionStep.cs`,
`Transformations/IInsertStatementTransformation.cs`), and `f374fce480` (`LinkerInjectionStep.Rewriter.cs` +22,
`Transformations/InsertPosition.cs` +3). The pattern was: one `Visit*` override delegating to the existing
generic handler; one `case` in the injected-node switch (621-637, which unwraps `ExtensionBlockBuilderData`,
a *different* builder-data type from `NamedTypeBuilderData`, and recurses); widening every `DeclarationKind`
switch (`LinkerInjectionStep.cs:251`, `:837-874` including `ForEachMethodInExtensionBlock` at 1136,
`LinkerInjectedMemberComparer.cs:29,73`, `LinkerInjectionStep.TransformationCollection.cs:834,842`,
`Transformations/InsertPosition.cs:73`); widening the "how do I write a reference to this member" helpers
because an extension member's receiver is a parameter rather than `this`
(`LinkerAspectReferenceSyntaxProvider.cs:213-214,268-269,289-290`, `Transformations/ProceedHelper.cs:234-235,252-253`);
and adding extension-block-specific guards where a member-shaped assumption breaks
(`LinkerInjectionRegistry.cs:203`, `LinkerInjectionStep.Rewriter.cs:636`).

The `field` keyword (#1094, commits `70bd44a5e1` and the fix-up `48541ada9b`) is **the pattern for a new
expression form**: (1) a dedicated walker for the new node (`LinkerAnalysisStep.AutoPropertyBodyWalker.cs:16-27`);
(2) a collection pass in the analysis step (`LinkerAnalysisStep.cs:1065-1140`, driven by the "hybrid auto
property" set computed at 198-201); (3) a new `SyntaxNodeSubstitution` (two of them, in fact); (4) threading
the new reference list through into `SubstitutionGenerator`; (5) widening `IsInlineableProperty`
(`LinkerAnalysisStep.InlineabilityAnalyzer.cs:387-391`).

User-defined compound assignment operators (#1116) is **the pattern for a new operator category**: extend
`OperatorData` and `OperatorCategory` outside the linker; generate the matching helper members in the helper
tree (`LinkerInjectionHelperProvider.cs:230-243`); emit the aspect reference through the helper
(`LinkerAspectReferenceSyntaxProvider.GetOperatorReference` 163-191, adding `Argument( ThisExpression() )`
for non-static operators at 181); teach the inliner to recognise the extra receiver argument
(`InlinerHelper.IsCanonicalInvocationWithStaticReceiver` 42-91); and widen `ResolveExpressionTarget`.

Partial members (#1110-#1113, #1114, #1143) is **the pattern for an existing declaration acquiring a new
form**: add the guard to every `RewriteX` entry point (the eleven sites listed in the map); normalise the
symbol in one place (`LinkerSymbolHelper.GetCanonicalDefinition`, which currently has blocks for methods
(20), properties (25) and events (30), so a fourth partial-able member kind needs a fourth block); accept
the body-less form in `LinkerSyntaxHandler.GetCanonicalRootNodeOrNull` (68); add the substitutions that
fabricate the missing body and register them in `CreateOriginalBodySubstitution` (884-898); and redirect
member-level-transformation lookups from the definition part to the implementation part
(`LinkerInjectionStep.Rewriter.cs:1491-1497`, the #1143 fix).

None of the four design documents was updated by any C# 14 commit; all four describe the pipeline in terms
of the declaration kinds that existed before extension blocks, `field` and partial members.

### 2.6 SYNGEN — syntax generation, serialisation, formatting and manifest serialisation

Scope, all under `Metalama.Framework/src/Metalama.Framework.Engine/`: `SyntaxGeneration/**` (21 files,
2 826 lines), `SyntaxSerialization/**` (49 files, 2 121 lines), `Formatting/**` (18 files, 2 316 lines),
`Serialization/**` (5 files, 393 lines).

Three facts frame everything else:

1. **Every file of this subsystem is compiled once per Roslyn variant.**
   `Metalama.Framework.Engine.5.0.0.csproj` is a `<Compile Include="../Metalama.Framework.Engine/**/*.cs" />`
   glob plus an import of `Roslyn.5.0.0.props`. Any reference to a Roslyn 5.10 API placed in these files
   without a `#if ROSLYN_5_10_0_OR_GREATER` guard breaks the Roslyn 5.0 variant build.
2. **There is currently no production `#if` on the Roslyn version anywhere in this subsystem.** Enabling
   C# 15 reintroduces production branching that commit `e247425d69` deliberately removed for the 4.x wave.
3. **The four C# 15 grammar changes are invisible to the code generator today**, because of the
   experimental filter.

**How C# 14 landed here: it almost entirely bypassed this subsystem.** Checking each C# 14 commit
individually (`aea7b2e5a2`, `929d055d85`, `aa5e62dbb0`, `df4ae55b09`, `81e5a5fed7`, `e3b3fc5959`,
`ca6c690592`, `e9edd7cacc`, `b4da958605`, `cf0861898b`, `a9698fa1e8`, `f374fce480`, `5a1ac3e5c4`,
`6c9ffc219d`, `f776fd9af9`, `bcdeb3a185`, `cdf076ee1a`, `22697b6ba5`, `30e21aea98`, `737e0347a9`,
`787ec4fcd8`, `5b121f3c21`, `6d8678e5d3`, `70bd44a5e1`, `48541ada9b`) shows **not one of them touched
`SyntaxGeneration/`, `SyntaxSerialization/`, `Formatting/` or `Serialization/`.** The one edit this
subsystem did receive was appending `TypeKind.Extension` to the two `TypeKind` lists in
`ContextualSyntaxGenerator.cs:142` and `:167` (commit `0622d353f5`, reshaped by `ee59906188` for issue
#1579).

The four mechanisms C# 14 used, and which one this subsystem sits in:

1. **Grammar-driven generation, the default.** New syntax nodes enter through `Syntax-<version>.xml` and are
   absorbed with no hand-written code. This is why the subsystem was untouched.
2. **A new `TypeKind` plus a virtual visitor method that falls back.** `TypeVisitor.cs:24,39` gives the new
   kind a `virtual` method whose default delegates to the nearest existing behaviour, so every existing
   visitor — including this subsystem's two `SyntaxGeneratorForIType` visitors — keeps compiling and
   produces something plausible without being edited.
3. **`#if ROSLYN_<version>_OR_GREATER` around the new-API call, then a strip commit when the floor moves.**
   The two instances in this subsystem (`ConstraintClauses` / `AllowsRefStruct`, and `SafeCastExpression` /
   `SyntaxKind.CollectionExpression`) were removed by `e247425d69`.
4. **A contextual-keyword escape hook placed where the keyword binds, not in `SafeIdentifier`.** The C# 14
   `field` keyword produced `TemplateSyntaxFactoryImpl.EscapeIdentifier` (933-942), which escapes `field`
   only inside a property accessor. `SyntaxFactoryEx.SafeIdentifier` was deliberately not changed: it stays
   reserved-keyword-only, and context-sensitive escaping is layered on top.

Two further conventions the wave established: divergent expected output is expressed with test directives,
not with source `#if`; and the version tables are edited together —
`SupportedCSharpVersions.Latest`, `.All`, `.ToLanguageVersion`, `.GetMaxLanguageVersion` and
`LanguageVersionProvider.GetLanguageVersionFromDotNetSdk` are five switches over the same axis, none of them
with a permissive default.

A correction the map makes explicitly: **`SyntaxGenerationOptions` does not carry the language version.**
It wraps a single `CodeFormattingOptions` field and exposes `WillBeTextualized` and `WillBeFormatted`. Any
claim that syntax generation depends on the language version through `SyntaxGenerationOptions` is false on
this branch; the dependency is on `SyntaxGenerationContext` and it is a single boolean
(`RequiresStructFieldInitialization`) plus a reference-set probe (`SupportsInitAccessors`). The corollary
matters: `CompilationContext.GetSyntaxGenerationContext` caches on a key that does **not** include the
language version, which is safe only because the cache is per-`CompilationContext`.

### 2.7 CT — compile-time compilation, pipeline and options

Scope: `Metalama.Framework.Engine/{CompileTime,Pipeline,Options}/**`, plus the four `Utilities/` files the
subsystem owns in practice (`SupportedCSharpVersions`, `AllLanguageVersions`, `LanguageVersionProvider`,
`ILanguageVersionProvider`) and the generator in `eng/src/GenerateMetaSyntaxRewriter/` that produces
`RoslynApiVersion` and `RoslynVersionSyntaxVerifier`. `Metalama.Premium` contains nothing in this
subsystem.

**There are four distinct language versions in play, chosen by four different mechanisms.** Confusing them
is the main hazard here.

| # | Version | Meaning | Where it is decided |
| --- | --- | --- | --- |
| 1 | `SupportedCSharpVersions.Latest` | The highest C# the current Metalama **build** admits | `Utilities/SupportedCSharpVersions.cs:31` |
| 2 | The project language version | `LangVersion` of the user project | `Options/MSBuildProjectOptions.cs:167` |
| 3 | The compile-time language version | Used to parse and compile the **compile-time compilation** | `Utilities/LanguageVersionProvider.cs:29` |
| 4 | The template language version | Enforced by the **template verifier** on template bodies | `Templating/TemplateCompiler.cs:33`, seeded from #3, overridden by `MetalamaTemplateLanguageVersion` |

The complete edit set to raise the compile-time language version to C# 15, in order:

1. `Utilities/AllLanguageVersions.cs` — add `CSharp15 = (LanguageVersion) 1500`.
2. `Utilities/Roslyn/LanguageVersionExtensions.cs:34` — add `(LanguageVersion) 1500 => "15.0"` before the throw.
3. `Utilities/SupportedCSharpVersions.cs:31` — `Latest => LanguageVersion.CSharp15`.
4. `Utilities/SupportedCSharpVersions.cs:38-43` — add `CSharp15` to `All`, and decide whether `CSharp10` leaves.
5. `Utilities/SupportedCSharpVersions.cs:52-62` — `RoslynApiVersion.V5_10_0 => AllLanguageVersions.CSharp15`.
6. `Utilities/SupportedCSharpVersions.cs:149-159` — split the `(>= 5, _)` arm so that `(5, >= 10)` yields
   C# 15 and lower 5.x still yields C# 14.
7. `Utilities/LanguageVersionProvider.cs:54-60` — add `>= 11 => LanguageVersion.CSharp15` above the `>= 10` arm.
8. `Directory.Build.props:16` — `MetalamaTemplateLanguageVersion`, only together with `RoslynApiMinVersion`.
9. `CompileTime/CompileTimeCompilationBuilder.cs:425` — the `languageVersion >= LanguageVersion.CSharp14`
   guard on `EMBED_SYSTEM_TYPES`; decide whether it stays at 14 or becomes version-independent.
10. `CompileTime/Manifest/CompileTimeProjectManifest.cs:101` — the `ResolvedLanguageVersion` default.
11. `eng/src/GenerateMetaSyntaxRewriter/GenerateMetaSyntaxRewriter.cs:17-18` — add the new grammar snapshot,
    and move `5.0.0` into `legacyVersionNames` if that variant is dropped.
12. `eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs:37,57` — the experimental filter must stop
    removing the C# 15 nodes once Roslyn drops `ExperimentalUrl`.
13. The standalone scenarios `Standalone/TemplateLanguageVersion14/` and `Standalone/DefaultLanguageVersion/`.

**The prerelease Roslyn package source** (issue #1885) lives entirely in
`Utilities/SupportedCSharpVersions.cs` and `CompileTime/CompileTimeAssemblyLocator.cs`, documented in
`docs/updating-roslyn.md:38-54`. `ToNuGetVersionString` (77-87) is the single switch; a version string
containing a hyphen is a prerelease, and `GetPrereleasePackageSourceUrl` (131-132) derives the feed URL from
that fact, deliberately, so that entering or leaving a prerelease is one edit. The constants are
`RoslynPrereleaseSourceKey = "roslyn-consolidated"` (93),
`RoslynPrereleaseSourceUrl = "https://proget.postsharp.net/nuget/roslyn-consolidated/v3/index.json"` (99),
`RoslynPackagePattern = "Microsoft.CodeAnalysis.*"` (104). `CompileTimeAssemblyLocator.cs:234` reads it,
lines 236-243 resolve the user-level `nuget.config` only when the source is non-null, lines 268 and 277-287
fold it into the cache key, lines 740-767 generate the project text, lines 777-830 write the generated
`nuget.config`, and `_unmappedPrereleasePackageSourceUrl` (130, assigned at 819) records the case where the
user's own configuration already maps `Microsoft.CodeAnalysis.*` elsewhere, so that
`ReferenceAssemblyBuildFailureClassifier` can explain the eventual `NU1101`.

**How C# 14 was absorbed here.** Two commits carry almost the whole wave.
`6e2b07a313` ("Adding Roslyn 5.0 and moving net6.0 to net8.0") added a `#if ROSLYN_5_0_0_OR_GREATER =>
LanguageVersion.CSharp14` arm to `Default`, added `CSharp14` to `All` under the same guard, added
`RoslynApiVersion.V5_0_0 => (LanguageVersion) 1400` to `ToLanguageVersion`, added
`V5_0_0 => "5.0.0-2.25460.106"` to `ToNuGetVersionString`, and turned the fall-through arm into
`#error Invalid Roslyn version`. The `#if` ladder was later flattened by `e247425d69` and `08d065a9f8`.
`afbab4eae8` ("Compile-time compilation uses lower lang version") is the template for the C# 15 work: it
renamed `Default` to `Latest`, created `ILanguageVersionProvider` and `LanguageVersionProvider` (separating
"what this Metalama build can do" from "what the .NET SDK in front of us can do"), registered the provider,
added `LanguageVersion?` to the manifest, replaced `DefaultParseOptions` in
`CompileTimeProjectRepository.Builder.cs` with parse options derived from the manifest, added
`MetalamaTemplateLanguageVersion` end to end, added `SdkVersion` to `IProjectOptions`, and added
`TestContextOptions.TemplateLanguageVersion`.

Later commits in the same wave: `aea7b2e5a2` (#1114) touched only `CompileTime/RunTimeAssemblyRewriter.cs`;
#1159 (extension blocks) added **nothing** to `CompileTime/**`, because no compile-time type may itself be
an extension block, which is why `SyntaxKindExtensions.IsTypeDeclaration` still excludes it; `a67fac8277`
(#1247) added the whole `GetLanguageVersionFromMSBuild` path; #1896 added the
`Standalone/TemplateLanguageVersion14` scenario.

The pattern restated for C# 15: (1) add the grammar snapshot, list it, run `build.ps1 prepare` — the
verifier and the meta rewriter follow automatically **for non-experimental nodes only**; (2) add the
`LanguageVersion` constant, its display string, and raise `Latest` and `All`; (3) map the new
`RoslynApiVersion` member and give it its package version string; (4) extend the SDK-major switch and the
Roslyn-assembly-version switch; (5) raise `MetalamaTemplateLanguageVersion` only together with
`RoslynApiMinVersion`; (6) add per-feature support in `AdviceImpl`, `CodeModel`, `Linking` and `Templating`
— `CompileTime/**` changes only when the construct can appear **inside compile-time code**, which for C# 15
means `union` and `closed` on an aspect class, and an indexer inside an extension block.

### 2.8 DT — design time and cross-process

Scope: `Metalama.Framework.DesignTime/**`, `Metalama.Framework.DesignTime.Contracts/**`,
`Metalama.Framework.DesignTime.Rpc/**`, `Metalama.Framework.CompilerExtensions/**`, plus the two design
documents `Metalama.Framework/docs/cross-process-communication.md` and `docs/design-time-memory.md`, and two
neighbouring files that *are* the design-time behaviour:
`Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs` and
`Metalama.Framework.Engine/Extensibility/ExtensionLoaderBase.cs` with
`Metalama.Framework.Engine/Options/TargetedAssemblyReference.cs`.

Four headline findings:

1. **The design-time assemblies themselves are almost entirely blind to the shape of the C# language.** A
   grep for `SyntaxKind`, `CSharpSyntaxVisitor`, `TypeDeclarationSyntax`, `DeclarationKind`, `TypeKind` and
   `LanguageVersion` across the three design-time projects returns hits in only four files:
   `Pipeline/Diff/PartialTypesVisitor.cs`, `Pipeline/Diff/PartialTypesHasher.cs`,
   `Refactoring/CSharpAttributeHelper.cs` and `CodeFixes/TheCodeFixProvider.cs`. Everything else works on
   `ISymbol`, `SemanticModel`, file paths and serialized identifiers.
2. **A grep for `ExtensionBlock` or `IsExtension` across the three design-time projects returns nothing.**
   The C# 14 wave produced *no* code in this subsystem. Its design-time work landed entirely in
   `DesignTimeSyntaxTreeGenerator.cs` plus design-time aspect-test baselines. That is the pattern C# 15 will
   repeat.
3. **The version sensitivity is concentrated, not diffuse.** Roughly twenty places carry all of it, in five
   groups: `ResourceExtractor` and `RoslynVariantPolicy`; the private-reflection bridges into Roslyn
   internals; the process-tree and process-name detection; the frozen `[Guid]` contract surface; and the
   generated code hashers.
4. **The dominant failure mode of this subsystem is silence.** `docs/platform-support.md:22-28` states it: a
   design-time payload that fails to load produces no diagnostic at all, and issue #1710 was diagnosed only
   after finding 8396 silently logged exceptions.

**Where C# 14 landed at design time.** `git log --grep` over the nineteen tracked issues, intersected with
paths under `Metalama.Framework.DesignTime*` and `Metalama.Framework.CompilerExtensions`, returns **no
commit**. The intersection with the design-time *pipeline* returns commits `5a1ac3e5c4`, `6c9ffc219d`,
`f374fce480`, `707522939d`, `1099dfba86`, `f776fd9af9` (#1159) and `0bc242649a`, `c36340bbf9`, `6c41855702`
(#1143), all in `DesignTimeSyntaxTreeGenerator.cs`. Commit `836bc53035` (#1119, "Test suites are broken with
legacy Roslyn versions") is the precedent for the *variant* dimension of a wave.

The pattern, stated: (1) the language change is absorbed in the code model and the engine, not in the
design-time assemblies, which consume `DeclarationKind`, `ISymbol` and `SerializableDeclarationId`;
(2) the one design-time file that changes is `DesignTimeSyntaxTreeGenerator.cs`, because it *emits* C# —
for C# 14 the edits were admitting `DeclarationKind.ExtensionBlock` in the target switch (115), adding
`CreateExtensionBlock` and `CreateExtensionBlockParameterList` (662-695), adding the extension-block
indentation depth (247-280) and special-casing `TypeKind.Extension` in the containing-type walk (381-388);
(3) a `@TestScenario(DesignTime)` aspect test is added with its generated-partial baselines; (4) the tests
are gated by a preprocessor constant, not by a Roslyn variant; (5) `SupportedCSharpVersions` moves last, in
three places at once, and the design-time pipeline does not consult it; (6) the contracts assembly does not
move — no C# 14 commit touched `Metalama.Framework.DesignTime.Contracts`, and
`CurrentContractVersions.ContractVersion_1_0` is still 3.

**The most recent wave in this subsystem is the platform wave, not a language wave.** `git log` restricted
to the four design-time projects, newest first: `335d6ff1a6`, `f41a609696`, `fcc028d43e` (#1898: degrade to
no implementation on a Roslyn below the floor, which produced `RoslynVariantPolicy`,
`ResourceExtractor.TryCreateInstance`, `ResourceExtractor.ReportUnsupportedHost` and LAMA0087);
`08d065a9f8`, `e413ad96f9` (#1881: replace the Roslyn 4.12 variant with a 5.0 variant and renumber the
latest to 5.10); `575be8b88a`, `cf2874353f`, `22d9d31779`, `751ef4c7f8` (#1876: `net8.0` to `net10.0`).
Its shape is: one policy type with unit tests, one loud diagnostic for the compile-time path, one written
report for the design-time path.

**The generated code hashers are the real language-shape machine of this subsystem.** `Generator.GenerateHasher`
(`eng/src/GenerateMetaSyntaxRewriter/Generator.cs:615-712`) emits one `Visit<Node>` per node in the grammar
snapshot and nothing else: `SyntaxToken` fields become `VisitTrivialToken` or `VisitNonTrivialToken`
(645-685), every other field becomes `this.Visit( node.<Field> )`. `IgnoreFieldContentInRunTimeCode`
(714-723) reduces `BlockSyntax`, `ArrowExpressionClauseSyntax` and `EqualsValueClauseSyntax` to a
null-or-not bit in the run-time hasher, which is what makes editing a method body not invalidate the
design-time pipeline. `IsTrivialToken` (725-735) makes only `StringLiteralToken`, `CharacterLiteralToken`,
`NumericLiteralToken` and `IdentifierToken` hash their text; every other token hashes its `RawKind` only.

The consequences for the four Roslyn 5.10 grammar additions: the latest variant gains `VisitUnionDeclaration`,
`VisitUnsafeExpression` and `VisitWithElement`; the Roslyn 5.0 variant does not. For the optional `Name`
field on `BreakStatementSyntax` and `ContinueStatementSyntax`, the 5.0 variant's generated method simply does
not read it — and unlike `MetaSyntaxRewriter`, `GenerateHasher` has **no** `switch ( this.TargetApiVersion )`
mechanism, so it cannot express "this field exists only above version X" and cannot fail when it meets one.

The `Metalama.Framework.DesignTime.csproj` glob at lines 33-36 falls back to a `-stubs` directory that does
not exist in the tree, so a missing generated directory compiles the variant with **no hasher at all**, and
the abstract `BaseCodeHasher` derivations that `DiffStrategy` lines 76 and 157 reference fail to resolve.

**The Premium repository is not yet on PB-2027.0** in a way that matters here:
`Metalama.Premium/src/Metalama.Extensions.CodeFixes.DesignTime/Metalama.Extensions.CodeFixes.DesignTime.csproj:6`
is still `net472;net8.0`, `src/Metalama.Extensions.CodeFixes.DesignTime.4.12.0/` is still present with its
`.csproj`, and `eng/RoslynVersions/` holds no `5.10.0`. Under the framework's current
`ExtensionLoaderBase` and `TargetedAssemblyReference` literals, a `net8.0`-targeted design-time extension is
never selected on .NET, and a `TargetRoslynVersion` of `4.12.0` never equals
`RoslynApiVersion.Current.ToVersion()`. Both mismatches are silent.

`docs/design-time-memory.md` bears on a language wave in one place: a new declaration kind must be nameable
by `SerializableDeclarationId` or `SerializableTypeId`, because that is the representation a durable
reference takes at design time (lines 74-95). Lines 130-137 record the precedent in which a durable
reference silently widened `Generic<int>` to `Generic<T>` with no diagnostic (issue #1797), which is the
exact failure shape to expect from an identifier grammar that does not cover a new construct.

### 2.9 BUILD — build, packaging and target frameworks

Scope: `eng/**`, `Directory.Packages.props`, `Directory.Packages.md`, `Directory.Build.props`,
`global.json`, `nuget.base.config`, `Metalama.Framework.CompilerExtensions.Resources`, the Package
projects, `Metalama.Framework/docs/platform-support.md` and `docs/compile-time-target-frameworks.md`.

The build subsystem is sensitive to the C# language set in exactly **one** place: the grammar-driven code
generator `eng/src/GenerateMetaSyntaxRewriter`. Everything else is sensitive to *versions*, not to language
shape.

Three facts dominate:

1. The four C# 15 grammar additions are already present in `Syntax-5.10.0.xml` but are deliberately deleted
   before code generation. Turning C# 15 on is therefore not a grammar-file edit; it is a decision about
   that filter, plus a refreshed grammar snapshot once the features go stable upstream.
2. `net11.0` is already declared supported to users (`MaximumNETCoreAppVersion` 11.0,
   `MaximumSdkVersion` 11.0), but **no shipped asset targets `net11.0`** and several derived values still
   cap the language at C# 14 and the SDK at major 10.
3. The single most dangerous line in the subsystem is the implicit-`LangVersion` clamp at
   `Metalama.Framework.targets:118`.

**Shipping target frameworks**, the complete set (test, benchmark and standalone projects omitted):

| Target frameworks | Projects |
| --- | --- |
| `netstandard2.0` only | `Metalama.Backstage.Tools`, `Metalama.Extensions.DependencyInjection`, `…DependencyInjection.ServiceLocator`, `Metalama.Extensions.Metrics`, `Metalama.Framework.Analyzers`, `Metalama.Framework.CompileTime`, `Metalama.Framework.CompileTimeContracts`, `Metalama.Framework.CompilerExtensions`, `Metalama.Framework.DesignTime.Rpc`, `Metalama.Framework.EditorExtensions`, `Metalama.Framework.Engine.Analyzers`, `Metalama.Framework.Package`, `Metalama.Framework.Sdk`, `Metalama.SourceTransformer`, `Metalama.Migration`, `Metalama.Migration.Transformer`, `Metalama.Licensing` |
| `netstandard2.0;net10.0` | `Metalama.Testing.Hooks`, `Metalama.Extensions.Multicast`, `Metalama.Framework` |
| `netframework4.7.2;net10.0;netstandard2.0` | `Metalama.Backstage` |
| `net472;net10.0` | `Metalama.Extensions.DiffEngine`, `Metalama.Extensions.HtmlWriter`, `Metalama.Framework.ConfigurationFiles`, `Metalama.Framework.DesignTime.Contracts`, `Metalama.Framework.DesignTime`, `Metalama.Framework.Engine`, `Metalama.Framework.Implementation.Package`, `Metalama.Framework.Introspection`, `Metalama.Testing.AspectTesting`, `Metalama.Testing.UnitTesting` |
| `net10.0;net472` | `Metalama.Framework.CompilerExtensions.Resources` — the two embedded flavours |
| `net472;net10.0;netstandard2.0` | `Flashtrace`, `Flashtrace.Formatters`, `Metalama.Patterns.Caching`, `…Caching.Aspects`, `…Caching.Backend`, `Metalama.Patterns.Contracts`, `Metalama.Patterns.Immutability`, `Metalama.Patterns.Memoization`, `Metalama.Patterns.Observability` |
| `net472;net10.0-windows` | `Metalama.Patterns.Wpf` |
| `net10.0` only | `Metalama.Backstage.Commands`, `Metalama.Backstage.DotNetTool`, `Metalama.Backstage.Worker`, `Metalama.Framework.Workspaces`, `Metalama.Tool` |
| `net10.0-windows*` | `Metalama.Backstage.Desktop.Windows`, `PostSharp.LicenseKeyGenerator`, `PostSharp.LicenseKeyReader`, `Metalama.LinqPad` |
| `net9.0` | `eng/src/BuildMetalama.csproj:6` — the build orchestrator itself |

**How the C# 14 wave was absorbed**, in three parts:

**Part A — take the Roslyn version.** Commit `6e2b07a313` touched, in one change: every
`<TargetFramework(s)>` element in the repository; `Directory.Packages.props`;
`Metalama.Framework/Directory.Build.props`; `build/RoslynVersion/Roslyn.5.0.0.props` (new) and
`Latest.imports`; the two `*.4.12.0.csproj` shim projects (new);
`Metalama.Framework.CompilerExtensions.Resources.csproj`; `Metalama.Framework.CompilerExtensions.csproj`;
`SupportedCSharpVersions.cs`; `CompileTimeAssemblyLocator.cs`; `ExtensionLoaderHelper.cs`;
`DevBackstageToolsLocator.cs`; and `Metalama.Framework.sln` (+370 lines of variant projects).

**Part B — take the grammar.** Commits `b46f9218a8` and `e1cbb88a77` (both #1881). The first added 52 lines
to `Syntax-5.10.0.xml` and removed three lines from `GenerateMetaSyntaxRewriter.cs`; the second added the
`ExperimentalUrl` attribute to the model and the removal pass to `TreeReader`. The principle, written into
`updating-roslyn.md:11-12`, is that the grammar snapshot stays faithful to upstream and the *filter* decides
what is generated. **That separation is the single most important thing to preserve for C# 15.**

**Part C — re-derive the variant set.** Commits `e413ad96f9`, `08d065a9f8`, `58e4141956`, `d92bbbb664` and
`d69c66e568`. The rule that came out of it is `platform-support.md:53-54`, rule 8: "An axis enters the
matrix only if some shipped asset depends on it. Before adding a target framework, a Roslyn variant or a
version cap for a platform, name the asset whose selection actually changes."

**The preprocessor-symbol discipline** (`Directory.Packages.md:211-221`): no production source branches on a
variant symbol; `ROSLYN_5_0_0_OR_GREATER` was removed together with **177 conditional blocks**, **69
`@RequiredConstant` and `@ForbiddenConstant` test directives**, and the `RequiredConstants` entries of three
`metalamaTests.json` files, once every variant sat on the same side of it; no new `DefineConstants` entry
unless the source has to branch on a distinction no existing constant expresses; and name a symbol after
the Roslyn version at which the distinction appears, never after a variant number.

**The template-language-version half** was a separate, later change: #1896, commits `778edd5dd6` and
`a5a1035bab`. Its build footprint is `Directory.Build.props:16`, a new
`Standalone/TemplateLanguageVersion14/` scenario pinning `<LangVersion>14.0</LangVersion>` so that the
scenario asserts the template language version alone, and `Standalone/Issue1757/OldAspects/OldAspects.csproj:7,11`
pinning both properties to `12.0` so that a raised repository default does not silently change what that
scenario tests.

**What was not needed**: no change to `Generator.cs`, to the model classes, to `VersionDetector`, or to
`TreeFlattening`. The generator absorbed C# 14's new nodes purely from the grammar diff. That is the
strongest single signal for the C# 15 estimate.

**Adding `net11.0` as a supported user target framework.** The platform *policy* already admits it
(`build/Metalama.Framework.props:31,33,38-39`; `platform-support.md:199-206`; `Directory.Packages.md:189`).
What is missing is the assets and the derived values. Rule 8 requires naming the asset whose selection
changes first, and the honest answer is likely that **no shipped asset needs to change**: a `net11.0`
project resolves the `net10.0` asset, and the `net10.0` embedded Core flavour runs on the .NET 11 runtime by
roll-forward. If an asset genuinely does need `net11.0`, the complete list of places a target framework is
declared, in the order they must move together, is: (1)
`Metalama.Framework.CompilerExtensions.Resources.csproj:6` — only if the Core flavour itself moves, which
requires a measurement that no host in the baseline runs a .NET runtime below 11, and which is the
highest-risk edit in the repository because there is exactly one Core flavour and no fallback; (2) the ten
glob path segments in `Metalama.Framework.CompilerExtensions.csproj`; (3) the two `"net10.0"` literals in
`TargetedAssemblyReference.cs:20` and `ExtensionLoaderBase.cs:31`; (4) the per-package
`<TargetFrameworks>` elements, of which `Metalama.Patterns.Wpf` is the one with a real user-visible
consequence; (5) `docs/extensibility.md` throughout; (6) `eng/src/DesignTimeSolution.cs:42`;
(7) `Metalama.Framework/Directory.Build.props:31`.

Version-derived values that must move regardless: `eng/src/Program.cs:26-31`;
`CompileTimeAssemblyLocator.cs:43`; `LanguageVersionProvider.cs:56`;
`Metalama.Framework.Workspaces.csproj:97`; and the re-derivation of `MicrosoftBuildVersion` and the
`*LatestVersion` properties in `Directory.Packages.props`.

**Moving the Roslyn floor or ceiling** follows `docs/updating-roslyn.md` exactly. Raising
`RoslynApiMaxVersion`: `Directory.Packages.props:28,30`; `eng/RoslynVersions/Roslyn.5.10.0.props:5`;
`SupportedCSharpVersions.ToNuGetVersionString:85`; `nuget.base.config:8`; a new
`Syntax-<new>.xml` copied unchanged from the matching `Metalama.Compiler` branch **keeping the experimental
nodes**; `GenerateMetaSyntaxRewriter.cs:18`; then `Build.ps1 prepare`. Raising `RoslynApiMinVersion`:
`Directory.Packages.props:23`; delete the old props file and shim projects and remove them from the
solution; `Metalama.Framework.CompilerExtensions.Resources.csproj:25-26`; `RoslynVariantPolicy.cs:22,32-53`;
the four `SupportedCSharpVersions` switches; `Directory.Build.props:16`; every preprocessor symbol now
defined by all remaining variants or by none, together with its `#if` sites, its test directives and its
`metalamaTests.json` entries; the tables in `Directory.Packages.md` and `platform-support.md`; and the same
steps mirrored in `Metalama.Premium`.

**Stale rationales that will mislead the next change.** None is a defect on its own; each is a comment that
no longer describes the code, in a place the next reader will treat as authority:
`Directory.Packages.props:15,65,84-85,168` (all reasoning from Visual Studio 2022 17.14 or the .NET 8 SDK);
`Metalama.Testing.AspectTesting.targets:53-54` (defaults `ThisRoslynVersionNoPreview` to `5.0.0` and calls
it "the latest version of Roslyn"); `eng/docker/build.Dockerfile:44` (installs .NET SDK 8.0.417, which
`Program.cs` no longer requests); `eng/docker/vs17.Dockerfile:33-36` with `Program.cs:34-46,54` installing
and pinning Visual Studio 17.14.15 while `build/Metalama.Framework.props:37` declares
`MinimumVisualStudioVersion` 18.0, so continuous integration tests on an MSBuild the product warns about
with LAMA0602; and the leftover `.generated/4.12.0/` tree with ten empty `*.4.12.0` project directories.

### 2.10 BACK — Metalama.Backstage

Scope: `Metalama.Backstage/src/**`, plus the one consumer of this subsystem that lives in the premium
repository (`Metalama.Licensing.BuildTasks`).

**Backstage is the only Metalama subsystem that references no Roslyn assembly at all.** It contains no
syntax node, no `SyntaxKind`, no `LanguageVersion`, no symbol, and no code that enumerates the shape of the
C# language. Verified: a grep for `Microsoft.CodeAnalysis` over `Metalama.Backstage/src/**/*.cs` returns one
hit, a string literal in a test; a grep for `SyntaxKind`, `LanguageVersion` or `CSharp14` returns zero;
there is no Roslyn `PackageReference` in any project; and the subsystem does not participate in
`eng/RoslynVersions/`. The C# 14 wave produced **zero commits** here: for each of #1034, #1035, #1036,
#1094, #1105, #1108 through #1116, #1127, #1131, #1143, #1159 and #1160,
`git log --all --oneline --grep=<issue> -- Metalama.Backstage` returns nothing.

The only places Roslyn is named at all are the *process-kind* vocabulary
(`Diagnostics/ProcessKind.cs:30`, `Utilities/ProcessUtilities.cs:46-49`) and the telemetry redaction filter
(`Telemetry/ExceptionSensitiveDataHelper.cs:29`). The nearest thing to a language enumeration is the licence
key field vocabulary, which is a private binary format.

**What Backstage is exposed to is the other half of the wave**: the .NET 11 runtime, the .NET 11 SDK, the
raised `AnalysisLevel` that follows the target framework, and the removal of finite-field DSA on macOS,
which is issue #1860 and is the single largest item in this subsystem. `LangVersion` is `latest` for every
project here, inherited from the PostSharp.Engineering SDK, so **C# 15 syntax silently becomes legal in this
codebase the moment the .NET 11 SDK is installed, with no gate and no opt-in.**

Because `RollForward=Major` is set on the Worker, the Desktop tray application and the dotnet tool, a
`net10.0` build of each runs on .NET 11 without change. That is the deliberate design and it is why .NET 11
does not force a target-framework bump on these three.

The complete `#if` list: `Licensing/Licenses/CryptographyHelper.cs:20` (`NET472 || NET5_0_OR_GREATER`);
`StringExtensions.cs:5,14`; `Diagnostics/ProfilingService.cs:5,14,26,34,46,56,80,91`;
`UserInterface/WindowsUserDeviceDetectionService.cs:9,151`; `UserInterface/WindowsUserInterfaceService.cs:11,68`;
the `METALAMA_BACKSTAGE` / `HAS_METALAMA_TESTING_HOOKS` markers in `Threading/*.cs` (which mark which *copy*
of the shared source files is being compiled, the other copies living in `Metalama.Framework.CompilerExtensions`
and `Metalama.Framework.DesignTime.Contracts`); and four `DEBUG` sites. There is **no** `#if NET10_0_OR_GREATER`
and no `#if ROSLYN_*` anywhere.

**The pattern that applies is the platform wave, PB-2027.0 / issue #1876**, which had four distinct kinds of
effect here: (1) target-framework strings in project files, one line each in seven Backstage projects plus
the five test projects and the `utilities/` pair (commit `575be8b88a`); (2) a target-framework string in
code — exactly one C# line, `DevBackstageToolsLocator.cs:235`, and the lesson is that a path segment naming
a target framework is the one place a target-framework move reaches source code and is invisible to the
compiler; (3) package references only needed by the old floor (commit `cf2874353f` removed the explicit
`System.Threading.AccessControl` reference from three projects); and (4) **the raised analysis level**,
which is the step most likely to repeat verbatim for .NET 11. The sequence is: bump the target framework,
observe the new default-on rules, add a temporary `NoWarn` in the repository-root `Directory.Build.props`
with a comment naming each rule and the issue, then resolve them in a follow-up commit and remove the
suppression — exactly what commit `69f3dcd2d4` (#1893) did for `IDE0270`, `IDE0074` and `IDE0033`.

**The pattern that applies to the DSA problem** is issue #1861, already landed on this branch in four
commits (`ad0937d4ed`, `8532d10481`, `2de6bfcb2e`, `17e0e8f3c9`). It establishes: test first, and observe
the *attempt* rather than the failure — `ILicensingAuthorityObserver` exists purely so a test can assert
that a code path creates **no** `DSA` object, and its remarks say why: "This observation cannot be replaced
by running the code path on a platform where finite field DSA is unavailable, because the test suite does
not run on such a platform"; make the expensive, platform-dependent object lazy; separate the key from the
provider; and use three providers over one base class, each naming the host it serves. **What #1861 did not
do, and #1864 must**: laziness only defers the failure. A machine on macOS with .NET 11 holding a *signed*
production licence key still reaches `DSA.Create` and still fails.

The extension points for a new signature algorithm are enumerated precisely in the map, and the one that
matters most is `License.cs:99-106`, whose `licenseKeyData.SignatureKeyId is 0 or 1` condition would let a
key signed by a new authority bypass the revocation list entirely.

### 2.11 PAT — Metalama.Patterns

Scope: `Metalama.Patterns/src/**`. Ten shipped packages, of which **only one reads C# syntax**:
`Metalama.Patterns.Observability`, whose dependency analyser is a `CSharpSyntaxWalker` over property getter
bodies. That single walker plus its two helper files is where a new expression form, a new
collection-expression element or a new statement field lands. Everything else reads the Metalama *code
model*, and is sensitive to the sets `DeclarationKind`, `TypeKind`, `SpecialType`, `MethodKind`, `RefKind`
and `Writeability`.

The subsystem ships **no per-Roslyn-version variant**. Two projects reference `Metalama.Framework.Sdk`
privately and therefore compile against a single Roslyn, whichever `Directory.Packages.props` resolves.
There is no `#if ROSLYN_*` anywhere in the subsystem. Any C# 15 handling written against a Roslyn 5.10 API
would fail to compile here unless it is reached reflectively or unless the subsystem grows the variant
mechanism it does not currently have.

`Metalama.Patterns/Directory.Build.props:26` sets `<LangVersion>$(LangMaxVersion)</LangVersion>`, defined
once in `Metalama.Framework/Directory.Build.props:45` as `14.0`. Raising it to `15.0` changes the language
version of **every** project in this subsystem at once, including all the aspect-test target projects. That
is the switch that makes C# 15 syntax reach these aspects.

**How C# 14 was absorbed.** None of the C# 14 issues produced a commit under `Metalama.Patterns/`; the work
landed in `Metalama.Framework` and this subsystem absorbed it downstream in three moves, plus a fourth that
accompanies every Roslyn uptake:

1. **Test the aspects against the new construct, with `.t.cs` baselines.** Six aspect tests were added to
   `Metalama.Patterns.Observability.AspectTests`, all named `FieldKeyword_*` (commit `32c2984143`). Only
   Observability got such tests, because the `field` keyword only changes the shape of a property getter
   body, which is exactly what the Observability dependency analyser reads.
2. **Fix the defect the tests exposed, then re-adopt the baselines.** Issue #1644, three commits in order:
   `16689158ff` (add a failing regression test), `dd2403521d` (update the snapshots), `07a7afffdb` (make the
   test snapshot-only). The comment left in `FieldKeyword_SetterSideEffect.cs:15-18` records what the
   baseline asserts.
3. **Silence the analyser suggestions the new construct raises**, at the narrowest scope that works: the
   repository-root `Directory.Build.props:5-9` disables `IDE0032` and `IDE0031`; a local
   `#pragma warning disable IDE0031` sits at `Templates.cs:115,120`; and
   `ReSharper disable ConvertToAutoPropertyWithInitializer` headers were added to each `FieldKeyword_*`
   test (commit `a7494c78c0`).
4. **Re-adopt the baselines that the Roslyn uptake itself moves.** Commit `32e6150298` updated the Patterns
   baselines for Roslyn 5.10's trivia handling. Expect the same for C# 15: a Roslyn uptake alone, with no
   language change, moves `.t.cs` files in this subsystem.

Test-suite sizes, for calibration: Caching 34, Contracts 61, Immutability 5, Memoization 6, Observability 68,
Wpf 59 test inputs.

Two further observations from the map. First, `NumericRange.cs:382` guards *compile-time* behaviour: the
generic-math contract support added by issue #1543 exists only in the `net10.0` build of
`Metalama.Patterns.Contracts`, and which build of an aspect assembly the pipeline loads is decided by the
referencing project's target framework, so the same source can produce different generated code depending
on the user's target framework, with no diagnostic either way. Second, the `NuGetAuditSuppress` comments in
`Metalama.Patterns.{Observability,Immutability}.csproj:31` name Roslyn 4.12 and the `net8.0` shared
framework, both out of PB-2027.0, so the suppression is now justified by a premise that is false.

For C# 15 specifically, the `closed` modifier is the one feature that creates an *opportunity* rather than a
risk in this subsystem: a closed hierarchy is exactly the condition under which deep immutability can be
*proved* rather than assumed (`ImmutabilityExtensions.cs:88-91`), and it would narrow the fallbacks in
`InpcInstrumentationKindLookup.cs:47-61` and the `LAMA5154` / `LAMA5155` refusals in
`ClassicObservabilityStrategyImpl.cs:961-980`. Nothing breaks if `closed` is ignored.

### 2.12 EXT — extensions, tooling and introspection

Scope: `Metalama.Extensions/src/**` (DependencyInjection, DependencyInjection.ServiceLocator, Metrics,
Multicast), `Metalama.Framework/src/Metalama.Extensions.HtmlWriter/**`,
`Metalama.Framework/src/Metalama.Extensions.DiffEngine/**`, `Metalama.Framework/src/Metalama.Framework.Workspaces/**`,
`Metalama.Framework/src/Metalama.Framework.Introspection/**`, `Metalama.Framework/src/Metalama.Tool/**`,
`Metalama.LinqPad/**`, `Metalama.Framework/src/Metalama.Framework.Analyzers/**`.

The subsystem carries almost no direct dependency on the *grammar* of C#. It depends on three abstractions
instead: `DeclarationKind` and `TypeKind` (used by Multicast and DependencyInjection); Roslyn's `ISymbol`,
`IOperation` and `SyntaxNode` shapes (used by `Metalama.Framework.Analyzers` and by the HTML writer's
member-path computation); and Roslyn's classification-type **strings** (used by the HTML writer).

The exception is `Metalama.Framework.Workspaces`, which is not language-sensitive at all but is the single
most platform-sensitive project in the whole repository: it is the only one that hosts MSBuild, that selects
a .NET SDK at run time, and that references `RoslynMaxVersion` rather than `RoslynApiMinVersion` — so it
already parses the four C# 15 grammar additions today.

**How the C# 14 wave was absorbed here: it was not.** Of the nineteen tracked issues, the commits they
produced touched **zero files** in this subsystem. The subsystem was touched exactly once, and indirectly,
by commit `18f7ed78d0` ("Deprecate DeclarationKind.Operator and DeclarationKind.Finalizer"), which deleted
two arms from `MulticastTargetsHelper.cs` — and only because `[Obsolete(…, error: true)]` turns the use into
a compile error. By contrast the two *additive* changes of the same wave, `88667a5265`
(`TypeKind.Extension` and `TypeKind.Tuple`) and `7df11b077c` (`DeclarationKind.ExtensionBlock`), touched
nothing here and produced no compiler diagnostic, because adding a value to an enumeration does not break a
`switch` that has a `default` or a `_ =>` arm.

`TypeKind.Extension` is handled in about twenty places in `Metalama.Framework.Engine` and
`Metalama.Framework` (`EligibilityExtensions.cs:787,796`, `EligibilityRuleFactory.cs:47,121`,
`IntroduceMemberAdvice.cs:197`, `NamedTypeBuilder.cs:52`, `CompilationElementVisitor.cs:48`,
`TypeVisitor.cs:24`, `ContextualSyntaxGenerator.cs:142,167`, `StructuralDeclarationComparer.cs:693`,
`MetaApi.cs:205`, and others). It is handled in **zero** places in Extensions, Workspaces, Introspection,
LinqPad, Tool or Analyzers.

So the pattern to expect for C# 15 is: (1) the code model gains the new values, and whoever adds them is not
obliged to visit this subsystem, and the compiler will not tell them to; (2) the one thing that would force
an edit is an `[Obsolete(…, error: true)]` on an existing member; (3) otherwise the subsystem keeps
compiling and starts producing quietly incomplete answers — multicast aspects that never reach the
construct, metrics that count it as an opaque node, an HTML member path with an empty segment, a workspace
`Types` collection that omits it, a LINQPad schema that never shows it.

There is one additive C# 14 change that did leave a written trace here, and it is a `TODO` rather than an
implementation: `Metalama.Extensions/src/Metalama.Extensions.Metrics/LinesOfCodeMetricProvider.cs:153`,
`// TODO: Add support for partial properties (C# 13), events and constructors (C# 14).`

**Test coverage, and what it does not cover.** The Multicast aspect tests filter by accessibility, member
kind and name, and **no test uses an extension block, a tuple type or any C# 14 construct**. The
DependencyInjection tests include the single golden HTML baseline `Html/EarlyRequired_Html.cs.html`, which
asserts the exact `cs-` class sequence for one file covering only `using`, `namespace`, `class`, `void`,
`public`, `readonly`, `override`, `dynamic` and `return`; the stylesheet it is compared against, generated
by `Metalama.Testing.AspectTesting/HtmlGenerationTestRunner.cs:24-70`, styles only the `cr-` and `diag-`
classes and never the `cs-` ones. `SchemaTests.SchemaWithoutWorkspace` asserts nothing, and
`SchemaTests.SchemaWithWorkspace` is `[Fact( Skip = "Cannot get MSBuildLocator to work." )]`, so **there is
no automated test anywhere in the repository that loads a project through `MSBuildWorkspace`**, which is the
component with the deepest SDK and MSBuild coupling in the subsystem.

### 2.13 TEST — test infrastructure and suites

Scope: `Metalama.Framework/src/Metalama.Testing.AspectTesting/**`,
`Metalama.Framework/src/Metalama.Testing.UnitTesting/**`, `Metalama.Backstage/src/Metalama.Testing.Hooks/**`,
`Metalama.Framework/src/tests/**`, `Metalama.Framework/docs/testing.md`.

**How a test declares what it needs.** `TestOptions.cs` is the single place where the `// @Directive(arg)`
vocabulary is defined: the recogniser at line 40, `ApplySourceDirectives` at 508, the rule at 525 that every
directive must appear after the first `#if` (which is why every payload wraps its directives in
`#if TEST_OPTIONS … #endif`; `TEST_OPTIONS` is **never defined anywhere in the repository**), the exhaustive
`switch` at 528, and the default arm at 841 which collects unknown directives so that
`BaseTestRunner.RunAsync` (186-191) throws — **a misspelled directive fails loudly**.

The directives that matter for a language or platform wave are `@Skipped` (540), `@TestScenario` (555),
`@RequiredConstant` (609), `@ForbiddenConstant` (614), `@DefinedConstant` (619),
`@DependencyDefinedConstant` (624), `@LanguageVersion` (681), `@DependencyLanguageVersion` (702),
`@LanguageFeature` (723) and `@TargetFrameworks` (835).

`metalamaTests.json` extends `TestOptions` with `Exclude` and `IsRoot`; `TestOptions.ApplyBaseOptions`
(407-501) merges field by field, and note that at 452-458 the four constant lists are **added**, never
replaced, so a directory-wide `RequiredConstants` is inherited by every test below it, while
`TargetFrameworks` (500) is `??=`, so a file-level directive wins. There are 25 `metalamaTests.json` files
and **none under `Tests/Aspects/CSharp1x`**.

**The two `MemberDeclarationSyntax` kind switches are the primary hotspot.** `TestSyntaxTree.cs:187-231`
(the document-root check) and `TestResult.cs:518-585` (the `// <target>` consolidation) each enumerate the
same 22 member-declaration kinds by hand and throw on anything else. `SyntaxKind.ExtensionBlockDeclaration`
is absent from both; the C# 14 suite got away with it because the `// <target>` marker in every
`Tests/Aspects/CSharp14/ExtensionMembers/*.cs` sits on the enclosing `static class`, not on the
`extension(...)` block. `SyntaxKind.UnionDeclaration` will hit the same `default` arms, and the target
*selection* at `TestResult.cs:490-499` is kind-agnostic, so a new declaration kind is **found and then
rejected**.

**How C# 14 was absorbed: the framework barely moved, the suites did.** `git log --grep` over the nineteen
issue numbers restricted to `Metalama.Testing.AspectTesting`, `Metalama.Testing.UnitTesting`,
`Metalama.Testing.Hooks` and `docs/testing.md` returns exactly **one** commit, `b4da958605`, whose only
framework change was one line in `SyntaxTreeStructureVerifier.cs` filtering out predefined syntax trees.

The commit that raised `SupportedCSharpVersions.Latest` to C# 14, `2c8c1c8189`, shows exactly what a
language bump costs inside this subsystem: two checked-in expected files that render
`SupportedCSharpVersions.All` verbatim (`Tests/Aspects/Misc/LanguageVersion.t.cs` and
`Tests/Aspects/LanguageVersion/LanguageVersionPreview.t.cs`); one expected file changed because the code
model changed shape (`BackingFieldAdvice_Error.t.cs`, `'TargetClass.<AutoProperty>k__BackingField'` becoming
`'TargetClass.AutoProperty.field'`); one test split per Roslyn variant using the constant idiom; two analyzer
suppressions added to `eng/style/AspectTests.editorconfig` (`SA1402` and `IDE0025`, the latter being "use
expression body for property", which the `field` keyword makes fire); and adjustments to three
`Metalama.Framework.Package/build/RoslynVersion/Roslyn.*.props` files.

**The `Tests/Aspects/CSharp14/**` suite** is 61 input files in six per-feature subdirectories
(`CompoundAssignmentOperator` 3, `ExtensionMembers` 17, `FieldKeyword` 20, `NullConditionalAssignment` 5,
`PartialConstructor` 9, `PartialEvent` 3, `SimpleLambdaModifier` 1). It started flat in January 2026, was
reorganised into per-feature subdirectories by commit `b789173193`, and grew regression tests later.
Twenty files carry only `@RequiredConstant(NET8_0_OR_GREATER)`, three carry `@TestScenario(DesignTime)`, one
carries `@IncludeAllSeverities`, one carries `@FormatOutput`, and **no file carries `@LanguageVersion`** —
the language version comes from `SupportedCSharpVersions.DefaultParseOptions`. The older suites match:
`CSharp11` 12 files, `CSharp12` 18, `CSharp13` 17. Since the matrix is now `net48;net10.0`, every
`NET5_0_OR_GREATER` through `NET9_0_OR_GREATER` constant means the same thing: "not `net48`".

Snapshot files that accompany a C# 14 test: `X.t.cs` for every test (mandatory, asserted by the loop at
`BaseTestRunner.cs:843-861`); `X.0.i.cs` and `X.1.i.cs` for a `@TestScenario(DesignTime)` test; `X.t.txt`
where the transformed program is executed; and `X.Dependency.cs` for a cross-project test.

The standalone scenario the wave produced, `Standalone/TemplateLanguageVersion14/`, has a `README.md` that
is the model to copy: it states what the scenario asserts, why the assertion is "the scenario builds and
runs cleanly" with no `test.json`, why the project sets its own `LangVersion` so that the source language
version is not what the scenario measures, and that the value it guards is bounded by
`RoslynApiMinVersion` because a template is compiled by the Roslyn of the host.

A `CSharp15` suite would be the direct extrapolation: `Tests/Aspects/CSharp15/{Union,UnsafeExpression,
WithElement,LabeledBreakContinue,ClosedModifier,ExtensionIndexer}/`, each test one `X.cs` with at most
`@RequiredConstant(NET10_0_OR_GREATER)` and `@TestScenario(DesignTime)` where applicable, a `// <target>`
marker, and a committed `X.t.cs` produced by running the test and accepting `obj/transformed/<tfm>/…`.
**No `@LanguageVersion(15)`**, because that directive silently skips on a Roslyn that does not parse `15`
and because the suite is meant to run at the project default.

### 2.14 PREM — Metalama.Premium

Scope: the whole of `Metalama.Premium/src/**`, plus that repository's `Directory.Packages.props`,
`Directory.Build.props`, `Directory.Build.targets` and `eng/**`. Repository state when the map was made:
branch `topic/2027.0/1829-durable-and-immutable-contracts`, head `7d5ce94`. `MainVersion` is
`2027.0.0-preview` and `global.json` pins the .NET 10.0.102 SDK, but the target frameworks, the Roslyn
variant set and `RoslynMaxVersion` are still those of 2026.1: the repository has **not yet** absorbed
PB-2027.0.

Four headline points:

1. Premium contains almost no syntax-level enumeration of C# constructs. The one genuine syntax rewriter
   that enumerates declaration forms is `ChangeVisibilityCodeAction.Rewriter`.
2. Its language sensitivity is concentrated in three *enum* switches over `Metalama.Framework.Code`
   enumerations: `DeclarationKind`, `Accessibility`, `MethodKind`. Two throw on an unknown value; one
   silently does nothing.
3. Its platform sensitivity is concentrated in six MSBuild files repeating the literal strings `net8.0`,
   `net472`, `4.12.0` and `5.0.0`, matched by **exact string and exact `Version` equality** in the core
   loader. A mismatch produces no diagnostic at all, only a trace log.
4. Issue #1913 is therefore mostly a mechanical rename plus one dangerous invariant: the literal `net8.0`
   in Premium's `MetalamaExtensionAssembly` items must become `net10.0` in the *same commit* in which the
   core repository's `TargetedAssemblyReference._targetFramework` is `net10.0`, otherwise every premium
   feature stops working with no error.

A latent defect worth recording: `Metalama.Premium/eng/RoslynVersions/Roslyn.5.0.0.props:3` reads
`$(RoslynApiMaxVersion)`, and that property is **not defined anywhere in that repository**. Nothing breaks
today because every consumer reads `ThisRoslynVersionNoPreview` or `ThisRoslynVersionProjectSuffix` instead,
but the property is dead and misleading.

No production `.cs` file in Premium contains a `#if ROSLYN_*` guard; the variants differ only by the Roslyn
assemblies they bind to. That is the same discipline the core repository adopted for its own 5.0 and 5.10
pair.

**How the C# 14 wave was absorbed here: three traces, none of them a commit naming the issues.**

1. **A deliberate opt-out for compile-time code.** `Directory.Build.props:19-20` pins
   `MetalamaTemplateLanguageVersion` to `13.0` with an explicit rationale ("must be compatible with
   VS 2022"). Premium's templates and build-time code stayed on C# 13 for the whole C# 14 wave. This is the
   *pattern* for a wave: the core moves, and Premium declares a lower ceiling until the platform baseline
   allows it to follow.
2. **Adoption of `field` in run-time library code only.** The `field` contextual keyword is used at five
   sites in the Redis caching backend, all in ordinary libraries compiled by the .NET SDK at its default
   language version, never by the Metalama template compiler, so the ceiling of point 1 does not apply.
3. **Enum members consumed without a corresponding switch update.** `DeclarationKind.ExtensionBlock` and
   `TypeKind.Extension` were added in the core; no switch in Premium was extended.
   `ReferenceValidationContext.GetInboundGranularity` still throws on `DeclarationKind.ExtensionBlock`.
   This is the wave's unfinished business, and it is a warning for C# 15: additions to core enumerations do
   not announce themselves at the Premium build.

**The Roslyn-variant wave is the pattern that #1913 will follow**, defined by two commits. `c9244ce`
(pull request #39, 2025-11-18) **introduced** the whole variant mechanism in 38 files: it created the four
`eng/RoslynVersions/*.props`, the six shim projects, `Metalama.Premium.LatestRoslyn.slnf` and both
`MetalamaExtensionAssemblies.props` files (deleting `ProjectReferenceSupplements.props`), and rewrote the
two `build/*.props` and both `Package.csproj` copy lists. `77e53e9` (2026-04-27, 23 files) **removed** a
variant, and its own summary states the recipe: drop the variant props file and the implementation package
reference, remove the always-true `ROSLYN_*_OR_GREATER` guards, and touch — in order —
`Directory.Packages.props`, the solution (63 lines removed), the remaining props files, the shim projects,
both `Engine.csproj` files, both `Package.Resources.csproj` files, both `Package.csproj` files, both
`build/*.props` files, both `Redist` `.csproj` files, both `MetalamaExtensionAssemblies.props` files,
`Metalama.Licensing.BuildTasks.csproj`, and the test file `AllReferences.cs`. The acceptance criterion
recorded there was "dotnet restore + Build.ps1 build are warning-free across all top-level configurations".

Note the naming inversion #1913 causes. Today `…Engine.5.0.0.dll` *is* the latest variant. After the change,
`…Engine.5.0.0.dll` is the Rider variant and `…Engine.5.10.0.dll` is the latest. Any file that mentions
`5.0.0` must be read to decide which of the two it now means; **a blind rename is wrong.**

**The shape of the test evidence for a language wave** is
`src/tests/Metalama.Extensions.Validation.AspectTests/AllReferences.cs` (248 lines), whose aspect reports,
for every inbound reference, the tuple `(ReferenceKinds, referencing DeclarationKind, referencing
declaration, referenced DeclarationKind, referenced declaration, SyntaxKind)`, with the expected output in
`AllReferences.t.cs` (62 lines). It currently covers explicit interface implementation, attributes on every
target, field types, `typeof`, constructors and `base(...)`, `override` and `base.` calls, parameters and
return types, target-typed `new()`, field reads, assignment and compound assignment, `nameof`, event
invocation and `+=`/`-=`, array creation and collection expressions, casts and `as`, `is` patterns,
automatic properties, overridden accessors, field-like and explicit events, locals, generic type and method
arguments, derived generic types, a positional record, and primary-constructor classes and structs.
Absent, and therefore the shopping list for C# 15: an extension block, an extension-block indexer, a `union`
declaration, an `unsafe(expr)` expression, a `with(...)` collection element, a labelled `break`/`continue`,
and a `closed` type.

Finally, `ReferenceKinds` declares `All = -1` (`Metalama.Framework/Code/ReferenceKinds.cs:23`), so a newly
added flag is automatically included in every `ReferenceKinds.All` default. That is additive-safe, and it is
why the C# 14 wave needed no change in the twenty-odd `ReferenceKinds referenceKinds = ReferenceKinds.All`
parameter defaults across `ArchitectureExtensions.cs`. `TransitiveValidatorInstance` likewise serialises the
value as an integer, so the wire format is additive-safe.

---

## 3. How a new language construct propagates

Every trace below starts at the grammar and ends at the tests. The first two steps are common to all six
and are stated once here.

**Step 0, common to every construct that adds or changes a grammar element.**
`eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml` already declares the element, carrying
`ExperimentalUrl`. `eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs:70-78` and `:90-109` delete every
such element before `VersionDetector` runs, so the element does not exist as far as the generator is
concerned. Nothing downstream can be reached until either the refreshed snapshot no longer carries the
attribute, or the filter is changed deliberately. `eng/src/GenerateMetaSyntaxRewriter/GenerateMetaSyntaxRewriter.cs:17-18`
must list the grammar version, and `Build.ps1 prepare` (`eng/src/Program.cs:250`) must be re-run.

**Step 0b, common to every construct that must be *gated* rather than merely parsed.** The verifier's
comparison is `version.ToLanguageVersion()` against `MaximalAcceptableLanguageVersion`
(`Templating/RoslynVersionSyntaxVerifier.cs:41-52`), and
`Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:52-62` maps **both** `V5_0_0` and `V5_10_0`
to `AllLanguageVersions.CSharp14`. Until that map is corrected, a `V5_10_0`-pinned node compares as C# 14
and is accepted in a project whose `LangVersion` is 14, so the whole version-checking mechanism silently
passes. The five version tables that move together are
`Utilities/AllLanguageVersions.cs:14-18`, `SupportedCSharpVersions.Latest` (31),
`SupportedCSharpVersions.All` (38-43), `SupportedCSharpVersions.ToLanguageVersion` (52-62),
`SupportedCSharpVersions.GetMaxLanguageVersion` (149-159) and
`Utilities/LanguageVersionProvider.cs:54-60`; plus the display string at
`Utilities/Roslyn/LanguageVersionExtensions.cs:34`, without which diagnostic formatting throws.

### 3.1 A new kind of type declaration (`union`, `UnionDeclarationSyntax : TypeDeclarationSyntax`)

This is the most expensive of the six, because it touches every kind taxonomy in the repository.

**Generated layer (no hand edit, once step 0 passes).** `MetaSyntaxRewriter.g.cs` gains
`VisitUnionDeclaration` and `TransformUnionDeclaration`; `MetaSyntaxFactoryImpl` gains a
`UnionDeclaration(...)` factory; `RoslynVersionSyntaxVerifier.g.cs` gains
`VisitUnionDeclaration( … ) => VisitVersionSpecificNode( node, RoslynApiVersion.V5_10_0 )`; both hashers
gain a `VisitUnionDeclaration`; `SyntaxNodePartialUpdateExtensions.g.cs` gains a `PartialUpdate` overload.
Note two grammar peculiarities to check when the minimal factory overload is generated:
`UnionDeclarationSyntax` carries `SkipConvenienceFactories="true"` (`Syntax-5.10.0.xml:1954`) and its
`OpenBraceToken` and `CloseBraceToken` are `Optional="true"` (1968, 1972), which is unusual; the relevant
generator helpers are `Generator.IsAutoCreatableToken` (156-162) and `IsRequiredFactoryField` (186-189).

**Public code model (CM-PUB).**
1. `Code/TypeKind.cs` — append a `Union` member after `Tuple` (88). Do not reorder.
2. Decide whether a union is an `INamedType` or a new interface. The precedent from C# 14 and from #1138 is
   a new interface derived from `INamedType`, exactly as `IExtensionBlock.cs:11` and `ITupleType.cs:27` do,
   living beside them in `Code/`, exposing its case members as a typed collection and declaring
   `new IRef<IUnionType> ToRef();`.
3. `Code/DeclarationKind.cs` — only if the new type is *not* reachable as `DeclarationKind.NamedType`. The
   precedent is split: `ITupleType` reuses `NamedType`, `IExtensionBlock` got its own member. Adding a
   member costs the switch edits at `DeclarationExtensions.cs:53-93,105-143,220-228`,
   `GenericExtensions.cs:42-50,56-62,299-334`, plus roughly 35 files in the engine (the measured cost of
   commit `7df11b077c`).
4. `Code/Collections/` — a new `I<X>Collection` if the type owns a new kind of child, plus the paired
   `X` / `AllX` properties on `INamedType.cs` near lines 80-187.
5. `Code/DeclarationBuilders/` — a new `I<X>Builder : INamedTypeBuilder, I<X>`, following
   `IExtensionBlockBuilder.cs:77-78` verbatim including `[InternalImplement]`.
6. `Code/IDeclarationFactory.cs` and `Code/TypeFactory.cs` — a creation method if the type can be
   synthesised, following `CreateTupleType` (`IDeclarationFactory.cs:98-120`, `TypeFactory.cs:137-157`).
7. `Code/NamedTypeExtensions.cs:72,110` and `Code/DeclarationExtensions.cs:334` if the new type owns members.

**Code model implementation (CM-ENG).** Follow the thirteen-step list in §2.2 verbatim. The decisions that
are not mechanical: whether the union gets its own `TypeKind` value or is `TypeKind.Class` plus a flag (the
record precedent argues for the flag and costs zero switch arms); which side of
`Utilities/Roslyn/TypeKindExtensions.cs:22-24` `IsNamedType` it lands on, decided once and propagating
everywhere; and `CodeModel/Source/SourceNamedTypeImpl.cs:69-79`, which throws for any Roslyn `TypeKind`
outside Class, Delegate, Enum, Interface, Struct and Error. Further required edits:
`Introductions/Builders/NamedTypeBuilder.cs:52` (which kinds an aspect may introduce);
`Visitors/TypeSymbolRewriter.cs:43`; `SerializableIds/SerializableTypeIdGenerator.cs:182-183`;
`Comparers/TypeOrderingComparer.cs:55` (already permissive for a named-type-like kind, and an
`InvalidCastException` for anything else); and, as prerequisites because they are already wrong for
extension blocks, `Utilities/Roslyn/SyntaxKindExtensions.cs:33-35` and
`CodeModel/Helpers/DependencyAnalysisHelper.FindDeclaredTypesVisitor.cs:35-57`.

**Compile time (CT).** `Utilities/Roslyn/SyntaxKindExtensions.cs:33-41` — decide whether `IsTypeDeclaration`
and `IsBaseTypeDeclaration` include `SyntaxKind.UnionDeclaration`; both
`ProduceCompileTimeCodeRewriter.cs:1460` and `:1530` and `FindCompileTimeCodeVisitor.cs:77,81` follow from
that one decision. Then `FindCompileTimeCodeVisitor.cs:89-99` — add `VisitUnionDeclaration`, otherwise a
file whose only compile-time type is a union is classified as containing no compile-time code and is
excluded from the compile-time compilation with no diagnostic. Then
`ProduceCompileTimeCodeRewriter.cs:204-210` (a `VisitUnionDeclaration` delegating to `VisitTypeDeclaration`),
`:540-541` (the nested-type case), `:356-357` or `:274` (depending on whether a compile-time union nested in
a run-time type must be un-nested), and a review of `TransformCompileTimeType` (449). Also
`CollectSerializableTypesVisitor.cs:64-76` and `CollectSerializableFieldsVisitor.cs:101-121` for a
serializable compile-time union; `SymbolClassifier.cs:1129,1210`; `CompileTimeTypeResolver.cs:70-130`.

**Templating (TMPL).** `TemplateAnnotator` — add
`public override SyntaxNode VisitUnionDeclaration( UnionDeclarationSyntax node ) => this.VisitTypeDeclaration( node, n => base.VisitUnionDeclaration( n ) );`
next to lines 743-754. Without it, `VisitTypeDeclaration`'s early exit for run-time types (765-774) never
runs, and the union is annotated `RunTimeOrCompileTime` by
`AddScopeAnnotationToVisitedNode`, so its run-time members are analysed as though they might be
compile-time. `TemplatingCodeValidator.Visitor` — add `VisitUnionDeclaration` next to 299-330, calling
`WithDeclaration( node )` and `VerifyTypeDeclaration( node, context )`; without it `_currentScope` is never
established for the union's body and `VisitCore` returns at 134-137 without checking a single reference.

**Advising (ADV).** In order: `Code/TypeKind.cs`; `SourceNamedTypeImpl.cs:69-79`;
`Metalama.Framework/Eligibility/EligibilityRuleFactory.cs:47,89-92,121,141,170-172` (the four hard-coded
`TypeKind` allow-lists); `Metalama.Framework/Advising/IAdviceFactory.cs` near 1015-1046 (a new
`IntroduceXxx` matching `IntroduceClass`); `Metalama.Framework/Aspects/AdviserExtensions.cs` near 1702-1740
(the public forwarder); `AdviceFactory.cs` near 2050-2093 (the implementation, the
`ValidateNotExtensionBlock` gate, and the `TypeKind` literal passed to `IntroduceNamedTypeAdvice`);
`AdviceImpl/Introduction/IntroduceNamedTypeTransformation.cs:62-92` (the syntax-factory arm);
`IntroduceNamedTypeAdvice.cs:108` (`IntroduceImplicitConstructorIfNeeded`);
`CodeModel/Helpers/ModifierHelper.cs:205-240` (the named-type modifier path, including the
`namedType.IsAbstract && namedType.TypeKind != TypeKind.Interface` test at 224);
`Metalama.Framework.Engine/Diagnostics/Ranges.md` if new errors are needed; and a new test folder. If the
new declaration can also **contain** members, add one `ValidateNotXxx` gate per `Introduce*` method it does
not admit, mirroring the eleven `ValidateNotExtensionBlock` call sites, plus an error aspect test per gate.
This is the first C# 15 change that would need a production `#if ROSLYN_5_10_0_OR_GREATER` in `AdviceImpl`.

**Linker (LINK).** In pipeline order: `LinkerInjectionStep.Rewriter.cs` — add `VisitUnionDeclaration` next
to 324; `LinkerLinkingStep.LinkingRewriter.cs` — add it next to 79;
`LinkerRewritingDriver.Types.cs` — add `RewriteUnion` if unions can carry a primary constructor (the grammar
says they can, `Syntax-5.10.0.xml:1965`); `LinkerInjectionStep.Rewriter.cs:639` — add
`SyntaxKind.UnionDeclaration` to the type-declaration arm of the injected-node switch;
`Linking/SymbolExtensions.cs:29-31` — add it to `GetDeclarationFlags`;
`LinkerLateTransformationRegistry.cs:147-150` and `:189-191` — add it to the primary-constructor predicates,
otherwise `.Single()` throws; `Utilities/Roslyn/SyntaxExtensions.cs:116-118` — add it to `GetDeclaringType`,
otherwise `LexicalScopeFactory` computes the wrong scope;
`LinkerInjectionStep.LinkerInjectedMemberComparer.cs:21-30` — a `DeclarationKind` entry;
`LinkerAnalysisStep.SemanticBodyAnalyzer.cs:244,418` and `LinkerSyntaxHandler.cs:104-109` if unions get
compiler-synthesized members the way records do; and a `LinkerUnionHelper` equivalent of
`LinkerRecordHelper.cs` if they have synthesized `Equals`, `GetHashCode` or `Deconstruct`.

**Syntax generation (SYNGEN).** `Formatting/TextSpanClassifier.cs` — add
`VisitUnionDeclaration` beside 115, reusing `VisitTypeDeclaration<T>` (77), which already covers every field
because `UnionDeclarationSyntax : TypeDeclarationSyntax` declares all of them as `Override="true"`.
`SyntaxGeneration/ContextualSyntaxGenerator.cs:793-816` — add
`SyntaxKind.UnionDeclaration => ((UnionDeclarationSyntax) oldNode).AddAttributeLists( attributeList )`,
without which `AddAttribute` throws at 815. `ContextualSyntaxGenerator.cs:142` and `:167` — add
`TypeKind.Union` to the two `TypeKind` lists, exactly as `Extension` was added.
`CodeModel/Visitors/TypeVisitor.cs:16-27,39` — a case plus a `protected virtual T VisitUnion( … ) =>
this.VisitNamedType( … )`, following the `VisitExtensionBlock` precedent; **this one edit keeps both
`SyntaxGeneratorForIType` visitors working unchanged.** All the `SyntaxGeneration` and `Formatting` edits
must be guarded by `#if ROSLYN_5_10_0_OR_GREATER`, because every file of that subsystem is compiled once per
Roslyn variant.

**Design time (DT).** `Pipeline/Diff/PartialTypesVisitor.cs:38-42` and `Pipeline/Diff/PartialTypesHasher.cs:43-47`
— both, or a `partial union` is never registered as a partial type and the fast path never reports a change
in one. `Refactoring/CSharpAttributeHelper.cs:74-191` — an arm, or "Add aspect" produces no edit.
`Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs:720-789` `CreatePartialType` — an arm, or
`AssertionFailedException` at 788, contained by the per-group `catch` at 90-105 and surfaced as LAMA0049;
`:510-511` — the `partial` modifier injection; `:817-823` — the generated-file header; `:115` — the
`DeclarationKind` admission, which throws at 126 if missed. `CodeFixes/TheCodeFixProvider.cs:187-193` needs
**no** change: it matches `BaseTypeDeclarationSyntax`.

**Test infrastructure (TEST).** `Metalama.Testing.AspectTesting/TestSyntaxTree.cs:200-221` and
`TestResult.cs:548-569` — add `SyntaxKind.UnionDeclaration` (and, while there,
`ExtensionBlockDeclaration`), or a test that marks a union with `// <target>` fails with
`InvalidOperationException: Don't know how to add a UnionDeclaration to the compilation unit.`
`Metalama.Framework.Tests.LinkerTests/Runner/LinkerTestInputBuilder.TestTypeRewriter.cs:49-89` — a
`VisitUnionDeclaration` calling `RewriteTypeDeclaration`, or the union never pushes onto `_currentTypeStack`;
`LinkerTestInputBuilder.TestRewriter.cs:112-152` — the matching override; and the three closed member
switches at 341-350, 555-568 and 641-696 if a union member may be introduced or overridden. Then the
payloads under `Tests/Aspects/CSharp15/Union/` with committed `.t.cs` and, for design-time scenarios,
`.i.cs`. No change is needed in the discoverer, the directive parser, `TestOptions`, the runners or the HTML
writer.

**Extensions (EXT).** `Metalama.Extensions.Multicast/MulticastTargets.cs:28-123` — a `Union` flag plus
updates to `AnyType` and `All`, a public API change, without which no multicast aspect can name a union as a
target; `MulticastTargetsHelper.cs:20-39` — a `case TypeKind.Union`, without which it returns
`MulticastTargets.Default` (0); `MulticastImplementation.cs:170-178` — an arm, without which `_ => false`
means a union is silently never a target; `MulticastImplementation.cs:193-200,222-236` — the
`AcceptClassOrStruct` eligibility rule and its message "is neither a class, struct or record".
`Metalama.Framework.Analyzers/{ImmutabilityContext.cs:250-450,DurabilityContext.cs:209-400}` — whether a
union's cases are reported as fields decides what the closing rules answer.
`Metalama.Framework.Workspaces/{ICompilationSet.cs:34,CompilationSet.cs:26,Project.cs:85}` — only if unions
are not `INamedType`, in which case a union is invisible to Workspaces, Introspection and LINQPad exactly as
an extension block is today. `Metalama.Extensions.HtmlWriter/HtmlCodeWriter.cs:213` needs **no** change:
`UnionDeclarationSyntax` derives from `BaseTypeDeclarationSyntax` and has an `Identifier` field.

**Patterns (PAT).** `Metalama.Patterns.Observability/Implementation/DependencyAnalysis/RoslynExtensions.cs:41-46`
— `GetEffectiveAccessibility` throws `NotSupportedException` for any `TypeKind` outside
`{Class, Struct, Interface, Enum}`, so a union containing a private field reached through a property chain
crashes the aspect (loudly, which is correct, but it crashes);
`Metalama.Patterns.Observability/ObservableAttribute.cs:52` — the `x.TypeKind is TypeKind.Class` eligibility
rule and its message; `Metalama.Patterns.Immutability/ImmutabilityExtensions.cs:40-93` — a union falls to
line 93 and is `ImmutabilityKind.None` silently;
`Metalama.Patterns.Contracts/CompileTimeHelpers.cs:33`;
`Metalama.Patterns.Observability/Implementation/InpcInstrumentationKindLookup.cs:26-84`;
`Metalama.Patterns.Contracts/ContractExtensions.cs:31-52`; and the four aspects that introduce members into
the target type must decide whether a union can receive them
(`DependencyPropertyAspectBuilder.cs:76-107`, `CacheAttribute.cs:108-136`, `MemoizeAttribute.cs:60-95`,
`ClassicObservabilityStrategyImpl.cs:225-421`).

**Premium (PREM).** `ChangeVisibilityCodeAction.cs` — a `VisitUnionDeclaration` beside the
class/record/struct trio at 72-79, guarded by a preprocessor symbol that repository does not define today;
`ReferenceValidationContext.cs:124` if the core gives a union a `DeclarationKind` other than `NamedType`;
`ReferenceEnd.cs:119,125` which cast to `INamedType`; `InternalOnlyImplementAttribute.cs:110` if a union may
be implemented; `ArchitectureExtensions.cs:152-174` and `InternalsUsageValidationAttribute.cs:144-152` if a
union carries members reachable by `Members()`; and `AllReferences.cs` plus its `.t.cs`.

### 3.2 A new modifier (`closed`, no new grammar node)

`closed` adds no syntax node. Modifiers are a `SyntaxList<SyntaxToken>` and
`MetaSyntaxRewriter.Transform( SyntaxToken )` (`MetaSyntaxRewriter.cs:239-294`) handles any kind
generically, so **the generator needs no change at all** and both code hashers hash the new token by its
`RawKind` through `this.Visit( node.Modifiers )` (`Generator.cs:684`), which correctly invalidates the
design-time cache. Three things are nevertheless worth checking in the generator: `Transform( SyntaxToken )`
lines 269-275 take the one-argument `Token( token.Kind() )` path for a keyword whose `Value` round-trips,
which is correct for a contextual keyword; `Generator.IsKeyword` (301-389) is the generator's escaping list
for *parameter names* and matters only if a grammar field were named `Closed`; and symbol-level consequences
belong to the code model.

**Public code model (CM-PUB).** Two lines: `bool IsClosed { get; }` on `Code/INamedType.cs` beside
`IsRecord` (202), and `new bool IsClosed { get; set; }` on `Code/DeclarationBuilders/INamedTypeBuilder.cs`
beside `IsPartial` (18); the commented-out `IsReadOnly` / `IsRef` block at 22-30 shows the intended shape.
Nothing else: no switch in that subsystem enumerates modifiers, they are independent booleans. This is the
cheapest of the six changes, and the precedent is `IMemberOrNamedType.IsPartial` (101) plus
`IMemberOrNamedTypeBuilder.IsPartial` (141) plus `INamedTypeBuilder.IsPartial` (18).

**Code model implementation (CM-ENG).** `CodeModel/Helpers/ModifierCategories.cs:12-23` — a new flag bit
**and** an edit to `All`. `CodeModel/Helpers/ModifierHelper.cs:198-236` `GetTypeSyntaxModifierList` — a new
`if`, if it is a type modifier; the existing `unsafe` handling at 178 is the model for a modifier read back
from syntax rather than from a symbol property
(`member.GetSymbol() is { } symbol && symbol.HasModifier( SyntaxKind.UnsafeKeyword ) == true`). If the
modifier is observable as a symbol property, the corresponding members on `SourceNamedTypeImpl.cs` (compare
`IsReadOnly` 169, `IsRef` 171, `IsRecord` 173, `IsPartial` 329), `NamedTypeBuilder.cs`,
`NamedTypeBuilderData.cs:31-35` (which currently hard-codes `IsReadOnly => false` and `IsRef => false`) and
`IntroducedNamedType.cs`. If it is *not* surfaced as a symbol property, the read goes through syntax and
lands in `SourceNamedTypeImpl.IsPartial`'s shape (329-352), whose `_ => default` is a silent false.
`Utilities/Roslyn/SymbolModifiersHelper.cs:16-41` is the `ISymbol` twin and must be edited together with
`ModifierHelper`; both are already noted as needing unification.

**Advising (ADV).** `CodeModel/Introductions/Builders/MemberBuilder.cs` or `NamedTypeBuilder.cs` — a settable
`IsXxx`; `AdviceImpl/Introduction/IntroduceMemberAdvice.cs:91-134` — derive the flag from the template and
from `TemplateAttributeProperties`; `:168-244` — a `ValidateBuilder` rule and, if it can conflict, a new
`AdviceDiagnosticDescriptors` entry (the pattern is `CannotIntroducePartialMemberToNonPartialType`, 288);
`AdviceImpl/Introduction/IntroduceMethodTransformation.cs:45` — `hasNoBody`, if the modifier implies a
bodyless member; `Metalama.Framework/Advising/TemplateAttributeProperties.cs` and `ITemplateAttribute`, to
let a template declare it; and **the six `GetSyntaxModifierList( ModifierCategories… )` masks** in
`AdviceImpl/Override/*BaseTransformation.cs`, each of which must decide whether the modifier propagates to
the generated override. That last row is the easiest to miss: the masks are explicit allow-lists, so a new
modifier is silently dropped from every generated override until each is revisited.

**Linker (LINK).** Nothing inside the linker folder, **provided** the linker keeps copying `node.Modifiers`
wholesale, which it does at `LinkerRewritingDriver.Methods.cs:116`, `.Operators.cs:104`,
`.ConversionOperators.cs:102`, `.Destructors.cs:104`, `.EventFields.cs:155,231` and `.Constructors.cs:353`.
The only modifier-aware code is `ModifierCategories`, `ModifierHelper`, `SyntaxExtensions.IsAccessModifierKeyword`
(86, used at `RewriterExtensions.cs:78` and `LinkerRewritingDriver.Properties.cs:519`) and four explicit
filters (`LinkerInjectionStep.Rewriter.cs:766,792,1212`, `AuxiliaryMemberFactory.cs:100`). The risk runs in
the opposite direction: the linker emits *new* members into a `closed` type — overrides, backing fields,
auxiliary contract members — and a `closed` type may reject them at compile time. That is a behaviour
question, not an extension point.

**Compile time (CT).** No new syntax node, but the subsystem **rebuilds** modifier lists, which drops
modifiers it does not name: `ProduceCompileTimeCodeRewriter.cs:328` replaces the **entire** modifier list of
an un-nested compile-time type with `TokenList( Token( SyntaxKind.InternalKeyword )… )`, so a `closed`
modifier on such a type is discarded; `:1248` filters `ReadOnlyKeyword`; `:642` synthesizes
`TokenList( Token( SyntaxKind.PublicKeyword )… )`; `RewriterHelper.cs:140,164,183,222` and
`RunTimeAssemblyRewriter.cs:273` filter `ExternKeyword`. Note also that the generator's version gate does
not cover a modifier at all: `GenerateVersionChecker` emits checks only for version-specific nodes, fields
and field kinds, so **a new contextual modifier is invisible to `RoslynVersionSyntaxVerifier`** and must be
added by hand if templates are to be gated on it.

**Syntax generation (SYNGEN).** `ModifierCategories.cs:12-23` and `ModifierHelper.GetTypeSyntaxModifierList`
(198), with the matching change in `SymbolModifiersHelper.cs`. Inside the subsystem proper,
`ContextualSyntaxGenerator.Parameter` (958) and `SyntaxFactoryEx.TokenWithTrailingSpace` (41) are the only
consumers and neither enumerates modifiers. Separately, if `closed` becomes reachable as an *identifier* in
generated code, `SyntaxFactoryEx.SafeIdentifier` (129-182) will **not** escape it, because
`SyntaxFacts.GetKeywordKind` returns `SyntaxKind.None` for contextual keywords; the C# 14 precedent says the
escape belongs in the context that binds the keyword, not in `SafeIdentifier`.

**Design time (DT).** No design-time file enumerates modifiers except the three `SyntaxKind.PartialKeyword`
tests, which look for `partial` specifically and are unaffected. But
`DesignTimeSyntaxTreeGenerator.CreatePartialType` lines 710-714 build the modifier list of the generated stub
from `type.IsStatic` alone (`static partial` or `partial`), so a modifier that the C# compiler requires to be
repeated on every partial declaration must be added here, and its absence produces a compiler error in the
generated file that the user sees in the editor and cannot fix. `TheCodeFixProvider.cs:173` `AddModifiers`
appends `partial` after the existing modifiers, so modifier ordering needs attention there.

**Extensions (EXT).** `Metalama.Extensions.Multicast/MulticastAttributes.cs:40-199` — a `Closed` and
`NonClosed` pair plus an aggregate, and an update to `All`; `MulticastAttributeInfo.cs:152-156` — a
`DoesClosednessMatch` call; `MulticastAttributeInfo.cs:284-320` — the predicate itself, beside
`DoesAbstractionMatch` and `DoesVirtualityMatch`. Without them the failure is under-filtering: an aspect
that asks for non-closed members gets closed ones too, silently. Also
`HtmlCodeWriter.cs:34-35` if the modifier can appear on an accessor, and
`Metalama.Framework.Analyzers/{ImmutabilityContext.cs:440,SymbolFacts.cs:221}` if `closed` changes what may
implement a type.

**Patterns (PAT), Premium (PREM), Test (TEST).** Nothing breaks. In Patterns the risk is entirely of the
missed-opportunity kind (see §2.11). In Premium, `ChangeVisibilityCodeAction.IsAccessibilityModifier`
(191-199) returns `false` for it and line 181 copies it through, which is correct; the only concern is
`ChangeModifiers` (124), which rebuilds the token list by placing the new accessibility keywords first and
appending the rest, so an ordering constraint relative to the accessibility keywords would produce
syntactically wrong output. In the test harness the only modifier inspection is
`LinkerTestInputBuilder.TestTypeRewriter.cs:501-502`, a positive test for `NewKeyword`, so a new modifier
passes through; what changes is the `.t.cs` baselines wherever the modifier survives the transformation, and
possibly a new suppression in `eng/style/AspectTests.editorconfig` and its three copies.

### 3.3 A new expression form (`unsafe(expr)`, `UnsafeExpressionSyntax : ExpressionSyntax`)

**Generated layer.** Once step 0 passes: `VisitUnsafeExpression` and `TransformUnsafeExpression` in
`MetaSyntaxRewriter.g.cs`, `MetaSyntaxFactoryImpl.UnsafeExpression`, a `VisitUnsafeExpression` pinned to
`V5_10_0` in `RoslynVersionSyntaxVerifier.g.cs`, hasher visits, and a `PartialUpdate` overload. The field
name `Keyword` camel-cases to `keyword`, which is not in `Generator.IsKeyword`, so no escaping is involved.

**Public code model (CM-PUB).** **No change.** `IExpression` is `[InternalImplement]` and describes an
expression only by its `Type`, its `RefKind` and its `Value`; it does not model expression syntax. New
expression forms reach users through `ExpressionFactory.Parse` (`SyntaxBuilders/ExpressionFactory.cs:160`)
and `ExpressionBuilder.AppendVerbatim` (`SyntaxBuilders/SyntaxBuilder.cs:59`), both string-in and
engine-parses. The only thing to check is whether `ISourceExpression.AsTypedConstant` (101) must recognise
the new form, whose closed list is `TypedConstant.CheckAcceptableType` (`TypedConstant.cs:174-`).

**Code model implementation (CM-ENG).** The blast radius is small and concentrated in the syntax-reading
helpers: `Utilities/Roslyn/SyntaxHelpers.cs:103-145` — `field` inside `unsafe(field = x)` would not be seen
by the exhaustive assignment list, which is the exact analogue of what `FieldExpressionSyntax` cost in #1114;
`CodeModel/Helpers/IteratorHelper.FindYieldVisitor.cs:24-43` — it stops descending at `ExpressionSyntax`
(28), so an expression form that can contain statements would hide a `yield`;
`CodeModel/Helpers/DeclarationExtensions.cs:436-457` `HasBody` and `:380-391` `HasExplicitAccessorBody`, both
of which enumerate the syntax forms that carry a body and both of which fall to `false`;
`SerializableIds/SerializableTypeIdResolver.cs:441` `DefaultVisit`, which throws on an unexpected node and is
correct as is unless the new form can appear inside a type syntax.

**Templating (TMPL).** `TemplateAnnotator` — a `VisitUnsafeExpression` override. The minimum viable form is
`this.ReportUnsupportedLanguageFeature( node.Keyword, "unsafe expression" ); return base.VisitUnsafeExpression( node );`,
mirroring `VisitUnsafeStatement` at 2594-2599. Without it, `AddScopeAnnotationToVisitedNode` gives the node
the combined scope of its `ExpressionSyntax` children, so **a construct that the statement form refuses with
LAMA0101 is accepted without a word in expression form.** `TemplateCompilerRewriter` needs nothing if the
annotator refuses the construct, and a `Transform*` override only if compile-time evaluation is wanted.

**Advising (ADV).** Nothing constructs arbitrary user expressions here. Two touch points:
`Advising/TemplateMember.cs:293-310` with `Utilities/Roslyn/SyntaxHelpers.cs:93-140`, if the new form can
appear inside a property accessor and changes what the accessor means, in which case the same "detect in
syntax, record on `CompiledTemplateAttribute`, read back on `TemplateMember`" pattern applies; and
`AdviceImpl/AdviceSyntaxGenerator.cs:172`, which collapses a single-return template to an expression and
would be affected by a new *statement* form rather than an expression form.

**Linker (LINK).** `Inlining/InlinerHelper.cs:99-108` — `UnsafeExpressionSyntax` is a transparent wrapper
and belongs in `SkipParenthesizedExpressionAncestors` if it may enclose an aspect reference; the downward
twins at `Utilities/Roslyn/SyntaxExtensions.cs:92-97,103-111` likewise.
`AspectReferenceResolver.cs:828-864` reads `expression.Parent`, so an intervening `unsafe(...)` node changes
the parent and **silently reclassifies a write as a read**; `:612` uses a fixed four-level
`expression.Parent?.Parent?.Parent?.Parent` chain to find the async-void wrapper. If the new expression form
can *denote a member* the way `field` does, the full #1094 recipe applies: a walker, a collection pass in
`LinkerAnalysisStep`, a `SyntaxNodeSubstitution`, and an arm in
`SubstitutionGenerator.CreateOriginalBodySubstitution` (861-911).

**Syntax generation (SYNGEN).** `ContextualSyntaxGenerator.cs:1034-1061` — `unsafe(x)` is already
parenthesised by its own syntax, so `SyntaxKind.UnsafeExpression => false` belongs beside
`SyntaxKind.CollectionExpression => false` (1052), which is precisely where the stripped
`#if ROSLYN_4_8_0_OR_GREATER` used to sit; omitting it costs a redundant pair of parentheses and nothing
breaks. `Formatting/CodeFormatter.CustomSimplifier.cs:51` — if the new expression can host a target-typed
delegate creation, `SyntaxKind.UnsafeExpression` must join the parent-kind list. `SyntaxSerialization/**` is
unaffected.

**Design time (DT), Compile time (CT), Test (TEST), Extensions (EXT), Patterns (PAT), Premium (PREM).**
Nothing in `Metalama.Framework.DesignTime` matches on expressions; the only impact there is the generated
hashers. In `CompileTime/**`, `ProduceCompileTimeCodeRewriter` handles expressions generically through
`VisitCore` (1628). In the test harness the relevant guard is `SyntaxTreeStructureVerifier.Verify`, and the
aspect-test project already sets `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`. In Extensions, the touch
points are `Metalama.Framework.Analyzers/DurabilityContext.Expressions.cs:119-142` (`Unwrap`, if the new
operation is a transparent wrapper), `:64-113` (`GetExpressionVerdict`), the registered `OperationKind` set
if the form is itself an assignment, and `:316` (`Descendants`); the statement metric is unaffected and the
node metric counts it automatically. In Patterns, `RoslynHelper.cs:52-70` needs
`case UnsafeExpressionSyntax: return GetAccessKind( parent );`, and the extra depth level that the wrapper
adds around an expression must be tested against `DependencyGraphBuilder.Visitor.cs:280-291`, not reasoned
about. In Premium nothing walks expressions; the whole burden is on the core's
`InboundReferenceIndexBuilder`, and what changes there is only test evidence.

### 3.4 A new collection-expression element (`with(...)`, `WithElementSyntax : CollectionElementSyntax`)

**Generated layer.** `CollectionElementSyntax` is an `AbstractNode` whose concrete forms today are
`ExpressionElementSyntax` and `SpreadElementSyntax`, both of which have an `ExpressionSyntax` child.
`CollectionExpression`, `ExpressionElement` and `SpreadElement` are already gated at `V4_8_0` in
`RoslynVersionSyntaxVerifier.g.cs`, so a new derived element joins them automatically once step 0 passes.

**Templating (TMPL) — the only subsystem with real work.**
`TemplateAnnotator.VisitCollectionExpression` (3495-3503) already visits every element, **but** a
`WithElementSyntax` has an `ArgumentListSyntax` child and no `ExpressionSyntax` child, so the filter at
`AddScopeAnnotationToVisitedNode` line 693 selects nothing and `GetExpressionScope` returns
`RunTimeOrCompileTime` (448-451) no matter what the arguments are. A `VisitWithElement` override, or a
widening of the line 693 filter to include `CollectionElementSyntax` and `ArgumentSyntax`, is required.
Second, `MetaSyntaxRewriter.Transform<T>( T? node )` (106-139): when the element is compile-time inside a
transformed collection expression, the `default:` arm at 131-133 throws
`AssertionFailedException( "Unexpected node kind: ..." )`, because a `CollectionElementSyntax` is neither
`ExpressionSyntax`, `ArgumentSyntax` nor `StatementSyntax`. **A `CollectionElementSyntax` arm, or a
`TransformCollectionElement` virtual, is required.** Third, `TemplateCompilerRewriter` needs a
`TransformWithElement` if the element must survive into run-time code.

**Public code model (CM-PUB).** **No change.** Collection expressions are not modelled. The nearest things
are `SyntaxBuilders/ArrayBuilder.cs`, which emits array-initializer syntax and has no notion of a
collection-expression element, and `SignatureMatcher.GetParamsElementType` / `GetIterationType`
(`SignatureMatcher.cs:282-`), which encode the *collection* rules of the language rather than the
*collection expression* rules and would change only if C# 15 changed what may be a `params` collection.

**Code model implementation (CM-ENG).** Nothing in `CodeModel/**` parses collection expressions. The two
places a collection-expression element can reach the code model are
`CodeModel/Invokers/ValueArrayExpression.cs` (which builds `params` arrays for invokers) and
`CodeModel/Helpers/TypedConstantExtensions.cs` with `CodeModel/StandaloneAttributeData.cs` (attribute
argument values, which can be array-valued). Neither reads a `CollectionExpressionSyntax`. The only exposure
is that `SerializableTypeIdResolver.cs:104` re-parses a generated type syntax with no explicit parse
options.

**Linker (LINK).** `WithElementSyntax` is not referenced anywhere in the linker today and the linker does
not enumerate `CollectionElementSyntax` at all. Two contact points: `SymbolReferenceFinder.cs:209` — a
`with(...)` argument list contains `IdentifierNameSyntax` nodes, which the `BodyWalker` index picks up, so
nothing to do; and `LinkerAnalysisStep.OnInitializedCallSiteFinder.cs` with
`Substitution/OnInitializedWithExpressionSubstitution.cs`, which handle the *`with` expression*
(`WithExpressionSyntax`), **a different node** — do not confuse the two. A collection `with(...)` element is
a constructor-argument site and may need to be treated as an object-creation call site by
`LinkerAnalysisStep.ObjectCreationCallSiteReference` and `CreateInitializerSubstitution`
(`SubstitutionGenerator.cs:918-930`) if `OnInitialized` advice should fire for it.

**Syntax generation (SYNGEN), Advising (ADV), Compile time (CT), Design time (DT), Test (TEST).** No
touch points. `SyntaxGeneration` never constructs or destructures a collection expression: the only mention
of `SyntaxKind.CollectionExpression` is `ContextualSyntaxGenerator.cs:1052`, about parenthesisation of the
whole expression, and `ArrayCreationExpression` (205-211), `ListSerializer.cs:29` and
`DictionarySerializer.cs:115-127` all emit the pre-C#-12 forms. `AdviceImpl` never parses or rewrites a
collection expression; it only serialises `IExpression` values through `IExpression.ToExpressionSyntax`.
`CollectionElementSyntax` is not a `MemberDeclarationSyntax`, so neither of the two big test-harness
switches is involved; the whole test cost is a payload under `Tests/Aspects/CSharp15/WithElement/` modelled
on `Tests/Aspects/CSharp12/CollectionExpressions.cs`.

**Patterns (PAT).** This is the benign case: the arguments are ordinary `ArgumentSyntax` nodes, so
`DependencyGraphBuilder.Visitor.VisitArgument` (305) already isolates each of them in its own root gather
context. Nothing must change, **provided** the arguments really do arrive as `ArgumentSyntax`; verify that
with a test rather than by reading the grammar, because the whole correctness of chain isolation rests on
it. Note also that `VisitInvocationExpression` is commented out (292-302), so argument *types* are not
validated and a `with(...)` argument raises no diagnostic either way.

**Extensions (EXT), Premium (PREM).** In Extensions: nothing in the metrics (`DefaultVisit` recurses);
`AnalyzeArgument` in the two analyzers if Roslyn reports the arguments as `OperationKind.Argument`; and
`GetExpressionVerdict` if a collection expression containing a `with(...)` element retains what it is given.
In Premium: no walker; `ReferenceKinds.ObjectCreation`, whose documentation already says "In case of
collection expression, the reference points to the type", is the flag most likely to gain a sibling, and
`ReferenceKinds.All = -1` picks a new flag up everywhere with no edit.

### 3.5 A new optional field on an existing statement (labelled `break` and `continue`)

This is the only one of the five grammar changes that exercises the generator's multi-version code path, and
it is the one with the sharpest failure mode.

**Generated layer.** Once the `ExperimentalUrl` attributes at `Syntax-5.10.0.xml:1296` and `:1307` are
dropped: `VersionDetector` gives the field `MinimalRoslynVersion = 5.10.0` while the node's other fields
keep `4.0.1`; `Generator.GenerateMetaSyntaxRewriter` (432-479) then emits a
`switch ( this.TargetApiVersion )` with one arm per relevant version and `default: throw` at 476-477;
`GenerateVersionChecker` (127-156) emits
`this.VisitVersionSpecificField( node.Name, RoslynApiVersion.V5_10_0 );` inside `VisitBreakStatement`;
`GenerateHasher` (645-705) adds `this.Visit( node.Name );`; and `GeneratePartialUpdate` (768-799) adds an
`Option<IdentifierNameSyntax?> name = default` parameter. **Nothing in the generator needs to change.**
Note also that `RoslynVersionSyntaxVerifier.VisitVersionSpecificField` guards with
`if ( !nodeOrToken.IsKind( SyntaxKind.None ) )`, and `BreakStatementSyntax.Name` is `SyntaxKind.None` for an
unlabelled `break`, so this is exactly the case the mechanism was designed for.

**Templating (TMPL).** `TemplateAnnotator.VisitBreakStatement` and `VisitContinueStatement` (1375-1379)
annotate the statement with `_currentScopeContext.CurrentBreakOrContinueScope`, that is, the scope of the
*innermost* enclosing loop or switch. A labelled `break` targets an *outer* construct, so the scope must be
taken from the labelled construct instead; `ScopeContext` (`TemplateAnnotator.ScopeContext.cs:21,123`)
carries a single `CurrentBreakOrContinueScope` with no label map, and **adding one is the structural
change.** `TemplateCompilerRewriter.VisitSwitchStatement` (2568-2578) appends a bare `BreakStatement()` when
the last transformed statement is not a control transfer; `BreakStatement` in the kind list at 2569 matches a
labelled break too, which is correct there, but the synthesised `BreakStatement()` is unlabelled and would be
wrong if it were ever used to close a labelled section. The same synthesis appears in the builder API at
`Templating/Statements/SwitchStatement.cs:277`.

**Linker (LINK).** Three contact points, the first of which is a genuine defect.
`LinkerLinkingStep.CountLabelUsesWalker.cs:24-31` must also count `VisitBreakStatement` and
`VisitContinueStatement` where `node.Name != null`; without it the counter is too low and
`RemoveTrivialLabelRewriter` may delete a label that a labelled `break` still targets.
`LinkerAnalysisStep.SemanticBodyAnalyzer.cs:254-391` treats `LabeledStatement` as a control statement (330)
but knows nothing about `break L;`, and a labelled break jumps *out* of an enclosing loop, so a `return`
currently classified as exit-flowing may no longer be. `Substitution/ReturnStatementSubstitution.cs:86,104,154`
uses the two-argument `BreakStatement` overload, which yields `Name == null`; that is still correct because
the linker's `break` must bind to the innermost switch section, but the call must be re-checked if the
factory overload set changes. `LinkerLinkingStep.CleanupBodyRewriter.cs` block flattening hoists statements
out of a generated block, and a labelled `break` inside the hoisted statements keeps its meaning only
because label names are unique per method.

**Public code model (CM-PUB), Code model implementation (CM-ENG), Advising (ADV), Syntax generation
(SYNGEN), Compile time (CT).** No exposure. Statements are never modelled in the code model; the only
statement-shaped code there is `IteratorHelper.FindYieldVisitor` and `SafeSyntaxWalker`'s generic descent,
both of which handle an unrecognised statement by walking its children. In the public model, statements are
opaque `IStatement` and the one structured builder is `SwitchStatementBuilder`, which has no `break` or
`continue` model at all. `AdviceImpl` never reads a `BreakStatementSyntax`. In `SyntaxGeneration` and
`Formatting`, nothing visits either statement: `TextSpanClassifier` handles only `VisitIfStatement`,
`VisitForEachStatement` and `VisitBlock`, so everything else falls to `DefaultVisit`, which marks the node
from its annotation and recurses, and a labelled `break` is coloured correctly by accident. In
`CompileTime/**`, this is the `IsVersionSpecificField` path and needs no hand edit.

**Design time (DT).** The generated hashers, per §2.8: the Roslyn 5.0 variant's `VisitBreakStatement` does
not read the new `Name` field, so two syntax trees differing only in a `break` label hash equal under that
variant. Also `TheDiagnosticAnalyzer.TryMapLocation` (459-460) matches tokens by text among the direct
children of a node, and a new identifier-valued token on `BreakStatementSyntax` adds a candidate there.

**Patterns (PAT) — the case with the highest silent-wrongness risk in that subsystem, and one line to fix.**
`DependencyGraphBuilder.Visitor.VisitIdentifierName` (409-437) fires on **every** `IdentifierNameSyntax` the
walker reaches, resolves it through the semantic model, and appends any resulting symbol to the current
dependency chain. The label of a `break outer;` inside a property getter is an `IdentifierNameSyntax`. If
Roslyn's semantic model returns a label symbol for it, the walker appends it; `AccessKind` for it is `Read`
by the fallback at `RoslynHelper.cs:75`, so by the guard at 421 it starts a chain and is not filtered out by
the `AccessKind` test at 227; and because a label symbol is neither `SymbolKind.Property` nor
`SymbolKind.Field`, `supportedStemAndLeafCount` (237-241) truncates the chain there and **every member after
the label is dropped from the dependency graph**. The remedy is a `VisitBreakStatement` and
`VisitContinueStatement` that skip the `Name` field, or a `SymbolKind.Label` filter in `VisitIdentifierName`
— add both, plus a test.

**Test (TEST), Extensions (EXT), Premium (PREM).** In the test harness, nothing constructs or destructures
those statements; the exposure is the meta-syntax round trip
(`SyntaxTreeStructureVerifier.VerifyMetaSyntax`, 30 and 37), where a node whose new field the generator does
not know is rendered without it and compared against an equally truncated reparse — **a false pass** — plus
the `.t.cs` baselines of any test whose transformed output contains a labelled `break`. In Extensions,
nothing structural changes: a `BreakStatementSyntax` is a `StatementSyntax` and is counted either way, the
extra `IdentifierNameSyntax` child is counted by the node metric, `LinesOfCodeMetricProvider` walks
`DescendantTokens()`, and a labelled `break` is not a `LabeledStatementSyntax` so `VisitLabeledStatement` is
unaffected; the only visible effect is an unstyled `cs-label-name` class in the HTML output. In Premium
there is no effect at all.

**Also affected regardless of subsystem**: `Metalama.Framework.Engine/Utilities/Roslyn/FlowAnalyzer.cs:42-89`
`NeverContinues`. A labelled `break` transfers control out of the enclosing switch, so the switch-section
arm (66-84) would over-report "never continues". It currently returns `false` for anything it does not
recognise, so this specific case is a false positive rather than a false negative.

### 3.6 A new kind of member in an extension block (indexers, no new grammar node)

C# 15 permits an indexer inside an `extension` block. There is no new syntax node: it is an ordinary
`IndexerDeclarationSyntax` member of an `ExtensionBlockDeclarationSyntax`.

**Public code model (CM-PUB).** **Nothing new is required.** `DeclarationExtensions.cs:71-73` already lists
`DeclarationKind.Indexer` in the `ExtensionBlock` arm of `CanContain`, and `INamedType.Indexers` (106) is
inherited by `IExtensionBlock`. Two pre-existing gaps should nevertheless be closed so that the new indexers
are actually enumerated: `NamedTypeExtensions.MethodsAndAccessors` (40-65) skips `INamedType.Indexers`
entirely and skips `IEvent.RaiseMethod`, and `DeclarationExtensions.ContainedChildren` (334-341) does not
descend into `INamedType.ExtensionBlocks` at all, so `ContainedDescendants` misses every member declared in
an extension block. A third, `DeclarationExtensions.GetMembers` (220-228), throws for
`DeclarationKind.Indexer` because the arm was never written.

**Advising (ADV) — where the substantive work is.**
1. Delete `AdviceFactory.cs:1406` (`ValidateNotExtensionBlock( targetType, "an indexer" )`).
2. Delete `Tests/Aspects/Introductions/ExtensionBlocks/ErrorIndexerIntoExtensionBlock.{cs,t.cs}` and replace
   it with a positive `IntroduceIndexerIntoExtensionBlock` test.
3. Add `IntroduceIndexerTransformation.GetImplicitDeclarations()`. Today the class (139 lines, only
   `GetInjectedMembers` at 28) inherits `BaseTransformation.GetImplicitDeclarations()` (63), which returns
   empty, so the moment line 1406 is deleted an introduced extension indexer is injected into the extension
   block while its static implementation methods (`get_Item`, `set_Item`) are never added to the code model.
   Nothing throws; the code model is simply incomplete, and invokers and the linker do not see them.
4. Extend `ExtensionImplementationHelper` with an indexer-accessor overload.
   `CreateImplicitAccessorMethod` (163-, with the name computed at 177 as `"set_"` or `"get_"` plus the
   property name) takes a single `propertyType` and no index parameters, so it cannot express an extension
   indexer at all.
5. Add a positive `ExtensionMembers_Contract_OnReceiver_Indexer` test, which exercises the loop at
   `ContractExtensionBlockTransformation.cs:93` that has been present since the first commit of that file
   and is speculative support until now.

**Linker (LINK).** The extension-member machinery already exists from #1034 through #1159:
`LinkerInjectionStep.Rewriter.cs:324,621-637`, `LinkerLinkingStep.LinkingRewriter.cs:79`,
`LinkerInjectionStep.cs:251,837-874,1136`, `LinkerInjectedMemberComparer.cs:29,73`,
`LinkerInjectionStep.TransformationCollection.cs:834,842`, `Transformations/InsertPosition.cs:73`,
`LinkerAspectReferenceSyntaxProvider.cs:213-214,268-269,289-290` and `ProceedHelper.cs:234-235,252-253`.
`LinkerAspectReferenceSyntaxProvider.CreateIndexerAccessExpression` (213-214) already accounts for a
parameter receiver. What must be checked is that `LinkerRewritingDriver.RewriteMember` (479-481, which
distinguishes a property from an indexer by `IPropertySymbol { Parameters.Length: 0 }`) and
`LinkerRewritingDriver.Indexers.cs` behave when the declaring type is an extension block, and that
`Linking/SymbolExtensions.cs:23-64` `GetDeclarationFlags` — which does not list
`SyntaxKind.ExtensionBlockDeclaration` — is not reached for the containing block.

**Code model implementation (CM-ENG).** No new kind. What is already required and now becomes live:
`SerializableIds/SerializableTypeIdResolverForIType.cs:127-130`, which accepts only `Namespace` and
`NamedType` as containers and throws otherwise; and, in the Premium repository,
`ReferenceValidationContext.GetInboundGranularity`, which throws on `DeclarationKind.ExtensionBlock`, so a
validated reference to an extension-block indexer throws today.

**Extensions (EXT) and Premium (PREM).** Two internal-surface enumerations omit indexers and are widened by
this feature: `Metalama.Premium/src/Metalama.Extensions.Architecture/ArchitectureExtensions.cs:169-174` and
its duplicate `Aspects/InternalsUsageValidationAttribute.cs:144-152` walk `t.Properties` and `p.Accessors`
but never `t.Indexers`, so an internal accessor of a public indexer is not validated; and
`Metalama.Framework.Workspaces/ICompilationSet.cs` exposes no `Indexers` at all, so an extension indexer is
invisible to Workspaces, Introspection and LINQPad on two counts.

**Patterns (PAT).** `Metalama.Patterns.Contracts/ContractExtensions.cs:93-108` and
`CheckInvariantsAspect.cs:27-33` already read `t.Indexers`; `Metalama.Patterns.Immutability/ImmutableAttribute.cs:57,72`
does not, so a mutable extension indexer on an `[Immutable]` type is not reported.

**Test (TEST).** A `Tests/Aspects/CSharp15/ExtensionIndexer/` directory with committed `.t.cs` baselines,
plus, for the design-time path, `.0.i.cs` and `.1.i.cs` companions, following
`Tests/Aspects/CSharp14/ExtensionMembers/ExtensionMembers_Introduce_DesignTime.cs`.

---

## 4. How a platform version propagates

### 4.1 A new .NET runtime version as a user target framework (`net11.0`)

**What is already done.** `Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.props:31`
sets `MaximumNETCoreAppVersion` to `11.0` and `:33` sets `MaximumSdkVersion` to `11.0`; lines 38-41 already
say ".NET 10 and .NET 11"; `docs/platform-support.md:199-206` records the decision; and
`Directory.Packages.md:189` already names the .NET 10 and .NET 11 SDKs in the variant table. A `net11.0`
project therefore gets **no** LAMA0600 and the .NET 11 SDK gets **no** LAMA0601 today. The platform check
cannot be used as a tripwire for this work: it is already green.

**Step 1: decide, per rule 8 of `platform-support.md:53-54`, which shipped asset's selection changes.**
On the evidence, likely none: a `net11.0` project resolves the `net10.0` asset, and the `net10.0` embedded
Core flavour runs on the .NET 11 runtime by roll-forward. If that is the conclusion, record it in the
"Shipped assets under PB-2027.0" table (`platform-support.md:268-281`) so the next reader does not re-derive
it.

**Step 2: the derived values that must move regardless of step 1.**
- `Metalama.Framework.Engine/Utilities/LanguageVersionProvider.cs:56` — the `>= 10 => CSharp14` arm silently
  caps the .NET 11 SDK at C# 14.
- `Metalama.Framework.Engine/CompileTime/CompileTimeAssemblyLocator.cs:43` — `_defaultCompileTimeTargetFrameworks`
  still names `net8.0`, which is written verbatim into the generated `TempProject.csproj` (749) that is
  restored and built on the user's machine.
- `Metalama.Framework.Engine/Options/DefaultProjectOptions.cs:56` — `TargetFramework => "net8.0"`, which
  becomes a directory segment through `OutputPathHelper.GetOutputPaths`.
- `eng/src/Program.cs:26-31` — add `PreferredVersions.DotNetSdk.V_11_0` to the container requirements, and
  drop the `V_9_0` entry once `eng/src/BuildMetalama.csproj:6` moves off `net9.0` (which also means updating
  the `WorkingDirectory` at `Metalama.Framework.CompilerExtensions.csproj:88-89`).
- `global.json:4` and `eng/docker/build.Dockerfile` — both generated; the .NET 8 line at
  `build.Dockerfile:44` should disappear at the same time.
- `Metalama.Framework.Workspaces/Metalama.Framework.Workspaces.csproj:97` — the `net9.0` fallback, which is
  guarded by an `<Error>` and so can wait.
- `Directory.Packages.props:50` `MicrosoftBuildVersion` (defined as the MSBuild of the lowest host) and the
  `*LatestVersion` properties at 53-73, re-derived against the .NET 11 line.

**Step 3: if an asset genuinely needs `net11.0`, the seven places a target framework is declared**, in the
order they must move together, are listed in §2.9 under "Adding `net11.0`". The highest-risk of them is
`Metalama.Framework.CompilerExtensions.Resources.csproj:6`, because there is exactly one Core flavour and no
fallback (`platform-support.md:76-82`), and the ten unguarded globs in
`Metalama.Framework.CompilerExtensions.csproj:53-70` must move in the same commit or the payload silently
becomes empty.

**Step 4: the extension-loader string equality.** `Options/TargetedAssemblyReference.cs:20` and
`Extensibility/ExtensionLoaderBase.cs:31` carry the literal `"net10.0"`, compared by exact string equality
against the `TargetFramework` metadata of every `MetalamaExtensionAssembly` item. The twelve literals in the
HtmlWriter and DiffEngine props files (EXT), and the four Premium manifests (PREM), must agree. A mismatch
produces an empty extension list and a single trace log.

**Step 5: Backstage.** No project must move: the Worker, the Desktop tray application and the dotnet tool all
declare `RollForward=Major`, and `Metalama.Backstage` itself is a library whose `net10.0` asset loads on
.NET 11. If a target framework does move,
`Metalama.Backstage/src/Metalama.Backstage/Tools/DevBackstageToolsLocator.cs:235` changes in the same commit.

**Step 6: Patterns and Premium.** `Metalama.Patterns.Wpf` (`net472;net10.0-windows`, no `netstandard2.0`) is
the package where a `net11.0-windows` leg would have to be decided or explicitly declined. In Premium,
`Metalama.Patterns.Caching.Backends.{Azure,Redis}` and `Metalama.Patterns.Caching.LoadTests` target `net471`,
below the PB-2027.0 .NET Framework floor of 4.7.2; and every Premium project still names `net8.0`.

**Step 7: the analysis level.** `AnalysisLevel` defaults to the version of the target framework, so moving a
project to `net11.0` turns on rules that are off today, and `CodeQuality.targets:17-19` sets
`TreatWarningsAsErrors` under `ContinuousIntegrationBuild`. The recorded sequence (issue #1876, then #1893)
is: bump, observe, add a temporary `NoWarn` in the repository-root `Directory.Build.props` naming each rule
and the issue, then resolve them in a follow-up commit and remove the suppression.

**Step 8: `MSBuildInitializer`.** `Metalama.Framework.Workspaces/MSBuildInitializer.cs:83-87` filters SDKs to
`ParsedVersion.Major <= Environment.Version.Major`, and `Metalama.Framework.Workspaces` is `net10.0` with no
`RollForward`. On a machine with the .NET 10 runtime and both SDKs, the .NET 11 SDK is filtered out and the
workspace evaluates a `net11.0` project with the .NET 10 SDK.

**Step 9: tests.** `Standalone/SupportedPlatform.TestedTargetFrameworks/…csproj:8-10,13` records in a comment
that `net481`, `net11.0` and `net11.0-windows` are in the tested matrix but omitted because the build agents
lack their targeting packs; and `SupportedPlatform.{MultiTargeting,UntestedTargetFramework,Exclusion,NoWarn,
CheckDisabled,MetalamaDisabled}` assert LAMA0600 for `net8.0` and `net9.0`, which become vacuous rather than
red if those floors move again. `docker/*/*/Dockerfile` and `docker/*/*/global.json` pin their SDK
independently.

**Step 10: verification.** `platform-support.md:344-364` makes three machine-based checks mandatory. For a
`net11.0` addition the relevant ones are item 1 (the Visual Studio 2026 long-term servicing channel private
runtime and Roslyn version, after 2026-11-10) and item 3 (a design-time smoke test on the floor, reading the
`ServiceHub.RoslynCodeAnalysisService` log rather than the editor).

**Step 11: Backstage's licensing, on macOS.** The .NET 11 removal of finite-field DSA (issue #1860) is the
largest single .NET 11 item in the repository. `DSA.Create` throws `PlatformNotSupportedException`, which
derives from `NotSupportedException` and **not** from `CryptographicException`, so it escapes
`LicenseKeyData.VerifySignature`'s `catch` (`LicenseKeyData.Validation.cs:46-68`), escapes
`License.TryGetConsumptionProperties` (`License.cs:108`, no `catch`), escapes `LicenseConsumptionService`,
and reaches the caller. That is loud, which is the better outcome; the danger is the wrong fix.

### 4.2 A new .NET SDK

**The single decision point** is `Metalama.Framework.Engine/Utilities/LanguageVersionProvider.cs:45-72`.
`GetCompileTimeLanguageVersion` (29) reads `IProjectOptions.SdkVersion` (MSBuild `NETCoreSdkVersion`); when
it is empty it takes the `msbuild.exe` path instead (74-123), and otherwise it parses the SDK version and
maps `version.Major switch { >= 10 => CSharp14, >= 9 => CSharp13, >= 8 => CSharp12, _ => throw
PlatformNotSupportedException }`, then returns the minimum of that and the project's `LanguageVersion`
(62-71). The `>=` makes a new SDK silent rather than an exception: the .NET 11 SDK returns C# 14. The
mechanical fix is a `>= 11 => CSharp15` arm inserted **above** line 56.

The `msbuild.exe` path has the same shape: `GetLanguageVersionFromMSBuild` probes
`<MSBuildBinPath>\Roslyn\Microsoft.CodeAnalysis.CSharp.dll` (88), reads its `AssemblyName.Version` (107) and
calls `SupportedCSharpVersions.GetMaxLanguageVersion` (111), whose `(>= 5, _) => CSharp14` arm at
`SupportedCSharpVersions.cs:152` caps every Roslyn 5.x at C# 14 with no LAMA0052 equivalent on that path.

**The MSBuild property plumbing**: `Options/MSBuildPropertyNames.cs:54` (`NETCoreSdkVersion`), `:55`
(`MSBuildBinPath`), the "all properties" array at 74, 91, 101, 102 and 105;
`Options/IProjectOptions.cs:263,270`; `Options/MSBuildProjectOptions.cs:186-188,191`;
`Metalama.Framework.Package/build/Metalama.CompilerVisibleProperties.props:40`.

**The nested build.** `CompileTime/CompileTimeAssemblyLocator.cs:194` stores the SDK version, `:705` writes
`GlobalJsonHelper.WriteCurrentVersion( this._cacheDirectory, this._sdkVersion )` — and
`Utilities/GlobalJsonHelper.cs:22-36` writes `"rollForward": "disable"` pinned to the host SDK version —
and `:838-848` chooses `msbuild.exe` over `dotnet` when `SdkVersion` is empty. Failures of that nested build
are classified by `CompileTime/ReferenceAssemblyBuildFailureClassifier.cs:147-152` (`NETSDK1045`, "SDK too
old for the target frameworks of the reference-assembly project") and `:184-208` (the `global.json` rule).

**The cache key does not include the SDK version.** `CompileTimeCompilationBuilder.ComputeProjectHash`
(169-247) appends `_buildId`, the assembly identity, the referenced compile-time identities, `sourceHash`,
`FormatCompileTimeCode`, `AllowPreviewLanguageFeatures`, `RequireOrderedAspects`, `RoslynIsCompileTimeOnly`,
`CompileTimeTargetFrameworks`, `TemplateLanguageVersion` (the raw MSBuild string, 239) and
`RoslynApiVersion.Current` (243). `ComputeSourceHash` (123-167) appends the target framework and the
preprocessor symbols. Neither appends `SdkVersion` nor the value returned by
`ILanguageVersionProvider.GetCompileTimeLanguageVersion()`.

**Source generators ship with the SDK.** `Metalama.Backstage/src/Metalama.Backstage/Serialization/BackstageJsonContext.cs:24-61`
is a source-generated `System.Text.Json` context, and two defects already recorded in that area
(`DiagnosticsConfiguration.cs:24-31` for #1777, `:39-53` for #1778) are consequences of generator behaviour,
so the configuration round trip must be re-tested after an SDK bump. `Metalama.Framework.Package/build/Metalama.Framework.props:66-78`
`MetalamaSourceGeneratorAttribute` is a hand-maintained list of attribute-based source generators whose
comments say ".Net 9" and "ASP.NET Core 9 … does not ship any attribute-based source generators"; a .NET 11
wave has to re-derive that list.

**The `LangVersion` clamp.** `Metalama.Framework.Package/build/Metalama.Framework.targets:118-121` is a
literal whitelist. The .NET 11 SDK will implicitly set `LangVersion` to `15.0` for a `net11.0` project;
`'15.0'` is not in the list, so the condition is true and the project is compiled as **C# 12**. The warning
raised at 243-247 says the version was raised "to … the lowest version supported by Metalama Framework",
which reads as a floor message and describes a ceiling action. This should be rewritten as a numeric
comparison rather than extended by one more literal.

**Workspaces.** `MSBuildInitializer.cs:59-68` shells out to `dotnet --list-sdks` and parses it with the
regular expression at 70; `:83-87` filters by runtime major version; `:123-154`
`HasMatchingProcessorArchitecture` parses line index 2 of the SDK's `.version` file and compares it to
`RuntimeInformation.RuntimeIdentifier`, so a change in that file's layout would silently reject every SDK.
`Workspace.DotNetRestore` (254-258) runs `dotnet restore` in the project directory, so the SDK it selects is
the one `global.json` resolves *for that directory*, not the one `MSBuildInitializer` chose.
`Workspace.cs:309-341` accepts `.csproj`, `.sln` and `.slnf` and throws on anything else, which will bite as
soon as the .NET 11 SDK makes `.slnx` the default solution format.

**Test containers.** `Metalama.Framework/src/tests/docker/**` pins `ARG DOTNET_VERSION=10.0.302` and a
per-scenario `global.json`; `docker/win-x64/ReferenceAssemblyArchitectureMismatch/Dockerfile:18,22` pins
`SDK_X64=10.0.302` and `SDK_X86=8.0.423`.

### 4.3 A new Roslyn version

The procedure is `Metalama.Framework/docs/updating-roslyn.md`, in twelve steps, and it is complete and
current. Restated against the files:

**Raising `RoslynApiMaxVersion` (taking a newer Roslyn).**
1. `Directory.Packages.props:28` `RoslynApiMaxVersion` and `:30` `RoslynMaxVersion`.
2. `eng/RoslynVersions/Roslyn.5.10.0.props:5` `ThisRoslynVersionNoPreview` if the identity changes; line 3
   already follows `RoslynApiMaxVersion`.
3. `SupportedCSharpVersions.ToNuGetVersionString:85` — the exact package version, prerelease label included.
   **This one string also decides whether `nuget.base.config`'s `roslyn-consolidated` source is written into
   the user-side generated `nuget.config`** (`SupportedCSharpVersions.cs:117-132`;
   `CompileTimeAssemblyLocator.cs:234-243,777-830`).
4. `nuget.base.config:8` — the feed itself, removed when leaving prerelease.
5. Add `eng/src/GenerateMetaSyntaxRewriter/Syntax-<new>.xml`, copied **unchanged** from
   `src/Compilers/CSharp/Portable/Syntax/Syntax.xml` of the matching `Metalama.Compiler` branch, **keeping
   the experimental nodes** (`updating-roslyn.md:12`: the grammar file has to keep describing the Roslyn
   version it is named after).
6. `eng/src/GenerateMetaSyntaxRewriter/GenerateMetaSyntaxRewriter.cs:18` — add the version to `versionNames`,
   and move the superseded one into `legacyVersionNames` (17) if it no longer needs generated code. **Do not
   remove a version outright**: `RoslynApiVersion` ordinals are positional
   (`Generator.cs:88`, `version.Version.Index + deprecatedVersionNames.Length`) and are the wire form of
   `TemplateSymbolManifest.UsedApiVersion`, so removing one from the head shifts every ordinal and silently
   reinterprets the manifests of already-compiled references. `CompileTimeProjectManifest.ManifestVersion`
   does not guard against that.
7. `Build.ps1 prepare`.
8. `Metalama.Framework.CompilerExtensions/RoslynVariantPolicy.cs:32-34` if a new variant is served, and
   `Metalama.Framework.CompilerExtensions.Resources.csproj:25-26` for its `ProjectReference`.
9. `Metalama.Framework.Introspection.csproj:22-23` — the `InternalsVisibleTo` names; and
   `Metalama.Framework/src/Metalama.Framework/Metalama.Framework.csproj:17-33`, the seven-entry list there.

**Raising `RoslynApiMinVersion` (dropping a variant).** `updating-roslyn.md:35` is the checklist:
`Directory.Packages.props:23`; delete `eng/RoslynVersions/Roslyn.<old>.props`; delete the shim projects and
remove them from `Metalama.Framework.sln`; `Metalama.Framework.CompilerExtensions.Resources.csproj:25-26`;
`RoslynVariantPolicy.cs:22,32-53`; the four `SupportedCSharpVersions` switches at 52-62, 77-87, 134-144 and
149-159; `Directory.Build.props:16` (the template language ceiling, whose ceiling *is* `RoslynApiMinVersion`,
so raising the floor is what unlocks a higher template language version); every preprocessor symbol now
defined by all remaining variants or by none, together with its `#if` sites, its `@RequiredConstant` and
`@ForbiddenConstant` directives and its `metalamaTests.json` entries (the 2027.0 precedent removed 177 blocks
and 69 directives for one symbol); the tables in `Directory.Packages.md:161-172,193-209` and
`platform-support.md:216-266`; and the same steps mirrored in `Metalama.Premium`.

**What a new Roslyn version costs even with no language change.** The formatting output moves: commit
`32e6150298` re-adopted the Patterns baselines for Roslyn 5.10's trivia handling. Expect the same for any
uptake. The private-reflection bridges may move: `RemoteWorkspaceProvider.cs:50-58` already accommodates
`Default` being a field in Roslyn 4 and a property in Roslyn 5;
`SyntaxFactoryEx.LiteralFormatter.cs:33,41` already tolerates two `FormatLiteral` shapes; and
`MSBuildInitializer.cs:97-112` reflects into an internal `Microsoft.Build.Locator` constructor. The
hand-copied `ObjectDisplayOptions` enum (`SyntaxGeneration/ObjectDisplayOptions.cs:234-266`) is validated by
nothing. `Formatting/FormattedCodeWriter.cs:116-149` matches classification-type strings and passes unknown
ones through. And `RoslynVariantPolicyTests.LatestVersionSelectsThe5100Variant` asserts explicitly that
Roslyn `5.11.0` and `6.0.0` are served by the `5.10.0` variant, so **the latest variant is the catch-all for
any future Roslyn**, and its grammar snapshot is therefore the one that can fall behind the host.

### 4.4 A new Visual Studio version

**The policy value** is `Metalama.Framework.Package/build/Metalama.Framework.props:37`
`MinimumVisualStudioVersion` `18.0`, checked by
`Metalama.Framework.Package/build/Metalama.Framework.targets:392-421`, which emits LAMA0602 for MSBuild and
Visual Studio (lower bound only).

**The run-time selection** is `Metalama.Framework.CompilerExtensions/RoslynVariantPolicy.cs:30-54`, keyed on
the *host Roslyn version* rather than on the Visual Studio version;
`ResourceExtractor.GetHostRoslynVersion` (633-656) reads `typeof(SyntaxNode).Assembly.GetName().Version` and,
when that equals the JetBrains build marker `42.42.42.42`, parses `AssemblyInformationalVersionAttribute` up
to the first `-`. Below the floor, `TryGetVariantName` returns `false`, `TryCreateInstance` returns `false`
(157-172), `ReportUnsupportedHost` writes `unsupported-roslyn-<version>.txt` into the crash-reports directory
(180-211), and every entry-point shim degrades to a no-op — **with the single exception of
`MetalamaSourceTransformer`, which reports LAMA0087 as an error on the compile-time path**.

**The process-tree shape** is `Metalama.Framework.DesignTime/VisualStudio/ServiceHub/ServiceHubClientEndpoint.cs:50-83`:
`parentProcesses[0] == "Microsoft.ServiceHub.Controller" && parentProcesses[1] == "devenv"` for VS 2022 and
`parentProcesses[0] == "devenv"` for VS 2026, else a log line and `false`. Commit `5146c0a252` (issue #1096,
"Incompatibilities with VS 2026") is the precedent for absorbing a new generation here. `PipeNameProvider.cs:13-19`
makes the pipe name include a hash of the package version, so two Metalama versions in one session never
share a pipe.

**The process name** is `Metalama.Framework.CompilerExtensions/ProcessKindHelper.cs:19-58`, duplicated in
`Metalama.Backstage/src/Metalama.Backstage/Utilities/ProcessUtilities.cs:34-138`; both must be edited
together, and `Metalama.Backstage/src/Metalama.Backstage/Maintenance/ProcessManagerBase.cs:18-30` too if the
new process holds file locks. Adding a member to `Metalama.Backstage/src/Metalama.Backstage/Diagnostics/ProcessKind.cs`
is picked up automatically by the configuration surface, which builds its defaults from
`Enum.GetValues( typeof(ProcessKind) )`.

**The assembly-loading rule** is `ResourceExtractor.cs:539-603`: the contracts assembly is loaded with
`Assembly.LoadFile` (outside any assembly load context) when the process kind is `DevEnv` or `Rider`, so
that COM type equivalence works (issue #1626), and through the assembly load context otherwise, because
`Assembly.LoadFile` broke DevHub (issue #1461). Adding a host means deciding which side of that it is on.

**The frozen contract surface** is `Metalama.Framework.DesignTime.Contracts`: assembly name
`Metalama.Framework.DesignTime.Contracts.v2`, assembly GUID `234D9C3E-29CA-4ACC-8DB5-3F0D5C931D41`, the
AppDomain data slot name at `DesignTimeEntryPointManager.cs:23` which "is used verbatim and must never
change", `CurrentContractVersions.ContractVersion_1_0 = 3`, and the Roslyn pin `VersionOverride="4.0.1"` at
`Metalama.Framework.DesignTime.Contracts.csproj:30-33`. `docs/cross-process-communication.md` rule 3 (line 20)
forbids cross-process *and* cross-version traffic outright; the frozen-GUID checklist is at 102-111 and the
same-version RPC checklist at 113-121, with a symptom table at 93-100 mapping `ConnectionLostException`,
`FileLoadException`, `InvalidCastException` and "added a method to a `[Guid]` interface" to their causes.

**The build agents.** `eng/src/Program.cs:34-46` and `eng/docker/vs17.Dockerfile:33-36` install Visual Studio
Build Tools **17.14.15**, and `Program.cs:54` sets `MSBuildVersion = new Version( 17, 14 )`, while the
product declares `MinimumVisualStudioVersion` 18.0. Continuous integration therefore tests on an MSBuild the
product warns about with LAMA0602.

**The out-of-band package ceilings.** `Directory.Packages.props:114` (`System.Runtime.CompilerServices.Unsafe`
6.1.2, capped by an assembly-version 6.0.3.0 binding redirect), `:121` (`System.Memory` 4.6.3, capped by
4.0.5.0) and `:132` (`System.Threading.Tasks.Extensions` 4.5.4) are all justified by `devenv.exe.config`
binding redirects measured in VS 2022 17.14 and VS 2026 18.9, and `:179` (`StreamJsonRpc` 2.20.17) by an
assembly-version freeze for the separately deployed Metalama.Vsx. `Directory.Packages.md:71-79` documents
the family. Several of the comments naming VS 2022 are stale under PB-2027.0.

**The Backstage detection heuristic.** `Metalama.Backstage/src/Metalama.Backstage/UserInterface/WindowsUserDeviceDetectionService.cs:153-166`
reads `HKLM\SOFTWARE\WOW6432Node\Microsoft\VisualStudio` and returns `true` for any subkey whose name parses
as a decimal `>= 17`. Under PB-2027.0 the floor is Visual Studio 2026, which is version 18, so the constant is
now below the supported floor, and it is not obvious that Visual Studio 2026 still writes under
`WOW6432Node`.

---

## 5. Silent failure risks

Every place recorded by the fourteen maps that would do the wrong thing without an exception, a diagnostic
or a log the user sees. Grouped by subsystem; within each group, ordered by how quietly the wrong answer
arrives. A site that fails loudly is included only where the map recorded it as a near-miss or as an
invitation to a wrong fix.

### 5.1 Public code model (CM-PUB)

1. **`DeclarationExtensions.IsNamedDeclaration` is already wrong** (`Code/DeclarationExtensions.cs:140-143`).
   `DeclarationKind.ExtensionBlock`, `DeclarationKind.Indexer` and `DeclarationKind.Constructor` are missing,
   although `IExtensionBlock` is an `INamedType` and both `IIndexer` and `IConstructor` are
   `INamedDeclaration`. The property returns `false` with no diagnostic. This is a C# 14 wave miss and the
   template for what a C# 15 miss looks like.
2. **`ReferenceKindsExtension.ToDisplayString` degrades to an integer, and truncates**
   (`Code/ReferenceKindsExtension.cs:71-75`). Two defects. A new `ReferenceKinds` member not added to the 24
   `ConsiderKind` calls makes *every* combination containing it render as a number, not just the new flag.
   And `ReferenceKinds` is declared `: long` while the fallback casts to `int`, so any flag at or above
   `1 << 31` truncates; the current maximum is `1 << 26`, leaving five bits of headroom.
3. **`SourceReference.Kind` is a stringly-typed open channel** (`Code/SourceReference.cs:47`). It returns the
   Roslyn `SyntaxKind` name as a string. User code comparing it against a literal (`"ClassDeclaration"`)
   keeps compiling and silently stops matching when the construct becomes a `UnionDeclaration`. There is no
   enumeration to extend and no compiler assistance.
4. **`DeclarationExtensions.ContainedChildren` does not descend into extension blocks**
   (`Code/DeclarationExtensions.cs:334-341`). `INamedType.ExtensionBlocks` is not visited, so
   `ContainedDescendants` and `ContainedDescendantsAndSelf` (350, 359) silently omit every member declared
   inside an extension block. Any fabric or validator that walks the compilation with these methods misses
   them, and C# 15 indexers in extension blocks inherit the hole. The `_ => []` arm makes a new container
   type silently empty.
5. **`NamedTypeExtensions.MethodsAndAccessors` omits indexer accessors and event raisers**
   (`Code/NamedTypeExtensions.cs:40-65`). `INamedType.Indexers` is skipped entirely and `IEvent.RaiseMethod`
   is skipped. Since C# 15 adds indexers inside extension blocks, the set of indexer accessors this method
   misses grows.
6. **`GenericExtensions.GetDefinition` returns the declaration unchanged** (`Code/GenericExtensions.cs:56-62`).
   Callers receive a generic *instance* where they asked for the generic *definition*, then compare it for
   equality against a definition and get `false`. `DeclarationKind.ExtensionBlock` is not in the list today,
   and `DeclarationExtensions.IsContainedIn` (34) calls `GetDefinition` on both operands, so this feeds a
   wrong containment answer.
7. **`GenericExtensions.GetBase` returns null** (`Code/GenericExtensions.cs:42-50`). For a new member kind
   that can be overridden, a validator walking the override chain stops one link early and reports nothing.
8. **`DeclarationExtensions.GetEffectiveAccessibility(IType)` returns `Public`**
   (`Code/DeclarationExtensions.cs:433-435`). A new `IType` shape whose components are `private` is reported
   as effectively `public`. In an architecture-validation aspect this is a false negative, the dangerous
   direction.
9. **`RefKindExtensions.IsByRef` and `IsReadable` are negations** (`Code/RefKindExtensions.cs:23,48`). A new
   `RefKind` is silently classified as by-reference and readable. `IsWritable` (32) is the only one of the
   three that throws.
10. **`DeclarationExtensions.GetMembers` throws for `DeclarationKind.Indexer`**
    (`Code/DeclarationExtensions.cs:220-228`). Not silent, but wrong: the method's own documentation
    (214-218) lists only five kinds, so the omission looks deliberate.
11. **There is no test that any of these switches is exhaustive.** A search for `Enum.GetValues` combined
    with `DeclarationKind`, `TypeKind`, `OperatorKind`, `MethodKind`, `SpecialType` or `RefKind` across
    `Metalama.Framework/src` and `Metalama.Framework/tests` returns nothing. Nothing detects a missing arm at
    build time or at test time, and the C# compiler does not help either, because every one of these switches
    has a `default` or a `_` arm.

### 5.2 Code model implementation (CM-ENG)

1. **`HasIdentityOrImplicitReferenceConversion` falls through to `false`**
   (`CodeModel/Comparers/DeclarationEqualityComparer.Conversions.cs:90-139`). No `default` arm; an
   unrecognised `left.TypeKind` yields "no conversion exists". This comparer backs `IType.Is()`, aspect
   eligibility (`EligibilityRuleFactory.cs:47,121`), contract applicability and advice validation. A new
   reference-type-like kind would make aspects **silently skip** the declarations they were meant to apply
   to, with no diagnostic. This is the highest-consequence silent failure in the subsystem.
2. **`SyntaxKindExtensions.IsTypeDeclaration` omits `ExtensionBlockDeclaration`, and will omit
   `UnionDeclaration`** (`Utilities/Roslyn/SyntaxKindExtensions.cs:33-35`). Three consumers degrade quietly:
   `CodeModel/Source/SourceNamedTypeImpl.cs:342-348` `IsPartial` matches `_ => default` and answers **false**;
   `CodeModel/Helpers/DependencyAnalysisHelper.FindDeclaredTypesVisitor.cs:35-57` gates its nested-type walk
   on the predicate and has only six `VisitXDeclaration` overrides, so a `union` declaration is **omitted
   from the dependency graph** and the design-time incremental pipeline does not invalidate the file when the
   union changes; and `Utilities/Roslyn/SymbolExtensions.cs:289` picks a different declaring reference for a
   partial member, changing which file a diagnostic is reported in.
3. **`SerializableTypeIdGenerator.IsWrittenInAnnotatedContext` returns `false` for unknown kinds**
   (`SerializableIds/SerializableTypeIdGenerator.cs:93-113` and `:174-195`). The result decides whether the
   generated `SerializableTypeId` carries the trailing `!` recording "written in a nullable-annotated
   context". A wrong answer produces a **valid-looking identifier that denotes a different type**, and the
   failure surfaces much later as a reference that resolves to the wrong nullability or does not resolve at
   all.
4. **`OperatorData.GetByName` returns `null` for an unknown operator name**
   (`Utilities/Roslyn/OperatorData.cs:281`, consumed at `Utilities/Roslyn/SymbolExtensions.cs:318-320`). A
   C# 15 operator whose `WellKnownMemberNames` entry is not in the table is reported as `OperatorKind.None`
   even though `MethodKind` is `UserDefinedOperator`. Downstream, `DisplayStringFormatter.VisitMethod` prints
   the mangled metadata name or `ToOperatorMethodName` throws. The C# 14 wave added nineteen rows to this
   table and nothing warned that they were missing beforehand.
5. **`ToOurSpecialType` and `ToRoslynSpecialType` fall back to `None`**
   (`Utilities/Roslyn/SymbolExtensions.cs:74,103`). `SpecialType.None` is a legitimate value, so a new Roslyn
   special type is indistinguishable from "not special". Low risk for C# 15, high risk for a .NET 11 base
   class library that adds special types.
6. **`DeclarationExtensions.HasBody`, `IsEventField` and `HasInitializer` return `false` or `null`**
   (`CodeModel/Helpers/DeclarationExtensions.cs:436-457,459-469,471-485`). A member declared in a form the
   switch does not recognise is treated as abstract-like, which changes whether the linker inlines it.
7. **`GetPropertyKind` detects `SemiAuto` purely by syntax**
   (`CodeModel/Helpers/DeclarationExtensions.cs:292-378,393-413`, through
   `Utilities/Roslyn/SyntaxHelpers.cs:95`). Any new syntactic wrapper around a `field` expression that the
   descendant walk does not reach would silently reclassify a semi-auto property as `Default`, which changes
   whether Metalama transfers the property initialiser to the backing field.
8. **The `#if DEBUG` interface-type guard is absent in release builds**
   (`CodeModel/References/RefExtensions.cs:90,136` and its three call sites). A kind or interface mismatch
   introduced by the C# 15 work is caught in a debug test run and **not** in a shipped build, where it
   becomes an `InvalidCastException` far from its cause.
9. **`RefExtensions.ToRef( this ITypeSymbol, … )` does not exclude extension blocks**
   (`CodeModel/References/RefExtensions.cs:163-170`). The `INamedTypeSymbol` overload three lines above does
   branch on `IsExtensionSafe()`. An extension block symbol reaching the `ITypeSymbol` overload yields a
   `SymbolRef<INamedType>` whose target interface violates the invariant at `SymbolRef.cs:81-86` — an assert
   that is not `#if DEBUG`-guarded, so it throws; but the `GetPossibleDeclarationInterfaceTypes` cross-check
   at `SymbolRef.cs:60-64` *is* `#if DEBUG`, so the release behaviour of a similar mismatch elsewhere is an
   unchecked wrong-typed reference.
10. **`CompilationElementVisitor<T>` has no `TypeKind.Tuple` arm**
    (`CodeModel/Visitors/CompilationElementVisitor{T}.cs:19-29`). A tuple reaches `_ => throw` at 28 while
    the non-generic sibling handles it at 53. Two files that should have identical dispatch and do not —
    exactly the failure mode a new `TypeKind` value re-creates.
11. **`TypeSymbolRewriter.Visit(ITypeSymbol)` has no `TypeKind.Extension` arm**
    (`CodeModel/Visitors/TypeSymbolRewriter.cs:38-52`). The `_ => throw new ArgumentOutOfRangeException()` at
    51 fires with no message and no context, which reads as a Metalama bug rather than as a missing arm.
12. **`SerializableTypeIdResolverForIType` does not accept an extension block as a container**
    (`SerializableIds/SerializableTypeIdResolverForIType.cs:127-130`). One of the eight
    `NamedType or ExtensionBlock` widenings was not applied here.
13. **`ReferenceValidationContext.GetInboundGranularity` in `Metalama.Premium` never learned about extension
    blocks** (`Metalama.Premium/src/Metalama.Extensions.Validation/ReferenceValidationContext.cs:124-134`).
    A reference validator applied to a declaration inside an extension block throws. This is the one place in
    the Premium repository that this subsystem's taxonomy leaks into, and it shows that widening
    `DeclarationKind` does not automatically get followed across repository boundaries.

### 5.3 Templating (TMPL)

1. **`TemplateAnnotator.AddScopeAnnotationToVisitedNode` gives every unknown node a scope**
   (`Templating/TemplateAnnotator.cs:685-697`). There is no "I do not know this construct" branch. Any C# 15
   node reaching `VisitCore` is annotated with the combined scope of its `ExpressionSyntax` children, and if
   it has none, with `RunTimeOrCompileTime`. Concretely: `unsafe(expr)` inherits the scope of `expr`, so a
   construct the statement form refuses with LAMA0101 is accepted without a word in expression form;
   `with(a, b)` is annotated `RunTimeOrCompileTime` no matter what `a` and `b` are, because neither is an
   `ExpressionSyntax` *child* of the `WithElementSyntax`; and a `union` declaration is annotated
   `RunTimeOrCompileTime`, so `VisitTypeDeclaration`'s "this is not a build-time type so there is no need to
   analyse it" shortcut never fires and the union's run-time members are analysed as though they might be
   compile-time.
2. **The version verifier cannot see a C# 15 construct at all, for two independent reasons.** First, the
   experimental filter deletes the four additions from *every* grammar document before `VersionDetector`
   runs, so no override is generated into `RoslynVersionSyntaxVerifier.g.cs`: a template using one of them
   raises no LAMA0232 and leaves `MaximalUsedVersion` untouched, so `CompiledTemplateAttribute` and
   `UsedApiVersion` under-report and LAMA0282 never warns the consumer project. Second, even after the filter
   is lifted, `SupportedCSharpVersions.ToLanguageVersion` maps both `V5_0_0` and `V5_10_0` to `CSharp14`, so
   a `V5_10_0`-pinned node compares as C# 14 and is accepted in a project whose `LangVersion` is 14.
   **This map must be corrected in the same change that lifts the filter, or the whole version-checking
   mechanism silently passes.**
3. **`PartialUpdate` and the generated `Transform*` drop an un-generated optional field**
   (`Generator.cs:788-799`, and the generated output at
   `.generated/5.0.0/.../SyntaxNodePartialUpdateExtensions.g.cs:1085-1093` and
   `.generated/5.0.0/.../MetaSyntaxRewriter.g.cs:3170-3183`). Roslyn keeps the old `Update` overload when it
   adds an optional field, so the generated three-argument call still compiles and **silently discards
   `node.Name`**. A labelled `break` in a run-time template body comes out of the template compiler as an
   unlabelled `break` — code that compiles and jumps to the wrong place.
4. **`HasAnyYieldVisitor` uses an allow-list of statement kinds**
   (`Templating/TemplateExpansionContext.cs:799-846`). `DefaultVisit` descends only into children whose kind
   is one of 24 named statement kinds. A statement kind not on that list is not descended into, so a
   `yield return` nested inside it is invisible, and the iterator detection that drives
   `CreateReturnStatement` and `AddYieldBreak` takes the non-iterator path. C# 15 adds no statement kind, so
   this is latent rather than immediate, but it is the clearest allow-list-shaped hazard in the subsystem.
5. **`TemplatingCodeValidator.Visitor` has no default for a new declaration kind**
   (`Templating/TemplatingCodeValidator.Visitor.cs:95-137`). `_currentScope` is set only by the explicit
   `Visit<Kind>Declaration` overrides. For a declaration kind with no override, the visitor walks the body
   with whatever `_currentScope` the enclosing context left, and if that is null it returns at 134-137
   without checking a single reference. C# 14's `ExtensionBlockDeclarationSyntax` is already in this
   position; `UnionDeclarationSyntax` would join it. The failure mode is that compile-time code inside such
   a declaration is never rejected, and the error surfaces much later as a `MissingMethodException` or a
   broken compile-time assembly.
6. **`SyntaxBuilderImpl` parses without parse options**
   (`Templating/Expressions/SyntaxBuilderImpl.cs:71,80`, through `SyntaxGeneration/SyntaxFactoryEx.cs:367-382`).
   The running Roslyn's default `LanguageVersion` applies, which on the 5.10 variant is C# 15, not
   `SupportedCSharpVersions.Latest`. Text handed to `ExpressionBuilder` or `StatementBuilder` that uses a
   C# 15 construct parses without a diagnostic, is injected into the target compilation, and fails there with
   a compiler error attributed to generated code. The same call shape appears at
   `TemplateSyntaxFactoryImpl.cs:79,646` and `Expressions/DurableExpression.cs:66,99`.
7. **`RoslynVersionSyntaxVerifier.VisitVersionSpecificField` and generalising fields**
   (`Templating/RoslynVersionSyntaxVerifier.cs:55-75`). The class carries its own recorded limitation: a
   field added in a new Roslyn that returns a concrete value for old code (the
   `UsingDirectiveSyntax.NamespaceOrType` generalisation of `Name`) is always "present", so every use of the
   *old* construct is reported as requiring the *new* language version. That is a false positive rather than
   a silent miss, but it is the same mechanism a C# 15 generalisation would trip. Note also that none of the
   three `VisitVersionSpecific*` methods calls `base.Visit`, so nothing *inside* a version-specific node is
   verified either.
8. **`FlowAnalyzer.NeverContinues` returns false for unknown statements**
   (`Metalama.Framework.Engine/Utilities/Roslyn/FlowAnalyzer.cs:86-88`). Conservative, so it produces a
   redundant `break` rather than a missing one.
9. **The generated `Transform*` for a node missing from the older grammar** (`Generator.cs:401-403`). In the
   Roslyn 5.0 variant, a node introduced in 5.10 has no `VisitFoo` override at all, so `MetaSyntaxRewriter`
   inherits Roslyn's own `VisitFoo`, which rewrites children and returns the node unchanged instead of
   turning it into a syntax-building expression; the node is then spliced verbatim into the compiled
   template. A Roslyn 5.0 host cannot parse a 5.10 construct today, so this is unreachable — until a
   construct is added to the *latest* grammar while an older variant is still shipped.
10. **`MetaSyntaxRewriter` silently passes through an unknown *expression***
    (`Templating/MetaSyntaxRewriter.cs:106-138`). When a node has no generated `Transform<Node>`,
    `this.Visit( node )` returns it unchanged and the result is cast at 136. For an unknown expression the
    cast succeeds and the raw syntax is emitted into the compiled template instead of the `SyntaxFactory`
    call that should build it, so a run-time expression is evaluated at compile time or emitted verbatim.
    For an unknown *member declaration* the `default:` arm at 132 throws, which is loud. **The dangerous half
    is the expression half, which is exactly the shape of `UnsafeExpressionSyntax` and `WithElementSyntax`.**

### 5.4 Advising (ADV)

1. **`AdviceFactory.IntroduceFinalizer` (730-751) has no `ValidateNotExtensionBlock`.**
   `AdviceKind.IntroduceFinalizer` maps to `_introduceRule` (`EligibilityRuleFactory.cs:250-251`), which
   admits `TypeKind.Extension` (121), so introducing a finalizer into an extension block passes validation
   and reaches `IntroduceMethodTransformation`'s `MethodKind.Finalizer` arm (49), which builds a
   `DestructorDeclaration` named after the extension block's own declaration. The result is invalid C#
   emitted without a Metalama diagnostic. There is no `ErrorFinalizerIntoExtensionBlock` test.
2. **`AdviceFactory.IntroduceEvent` — the add/remove overload (1513) has no gate, while its sibling (1478)
   has one at 1490.** `ErrorEventIntoExtensionBlock.cs` exercises only the guarded overload. The unguarded
   overload emits an event declaration into an `extension` block, which C# rejects, again with no Metalama
   diagnostic.
3. **`IntroduceIndexerTransformation` does not override `GetImplicitDeclarations()`.** The moment
   `AdviceFactory.cs:1406` is deleted for C# 15, an introduced extension indexer is injected into the
   extension block but the static implementation methods are never added to the code model, so invokers and
   the linker do not see them. Nothing throws; the code model is simply incomplete.
4. **`ContractExtensionBlockTransformation.GetInsertedStatements` enumerates only `Methods`, `Properties` and
   `Indexers`** (75, 80, 93). A future extension member kind — events, constructors, fields — receives no
   receiver contract and no diagnostic. The `.Where( … !IsStatic )` filters are deliberate and documented,
   but they also mean that if C# ever gives a static extension member a receiver, the contract is silently
   skipped.
5. **`ImplementInterfaceAdvice` skips interface indexers by design** (184-211, with the comment at 196). The
   aspect gets no diagnostic; the interface is declared as implemented while the indexer is not, and the
   failure surfaces as a raw Roslyn CS0535 on generated code. The explicit-specification path does throw
   (780), so only the declarative path is silent.
6. **`AdviceSyntaxGenerator.GetAttributeLists` has no default arm** (40-63). For a declaration kind it does
   not know, it returns the declaration's own attributes and drops any that belong to an implicit
   sub-declaration. Line 61 already carries `// TODO: field-level attributes`.
7. **`IntroduceMemberAdvice` silently downgrades `IsVirtual` twice** (136-141 and 237-243), with the comment
   "Silently ignore IsVirtual when the target type is sealed or a struct". The condition is
   `targetDeclaration.IsSealed || targetDeclaration.TypeKind == TypeKind.Struct`, so a new value-like type
   kind is not covered and a virtual member would be emitted into a type that cannot have one.
8. **`ValidateNotExtensionBlockReceiver` identifies the extension receiver structurally** (538), as
   `IParameter { DeclaringMember: null }`. Any future parameter whose `DeclaringMember` is null for another
   reason is reported as an extension receiver with a wrong message; conversely, if the code model ever gives
   the receiver a non-null `DeclaringMember`, the guard silently stops firing.
9. **`OverrideHelper.ComputeBackingFieldName` checks collisions against `AllFields`, `AllProperties`,
   `AllEvents` and `AllMethods` only** (143-171). It does not consult `AllTypes` or `AllIndexers`, so a
   nested type whose name matches the computed hint produces a genuine C# name collision rather than a
   Metalama diagnostic.
10. **`ContractBaseTransformation.ToDisplayString` falls through to
    `_ => $"unexpected declaration '{target}'"`** (119). Display-only, but it means an unexpected contract
    target reaches introspection and linker-log output as text rather than failing a test.
11. **`_introduceRule` admits exactly `Class or Struct or Interface or Extension`**
    (`EligibilityRuleFactory.cs:117-125`). `TypeKind.RecordClass` and `TypeKind.RecordStruct` exist in the
    enum but are never produced by `SourceNamedTypeImpl.TypeKind`. This is a latent trap: the day the code
    model starts reporting a distinct record kind, every `Introduce*` advice on a record becomes ineligible
    with a message about classes, structs and interfaces, and no code in this subsystem changes.
12. **`Roslyn.5.0.0.props` defines no constant and `updating-roslyn.md` step 12 discourages adding one.** A
    C# 15 syntax factory called unconditionally from `AdviceImpl` compiles against the latest variant and
    throws `MissingMethodException` at run time in a Roslyn 5.0 host, which surfaces as an aspect-level
    LAMA0041 rather than as a supported-version diagnostic.

### 5.5 Linker (LINK)

1. **`AspectReferenceResolver.ResolveExpressionTarget` classifies an unknown assignment as a read**
   (`Linking/AspectReferenceResolver.cs:832-852`). The property and field arms list thirteen assignment
   `SyntaxKind`s explicitly; the fall-through is `PropertyGetAccessor` at 842 and 852. If a new assignment
   operator kind is introduced, an aspect's write to an overridden property resolves to the **getter**
   semantic; the linker then links a read where the aspect wrote, and the generated code compiles.
   **This is the single highest-risk silent failure in the subsystem.**
2. **A new type declaration is never linked.** `LinkerLinkingStep.LinkingRewriter.cs` has no
   `VisitUnionDeclaration`, so `GetMembersForTypeDeclaration` is never called for a union and
   `LinkerRewritingDriver.RewriteMember` is never invoked for its members; `SafeSyntaxRewriter` recurses
   generically and returns them unchanged. Symmetrically,
   `LinkerInjectionStep.Rewriter.VisitMember:1132` means a nested union receives no injections. An aspect
   applied to a member of a union produces a compilation where the override methods exist but the original
   member still contains the original body: **the aspect silently does nothing.**
3. **`CountLabelUsesWalker` under-counts label references**
   (`Linking/LinkerLinkingStep.CountLabelUsesWalker.cs:24-31`). With a labelled `break`, a label referenced
   by `goto L;` once *and* by `break L;` elsewhere has `counter == 1`, so `RemoveTrivialLabelRewriter`
   deletes both the `goto` and the `L:` label, and the remaining `break L;` no longer resolves. This produces
   a broken *generated* compilation rather than a linker exception, and it only fires in design-time or
   `CodeFormattingOptions.Formatted` mode, so it would not reproduce in a plain build.
4. **`LexicalScopeFactory` computes a scope for the wrong type**
   (`Utilities/Roslyn/SyntaxExtensions.cs:113-120` `GetDeclaringType`, which recognises neither
   `ExtensionBlockDeclaration` nor a future `UnionDeclaration` and walks to the parent).
   `LexicalScopeFactory.CreateLexicalScope` (190-197) then seeds the identifier set from the wrong type
   declaration, so names from `TemplateLexicalScope.GetUniqueIdentifier` may collide with names declared in
   the inner type. For a *top-level* union the same path throws at 197, which at least fails loudly.
5. **`LexicalScopeFactory.Visitor` misses a new binding form**
   (`Linking/LexicalScopeFactory.Visitor.cs:31-108`). A new binding form is simply absent from the set of
   thirteen, so `GetUniqueIdentifier` can hand out a name that is already in scope. The result compiles if
   the shadowing is legal, and silently changes which variable the template body reads.
6. **`DiscoverExitFlowingStatements` has no default arm**
   (`Linking/LinkerAnalysisStep.SemanticBodyAnalyzer.cs:268-365`). An unknown statement wrapper is not
   recorded as exit-flowing, so a `return` inside it gets the T1/T3 treatment instead of the simpler T2/T4.
   That direction is conservative and correct, but invisible, showing up only as worse generated code. The
   opposite direction — a construct that stops being exit-flowing, such as an enclosing loop that a labelled
   `break` can leave — would be incorrect and equally invisible.
7. **`AspectReferenceWalker` drops unresolvable references**
   (`Linking/LinkerAnalysisStep.AspectReferenceWalker.cs:108-126`). The comment at 117 says it: "Otherwise we
   will skip this reference completely, which will cause it not to be transformed." If a new expression form
   makes `GetSymbolInfo` return zero or several candidates, the aspect reference is silently left in the
   output. Combined with the fast path at 75-94, this is a two-branch resolution with a silent fall-through.
8. **The injected-node post-processing switch has no default**
   (`Linking/LinkerInjectionStep.Rewriter.cs:578-670`). An injected member whose syntax kind is not one of
   the five listed is added to the target list verbatim (673) and receives no nested injections, no
   member-level transformations and no injected interfaces.
9. **`LinkerInjectedMemberComparer.GetKindOrder` buckets unknown kinds together** (194, returning `10`). Two
   injected members of two different new kinds compare equal on kind and fall through to name, signature and
   accessibility comparison. Output member *order* changes, which breaks the aspect-test baselines rather
   than the code: noisy, not dangerous, but easy to misdiagnose.
10. **`IteratorHelper.IsIteratorMethod` returns `false` for an unrecognised declaration**
    (`CodeModel/Helpers/IteratorHelper.cs:59-65`). `MethodInliner` and `AsyncMethodInliner` then consider the
    method inlineable, and the linker inlines a `yield`-bearing body into a caller that is not a state
    machine. The result is a compile error in the generated code, not a linker exception, and it points at
    the generated file rather than at the cause.
11. **`LinkerSyntaxHelper.IsUnsupportedMemberSyntax` is a two-case whitelist** (16-23). Any *other* malformed
    or unrecognised member proceeds into the `symbol.Kind` switch at `LinkerRewritingDriver.cs:468` and
    throws an `AssertionFailedException`: a crash rather than a graceful skip.
12. **`SymbolReferenceFinder.BodyWalker` indexes only identifiers and invocations** (209, 220). The index
    backs three analyses — caller-attribute fix-ups, get-only auto-property redirection, event-field raise
    redirection. A member reference expressed through a new syntax form that is not an `IdentifierNameSyntax`
    is invisible to all three, and the corresponding fix-up silently does not happen. This is exactly why
    `field` needed its own `AutoPropertyBodyWalker`.
13. **The linker reports almost nothing to the user.** `AspectLinkerDiagnosticDescriptors.cs` defines only
    LAMA0650, LAMA0651 and LAMA0699. Every other unexpected shape is an `AssertionFailedException` or a
    `NotSupportedException`. There is no diagnostic that says "the linker met a construct it does not
    understand", so any new language construct either crashes with an internal-error message or is silently
    ignored.

### 5.6 Syntax generation, serialisation and formatting (SYNGEN)

1. **`NullableSyntaxAnnotationEx` degrades to no annotation**
   (`SyntaxGeneration/SyntaxGeneratorForIType.NullableSyntaxAnnotationEx.cs:20-32`). If
   `Microsoft.CodeAnalysis.CodeGeneration.NullableSyntaxAnnotation` moves or is renamed, both properties
   become `null` and the consumer at `SyntaxGeneratorForIType.cs:49-58,71-80` skips the annotation. Generated
   code then loses its nullable-oblivious versus annotated distinction and the Roslyn simplifier makes
   different decisions. **There is no diagnostic, no log and no assertion**, and because the two Roslyn
   variants are separate builds this can be true in one variant and false in the other.
2. **`ObjectDisplayOptions` value drift** (`SyntaxGeneration/ObjectDisplayOptions.cs:234-266`). A hand-copy
   of a Roslyn internal enum, cast numerically into Roslyn. If Roslyn renumbers, `IncludeTypeSuffix` (used
   unconditionally for `decimal`) or `UseHexadecimalNumbers` becomes some other option and every literal in
   every generated file is formatted wrongly, while still parsing. The only protection is the comment.
3. **The experimental-node filter drops a stabilised-but-still-marked construct**
   (`Model/TreeReader.cs`). A template using a construct that Roslyn parses but still marks experimental is
   not rejected by `RoslynVersionSyntaxVerifier`, not rewritten by `MetaSyntaxRewriter`, and not hashed by
   either code hasher. The optional-field case is the sharpest: a labelled `break` compiles, the template
   compiles, and the label vanishes from the expanded code.
4. **`LanguageVersionJsonConverter` accepts any integer**
   (`Serialization/LanguageVersionJsonConverter.cs:27`). An unchecked enum cast always succeeds. A manifest
   written by a build that knows `CSharp15` (1500) and read by one that does not yields a `LanguageVersion`
   value that Roslyn's own `IsValid()` rejects and which compares greater than every known version in the
   `>=` tests of `LanguageVersionProvider.cs:223,274`. No exception, no diagnostic.
   `ManifestSerializer.cs:158-172` compounds this: `TryDeserialize` catches `JsonException` and returns
   `false`, and `Deserialize` converts that into a `JsonException` naming only the type, so a manifest that
   deserialises structurally but carries an unknown language version never reaches either path.
5. **Constraint clauses drop silently** (`ContextualSyntaxGenerator.cs:283-342` and `:964-1024`). The
   `TypeKindConstraint` switch has **no default case** and the symbol-based twin is an `if`/`else if` chain
   with no fallback. A constraint form the code model gains but these two do not know is emitted as nothing,
   and the generated declaration is **less constrained** than the original. The asymmetry is already visible:
   the `IGeneric` overload emits `allows ref struct` and the `ITypeParameterSymbol` overload does not.
6. **`TextSpanClassifier` colours nothing for unhandled declarations**
   (`Formatting/TextSpanClassifier.cs:113-259`). A compile-time `interface` today, and a compile-time `union`
   tomorrow, keep their default colouring in the editor and in the HTML output. Design-time only, cosmetic,
   and invisible in tests unless a classification baseline covers the construct.
7. **`SafeIdentifier` and contextual keywords** (`SyntaxGeneration/SyntaxFactoryEx.cs:137,168`).
   `SyntaxFacts.GetKeywordKind` covers reserved keywords only, so a declaration named `field`, `record`,
   `required`, `union` or `closed` is emitted unescaped. Whether that is wrong depends on position.
8. **`RenderInterpolatedString` drops unknown content** (`ContextualSyntaxGenerator.cs:512-544`). The switch
   handles `InterpolatedStringText` and `Interpolation` and has no default, so any third content kind is
   **dropped from the rebuilt list**.
9. **`SyntaxSerializationService` name-based fallback** (`SyntaxSerializationService.cs:196-200`). It falls
   back to a lookup by `Type.FullName` and explicitly skips `ValidateContractType`, then
   `ConvertCrossAssemblyObject` (328-356) copies fields by name into an uninitialised instance. A type whose
   full name matches but whose field set has drifted is copied partially, silently, with the missing fields
   left at their default values.
10. **`SerializableTypes` reports false positives by design** (`SerializableTypes.cs:106-109`). The
    compile-time check passes and the failure surfaces only at serialisation time as an
    `InvalidOperationException`.
11. **`SyntaxFactoryDebugHelper.NormalizeRewriter` carries an explicitly incomplete allow-list** (128-171,
    with the comment at 135), and `SyntaxFactoryDebugHelper.cs:210-225` swallows every exception into
    `ex.ToString()`. Debug-only, but it is the rendering the meta-syntax round-trip test compares.

### 5.7 Compile time (CT)

1. **Experimental nodes are erased from the version gate** (`Model/TreeReader.cs:37,57`). A template that
   uses `union`, `unsafe(expr)`, `with(...)` in a collection expression, or a labelled `break` or `continue`
   is **not reported** by `RoslynVersionSyntaxVerifier`, `MaximalUsedVersion` is not raised,
   `TemplateSymbolManifest.UsedApiVersion` under-reports, and LAMA0282 is never produced for a consumer on an
   older language version. `docs/updating-roslyn.md:11` records the policy but the policy has no enforcement
   point: nothing rejects experimental syntax, it is merely unmodelled.
2. **A top-level compile-time `union` would not be found at all**
   (`CompileTimeCompilationBuilder.FindCompileTimeCodeVisitor.cs:89-99`). Six declaration forms are
   enumerated and there is no fallback classifying an unknown type declaration. A syntax tree whose only
   compile-time type is such a construct sets `HasCompileTimeCode = false` and the whole file is excluded
   from the compile-time compilation. **The user sees no diagnostic; the aspect simply does not exist.** The
   same shape applies today to an aspect declared inside an `extension` block.
3. **The compile-time language version is not part of any cache key**
   (`CompileTimeCompilationBuilder.ComputeProjectHash` 169-247 and `ComputeSourceHash` 123-167). Upgrading
   the .NET SDK from 10 to 11 changes the compile-time language version once `LanguageVersionProvider.cs:54-60`
   gains an arm, but the project hash is unchanged, so the previously emitted compile-time assembly and its
   manifest are served from the disk cache with the old language version. The failure is a stale compile-time
   assembly, not an error.
4. **`manifest.LanguageVersion ?? SupportedCSharpVersions.Latest` versus `ResolvedLanguageVersion`**
   (`CompileTimeProjectManifest.cs:99-101` versus `CompileTimeProjectRepository.Builder.cs:596` and
   `CompileTimeCompilationBuilder.cs:1355`). `ResolvedLanguageVersion` defaults to `CSharp13` and **has no
   callers anywhere in the repository**; the two places that actually resolve the absent value default to
   `Latest`. So a reference compiled by a Metalama that predates the manifest field is re-parsed as C# 14
   today and would be re-parsed as C# 15 after the bump. The two defaults disagree and one of them is dead
   code, so the intent is not recoverable from the code.
5. **`GetMaxLanguageVersion` caps every Roslyn 5.x at C# 14** (`SupportedCSharpVersions.cs:150-152`). On the
   `msbuild.exe` path, a Visual Studio carrying a Roslyn that supports C# 15 is silently limited to C# 14 and
   the project's `LangVersion` is silently lowered at `LanguageVersionProvider.cs:115-122` with no
   diagnostic. There is no equivalent of LAMA0052 on that path.
6. **`MSBuildProjectOptions.LanguageVersion` swallows an unparseable `LangVersion`** (172-178). A
   `LangVersion` of `15.0` on a host Roslyn that does not know C# 15 returns
   `SupportedCSharpVersions.Latest`. `VerifyLanguageVersion` then sees a supported version and reports
   nothing, while the compile-time compilation is built for a lower language version than the run-time
   compilation. The user gets template compilation errors whose cause is not named.
7. **`PopulateNestedCompileTimeTypes` default arm** (`ProduceCompileTimeCodeRewriter.cs:369-372`). Any member
   kind of a run-time type that is not in the two explicit cases is skipped with the comment "Non-type
   members of a run-time type are always run-time too". A **new type form** nested in a run-time type falls
   into this arm, and a compile-time type declared with it is neither un-nested nor reported. The comment's
   premise stops being true the moment a new type declaration kind exists.
8. **`TransformCompileTimeType` default arm** (`ProduceCompileTimeCodeRewriter.cs:555-558`). An unknown
   member of a compile-time type is copied through without the template compilation, the manifest entry or
   the scope classification that every named case performs; the member survives into the compile-time
   assembly uninterpreted. Compare line 515-516, where an indexer is at least a loud
   `NotImplementedException` — and note that "indexers declared inside an extension block" is one of the two
   no-new-syntax C# 15 features.
9. **`_defaultCompileTimeTargetFrameworks` still names `net8.0`** (`CompileTimeAssemblyLocator.cs:43`). It is
   written verbatim into the generated `TempProject.csproj` restored and built on the user's machine. On a
   machine carrying only the .NET 10 and .NET 11 targeting packs, the `net8.0` inner build is what fails,
   inside a nested build whose output goes to a binary log the user never looks at. The `net8.0` entry serves
   no asset that anything reads, since only the `netstandard2.0` list is consumed.
10. **`RoslynApiVersion` is serialized as a bare integer**
    (`CompileTime/Manifest/TemplateSymbolManifest.cs:31`). The ordinals are assigned by position in
    `GenerateMetaSyntaxRewriter.cs:17-18`. Appending a version is safe; **removing** one from the head of
    `legacyVersionNames` without moving it into `deprecatedVersionNames` shifts every ordinal and silently
    reinterprets the manifests of already-compiled references. `CompileTimeProjectManifest.ManifestVersion`
    does not cover an ordinal change inside an unchanged manifest version.

### 5.8 Design time (DT)

1. **A syntax node or field absent from the variant's grammar snapshot is not hashed.** `BaseCodeHasher`
   derives from `SafeSyntaxWalker`, whose `DefaultVisit` recurses into children but appends nothing to the
   hash, and every generated `Visit<Node>` overrides the base without calling it. So for a node type the
   host's Roslyn produces but the variant's snapshot does not contain, its own tokens contribute nothing to
   the hash. An edit confined to those tokens produces an identical `DeclarationHash`,
   `DiffStrategy.IsDifferent` returns `false`, the syntax tree version is reused, the design-time pipeline
   does not re-run, and **the integrated development environment keeps showing the previous generated code
   and the previous diagnostics. Nothing is logged.** Reachable two ways: the Roslyn 5.0 variant meeting
   C# 15 syntax, and — the live one — the latest variant meeting a Roslyn newer than `5.10.0-1.26365.3`,
   which `RoslynVariantPolicyTests.LatestVersionSelectsThe5100Variant` explicitly permits for `5.11.0` and
   `6.0.0`.
2. **`CompileTimeCodeFastDetector` misclassifies a file, and the wrong hasher runs**
   (`CompileTimeCodeFastDetector.cs:77-83`). A using directive reachable only through a container not in the
   three-item list is not seen, the file is classified as run-time-only, and `RunTimeCodeHasher` is chosen —
   the hasher that deliberately ignores the *content* of `BlockSyntax`, `ArrowExpressionClauseSyntax` and
   `EqualsValueClauseSyntax`. Edits inside a template body then stop invalidating the pipeline, and the user
   sees stale generated code with no error.
3. **A design-time host with no loadable payload variant reports nothing to the editor.**
   `ResourceExtractor.TryCreateInstance` returns `false`, every entry-point shim holds a null implementation
   and degrades to a no-op: `MetalamaDiagnosticAnalyzer.SupportedDiagnostics` returns
   `ImmutableArray<DiagnosticDescriptor>.Empty` and `Initialize` does nothing;
   `MetalamaSourceGenerator.Initialize` does nothing. The only trace is the file written by
   `ReportUnsupportedHost` into the crash-reports directory. The compile-time path is the exception:
   `MetalamaSourceTransformer.Execute` reports LAMA0087 as an *error*. This is deliberate and documented, but
   it means every mistake in the variant table, in the target framework of the Resources project, or in the
   `CoreAssemblyToEmbed` glob is invisible in the editor.
4. **A target-framework literal that stops matching drops every extension, silently.**
   `TargetedAssemblyReference.SatisfiesCurrentProcess` (22-24) compares by string equality and
   `ExtensionLoaderBase.GetExtensionAssemblyPaths` (35-37) filters the sequence. An extension whose props
   file declares `net8.0` yields an empty path list, `DesignTimeExtensionManager.OnProjectDiscovered` (67-71)
   discovers no extension type, and the loop at 73 does nothing. No diagnostic is reported:
   `NullDiagnosticAdder.Instance` is passed at 71. **The Premium repository is in this state today.**
5. **An empty embedded resource set instead of a build error.** `platform-support.md:300`: "The two files
   must move together, and a mismatch produces an empty resource set rather than a build error." The
   `CoreAssemblyToEmbed` and `DesktopAssemblyToEmbed` items are MSBuild globs over a build-output directory;
   a target-framework name that names no directory matches nothing. Lines 305-309 add the second half of the
   trap: a path segment that names a target framework is not always ours.
6. **The "Add aspect" refactoring produces no edit for an unknown declaration kind.**
   `CSharpAttributeHelper.AddAttribute` returns `null` at 190, `AddAttributeAsync` returns `null` at 35-38,
   and the Premium consumer `AddAspectAttributeCodeActionModel.cs:96-99` turns that into
   `CodeActionResult.Empty`. The code action appears in the menu, the user invokes it, and nothing happens.
   Already true for `record`, `record struct` and extension blocks.
7. **A Roslyn internal that moves turns a refresh into a no-op.**
   `AnalysisProcessInvalidationService`: `_diagnosticsRefreshAction` becomes `null` and the `?.Invoke` at 36
   does nothing, so diagnostics are never refreshed after a pipeline run in the language-server host.
   `UserProcessInvalidationService`: `_updateSourceGeneratorsAction` becomes `null` and generated source is
   never regenerated in the Visual Studio user process. `RemoteWorkspaceProvider.TryCreate` returns `false`
   and `OnCompilationResultChanged` returns early. All three log at `Warning` or below and none reports a
   diagnostic; line 67 of `AnalysisProcessInvalidationService` explicitly records that "no export" is the
   normal state in Visual Studio and Rider, **so absence cannot be used as a failure signal.**
8. **A host process tree that changes shape disables the whole cross-process layer.**
   `ServiceHubClientEndpoint.TryGetPipeName` returns `false` at 74-77 with a log line only, `TryStart` then
   returns `false`, and every service that flows through the service hub — CodeLens, preview, the aspect
   explorer, the compile-time editing status — is simply absent from the editor.
9. **Contract-version validation accepts a missing contract**
   (`DesignTimeEntryPointManager.Consumer.cs:30-43`). A candidate that does not declare the contract at all
   yields `Revision == 0` and passes. A future `ContractVersion_2_0` added on one side only is therefore
   accepted rather than rejected, and the mismatch surfaces later as an `InvalidCastException` or a
   `MissingMethodException` rather than as the `ContractVersionMismatchDetected` event that exists for the
   purpose.
10. **A contract service resolved by simple type name returns null for anything unknown**
    (`VersionNeutral/CompilerServiceProvider.cs:34,42`; `VisualStudio/Services/VsUserProcessCompilerServiceProvider.cs:28-44`).
    A Visual Studio extension asking for a contract interface this build does not implement receives `null`
    and must decide for itself what that means.
11. **An unresolvable declaration identifier drops a row from the Aspect Explorer**
    (`AspectDatabaseService.cs:140-143,155-158,172-186`). A declaration kind that `SerializableDeclarationId`
    cannot name, or can name but cannot resolve, is silently omitted. The declaration-kind enum on the wire
    has exactly two members and carries a `[Guid]`, so it is frozen.
12. **An unparseable `LangVersion` silently becomes the latest supported version**
    (`Options/MSBuildProjectOptions.cs:167-183`). A project on `<LangVersion>15</LangVersion>` edited in a
    host whose Roslyn cannot parse `15` is analysed as C# 14. Combined with the absence of any
    language-version check in the design-time pipeline, the design-time experience never tells the user that
    the language version is out of range; only the build does, through LAMA0052.
13. **The design-time pipeline contains a failure per generated file rather than reporting it**
    (`DesignTimeSyntaxTreeGenerator.cs:84-106`). A failure while processing one transformation group is
    caught and routed to `ICompileTimeExceptionHandler` with `canIgnoreException: true`, producing LAMA0049
    as a warning, "and when the service is not registered, the failure is contained but not reported". The
    rationale (issue #1767) is that letting the exception escape makes the generated source of the whole
    project disappear. The consequence for a new language construct is that an `AssertionFailedException`
    from `CreatePartialType` costs the user one generated file, quietly.

### 5.9 Build and packaging (BUILD)

1. **The implicit-`LangVersion` clamp downgrades a `net11.0` project to C# 12**
   (`Metalama.Framework.Package/build/Metalama.Framework.targets:118-121`). The whitelist is an inclusion
   test against three literal versions. The .NET 11 SDK will implicitly set `LangVersion` to `15.0` for a
   `net11.0` project; `'15.0'` is not in the list, so the condition is true and the project is compiled as
   **C# 12** — ten language versions below what the user asked for and two below what Metalama already
   supports. A `MetalamaCheckLangVersion` warning is raised at 243-247, but its text says the version was
   raised "to … the lowest version supported by Metalama Framework", which reads as a *floor* message and
   describes a *ceiling* action. A user reading it will not conclude that C# 14 features have just stopped
   compiling.
2. **The experimental filter silently drops a field from an existing node**
   (`Model/TreeReader.cs:92`), verified against the generated output:
   `.generated/5.0.0/…/RunTimeCodeHasher.g.cs:865-870` hashes only `AttributeLists`, `BreakKeyword` and
   `SemicolonToken`, and `.generated/5.0.0/…/MetaSyntaxRewriter.g.cs:3170-3183` calls the three-argument
   `SyntaxFactory.BreakStatement`. Two silent consequences: design-time incremental staleness, because
   `break loop1;` and `break loop2;` produce the *same* hash and the pipeline serves a stale result with no
   diagnostic; and silent label loss in a template, because `TransformBreakStatement` reconstructs the
   statement through an overload Roslyn keeps for binary compatibility. This is not a defect today, and
   becomes one the moment `SupportedCSharpVersions.Latest` moves past C# 14 without the grammar filter being
   revisited in the same change. **The two edits are far apart and nothing links them mechanically.**
3. **`RoslynApiVersion.V5_10_0` maps to C# 14, so the template version guard passes silently** (see §5.3.2).
4. **`GetLanguageVersionFromDotNetSdk` caps the .NET 11 SDK at C# 14**
   (`LanguageVersionProvider.cs:54-60`). The `>=` makes this silent rather than an exception, and lines 64-71
   then take the minimum of that and the project's own version, so a `net11.0` project with
   `LangVersion=15.0` has its compile-time (template) language version silently reduced to 14.
5. **`_defaultCompileTimeTargetFrameworks` still names `net8.0`** (see §5.7.9).
6. **Path segments that name a target framework and are not ours.**
   `Metalama.Framework.CompilerExtensions.csproj:53-70` holds ten `Include` globs containing `net472` or
   `net10.0`. A glob that matches nothing produces **no error and no item**; the assembly is not embedded,
   `ResourceExtractor` finds no resource at run time, and the design-time payload fails to load with the
   silent-in-Visual-Studio failure mode. There is no `<Error>` guard on any of them. Contrast
   `Metalama.Framework.Workspaces.csproj:117-118` and `eng/src/DesignTimeSolution.cs:104-107`, which do
   guard the equivalent situation.
7. **The `MetalamaPlatformRequirement` matrix already claims `net11.0` that nothing ships.** A `net11.0`
   project gets no LAMA0600 while no package ships a `net11.0` asset,
   `Metalama.Patterns.Wpf` ships `net472;net10.0-windows` only so a `net11.0-windows` application resolves
   `net10.0-windows` unverified, and the .NET 11 SDK produces no warning while `LanguageVersionProvider`
   silently caps the language at C# 14. **The matrix cannot be relied on as a tripwire: it is already green.**
8. **Stale rationales that will mislead the next change** — enumerated in §2.9.
9. **`Metalama.Premium` is a whole wave behind.** It still carries `Roslyn.4.12.0.props` with `Latest.props`
   pointing at 5.0.0; every `ROSLYN_*` symbol the main repository has removed; `RoslynVersion` and
   `RoslynMaxVersion` at 5.0.0; package references to `Metalama.Framework.Implementation.4.12.0` and
   `.5.0.0`, neither of which the main repository will publish for 2027.0; `net8.0` as the Core target
   framework in every project including both `*.Package.Resources.csproj` files; and
   `eng/src/BuildMetalamaPremium.csproj:5` on `net9.0`. Because the payload resources project targets
   `net8.0`, **a Premium extension built today cannot be loaded by a PB-2027.0 design-time host at all**, and
   by the string-equality rule that fails by producing an empty extension list rather than by reporting
   anything.

### 5.10 Backstage (BACK)

1. **Signature verification catches the wrong exception type — this is issue #1860**
   (`Licensing/Licenses/LicenseKeyData.Validation.cs:46-68`). `DSA.Create` on macOS with .NET 11 throws
   `PlatformNotSupportedException`, which derives from `NotSupportedException` and **not** from
   `CryptographicException`, so it escapes `VerifySignature`, escapes `License.TryGetConsumptionProperties`
   (108, no `catch`), escapes `LicenseConsumptionService` (no `catch` anywhere in `Licensing/Consumption/`)
   and reaches the caller. That is a loud failure, which is the better outcome; but the shape of the `catch`
   **invites the wrong fix**. Widening it to `NotSupportedException` would convert "this platform cannot
   verify signatures" into "this license key has an invalid signature", which is the silent wrong answer:
   every paying customer on macOS would be told their key is forged. The distinction has to be made where the
   authority is created, not where the signature is checked. Related dead branch:
   `Infrastructure/StandardDirectories.cs:83` compares `Environment.Version < new Version( 8, 0 )` on macOS;
   under PB-2027.0 no supported runtime satisfies it for the `net10.0` flavour, but it remains reachable from
   the `netstandard2.0` flavour, and removing it without that argument loses a directory migration silently.
2. **The setup web server token permission is applied reflectively and failure is swallowed**
   (`UserInterface/SetupWebServerToken.cs:146-165`). Both the type lookup and the method lookup are
   null-tolerant and the invocation is null-conditional. If .NET 11 moves `System.IO.UnixFileMode`, changes
   the overload set of `File.SetUnixFileMode`, or the assembly is trimmed, the permissions are simply **not
   applied**, no warning is logged, and the token file that authenticates the local setup web server stays
   readable by every local user — precisely the exposure the token was introduced to close (issue #1769).
   There is no assertion, no log, and no test that the call actually happened.
3. **The named-lock service degrades to a process-local lock without failing**
   (`Threading/NamedLockService.cs:82-93,397,432`). `IsMachineWideRefusal` treats `IOException`,
   `PlatformNotSupportedException` and `NotSupportedException` as "this machine cannot provide named objects
   at all" and latches for the lifetime of the process; the catch-all at 397 degrades on anything else; and
   the fallback dictionary is per-assembly-copy, so two loaded copies of the file do not exclude each other
   and cross-process exclusion is lost entirely. This is deliberate and documented and the alternative
   (failing the compilation) is worse, but if .NET 11 changes the exception a named mutex raises on any
   platform, the product keeps building while silently losing cross-process exclusion over the configuration
   files, the tool extraction directory and the crash-dump directory. The only signal is a
   `LockEventReported` event whose filter suppresses it unless tracing is on.
4. **The revocation check is keyed to signature key identifiers 0 and 1** (`License.cs:99-106`). A key signed
   by a new authority — identifier 2, which is what #1864 would introduce — skips the revocation list
   entirely and is accepted, with nothing reported. This is the clearest silent-wrong-answer risk the
   elliptic-curve work would introduce if the condition is not revisited at the same time.
5. **The process manager is not registered on an unrecognised platform**
   (`Extensibility/RegisterServiceExtensions.cs:377-392`, ending `else { // Not supported. }`).
   `IProcessManager` is then absent from the service provider, every caller resolves it with
   `GetBackstageService<…>()?`, and the cleanup commands report success having killed nothing. The same shape
   at 311-322 means `IIdeExtensionStatusService` is registered only on Windows and the recommendation is
   simply never made elsewhere.
6. **The machine identifier falls back to the machine name without distinguishing "unavailable" from
   "unknown"** (`Infrastructure/MachineIdProvider.cs:48-70`). The value feeds the cross-product device hash
   used by the license audit, so a change in how .NET 11 exposes the registry on Windows, or a Linux image
   without `/etc/machine-id`, produces a *different but plausible* device hash rather than an error, and the
   device count silently changes.
7. **The Windows detection heuristics return `null`, which the callers read as "yes"**
   (`UserInterface/WindowsUserDeviceDetectionService.cs:125-145`, with the bare `catch { return null; }` at
   71-99 and 102-123). If the `user32` calls start failing, the device is classified as interactive and the
   product opens a browser window or a toast on a machine that has no user.
8. **The Windows tool zip is extracted on every platform**
   (`Metalama.Backstage.Tools/BackstageToolsExtractor.cs:58-62`). Not a correctness defect, but a failure to
   extract the Windows tool fails the whole extraction on a platform that does not need it.
9. **Package-version drift in the premium licensing build task.**
   `Metalama.Premium/src/Metalama.Licensing.BuildTasks/Metalama.Licensing.BuildTasks.csproj:4` declares
   `net8.0;net472` on a 2027.0 branch. It is an MSBuild task, so a host below the .NET 10 SDK floor would
   load the `net8.0` asset and never report that it is below the baseline.

### 5.11 Patterns (PAT)

1. **`DependencyGraphBuilder.Visitor.VisitIdentifierName` has no symbol-kind filter** (409-437). Any
   identifier that resolves to a symbol enters the dependency chain. A labelled `break` or `continue`
   truncates the chain silently, so a property stops raising `PropertyChanged` for part of its dependency
   set. No diagnostic.
2. **The `SymbolKind` switch in `ValidatePathElement` has no `default`** (144-212). Only `Field`,
   `Property when …` and `Method` are validated. Any other symbol kind passes validation without a
   diagnostic, which is the wrong default for a validator whose whole purpose is to report unanalysable
   constructs.
3. **`RoslynHelper.GetAccessKind` returns `Read` for everything it does not recognise** (75), and the comment
   at 72-73 says so deliberately. A new expression form that makes its operand a write target would be
   classified as a read, so the aspect would treat an assignment as a dependency. The comment's reasoning
   ("In current use cases there's no benefit to having accurate Undefined returns") is what must be revisited
   when the set of expression forms grows.
4. **`SingleOrDefault()` over declaring syntax references** (36), with `Cast<PropertyDeclarationSyntax>()` at
   34. A **partial property**, which C# 13 already allows, makes `SingleOrDefault()` throw
   `InvalidOperationException` from inside the aspect; the neighbouring cast is an `InvalidCastException` for
   any property whose declaring syntax is not a `PropertyDeclarationSyntax`. Crashes rather than silent wrong
   answers, but unhandled ones.
5. **`VisitLocalFunctionStatement` is empty by design** (331-335). A dependency expressed only through a
   local function is invisible, and the comment says so. A new form of nested callable would inherit the same
   blindness without even the comment.
6. **`InpcInstrumentationKindLookup` `default: return InpcInstrumentationKind.None`** (82-83). An `IType`
   shape the lookup does not recognise is reported as "does not implement `INotifyPropertyChanged`", so
   `[Observable]` generates no subscription for it. `IsImplemented()` then maps `None` to `false`, and the
   third state `Unknown` → `null` exists precisely because the author knew a silent `false` was dangerous —
   but the `default` case does not use it.
7. **`ImmutabilityExtensions` `return ImmutabilityKind.None`** (93). Any unrecognised type is "not
   immutable". Conservative and therefore safe for correctness, but silent, and it is what a `union` or a
   `closed` hierarchy will hit.
8. **`GraphBuildingContext` `_ => DependencyAnalysisOptions.Default`** (37-45). A declaration kind that is
   not `ICompilation`, `INamespace`, `INamedType` or `IMember` gets default options, so any fabric-configured
   observability contract on it is ignored without warning.
9. **`CommandAttribute.DiagnosticReporter.cs:19-21`** produces the *method* explanation for every declaration
   kind that is not a property. A user given the wrong reason for a rejected candidate is a silent error in
   the diagnostic itself.
10. **The `#if NET8_0_OR_GREATER` generic-math branch** (`Numeric/NumericRange.cs:382-419`). In the
    `netstandard2.0` and `net472` builds of the aspect assembly, a `[Range]` contract on a generic-math type
    generates **no check at all**: `GeneratePattern` falls to the final `else` at 419, which calls
    `AppendConvertedValueToExpression` on a type it cannot convert. The guard makes the aspect's generated
    code depend on which asset of `Metalama.Patterns.Contracts` the pipeline loaded, and nothing reports the
    difference.
11. **The stale `NuGetAuditSuppress` comment** (`Metalama.Patterns.{Observability,Immutability}.csproj:31`).
    It names "Microsoft.CodeAnalysis.Features 4.12.0" and reasons about the `net8.0` shared framework pruning
    `System.Text.Json`; both premises are false under PB-2027.0, so the suppression keeps working and hides
    whatever the current graph actually contains.
12. **Name reservation via `AllMembers()`** (`ClassicObservabilityStrategyImpl.cs:822`,
    `DependencyPropertyAspectBuilder.cs:237-239`, `CommandNamingConventionMatcher.cs:27-28`,
    `DependencyPropertyNamingConventionMatcher.cs:30-31`). If a future member kind is not enumerated by
    `AllMembers()`, the aspect introduces a colliding member and the collision surfaces as a raw C# error in
    generated code, pointing at the aspect rather than at the cause.
13. **`ContractExtensions.cs:93-108` omits events.** The fabric documents itself accurately as covering
    fields, properties and parameters, but a user who reads it as "all declarations" gets silently incomplete
    coverage, and any new member kind is added to that silent gap by default.
14. **`ImmutableAttribute.cs:57-93` checks only `Fields` and `Properties`.** A mutable indexer or a mutable
    member of a new kind on an `[Immutable]` type is not reported, so the type is declared immutable and
    other aspects — notably the Observability dependency analyser through
    `GraphBuildingContext.IsDeeplyImmutable` (74) — trust that declaration and skip change tracking. **This
    is the one silent gap in Patterns whose consequence propagates into another package's generated code.**

### 5.12 Extensions and tooling (EXT)

1. **`MulticastTargets.Default` is zero, and `HasFlagFast(x, 0)` is `true`.**
   `MulticastTargetsHelper.GetMulticastTargets` returns zero for any declaration kind it does not recognise
   (67), and zero is then consumed by two predicates with opposite semantics: `MulticastAttributeInfo.IsMatch`
   (84) uses `HasAnyFlag`, so an unrecognised kind is **excluded** whenever the attribute names any target;
   `DoesDeclarationKindMatch` (348) uses `HasFlagFast`, so an unrecognised kind **matches every filter**. The
   second is dead code today (`testDeclarationKind` is `false` at both call sites), but enabling it, or
   writing a third caller, turns "I do not know this kind" into "this kind matches everything". The root
   cause is `0` doing double duty as "inherited from the parent attribute" and as "unrecognised", and it will
   not be revealed by any test that only exercises known kinds.
2. **`MatchesTypeKind` returns `false` for anything new** (`MulticastImplementation.cs:177`). An extension
   block today, a union tomorrow, is simply never a multicast target. No diagnostic, no trace, no test
   failure: the aspect is applied to fewer declarations than the user asked for and the output compiles. The
   same shape at 131-153, where `switch ( builder )` has no `default` at all.
3. **The analyzer is compiled against Roslyn 5.0 and executes against Roslyn 5.10 or later.** Two
   consequences: `DurabilityContext.Expressions.GetExpressionVerdict` (64-110) falls through to
   `this.GetVerdict( value.Type )` for any operation shape it does not know, and `GetVerdict` returns
   `DurabilityVerdict.Durable` when the type is `null` (188-193) — which is what an unknown or invalid
   expression form frequently produces, so it is **declared durable**. And
   `ImmutabilityContext.GetVerdictCore:348` returns `ImmutabilityVerdict.Immutable` for anything that is not
   an `INamedTypeSymbol`. Both are silent passes on a correctness analyzer.
4. **A new Roslyn classification type produces an unstyled HTML token**
   (`HtmlCodeWriter.cs:296-303`). The `cs-<token>` classes are emitted without validating the token against
   any list. When Roslyn adds a classification for `union`, `closed`, or a labelled break target, the class is
   emitted, no stylesheet rule matches it, the token renders in the default colour, and nothing reports it.
   The one guard is a golden HTML baseline covering nine constructs, compared against a stylesheet that
   styles only the `cr-` and `diag-` classes.
5. **`Permalink.Format` swallows every exception** (`Metalama.LinqPad/src/Metalama.LinqPad/Permalink.cs:30-38`).
   A declaration kind for which `ToSerializableId` is not implemented produces no permalink and no message;
   the user sees a row with an empty link column and cannot tell it from a row for which a link was never
   expected.
6. **`LinesOfCodeMetricProvider` under-counts partial members** (154-187). Only `IMethodSymbol` aggregates its
   partial definition and implementation parts; a partial property, event or constructor returns
   `symbol.DeclaringSyntaxReferences` from the `default` arm, which for a partial member is one part only.
   The metric is silently low; the `TODO` at 153 records this and was never acted on.
7. **The workspace API omits whole constructs.** `ICompilationSet` exposes seven member categories and
   `Project.Types` (85) recurses only through `INamedType.Types`, so extension blocks are unreachable. A query
   such as "all public methods" over a workspace returns a wrong answer for any project that uses extension
   blocks, and the LINQPad schema, generated by reflection over the same interfaces, shows no sign that
   anything is missing.
8. **`MSBuildInitializer` chooses an SDK older than the project needs** (84). On a machine with the .NET 10
   runtime and both the .NET 10 and .NET 11 SDKs, the .NET 11 SDK is filtered out and the workspace evaluates
   a project that requires .NET 11 SDK targets with the .NET 10 SDK. MSBuild reports the failure as workspace
   diagnostics, which `Workspace.LoadProjectSetCoreAsync` (357-369) logs but does not throw on — the comment
   at 354 says "Throw an exception upon failure because otherwise it's too difficult to diagnose", but the
   code below it only logs. `Metalama.Framework.Workspaces` is `net10.0` with no `RollForward`, so this is
   reachable as soon as a project targets `net11.0`.
9. **The extension loader is an exact string and version match with no report** (see §5.8.4).
10. **`Metalama.Framework.Analyzers` well-known tables drift** (`WellKnownDurableTypes.cs:133-141,218-260`,
    `WellKnownImmutableTypes.cs:253-259`). A Roslyn type that is added or renamed simply does not match, and
    the analyzer falls through to its closing rule, which is `Durable` or `NotImmutable` depending on the
    table. There is a guard for user-declared names (LAMA0879) but it never applies to the built-in tables.
11. **The `.slnx` gap** (`Workspace.cs:309-341`). Fail-loud rather than silent, but it will bite as soon as
    the .NET 11 SDK makes `.slnx` the default solution format.

### 5.13 Test infrastructure (TEST)

1. **The golden-file comparison reparses with the wrong language version**
   (`TestOutputNormalizer.cs:22`). `CSharpSyntaxTree.ParseText( s )` takes no `CSharpParseOptions` and the
   diagnostics are never inspected. Both the **actual** transformed text and the **expected** `.t.cs` text go
   through it and are then whitespace-normalised. If the transformed output contains a construct the default
   parse options do not accept, **both sides are mangled the same way and the comparison passes** — the test
   asserts nothing about the construct it was written for. `@LanguageVersion(preview)` and
   `@LanguageFeature(preview)` do not reach this function at all, so a preview-gated construct is exactly the
   case that hits it. The `.ct.cs` compiled-template path has the same defect.
2. **A `@RequiredConstant` naming an undefined constant skips the test forever** (`TestInput.cs:76-83`).
   There is no validation that the constant is one the build ever defines. Two tests are in this state today:
   `Tests/Aspects/Introductions/InterfaceImplementation/Operator.cs:7` and `Operator_Explicit.cs:7` both
   require `ROSLYN4_4_OR_GREATER`, which is defined **nowhere** in the repository, and both also guard their
   body with `#if NET8_0_OR_GREATER && ROSLYN4_4_OR_GREATER`, so they compile to an empty file and are
   reported as skipped on every leg of every variant. A skip is not a failure, and continuous integration
   does not gate on skip counts. The same hazard applies to `@ForbiddenConstant` (a wrong name never matches,
   so the test always runs) and to `@TargetFrameworks` (a wrong name never matches, so the test always skips).
3. **`@LanguageVersion` on an unrecognised version silently skips** (`TestOptions.cs:687-694,708-715`). Any
   integral value at or above 10 that `LanguageVersionFacts.TryParse` rejects becomes `SkipReason`, not an
   error. **A whole `CSharp15` suite written with `// @LanguageVersion(15)` can be entirely inert while the
   run is green.**
4. **The `.t.txt` program-output snapshot is discarded when the program produces nothing**
   (`AspectTestRunner.SaveResultsAsync:479-519`). When `actualProgramOutput` is blank the committed expected
   file is never read, `expectedProgramOutput` is set to `""`, and `ExecuteAssertions` compares `""` with
   `""` and passes. Three ways to land there: `FindProgramMain` (385-453) returning `null` when there is no
   type named `Program` or no method of the configured name, so a rename silently disables execution; the
   `net48` leg, where `ExecuteTestProgramAsync` is not compiled at all; and `@DisableExecuteProgram`. The
   `.t.cs`, `.i.cs` and `.ct.cs` files are protected by the "verify that all expected files have been
   written" loop at `BaseTestRunner.cs:843-861`; **`.t.txt` is not covered by that loop.**
5. **Orphaned test payload directories that are no longer in any project.** `docs/testing.md:12,179-185`
   describes `Metalama.Framework.Tests.AspectTests.Internals` and `Metalama.Framework.Tests.PublicPipeline`
   as test projects; neither has a `.csproj` any more and neither appears in the solution or the solution
   filter, yet seven payload files survive and are discovered by nothing. Discovery walks from
   `MetalamaTestSourceDirectory`, so sibling directories are outside its reach and nothing reports these as
   missing. Related dead weight: the five `*.4.12.0` test directories, the never-built
   `Metalama.Framework.Tests.Benchmarks.5.0.0` (which sets `BenchmarkRoslynVersion` to `4.14.0` and is absent
   from the solution), and a committed WPF temporary project pinning `net8.0-windows` and absolute machine
   paths.
6. **`VerifyMetaSyntax` reparses with `SupportedCSharpVersions.DefaultParseOptions`**
   (`SyntaxTreeStructureVerifier.cs:32-37`), ignoring the tree's own parse options. For a test running under
   `@LanguageVersion(preview)` or an older `@LanguageVersion`, the comparison is against a differently-parsed
   tree. It is only reachable from the AspectWorkbench, so it does not affect continuous integration — but
   the same pinning at `LinkerInlineAssertionWalker.cs:34-35` **is** on the linker-test path. Separately, a
   node whose new field the generator does not know is rendered without it and compared against an equally
   truncated reparse, which is a **false pass**.
7. **The linker test rewriter relocates members of an unhandled type declaration.**
   `LinkerTestInputBuilder.TestTypeRewriter` handles class, record and struct only. A member declared inside
   an interface, an extension block or a future union is still visited by `VisitMethodDeclaration` (119) and
   friends, each of which does `this._currentTypeStack.Peek().Members.AddRange( … ); return null;` — so it is
   removed from its real parent and appended to the **enclosing** class's member list, or, with an empty
   stack, throws `InvalidOperationException` from `Stack<T>.Peek`. Neither outcome names the cause.
8. **`SupportedPlatform` scenarios encode the matrix by hand.**
   `SupportedPlatform.TestedTargetFrameworks/…csproj:8-10` states in a comment that `net481`, `net11.0` and
   `net11.0-windows` are in the tested matrix but omitted because the build agents lack their targeting
   packs. Nothing verifies that the `TargetFrameworks` list stays in step with
   `Metalama.Framework.props:26-41`. When `MaximumNETCoreAppVersion` moves, the scenario keeps passing while
   covering less; conversely, the five scenarios that *assert* LAMA0600 for `net8.0` and `net9.0` become
   vacuous rather than red if those floors move again.
9. **Documentation drift in `docs/testing.md`.** Line 41 says the `.5.0.0` sibling exists for `Benchmarks`
   (it does, but it is not in the solution and is never built); line 147 documents
   `@TargetFrameworks(net10.0;net472)` while `TestOptions.cs:287` still documents `net8.0;net472`.
10. **There is no automated signal about grammar coverage.** `Utilities/SyntaxCover` reads
    `artifacts/tests/SyntaxCover/**/*.txt`, and nothing in the repository writes those files any more; the
    project targets `netcoreapp3.1`.
11. **There is no automated test anywhere in the repository that loads a project through `MSBuildWorkspace`.**
    `SchemaTests.SchemaWithoutWorkspace` asserts nothing and `SchemaTests.SchemaWithWorkspace` is
    `[Fact( Skip = "Cannot get MSBuildLocator to work." )]`.

### 5.14 Premium (PREM)

1. **A target-framework or Roslyn-version string that matches nothing.**
   `TargetedAssemblyReference.SatisfiesCurrentProcess` compares by exact equality;
   `ExtensionLoaderBase.GetExtensionAssemblyPaths` filters by it and, when nothing matches, returns an empty
   sequence after a single `Trace` log at line 33. `LoadExtensionAssemblies` (56-83) reports
   `CannotLoadExtensionAssembly` only for an assembly that *was* selected and then failed to load. **There is
   no diagnostic for an empty selection.** The observable result is that code fixes, refactorings,
   architecture rules and validation rules all stop working, with a green build and no message. The literals
   at risk are the `net8.0` entries in the four manifest files and the `TargetRoslynVersion` values `4.12.0`
   and `5.0.0` in the same lines. **This is the single most important risk in issue #1913, and it is
   invisible to `Build.ps1 build`.** The only guards are the standalone tests under `src/tests/Standalone/`,
   which are themselves all `net8.0` and must move too or they will test the wrong thing.
2. **`ChangeVisibilityCodeAction` on an unknown declaration form.** `ExecuteAsync` (33-50) visits every
   declaring syntax reference and calls `context.UpdateTree( newRoot, syntaxTree )` unconditionally at 48,
   whether or not the rewriter changed anything. For a declaration form without a `Visit*` override — an
   interface, an indexer, an extension block today, a union tomorrow — `CSharpSyntaxRewriter.DefaultVisit`
   rebuilds the node unchanged, the code fix reports success, and the user sees a light bulb that does
   nothing.
3. **The internal-surface enumeration misses member containers.**
   `ArchitectureExtensions.VerifyInternalsAccess` (152-174) and its duplicate in
   `InternalsUsageValidationAttribute.BuildAspect` (144-152) enumerate internal types, `t.Members()` of
   public types, and internal accessors of public `t.Properties`. `t.Indexers` is missing, so
   `InternalsCanOnlyBeUsedFrom` and `InternalsCannotBeUsedFrom` already fail to protect an internal accessor
   of a public indexer. Nothing reports it: the rule simply never fires, and **a false negative in an
   architecture rule is silent by construction.** C# 15 widens the gap.
4. **`TransitiveValidatorInstance` does not serialise `Granularity` or `IncludeDerivedTypes`** (103-121). A
   validator that crosses a project boundary comes back with `Granularity = ReferenceGranularity.SyntaxNode`
   — the field initialiser at 78, whose comment says "Default value for backward compatibility with
   serialized values" — and `IncludeDerivedTypes = false`. Both consequences are silent: the validator runs
   on the obsolete per-syntax-node path, which costs one user-code call per node instead of one per group;
   and a rule declared with `ReferenceValidationOptions.IncludeDerivedTypes` stops seeing derived types in a
   downstream project. `SideBySideVersionTests.TransitiveValidator` exercises exactly this path, so its
   baseline encodes the current behaviour rather than the intended one.
5. **The `MethodKind` switches fall through** (`ReferenceValidatorQuerySource.cs:56-73`,
   `DynamicReferenceValidatorQuerySource.cs:53-67`). Four values each, no `default`. A validator attached to
   any other accessor-like method is dropped without a message; reaching these switches with an indexer
   accessor produces a validator that is registered and never runs.
6. **The grouping-key switch defaults silently** (`ReferenceValidatorRunner.cs:135-143`, `_ => GetDeclaration`).
   A `ReferenceGranularity` value the switch does not name silently degrades to per-declaration grouping.
   Combined with the previous item, a deserialised transitive validator lands on this arm.
7. **The `net471` floor.** `Metalama.Patterns.Caching.Backends.Azure`, `…Redis` and
   `Metalama.Patterns.Caching.LoadTests` target `net471`, below the PB-2027.0 .NET Framework floor of 4.7.2.
   A `net472` consumer resolves the `net471` asset happily, so this produces no error; it produces an asset
   tested against a runtime we no longer claim to support.
8. **The licensing task selection has no version guard**
   (`Metalama.Licensing/build/Metalama.Licensing.targets:11-18`). Line 11 computes the host runtime version
   and discards it; line 12 chooses `tasks/net8.0` from `$(MSBuildRuntimeType) == 'Core'` alone. Below the
   SDK floor this fails with a raw assembly-load error from `UsingTask`, with no `LAMA`-numbered diagnostic.
   `Metalama.Compiler` solves the same problem with LAMA0622.
9. **The packaging `Include` globs.** Sixteen `TfmSpecificPackageFile` patterns over build outputs, with no
   existence check. A path that no longer exists contributes nothing and raises no error, so a half-renamed
   variant produces a package that is missing an assembly and a `build/*.props` that references it — and by
   item 1, the loader then says nothing.
10. **`Roslyn.5.0.0.props:3` reads an undefined property.** `$(RoslynApiMaxVersion)` is defined nowhere in
    the Premium repository, so `ThisRoslynVersion` evaluates to the empty string in the latest variant.
    Nothing breaks today because every consumer reads a different property, but the value is dead and
    misleading, and a renumbering that starts to depend on it would be silently wrong.
