# Coverage record: the subsystems that no theme document examined

This note records what was read for the completeness review of
[`OPEN-QUESTIONS.md`](../OPEN-QUESTIONS.md), which subsystems were found
unaffected by C# 15 and .NET 11, and on what evidence. One story came out of the review, which is S-31. Every other
subsystem below needs no work, and the paragraph that names it says why.

## The decision that the review asked for: syntax serialization

The completeness review said that syntax serialization deserved a decision rather than a note, because it holds one
serializer per supported type and turns a compile-time value into run-time syntax. The decision is that no serializer
is needed for a union in 2027.0, and the reason is structural rather than accidental.

A compile-time value cannot be an instance of a union type. The symbol classifier gives a named type the neutral
scope only when the symbol is available at compile time, and gives it the run-time scope otherwise
(`Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/SymbolClassifier.cs:781`, `:814-821`). Availability is
decided against the compile-time reference set, which is why a member added to a type that exists in
`netstandard2.0` is classified as run-time only (`SymbolClassifier.cs:907-911`). The compile-time compilation always
targets `netstandard2.0`, which provides neither `System.Runtime.CompilerServices.IUnion` nor the union attribute
(`Metalama.Framework/docs/2027.0/03-code-model-unions-closed.md:673-676`), and section 5 of `DECISIONS.md` keeps
`MetalamaTemplateLanguageVersion` at 14.0. A union type is therefore run-time only in every configuration that
2027.0 ships, and the serializer is never asked to reconstruct one.

If a union type did reach the serializer, the fallback reports a clean diagnostic and never produces wrong syntax.
`SerializableTypes.IsSerializable` tests the intrinsic types, the array element type, the registered contract types,
the enum underlying type, the generic definition with its arguments, and the implemented interfaces
(`Metalama.Framework/src/Metalama.Framework.Engine/SyntaxSerialization/SerializableTypes.cs:56-102`). A union matches
none of them: it is a struct whose only implicit interface is `IUnion`, and `IUnion` is not a registered contract
type. Control reaches `SerializableTypes.cs:104`, which reports `LAMA0200` with the message "A compile-time value of
type '{0}' was used in a context where a run-time value was expected"
(`SyntaxSerialization/SerializationDiagnosticDescriptors.cs:19-27`). The caller substitutes a `default` expression and
lets the compilation continue (`Metalama.Framework.Engine/Templating/TemplateCompilerRewriter.cs:850-864`). No
serializer produces a creation expression for a type it does not recognize, because
`SyntaxSerializationService.TryGetSerializer` resolves only by concrete type, by base type and by implemented
interface (`SyntaxSerialization/SyntaxSerializationService.cs:183-240`), and `Serialize` throws when the resolution
fails (`:277-291`).

Two adjacent points were checked and are also clear. `TypeSerializationHelper.SerializeTypeSymbolRecursive` writes a
`typeof` expression for any type symbol that is not a type parameter
(`SyntaxSerialization/TypeSerializationHelper.cs:14-34`), so `typeof` of a run-time union type is already correct.
The reflection serializers require a symbol for the declaring type of the member and already refuse an introduced
type through the named unsupported feature `IntroducedTypeSerialization`
(`SyntaxSerialization/MetalamaMethodBaseSerializer.cs:33`, `:47`;
`Metalama.Framework.Engine/UnsupportedFeatures.cs:11`). The synthesized members of a union introduced by story S-17
fall under that existing refusal and add no new case.

## Metalama.Framework.EditorExtensions

The project holds two source files, `MetalamaCodeFixProvider.cs` and `MetalamaCodeRefactoringProvider.cs`
(`Metalama.Framework/src/Metalama.Framework.EditorExtensions/`). Neither switches over a declaration kind, a syntax
kind or a type kind. The only switch in either file is over `ProcessKindHelper.CurrentProcessKind`
(`MetalamaCodeFixProvider.cs:24`, `MetalamaCodeRefactoringProvider.cs:22`), and each arm forwards to an
implementation loaded from the design-time assembly of the selected Roslyn variant. A union therefore reaches no code
in this project.

The project is compiled once against `RoslynApiMinVersion` and packed once, not once per Roslyn variant
(`Metalama.Framework.EditorExtensions.csproj:14-17`;
`Metalama.Framework/src/Metalama.Framework.Package/Metalama.Framework.Package.csproj:79`). That does not create the
divergence of section 6 of `DECISIONS.md`, because the project exposes no code model member and answers no question
about the compilation. It only constructs a type from the variant-specific assembly, and the variant is chosen from
the Roslyn version of the host, not from the version this project was compiled against
(`Metalama.Framework/src/Metalama.Framework.CompilerExtensions/ResourceExtractor.cs:633-655`, `:244-246`).

The one defect found in this project is the subject of story S-31.

## Host process detection

The review also names host process detection, whose two copies were said to have diverged on the C# Dev Kit. The divergence
is real. `Metalama.Backstage/src/Metalama.Backstage/Utilities/ProcessUtilities.cs:78-80` and `:102-105` recognize the
C# Dev Kit language server and return `ProcessKind.LanguageServer`, while
`Metalama.Framework/src/Metalama.Framework.CompilerExtensions/ProcessKindHelper.cs:14-59` recognizes none of those
process names and returns `ProcessKind.Other`. Both files carry a comment saying that the logic is duplicated and
that a change in one must be made in the other (`ProcessKindHelper.cs:16-17`, `ProcessUtilities.cs:36-37`).

The divergence has no functional consequence in `Metalama.Framework`. Every switch over the process kind sends an
unrecognized host to a default arm that loads the general implementation:
`MetalamaSourceGenerator.cs:52-58`, `MetalamaDiagnosticAnalyzer.cs:50-56`, `MetalamaDiagnosticSuppressor.cs:43-49`,
`MetalamaCodeFixProvider.cs:50-56` and `MetalamaCodeRefactoringProvider.cs:48-54`. The general implementation is the
correct one for a host that is neither Visual Studio nor Rider. The only other reader of the process kind selects an
assembly loading strategy for the design-time contracts assembly, and it selects it for Visual Studio and Rider only
(`ResourceExtractor.cs:578-583`), which is where the requirement comes from. No story follows.

## The six engine subsystems that need nothing

Aspect ordering, hierarchical options, additional outputs, observers, queries and reflection mocks were each read for
a place that a union, a closed class, an extension indexer or the C# 15 language version could reach. None of them
has one, for the reasons below.

Aspect ordering operates on aspect class names and layer names, never on the code that an aspect targets. The sort
input is a set of match expressions of the form `aspect:layer` (`AspectOrdering/AspectLayerSorter.cs:60-95`), and the
tie-break of the total order compares distances and then aspect names
(`AspectOrdering/AspectLayerSorter.cs:245-262`). No file of `AspectOrdering/` mentions a type kind, a declaration
kind, a syntax kind or a symbol kind. The aspect instance ordering that does throw on a union is
`AspectInstanceComparer.Compare` in `Metalama.Framework.Engine/Pipeline/ExecuteAspectLayerPipelineStep.cs`, which is
a different subsystem and is already story S-16.

Hierarchical options dispatch on `DeclarationKind` when they compute the inherited options of a declaration
(`HierarchicalOptions/HierarchicalOptionsManager.OptionTypeNode.cs:119-184`). A union is
`DeclarationKind.NamedType` and reaches the named-type arm, which reads the base type, the declaring type and the
containing namespace. A closed class is an ordinary class and reaches the same arm. An extension indexer is a member
and reaches the member arm at `:144`. The inheritable options are keyed by serializable declaration identifier
(`:319-331`), and a union and its synthesized members receive ordinary identifiers, which
`03-code-model-unions-closed.md:801-805` establishes.

Additional outputs carry two file kinds, which are the design-time generated code and the design-time touch file
(`AdditionalOutputs/AdditionalCompilationOutputFileKind.cs:7-11`), and one provider interface that returns files
(`AdditionalOutputs/IAdditionalOutputFileProvider.cs:21-24`). Nothing in the subsystem inspects the code model.

Observers are four service interfaces whose methods take a compilation, a partial compilation or a syntax tree
(`Observers/ICompilationModelObserver.cs:15-22`, and the three sibling files of `Observers/`). They are described as
being for testing only and add no dispatch of their own.

Queries dispatch on `DeclarationKind` in three places (`Queries/Query.cs:145-200`, `:310-335`, `:475-485`), and a
union reaches the named-type arm in each. One point is worth recording for story S-16 rather than for a new story:
the query that selects derived types of a declaration that is not the compilation filters the candidates with
`child.IsConvertibleTo( baseType )` (`Queries/Query.cs:188-192`), which resolves to the conversion reimplementation
of `DeclarationEqualityComparer`
(`Metalama.Framework/Code/TypeExtensions.cs:33-38`;
`Metalama.Framework.Engine/CodeModel/Comparers/DeclarationEqualityComparer.cs:99-118`). That is the same comparer
whose ignorance of the union conversions section 10 of `DECISIONS.md` records, so the fix of S-16 also fixes this
consumer and no separate work is needed.

Reflection mocks build a mock type from a symbol through a switch over `SymbolKind`
(`ReflectionMocks/CompileTimeTypeFactory.cs:125-165`). A union is `SymbolKind.NamedType`, so it reaches the
named-type arm, which records whether the type is an enum and whether it is a value type
(`:157-158`). A union declaration is a struct and a closed class is a class, so both are described correctly. The
mock hierarchy answers every other question by throwing a documented not-supported exception
(`ReflectionMocks/CompileTimeMocksHelper.cs:17-19`), which is unchanged by C# 15.

## Conditional compilation and the .NET 11 runtime

The only conditional compilation in the seven engine subsystems is the desktop and core split of the index and range
serializers (`SyntaxSerialization/SyntaxSerializationService.cs:18`, `:80`, `:331`;
`SyntaxSerialization/IndexSerializer.cs:5`; `SyntaxSerialization/RangeSerializer.cs:5`). Those symbols separate
`net472` from .NET Core and are not version specific, which
`analysis-reports/06-user-tfm-patterns-tests-docs.md:251` already records. No file of the eight subsystems mentions a
target framework above `net472` or a Roslyn variant symbol.

— Claude for @gfraiteur
