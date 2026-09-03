# Terrain map: the template compiler (T#) and the MetaSyntaxRewriter generator

Scope of this map:

- `Metalama.Framework/src/Metalama.Framework.Engine/Templating/**`
- `eng/src/GenerateMetaSyntaxRewriter/**`
- The generated output of that generator, which lands in `Metalama.Framework/.generated/<roslyn version>/**`
  (git-ignored, produced by `Build.ps1 prepare`).

All paths are repository-relative to `C:/src/Metalama-2027.0/Metalama` unless stated otherwise.

---

## 1. The generator: what `Syntax-*.xml` produces

### 1.1 Entry point and version list

`eng/src/GenerateMetaSyntaxRewriter/GenerateMetaSyntaxRewriter.cs`

```csharp
16  var deprecatedVersionNames = Array.Empty<string>();
17  string[] legacyVersionNames = ["4.0.1", "4.4.0", "4.8.0", "4.12.0"]; // versions that should be considered when generating code, but not have their own generated code
18  string[] versionNames = [.. legacyVersionNames, "5.0.0", "5.10.0"];
```

- Line 24-25: one `SyntaxDocument` per version name, index = position in `versionNames`.
- Line 28: `VersionDetector.DetectVersions( syntaxDocuments )` computes, for every node, field and field kind,
  the lowest (and for fields, the highest) grammar version in which it appears.
- Lines 30-49: for every *non-legacy* version (today `5.0.0` and `5.10.0`) it writes six files into
  `.generated/<version>/`:

  | Generator method | Output file | Target project | Line |
  |---|---|---|---|
  | `GenerateRoslynApiVersionEnum` | `Metalama.Framework.Engine/RoslynApiVersion.g.cs` | Engine | 39 |
  | `GenerateTemplateFiles` | `Metalama.Framework.Engine/MetaSyntaxRewriter.g.cs` | Engine | 44 |
  | `GenerateVersionChecker` | `Metalama.Framework.Engine/RoslynVersionSyntaxVerifier.g.cs` | Engine | 45 |
  | `GenerateHasher` (run-time) | `Metalama.Framework.DesignTime/RunTimeCodeHasher.g.cs` | DesignTime | 46 |
  | `GenerateHasher` (compile-time) | `Metalama.Framework.DesignTime/CompileTimeCodeHasher.g.cs` | DesignTime | 47 |
  | `GeneratePartialUpdate` | `Metalama.Framework.Engine/SyntaxNodePartialUpdateExtensions.g.cs` | Engine | 48 |

  Wiring into the build:
  `Metalama.Framework/src/Metalama.Framework.Engine/Metalama.Framework.Engine.csproj:37-38` and
  `Metalama.Framework/src/Metalama.Framework.DesignTime/Metalama.Framework.DesignTime.csproj:34-35`
  glob `../../.generated/$(ThisRoslynVersionNoPreview)/<project>/*.cs`, falling back to a
  `<version>-stubs` directory that does not exist in the tree today.
  `ThisRoslynVersionNoPreview` is `5.0.0` or `5.10.0`
  (`eng/RoslynVersions/Roslyn.5.0.0.props:5`, `eng/RoslynVersions/Roslyn.5.10.0.props:19`).

### 1.2 The object model read from the XML

`eng/src/GenerateMetaSyntaxRewriter/Model/`

- `Tree.cs` — root; `Types` is a heterogeneous list of `Node`, `AbstractNode`, `PredefinedNode`.
- `TreeType.cs` — `Name`, `Base`, `SkipConvenienceFactories`, `ExperimentalUrl` (l. 305),
  `IsExperimental` (l. 310), `Children`, and the computed `MinimalRoslynVersion` (l. 324).
- `Node.cs` — adds `Kinds` (l. 270) and the flattened `Fields` (l. 272).
- `Field.cs` — `Name`, `Type`, `Optional`, `Override`, `New`, `MinCount`, `AllowTrailingSeparator`,
  `ExperimentalUrl` (l. 199), `Kinds` (l. 207), and the computed `MinimalRoslynVersion` /
  `MaximalRoslynVersion` / `KindsMinimalRoslynVersions` (l. 217-223).
- `Kind.cs` — a `SyntaxKind` name, with value equality on the name.
- `TreeFlattening.cs` — flattens `Choice` / `Sequence` into a flat `Fields` list; every child of a
  `Choice` becomes `Optional="true"` (l. 372-376).
- `TreeReader.cs:113-152` — **`RemoveExperimentalDeclarations`**: removes every `TreeType` and every
  `Field` carrying `ExperimentalUrl`, recursively through `Choice`/`Sequence`. Rationale in the doc
  comment: Roslyn annotates those APIs with `ExperimentalAttribute`, so referencing them from generated
  code is an `RSEXPERIMENTAL` error.
- `VersionDetector.cs:11-57` — for every node present in the *latest* document, computes the minimal
  version across all documents; per field, minimal and maximal; per field kind, minimal.

### 1.3 What a NEW SYNTAX NODE causes to be generated

Assume a new `<Node Name="FooSyntax" Base="ExpressionSyntax">` with kind `Foo` appears in
`Syntax-5.10.0.xml` and not in `Syntax-5.0.0.xml`, and is not experimental.

1. **`MetaSyntaxRewriter.g.cs`** (`Generator.GenerateMetaSyntaxRewriter`, `Generator.cs:396-525`):
   two methods per node.
   - `public override SyntaxNode? VisitFoo( FooSyntax node )` (l. 409-422): switches on
     `this.GetTransformationKind( node )`; `Transform` → `TransformFoo(node)`, anything else →
     `base.VisitFoo(node)`.
   - `protected virtual ExpressionSyntax TransformFoo( FooSyntax node )` (l. 425-523): builds
     `SyntaxFactory.Foo( Transform(node.Field1), Transform(node.Field2), ... )` as syntax. If the node
     has more than one `Kind`, a leading `Argument(this.Transform(node.Kind()))` is emitted (l. 497-501).
   - Only generated for the version documents where the node exists. In the `5.0.0` generated file the
     methods are absent entirely, so `MetaSyntaxRewriter` in that variant simply inherits Roslyn's
     `VisitFoo`, which does *not* transform. See §5.
2. **`MetaSyntaxFactoryImpl`** (`Generator.GenerateMetaSyntaxFactory`, `Generator.cs:527-613`):
   `public InvocationExpressionSyntax Foo( ExpressionSyntax field1, ... )` returning
   `SyntaxFactory.InvocationExpression( SyntaxFactory.Foo, ... )`. A second "minimal factory" overload
   is emitted when the node has auto-creatable token fields (l. 597-602).
3. **`RoslynVersionSyntaxVerifier.g.cs`** (`Generator.GenerateVersionChecker`, `Generator.cs:100-174`):
   because `IsVersionSpecificType( t ) => t.MinimalRoslynVersion!.Index > 0` (l. 160), the node gets

   ```csharp
   public override void VisitFoo( FooSyntax node )
   {
       this.VisitVersionSpecificNode( node, RoslynApiVersion.V5_10_0 );
   }
   ```

   Example of the existing shape, `.generated/5.0.0/Metalama.Framework.Engine/RoslynVersionSyntaxVerifier.g.cs:30-33`:

   ```csharp
   public override void VisitFieldExpression( FieldExpressionSyntax node )
   {
       this.VisitVersionSpecificNode( node, RoslynApiVersion.V5_0_0 );
   }
   ```

4. **`RunTimeCodeHasher.g.cs` / `CompileTimeCodeHasher.g.cs`** (`Generator.GenerateHasher`,
   `Generator.cs:615-712`): a `VisitFoo` that hashes each field. Tokens are hashed by kind
   (`VisitTrivialToken`) unless the field's kinds include an identifier or a literal token
   (`IsTrivialToken`, l. 725-735), in which case the text is hashed. In the run-time hasher, fields of
   type `BlockSyntax`, `ArrowExpressionClauseSyntax` and `EqualsValueClauseSyntax` are reduced to a
   null/not-null bit (`IgnoreFieldContentInRunTimeCode`, l. 714-723).
5. **`SyntaxNodePartialUpdateExtensions.g.cs`** (`Generator.GeneratePartialUpdate`, `Generator.cs:737-803`):
   `public static FooSyntax PartialUpdate( this FooSyntax node, Option<T> field1 = default, ... )`
   forwarding to `node.Update( ... )`.
6. **`RoslynApiVersion.g.cs`** is unaffected by a node; it enumerates versions only
   (`Generator.cs:64-98`).

Nothing else is generated. In particular the generator emits **no** `TemplateAnnotator` override, **no**
`TemplateCompilerRewriter` override and **no** diagnostic. Those are all hand-written.

### 1.4 What a NEW OPTIONAL FIELD on an existing node causes to be generated

Assume `BreakStatementSyntax` gains `<Field Name="Name" Type="IdentifierNameSyntax" Optional="true" />`
in `Syntax-5.10.0.xml` only (this is exactly the labeled-`break` case, currently marked experimental —
`Syntax-5.10.0.xml:1296` and `:1307`).

1. **`TransformBreakStatement` becomes version-switched** (`Generator.cs:432-479`). Because
   `node.Fields.Select( f => f.MinimalRoslynVersion ).Distinct()` now has more than one element, the
   generator emits `switch ( this.TargetApiVersion ) { case RoslynApiVersion.V4_0_1: ... case
   RoslynApiVersion.V5_10_0: ... default: throw new AssertionFailedException(); }`, with one
   `SyntaxFactory.BreakStatement(...)` call per field set. The existing example of this shape is
   `TransformClassDeclaration` in `.generated/5.0.0/.../MetaSyntaxRewriter.g.cs:4441-4501`, split on
   `ClassDeclarationSyntax.ParameterList` (added in Roslyn 4.8).
2. **`MetaSyntaxFactoryImpl.BreakStatement`** gains a parameter (`Generator.cs:594`, from `node.Fields`
   of the *current* document only, so the 5.0.0 variant keeps the three-argument form).
3. **`RoslynVersionSyntaxVerifier`** gets, via the second loop (`Generator.cs:127-156`) and
   `IsVersionSpecificField` (l. 162):

   ```csharp
   public override void VisitBreakStatement( BreakStatementSyntax node )
   {
       this.VisitVersionSpecificField( node.Name, RoslynApiVersion.V5_10_0 );
   }
   ```

   The existing example is `VisitUsingDirective` /`VisitClassDeclaration` in
   `.generated/5.0.0/.../RoslynVersionSyntaxVerifier.g.cs:131-139`.
4. **Both hashers** gain a `this.Visit( node.Name );` line, so a labeled break changes the code hash and
   therefore invalidates the design-time cache.
5. **`PartialUpdate`** gains an `Option<IdentifierNameSyntax?> name = default` parameter and passes it
   to `node.Update(...)`. See §5 for what happens *before* that field is un-experimentalised.

### 1.5 What a NEW SyntaxKind on an existing field causes to be generated

Only `RoslynVersionSyntaxVerifier` changes (`Generator.cs:135-153`, helper
`GetVersionSpecificKinds` at l. 164-173):

```csharp
public override void VisitLiteralExpression( LiteralExpressionSyntax node )
{
    switch ( node.Token.Kind() )
    {
        case SyntaxKind.Utf8StringLiteralToken:
            this.VisitVersionSpecificFieldKind( node.Token, RoslynApiVersion.V4_4_0 );
            break;
        ...
    }
}
```

(`.generated/5.0.0/.../RoslynVersionSyntaxVerifier.g.cs:92-112`; the C# 14 compound-assignment operators
are the same mechanism at l. 148-190 and 195-237.)

Guard at `Generator.cs:166-170`: if *every* kind of the field is version-specific, none is reported.
The rewriter itself is unaffected, because `MetaSyntaxRewriter.Transform( SyntaxToken )`
(`Templating/MetaSyntaxRewriter.cs:239-294`) is kind-generic: it special-cases `None`, `IdentifierToken`
and the three literal tokens, and otherwise emits `SyntaxFactory.Token( kind )` or the five-argument
`SyntaxFactory.Token( leading, kind, text, valueText, trailing )` overload.

### 1.6 The experimental filter is the single switch for C# 15

`Syntax-5.10.0.xml` already declares all four Roslyn 5.10 grammar additions, all marked experimental,
and `TreeReader.RemoveExperimentalDeclarations` deletes them before any code is generated:

| Grammar element | Line in `Syntax-5.10.0.xml` | `ExperimentalUrl` |
|---|---|---|
| `UnsafeExpressionSyntax` (`unsafe(expr)`) | 496 | dotnet/roslyn#82789 |
| `WithElementSyntax` (`with(...)` collection element, base `CollectionElementSyntax`) | 816 | dotnet/roslyn#82210 |
| `BreakStatementSyntax.Name` | 1296 | dotnet/roslyn#83266 |
| `ContinueStatementSyntax.Name` | 1307 | dotnet/roslyn#83266 |
| `UnionDeclarationSyntax` (base `TypeDeclarationSyntax`, `SkipConvenienceFactories`) | 1954 | dotnet/roslyn#82567 |

Removing an `ExperimentalUrl` attribute from the grammar file is therefore the act that turns the
generated support on. `Metalama.Framework/docs/updating-roslyn.md:11` states the standing policy:
"Study the new C# syntax features. We IGNORE any experimental feature. They are not supported."

---

## 2. Files and types sensitive to the shape of the C# language

Ordered by weight.

### 2.1 `Templating/TemplateAnnotator.cs` (3516 lines) — the classifier

`internal sealed partial class TemplateAnnotator : SafeSyntaxRewriter, IDiagnosticAdder` (l. 39).

Per-construct overrides (the full census; each line is the `public override` declaration):

| Line | Member | Notes |
|---|---|---|
| 627 | `protected override SyntaxNode? VisitCore` | → `DefaultVisitImpl` |
| 630 | `DefaultVisitImpl` | the fall-through for every unhandled node |
| 648 | `AddScopeAnnotationToVisitedNode` | the default scope rule; see §5.1 |
| 702 | `VisitAnonymousObjectMemberDeclarator` | |
| 709 | `VisitAnonymousObjectCreationExpression` | |
| 743 | `VisitClassDeclaration` | → `VisitTypeDeclaration` |
| 745 | `VisitStructDeclaration` | |
| 748 | `VisitRecordDeclaration` | |
| 751 | `VisitDelegateDeclaration` | |
| 754 | `VisitEnumDeclaration` | |
| 756 | `VisitTypeDeclaration<T>` | the shared body; **no `VisitInterfaceDeclaration`, no `VisitExtensionBlockDeclaration`** |
| 777 | `VisitIdentifierName` | also the `dynamic`-in-local-function check |
| 876 | `VisitMemberBindingExpression` | |
| 883 | `VisitMemberAccessExpression` | |
| 910 | `VisitConditionalAccessExpression` | |
| 1030 | `VisitElementAccessExpression` | |
| 1064 | `VisitInvocationExpression` | the largest single method |
| 1319 | `VisitArgument` | |
| 1327 | `VisitIfStatement` | |
| 1375 | `VisitBreakStatement` | `node.AddScopeAnnotation( this._currentScopeContext.CurrentBreakOrContinueScope )` |
| 1378 | `VisitContinueStatement` | same |
| 1381 | `VisitForEachStatement` | |
| 1445 | `VisitForEachVariableStatement` | |
| 1508 | `VisitDeclarationPattern` | |
| 1533 | `VisitIsPatternExpression` | |
| 1562 | `VisitSingleVariableDesignation` | |
| 1586 | `VisitDeclarationExpression` | |
| 1611 | `VisitVariableDeclarator` | |
| 1742 | `VisitVariableDeclaration` | |
| 1812 | `VisitLocalDeclarationStatement` | |
| 1821 | `VisitAttribute` / 1836 `VisitAttributeList` | |
| 1898 | `VisitConstructorDeclaration` | returns `node` unchanged |
| 1900 | `VisitMethodDeclaration` | |
| 1911 | `VisitParameter` / 1956 `VisitTypeParameter` | |
| 1983 | `VisitAccessorDeclaration` | |
| 1990 | `VisitPropertyDeclaration` / 1998 `VisitEventDeclaration` | |
| 2003 | `VisitPostfixUnaryExpression` / 2010 `VisitPrefixUnaryExpression` | |
| 2041 | `VisitAssignmentExpression` | tuple-`var` deconstruction analysis at 2056-2099 |
| 2243 | `VisitExpressionStatement` | |
| 2268 | `VisitCastExpression` | |
| 2358 | `VisitBinaryExpression` / 2396 `VisitCoalesceExpression` | |
| 2437 | `VisitConditionalExpression` | |
| 2480 | `VisitForStatement` / 2522 `VisitWhileStatement` / 2548 `VisitDoStatement` | |
| 2576 | `VisitReturnStatement` | |
| 2594 | `VisitUnsafeStatement` | reports LAMA0101 `"unsafe"` |
| 2601 | `VisitGotoStatement` | reports LAMA0101 `"goto"` |
| 2608 | `VisitLocalFunctionStatement` | |
| 2661 | `VisitAnonymousMethodExpression` | |
| 2668 | `VisitQueryExpression` | reports LAMA0101 `"LINQ"` |
| 2675 | `VisitAwaitExpression` / 2689 `VisitYieldStatement` | |
| 2710 | `VisitFieldExpression` | C# 14 `field`; returns `node.AddScopeAnnotation( RunTimeOnly )` |
| 2779 | `VisitParenthesizedLambdaExpression` / 2803 `VisitSimpleLambdaExpression` | |
| 2810 | `VisitSwitchExpressionArm` / 2839 `VisitSwitchExpression` | |
| 2877 | `VisitSwitchStatement` / 2954 `VisitCasePatternSwitchLabel` | |
| 3028 | `VisitLockStatement` / 3044 `VisitUsingStatement` | |
| 3064 | `VisitArrayType` / 3079 `VisitRefType` | |
| 3094 | `VisitGenericNameCore<T>` / 3158 `VisitGenericName` / 3205 `VisitTupleType` / 3207 `VisitNullableType` | |
| 3222 | `VisitObjectCreationExpression` / 3271 `VisitWithExpression` | |
| 3300 | `VisitThrowExpression` / 3312 `VisitThrowStatement` / 3324 `VisitTryStatement` / 3357 `VisitCatchDeclaration` | |
| 3369 | `VisitTypeOfExpression` / 3402 `VisitArrayRankSpecifier` | |
| 3423 | `VisitThisExpression` / 3435 `VisitTupleExpression` | |
| 3450 | `VisitInterpolatedStringExpression` | hard `AssertionFailedException` on an unknown content kind, l. 3483 |
| 3492 | `VisitInitializerExpression` | |
| 3495 | `VisitCollectionExpression` | visits `node.Elements`, combines scopes |
| 3505 | `VisitDefaultExpression` | |

Diagnostic helper: `ReportUnsupportedLanguageFeature` at l. 2591, in the `#region Unsupported Features`
that spans l. 2589-2719.

Support types:
- `TemplateAnnotator.ScopeContext.cs` — `CurrentBreakOrContinueScope` (l. 21), `BreakOrContinue` (l. 123).
- `TemplateAnnotator.RedundantReturnVisitor.cs` — flow analysis by statement kind
  (`VisitBlock` 101, `VisitIfStatement` 106, `VisitSwitchStatement` 120, `VisitTryStatement` 138,
  `DefaultVisit` 65).
- `TemplateAnnotator.TypeParameterDetectionVisitor.cs:24` — `DefaultVisit( ISymbol )` throws.

### 2.2 `Templating/TemplateCompilerRewriter.cs` (3299 lines) — the T# → C# rewriter

`internal sealed partial class TemplateCompilerRewriter : MetaSyntaxRewriter`. 138 `SyntaxKind.`
references, the highest density in the subsystem. Key kind-sensitive sites:

- `GetTransformationKind` (l. 196) and `IsCompileTimeCode` (l. 199-264). The nested `GetFromParent`
  (l. 236-263) hard-codes the statements whose children *must* carry their own annotation:
  `IfStatement`, `ElseClause`, `SwitchSection`, `ForEachStatement`, `ForEachVariableStatement`,
  `WhileStatement`, `DoStatement` (l. 251-257) → `AssertionFailedException` (l. 258).
- `VisitFieldExpression` (l. 310-316) — C# 14 `field`; rewrites to
  `ITemplateSyntaxFactory.GetPropertyBackingField()`.
- `Transform( SyntaxToken )` override (l. 394).
- `TransformIdentifierName` (l. 476-501) — switches on `GlobalKeyword`, `VarKeyword`, `IdentifierToken`;
  `AssertionFailedException` at l. 488 for any other identifier kind.
- `CreateRunTimeExpression` (l. 641-...) — the big expression-kind switch at l. 649-731:
  `NullLiteralExpression`, `DefaultLiteralExpression`, `DefaultExpression`, `IdentifierName`,
  `InvocationExpression`, `SimpleLambdaExpression`, `ThisExpression`, `TypeOfExpression`. The
  `ThisExpression` arm enumerates 22 binary/invocation kinds at l. 703-711 purely to pick a location for
  the diagnostic. Type-name switch at l. 800-853, `AssertionFailedException` at l. 842.
- `VisitInvocationExpression` (l. 1182) — `AssertionFailedException` at l. 1373 and l. 1549.
- `VisitBlock` (l. 1952), `BuildRunTimeBlock` (l. 1981, 2003, 2055),
  `GetFunctionLikeRunTimeBlockInfo` (l. 2011-2046) switching on `LocalFunctionStatement` and the three
  anonymous-function kinds.
- `ToMetaStatement` (l. 2212-2221) — the "cannot be embedded in an `if`" list:
  `LocalDeclarationStatementSyntax or LabeledStatementSyntax or LocalFunctionStatementSyntax` (l. 2218).
- `ToMetaStatements` (l. 2232-...) — `AssertionFailedException` at l. 2347 for an unexpected
  transformed-node kind.
- `TransformInterpolatedStringExpression` (l. 2444-2492) — `AssertionFailedException` at l. 2490.
- `VisitSwitchStatement` (l. 2548-2598) — the control-transfer list at l. 2568-2575:
  `BreakStatement`, `ContinueStatement`, `ReturnStatement`, `ThrowStatement`, `GotoCaseStatement`,
  `GotoDefaultStatement`, `GotoStatement`.
- `VisitAssignmentExpression` (l. 3245) and `TransformAssignmentExpression` (l. 3293-3299), which wraps
  every transformed assignment in `ITemplateSyntaxFactory.RewriteAssignmentExpression` — the C# 14
  null-conditional-assignment support.
- Nested partials:
  - `TemplateCompilerRewriter.BuildTimeOnlyRewriter.cs` — `CompileTimeOnlyRewriter`;
    `TryRewriteProceedInvocation` (l. 52) switches on `SimpleMemberAccessExpression` / `IdentifierName`
    with `AssertionFailedException` at l. 63; literal-kind list at l. 87-89;
    `VisitIdentifierName` (l. 140) with symbol-kind lists at l. 150-151 and 159-176.
  - `TemplateCompilerRewriter.StatementCompileTimeVariableFinder.cs` — a plain `CSharpSyntaxWalker`
    (not `SafeSyntaxWalker`); assignment-left kinds at l. 55-68, designation parents at l. 96-146,
    the "not visible after" kinds at l. 169 (`Block`, `SwitchSection`, `ParenthesizedExpression`).

### 2.3 `Templating/MetaSyntaxRewriter.cs` + its generated partial

`internal partial class MetaSyntaxRewriter : SafeSyntaxRewriter` (l. 31).

- Constructor takes `RoslynApiVersion targetApiVersion` (l. 42-49); `TargetApiVersion` (l. 54) is what
  the generated `switch ( this.TargetApiVersion )` blocks read.
- `GetTransformationKind` (l. 59) — virtual, default `Transform`.
- `Transform<T>( T? node )` (l. 106-139) — for a node the transformation kind says not to transform,
  it dispatches on `ExpressionSyntax` / `ArgumentSyntax` / `StatementSyntax` and otherwise throws
  `AssertionFailedException( $"Unexpected node kind: {node.Kind()}." )` (l. 132). Any *new* abstract
  node category (for example `CollectionElementSyntax`) lands here.
- `Transform( SyntaxToken )` (l. 239-294) — kind-generic, see §1.5.
- Overloads for `SeparatedSyntaxList<T>` (l. 144), `BracketedArgumentListSyntax` (l. 161),
  `ArgumentListSyntax` (l. 172), `ParameterListSyntax` (l. 176), `SyntaxTokenList` (l. 180),
  `SyntaxList<T>` (l. 207), `SyntaxList<StatementSyntax>` (l. 237), `bool` (l. 300).
- `MetaSyntaxRewriter.MetaSyntaxFactoryImpl.cs` — the hand-written half of the generated factory;
  `Kind( SyntaxKind kind )` at l. 78-82 emits `SyntaxKind.<name>` by `kind.ToString()`, so a new
  `SyntaxKind` needs no code change here.
- `MetaSyntaxRewriter.TransformationKind.cs` — `None` / `Clone` / `Transform`.
- `MetaSyntaxRewriter.IndentRewriter.cs` — trivia only.
- Generated partial: `.generated/5.0.0/Metalama.Framework.Engine/MetaSyntaxRewriter.g.cs`, 10 436 lines.

### 2.4 `Templating/RoslynVersionSyntaxVerifier.cs` + its generated partial

`internal sealed partial class RoslynVersionSyntaxVerifier : SafeSyntaxWalker` (l. 17).

- `MaximalAcceptableLanguageVersion` (l. 22) — supplied by the caller.
- `MaximalUsedVersion` (l. 24), initialised to `RoslynApiVersion.Lowest`.
- `OnForbiddenSyntaxUsed` (l. 32) reports `TemplatingDiagnosticDescriptors.TemplateUsesUnsupportedLanguageVersion`
  (LAMA0232).
- `VisitVersionSpecificNode` (l. 41), `VisitVersionSpecificField` (l. 55),
  `VisitVersionSpecificFieldKind` (l. 78). All three compare `version.ToLanguageVersion()` against
  `MaximalAcceptableLanguageVersion` and raise `MaximalUsedVersion`.
- Known limitation recorded in the source, l. 57-61: a field added in a new Roslyn that returns a
  non-null value for *old* code (the `UsingDirectiveSyntax.NamespaceOrType` generalisation of `Name`)
  is mis-detected. Any C# 15 field that generalises an existing one inherits this defect.
- The whole class is only reachable through `TemplateCompiler.TryAnnotate`
  (`Templating/TemplateCompiler.cs:106-108`).

### 2.5 `Templating/TemplatingCodeValidator.cs` + `.Visitor.cs`

`private sealed class Visitor : SafeSyntaxWalker, IDiagnosticAdder` (`.Visitor.cs:34`).

- Generic reference validation in `VisitCore` (l. 95-243); returns early when `_currentScope` has no
  value (l. 134-137).
- Per-declaration overrides: `VisitAttributeList` 284, `VisitBaseList` 294,
  `VisitClassDeclaration` 299, `VisitStructDeclaration` 307, `VisitRecordDeclaration` 315,
  `VisitInterfaceDeclaration` 323, `VerifyTypeDeclaration` 332, `VisitMethodDeclaration` 478,
  `VisitAccessorDeclaration` 510, `VisitPropertyDeclaration` 563, `VisitIndexerDeclaration` 588,
  `VisitArrowExpressionClause` 613, `VisitConstructorDeclaration` 641, `VisitDestructorDeclaration` 664,
  `VisitOperatorDeclaration` 684, `VisitConversionOperatorDeclaration` 707,
  `VisitLocalFunctionStatement` 730, `VisitEventDeclaration` 757, `VisitEventFieldDeclaration` 775,
  `VisitFieldDeclaration` 794, `VisitIncompleteMember` 813.
  **There is no `VisitEnumDeclaration` and no `VisitExtensionBlockDeclaration`.**
- The warning-suppression `hasBody` switch at l. 1112-1129 enumerates every member kind that can be a
  template and ends with `_ => throw new AssertionFailedException()` (l. 1128). A new member-declaration
  kind that reaches this switch crashes rather than misbehaves.

### 2.6 `Templating/TemplateExpansionContext.cs` (890 lines)

- `CreateReturnStatementVoid` (l. 606-657): expression-kind switch; `default` uses a discard, so it is
  safe for unknown expressions.
- `HasAnyYieldVisitor.DefaultVisit` (l. 799-846): an explicit **allow-list of 24 statement kinds**
  (l. 807-836). A statement kind not in that list is never descended into. See §5.3.
- `CheckTemplateLanguageVersion` (l. 861-878) reports `AspectUsesHigherCSharpVersion` (LAMA0282) by
  comparing `templateMember.TemplateClassMember.TemplateInfo.UsedApiVersion?.ToLanguageVersion()` against
  the target project's `LanguageVersion`.
- `TemplateExpansionContext.HasYieldInTryCatchVisitor.cs` — statement-shape analysis; `DefaultVisit`
  descends into every child (l. 22-35), so it is robust to new nodes.
- `BackingFieldName` — the C# 14 `field` plumbing consumed by `TemplateSyntaxFactoryImpl.GetPropertyBackingField`.

### 2.7 `Templating/TemplateSyntaxFactoryImpl.cs` (1002 lines)

The run-time half of `ITemplateSyntaxFactory`
(`Metalama.Framework/src/Metalama.Framework.CompileTimeContracts/ITemplateSyntaxFactory.cs`).
Kind-sensitive members:

- `ToStatement` l. 105-118, `ToStatementList` l. 119-205 (trivia kinds).
- `Boolean` l. 206, `DynamicLocalAssignment` l. 216 (checks `SimpleAssignmentExpression` at l. 224),
  `DynamicLocalDeclaration` l. 254 (`IdentifierName`/`IsVar` at l. 284).
- `SimplifyAnonymousFunction<T>` l. 365-425 — switches on `SimpleLambdaExpression` /
  `ParenthesizedLambdaExpression` with sub-cases.
- `ConditionalExpression` l. 430-445 (`TrueLiteralExpression` / `FalseLiteralExpression` folding).
- `RunTimeExpression` l. 501 (numeric-literal preservation at l. 519).
- `FixInterpolationSyntax` l. 704-740.
- `RewriteAssignmentExpression` l. 944-980 — the C# 14 null-conditional-assignment fix-up. It checks
  `assignmentExpression.Left.IsKind( SyntaxKind.ConditionalAccessExpression )` and re-shapes
  `a?.b = c` into `ConditionalAccessExpression(a, AssignmentExpression(MemberBinding(b), c))`, because
  "In Roslyn 5, the expression `a?.b = c` is parsed as ..." (comment l. 948-968).
- `GetPropertyBackingField` l. 991-999 — throws `InvalidOperationException` when
  `TemplateExpansionContext.BackingFieldName` is null.
- `ConvertToExpressionSyntax` l. 982-989 — `AssertionFailedException` on an unexpected type.
- `TemplateSyntaxFactoryImpl.SerializedTypeOfRewriter.cs` — `typeof` substitution.

### 2.8 `Templating/TemplatingDiagnosticDescriptors.cs` (720 lines)

Reserved id ranges `LAMA0100-0119` and `LAMA0220-0299` (comment l. 20). The three descriptors that carry
the language-version contract:

- `LanguageFeatureIsNotSupported` — **LAMA0101**, l. 24-30, `"'{0}' is not supported in a template."`
  This is the single diagnostic used to refuse a construct in a template.
- `TemplateUsesUnsupportedLanguageVersion` — **LAMA0232**, l. 247-253,
  `"Template code must be written in C# {0}."` Raised only by `RoslynVersionSyntaxVerifier`.
- `AspectUsesHigherCSharpVersion` — **LAMA0282**, l. 618-625, a *warning*, raised at expansion time when
  the aspect's `UsedApiVersion` exceeds the target project's `LangVersion`.

Adding a C# 15 feature to the "unsupported" list therefore costs no new descriptor: it is a call to
`ReportUnsupportedLanguageFeature( node, "<feature name>" )`.

### 2.9 `Templating/Statements/` and `Templating/Expressions/`

These are the *builder* API, that is, the objects a template author constructs from compile-time code.
They emit syntax; they do not analyse it. Their language sensitivity is therefore in what they can
produce, not in what they can accept.

- `Statements/IStatementImpl.cs`, `IStatementListImpl.cs` — the two interfaces; both take a nullable
  `TemplateSyntaxFactoryImpl`.
- `Statements/StatementList.cs:85-109` — `AssertionFailedException` on an unexpected item type.
- `Statements/UnwrappedBlockStatementList.cs:134` — checks `SyntaxKind.Block`.
- `Statements/SwitchStatement.cs:213-294` — builds `CaseSwitchLabel`, `CasePatternSwitchLabel`,
  `RecursivePattern`, `DefaultSwitchLabel`, and appends `BreakStatement()` at l. 277. Nothing here
  knows about a labeled `break`.
- `Statements/BlockStatement.cs`, `UserStatement.cs`, `TemplateInvocationStatement.cs` — thin.
- `Expressions/SyntaxBuilderImpl.cs` — `ParseExpression` l. 69-77 via
  `SyntaxFactoryEx.ParseExpressionSafe`, `ParseStatement` l. 78-... via `ParseStatementSafe`;
  literal-kind switch at l. 308-330.
- `Expressions/SourceUserExpression.cs:57-79` — `SuppressNullableWarningExpression`,
  `SimpleMemberAccessExpression`, `IdentifierName`.
- `Expressions/InterpolatedStringUserExpression.cs:40-120` — token construction;
  `AssertionFailedException` at l. 108.
- `Expressions/TupleItemExpression.cs:52-70`, `Expressions/TypedExpressionSyntaxImpl.cs:199-205`,
  `Expressions/CastUserExpression.cs:32`, `Expressions/DurableExpression.cs:66,99`.
- `Expressions/UserExpression.cs`, `UserReceiver.cs`, `ExpressionExtensions.cs` — the abstractions.

### 2.10 Other files in the folder

- `CompileTimeSideEffectDetector.cs` — expression-shape heuristics (l. 104, 110, 129, 173, 193).
  `HasCompileTimeSideEffect` returns **`true`** (report LAMA0288) for anything it does not recognise
  (l. 116-118), so a new expression form errs toward a false positive, not a silent miss.
- `SyntaxTreeAnnotationMap.cs` / `.AnnotatingRewriter.cs` — fully generic (`VisitCore` at l. 55);
  no per-kind logic apart from the `var` special case at l. 103.
- `TypeAnnotationMapper.cs:96` — one `ParenthesizedExpression` check.
- `FlattenBlocksRewriter.cs`, `InterpolationSyntaxHelper.cs`, `PreprocessorFixer.cs`,
  `RemoveConditionalAccessRewriter.cs` (`VisitMemberBindingExpression` l. 134,
  `VisitElementBindingExpression` l. 142) — small, shape-specific.
- `TemplateMemberClassifier.cs`, `TemplateMemberSymbolClassifier.cs`, `MetaMemberKind.cs` — symbol-based,
  not syntax-based; insensitive to new grammar.
- `SyntaxAnnotationExtensions.cs` — the scope-annotation encoding; five `AssertionFailedException` sites.
- `TemplateNameHelper.cs:42`, `TemplateLexicalScope.cs`, `TwoPhaseValidationResult.cs`,
  `WellKnownTemplateWarningSuppression(s).cs`, `MetaModel/MetaApi.cs` — declaration-kind switches
  at `MetaApi.cs:276,283,327,344`.

---

## 3. Files and types sensitive to the runtime, SDK, Roslyn version or host IDE

### 3.1 Inside the subsystem

| File / member | Sensitive to | Detail |
|---|---|---|
| `eng/src/GenerateMetaSyntaxRewriter/GenerateMetaSyntaxRewriter.cs:17-18` | Roslyn version | the hard-coded `legacyVersionNames` / `versionNames` lists |
| `eng/src/GenerateMetaSyntaxRewriter/Syntax-*.xml` | Roslyn version | one grammar snapshot per version; six files today |
| `eng/src/GenerateMetaSyntaxRewriter/Model/VersionDetector.cs` | Roslyn version | derives min/max versions per node, field and kind |
| `eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs:113-152` | Roslyn version | the experimental filter |
| `eng/src/GenerateMetaSyntaxRewriter/Generator.cs:432-479` | Roslyn version | emits `switch ( this.TargetApiVersion )` |
| `Templating/MetaSyntaxRewriter.cs:42,54` | Roslyn version | `TargetApiVersion` |
| `Templating/RoslynVersionSyntaxVerifier.cs` (whole file) | Roslyn version, language version | |
| `Templating/TemplateCompiler.cs:51,58-79,106-108,232` | SDK version, language version | `TemplateLanguageVersion` from `ILanguageVersionProvider`; `MetalamaTemplateLanguageVersion` override; `usedApiVersion` out-parameter |
| `Templating/TemplateExpansionContext.cs:861-878` | SDK version | LAMA0282 |
| `Templating/TemplateSyntaxFactoryImpl.cs:944-980` | Roslyn version | the comment names Roslyn 5 parse shape explicitly |
| `Templating/TemplatingCodeValidator.Visitor.cs:40,84` | host IDE | `_isDesignTime`, `_validateRunTimeCode` |
| `Templating/Expressions/SyntaxBuilderImpl.cs:71,80,308` | Roslyn version | `SyntaxFactory.ParseExpression` / `ParseStatement` with **no `ParseOptions`**, so the running Roslyn's default `LanguageVersion` applies |

### 3.2 Immediately adjacent, and load-bearing for this subsystem

| File / member | Detail |
|---|---|
| `Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:31-32` | `Latest => LanguageVersion.CSharp14` |
| `...:38-43` | `All` = C# 10 … 14 |
| `...:52-62` | `RoslynApiVersion.ToLanguageVersion`; note `V5_0_0` **and** `V5_10_0` both map to `CSharp14` |
| `...:77-87` | `ToNuGetVersionString`; `V5_10_0 => "5.10.0-1.26365.3"` |
| `...:134-144` | `ToVersion` |
| `...:149-159` | `GetMaxLanguageVersion( Version roslynVersion )`; `(>= 5, _) => CSharp14` |
| `Metalama.Framework.Engine/Utilities/AllLanguageVersions.cs:14-18` | `CSharp10`=1000 … `CSharp14`=1400; a `CSharp15 = (LanguageVersion) 1500` constant does not exist |
| `Metalama.Framework.Engine/Utilities/LanguageVersionProvider.cs:54-60` | **`version.Major switch { >= 10 => CSharp14, >= 9 => CSharp13, >= 8 => CSharp12, _ => throw }`** — the .NET SDK major → language version map |
| `Metalama.Framework.Engine/Utilities/LanguageVersionProvider.cs:74-123` | the msbuild.exe path: reads the assembly version of `Roslyn/Microsoft.CodeAnalysis.CSharp.dll` under `MSBuildBinPath` |
| `Metalama.Framework.Engine/Utilities/Roslyn/FlowAnalyzer.cs:42-89` | `NeverContinues`; `default: return false` |
| `Metalama.Framework.Engine/Utilities/Roslyn/SafeSyntaxRewriter.cs` (in `Metalama.Framework.Sdk`) | the base of `MetaSyntaxRewriter` and `TemplateAnnotator`; `Visit` is sealed, `VisitCore` is the extension point |
| `Metalama.Framework.CompileTimeContracts/ITemplateSyntaxFactory.cs` | the contract between compiled templates and the engine; `RewriteAssignmentExpression` l. 132, `GetPropertyBackingField` l. 137 |
| `Metalama.Framework/Aspects/CompiledTemplateAttribute.cs:43,49` | `IntroducesBackingField`, `IsBackingFieldAssigned` — added for C# 14 `field` |
| `eng/RoslynVersions/Roslyn.5.0.0.props`, `Roslyn.5.10.0.props`, `Latest.props` | the two variants; only `ROSLYN_5_10_0_OR_GREATER` is defined, and only aspect tests use it |
| `Metalama.Framework/docs/updating-roslyn.md` | the 12-step procedure; step 3 is the "ignore experimental" rule, step 12 forbids new `DefineConstants` |
| `Metalama.Framework/docs/platform-support.md` | PB-2027.0 |

Note on `RoslynApiVersion`: the generated enum
(`.generated/5.0.0/Metalama.Framework.Engine/RoslynApiVersion.g.cs`) still has
`V4_0_1 = 0 … V5_0_0 = 4`, with `Lowest = V4_0_1`, because the four 4.x grammars remain in
`legacyVersionNames` even though no 4.x *variant* ships. The 5.10 generated enum will additionally have
`V5_10_0 = 5`, `Current = V5_10_0`, `Highest = V5_10_0`.

---

## 4. How the previous wave, C# 14, was absorbed

The C# 14 issues that left code in this subsystem are **#1105**, **#1108**, **#1109** and **#1114**.
The others (#1034, #1035, #1036, #1094, #1110-#1113, #1115, #1116, #1127, #1131, #1143, #1159, #1160)
landed in the code model, the advice implementations and the linker, not in `Templating/`.

### 4.1 The four-step pattern

**Step 1 — refuse the feature, loudly, in the annotator.**
Commit `cf0861898b` ("#1105 Unsupported language features in templates: field keyword,
null-conditional assignments") added exactly two things to `TemplateAnnotator.cs`:

```csharp
#if ROSLYN_5_0_0_OR_GREATER
public override SyntaxNode? VisitFieldExpression( FieldExpressionSyntax node )
{
    this.ReportUnsupportedLanguageFeature( node, "field keyword" );
    return node;
}
#endif
```

and, inside `VisitAssignmentExpression`:

```csharp
if ( node.Parent.IsKind( SyntaxKind.ConditionalAccessExpression ) )
{
    // Null-conditional assignments are not implemented.
    this.ReportUnsupportedLanguageFeature( node, "null-conditional assignment" );
    // The rest of the analysis should be ok anyway.
}
```

Both used the existing LAMA0101 descriptor. The commit also added the two `Tests/Aspects/CSharp14/`
test files and their `.t.cs` baselines, in the same change.

**Step 2 — implement the feature end to end, and delete the refusal.**

- Null-conditional assignment: commit `b4da958605` added
  `ITemplateSyntaxFactory.RewriteAssignmentExpression`, its implementation in
  `TemplateSyntaxFactoryImpl.cs`, and the `TransformAssignmentExpression` override in
  `TemplateCompilerRewriter.cs` that wraps every transformed assignment in a call to it. Commit
  `e9edd7cacc` (#1109) then removed the eight lines of the refusal, with the commit message
  "Remove unconditional blocking of null-conditional assignments (?.=) in templates. The existing scope
  validation (LAMA0259) handles mixed-scope scenarios."
- `field` keyword: commit `aea7b2e5a2` (#1114) replaced the refusal with
  `return node.AddScopeAnnotation( RunTimeOnly );` in `TemplateAnnotator.VisitFieldExpression`, added
  `TemplateCompilerRewriter.VisitFieldExpression` emitting a call to
  `ITemplateSyntaxFactory.GetPropertyBackingField()`, added that member to the interface and its
  implementation to `TemplateSyntaxFactoryImpl`, and added
  `CompiledTemplateAttribute.IntroducesBackingField` / `IsBackingFieldAssigned` so the advice layer
  knows to introduce the field. Follow-ups `929d055d85`, `81e5a5fed7`, `e8e49f26f5`.

**Step 3 — guard the new API with `#if ROSLYN_<version>_OR_GREATER` while an older variant is still shipped, then delete the guard when it is dropped.**
Both new overrides were written inside `#if ROSLYN_5_0_0_OR_GREATER`. Commit `e247425d69`
("Strip always-true `#if ROSLYN_4_(4|8|12)_0_OR_GREATER` guards (#1603)") removed the earlier
generation of those guards, and the 2027.0 baseline removed the 5.0 ones: **`Templating/` contains no
`#if ROSLYN_*` today** (the only conditional compilation left is `#if DEBUG` at
`MetaSyntaxRewriter.cs:308`, `SyntaxAnnotationExtensions.cs:121,315`, `SyntaxTreeAnnotationMap.cs:68`).
`updating-roslyn.md:35-36` codifies this: when a variant is dropped, every constant that all remaining
variants define, or that none defines, must be removed together with its `#if` sites and its
`@RequiredConstant` / `@ForbiddenConstant` test directives; and no new `DefineConstants` may be added
unless the source has to branch on a distinction no existing constant expresses.

**Step 4 — a test folder per feature, with committed baselines.**
`Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/CSharp14/` has one
subfolder per feature: `CompoundAssignmentOperator`, `ExtensionMembers`, `FieldKeyword`,
`NullConditionalAssignment`, `PartialConstructor`, `PartialEvent`, `SimpleLambdaModifier`.
`FieldKeyword/` alone holds 21 test pairs. Naming is `<Feature>_<Scenario>.cs` plus `.t.cs`;
design-time scenarios also carry `.0.i.cs` / `.1.i.cs`. Templating-level refusals live in
`Metalama.Framework.Tests.TemplateTests/Tests/UnsupportedSyntax/` (`GotoNotSupported`,
`LinqNotSupported`, `UnsafeNotSupported`, ...). Variant-specific expectations use
`// @RequiredConstant(ROSLYN_5_10_0_OR_GREATER)` /
`// @ForbiddenConstant(ROSLYN_5_10_0_OR_GREATER)` — the only two such tests today are
`Tests/Aspects/DesignTimeInvalidCode/UnknownAccessorInTemplate.cs` and
`UnknownAccessorInTemplate_Roslyn5_0.cs`.

### 4.2 The grammar side of the same wave

- `b46f9218a8` "Use the actual Roslyn 5.10 grammar for the syntax rewriter (#1881)" replaced a
  hand-edited `Syntax-5.10.0.xml` with the real one from `Metalama.Compiler`, and updated
  `updating-roslyn.md`.
- `e1cbb88a77` "Skip experimental Roslyn nodes in the syntax rewriter generator (#1881)" added
  `ExperimentalUrl` / `IsExperimental` to `Field.cs` and `TreeType.cs` and
  `RemoveExperimentalDeclarations` / `RemoveExperimentalChildren` to `TreeReader.cs`. This is the
  mechanism the C# 15 work will invert.
- `08d065a9f8` "Replace the Roslyn 4.12 variant with a Roslyn 5.0 variant (#1881)" is the model for
  renumbering / dropping a variant.

### 4.3 What C# 14 did NOT do, and should be read as a gap rather than as precedent

C# 14 introduced a genuinely new *type declaration*, the extension block
(`ExtensionBlockDeclarationSyntax`, present in `Syntax-5.0.0.xml`). The templating subsystem gained
**no** `VisitExtensionBlockDeclaration` in `TemplateAnnotator` and **none** in
`TemplatingCodeValidator.Visitor`. The only hand-written handling is in the linker and the design-time
generator:

```
Metalama.Framework.Engine/Linking/LinkerInjectionStep.Rewriter.cs:324
Metalama.Framework.Engine/Linking/LinkerLinkingStep.LinkingRewriter.cs:79
Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs:277,662,672
```

So the C# 15 `union` declaration will not be covered by following the C# 14 precedent; see §5.2.

---

## 5. Extension points to change, per kind of language addition

### 5.0 The classification algorithm, for context

Two annotations drive everything, both defined in `Templating/SyntaxAnnotationExtensions.cs`:

- the **scope** annotation (`TemplatingScope`: `RunTimeOnly`, `CompileTimeOnly`,
  `RunTimeOrCompileTime`, `CompileTimeOnlyReturningBoth`, `CompileTimeOnlyReturningRuntimeOnly`,
  `RunTimeTemplateParameter`, `LateBound`, `Conflict`, `TypeOfRunTimeType`, `DynamicTypeConstruction`);
- the **target scope** annotation, whose only interesting value is `MustFollowParent`.

`TemplateAnnotator` computes them bottom-up:

1. `TemplateAnnotator.VisitCore` → `DefaultVisitImpl` (l. 627-643): visit children through
   `base.VisitCore`, then `AddScopeAnnotationToVisitedNode`.
2. `AddScopeAnnotationToVisitedNode` (l. 648-698): if the context forces compile-time and the node is
   run-time, report LAMA0104 (`ScopeMismatch`); otherwise, if the node already has an annotation or is a
   `StatementSyntax`, keep it (l. 685-690); otherwise

   ```csharp
   var childNodes = visitedNode.ChildNodes().Where( n => n is ExpressionSyntax or InterpolationSyntax );
   var combinedScope = this.GetExpressionScope( childNodes.ToReadOnlyList(), node );
   return visitedNode.AddScopeAnnotation( combinedScope );
   ```

3. `GetExpressionScope` (l. 446-590) combines child execution scopes and value scopes, adds the scope of
   the node's own expression type (`GetExpressionTypeScope`, l. 434-444), and maps the pair through the
   table at l. 559-571. **An empty child list returns `RunTimeOrCompileTime` (l. 448-451).**
4. Symbol scopes come from `GetSymbolScope` (l. 181-297), which delegates to
   `ISymbolClassifier.GetTemplatingScope` for everything that is not a local, a parameter, a template
   type parameter or an aspect member.

`TemplateCompilerRewriter.IsCompileTimeCode` (l. 199-264) then reads those annotations: compile-time
code is copied through, run-time code is transformed into the syntax-building expression by the
generated `Transform*` methods.

### 5.1 A NEW EXPRESSION FORM (`unsafe(expr)`, `UnsafeExpressionSyntax`)

Must change:

1. `Syntax-5.10.0.xml:496` — drop `ExperimentalUrl` (or add the node to a `Syntax-5.11.0.xml`). This
   alone generates `VisitUnsafeExpression` / `TransformUnsafeExpression` in `MetaSyntaxRewriter.g.cs`,
   `MetaSyntaxFactoryImpl.UnsafeExpression`, a `VisitUnsafeExpression` in
   `RoslynVersionSyntaxVerifier.g.cs` pinned to `V5_10_0`, hasher visits, and `PartialUpdate`.
2. `SupportedCSharpVersions.ToLanguageVersion` (`Utilities/SupportedCSharpVersions.cs:52-62`) — map
   `V5_10_0` to a `CSharp15` value so the verifier's comparison is meaningful; today `V5_0_0` and
   `V5_10_0` both map to `CSharp14`, so **the verifier cannot currently distinguish a C# 15 construct
   from a C# 14 one**.
3. `AllLanguageVersions.cs` — add `CSharp15 = (LanguageVersion) 1500`.
4. `SupportedCSharpVersions.Latest` and `.All`.
5. `LanguageVersionProvider.GetLanguageVersionFromDotNetSdk` (l. 54-60) — `>= 11 => CSharp15`, and
   `SupportedCSharpVersions.GetMaxLanguageVersion` (l. 149-159) — `(>= 5, >= 10) => CSharp15`.
6. `TemplateAnnotator` — a `VisitUnsafeExpression` override. Minimum viable:
   `this.ReportUnsupportedLanguageFeature( node.Keyword, "unsafe expression" ); return base.VisitUnsafeExpression( node );`,
   mirroring `VisitUnsafeStatement` at l. 2594-2599.
7. `TemplateCompilerRewriter` — nothing, if the annotator refuses the construct; a `Transform*` override
   only if compile-time evaluation of the form is wanted.
8. Tests under `Tests/Aspects/CSharp15/UnsafeExpression/` and, for the refusal,
   `Metalama.Framework.Tests.TemplateTests/Tests/UnsupportedSyntax/`.

### 5.2 A NEW KIND OF TYPE DECLARATION (`union`, `UnionDeclarationSyntax`)

Must change, beyond items 1-5 of §5.1:

1. `TemplateAnnotator` — add `public override SyntaxNode VisitUnionDeclaration( UnionDeclarationSyntax node ) => this.VisitTypeDeclaration( node, n => base.VisitUnionDeclaration( n ) );`
   next to l. 743-754. Without it, `VisitTypeDeclaration`'s early exit for run-time types
   (l. 765-774: "This is not a build-time type so there's no need to analyze it") never runs.
2. `TemplatingCodeValidator.Visitor` — add `VisitUnionDeclaration` next to l. 299-330, calling
   `this.WithDeclaration( node )` and `this.VerifyTypeDeclaration( node, context )`. Without it,
   `_currentScope` is never established for the union's body and `VisitCore` returns at l. 134-137.
3. Note the type also needs an entry in `Metalama.Premium`'s
   `src/Metalama.Extensions.CodeFixes.Engine/Implementations/ChangeVisibilityCodeAction.cs` (the
   `Rewriter` class enumerates class/record/struct/field/event/property/enum/delegate/constructor/
   method/destructor at l. 72-105 and has no union or extension-block arm).
4. `UnionDeclarationSyntax` carries `SkipConvenienceFactories="true"`
   (`Syntax-5.10.0.xml:1954`), and its `OpenBraceToken` / `CloseBraceToken` are `Optional="true"`
   (l. 1968, 1972), which is unusual and worth checking against `Generator.IsAutoCreatableToken`
   (`Generator.cs:156-162`) and `IsRequiredFactoryField` (l. 186-189) when the minimal factory overload
   is generated.

### 5.3 A NEW MODIFIER (`closed`, no new node)

No generator change at all: modifiers are `SyntaxList<SyntaxToken>` and
`MetaSyntaxRewriter.Transform( SyntaxToken )` handles any kind generically. What must be checked:

1. `MetaSyntaxRewriter.Transform( SyntaxToken )` (l. 269-275): for a keyword token,
   `Token( token.Kind() )` is compared with the source token by `Value`; a new contextual keyword whose
   `Value` round-trips takes the one-argument path, which is correct.
2. `Generator.IsKeyword` (`Generator.cs:301-389`) is the generator's own escaping list for *parameter
   names*; `closed` is not in it, but it only matters if a grammar field were named `Closed`.
3. `RunTimeCodeHasher` / `CompileTimeCodeHasher` hash `node.Modifiers` through
   `this.Visit( node.Modifiers )` (`Generator.cs:684`), so a new modifier changes the hash and
   invalidates the design-time cache correctly.
4. Symbol-level consequences (accessibility, sealedness) belong to the code model, not here.

### 5.4 A NEW COLLECTION-EXPRESSION ELEMENT (`with(...)`, `WithElementSyntax`)

`CollectionElementSyntax` is an `AbstractNode`; today its concrete forms are `ExpressionElementSyntax`
and `SpreadElementSyntax`, both of which have an `ExpressionSyntax` child.

Must change:

1. `Syntax-5.10.0.xml:816` — drop `ExperimentalUrl`.
2. `TemplateAnnotator.VisitCollectionExpression` (l. 3495-3503) already visits every element, but a
   `WithElementSyntax` has an `ArgumentListSyntax` child and no `ExpressionSyntax` child, so
   `AddScopeAnnotationToVisitedNode`'s filter at l. 693 selects nothing and
   `GetExpressionScope` returns `RunTimeOrCompileTime` (l. 448-451). A `VisitWithElement` override, or a
   widening of the l. 693 filter to include `CollectionElementSyntax` and `ArgumentSyntax`, is required.
3. `MetaSyntaxRewriter.Transform<T>( T? node )` (l. 106-139): when the element is compile-time inside a
   transformed collection expression, the `default:` arm at l. 131-133 throws
   `AssertionFailedException( "Unexpected node kind: ..." )`, because a `CollectionElementSyntax` is
   neither `ExpressionSyntax`, `ArgumentSyntax` nor `StatementSyntax`. Adding a
   `CollectionElementSyntax` arm (or a `TransformCollectionElement` virtual) is required.
4. `TemplateCompilerRewriter` — a `TransformWithElement` if the element must survive into run-time code.

### 5.5 A NEW OPTIONAL FIELD ON AN EXISTING STATEMENT (labeled `break` / `continue`)

Must change:

1. `Syntax-5.10.0.xml:1296` and `:1307` — drop `ExperimentalUrl` on
   `BreakStatementSyntax.Name` and `ContinueStatementSyntax.Name`. This makes
   `TransformBreakStatement` / `TransformContinueStatement` version-switched, adds the
   `VisitVersionSpecificField` calls to `RoslynVersionSyntaxVerifier.g.cs`, adds the field to the
   hashers, and adds an `Option<IdentifierNameSyntax?> name` parameter to `PartialUpdate`.
2. `TemplateAnnotator.VisitBreakStatement` / `VisitContinueStatement` (l. 1375-1379). Today they
   annotate the statement with `this._currentScopeContext.CurrentBreakOrContinueScope`, that is, the
   scope of the *innermost* enclosing loop or switch. A labeled `break` targets an *outer* construct, so
   the scope must be taken from the labeled construct instead. `ScopeContext`
   (`TemplateAnnotator.ScopeContext.cs:21,123`) carries a single `CurrentBreakOrContinueScope`, with no
   label map; adding one is the structural change.
3. `TemplateCompilerRewriter.VisitSwitchStatement` (l. 2568-2578) appends a bare `BreakStatement()`
   when the last transformed statement is not a control transfer. `BreakStatement` in the kind list at
   l. 2569 matches a labeled break too, which is correct there, but the synthesised
   `BreakStatement()` is unlabeled and would be wrong if it were ever used to close a labeled section.
4. `Statements/SwitchStatement.cs:277` (`statements.Add( BreakStatement() )`) — same synthesis, in the
   builder API.
5. `Metalama.Framework.Engine/Utilities/Roslyn/FlowAnalyzer.cs:42-89` — a labeled `break` transfers
   control out of the enclosing switch, so `NeverContinues` for a switch section
   (l. 66-84) would over-report "never continues". It currently returns `false` for anything it does not
   recognise, so this specific case is a false *positive*, not a false negative.

---

## 6. Where the subsystem would silently do the wrong thing

Ranked by how quietly the wrong answer arrives.

### 6.1 `TemplateAnnotator.AddScopeAnnotationToVisitedNode` gives every unknown node a scope

`Templating/TemplateAnnotator.cs:685-697`

```csharp
if ( visitedNode.HasScopeAnnotation() || visitedNode is StatementSyntax ) { return visitedNode; }
var childNodes = visitedNode.ChildNodes().Where( n => n is ExpressionSyntax or InterpolationSyntax );
var combinedScope = this.GetExpressionScope( childNodes.ToReadOnlyList(), node );
return visitedNode.AddScopeAnnotation( combinedScope );
```

There is no "I do not know this construct" branch. Any C# 15 node reaching `VisitCore` is annotated
with the combined scope of its `ExpressionSyntax` children, and if it has none, with
`RunTimeOrCompileTime` (`GetExpressionScope` l. 448-451). `TemplateCompilerRewriter.IsCompileTimeCode`
then reads that annotation and copies or transforms the node accordingly. Concretely:

- `unsafe(expr)` inherits the scope of `expr`, so a construct that the statement form refuses with
  LAMA0101 (l. 2594-2599) is accepted without a word in expression form.
- `with(a, b)` in a collection expression is annotated `RunTimeOrCompileTime` no matter what `a` and `b`
  are, because neither is an `ExpressionSyntax` *child* of the `WithElementSyntax`.
- A `union` declaration is annotated `RunTimeOrCompileTime`, so `VisitTypeDeclaration`'s
  "this is not a build-time type so there is no need to analyse it" shortcut (l. 765-774) never fires
  and the union's run-time members are analysed as though they might be compile-time.

### 6.2 The version verifier cannot see a C# 15 construct at all, for two independent reasons

- **The experimental filter.** `TreeReader.RemoveExperimentalDeclarations`
  (`eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs:113-152`) deletes the four C# 15 grammar
  additions from *every* document before `VersionDetector` runs, so no `VisitUnion*`,
  `VisitUnsafeExpression`, `VisitWithElement` or `VisitBreakStatement` override is generated into
  `RoslynVersionSyntaxVerifier.g.cs`. A template that uses one of them raises no LAMA0232 and leaves
  `MaximalUsedVersion` untouched, so `CompiledTemplateAttribute`/`UsedApiVersion` under-reports and
  LAMA0282 (`AspectUsesHigherCSharpVersion`, `TemplateExpansionContext.cs:861-878`) never warns the
  consumer project.
- **The version-to-language map.** Even after the filter is lifted,
  `SupportedCSharpVersions.ToLanguageVersion` (`Utilities/SupportedCSharpVersions.cs:52-62`) maps both
  `V5_0_0` and `V5_10_0` to `AllLanguageVersions.CSharp14`. `VisitVersionSpecificNode`
  (`RoslynVersionSyntaxVerifier.cs:41-52`) compares `version.ToLanguageVersion()` against
  `MaximalAcceptableLanguageVersion`, so a `V5_10_0`-pinned node compares as C# 14 and is accepted in a
  project whose `LangVersion` is 14. **This map must be corrected in the same change that lifts the
  filter, or the whole version-checking mechanism silently passes.**

### 6.3 `PartialUpdate` and the generated `Transform*` drop an un-generated optional field

`Generator.GeneratePartialUpdate` (`Generator.cs:788-799`) emits `node.Update( <every generated field> )`.
Roslyn keeps the old `Update` overload when it adds an optional field, so on a Roslyn 5.10 reference the
generated

```csharp
public static BreakStatementSyntax PartialUpdate(
    this BreakStatementSyntax node,
    Option<SyntaxList<AttributeListSyntax>> attributeLists = default,
    Option<SyntaxToken> breakKeyword = default,
    Option<SyntaxToken> semicolonToken = default )
    => node.Update( ..., ..., ... );
```

(`.generated/5.0.0/.../SyntaxNodePartialUpdateExtensions.g.cs:1085-1093`) still compiles and silently
discards `node.Name`. The same is true of the generated
`TransformBreakStatement` (`.generated/5.0.0/.../MetaSyntaxRewriter.g.cs:3170-3183`), which rebuilds the
statement from three arguments. A labeled `break` in a run-time template body would therefore come out
of the template compiler as an unlabeled `break` — code that compiles and jumps to the wrong place.

### 6.4 `HasAnyYieldVisitor` uses an allow-list of statement kinds

`Templating/TemplateExpansionContext.cs:799-846`. `DefaultVisit` descends only into children whose kind
is one of 24 named statement kinds (l. 807-835). A statement kind not on that list is not descended
into, so a `yield return` nested inside it is invisible. The consequence is that the iterator-detection
that drives `CreateReturnStatement` / `AddYieldBreak` (l. 516, 599, 774) takes the non-iterator path.
C# 15 adds no statement kind, so this is latent rather than immediate, but it is the clearest
allow-list-shaped hazard in the subsystem.

### 6.5 `TemplatingCodeValidator.Visitor` has no default for a new declaration kind

`Templating/TemplatingCodeValidator.Visitor.cs:95-137`. `_currentScope` is set only by the explicit
`Visit<Kind>Declaration` overrides at l. 299-813. For a declaration kind with no override, the visitor
walks the body with whatever `_currentScope` the enclosing context left, and if that is null it returns
at l. 134-137 without checking a single reference. C# 14's `ExtensionBlockDeclarationSyntax` is already
in this position; C# 15's `UnionDeclarationSyntax` would join it. The failure mode is that compile-time
code inside such a declaration is never rejected, and the error surfaces much later, as a
`MissingMethodException` or a broken compile-time assembly.

### 6.6 `SyntaxBuilderImpl` parses without parse options

`Templating/Expressions/SyntaxBuilderImpl.cs:71,80` call
`SyntaxFactoryEx.ParseExpressionSafe` / `ParseStatementSafe`
(`SyntaxGeneration/SyntaxFactoryEx.cs:367-382, 384-...`), which call
`SyntaxFactory.ParseExpression( text )` / `ParseStatement( text )` with **no `CSharpParseOptions`**.
The running Roslyn's default `LanguageVersion` therefore applies, which on the 5.10 variant is C# 15,
not `SupportedCSharpVersions.Latest`. Text handed to `ExpressionBuilder`/`StatementBuilder` that uses a
C# 15 construct parses without a diagnostic, is injected into the target compilation, and fails there
with a compiler error attributed to generated code. The same call shape appears at
`TemplateSyntaxFactoryImpl.cs:79` and `:646` and `Expressions/DurableExpression.cs:66,99`.

### 6.7 `RoslynVersionSyntaxVerifier.VisitVersionSpecificField` and generalising fields

`Templating/RoslynVersionSyntaxVerifier.cs:55-75` carries its own TODO:

> A field can be added in a new version of Roslyn that returns a concrete value for old code, when the
> new field is a generalization of an old field. For example, in Roslyn 4.8, the new field
> `UsingDirectiveSyntax.NamespaceOrType` (a generalization of `UsingDirectiveSyntax.Name`) is always not
> null.

The check is `if ( !nodeOrToken.IsKind( SyntaxKind.None ) )`, so a generalising field is always
"present" and every use of the *old* construct is reported as requiring the *new* language version.
That is a false positive rather than a silent miss, but it is the same mechanism a C# 15 generalisation
would trip.

### 6.8 `FlowAnalyzer.NeverContinues` returns false for unknown statements

`Metalama.Framework.Engine/Utilities/Roslyn/FlowAnalyzer.cs:86-88`. Conservative, so it produces a
redundant `break` rather than a missing one; recorded for completeness.

### 6.9 The generated `Transform*` for a node missing from the older grammar

`Generator.GenerateMetaSyntaxRewriter` (`Generator.cs:401-403`) emits methods only for nodes present in
the document being generated. In the Roslyn 5.0 variant, a node introduced in 5.10 has no
`VisitFoo` override at all, so `MetaSyntaxRewriter` inherits Roslyn's own `VisitFoo`, which rewrites
children and returns the node unchanged instead of turning it into a syntax-building expression. The
node is then spliced verbatim into the compiled template. In practice a Roslyn 5.0 host cannot parse a
5.10 construct, so this is unreachable today, but it becomes reachable the moment a construct is added
to the *latest* grammar while an older variant is still shipped.

---

## 7. Checklist derived from the above, for the C# 15 wave

1. Add `Syntax-<new>.xml` (or lift `ExperimentalUrl` on the five elements of `Syntax-5.10.0.xml`), and
   list the version in `GenerateMetaSyntaxRewriter.cs:17-18`.
2. `AllLanguageVersions.CSharp15`; `SupportedCSharpVersions.Latest`, `.All`, `.ToLanguageVersion`,
   `.ToNuGetVersionString`, `.ToVersion`, `.GetMaxLanguageVersion`.
3. `LanguageVersionProvider.GetLanguageVersionFromDotNetSdk` — the `.NET 11 SDK → C# 15` arm.
4. `TemplateAnnotator`: `VisitUnionDeclaration`, `VisitUnsafeExpression`, `VisitWithElement`, and the
   label-aware `VisitBreakStatement` / `VisitContinueStatement`; plus a `ScopeContext` label map.
5. `TemplatingCodeValidator.Visitor`: `VisitUnionDeclaration` (and, while there,
   `VisitExtensionBlockDeclaration`, missing since C# 14).
6. `MetaSyntaxRewriter.Transform<T>`: a `CollectionElementSyntax` arm.
7. Decide, per feature, refusal (LAMA0101 via `ReportUnsupportedLanguageFeature`) or support; refusal is
   the C# 14 first step and costs about five lines per feature.
8. Tests: `Tests/Aspects/CSharp15/<Feature>/` with committed `.t.cs` baselines, plus
   `TemplateTests/Tests/UnsupportedSyntax/` entries for whatever is refused.
