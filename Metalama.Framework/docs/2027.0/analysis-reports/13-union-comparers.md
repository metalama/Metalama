# 13. The impact of union introduction and union leg introduction on the comparers

Written on 2026-09-04 against the branch `topic/2027.0/26-09-03-update-eng-7e3j07` of `/home/user/Metalama`, with the
local branch `pr1879` read for pull request metalama/Metalama#1879. Every path is relative to `/home/user/Metalama`.
Inputs: sections 5c, 5d and 5e of `Metalama.Framework/docs/2027.0/DECISIONS.md`,
`Metalama.Framework/docs/2027.0/analysis-reports/11-introducing-unions-design.md`, and `FACTS.md`. The facts that the
request supplies are used without re-derivation; Roslyn behaviour is taken from analysis 11, which cites `dotnet/roslyn`
`main`, because no Roslyn that this repository consumes exposes the union application programming interface as
non-experimental.

## Question

What does the introduction of a whole union, and separately the introduction of a union leg, do to the comparers of the
code model and of the engine? Which comparer meets a declaration it cannot key correctly, and what is the fix?

## Answer

Almost nothing. Nine of the named comparers key on a symbol, a declaration reference or a signature, and every one of
them distinguishes two union case constructors correctly, because the case types differ and every signature comparison
in the code base compares parameter types rather than parameter counts. `ConstructorSignatureEqualityComparer` does not
collide the case constructors. `InjectedMemberComparer` never sees a synthesized union member, because the design of
analysis 11 registers those members as builders without injecting syntax. `DeclarationOrderingComparer` does not order
generated members at all; it serves the public `WithDeterministicOrder`. The determinism fix of pull request #1879 is
not needed a second time, because it lives inside the two methods whose kind list a union would have to join, and
because the design forbids overriding a union member in the first place.

Two genuine defects remain, and neither is a comparer of members. The first is that the conversion half of
`DeclarationEqualityComparer` enumerates `op_Implicit` methods only, so an introduced union, which has no symbol, does
not accept the implicit conversion from its case types that Roslyn grants a source union. The second is
`AspectInstanceComparer`, which orders by primary declaration syntax and throws when two aspect targets share a span;
the synthesized `Value` property shares the span of the union declaration.

## The comparer inventory

Found by searching `Metalama.Framework/src` for `Comparer` file names and for declarations of `IEqualityComparer<>` and
`IComparer<>`. Serialization, options, pipeline-identifier and collection-utility comparers
(`ComparerExtensions.cs`, `AnalyzerConfigOptionsComparer.cs`, `PipelineStepIdComparer.cs`, `ValueTupleComparer.cs`,
`StructuralDictionaryComparer.cs`, `ReferenceEqualityComparer.cs`) key on strings, integers or object identity and
never see a declaration; they are listed once here and not carried into the table.

| Comparer | File | Keys on |
| --- | --- | --- |
| `DeclarationEqualityComparer` | `Metalama.Framework.Engine/CodeModel/Comparers/DeclarationEqualityComparer.cs:49-87` | For a declaration, `IRef.Equals(RefComparison.Structural)` (`:69`); for a type, `StructuralDeclarationComparer` (`:81-87`) |
| `DeclarationEqualityComparer.Conversions` | `.../DeclarationEqualityComparer.Conversions.cs:59-63`, `:433-461` | For an implicit conversion without symbols, the `op_Implicit` methods of the participating types |
| `StructuralDeclarationComparer` | `Metalama.Framework.Engine/Utilities/Comparers/StructuralDeclarationComparer.cs:676-712` | The `TypeKind`, then names, namespaces and type arguments |
| `StructuralSymbolComparer` | `Metalama.Framework.Engine/Utilities/Comparers/StructuralSymbolComparer.cs:119-256` | Symbol kind, name, arity, parameter types and modifiers (`:438-486`, `:503-...`), containing declaration |
| `SignatureTypeComparer` | `Metalama.Framework.Engine/CodeModel/Comparers/SignatureTypeComparer.cs:21-22` | A signature: name, arity, parameter types, delegating to `StructuralSymbolComparer` |
| `ConstructorSignatureEqualityComparer` | `Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs:886-938` | An array of (parameter type, `RefKind`), compared with `SignatureTypeComparer` (`:890`, `:915`) |
| `DeclarationOrderingComparer` | `Metalama.Framework.Engine/CodeModel/Comparers/DeclarationOrderingComparer.cs:22-95` | Depth, containing declaration, name, signature, type structure |
| `SignatureOrderingComparer` | `.../SignatureOrderingComparer.cs:22-79` | Parameter count, then each parameter's `RefKind` and type |
| `TypeOrderingComparer` | `.../TypeOrderingComparer.cs:23-104` | `TypeKind`, namespace, declaring type, name, arity, type arguments |
| `InjectedMemberComparer` | `Metalama.Framework.Engine/Linking/LinkerInjectionStep.LinkerInjectedMemberComparer.cs:19-190` | Declaration kind, name, signature (methods only, `:83-91`), accessibility, advice ordering indices, then the syntax text (`:190`) |
| `AspectInstanceComparer` | `Metalama.Framework.Engine/Pipeline/ExecuteAspectLayerPipelineStep.cs:198-275` | The primary declaration syntax of the target: file path then span start (`:213-242`), otherwise `ToString()` (`:272`) |
| `AspectReferenceTargetEqualityComparer` | `Metalama.Framework.Engine/Linking/AspectReferenceTargetEqualityComparer.cs:24-27` | A symbol, within one compilation (`SafeSymbolComparer`) |
| `IntermediateSymbolSemanticEqualityComparer` | `.../IntermediateSymbolSemanticEqualityComparer.cs:26-35` | A symbol plus a semantic kind |
| `InliningContextIdentifierEqualityComparer` | `.../InliningContextIdentifierEqualityComparer.cs:26-32` | An inlining identifier plus a symbol |
| `TransformationLinkerOrderComparer` | `.../TransformationLinkerOrderComparer.cs:16-40` | `AdviceOrderingIndices` only |
| `DeclarativeAdviceSymbolComparer` | `Metalama.Framework.Engine/Aspects/DeclarativeAdviceSymbolComparer.cs:53-81` | Symbol kind, method kind, name, then `StructuralSymbolComparer.Default` |
| `MemberComparer<T>` | `Metalama.Framework.Engine/Utilities/Comparers/MemberComparer.cs:20-73` | Name, static flag, declaration kind, parameter types |
| `SafeSymbolComparer` | `.../SafeSymbolComparer.cs:24-38` | `SymbolEqualityComparer` within one validated compilation |
| `RefFactory.SymbolCacheKeyComparer` | `Metalama.Framework.Engine/CodeModel/References/RefFactory.SymbolCacheKeyComparer.cs:14-20` | A symbol, a target kind and a generic context |
| `InheritableAspectInstance.ByTargetComparer` | `Metalama.Framework.Engine/Aspects/InheritableAspectInstance.ByTargetComparer.cs:21-42` | The target reference, compared structurally |
| `ConstructorUpdatableCollection.MemberRefComparer` | `.../UpdatableCollections/ConstructorUpdatableCollection.cs:19` | `RefEqualityComparer<IConstructor>.Default`; the collection itself is bucketed by ordinal name (`NonUniquelyNamedUpdatableCollection.cs:24-46`) |
| `MethodUpdatableCollection`, `IndexerUpdatableCollection`, `TypeUpdatableCollection`, `ExtensionBlockUpdatableCollection` | `.../MethodUpdatableCollection.cs:17`, `IndexerUpdatableCollection.cs:18`, `TypeUpdatableCollection.cs:21`, `ExtensionBlockUpdatableCollection.cs:22` | The same reference comparer, per collection |
| `AllMemberOrNamedTypesCollection.Comparer` | `Metalama.Framework.Engine/CodeModel/Collections/AllMemberOrNamedTypesCollection.cs:40-45` | `MemberComparer<T>` through `CompilationContext.cs:95-110`, used to deduplicate inherited members |

Two supporting mechanisms are not comparers but decide what the comparers above receive, and both are relevant.

`SymbolRef.Equals` (`Metalama.Framework.Engine/CodeModel/References/SymbolRef.cs:191-224`) refuses every reference that
is not a `SymbolRef` (`:197-200`) and otherwise delegates to the symbol comparer chosen by
`RefExtensions.GetSymbolComparer` (`RefExtensions.cs:177-190`). `IntroducedRef.Equals`
(`.../IntroducedRef.cs:232-266`) is the mirror image and compares builder data. A builder therefore never equals a
symbol at the reference level. `DurableRef.Equals` (`.../DurableRef.cs:134-156`) is the one that does equate them,
because it compares the identifier string that `SerializableDeclarationIdProvider.TryGetSerializableId`
(`Metalama.Framework.Engine/SerializableIds/SerializableDeclarationIdProvider.FromDeclaration.cs:94-120`) builds through
`DocumentationIdHelper.CreateDeclarationId`, from the code model rather than from a symbol.

`SyntaxRef` (`.../SyntaxRef.cs:89-118`) is the only reference that keys on a syntax node, and it is constructed at
exactly one site, `SyntaxAttributeRef.cs:46`, for the lazy resolution of an attribute. No union member reaches it.

## Introducing a whole union

The operation registers, per analysis 11, a `NamedTypeBuilderData` for the union, and one `ConstructorBuilderData` per
case plus one `PropertyBuilderData` for `Value`, through a transformation shaped like
`IntroduceNamespaceTransformation`, which implements `IIntroduceDeclarationTransformation` and not
`IInjectMemberTransformation`.

### The synthesized member has no declaring syntax: refuted for every comparer of members

`PrimarySyntaxNodeHelper.GetPrimaryDeclarationSyntax` returns null for anything that is not symbol-based
(`Metalama.Framework.Engine/CodeModel/Helpers/PrimarySyntaxNodeHelper.cs:17-22`), and
`SymbolExtensions.GetPrimarySyntaxReference` returns null when `DeclaringSyntaxReferences` is empty
(`Metalama.Framework.Engine/Utilities/Roslyn/SymbolExtensions.cs:400-404`). Neither result reaches a comparer of
members, because no comparer of members reads it. `DeclarationEqualityComparer` reads a reference
(`DeclarationEqualityComparer.cs:69`); `StructuralSymbolComparer` reads symbol properties (`:119-256`);
`InjectedMemberComparer` reads declaration kinds, names and signatures. The hazard does not exist for them.

It does exist for `AspectInstanceComparer`, which is the one comparer that keys on a syntax node, and which is
described separately below.

### Two case constructors differ only in their parameter type: refuted

`ConstructorSignatureEqualityComparer` compares a signature element by element and calls `SignatureTypeComparer` on
each parameter type (`DesignTimeSyntaxTreeGenerator.cs:915-918`), and its hash combines the hash of each parameter type
(`:927-937`). `SignatureTypeComparer` compares two named types by their original definitions and their type arguments
(`SignatureTypeComparer.cs:105-110`) and hashes a named type by its metadata name (`:143-145`). Two case constructors
`Result(Document)` and `Result(Error)` therefore compare unequal and hash differently. The comparer does not collide
them.

The same holds for every other signature comparison: `StructuralSymbolComparer.CompareMethods` ends in
`CompareParameters` (`StructuralSymbolComparer.cs:485`) and its hash folds in the hash of each parameter type
(`:818-835`); `SignatureOrderingComparer` compares each parameter type through `TypeOrderingComparer`
(`SignatureOrderingComparer.cs:47-58`); `MemberComparer<T>` compares parameter types for a constructor explicitly
(`MemberComparer.cs:46-70`). No comparer in the inventory keys a constructor on its parameter count alone.

One residual, of low probability and shared with every other overload set. `StructuralSymbolComparer.Default`
(`:19-27`) does not carry `StructuralComparerOptions.ContainingAssembly`, so two case types with the same name in the
same namespace but in two different assemblies compare equal. That is a pre-existing property of the comparer, it is
not introduced by unions, and the `IncludeAssembly` variant (`:29-38`) exists for callers that need the distinction.

### The design-time constructor generator is not confused by the case constructors

`CreateInjectedConstructors` (`DesignTimeSyntaxTreeGenerator.cs:525-660`) first indexes every constructor of the
initial type into `existingSignatures` (`:539-542`), then skips any constructor whose final signature is already in that
set (`:583-587`). For a union, the synthesized case constructors are indexed there, so the generator emits nothing for
them. Nothing in this method depends on their having syntax.

### A builder in one compilation version and a symbol in the next

Within one pipeline run, no comparer has to equate them, and none does: `SymbolRef.Equals` and `IntroducedRef.Equals`
each return false for the other kind (`SymbolRef.cs:197-200`, `IntroducedRef.cs:239-242`). The introduction pipeline
never re-reads the final model from Roslyn, which analysis 11 states and which is why the members have to be registered
as builders at all.

Across runs, the equation is done by `DurableRef`, on the identifier string (`DurableRef.cs:150-165`). The identifier
of a builder is produced by the same code path as the identifier of a symbol, from the code model
(`SerializableDeclarationIdProvider.FromDeclaration.cs:94-120`), so a `ConstructorBuilderData` for `Result(Document)`
and the symbol `SynthesizedUnionCtor` that the compiler later produces yield the same documentation identifier,
`M:Result.#ctor(Document)`. The equation therefore works, and it works for a member the compiler synthesizes exactly as
for a member a builder declares, because the identifier is computed from the signature and never from a syntax node.

The caveat is the one `DocumentationIdHelper` already records: an identifier can match several declarations, in
particular for constructors, because it is written from the source signature
(`Metalama.Framework.Engine/SerializableIds/DocumentationIdHelper.cs:71-75`). That is a property of constructors in
general and the case constructors of a union do not aggravate it, since their signatures differ from each other.

### Ordering

`InjectedMemberComparer` is used at one site, `LinkerInjectionStep.TransformationCollection.cs:326`, to sort the
members injected at one insert position. The synthesized union members are never injected, so the comparer never
receives them. Members that an aspect introduces into an introduced union are ordinary injected members and are ordered
as they are anywhere else.

`DeclarationOrderingComparer` does not order generated members. Its only consumer is the public
`WithDeterministicOrder` (`Metalama.Framework/src/Metalama.Framework/Code/DeclarationExtensions.cs:464-476`, comparer
read at `:474`), which an aspect author calls to make their own generated code independent of source order. Applied to
the constructors of a union it is a total order: same depth, same containing declaration, same name, then
`SignatureOrderingComparer` separates them by parameter type. The premise that this comparer decides the order of
generated members is not correct, and no fix is needed for it.

The determinism fix of pull request #1879 is not needed a second time. On `pr1879`,
`LinkerRecordHelper.GetSynthesizedMethodOverrideTargets` and `GetSynthesizedPropertyOverrideTargets`
(`Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerRecordHelper.cs:37-58` and `:64-85`) collect the
overridden members whose primary declaration syntax is a record declaration and sort them with
`StructuralSymbolComparer.Default`, with the comment that "these members have no declaration in source whose position
could order them". Two facts make the union case different. First, both methods enumerate
`InjectionRegistry.GetOverriddenMembers()`, so they see a member only when an aspect overrides it, and analysis 11
plans to refuse exactly that for `Value` and the case constructors through `CanBeDeclaredExplicitly`. Second, if that
plan is reversed and the kind lists gain `SyntaxKind.UnionDeclaration`, the sort is inside the same two methods and
applies to the union members with no further change, and `StructuralSymbolComparer.Default` separates the case
constructors by parameter type as shown above. The correct statement is that the union work depends on #1879 for the
eligibility concept, not for a repeat of its determinism fix.

`TransformationLinkerOrderComparer` compares only `AdviceOrderingIndices` (`:33-36`) and already documents that
remaining ties do not affect the linker output (`:38-39`). Registering the synthesized members as transformations gives
them distinct advice ordering indices in the order the advice created them, which is the order of the case list.

### The genuine defect: the conversion from a case type to an introduced union

`DeclarationEqualityComparer.IsConvertibleTo` answers an implicit conversion from Roslyn when both types have symbols
(`DeclarationEqualityComparer.cs:158-167`), and otherwise from its own reimplementation
(`:181`, into `DeclarationEqualityComparer.Conversions.cs`). The reimplementation answers a user-defined implicit
conversion by enumerating the `op_Implicit` methods of the participating types
(`DeclarationEqualityComparer.Conversions.cs:59-63`, `:337-343`, `:433-437`). Analysis 11 establishes that the
conversion from a case type to a union is a language conversion computed from `UnionFactoryMethods`, and not an
`op_Implicit` an aspect can supply.

An introduced union has no symbol, so the symbol path is unavailable and the reimplementation answers. It will answer
false where Roslyn answers true. Every eligibility check, template type check and advice validation that asks whether a
case type is convertible to the introduced union gets the wrong answer, and the failure is silent. This is a real
addition to the work of half one, it is not in the work breakdown of analysis 11, and it is exactly what
`ComparerAgreesWithRoslynTests` is built to catch.

The fix is a union arm in `ComputeApplicableUserDefinedImplicitConversionSet` (`:415-490`), or earlier in
`HasConversion`, that treats each element of the union's case set as a source of an implicit conversion to the union.
For an introduced union the case set is the builder's case list; for a hand-written `[Union]` type it is the public
single-parameter constructors, which is what the request records as the derivation of `UnionCaseTypes`. Size M.

### The other defect: `AspectInstanceComparer` and the span of the union declaration

`AspectInstanceComparer` orders the aspect instances of one layer by the primary declaration syntax of their targets
(`ExecuteAspectLayerPipelineStep.cs:213-242`, used at `:109`). When two targets have the same file and the same span
start and are not equal, it takes one special arm for implicitly declared record methods (`:249-265`) and otherwise
throws `AssertionFailedException` (`:267`).

Analysis 11 records that `SynthesizedUnionValuePropertySymbol` derives from `SourcePropertySymbolBase` and is
constructed with the union declaration. If it exposes that declaration as a declaring syntax reference, then the
`Value` property and the union type itself return the same syntax node from `GetPrimaryDeclarationSyntax`, so they have
the same span start, they are not equal, and neither is a method. The comparer throws. It takes two aspect instances of
one aspect class in one layer, one on the union type and one on its `Value` property, which a fabric can produce.

This is a hazard of reading a union, not of introducing one: for an introduced union both targets are builders, both
return null (`PrimarySyntaxNodeHelper.cs:21`), and the comparer falls to the ordinal comparison of `ToString()`
(`:272`), which is deterministic because `DisplayStringFormatter.VisitConstructor` prints the parameter list
(`Metalama.Framework.Engine/CodeModel/Visitors/DisplayStringFormatter.cs:152-163`). It is nevertheless in scope for
this release, because the reading half ships. It is the same class of defect that the record arm at `:249-265` was
added to fix, and the fix is the same shape: extend that arm to compare any two implicitly declared members of the same
type by signature rather than restricting it to methods of a record. Size S.

I have not verified that the `Value` property exposes a declaring syntax reference. What settles it is one compilation
against Roslyn 5.12 reading `DeclaringSyntaxReferences` of the `Value` property of a union. The corresponding record
property, `EqualityContract`, appears not to expose one, which is why `pr1879` writes
`propertySymbol.GetPrimaryDeclarationSyntax() ?? propertySymbol.ContainingType?.GetPrimaryDeclarationSyntax()`
(`LinkerRecordHelper.cs:75-76` on `pr1879`).

## Introducing a leg

### Into a `union` declaration

The operation rewrites `UnionDeclarationSyntax.ParameterList` of the part the user wrote. Nothing in that rewrite
passes through a comparer of declarations: it is a syntax edit in
`ApplyMemberLevelTransformationsToPrimaryConstructor`, and the routing defect that analysis 11 identifies is in
`TransformationCollection.GetOrAddMemberLevelTransformations`, which keys on
`symbolRef.Symbol.GetPrimaryDeclarationSyntax()`. That is a syntax-node dictionary key, not a comparer, and analysis 11
already books the work.

The comparer-relevant consequence is what the code model does with the new constructor.
`CompilationModel.AddTransformation` returns early for a transformation whose `Observability` is `None`
(`CompilationModel.Members.cs:222-226`) and otherwise increments `Revision` (`:228`) before registering the builder
(`:243-248`). A leg transformation that is modelled as a pure linker rewrite, with no observability and no
`ConstructorBuilderData`, leaves the union's `Constructors` collection without the new case constructor, so the code
model and the emitted code disagree. The precedent that analysis 11 names, parameter introduction into a partial
constructor, does register its parameter (`:258-261`), so the leg must register a constructor builder in the same way.
This is a design instruction rather than a comparer defect, and it is the prerequisite of everything below.

### The effect on the cached and memoized collections

Once the leg is registered as a `ConstructorBuilderData`, `AddDeclaration` routes it to
`GetConstructorCollection(...).Add(...)` (`CompilationModel.Members.cs:415-419`). The collection is bucketed by ordinal
name (`NonUniquelyNamedUpdatableCollection.cs:24-46`, `:90-127`), so all constructors share one bucket and the new one
is appended to an array rather than inserted into a set. No deduplication occurs on insertion, and
`RefEqualityComparer<IConstructor>.Default` is used only by `Remove` (`:151`, `:177`). Adding a leg therefore cannot
collide with an existing case constructor at this level, and adding a duplicate case is not rejected here either, which
is consistent with the open question that analysis 11 records about duplicate cases.

The dictionary that holds the per-type collections is keyed by `IFullRef<INamedType>`
(`CompilationModel.Members.cs:24-40`, resolved in `GetMemberCollection` at `:52-91`), and the key is normalised to the
definition reference at `:64-68`. A union is one named type whether or not a leg was added, so the key does not change
and no cache is orphaned.

The one cache that could go stale is `AllMemberOrNamedTypesCollection`, which memoizes a `HashSet` of members
(`AllMemberOrNamedTypesCollection.cs:41-45`, `:64-77`). It is invalidated by comparing the compilation `Revision`
(`:66-73`), which `AddTransformation` increments (`CompilationModel.Members.cs:228`). It also never contains
constructors: the derived collections are `AllMethodsCollection`, `AllPropertiesCollection`, `AllFieldsCollection`,
`AllEventsCollection`, `AllIndexersCollection` and `AllTypesCollection`. Adding a leg therefore does not affect it, and
the deduplication comparer it uses, `MemberComparer<T>` (`CompilationContext.cs:95-110`), is not reached.

### The deduplication between the initial and the final compilation

The one place that deduplicates members across the two compilation versions is `CreateInjectedConstructors`
(`DesignTimeSyntaxTreeGenerator.cs:525-660`), through `ConstructorSignatureEqualityComparer`. For a `union`
declaration, analysis 11 establishes that the design-time pipeline emits nothing at all, so this code path produces
nothing for the leg and the comparer is not exercised. For the hand-written `[Union]` form, the leg is an introduced
constructor and the path is the ordinary one: the initial signatures are indexed at `:539-542`, the introduced
constructor is indexed at `:545-556`, and a constructor whose final signature is already present is skipped at
`:583-587`. The comparison is by parameter type throughout, so a new case whose type differs from every existing case
is emitted, and a duplicate case is silently skipped. The silent skip is the behaviour that the duplicate-case
question of analysis 11 has to decide; it is a product decision, not a defect of the comparer.

### Into a hand-written `[Union]` type

This is ordinary constructor introduction. It touches `InjectedMemberComparer`, which orders two injected constructors
of the same name and accessibility by `AdviceOrderingIndices` (`LinkerInjectedMemberComparer.cs:158-163`), because the
signature comparison at `:83-91` is applied to methods only. Two constructors introduced by two advices have distinct
ordering indices, so the order is deterministic. Two constructors introduced by one advice with identical indices would
fall to the ordinal comparison of the syntax text at `:190`, which is deterministic as well although arbitrary.
Extending the signature arm at `:83-91` from `DeclarationKind.Method` to include `DeclarationKind.Constructor` would
make the order signature-based and therefore stable against a change of advice order; that is a small optional
improvement and not a defect.

## The equality of the union type itself

No impact, and it must not be conflated with the above. Nothing in Metalama depends on the `Equals` or `GetHashCode`
of a target type. The caching pattern builds its cache key by formatting, not by hashing: `DefaultFormatter<TValue>`
appends the result of `value.ToString()` when the type overrides it, and otherwise appends the type name
(`Metalama.Patterns/src/Flashtrace.Formatters/Implementations/DefaultFormatter.cs:107-147`), the test for a custom
`ToString` being `DefaultFormatterHelper.HasCustomToStringMethod`
(`.../Implementations/DefaultFormatterHelper.cs:14-21`). A union used as a cached method parameter therefore behaves
exactly as any other struct: if it has no custom `ToString`, every value of it formats to the same text and the cache
key does not distinguish two values. That is a pre-existing property of the caching pattern for structs, it is not a
comparer question, and a union does not make it worse or better. Whether Roslyn synthesizes a `ToString` for a union
was not established.

The comparers of the code model compare a union type as they compare any struct:
`StructuralDeclarationComparer` routes `TypeKind.Struct` to the named-type arm (`:693-694`) and no comparer in the
inventory reads `IsRecord` or `IsUnion`.

## Tests

`ComparerAgreesWithRoslynTests` (`Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/CodeModel/
ComparerAgreesWithRoslynTests.cs`) is the test that must gain a union case, and it is the test that would have found
the conversion defect. Its corpus is a set of fields whose types are compared pairwise (`:44-81`), and every pair is
checked for equality, for hash consistency and for the identity and implicit conversions, twice: once as the code model
answers by delegating to Roslyn, and once with `bypassSymbols: true`, which is the path an introduced type takes
(`:150-172`). Adding `public union Pet( Cat, Dog );` and a field of each case type to the corpus asserts that
`IsConvertibleTo( Cat, Pet, ConversionKind.Implicit )` gives the same answer with and without symbols, which is exactly
the assertion that fails today.

`DeclarationComparerTests` (`.../CodeModel/DeclarationComparerTests.cs`) is the second test, and it should gain a
`ConversionKindImplicit` case in the shape of the existing one (`:134-197`), which already runs both
`bypassSymbols: false` and `true` from `[InlineData]`. The union case asserts that a case type converts implicitly to
the union, that the union does not convert implicitly to a case type, and that a type which is not a case does not
convert.

`StructuralSymbolComparerTests` (`.../Utilities/StructuralSymbolComparerTests.cs`) does not need a union case for the
hazards examined here, because the comparer distinguishes the case constructors by the parameter-type comparison it
already exercises. A case would be worth adding only to pin the statement that the two case constructors of a union
compare unequal and hash differently, which is cheap and is the assertion the report rests on.

No comparer test covers `AspectInstanceComparer`. Its defect is reached only through the pipeline, so it is pinned by
an aspect test with a fabric that adds one aspect to a union and to its `Value` property, in the union test directory
that analysis 11 proposes.

## Verdict table

| Comparer | Keys on | Introducing a union | Introducing a leg | Fix | Size |
| --- | --- | --- | --- | --- | --- |
| `DeclarationEqualityComparer` (declarations) | A declaration reference, structurally | No impact | No impact | None | - |
| `DeclarationEqualityComparer` (conversions, `bypassSymbols`) | The `op_Implicit` methods of the participating types | Wrong: no implicit conversion from a case type to an introduced union | Wrong for a leg added to an introduced union | A union arm in the conversion set, reading the case set from the builder or from the public single-parameter constructors | M |
| `StructuralDeclarationComparer` | `TypeKind` then names and type arguments | No impact; a union is `TypeKind.Struct` | No impact | None | - |
| `StructuralSymbolComparer` | Symbol kind, name, arity, parameter types | No impact; separates the case constructors | No impact | None | - |
| `SignatureTypeComparer` | Name, arity, parameter types | No impact | No impact | None | - |
| `ConstructorSignatureEqualityComparer` | Parameter types and `RefKind`s | No impact; does not collide the case constructors | No impact; a duplicate case is skipped silently, which is a product decision | None, unless the duplicate case must be reported | S if reported |
| `DeclarationOrderingComparer` | Depth, containing declaration, name, signature | No impact; it does not order generated members | No impact | None | - |
| `SignatureOrderingComparer` | Parameter count, `RefKind`s, parameter types | No impact | No impact | None | - |
| `TypeOrderingComparer` | `TypeKind`, namespace, name, type arguments | No impact | No impact | None | - |
| `InjectedMemberComparer` | Kind, name, method signature, accessibility, advice order | No impact; synthesized members are not injected | Ordered by advice order for a hand-written union; deterministic | Optional: extend the signature arm to constructors | S |
| `AspectInstanceComparer` | Primary declaration syntax, file path and span | Not affected for an introduced union; throws for a source union targeted on both the type and `Value` | Same | Extend the same-span arm to any implicitly declared member, comparing by signature | S |
| `AspectReferenceTargetEqualityComparer` | A symbol, one compilation | No impact; a union member cannot be an override target | No impact | None | - |
| `IntermediateSymbolSemanticEqualityComparer` | A symbol plus a semantic kind | No impact | No impact | None | - |
| `InliningContextIdentifierEqualityComparer` | An inlining identifier plus a symbol | No impact | No impact | None | - |
| `TransformationLinkerOrderComparer` | `AdviceOrderingIndices` | No impact | No impact | None | - |
| `DeclarativeAdviceSymbolComparer` | Kind, name, then `StructuralSymbolComparer` | No impact; it orders members of the aspect class | No impact | None | - |
| `MemberComparer<T>` | Name, static, kind, parameter types | No impact | No impact; constructors are not in an `All*` collection | None | - |
| `SafeSymbolComparer` | `SymbolEqualityComparer`, one compilation | No impact | No impact | None | - |
| `RefFactory.SymbolCacheKeyComparer` | Symbol, target kind, generic context | No impact | No impact | None | - |
| `InheritableAspectInstance.ByTargetComparer` | The target reference, structurally | No impact | No impact | None | - |
| `ConstructorUpdatableCollection` and the other updatable collections | Ordinal name buckets; `RefEqualityComparer<T>.Default` on removal | No impact | No impact; the new constructor is appended, the dictionary key does not change | None | - |
| `AllMemberOrNamedTypesCollection` | `MemberComparer<T>`, invalidated by `Revision` | No impact | No impact; constructors are not in it, and `Revision` invalidates it | None | - |
| `DurableRef` (the builder-to-symbol equation) | The documentation identifier | Works; the identifier is computed from the code model, not from syntax | Works | None | - |
| `SyntaxRef` | A syntax node | Not reached; constructed only for attributes | Not reached | None | - |

## Work

Prerequisites of the introduction work, in this order.

1. Register the leg as an observable transformation carrying a `ConstructorBuilderData`, so that the code model sees
   the new case constructor. Without it the code model and the emitted code disagree and every item below is moot.
   This is a design instruction for the leg story rather than a comparer fix. Size S, and it belongs to the leg story.
2. Add the union arm to the conversion reimplementation of `DeclarationEqualityComparer.Conversions`, so that a case
   type converts implicitly to a union that has no symbol. Size M. This is a prerequisite of the introduction work:
   an aspect that introduces a union and then assigns a case value to it, or that validates eligibility against the
   introduced union, gets the wrong answer until it is done. It needs no C# 15 Roslyn member for the introduced-union
   half, because the case list comes from the builder, so it can proceed before Roslyn 5.12.
3. Extend the union case of `ComparerAgreesWithRoslynTests` and `DeclarationComparerTests`. Size S. It cannot run
   before Roslyn 5.12, because the corpus is read from source, so item 2 is written first and pinned afterwards.

Follow-ups, which do not block the introduction work.

4. Fix `AspectInstanceComparer` so that two implicitly declared members of the same type that share a span are ordered
   by signature rather than throwing. Size S. It is a defect of the reading half, it is reachable today for records,
   and it becomes likely with unions because `Value` is public.
5. Optionally extend the signature arm of `InjectedMemberComparer` from methods to constructors. Size S. It makes the
   order of two introduced constructors independent of advice order and it benefits the hand-written union form.
6. Add a union case to `StructuralSymbolComparerTests` pinning that the two case constructors compare unequal and hash
   differently. Size S.

## What could not be verified

1. Whether `SynthesizedUnionValuePropertySymbol` exposes a declaring syntax reference to the union declaration. The
   whole of the `AspectInstanceComparer` finding depends on it. Settled by reading `DeclaringSyntaxReferences` of the
   `Value` property in a compilation against Roslyn 5.12. If it is empty, the finding reduces to the ordinary
   null-syntax path and no fix is needed.
2. Whether the union conversion is reported by `Compilation.ClassifyConversion` as an implicit conversion for a source
   union. The symbol path of `DeclarationEqualityComparer` delegates to it (`DeclarationEqualityComparer.cs:225`), so
   if it does not, the reading half has the same defect as the introduction half and item 2 of the work grows.
   Settled by one call to `ClassifyConversion` against Roslyn 5.12.
3. Whether Roslyn synthesizes a `ToString` for a union. It decides whether a union used as a cached parameter produces
   a usable cache key. It is not a comparer question and it is recorded here only so that it is not lost.
4. Whether a `ConstructorBuilderData` with no injected member survives `LinkerInjectionRegistry`. This is risk 4 of
   analysis 11 and it is not a comparer question, but every conclusion above about the synthesized members of an
   introduced union assumes that the registration succeeds. Settled by the prototype that analysis 11 already
   proposes, which needs no C# 15 Roslyn member.
5. The exact behaviour of the duplicate-case case. Roslyn collapses duplicate case types silently, per analysis 11,
   and `ConstructorSignatureEqualityComparer` skips a duplicate signature silently. Whether Metalama reports it is an
   open product decision, not a fact about the code.
