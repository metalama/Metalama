# Terrain map: syntax generation, syntax serialisation, formatting and manifest serialisation

Subsystem scope, all under `C:/src/Metalama-2027.0/Metalama/Metalama.Framework/src/Metalama.Framework.Engine/`:

- `SyntaxGeneration/**` (21 files, 2 826 lines)
- `SyntaxSerialization/**` (49 files, 2 121 lines)
- `Formatting/**` (18 files, 2 316 lines)
- `Serialization/**` (5 files, 393 lines)

Adjacent files that the subsystem cannot be analysed without are listed in section 6 and are marked
"adjacent" wherever they appear.

Branch: `topic/2027.0/26-09-03-net11-impact`. Everything below was read on that branch.

---

## 0. The three facts that frame everything else

1. **Every file of this subsystem is compiled once per Roslyn variant.**
   `Metalama.Framework/src/Metalama.Framework.Engine.5.0.0/Metalama.Framework.Engine.5.0.0.csproj` is
   `<Compile Include="../Metalama.Framework.Engine/**/*.cs" />` plus
   `<Import Project="../../../eng/RoslynVersions/Roslyn.5.0.0.props" />`. Any reference to a Roslyn 5.10 API
   (`UnionDeclarationSyntax`, `UnsafeExpressionSyntax`, `WithElementSyntax`,
   `BreakStatementSyntax.Name`) placed in these files without a `#if ROSLYN_5_10_0_OR_GREATER` guard
   breaks the Roslyn 5.0 variant build.

2. **There is currently no production `#if` on the Roslyn version anywhere in this subsystem.**
   `eng/RoslynVersions/Roslyn.5.10.0.props:22-24` states it explicitly:
   `"ROSLYN_5_10_0_OR_GREATER is defined by this variant only. No production source branches on it. It
   exists for the two aspect tests whose expected output differs between Roslyn 5.0 and Roslyn 5.10."`
   The only two consumers of the constant in the whole repository are test-directive markers:
   `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/DesignTimeInvalidCode/UnknownAccessorInTemplate.cs:7`
   (`// @RequiredConstant(ROSLYN_5_10_0_OR_GREATER)`) and
   `.../UnknownAccessorInTemplate_Roslyn5_0.cs:7` (`// @ForbiddenConstant(...)`).
   Enabling C# 15 reintroduces production branching that commit `e247425d69` deliberately removed for the
   4.x wave.

3. **The four C# 15 grammar changes are invisible to the code generator today.**
   `eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs:19` calls
   `RemoveExperimentalDeclarations( tree )` (lines 35-43), which does
   `tree.Types.RemoveAll( t => t.IsExperimental )` and recursively removes every
   `Field { IsExperimental: true }` (lines 55-74). `IsExperimental` is
   `!string.IsNullOrEmpty( this.ExperimentalUrl )` (`Model/TreeType.cs:37`, `Model/Field.cs:51`).
   All four C# 15 additions in `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml` carry
   `ExperimentalUrl`:
   - `UnionDeclarationSyntax` line 1954 (`Base="TypeDeclarationSyntax"`, kind `UnionDeclaration`, keyword `UnionKeyword`)
   - `UnsafeExpressionSyntax` line 496 (`Base="ExpressionSyntax"`, kind `UnsafeExpression`)
   - `WithElementSyntax` line 816 (`Base="CollectionElementSyntax"`, kind `WithElement`)
   - `BreakStatementSyntax.Name` line 1296 and `ContinueStatementSyntax.Name` line 1307
     (`Type="IdentifierNameSyntax" Optional="true"`)

   So the whole generated layer (`MetaSyntaxRewriter.g.cs`, `RoslynVersionSyntaxVerifier.g.cs`,
   `RunTimeCodeHasher.g.cs`, `CompileTimeCodeHasher.g.cs`, `SyntaxNodePartialUpdateExtensions.g.cs`,
   produced by `eng/src/GenerateMetaSyntaxRewriter/GenerateMetaSyntaxRewriter.cs:39-48`) simply does not
   know these nodes exist. When Roslyn drops the `ExperimentalAttribute`, the generator picks them up
   with no source change; while they remain experimental, nothing does.

---

## 1. Files and types sensitive to the shape of the C# language

### 1.1 `SyntaxGeneration/ContextualSyntaxGenerator.cs` (1 167 lines) — the dominant hotspot

`public sealed partial class ContextualSyntaxGenerator`. Five distinct language-shape dependencies.

| Lines | Member | What is enumerated | Behaviour on an unknown construct |
| --- | --- | --- | --- |
| 780-817 | `public SyntaxNode AddAttribute( SyntaxNode oldNode, IAttributeData attribute )` | 20 `SyntaxKind` cases of declaration node, each cast to its concrete `*Syntax` type to call `AddAttributeLists` | **Throws.** `_ => throw new AssertionFailedException( $"Unexpected syntax kind {oldNode.Kind()} at '{oldNode.GetLocation()}'." )` (line 815) |
| 1026-1072 | `internal CastExpressionSyntax SafeCastExpression( TypeSyntax type, ExpressionSyntax syntax )` | `var requiresParenthesis = syntax.Kind() switch` over ~25 expression kinds (lines 1034-1061) | **Silently parenthesises.** `_ => true` (line 1060) |
| 142, 167 | both `TypeOfExpression` overloads | `type.TypeKind is TypeKind.Class or TypeKind.Struct or TypeKind.Interface or TypeKind.Delegate or TypeKind.Enum or TypeKind.Extension` | **Silently skips** unbound-generic and nullable-annotation stripping |
| 283-342 | `internal SyntaxList<TypeParameterConstraintClauseSyntax> ConstraintClauses( IGeneric methodOrType )` | `genericParameter.TypeKindConstraint` switch over `Class`, `Struct`, `Unmanaged`, `NotNull`, `Default`; then `HasDefaultConstructorConstraint`, then `AllowsRefStruct` (line 343) | **Silently drops** the constraint (switch has no `default`) |
| 964-1024 | `internal SyntaxList<TypeParameterConstraintClauseSyntax> TypeParameterConstraintClauses( ImmutableArray<ITypeParameterSymbol> )` | the symbol-based twin of the above; `HasNotNullConstraint` / `HasReferenceTypeConstraint` / `HasValueTypeConstraint` / `HasUnmanagedTypeConstraint` / `HasConstructorConstraint` | **Silently drops.** Note it does *not* emit `allows ref struct`, unlike the `IGeneric` overload |
| 505-547 | `internal ExpressionSyntax RenderInterpolatedString( InterpolatedStringExpressionSyntax )` | `content.Kind()` switch on `InterpolatedStringText` / `Interpolation` | **Silently discards** any other content kind (no `default`) |
| 1113-1167 | `internal ExpressionSyntax TupleExpression(...)` and its local `GetRightMostIdentifier` | `expression.Kind() switch` over `IdentifierName`, `SimpleMemberAccessExpression`, `ConditionalAccessExpression` | `_ => null` → element name not elided; cosmetic only |
| 704-741 | `internal AttributeSyntax Attribute( IAttributeData )` | `lastParameter is { IsParams: true }` special-cases `params` arrays into the compact form | not kind-based |
| 819-882 | `private ExpressionSyntax TypedConstantExpression(...)` | `type.TypeKind` switch on `Enum` / `Array` then a `value` type switch | throws `ArgumentOutOfRangeException` (line 872) |
| 948-962 | `private ParameterSyntax Parameter(...)` | delegates modifier emission to `parameter.GetSyntaxModifierList()` (line 958) — see §6.1 | |

Historical note, exactly the shape the C# 15 work will repeat: commit `e247425d69`
("Strip always-true `#if ROSLYN_4_(4|8|12)_0_OR_GREATER` guards (#1603)") removed two guards from this
very file:

```
-#if ROSLYN_4_12_0_OR_GREATER
             if ( genericParameter.AllowsRefStruct )        // now line 343
-#endif
...
-#if ROSLYN_4_8_0_OR_GREATER
             SyntaxKind.CollectionExpression => false,      // now line 1052
-#endif
```

### 1.2 `SyntaxGeneration/SyntaxGeneratorForIType.*` — the `IType` → `TypeSyntax` visitor

- `SyntaxGeneratorForIType.AbstractGeneratorVisitor.cs:111`
  `protected override T DefaultVisit( IType type ) => throw new AssertionFailedException();`
  Its base is `TypeVisitor<T>` (adjacent, §6.2), whose `Visit` dispatches on `IType.TypeKind` and throws
  on an unknown kind. This pair is the single extension point for a new `IType` kind.
- `SyntaxGeneratorForIType.TypeSyntaxGeneratorVisitor.cs`:
  - line 81 `VisitFunctionPointerType( IFunctionPointerType ) => throw new NotImplementedException();`
  - line 99 `if ( type is { TypeKind: TypeKind.Error, Name: "var" } ) return CreateSystemObject();`
  - line 126-145 `TryCreateSpecializedNamedTypeSyntax`: `SpecialType.Void`, then
    `type is { IsNullable: true, IsReferenceType: false }` and `innerType.TypeKind != TypeKind.Pointer`
  - line 151 `if ( typeSyntax.Kind() is not (SyntaxKind.IdentifierName or SyntaxKind.GenericName) ... )`
  - line 160-161 `containingTypeSyntax.Kind() is SyntaxKind.IdentifierName or GenericName or QualifiedName or AliasQualifiedName`
  - line 175 `if ( type.TypeKind != TypeKind.Error )` gates the `global::` alias
  - line 197-230 `VisitTupleType` special-cases `TupleLength` 0 and 1
- `SyntaxGeneratorForIType.ExpressionSyntaxGeneratorVisitor.cs:37` — the same `TypeKind.Error` gate on
  the `global::` alias.

### 1.3 `SyntaxGeneration/SyntaxFactoryEx.cs` — identifier escaping and literal emission

- `internal static SyntaxToken SafeIdentifier( string name )` lines 129-151 and its trivia-preserving
  overload lines 160-182: `var keywordKind = SyntaxFacts.GetKeywordKind( name ); if ( keywordKind !=
  SyntaxKind.None ) { … "@" + name … }`.
  `SyntaxFacts.GetKeywordKind` recognises **reserved** keywords only. Contextual keywords
  (`field`, `record`, `required`, and the C# 15 additions `union` and `closed`) return
  `SyntaxKind.None` and are emitted unescaped.
- `internal static IdentifierNameSyntax SafeIdentifierName( string name )` line 207,
  `WellKnownIdentifier`/`WellKnownIdentifierName` lines 189-236 — the deliberate opt-out, enforced by
  analyzer `LAMA0850` (§6.3).
- `LiteralExpressionOrNull( object?, ObjectDisplayOptions )` lines 335-356: a type switch over the 13
  primitive CLR types, `_ => null` (line 355). `LiteralExpression( object, … )` line 329 turns that
  `null` into `ArgumentOutOfRangeException`.
- `internal static SyntaxToken InvocationRefKindToken( this RefKind )` lines 85-93: switch over
  `RefKind`, `_ => throw new AssertionFailedException`.
- `internal static AccessorListSyntax FormattedAccessorList(...)` lines 49-83: knows only
  `accessor.Body == null && accessor.ExpressionBody == null && accessor.SemicolonToken.IsKind( SemicolonToken )`.

### 1.4 `SyntaxGeneration/SyntaxFactoryDebugHelper.NormalizeRewriter.cs`

`VisitQualifiedName` lines 128-171 carries an explicitly incomplete allow-list of parent contexts in
which a `QualifiedNameSyntax` must stay a name rather than become a member access. The comment at line
135 says so: `"The following list of exceptions is incomplete. If you get into an
InvalidCastException in the rewriter, you have to extend it."` The 15 enumerated contexts are
`GenericName`, `UsingDirective`, `NamespaceDeclaration.Name`, `FileScopedNamespaceDeclaration.Name`,
`MethodDeclaration.ReturnType`, `VariableDeclaration.Type`, `TypeConstraint.Type`,
`ArrayType.ElementType`, `ObjectCreationExpression.Type`, `DefaultExpression.Type`,
`CastExpression.Type`, `ExplicitInterfaceSpecifier.Name`, `Parameter.Type`, `PropertyDeclaration.Type`,
`EventDeclaration.Type`, `SimpleBaseType`, `TypeOfExpression`. Debug-only path
(`SyntaxFactoryDebugHelper.cs:210-225` swallows every exception into `ex.ToString()`).

### 1.5 `SyntaxGeneration/ContextualSyntaxGenerator.*Rewriter.cs`

Four `SafeSyntaxRewriter` derivations, each overriding a fixed set of node visits:

- `RemoveReferenceNullableAnnotationsRewriter.cs`: two near-identical rewriters,
  `RemoveReferenceNullableAnnotationsRewriterForSymbol` (lines 19-157) and
  `RemoveReferenceNullableAnnotationsRewriter` (lines 159-270). Each overrides exactly
  `VisitGenericName`, `VisitArrayType`, `VisitTupleType`, `VisitFunctionPointerType`,
  `VisitNullableType`. A new *type* syntax form would be walked by the base rewriter with a stale
  `_type` field, producing wrong annotations rather than an error.
- `DynamicToVarRewriter.cs:16-26` — string comparison `node.Identifier.Text == "dynamic"`.
- `RemoveTypeArgumentsRewriter.cs:45-55` — `VisitGenericName` only.
- `SubstitutionRewriter.cs:80-90` — `VisitIdentifierName` only.
- `NormalizeSpaceRewriter.cs:107` — `VisitTupleType` only.

### 1.6 `Formatting/TextSpanClassifier.cs` (446 lines)

The design-time compile-time/run-time colouring walker. `internal sealed partial class TextSpanClassifier
: ClassifierBase`. Its declaration-level overrides (complete list):

| Line | Override |
| --- | --- |
| 113 | `VisitClassDeclaration` → `VisitTypeDeclaration<T>` (lines 77-111) |
| 115 | `VisitStructDeclaration` → `VisitTypeDeclaration<T>` |
| 117-123 | `VisitRecordDeclaration` → `VisitTypeDeclaration<T>` plus `node.ParameterList` |
| 138-139 | `VisitDelegateDeclaration` → `VisitSimpleTypeDeclaration` (lines 125-136) |
| 141 | `VisitEnumDeclaration` → `VisitSimpleTypeDeclaration` |
| 143-161 | `VisitMethodDeclaration` |
| 175-197 | `VisitFieldDeclaration` |
| 199 | `VisitEventDeclaration` |
| 201-220 | `VisitPropertyDeclaration` |
| 222-235 | `VisitAccessorDeclaration` |
| 237-259 | `VisitEventFieldDeclaration` |
| 381-404 | `VisitIfStatement` |
| 406-423 | `VisitForEachStatement` |
| 425-445 | `VisitBlock` |

**There is no `VisitInterfaceDeclaration`, no `VisitExtensionBlockDeclaration`, no
`VisitIndexerDeclaration`, no `VisitConstructorDeclaration`, no `VisitOperatorDeclaration`.**
That is a pre-existing gap, not a C# 15 one, and it shows what happens: `DefaultVisit` (lines 278-298)
runs instead, so the type keyword, identifier, braces, base list and constraint clauses of a
compile-time interface are never marked `TextSpanClassification.CompileTime`. A `union` declaration
would land in the same hole.

`VisitTypeDeclaration<T>` is generic over `TypeDeclarationSyntax` (line 78) and marks
`Modifiers`, `Keyword`, `OpenBraceToken`, `CloseBraceToken`, `ConstraintClauses`, `Identifier`,
`BaseList`, `AttributeLists`, `TypeParameterList` (lines 90-98). Because
`UnionDeclarationSyntax : TypeDeclarationSyntax` and declares all of those as `Override="true"` fields
(`Syntax-5.10.0.xml:1956-1977`), a one-line `VisitUnionDeclaration` override reusing
`VisitTypeDeclaration` is all that is needed.

Also: `ShouldMarkTrivia` lines 46-52 (`CompileTime`/`RunTime` only); `Mark( SyntaxToken … )` line
325-340 skips `XmlTextLiteralNewLineToken`; `Mark( SyntaxTriviaList … )` lines 342-372 skips
`SingleLineCommentTrivia`, `MultiLineCommentTrivia`, `EndOfLineTrivia` and everything with structure.

### 1.7 `Formatting/CodeFormatter.CustomSimplifier.cs` (235 lines)

`private sealed class CustomSimplifier : SafeSyntaxRewriter`, three overrides:

- `VisitObjectCreationExpression` lines 42-142: delegate-creation elision. Gated on
  `node.Parent?.Kind() is SyntaxKind.Argument or SyntaxKind.EqualsValueClause or
  SyntaxKind.SimpleAssignmentExpression` (line 51) and then on the grandparent being
  `InvocationExpression` (line 60) or `ObjectCreationExpression` (line 79). A delegate argument passed
  in a syntactic position not in that list is simply not simplified.
- `VisitCastExpression` lines 144-210: `node.Type.IsKind( SyntaxKind.TupleType )` (line 148) and
  `node.Type.IsKind( SyntaxKind.NullableType )` (line 184).
- `VisitPostfixUnaryExpression` lines 212-234: `node.OperatorToken.IsKind( SyntaxKind.ExclamationToken )`.

All three are `Simplifier.Annotation`-gated, so a construct they do not know is left over-specified but
correct.

### 1.8 `Formatting/ClassifierBase.cs`

`VisitTrivia` lines 33-58: `EndOfLineTrivia`, `WhitespaceTrivia`, `MultiLineCommentTrivia`,
`SingleLineCommentTrivia`, `DocumentationCommentExteriorTrivia`. No `default`.

### 1.9 `Formatting/FormattedCodeWriter.FormattingVisitor.cs:77`

`if ( node.IsKind( SyntaxKind.MethodDeclaration ) && node is MethodDeclarationSyntax method ) span =
method.Identifier.Span;` — narrows a diagnostic tag to the identifier for methods only.

### 1.10 `Formatting/XmlDocumentationReader.cs`

- lines 57-69: `SymbolKind.Method` (override / explicit interface implementation / plain) and
  `SymbolKind.NamedType` cases for inherited documentation.
- line 80: `symbol is IMethodSymbol { MethodKind: MethodKind.Constructor }`.
- lines 166-172: kind display switch —
  `TypeKind.FunctionPointer => "function pointer"`, `TypeKind.TypeParameter => "type parameter"`,
  `_ => namedType.TypeKind.ToString().ToLowerInvariant()`, fallback
  `symbol.GetDeclarationKind( compilationContext ).ToDisplayString()`. A new `TypeKind` renders its enum
  name lower-cased, which is accidentally right for `Union` and was accidentally right for `Extension`.

### 1.11 `SyntaxSerialization/**` — declaration-kind switches

| File:line | Switch | Unknown case |
| --- | --- | --- |
| `CompileTimePropertyInfoSerializer.cs:38-111` | `propertyOrIndexer.DeclarationKind`: `Property`, `Indexer` | **Throws** `AssertionFailedException( $"Unexpected type: {…DeclarationKind}." )` line 110 |
| `CompileTimeFieldOrPropertyInfoSerializer.cs:25-29` | `member.DeclarationKind`: `Property or Indexer`, `Field` | switch expression, throws `InvalidOperationException` implicitly |
| `CompileTimeParameterInfoSerializer.cs:28-36` | `declaringMember?.DeclarationKind`: `Method or Constructor`, `Indexer` | |
| `CompileTimeReturnParameterInfoSerializer.cs:21-26` | `parameter.DeclaringMember?.DeclarationKind`: `Method`, `Indexer` | |
| `MetalamaMethodBaseSerializer.cs:81, 161` | `method.DeclarationKind == DeclarationKind.Constructor` | binary, no throw |
| `TypeSerializationHelper.cs:32-42` | `symbol.Kind`: `SymbolKind.TypeParameter`, `default` → `typeof(...)` | never throws |
| `SerializableTypes.cs:123-146` | `IsSerializableIntrinsic`: 14 `SpecialType` cases, `default => false` | |
| `SerializableTypes.cs:62, 79` | `type.Kind == SymbolKind.ArrayType` / `SymbolKind.NamedType` | reports `LAMA…UnsupportedSerialization` |
| `ReflectionSignatureBuilder.cs:45-62` | `TypeArgumentDetector : SymbolVisitor<bool>`: `DefaultVisit` **throws**; `VisitFunctionPointerType` **throws** `NotImplementedException` | |
| `ReflectionSignatureBuilder.cs:64-236` | `StringBuildingVisitor : SymbolVisitor`: `DefaultVisit` **throws** `NotSupportedException` line 142; `VisitFunctionPointerType` **throws** line 231; `VisitNamedType` line 161-176 hard-codes 13 `SpecialType` values that need no namespace | |

`SyntaxSerializationService.cs` (357 lines) is the registry. `TryGetSerializer` lines 161-181 special-cases
`Enum` and `Array` before falling back to a type-identity lookup; failure surfaces as
`InvalidOperationException` from `Serialize` line 281 with
`SerializationDiagnosticDescriptors.UnsupportedSerializationMessage`.

### 1.12 What is *not* language-shape sensitive

`Serialization/**` (`LanguageVersionJsonConverter.cs`, `LinePositionSpanJsonConverter.cs`,
`TextSpanJsonConverter.cs`, `ManifestJsonContext.cs`, `ManifestSerializer.cs`) contains no C# grammar
knowledge. `ManifestJsonContext.cs:64-84` is a fixed `[JsonSerializable]` list of manifest record types.

---

## 2. Files and types sensitive to runtime, SDK, Roslyn or host IDE version

### 2.1 Reflection into Roslyn internals (Roslyn version and host IDE)

| File:line | Target | Failure mode |
| --- | --- | --- |
| `SyntaxGeneration/ContextualSyntaxGenerator.cs:41-48` | static ctor loads `Microsoft.CodeAnalysis.CSharp.CodeGeneration.CSharpSyntaxGenerator` from `WorkspaceHelper.CSharpWorkspacesAssembly` and reads its public static `Instance` field | `!` on `GetType`, `!` on `GetField`, `.AssertNotNull()` on the value → `TypeInitializationException` on first use. Loud. |
| `SyntaxGeneration/SyntaxGeneratorForIType.cs:23-28` | the identical reflection, duplicated | same |
| `SyntaxGeneration/SyntaxGeneratorForIType.NullableSyntaxAnnotationEx.cs:18-33` | `Microsoft.CodeAnalysis.CodeGeneration.NullableSyntaxAnnotation` from `typeof(Workspace).Assembly`, fields `Oblivious` and `AnnotatedOrNotAnnotated` | `throwOnError: false` and `?.GetValue( null )` → both properties become `null`. **Silent.** See §5.1 |
| `SyntaxGeneration/SyntaxFactoryEx.LiteralFormatter.cs:30-58` | `Microsoft.CodeAnalysis.CSharp.ObjectDisplay` and `Microsoft.CodeAnalysis.ObjectDisplayOptions`; picks `FormatLiteral` by first-parameter type via `.Single(…)` (line 33) and tolerates both the 2-parameter and 3-parameter shape (line 41) | `AssertNotNull()` / `Single` throw. Loud, and it already carries a compatibility shim for an earlier Roslyn signature change. |
| `Utilities/Roslyn/WorkspaceHelper.cs:19-52` (adjacent) | loads `Microsoft.CodeAnalysis.CSharp.Workspaces` at the version referenced by `Microsoft.CodeAnalysis.Workspaces`, preferring an already-loaded higher version (`MaxByOrNull( a => a.GetName().Version )`, line 36) | This is the host-IDE adaptation point: in `devenv.exe` / the Rider backend the workspaces assembly is whatever the host loaded. |

### 2.2 Copied Roslyn internals (silent-drift risk)

- `SyntaxGeneration/ObjectDisplayOptions.cs:234-266`. Header comment line 234:
  `"This type is copied from the Roslyn source code. The member integer values must match."`
  It is cast straight to Roslyn's internal enum by `LiteralFormatter.Format` (`(int) options`,
  `SyntaxFactoryEx.LiteralFormatter.cs:61`, `Expression.Convert( optionsParameter,
  objectDisplayOptionsType )`, line 39). Nothing validates the values.
- `SyntaxGeneratorForIType.cs:86` — `"Copy of Microsoft.CodeAnalysis.CSharp.Shared.Lightup.NullableSyntaxAnnotationEx."`
- `SyntaxGeneratorForIType.TypeSyntaxGeneratorVisitor.cs:22` — `"Based on Roslyn TypeSyntaxGeneratorVisitor."`
- `SyntaxGeneratorForIType.cs:41` — `"Based on Roslyn ITypeSymbolExtensions.GenerateTypeSyntax."`

### 2.3 Target-framework conditional compilation (.NET runtime)

Complete list of `#if` in the subsystem:

- `SyntaxSerialization/SyntaxSerializationService.cs:18-22` — `#if NET472 using System.Runtime.Serialization; #else using System.Runtime.CompilerServices; #endif`
- `SyntaxSerialization/SyntaxSerializationService.cs:80-83` — `IndexSerializer` and `RangeSerializer` are registered only on non-`net472`
- `SyntaxSerialization/SyntaxSerializationService.cs:331-335` — `FormatterServices.GetUninitializedObject` vs `RuntimeHelpers.GetUninitializedObject`
- `SyntaxSerialization/IndexSerializer.cs:5` and `RangeSerializer.cs:5` — whole file under `#if !NET472`
- `Formatting/ClassifierBase.cs:17-21` and `Formatting/TextSpanClassifier.cs:24-26` — `#if !DEBUG` warning suppression only
- `Formatting/ClassifierBase.cs:77-81` — `#if DEBUG` source-text peek

Consequence of the `net8.0` → `net10.0` move: nothing in this subsystem branches on a .NET *version*,
only on desktop vs core. The `net472` branches survive PB-2027.0 unchanged.

### 2.4 Roslyn / .NET SDK version mapping (adjacent, but this subsystem's tables)

`Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs`:

- `Latest => LanguageVersion.CSharp14` line 31-32
- `All` line 38-43: `CSharp14, CSharp13, CSharp12, CSharp11, CSharp10`
- `ToLanguageVersion( this RoslynApiVersion )` lines 52-62: `V5_0_0 => CSharp14`, `V5_10_0 => CSharp14`,
  `_ => throw new AssertionFailedException`
- `ToNuGetVersionString` lines 77-87: `V5_10_0 => "5.10.0-1.26365.3"` — the prerelease marker that
  drives `ToPrereleasePackageSourceUrl` (lines 117-132) to the ProGet feed
- `ToVersion` lines 134-144
- `GetMaxLanguageVersion( Version roslynVersion )` lines 149-159: `(>= 5, _) => CSharp14`

`Metalama.Framework.Engine/Utilities/LanguageVersionProvider.cs`:

- `GetLanguageVersionFromDotNetSdk` lines 204-231: `version.Major switch { >= 10 => CSharp14, >= 9 =>
  CSharp13, >= 8 => CSharp12, _ => throw new PlatformNotSupportedException }`
- `GetLanguageVersionFromMSBuild` lines 233-282: reads
  `<MSBuildBinPath>\Roslyn\Microsoft.CodeAnalysis.CSharp.dll` (and the amd64 parent fallback), then
  `SupportedCSharpVersions.GetMaxLanguageVersion`.

`eng/RoslynVersions/Roslyn.5.0.0.props` pins `SystemTextJsonVersion` to `9.0.0` (line 12) and
`Roslyn.5.10.0.props` to `10.0.11` (line 26) — a runtime-version-driven package pin that the
`Serialization/` folder depends on.

### 2.5 Language version inside the subsystem

`SyntaxGeneration/SyntaxGenerationContext.cs` is the **only** file in the subsystem that reads the
language version:

```
39    private LanguageVersion LanguageVersion => this.Compilation.GetLanguageVersion();
41    internal bool RequiresStructFieldInitialization => this.LanguageVersion < (LanguageVersion) 1100;
44    [Memo] internal bool SupportsInitAccessors => this.Compilation.GetTypeByMetadataName( typeof(IsExternalInit).FullName! ) != null;
```

Note the two different techniques: line 41 tests the *language* version numerically (1100 = C# 11);
line 44 tests the *reference set* for a well-known type. Consumers, all outside this subsystem:
`AdviceImpl/Introduction/Constructors/IntroduceConstructorTransformation.cs:131`,
`AdviceImpl/Introduction/IntroduceEventTransformation.cs:61`,
`AdviceImpl/Introduction/IntroduceFieldTransformation.cs:52`,
`AdviceImpl/Introduction/IntroducePropertyTransformation.cs:61`,
`AdviceImpl/Override/OverridePropertyBaseTransformation.cs:52`,
`Linking/LinkerInjectionStep.AuxiliaryMemberFactory.cs:411` and `:524`,
`Linking/RewriterExtensions.cs:68`.

`Compilation.GetLanguageVersion()` is `Utilities/Roslyn/CompilationExtensions.cs:114-124`: it reads
`((CSharpParseOptions) compilation.SyntaxTrees.FirstOrDefault().Options).LanguageVersion`, that is, the
**first syntax tree only**, falling back to `LanguageVersion.Default.MapSpecifiedToEffectiveVersion()`
for an empty compilation.

### 2.6 `SyntaxGenerationOptions` does **not** carry the language version

`SyntaxGeneration/SyntaxGenerationOptions.cs` (55 lines) is a `sealed record` wrapping a single
`CodeFormattingOptions` field and exposing exactly two predicates:

- `internal bool WillBeTextualized => this._codeFormattingOptions != CodeFormattingOptions.None;` (line 33)
- `internal bool WillBeFormatted => this._codeFormattingOptions == CodeFormattingOptions.Formatted;` (line 43)

and two instances, `Formatted` (line 53) and `Unformatted` (line 55). **It is a formatting switch, not a
language switch.** Any claim that "syntax generation depends on the language version through
`SyntaxGenerationOptions`" is false on this branch; the dependency is on `SyntaxGenerationContext`
(§2.5), and it is a single Boolean.

The corollary matters for C# 15: `CompilationContext.GetSyntaxGenerationContext`
(`Metalama.Framework.Engine/Services/CompilationContext.cs:146-172`) caches on
`private record struct SyntaxGenerationContextCacheKey( bool IsNullOblivious, bool IsPartial, string
EndOfLine, SyntaxGenerationOptions Options )` (line 174) — **the language version is not part of the
cache key.** The cache is per-`CompilationContext`, so this is safe today, but a language-version
predicate added to `SyntaxGenerationOptions` rather than to `SyntaxGenerationContext` would be cached
across language versions.

Where the two predicates are consumed (the whole simplification/elastic-trivia machinery):
`Utilities/Roslyn/SyntaxExtensions.cs:133-159` (`NormalizeWhitespaceIfNecessary`,
`WithSimplifierAnnotationIfNecessary`), `:180-193`, `:241-344`
(`WithOptionalLeadingTrivia`, `WithOptionalTrailingLineFeed`, `WithOptionalTrailingTrivia`);
`SyntaxGeneration/SyntaxGenerationContext.cs:68-80`;
`SyntaxGeneration/SyntaxFactoryEx.cs:53`;
`SyntaxGeneration/SyntaxGeneratorForIType.AbstractGeneratorVisitor.cs:118, 125`;
`SyntaxGeneration/ContextualSyntaxGenerator.cs:640, 658`.

### 2.7 Roslyn-version-dependent formatting services

`Formatting/CodeFormatter.cs` orchestrates five passes over each document (lines 108-215):
`AddDiagnosticAnnotations` → `CustomSimplifier` → `ImportAdder.AddImportsAsync` (line 155) →
`Simplifier.ReduceAsync` + `SimplifierFixer` (lines 164-174) → `Simplifier.ReduceAsync` again (line 182)
→ `Formatter.FormatAsync` (lines 191 / 209). `ImportAdder`, `Simplifier` and `Formatter` all come from
`Microsoft.CodeAnalysis.Workspaces`, whose behaviour is the host's Roslyn.
`Formatting/CodeFormatter.SimplifierFixer.cs:99-145` exists purely to repair one Roslyn defect
(`"It seems that Simplifier can remove an EOL trivia after single-line comment."`).

`Formatting/FormattedCodeWriter.cs:116-149` consumes
`Microsoft.CodeAnalysis.Classification.Classifier.GetClassifiedSpansAsync` and matches the returned
classification-type **strings** (`"comment"`, line 129). Roslyn adds classification types for new
keywords; unknown strings flow through untouched into
`ClassifiedTextSpan.CSharpClassTagName` (line 147).

### 2.8 Manifest serialisation and cross-version compatibility

`Serialization/LanguageVersionJsonConverter.cs`, doc comment lines 12-15:
`"We serialize the language version as an integer for cross-Roslyn-version compatibility."`
`Read` line 27 is `(LanguageVersion) reader.GetInt32()` with no range validation;
`Write` line 41 is `writer.WriteNumberValue( (int) value.Value )`. Registered at
`Serialization/ManifestJsonContext.cs:112`. See §5.4.

---

## 3. How the C# 14 wave (issues #1034 … #1160) landed here

### 3.1 The blunt answer: it almost entirely bypassed this subsystem

`git log --since=2025-01-01` over the four directories returns no commit carrying any of the C# 14 issue
numbers. Checking each C# 14 commit individually
(`aea7b2e5a2`, `929d055d85`, `aa5e62dbb0`, `df4ae55b09`, `81e5a5fed7`, `e3b3fc5959`, `ca6c690592` for
#1114 field keyword; `e9edd7cacc`, `b4da958605`, `cf0861898b` for #1105/#1108/#1109 null-conditional;
`a9698fa1e8`, `f374fce480`, `5a1ac3e5c4`, `6c9ffc219d`, `f776fd9af9` for #1159 extension blocks;
`bcdeb3a185`, `cdf076ee1a` for #1034; `22697b6ba5` for #1036; `30e21aea98` for #1127;
`737e0347a9` for #1035; `787ec4fcd8` for #1110-#1113; `5b121f3c21`, `6d8678e5d3` for #1116;
`70bd44a5e1`, `48541ada9b` for #1094) shows **not one of them touched
`SyntaxGeneration/`, `SyntaxSerialization/`, `Formatting/` or `Serialization/`.**

The C# 14 work landed in `CodeModel/`, `Advising/`, `AdviceImpl/`, `Linking/`, `Templating/` and
`Transformations/`.

### 3.2 The four mechanisms C# 14 actually used, and which one this subsystem sits in

1. **Grammar-driven generation, the default.** New syntax nodes enter through
   `eng/src/GenerateMetaSyntaxRewriter/Syntax-<version>.xml` and are absorbed by the generator with no
   hand-written code: `RoslynVersionSyntaxVerifier.g.cs` gains a `VisitVersionSpecificNode` entry, and
   `MetaSyntaxRewriter.g.cs`, the two code hashers and `SyntaxNodePartialUpdateExtensions.g.cs` gain
   full support. In the current `.generated/5.0.0/…/RoslynVersionSyntaxVerifier.g.cs` the C# 14
   additions appear as:
   ```
   public override void VisitFieldExpression( FieldExpressionSyntax node )
       { this.VisitVersionSpecificNode( node, RoslynApiVersion.V5_0_0 ); }
   public override void VisitExtensionBlockDeclaration( ExtensionBlockDeclarationSyntax node )
       { this.VisitVersionSpecificNode( node, RoslynApiVersion.V5_0_0 ); }
   public override void VisitExtensionMemberCref( ExtensionMemberCrefSyntax node )
       { this.VisitVersionSpecificNode( node, RoslynApiVersion.V5_0_0 ); }
   public override void VisitIgnoredDirectiveTrivia( IgnoredDirectiveTriviaSyntax node )
       { this.VisitVersionSpecificNode( node, RoslynApiVersion.V5_0_0 ); }
   ```
   This is why the subsystem was untouched: the generated layer absorbed the grammar.

2. **A new `TypeKind` plus a virtual visitor method that falls back.** C# 14 extension blocks added
   `TypeKind.Extension` (`Metalama.Framework/src/Metalama.Framework/Code/TypeKind.cs:83`) and, in
   `CodeModel/Visitors/TypeVisitor.cs`:
   ```
   24    TypeKind.Extension => this.VisitExtensionBlock( (IExtensionBlock) type ),
   ...
   39    protected virtual T VisitExtensionBlock( IExtensionBlock extensionBlock ) => this.VisitNamedType( extensionBlock );
   ```
   The new kind gets a `virtual` method whose default delegates to the nearest existing behaviour, so
   every existing visitor — including this subsystem's
   `SyntaxGeneratorForIType.TypeSyntaxGeneratorVisitor` and `ExpressionSyntaxGeneratorVisitor` — keeps
   compiling and produces something plausible without being edited.
   The one place in this subsystem that *was* edited is
   `ContextualSyntaxGenerator.cs:142` and `:167`, where `TypeKind.Extension` was appended to the
   `TypeKind.Class or Struct or Interface or Delegate or Enum` lists (commit `0622d353f5`, later
   reshaped by `ee59906188` for issue #1579).

3. **`#if ROSLYN_<version>_OR_GREATER` around the new-API call, then a strip commit when the floor
   moves.** The two instances in this subsystem
   (`ConstraintClauses` / `AllowsRefStruct`, and `SafeCastExpression` / `SyntaxKind.CollectionExpression`)
   were removed by `e247425d69` once the floor reached 4.12. The guard-and-strip cycle is the
   established pattern for a syntax API that exists only in the newer variant.

4. **A contextual-keyword escape hook placed where the keyword binds, not in `SafeIdentifier`.**
   The C# 14 `field` keyword produced
   `Metalama.Framework.Engine/Templating/TemplateSyntaxFactoryImpl.cs:933-942`:
   ```
   public string EscapeIdentifier( string name )
       => name == "field"
          && this._templateExpansionContext.TargetDeclaration?.DeclarationKind == DeclarationKind.Method
          && this._templateExpansionContext.TargetDeclaration is IMethod { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet }
       ? "@field" : name;
   public SyntaxToken EscapeIdentifier( SyntaxToken token ) => SyntaxFactoryEx.SafeIdentifier( this.EscapeIdentifier( token.Text ) );
   ```
   `SyntaxFactoryEx.SafeIdentifier` was deliberately not changed: it stays reserved-keyword-only, and
   context-sensitive escaping is layered on top.

### 3.3 Two further conventions the wave established

- **Divergent expected output is expressed with test directives, not with source `#if`.** See
  `UnknownAccessorInTemplate.cs:7` (`@RequiredConstant(ROSLYN_5_10_0_OR_GREATER)`) and
  `UnknownAccessorInTemplate_Roslyn5_0.cs:7` (`@ForbiddenConstant(...)`) — two copies of the same test,
  each pinned to one variant.
- **The version tables are edited together.** `SupportedCSharpVersions.Latest`,
  `SupportedCSharpVersions.All`, `ToLanguageVersion`, `GetMaxLanguageVersion` and
  `LanguageVersionProvider.GetLanguageVersionFromDotNetSdk` are five switches over the same axis; C# 15
  requires all five, and none of them has a permissive default (each throws
  `AssertionFailedException` or `PlatformNotSupportedException`).

---

## 4. Extension points, per kind of language change

### 4.1 A new kind of type declaration (`union`)

Syntax side, ordered by how the change reaches them:

1. `eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs:35-43` — while `UnionDeclarationSyntax`
   carries `ExperimentalUrl`, the node is deleted from the model and nothing downstream sees it. This is
   the first switch to throw.
2. Generated, no edit needed once (1) passes: `MetaSyntaxRewriter.g.cs`,
   `RoslynVersionSyntaxVerifier.g.cs` (which will emit
   `VisitUnionDeclaration( … ) => VisitVersionSpecificNode( node, RoslynApiVersion.V5_10_0 )`),
   `RunTimeCodeHasher.g.cs`, `CompileTimeCodeHasher.g.cs`,
   `SyntaxNodePartialUpdateExtensions.g.cs`.
3. `Formatting/TextSpanClassifier.cs` — add `public override void VisitUnionDeclaration(
   UnionDeclarationSyntax node ) => this.VisitTypeDeclaration( node, n => base.VisitUnionDeclaration( n ) );`
   beside line 115. `VisitTypeDeclaration<T>` (line 77) already covers every field, because
   `UnionDeclarationSyntax : TypeDeclarationSyntax`. Guarded by `#if ROSLYN_5_10_0_OR_GREATER`.
4. `SyntaxGeneration/ContextualSyntaxGenerator.cs:793-816` (`AddAttribute`) — add
   `SyntaxKind.UnionDeclaration => ((UnionDeclarationSyntax) oldNode).AddAttributeLists( attributeList )`.
   Guarded. Without it the switch throws at line 815.

Model side:

5. `Metalama.Framework/src/Metalama.Framework/Code/TypeKind.cs` — a `Union` member.
6. `Metalama.Framework.Engine/CodeModel/Visitors/TypeVisitor.cs:16-27` — a case in `Visit`, plus a
   `protected virtual T VisitUnion( … ) => this.VisitNamedType( … )` following the
   `VisitExtensionBlock` precedent at line 39. This one edit keeps
   `SyntaxGeneratorForIType.TypeSyntaxGeneratorVisitor` and `ExpressionSyntaxGeneratorVisitor` working
   unchanged.
7. `SyntaxGeneration/ContextualSyntaxGenerator.cs:142` and `:167` — add `TypeKind.Union` to the two
   `TypeKind.Class or Struct or Interface or Delegate or Enum or Extension` lists, exactly as
   `Extension` was added.
8. `Metalama.Framework.Engine/CodeModel/Helpers/ModifierHelper.cs:198-236`
   (`GetTypeSyntaxModifierList`) — if a union carries a modifier the switch does not know.

### 4.2 A new modifier (`closed`)

The subsystem produces modifier tokens in exactly two places, both adjacent:

- `Metalama.Framework.Engine/CodeModel/Helpers/ModifierCategories.cs:10-24` — the `[Flags]` enum
  currently `Accessibility=1, Inheritance=2, Async=4, Static=8, ReadOnly=16, Unsafe=32, Volatile=64,
  Required=128, Const=256, Partial=512, Extern=1024, All=<or of all>`. A new modifier needs a new flag
  **and** inclusion in `All` (line 23).
- `Metalama.Framework.Engine/CodeModel/Helpers/ModifierHelper.cs`:
  - `GetSyntaxModifierList( this IDeclaration, ModifierCategories )` lines 22-56, a `DeclarationKind`
    switch that throws on an unknown kind (line 54)
  - `GetMemberSyntaxModifierList` lines 76-196
  - `GetTypeSyntaxModifierList` lines 198-236 — **`closed` would go here**; it currently emits only
    accessibility, `static`, `new`, `abstract`, `sealed`
  - `GetParameterSyntaxModifierList` lines 313-335 — `this`, ref-kind, `params`
  - `AddRefKindModifiers` lines 348-385 — throws on an unknown `RefKind` (line 383)
- `Metalama.Framework.Engine/Utilities/Roslyn/SymbolModifiersHelper.cs:16-41` — the `ISymbol` twin,
  which also throws on an unknown `SymbolKind` (line 39). Both must be edited together; they are
  already noted as needing unification (`"TODO: Unify with ToRoslynAccessibility"`,
  `ModifierHelper.cs:62, 78`).

Inside the subsystem, `ContextualSyntaxGenerator.Parameter` line 958 and `SyntaxFactoryEx.TokenWithTrailingSpace`
(line 41, a `ConcurrentDictionary<SyntaxKind, SyntaxToken>` cache, line 27) are the only consumers, and
neither enumerates modifiers, so neither needs an edit.

Separately, if `closed` becomes reachable as an identifier in generated code,
`SyntaxFactoryEx.SafeIdentifier` (lines 129-182) will **not** escape it, because
`SyntaxFacts.GetKeywordKind` returns `SyntaxKind.None` for contextual keywords. The C# 14 precedent
(§3.2 item 4) says the escape belongs in the context that binds the keyword, not in `SafeIdentifier`.

### 4.3 A new expression form (`unsafe(expr)`)

1. `TreeReader.RemoveExperimentalDeclarations` again — first blocker.
2. `SyntaxGeneration/ContextualSyntaxGenerator.cs:1034-1061` — `SafeCastExpression`'s
   `requiresParenthesis` switch. `unsafe(x)` is already parenthesised by its own syntax, so
   `SyntaxKind.UnsafeExpression => false` belongs beside `SyntaxKind.CollectionExpression => false`
   (line 1052), which is precisely where the stripped `#if ROSLYN_4_8_0_OR_GREATER` used to sit.
   Omitting it costs a redundant pair of parentheses; nothing breaks.
3. `Formatting/CodeFormatter.CustomSimplifier.cs:51` — if the new expression can host a
   target-typed delegate creation, `SyntaxKind.UnsafeExpression` must join the parent-kind list.
4. `SyntaxGeneration/SyntaxFactoryEx.cs:335-356` (`LiteralExpressionOrNull`) — unaffected; the new form
   is not a literal.
5. `SyntaxSerialization/**` — unaffected; the serialisers build expressions, they never consume
   arbitrary user expressions except `ExpressionSerializer.cs` / `ExpressionBuilderSerializer.cs`, which
   pass a pre-built `ExpressionSyntax` through.

### 4.4 A new collection-expression element (`with(...)`)

`WithElementSyntax : CollectionElementSyntax` (`Syntax-5.10.0.xml:816`). This subsystem never
constructs or destructures a collection expression: the only mention of `SyntaxKind.CollectionExpression`
is `ContextualSyntaxGenerator.cs:1052`, and it is about parenthesisation of the whole expression, not
its elements. Grep confirms no `ExpressionElementSyntax` or `SpreadElementSyntax` reference anywhere in
the four directories.

So the change is entirely absorbed by:
- `TreeReader.RemoveExperimentalDeclarations` (blocker), then
- the generated `MetaSyntaxRewriter.g.cs` / `RoslynVersionSyntaxVerifier.g.cs` / hashers.

`ArrayCreationExpression` (`ContextualSyntaxGenerator.cs:205-211`), `ListSerializer.cs:29` and
`DictionarySerializer.cs:115-127` all emit the pre-C#-12 `new T[]{…}` /
`InitializerExpression( SyntaxKind.CollectionInitializerExpression, … )` forms and are unaffected.

### 4.5 A new optional field on an existing statement (`break label;` / `continue label;`)

`BreakStatementSyntax.Name` and `ContinueStatementSyntax.Name`, `Optional="true"`,
`Type="IdentifierNameSyntax"` (`Syntax-5.10.0.xml:1296, 1307`).

This is the case the generator handles *worst*, because an optional field is removed from an existing
node rather than removing a whole node:

- `TreeReader.RemoveExperimentalChildren` (lines 55-74) strips the field, so
  `MetaSyntaxRewriter.g.cs`'s `VisitBreakStatement` will keep calling
  `MetaSyntaxFactory.BreakStatement( … )` **without** the name argument, silently discarding the label
  when a labelled `break` occurs inside a compile-time-transformed template.
- `SyntaxNodePartialUpdateExtensions.g.cs`'s `PartialUpdate( this BreakStatementSyntax node, … )` calls
  `node.Update( … )` with the fields it knows; a missing field means the `Update` overload resolution
  either fails to compile (if Roslyn removed the old overload) or drops the label.
- `RunTimeCodeHasher.g.cs` / `CompileTimeCodeHasher.g.cs` would not hash the label, so two templates
  differing only in the label would collide.

Nothing in this subsystem visits `BreakStatementSyntax` or `ContinueStatementSyntax` directly.
`Formatting/TextSpanClassifier.cs` handles only `VisitIfStatement` (381), `VisitForEachStatement` (406)
and `VisitBlock` (425); everything else falls to `DefaultVisit` (278), which marks the node from its
annotation and recurses, so a labelled `break` is coloured correctly by accident.

---

## 5. Places that would silently do the wrong thing

Ordered by how hard the failure is to notice.

### 5.1 `NullableSyntaxAnnotationEx` degrades to no-annotation

`SyntaxGeneration/SyntaxGeneratorForIType.NullableSyntaxAnnotationEx.cs:20-32` uses
`GetType( …, throwOnError: false )`, `?.GetField( … )`, `?.GetValue( null )`. If
`Microsoft.CodeAnalysis.CodeGeneration.NullableSyntaxAnnotation` moves or is renamed in a future Roslyn,
both properties become `null`, and the consumer

```
SyntaxGeneratorForIType.cs:49-58 (and :71-80)
    var additionalAnnotation = type.IsNullable switch { null => …Oblivious, true or false => …AnnotatedOrNotAnnotated };
    if ( additionalAnnotation is not null ) { syntax = syntax.WithAdditionalAnnotations( additionalAnnotation ); }
```

skips the annotation. Generated code then loses its nullable-oblivious / annotated distinction and the
Roslyn simplifier makes different decisions. There is no diagnostic, no log and no assertion. Because
the two Roslyn variants are separate builds, this can be true in one variant and false in the other.

### 5.2 `ObjectDisplayOptions` value drift

`SyntaxGeneration/ObjectDisplayOptions.cs` is a hand-copy of a Roslyn internal enum whose values are
cast numerically into Roslyn (`SyntaxFactoryEx.LiteralFormatter.cs:39, 61`). If Roslyn renumbers,
`IncludeTypeSuffix` (used unconditionally for `decimal`, `SyntaxFactoryEx.cs:353`) or
`UseHexadecimalNumbers` becomes some other option and every literal in every generated file is formatted
wrongly, while still parsing. The only protection is the comment at line 234.

### 5.3 The experimental-node filter drops a stabilised-but-still-marked construct

`TreeReader.RemoveExperimentalDeclarations` is a whole-node and whole-field delete, and the
`RoslynVersionSyntaxVerifier` never learns about the removed construct. So a template using a construct
that Roslyn parses but still marks experimental is:

- not rejected by `RoslynVersionSyntaxVerifier` (which is the mechanism that produces the "requires a
  newer Roslyn" diagnostic),
- not rewritten by `MetaSyntaxRewriter`,
- not hashed by `RunTimeCodeHasher` / `CompileTimeCodeHasher`.

The optional-field case (§4.5) is the sharpest: a labelled `break` compiles, the template compiles, and
the label vanishes from the expanded code.

### 5.4 `LanguageVersionJsonConverter` accepts any integer

`Serialization/LanguageVersionJsonConverter.cs:27` — `return (LanguageVersion) reader.GetInt32();`
An unchecked enum cast always succeeds. A compile-time project manifest written by a Metalama build
that knows `CSharp15` (1500) and read by one that does not yields a `LanguageVersion` value that
Roslyn's own `IsValid()` rejects, and which compares greater than every known version in the
`>=` tests of `LanguageVersionProvider.cs:223` and `:274`. No exception, no diagnostic.

`Serialization/ManifestSerializer.cs:158-172` compounds this: `TryDeserialize` catches `JsonException`
and returns `false`, and `Deserialize` (line 145) converts that into a `JsonException` whose message
names only the type. A manifest that deserialises structurally but carries an unknown language version
never reaches either path.

### 5.5 Constraint clauses drop silently

`ContextualSyntaxGenerator.ConstraintClauses` lines 283-342: the `genericParameter.TypeKindConstraint`
switch (lines 256-293 of the method body, file lines 291-328) has **no default case**, and
`TypeParameterConstraintClauses` lines 964-1024 is an `if`/`else if` chain with no fallback. A
constraint form the code model gains but these two do not know is emitted as nothing, and the generated
declaration is less constrained than the original. The asymmetry is already visible: the `IGeneric`
overload emits `allows ref struct` (line 343) and the `ITypeParameterSymbol` overload does not.

### 5.6 `TextSpanClassifier` colours nothing for unhandled declarations

Per §1.6. A compile-time `interface` today, and a compile-time `union` tomorrow, keep their default
colouring in the editor and in the HTML output. Design-time-only, cosmetic, and invisible in tests
unless a classification baseline covers the construct.

### 5.7 `SafeIdentifier` and contextual keywords

`SyntaxFactoryEx.cs:137` / `:168` — `SyntaxFacts.GetKeywordKind` covers reserved keywords only. A
declaration named `field`, `record`, `required`, `union` or `closed` is emitted unescaped. Whether that
is wrong depends on position: it is wrong for `field` inside a property accessor (which is why
`TemplateSyntaxFactoryImpl.EscapeIdentifier` exists, §3.2 item 4) and harmless elsewhere.

### 5.8 `RenderInterpolatedString` drops unknown content

`ContextualSyntaxGenerator.cs:512-544` — the `switch ( content.Kind() )` handles
`InterpolatedStringText` and `Interpolation` and has no default, so any third content kind is
**dropped from the rebuilt list** (line 546 rebuilds `contents` wholesale). Today Roslyn has only those
two; a third would silently truncate the string.

### 5.9 `SyntaxSerializationService` name-based fallback

`SyntaxSerializationService.cs:196-200` falls back to a lookup by `Type.FullName` and explicitly skips
`ValidateContractType` (comment lines 193-195), then `ConvertCrossAssemblyObject` (lines 328-356) copies
fields by name into an uninitialised instance. A type whose full name matches but whose field set has
drifted is copied partially, silently, with the missing fields left at their default values.

### 5.10 `SerializableTypes` reports false positives by design

`SyntaxSerialization/SerializableTypes.cs:106-109` — `"It may return false positives."` The
compile-time check passes and the failure surfaces only at serialisation time as
`InvalidOperationException` from `SyntaxSerializationService.Serialize` (line 281).

---

## 6. Adjacent files this subsystem depends on

### 6.1 Modifier emission

- `Metalama.Framework.Engine/CodeModel/Helpers/ModifierCategories.cs` (25 lines)
- `Metalama.Framework.Engine/CodeModel/Helpers/ModifierHelper.cs` (386 lines)
- `Metalama.Framework.Engine/Utilities/Roslyn/SymbolModifiersHelper.cs`

### 6.2 Visitor and rewriter bases

- `Metalama.Framework.Engine/CodeModel/Visitors/TypeVisitor.cs` (48 lines) — the `IType` dispatch, §3.2
- `Metalama.Framework.Sdk/Utilities/Roslyn/SafeSyntaxRewriter.cs` — `sealed override Visit` wrapping
  every exception in `SyntaxProcessingException` (lines 44-62) plus a `RecursionGuard`; derived classes
  override `VisitCore` (line 64)
- `Metalama.Framework.Sdk/Utilities/Roslyn/SafeSyntaxWalker.cs` — same shape (lines 111-139)
- `Metalama.Framework.Sdk/Utilities/Roslyn/SafeSyntaxVisitor.cs`, `SafeSyntaxVisitor{T}.cs`

  All four delegate to `CSharpSyntaxRewriter` / `CSharpSyntaxWalker`, whose dispatch table is Roslyn's
  and therefore already knows every node of the compiled-against Roslyn. A node the derived class does
  not override is walked by the base implementation and returned unchanged.

### 6.3 Annotations and trivia helpers

- `Metalama.Framework.Sdk/Formatting/FormattingAnnotations.cs` (185 lines) — `SourceCodeAnnotation`
  (line 62), `SystemGeneratedCodeAnnotation` (line 49), `WithSimplifierAnnotation` (line 79) backed by
  a `_simplifier` field injected by the engine (`Initialize`, line 67, comment: *"This property must be
  set by the engine assembly because we don't want a dependency on workspaces here."*),
  `WithAnnotationInsideBlock` (lines 82-101, special-cases `BlockSyntax`)
- `Metalama.Framework.Sdk/Formatting/TextSpanClassification.cs` — the classification enum; the header
  comment warns that declaration order is significant and that renaming breaks string-based tests
- `Metalama.Framework.Engine/Utilities/Roslyn/SyntaxExtensions.cs:129-344` — `GetParameterList`,
  `NormalizeWhitespaceIfNecessary`, `WithSimplifierAnnotationIfNecessary`, the `WithOptional*` family
- `Metalama.Framework.Engine/Utilities/Roslyn/WorkspaceHelper.cs`

### 6.4 Analyzers that police this subsystem's conventions

`Metalama.Framework/src/Metalama.Framework.Engine.Analyzers/`:

- `UnsafeIdentifierAnalyzer.cs:23` — `LAMA0850`, *"Call to SyntaxFactory.{0} with dynamic name may
  produce invalid code if the name is a C# keyword."* Suppressed 8 times inside
  `SyntaxGeneration/SyntaxFactoryEx.cs` (lines 99, 121, 128, 206, 215, 226, 235 and their `restore`).
- `MetalamaPerformanceAnalyzer.cs:24` — `LAMA0830`, `NormalizeWhitespace` is expensive. Suppressed at
  `SyntaxExtensions.cs:141`, `ContextualSyntaxGenerator.NormalizeSpaceRewriter.cs:106`.
- `MetalamaPerformanceAnalyzer.cs:41` — `LAMA0832`, avoid `WithLeadingTrivia`/`WithTrailingTrivia`.
  Suppressed at `SyntaxExtensions.cs:178`, `CodeFormatter.SimplifierFixer.cs:137`.
- `KindCheckOptimizationAnalyzer.cs:26` — `LAMA0860`, pattern matching on `SyntaxNode` / `ISymbol` /
  `IDeclaration` subtypes must be preceded by a `Kind` check. This is why almost every switch in this
  subsystem is written as `case SyntaxKind.X when node is XSyntax x:` rather than `case XSyntax x:`.
  A new construct therefore needs **both** a `SyntaxKind` case and a type pattern, and forgetting the
  kind is caught by the analyzer while forgetting the type pattern is not.

### 6.5 Code generation from the grammar

`eng/src/GenerateMetaSyntaxRewriter/`:

- `GenerateMetaSyntaxRewriter.cs:16-18` —
  `legacyVersionNames = ["4.0.1","4.4.0","4.8.0","4.12.0"]` (considered but not code-generated),
  `versionNames = [...legacy, "5.0.0", "5.10.0"]`
- `GenerateMetaSyntaxRewriter.cs:39-48` — the five generated artefacts
- `Generator.cs:64-98` `GenerateRoslynApiVersionEnum`; `:100-…` `GenerateVersionChecker` with
  `IsVersionSpecificType` (line 118); `:442-452` the min/max version pairing
- `Model/TreeReader.cs`, `Model/TreeType.cs:27-37`, `Model/Field.cs:41-51` — the experimental filter
- `Syntax-5.0.0.xml`, `Syntax-5.10.0.xml`
- output goes to `Metalama.Framework/.generated/<version>/…`, which is `.gitignore`d
  (`.gitignore:62`). On this machine only `.generated/4.12.0/` and `.generated/5.0.0/` exist; they are
  stale build outputs, not evidence about the branch.

### 6.6 Version tables

- `Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs`
- `Metalama.Framework.Engine/Utilities/LanguageVersionProvider.cs`
- `Metalama.Framework.Engine/Utilities/Roslyn/CompilationExtensions.cs:114-124`
- `Metalama.Framework.Engine/Services/CompilationContext.cs:112-174`
- `eng/RoslynVersions/Roslyn.5.0.0.props`, `Roslyn.5.10.0.props`, `Latest.props`

### 6.7 Premium repository

`C:/src/Metalama-2027.0/Metalama.Premium` consumes this subsystem from four files only, all in the
code-fixes extension, and none of them enumerates language constructs:

- `src/Metalama.Extensions.CodeFixes.DesignTime/AddAspectAttributeCodeActionModel.cs`
- `src/Metalama.Extensions.CodeFixes.Engine/CodeFixPipeline.cs`
- `src/Metalama.Extensions.CodeFixes.Engine/Implementations/AddAttributeCodeAction.cs`
- `src/Metalama.Extensions.CodeFixes.Engine/Implementations/ChangeVisibilityCodeAction.cs`

`AddAttributeCodeAction` is the one to check when `ContextualSyntaxGenerator.AddAttribute`
(line 780) gains a declaration kind, because it is the public caller of that method.

---

## 7. Shortest correct plan for C# 15 in this subsystem

1. Decide the fate of `TreeReader.RemoveExperimentalDeclarations`. Nothing else can proceed while it
   deletes all four constructs. Either wait for Roslyn to drop `ExperimentalAttribute`, or replace the
   blanket removal with an allow-list and accept `RSEXPERIMENTAL` suppression in the generated files.
2. Bump the five version tables together (§3.3): `SupportedCSharpVersions.Latest`, `.All`,
   `.ToLanguageVersion`, `.GetMaxLanguageVersion`, `LanguageVersionProvider.GetLanguageVersionFromDotNetSdk`.
3. Reintroduce `#if ROSLYN_5_10_0_OR_GREATER` in production source, and amend the comments in
   `eng/RoslynVersions/Roslyn.5.0.0.props:8-10` and `Roslyn.5.10.0.props:22-23`, which currently assert
   that no production source branches on the constant.
4. Four hand edits inside this subsystem, all guarded:
   `ContextualSyntaxGenerator.cs:793-816` (union in `AddAttribute`),
   `ContextualSyntaxGenerator.cs:142` and `:167` (`TypeKind.Union` in the two `TypeKind` lists),
   `ContextualSyntaxGenerator.cs:1052` (`SyntaxKind.UnsafeExpression => false`),
   `Formatting/TextSpanClassifier.cs:115` (`VisitUnionDeclaration`).
5. One adjacent edit for `closed`: `ModifierCategories.cs:12-23` and
   `ModifierHelper.GetTypeSyntaxModifierList` (line 198), with the matching change in
   `SymbolModifiersHelper.cs`.
6. One adjacent edit for a new `TypeKind`: `TypeKind.cs` and `TypeVisitor.cs:16-27, 39`, following the
   `VisitExtensionBlock` fallback pattern so no visitor in this subsystem has to change.
7. Close the two silent gaps found here regardless of C# 15:
   `NullableSyntaxAnnotationEx` (§5.1) should assert rather than degrade, and
   `LanguageVersionJsonConverter.Read` (§5.4) should validate the integer.
