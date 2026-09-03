# Terrain map: the aspect linker (C# 15 / .NET 11 impact analysis)

Subsystem scope:

- `C:/src/Metalama-2027.0/Metalama/Metalama.Framework/src/Metalama.Framework.Engine/Linking/**` (167 files)
- `C:/src/Metalama-2027.0/Metalama/Metalama.Framework/src/Metalama.Framework.Engine/Transformations/**` (31 files)
- Design documents: `C:/src/Metalama-2027.0/Metalama/Metalama.Framework/docs/linker-overview.md`,
  `linker-architecture.md`, `linker-inlining.md`, `linker-callsite.md`
- Test harness: `C:/src/Metalama-2027.0/Metalama/Metalama.Framework/src/tests/Metalama.Framework.Tests.LinkerTests/**`
  and `.../Metalama.Framework.Tests.UnitTests/Linker/LinkerTriviaPreservationTests.cs`

`C:/src/Metalama-2027.0/Metalama.Premium` contains **no** linker or transformation code
(`grep -rl "Engine.Linking|Engine.Transformations" src` returns nothing). The whole subsystem lives in the
open-source repository, so the C# 15 work is confined to `Metalama.Framework`.

---

## 0. The three steps, and where language shape enters each

From `docs/linker-architecture.md` and `docs/linker-overview.md`:

| Step | Class | Where language shape enters |
|---|---|---|
| 1. Injection | `LinkerInjectionStep` (`Linking/LinkerInjectionStep.cs`) | A `CSharpSyntaxRewriter` (`LinkerInjectionStep.Rewriter`) that overrides one `Visit*Declaration` per concrete type/member syntax kind. |
| 2. Analysis | `LinkerAnalysisStep` (`Linking/LinkerAnalysisStep.cs`) | Control-flow shape of statements (`BodyAnalyzer`), expression shape around aspect references (`AspectReferenceResolver`, `Inliner`s). |
| 3. Linking | `LinkerLinkingStep` (`Linking/LinkerLinkingStep.cs`) | A second `CSharpSyntaxRewriter` (`LinkingRewriter`) plus `LinkerRewritingDriver`, dispatching on `SymbolKind` / `MethodKind` / `SyntaxKind`. |

The rewriters all derive from `Metalama.Framework.Engine.Utilities.Roslyn.SafeSyntaxRewriter`
(`Metalama.Framework/src/Metalama.Framework.Sdk/Utilities/Roslyn/SafeSyntaxRewriter.cs:35`), which
adds a recursion guard and wraps exceptions in `SyntaxProcessingException`. It does **not** add any
"unknown node" detection: an unrecognised node falls through to `CSharpSyntaxRewriter.Visit`, which
recurses generically and returns the node unchanged. That default is the root cause of most of the
silent-failure risks in section 5.

---

## 1. Files and types sensitive to the set of C# language constructs

### 1.1 Type-declaration dispatch (a NEW kind of type declaration lands here)

**`Linking/LinkerInjectionStep.Rewriter.cs`** — one override per concrete type syntax:

```
316: public override SyntaxNode VisitClassDeclaration( ClassDeclarationSyntax node ) => this.VisitTypeDeclaration( node );
318: public override SyntaxNode VisitStructDeclaration( StructDeclarationSyntax node ) => this.VisitTypeDeclaration( node );
320: public override SyntaxNode VisitInterfaceDeclaration( InterfaceDeclarationSyntax node ) => this.VisitTypeDeclaration( node );
322: public override SyntaxNode VisitRecordDeclaration( RecordDeclarationSyntax node ) => this.VisitTypeDeclaration( node );
324: public override SyntaxNode VisitExtensionBlockDeclaration( ExtensionBlockDeclarationSyntax node ) => this.VisitTypeDeclaration( node );
326: public override SyntaxNode VisitEnumDeclaration( EnumDeclarationSyntax node )
348: public override SyntaxNode VisitDelegateDeclaration( DelegateDeclarationSyntax node )
359: private SyntaxNode VisitTypeDeclaration<T>( T node ) where T : TypeDeclarationSyntax
```

`VisitTypeDeclaration<T>` (359-452) is the only place that:
- applies member-level transformations to a primary constructor (`ApplyMemberLevelTransformationsToPrimaryConstructor`, line 366);
- emits the compatibility `Deconstruct` overload for records (lines 377-387);
- calls `AddInjectionsOnPosition` for `InsertPositionRelation.After` each member and `Within` the type (390-404);
- adds braces when the type has none (`node.OpenBraceToken.IsKind( SyntaxKind.None )`, line 407);
- appends injected interfaces to the base list (418-441).

A `UnionDeclarationSyntax` (Roslyn 5.10, `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml:1954`,
`Base="TypeDeclarationSyntax"`, with `ParameterList`, `BaseList` and `Members`) needs
`public override SyntaxNode VisitUnionDeclaration( UnionDeclarationSyntax node ) => this.VisitTypeDeclaration( node );`
added here. Without it none of the above happens for members of a union.

**`Linking/LinkerLinkingStep.LinkingRewriter.cs`** — the same shape at step 3:

```
37: VisitStructDeclaration    -> GetMembersForTypeDeclaration + LinkerRewritingDriver.RewriteStruct
50: VisitClassDeclaration     -> ... RewriteClass
63: VisitInterfaceDeclaration -> node.WithMembers( List( this.GetMembersForTypeDeclaration( node ) ) )
66: VisitRecordDeclaration    -> ... RewriteRecord
79: VisitExtensionBlockDeclaration -> node.WithMembers( List( transformedMembers ) )
88: private IReadOnlyList<MemberDeclarationSyntax> GetMembersForTypeDeclaration( TypeDeclarationSyntax node )
```

**`Linking/LinkerRewritingDriver.Types.cs`** — `RewriteClass` (18), `RewriteStruct` (46),
`RewriteRecord` (64). Each undoes a removed primary constructor
(`this.LateTransformationRegistry.HasRemovedPrimaryConstructor( symbol )`) and rewrites
`PrimaryConstructorBaseTypeSyntax` back to `SimpleBaseTypeSyntax` (lines 34-38, 80-84). There is no
`RewriteExtensionBlock` (extension blocks cannot have a primary constructor) and there would be no
`RewriteUnion`.

**`Linking/LinkerLateTransformationRegistry.cs`** — hard-coded list of the type declaration kinds that
may carry a primary constructor:

```
147:                    d => (d.IsKind( SyntaxKind.ClassDeclaration ) || d.IsKind( SyntaxKind.StructDeclaration ) || d.IsKind( SyntaxKind.RecordDeclaration )
150:                          || d.IsKind( SyntaxKind.RecordStructDeclaration )) && ((TypeDeclarationSyntax) d).ParameterList != null );
```

Repeated verbatim at lines 189-191 in `GetPrimaryConstructorBaseArgumentList`. Both use `.Single(...)`,
so a union with a primary constructor throws `InvalidOperationException` from LINQ, not a diagnostic.
Also `GetPrimaryConstructorProperties` (line 168) and the switch at 77-125 which maps
`SymbolKind.Property` primary declarations of kind `SyntaxKind.PropertyDeclaration` (96) or
`SyntaxKind.Parameter` (108).

**`Metalama.Framework.Engine/Utilities/Roslyn/SyntaxExtensions.cs:113-120`** — `GetDeclaringType`,
consumed by `LexicalScopeFactory`:

```csharp
internal static TypeDeclarationSyntax? GetDeclaringType( this SyntaxNode node )
    => node.Kind() switch
    {
        SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration or SyntaxKind.InterfaceDeclaration
            or SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration or SyntaxKind.EnumDeclaration
            when node is TypeDeclarationSyntax type => type,
        _ => node.Parent?.GetDeclaringType()
    };
```

Neither `ExtensionBlockDeclaration` nor a future `UnionDeclaration` is listed, so the walk continues to
the *enclosing* type. (The `EnumDeclaration` arm is already dead: `EnumDeclarationSyntax` derives from
`BaseTypeDeclarationSyntax`, not `TypeDeclarationSyntax`.)

**`Linking/LinkerInjectionStep.LinkerInjectedMemberComparer.cs:21-30`** — ordering table over
`DeclarationKind`:

```csharp
{ DeclarationKind.Field, 0 }, { DeclarationKind.Constructor, 1 }, { DeclarationKind.Property, 2 },
{ DeclarationKind.Method, 3 }, { DeclarationKind.Event, 4 }, { DeclarationKind.NamedType, 5 },
{ DeclarationKind.ExtensionBlock, 6 }
```

with `GetKindOrder` (line 194) returning `10` for anything unlisted, and a special case at line 73
(`if ( x.Kind != DeclarationKind.ExtensionBlock )`) that skips name comparison for extension blocks.

### 1.2 Member-declaration dispatch (a NEW kind of member declaration lands here)

**`Linking/LinkerInjectionStep.Rewriter.cs:1113-1133`** — `VisitMember`:

```csharp
return member.Kind() switch
{
    SyntaxKind.ConstructorDeclaration ... VisitConstructorDeclarationCore
    SyntaxKind.MethodDeclaration      ... VisitMethodDeclarationCore
    SyntaxKind.PropertyDeclaration    ... VisitPropertyDeclarationCore
    SyntaxKind.IndexerDeclaration     ... VisitIndexerDeclarationCore
    SyntaxKind.OperatorDeclaration    ... VisitOperatorDeclarationCore
    SyntaxKind.EventDeclaration       ... VisitEventDeclarationCore
    SyntaxKind.FieldDeclaration       ... VisitFieldDeclarationCore
    SyntaxKind.EventFieldDeclaration  ... VisitEventFieldDeclarationCore
    _ => Singleton( (MemberDeclarationSyntax) this.Visit( member )! )
};
```

Note the absent cases: `ConversionOperatorDeclaration` and `DestructorDeclaration` never receive
inserted entry/exit statements at injection time (only `Method`, `Constructor`, `Operator` have `*Core`
methods with `InjectStatementsIntoMemberDeclaration`).

Individual `*Core` methods: `VisitFieldDeclarationCore` (1393), `VisitEnumMemberDeclarationCore` (1471),
`VisitConstructorDeclarationCore` (1482), `VisitMethodDeclarationCore` (1553),
`VisitOperatorDeclarationCore` (1582), `VisitParameter` (1606), `VisitTypeParameter` (1629),
`VisitPropertyDeclarationCore` (1652), `VisitIndexerDeclarationCore` (1712),
`VisitAccessorDeclaration` (1760), `VisitEventDeclarationCore` (1783),
`VisitEventFieldDeclarationCore` (1795), `VisitCompilationUnit` (1836),
`VisitNamespaceDeclaration` (1874), `VisitFileScopedNamespaceDeclaration` (1894).

**`Linking/LinkerInjectionStep.Rewriter.cs:578-670`** — the switch inside `AddInjectionsOnPosition`
that post-processes an already-generated `InjectedMember`'s syntax:

```
580: case SyntaxKind.ConstructorDeclaration ...   apply member-level transformations
595: case SyntaxKind.PropertyDeclaration ...      synthesized setter
621: case SyntaxKind.ExtensionBlockDeclaration ... nested injections into an introduced extension block
639: case SyntaxKind.ClassDeclaration or ...RecordStructDeclaration when injectedNode is TypeDeclarationSyntax
657: case SyntaxKind.NamespaceDeclaration ...
```

The switch has **no `default`** — an unknown injected node kind is added verbatim (line 673:
`targetList.Add( (T) injectedNode );`) and never receives nested injections.

**`Linking/LinkerLinkingStep.LinkingRewriter.cs:111-129`** — symbol lookup by member syntax kind, with
the silent fallback:

```csharp
var symbols = member.Kind() switch
{
    SyntaxKind.ConstructorDeclaration ... SyntaxKind.OperatorDeclaration ...
    SyntaxKind.ConversionOperatorDeclaration ... SyntaxKind.DestructorDeclaration ...
    SyntaxKind.MethodDeclaration ...
    SyntaxKind.PropertyDeclaration or SyntaxKind.IndexerDeclaration or SyntaxKind.EventDeclaration ...
    SyntaxKind.FieldDeclaration ... SyntaxKind.EventFieldDeclaration ...
    _ => []
};
```

**`Linking/LinkerRewritingDriver.cs:466-496`** — `RewriteMember`, the step-3 dispatch on
`ISymbol.Kind` x `IMethodSymbol.GetImplementedMethodKind()`:

```
468: return symbol.Kind switch
470:   SymbolKind.Method when ... methodSymbol.GetImplementedMethodKind() switch
471:     MethodKind.Ordinary -> RewriteMethod
472:     MethodKind.Destructor -> RewriteDestructor
473:     MethodKind.Constructor or MethodKind.StaticConstructor -> RewriteConstructor
475:     MethodKind.Conversion -> RewriteConversionOperator
476:     MethodKind.UserDefinedOperator -> RewriteOperator
478:     _ => throw new AssertionFailedException( $"Unsupported method kind: ..." )
479:   SymbolKind.Property when IPropertySymbol { Parameters.Length: 0 } -> RewriteProperty
481:   SymbolKind.Property when IPropertySymbol -> RewriteIndexer
485:   SymbolKind.Field -> RewriteField
487:   SymbolKind.Event -> syntax.Kind() switch { EventDeclaration -> RewriteEvent, EventFieldDeclaration -> RewriteEventField, _ => throw }
495:   _ => throw new AssertionFailedException( $"Unsupported symbol kind: {symbol}." )
```

Per-member rewriting drivers, each a partial of `LinkerRewritingDriver`:
`.Constructors.cs`, `.ConversionOperators.cs`, `.Destructors.cs`, `.EventFields.cs`, `.Events.cs`,
`.Fields.cs`, `.Indexers.cs`, `.Initializers.cs`, `.Methods.cs`, `.Operators.cs`,
`.PositionalProperties.cs`, `.Properties.cs`, `.Types.cs`.

**`Linking/LinkerSyntaxHandler.cs:17-149`** — `GetCanonicalRootNodeOrNull`, two switches over the
primary declaration syntax kind, one for override targets (30-113) and one for overrides (118-145),
both ending in `throw new AssertionFailedException`. Handles `MethodDeclaration`,
`ConstructorDeclaration`, `DestructorDeclaration`, `OperatorDeclaration`,
`ConversionOperatorDeclaration`, the five accessor kinds, `ArrowExpressionClause`,
`VariableDeclarator` (event fields), `Parameter` (record positional property),
`RecordDeclaration`/`RecordStructDeclaration` (record-synthesized members).

**`Linking/SymbolExtensions.cs:23-64`** — `GetDeclarationFlags`, an explicit enumeration of *every*
declaration syntax kind that may carry a linker annotation:

```
25: case SyntaxKind.MethodDeclaration or SyntaxKind.ConstructorDeclaration or SyntaxKind.DestructorDeclaration or SyntaxKind.OperatorDeclaration
26:     or SyntaxKind.ConversionOperatorDeclaration
27:     or SyntaxKind.PropertyDeclaration or SyntaxKind.IndexerDeclaration or SyntaxKind.EventDeclaration or SyntaxKind.FieldDeclaration
28:     or SyntaxKind.EventFieldDeclaration
29:     or SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration or SyntaxKind.InterfaceDeclaration or SyntaxKind.RecordDeclaration
30:     or SyntaxKind.RecordStructDeclaration
31:     or SyntaxKind.EnumDeclaration or SyntaxKind.DelegateDeclaration or SyntaxKind.NamespaceDeclaration or SyntaxKind.FileScopedNamespaceDeclaration:
...
62: default:
63:     throw new AssertionFailedException( $"Unexpected declaration syntax for '{symbol}': {declaration}" );
```

`SyntaxKind.ExtensionBlockDeclaration` is **not** in this list. A new type kind is a required edit here.

**`Linking/LinkerSyntaxHelper.cs:14-24`** — `IsUnsupportedMemberSyntax`, the linker's only
"give up gracefully" gate; it recognises only `UnknownAccessorDeclaration` inside a property or an
indexer, and returns `false` for everything else.

### 1.3 Statement-shape dependence (control-flow analysis and inlining)

**`Linking/LinkerAnalysisStep.SemanticBodyAnalyzer.cs`** — class `BodyAnalyzer`. This is the most
statement-shape-dependent file in the subsystem.

- `Analyze` (121-252) switches over the body node kind: `Block` (133), `ArrowExpressionClause` (212),
  body-less `MethodDeclaration` (218), body-less accessor (224), event-field `VariableDeclarator` (232),
  record positional `Parameter` (238), `RecordDeclaration`/`RecordStructDeclaration` (244),
  `default: throw new AssertionFailedException( $"Unexpected body for '{symbol}'." )` (250).
- The return-statement parent switch (146-195) enumerates `Block`, `IfStatement`, `ElseClause`,
  `SwitchSection`, `LockStatement`, `FixedStatement`, `LabeledStatement`, `UsingStatement`; the
  `default` (191) records `new ReturnStatementProperties( false, false )`, that is, "not an
  unconditional end point" (conservative).
- `DiscoverExitFlowingStatements` (254-391) enumerates `ReturnStatement` (270), `Block` (275),
  `IfStatement` (282), `SwitchStatement` (295), `LockStatement` (309), `FixedStatement` (317),
  `CheckedStatement` (325), `LabeledStatement` (330), `UnsafeStatement` (338), `UsingStatement` (343),
  `TryStatement` (351). **The switch has no `default` arm at all.**
- `ProcessStatementList` (367-390) and `GetLastFlowStatement` (425-441) both special-case
  `SyntaxKind.LocalFunctionStatement` as flow-neutral.
- `GetBlocksWithReturnBeforeUsingLocal` (443-508) detects `using` locals with
  `statement.Kind() == SyntaxKind.LocalDeclarationStatement && ... .UsingKeyword != default` (495-497).
- `GetDeclarationBody` (393-422) is a second copy of the declaration-kind switch, ending with
  `_ => throw new AssertionFailedException( $"Unexpected node: {CSharpExtensions.Kind( declaration )}." )`.

**`Linking/Substitution/ReturnStatementSubstitution.cs:48-170`** — implements transformations T1-T4 from
`docs/linker-overview.md`. Emits an **unlabeled** `break;` when `_replaceByBreakIfOmitted` is set
(lines 86-92, 104-110, 154-159), and a `goto <returnLabel>;` otherwise (`CreateGotoStatement`, 213-223).

**`Linking/LinkerLinkingStep.CountLabelUsesWalker.cs:15-32`** — counts label uses; recognises **only**
`GotoStatementSyntax`:

```csharp
public override void VisitGotoStatement( GotoStatementSyntax node )
{
    if ( node.Expression?.Kind() == SyntaxKind.IdentifierName && node.Expression is IdentifierNameSyntax identifierName )
    { ... counter + 1 ... }
}
```

**`Linking/LinkerLinkingStep.RemoveTrivialLabelRewriter.cs:51-131`** — removes an adjacent
`goto L; L: stmt;` pair when the observed counter for `L` is exactly `1`.

**`Linking/LinkerLinkingStep.CleanupBodyRewriter.cs`** — `VisitBlock` (110), `VisitSwitchSection` (138),
`TransformStatementList` (165 onwards) which flattens `LinkerGeneratedFlags.FlattenableBlock` blocks
(176-197 and `AddFlattenedBlockStatements`, 192 onwards), removes `EmptyLabeledStatement` markers (235)
and `EmptyTriviaStatement` markers (255). Only `Block` and `SwitchSection` hold statement lists here.

**`Linking/LinkerLinkingStep.CleanupRewriter.cs:29-60`** — chooses which declarations get body cleanup:
`VisitMethodDeclaration`, `VisitOperatorDeclaration`, `VisitConversionOperatorDeclaration`,
`VisitConstructorDeclaration`, `VisitDestructorDeclaration`, and the three `BasePropertyDeclarationSyntax`
kinds. Only `node.Body` (a `BlockSyntax`) is cleaned; expression bodies and local functions are not.

**`Linking/ConstructorEpilogueRewriter.cs:27-74`** — rewrites top-level `return;` to
`goto <label>;`; explicitly returns unchanged for `SimpleLambdaExpressionSyntax` (67),
`ParenthesizedLambdaExpressionSyntax` (69), `AnonymousMethodExpressionSyntax` (71) and
`LocalFunctionStatementSyntax` (73). Any *new* nested-function form would have to be added here.

### 1.4 Expression-shape dependence (aspect reference resolution and inliners)

**`Linking/AspectReferenceResolver.cs`**:

- `ResolveExpressionTarget` (828-864) decides whether a reference is a read or a write by enumerating
  every compound-assignment `SyntaxKind` **twice** (once for `SymbolKind.Property`, lines 832-842, once
  for `SymbolKind.Field`, lines 843-852):
  `SimpleAssignmentExpression, AddAssignmentExpression, SubtractAssignmentExpression,
  MultiplyAssignmentExpression, DivideAssignmentExpression, ModuloAssignmentExpression,
  AndAssignmentExpression, OrAssignmentExpression, ExclusiveOrAssignmentExpression,
  LeftShiftAssignmentExpression, RightShiftAssignmentExpression, UnsignedRightShiftAssignmentExpression,
  CoalesceAssignmentExpression`. Events use `AddAssignmentExpression` / `SubtractAssignmentExpression`
  (853-861) and otherwise fall to `EventRaiseAccessor` (862).
- Helper-method recognition (612-816): `expression.Parent?.Parent?.Parent?.Parent.IsKind(
  SyntaxKind.InvocationExpression )` for the async-void wrapper (612), then per-helper switches on
  `expression.Parent?.Kind()` for `__StaticConstructor` (658), `__Constructor` (673), `__Property` (702)
  and `__RaiseEvent` (736), each with `default: throw new AssertionFailedException`.
- The reference-order switch at 401 and the referenced/resolved symbol pairing at 930-954.

**`Linking/LinkerAnalysisStep.AspectReferenceWalker.cs`**:

- `VisitCore` (63-160). Lines 99-104 special-case `SyntaxKind.ConditionalAccessExpression`.
  Lines 108-119 fall back to `_ => null` and the comment at 117 states the consequence:
  *"Otherwise we will skip this reference completely, which will cause it not to be transformed."*
- Line 139 casts the annotated node to `ExpressionSyntax`.

**`Linking/Inlining/**` (36 files)** — an inliner is chosen by
`InlinerProvider.TryGetInliner` (`Inlining/InlinerProvider.cs:48-58`) as the single element of the
static array at lines 17-46 that matches. The array is the extension point for a new statement or
expression form:

```
MethodAssignmentInliner, MethodLocalDeclarationInliner, MethodReturnStatementInliner,
MethodCastReturnStatementInliner, MethodInvocationInliner, MethodDiscardInliner,
AwaitAssignmentInliner, AwaitLocalDeclarationInliner, AwaitReturnStatementInliner,
AwaitCastReturnStatementInliner, AwaitExpressionStatementInliner, AwaitDiscardInliner,
PropertyGetAssignmentInliner, PropertyGetReturnInliner, PropertyGetCastReturnInliner,
PropertyGetLocalDeclarationInliner, PropertySetValueAssignmentInliner,
EventAddAssignmentInliner, EventRemoveAssignmentInliner, ConstructorInliner
```

(`PropertyGetExpressionBodyInliner` and `PropertySetExpressionBodyInliner` are commented out, lines
39 and 42, with the note *"Expression body inliners are disabled because substitution generator does
not handle them well"*.)

Each inliner's `CanInline` walks a fixed ancestor chain, for example
`Inlining/MethodReturnStatementInliner.cs:31-50`:

```csharp
if ( !aspectReference.RootExpression.AssertNotNull().Parent.IsKind( SyntaxKind.InvocationExpression ) ... ) return false;
var possibleReturn = InlinerHelper.SkipParenthesizedExpressionAncestors( invocationExpression ).Parent;
if ( !possibleReturn.IsKind( SyntaxKind.ReturnStatement ) ... ) return false;
```

**`Linking/Inlining/InlinerHelper.cs:99-108`** — `SkipParenthesizedExpressionAncestors`, the list of
"transparent" wrapper expressions:

```csharp
while ( node.Parent is ParenthesizedExpressionSyntax
       or PostfixUnaryExpressionSyntax { RawKind: (int) SyntaxKind.SuppressNullableWarningExpression } )
```

with the downward twin at `Metalama.Framework.Engine/Utilities/Roslyn/SyntaxExtensions.cs:103-111`
(`RemoveParenthesisAndNullForgiving`) and `:92-97` (`RemoveParenthesis`).

**`Linking/LinkerAnalysisStep.InliningAlgorithm.cs:149,174`** — `info.ReplacedRootNode.Kind() is
SyntaxKind.ReturnStatement or SyntaxKind.EqualsValueClause`.

**`Linking/LinkerAnalysisStep.OnInitializedCallSiteFinder.cs:148-165`** — switches on
`node.Parent?.Kind()`: `VariableDeclarator` inside `FieldDeclaration`/`EventFieldDeclaration` (150-153),
`PropertyDeclaration` (160), `default` (163).

**`Linking/LinkerAnalysisStep.SymbolReferenceFinder.cs:196-230`** — `BodyWalker` indexes only
`IdentifierNameSyntax` (by identifier text, `VisitIdentifierName`, 209) and `InvocationExpressionSyntax`
(`VisitInvocationExpression`, 220, skipping `nameof`). Any *new* expression form that can denote a
member without an `IdentifierNameSyntax` is invisible to this index.

### 1.5 Substitutions (the "one node kind, one substitution class" registry)

`Linking/Substitution/**` — 34 classes, all deriving from `SyntaxNodeSubstitution`
(`Substitution/SyntaxNodeSubstitution.cs`), each with a `Substitute( SyntaxNode currentNode, ... )` that
switches on `currentNode.Kind()` and throws `AssertionFailedException` for anything else. Examples:

- `Substitution/PropertyBackingFieldReferenceSubstitution.cs:35-46` — the C# 14 `field` keyword:
  `case SyntaxKind.FieldExpression when currentNode is FieldExpressionSyntax fieldExpression:`
- `Substitution/PropertyImplicitAccessorSubstitution.cs:83-110` — body-less `get`/`set`/`init`.
- `Substitution/EmptyPartialMethodSubstitution.cs:93-98` and
  `Substitution/EmptyPartialAccessorSubstitution.cs:130-135` — the C# 14 partial members.
- `Substitution/ReturnStatementSubstitution.cs:52-170`.
- `Substitution/ExpressionBodySubstitution.cs:56,70`.
- `Substitution/RecordParameterSubstitution.cs`, `Substitution/ForcedInitializationSubstitution.cs:44`.

The registry that maps a body root node kind to a substitution class is
**`Linking/LinkerAnalysisStep.SubstitutionGenerator.cs:861-911`**, `CreateOriginalBodySubstitution`:

```
865: SyntaxKind.ArrowExpressionClause                      -> ExpressionBodySubstitution
872: five accessor kinds, body-less, auto-property         -> PropertyImplicitAccessorSubstitution
884: SyntaxKind.MethodDeclaration, body-less               -> EmptyPartialMethodSubstitution
891: five accessor kinds, body-less                        -> EmptyPartialAccessorSubstitution
900: SyntaxKind.Parameter in a RecordDeclaration           -> RecordParameterSubstitution
903: SyntaxKind.RecordDeclaration or RecordStructDeclaration -> throw CannotUseProceedWithSynthesizedRecordMember (LAMA0651)
906: _ => throw new AssertionFailedException( $"Unexpected syntax: '{root}'." )
```

### 1.6 Async, iterators and state machines

- `Linking/Inlining/MethodInliner.cs:15-20` — `IsValidForTargetSymbol` requires
  `IsAsync: false` **and** `!methodSymbol.IsIteratorMethod()`.
- `Linking/Inlining/AsyncMethodInliner.cs:86-105` — the mirror: `IsAsync: true`,
  `!methodSymbol.IsIteratorMethod()`; `GetAwaitExpression` (99) unwraps parentheses to an
  `AwaitExpressionSyntax`; `GetInvocationFromAwait` (104).
- `Metalama.Framework.Engine/CodeModel/Helpers/IteratorHelper.cs:50-72` — `IsIteratorMethod(IMethodSymbol)`;
  it inspects `DeclaringSyntaxReferences` for `MethodDeclarationSyntax { Body: { } }` (62) or
  `AccessorDeclarationSyntax { Body: { } }` (63) and returns `false` (`_ => false`, 64) for anything else.
  The yield search is `IteratorHelper.FindYieldVisitor`
  (`CodeModel/Helpers/IteratorHelper.FindYieldVisitor.cs:16-44`), which stops at `ExpressionSyntax` and
  `LocalFunctionStatementSyntax` (28-30) and otherwise walks all children generically.
- `Linking/LinkerInjectionStep.AuxiliaryMemberFactory.cs:156-168` — the
  `(returnVariableName != null, asyncInfo, iteratorInfo)` tuple switch that picks
  `useStateMachine` and the emulated `TemplateKind`; lines 170-205 add or remove the `async`
  modifier and rewrite an `async void` return type to `ValueTask`.
- `Transformations/ProceedHelper.cs:26-145` — `CreateProceedExpression`; wraps `meta.Proceed()` in
  `RunTimeAspectHelper.Buffer` / `BufferAsync` for iterators (37-64), `await` for async (67-96),
  and `__LinkerInjectionHelpers__.__AsyncVoidMethod` for `async void` (`WrapAsyncVoid`, 146-179).
- `Linking/LinkerRewritingDriver.Methods.cs:116-125` — adds or removes `SyntaxKind.AsyncKeyword`
  when the linked body's asyncness differs from the symbol's; `:315` strips `async` for the
  "empty" member.
- `Linking/LinkerRecordHelper.cs:189-198` — adds `SyntaxKind.AsyncKeyword` to a synthesized record
  method trampoline.

### 1.7 Partial members (C# 14 shape, repeated in eleven places)

`symbol is { IsPartialDefinition: true, PartialImplementationPart: ... }` guards appear at:

```
LinkerInjectionStep.Rewriter.cs:1495, 1504, 1517, 1562, 1664, 1723
LinkerRewritingDriver.Constructors.cs:130, 136, 393
LinkerRewritingDriver.EventFields.cs:26, 34, 166
LinkerRewritingDriver.Indexers.cs:38, 46, 197
LinkerRewritingDriver.Methods.cs:30, 38, 169
LinkerRewritingDriver.Properties.cs:40, 48, 301
```

plus `Linking/LinkerSymbolHelper.cs:13-36` (`GetCanonicalDefinition`, which normalises
`PartialDefinitionPart` for methods (20), properties (25) and events (30) — a new partial-able member
kind needs a fourth block here).

---

## 2. Files and types sensitive to runtime, SDK, Roslyn or IDE version

### 2.1 C# language version

Exactly two tests in the whole subsystem:

1. **`Linking/LinkerAnalysisStep.cs:553`**

   ```csharp
   if ( context.CompilationContext.LanguageVersion < AllLanguageVersions.CSharp14 )
   {
       // Before C# 14, we must specify the type of all lambda parameters because of `in`.
       handlerTypeSyntax = context.SyntaxGenerator.TypeSyntax( delegateType ).WithRequiredTrailingSpace();
       ...
   }
   ```

   inside `GetEventBrokerInvokerDelegateInitializationExpression` (542-604). The constant lives in
   `Metalama.Framework.Engine/Utilities/AllLanguageVersions.cs:14-18`, which stops at
   `CSharp14 = (LanguageVersion) 1400` and will need `CSharp15 = (LanguageVersion) 1500`.

2. **`Linking/LinkerInjectionHelperProvider.cs:219`**

   ```csharp
   var useNullability = this._useNullability && options.Version is LanguageVersion.CSharp9 or LanguageVersion.CSharp10;
   ```

   This is an equality test against two *specific* versions, not a floor. With `CSharp9`/`CSharp10`
   below the PB-2027.0 floor, the expression is now permanently `false` and `#nullable enable` is never
   emitted in the helper tree. Whether that is still intended must be re-checked when the floor moves
   again.

### 2.2 Roslyn version

- The Linking and Transformations folders contain **no `#if ROSLYN_*` conditional compilation at all**
  (`grep -rn "#if ROSLYN" Linking/ Transformations/` returns nothing). Roslyn-version differences are
  absorbed by *not building* the older variant: `ExtensionBlockDeclarationSyntax`,
  `FieldExpressionSyntax`, `IsPartialDefinition` on properties and events, and `PartialDefinitionPart`
  are all Roslyn 5.0+ API used unconditionally.
- The single remaining historical comment is
  `Linking/LinkerAnalysisStep.AspectReferenceWalker.cs:45-49`:
  `// Cast is required for Roslyn 4.8.0 where GetDeclaredSymbol returns ISymbol? instead of IMethodSymbol?`
  with `#pragma warning disable IDE0004`. This is now dead weight and would fail a
  `-p:ContinuousIntegrationBuild=True` build if the pragma were removed.
- `Metalama.Framework.Engine/CodeModel/LanguageOptions.cs:30,34-35` — `LanguageOptions.Default` is
  `SupportedCSharpVersions.Latest`, and `ToParseOptions()` starts from
  `SupportedCSharpVersions.DefaultParseOptions`. The linker helper syntax tree is parsed through this
  (`LinkerInjectionHelperProvider.GetLinkerHelperSyntaxTree`, line 214, cached per `LanguageOptions` in
  `_linkerHelperSyntaxTreeCache`, line 44), so raising `SupportedCSharpVersions.Latest` to `CSharp15`
  changes how the helper tree parses and invalidates that cache key.
- Test projects are duplicated per Roslyn variant:
  `src/tests/Metalama.Framework.Tests.LinkerTests.5.0.0/Metalama.Framework.Tests.LinkerTests.5.0.0.csproj`
  and a residual, now-empty `src/tests/Metalama.Framework.Tests.LinkerTests.4.12.0/` (contains only
  `obj/`, no `.csproj`) that should be deleted.
- `src/tests/Metalama.Framework.Tests.LinkerTests/Metalama.Framework.Tests.LinkerTests.csproj` imports
  `../../../../eng/RoslynVersions/Latest.props` and names the assembly
  `Metalama.Framework.Tests.LinkerTests.$(ThisRoslynVersionNoPreview)`.

### 2.3 .NET runtime and target framework

- `src/tests/Metalama.Framework.Tests.LinkerTests/Metalama.Framework.Tests.LinkerTests.csproj`:
  `<TargetFrameworks>net48;net10.0</TargetFrameworks>`. Note this is **net48**, not the net472 desktop
  flavour stated for PB-2027.0; worth reconciling.
- `Metalama.Framework.Engine/SyntaxGeneration/SyntaxGenerationContext.cs:44` —
  `SupportsInitAccessors => this.Compilation.GetTypeByMetadataName( typeof(IsExternalInit).FullName! ) != null`.
  This is a *target-framework* probe, not a language-version probe. Consumed by
  `Linking/RewriterExtensions.cs:68` (`WithSynthesizedSetter`) and
  `Linking/LinkerInjectionStep.AuxiliaryMemberFactory.cs:411,524`.
- `Linking/LinkerInjectionStep.cs:69` —
  `var supportsNullability = input.FinalCompilationModel.RoslynCompilation.Options.NullableContextOptions != NullableContextOptions.Disable;`
  (a compilation option, not a runtime version).
- `Transformations/ProceedHelper.cs` depends on `RunTimeAspectHelper.Buffer` / `BufferAsync` and on
  `System.Collections.Generic.IAsyncEnumerable<T>` being present
  (`CodeModel/Helpers/IteratorHelper.cs:99-106` matches those by *name and namespace string*, not by
  `SpecialType`).

### 2.4 Host integrated development environment

- The subsystem has no direct IDE dependency. The only design-time coupling is
  `Linking/LinkerInjectionStep.cs:163-172` and `LinkerLinkingStep.cs:75-78`, which honour
  `PartialCompilation.HasObservabilityFilter` / `IsSyntaxTreeObserved` so that only the syntax trees the
  IDE asked for are linked.
- `Linking/LinkerLinkingStep.CleanupRewriter.cs:68` gates label and trailing-return cleanup on
  `this._projectOptions?.CodeFormattingOptions == CodeFormattingOptions.Formatted`, which is the
  design-time ("show the transformed code") mode. Formatting-only differences between the compile-time
  and design-time output therefore all flow through this one branch.
- `Linking/LinkerInjectionHelperProvider.cs:51` carries the standing TODO
  `// TODO: Usage of nullability should be determined from context (design time).`

---

## 3. How the subsystem absorbed C# 14 (the pattern to repeat)

Every C# 14 change in the linker is a *widening* of an existing enumeration, plus (where a genuinely new
node type appeared) one new `SyntaxNodeSubstitution` class and one new entry in
`CreateOriginalBodySubstitution`. No new abstraction was introduced; no `#if` was added.

### 3.1 Extension members — issues #1034, #1035, #1036, #1127, #1159

Commits (in `develop/2027.0` history):

| Commit | Issue | Linker/Transformations files touched |
|---|---|---|
| `cdf076ee1a` | #1034 (code model) | none in this subsystem |
| `737e0347a9` | #1035 | method overrides and properties |
| `22697b6ba5` | #1036 | `Transformations/BaseTransformation.cs` (+6), `Transformations/ITransformation.cs` (+9) |
| `30e21aea98` | #1127 | `LinkerInjectionRegistry.cs`, `LinkerInjectionStep.InsertStatementTransformationContextImpl.cs`, `LinkerInjectionStep.Rewriter.cs`, `LinkerInjectionStep.TransformationCollection.cs`, `LinkerInjectionStep.cs`, `Transformations/IInsertStatementTransformation.cs` (163 insertions) |
| `f374fce480` | #1159 | `LinkerInjectionStep.Rewriter.cs` (+22), `Transformations/InsertPosition.cs` (+3) |

Concretely, the pattern was:

1. Add one `Visit*` override that delegates to the existing generic handler —
   `LinkerInjectionStep.Rewriter.cs:324` and `LinkerLinkingStep.LinkingRewriter.cs:79`.
2. Add one `case` to the injected-node switch —
   `LinkerInjectionStep.Rewriter.cs:621-637`, which unwraps `ExtensionBlockBuilderData` (a *different*
   builder-data type from `NamedTypeBuilderData`) and recurses via `AddInjectionsOnPosition`.
3. Widen every `DeclarationKind` switch:
   `LinkerInjectionStep.cs:251` (`FlushPendingInsertStatementContext`),
   `LinkerInjectionStep.cs:837-874` (`IndexInsertStatementTransformation`, the extension-block arm that
   groups inserted statements by target method and calls `ForEachMethodInExtensionBlock`, line 1136),
   `LinkerInjectionStep.LinkerInjectedMemberComparer.cs:29` and `:73`,
   `LinkerInjectionStep.TransformationCollection.cs:834,842`,
   `Transformations/InsertPosition.cs:73`.
4. Widen the "how do I write a reference to this member" helpers, because an extension member's
   receiver is a parameter rather than `this`:
   `LinkerAspectReferenceSyntaxProvider.cs:213-214` (`CreateIndexerAccessExpression`),
   `:268-269` and `:289-290` (`CreateMemberAccessExpression`),
   `Transformations/ProceedHelper.cs:234-235` and `:252-253`.
5. Add extension-block-specific guards where a member-shaped assumption breaks —
   `LinkerInjectionRegistry.cs:203` (`// Extension blocks don't have auxiliary overrides in the same way members do.`)
   and `LinkerInjectionStep.Rewriter.cs:636` (`// Extension blocks don't implement interfaces.`).

### 3.2 The `field` keyword — issue #1094 (`70bd44a5e1`, fix-up `48541ada9b`)

Files: `LinkerAnalysisStep.cs` (+138/-29), `LinkerAnalysisStep.SubstitutionGenerator.cs` (+31),
`LinkerAnalysisStep.InlineabilityAnalyzer.cs` (+4/-2),
`LinkerInjectionStep.TransformationCollection.cs`, `LinkerRewritingDriver.Properties.cs`, and two brand
new substitution classes.

The pattern for **a new expression form**:

1. A dedicated walker for the new node —
   `Linking/LinkerAnalysisStep.AutoPropertyBodyWalker.cs:16-27`
   (`public override void VisitFieldExpression( FieldExpressionSyntax node )`).
2. A collection pass in the analysis step —
   `LinkerAnalysisStep.cs:1065-1140` `GetPropertyBackingFieldReferencesAsync`, driven by the set
   computed at `LinkerAnalysisStep.cs:198-201`:
   `.Where( s => s is IPropertySymbol propertySymbol && propertySymbol.IsAutoProperty() == true && propertySymbol.HasBody() == true )`
   ("hybrid" auto properties).
3. A new `SyntaxNodeSubstitution` —
   `Substitution/PropertyBackingFieldReferenceSubstitution.cs` (rewrites `field` to the generated
   backing-field name) and `Substitution/PropertyImplicitAccessorSubstitution.cs` (synthesizes the
   body of the accessor the compiler would have generated).
4. Threading the new reference list through `LinkerAnalysisStep` into `SubstitutionGenerator`
   (`LinkerAnalysisStep.cs:218-222`, `:248` in the generator's constructor argument list).
5. Widening `IsInlineableProperty` in
   `LinkerAnalysisStep.InlineabilityAnalyzer.cs:387-391`:
   `if ( semantic.Symbol.IsAutoProperty() == true && semantic.Symbol.HasBody() != true && ... ) return false;`

### 3.3 User-defined compound assignment operators — issue #1116 (`5b121f3c21`, `6d8678e5d3`)

Files: `AspectReferenceResolver.cs` (-39/+39), `Inlining/InlinerProvider.cs`, five `Method*Inliner.cs`
files, `Inlining/MethodInliner.cs` (+24), a new `Inlining/StaticReceiverMethodInvocationInliner.cs`
(+92, since removed and folded into `InlinerHelper.IsCanonicalInvocationWithStaticReceiver`,
`Inlining/InlinerHelper.cs:42-91`), `LinkerAspectReferenceSyntaxProvider.cs` (+77),
`LinkerInjectionHelperProvider.cs` (+76), `LinkerRewritingDriver.SubstitutingRewriter.cs`,
`Substitution/AspectReferenceRenamingSubstitution.cs`.

The pattern for **a new operator category**:

1. Extend `OperatorData` / `OperatorCategory` (outside the linker) with
   `BinaryAssignment` and `UnaryAssignment`.
2. Generate the matching helper members in the linker helper tree —
   `LinkerInjectionHelperProvider.cs:230-234` and `:239-243`:
   `public static void {op.MemberName}<A,B>(A a, B b) {{}}`.
3. Emit the aspect reference through the helper —
   `LinkerAspectReferenceSyntaxProvider.GetOperatorReference` (`:163-191`), which adds
   `Argument( ThisExpression() )` for non-static operators (line 181).
4. Teach the inliner to recognise the extra receiver argument —
   `InlinerHelper.IsCanonicalInvocationWithStaticReceiver` (`:42-91`).
5. Widen `ResolveExpressionTarget` (`AspectReferenceResolver.cs:832-852`) with the new assignment
   `SyntaxKind`s.

### 3.4 Partial constructors, events, properties — issues #1110, #1111, #1112, #1113, #1114, #1143

Commits `787ec4fcd8` (#1110-#1113), `aa5e62dbb0` (#1114), `0bc242649a`/`c36340bbf9`/`6c41855702` (#1143).

`787ec4fcd8` touched, in this subsystem: `Inlining/ImplicitLastOverrideReferenceInliner.cs` (+2),
`LinkerAnalysisRegistry.cs` (+4), `LinkerAnalysisStep.cs` (+5), `LinkerInjectionStep.Rewriter.cs` (+7),
`LinkerRewritingDriver.Constructors.cs` (+30), `LinkerRewritingDriver.EventFields.cs` (+29),
`LinkerRewritingDriver.cs` (+3), `LinkerSymbolHelper.cs` (+7), `LinkerSyntaxHandler.cs` (+3).

The pattern for **an existing declaration acquiring a new form**:

1. Add the `IsPartialDefinition` / `PartialImplementationPart` guard to every `RewriteX` entry point
   (see the eleven sites listed in 1.7).
2. Normalise the symbol in one place — `LinkerSymbolHelper.GetCanonicalDefinition`.
3. Accept the body-less form in `LinkerSyntaxHandler.GetCanonicalRootNodeOrNull`
   (`:68`: `return accessorDecl.Body ?? (SyntaxNode?) accessorDecl.ExpressionBody ?? accessorDecl;`
   with the comment *"Accessors with no body are auto-properties or partial properties, in which case we
   have a substitution for the whole accessor declaration."*).
4. Add the substitutions that fabricate the missing body —
   `Substitution/EmptyPartialMemberSubstitution.cs`, `EmptyPartialMethodSubstitution.cs`,
   `EmptyPartialAccessorSubstitution.cs` — and register them in `CreateOriginalBodySubstitution`
   (`SubstitutionGenerator.cs:884-898`).
5. Redirect member-level-transformation lookups from the definition part to the implementation part —
   `LinkerInjectionStep.Rewriter.cs:1491-1497`
   (`// For partial constructor definitions, transformations are stored under the implementation part's syntax`),
   which is the #1143 fix.

### 3.5 Summary of the C# 14 pattern

- Widen the enumerations, do not abstract them.
- One new syntax node means one new walker, one new `SyntaxNodeSubstitution`, and one new arm in
  `LinkerAnalysisStep.SubstitutionGenerator.CreateOriginalBodySubstitution`.
- One new *type* declaration means one `Visit*` override in each of the two rewriters, one arm in the
  injected-node switch, and a widening of every `DeclarationKind` table.
- One new form of an existing declaration means a guard repeated at every `RewriteX` entry point plus a
  normalisation helper.
- No conditional compilation; the older Roslyn variant is simply dropped from the build.
- The design documents were not updated by any of the C# 14 commits.

---

## 4. Extension points per kind of C# 15 change

### 4.1 A NEW kind of type declaration (`union`)

Required edits, in order of the pipeline:

1. `Linking/LinkerInjectionStep.Rewriter.cs` — add `VisitUnionDeclaration` next to line 324.
2. `Linking/LinkerLinkingStep.LinkingRewriter.cs` — add `VisitUnionDeclaration` next to line 79.
3. `Linking/LinkerRewritingDriver.Types.cs` — add `RewriteUnion` if unions can carry a primary
   constructor (the grammar says they can: `Syntax-5.10.0.xml:1965`).
4. `Linking/LinkerInjectionStep.Rewriter.cs:639` — add `SyntaxKind.UnionDeclaration` to the
   `ClassDeclaration or StructDeclaration or ...` arm of the injected-node switch.
5. `Linking/SymbolExtensions.cs:29-31` — add `SyntaxKind.UnionDeclaration` to `GetDeclarationFlags`.
6. `Linking/LinkerLateTransformationRegistry.cs:147-150` and `:189-191` — add `UnionDeclaration` to the
   primary-constructor predicates (otherwise `.Single()` throws).
7. `Metalama.Framework.Engine/Utilities/Roslyn/SyntaxExtensions.cs:116-118` — add `UnionDeclaration`
   to `GetDeclaringType`, otherwise `LexicalScopeFactory` computes the wrong scope.
8. `Linking/LinkerInjectionStep.LinkerInjectedMemberComparer.cs:21-30` — add a `DeclarationKind` entry
   (assuming the code model adds one).
9. `Linking/LinkerAnalysisStep.SemanticBodyAnalyzer.cs:244` and `:418`, and
   `Linking/LinkerSyntaxHandler.cs:104-109`, if unions get compiler-synthesized members the way records
   do.
10. `Linking/LinkerRecordHelper.cs` — a `LinkerUnionHelper` equivalent if unions have synthesized
    `Equals`/`GetHashCode`/`Deconstruct`.
11. Test harness: `src/tests/Metalama.Framework.Tests.LinkerTests/Runner/LinkerTestInputBuilder.TestTypeRewriter.cs:49,65,81`
    and `LinkerTestInputBuilder.TestRewriter.cs:112,126,140`.

### 4.2 A NEW modifier (`closed`)

The linker never parses modifiers by name; it copies `node.Modifiers` wholesale
(`LinkerRewritingDriver.Methods.cs:116`, `.Operators.cs:104`, `.ConversionOperators.cs:102`,
`.Destructors.cs:104`, `.EventFields.cs:155,231`, `.Constructors.cs:353`). The only modifier-aware code
is:

- `Metalama.Framework.Engine/CodeModel/Helpers/ModifierCategories.cs:10-24` — the flag enum
  (`Accessibility, Inheritance, Async, Static, ReadOnly, Unsafe, Volatile, Required, Const, Partial, Extern`).
- `Metalama.Framework.Engine/CodeModel/Helpers/ModifierHelper.cs:22-56` — `GetSyntaxModifierList`,
  used from `LinkerInjectionStep.AuxiliaryMemberFactory.cs:172`
  (`method.GetSyntaxModifierList( ModifierCategories.Static | ModifierCategories.Async | ModifierCategories.Unsafe )`).
- `Metalama.Framework.Engine/Utilities/Roslyn/SyntaxExtensions.cs:86` —
  `IsAccessModifierKeyword( token ) => SyntaxFacts.IsAccessibilityModifier( token.Kind() )`, used at
  `Linking/RewriterExtensions.cs:78` and `Linking/LinkerRewritingDriver.Properties.cs:519`
  (which strips access modifiers from an accessor).
- Explicit modifier filters:
  `LinkerInjectionStep.Rewriter.cs:766` (`ExternKeyword` assertion),
  `:792` (`AbstractKeyword`/`ExternKeyword` assertion),
  `:1212` (`ParamsKeyword`, to insert new parameters before a `params` parameter),
  `LinkerInjectionStep.AuxiliaryMemberFactory.cs:100`.

A `closed` modifier on a type therefore needs a `ModifierCategories` flag plus a
`GetTypeSyntaxModifierList` arm in `ModifierHelper`, and nothing inside the linker folder, **provided**
the linker keeps copying `Modifiers` verbatim. The risk runs in the opposite direction: the linker emits
*new* members into a `closed` type (overrides, backing fields, auxiliary contract members), and a
`closed` type may reject them at compile time. That is a behaviour question, not an extension point.

### 4.3 A NEW expression form (`unsafe(expr)`)

1. `Linking/Inlining/InlinerHelper.cs:99-108` — `SkipParenthesizedExpressionAncestors` decides which
   wrappers an inliner may look through. `UnsafeExpressionSyntax` is a transparent wrapper and belongs
   here if it may enclose an aspect reference.
2. `Metalama.Framework.Engine/Utilities/Roslyn/SyntaxExtensions.cs:92-97` (`RemoveParenthesis`, used by
   `AsyncMethodInliner.GetAwaitExpression`) and `:103-111` (`RemoveParenthesisAndNullForgiving`) — the
   downward twins.
3. `Linking/AspectReferenceResolver.cs:828-864` — `ResolveExpressionTarget` reads `expression.Parent`;
   an intervening `unsafe(...)` node changes the parent and silently reclassifies a write as a read
   (see 5.1).
4. `Linking/AspectReferenceResolver.cs:612` — the fixed four-level
   `expression.Parent?.Parent?.Parent?.Parent` chain used to find the async-void wrapper.
5. If the new expression form can *denote a member* (the way `field` does), the full #1094 recipe from
   3.2 applies: a walker, a collection pass in `LinkerAnalysisStep`, a `SyntaxNodeSubstitution`, and an
   arm in `SubstitutionGenerator.CreateOriginalBodySubstitution` (`:861-911`).

### 4.4 A NEW collection-expression element (`with(...)`)

`WithElementSyntax : CollectionElementSyntax` (`Syntax-5.10.0.xml:816`) is *not* referenced anywhere in
the linker today, and the linker does not enumerate `CollectionElementSyntax` at all
(`grep -rn "CollectionElement|ExpressionElement|SpreadElement" Linking/ Transformations/` returns
nothing). Two contact points:

1. `Linking/LinkerAnalysisStep.SymbolReferenceFinder.cs:209` — a `with(...)` argument list contains
   `IdentifierNameSyntax` nodes, which the `BodyWalker` index will pick up; nothing to do.
2. `Linking/LinkerAnalysisStep.OnInitializedCallSiteFinder.cs` and
   `Substitution/OnInitializedWithExpressionSubstitution.cs` handle the *`with` expression*
   (`WithExpressionSyntax`), a different node. Do not confuse the two; a collection
   `with(...)` element is a constructor-argument site and may need to be treated as an object-creation
   call site by `LinkerAnalysisStep.ObjectCreationCallSiteReference` / `CreateInitializerSubstitution`
   (`SubstitutionGenerator.cs:918-930`) if `OnInitialized` advice should fire for it.

### 4.5 A NEW optional field on an existing statement (labeled `break` / `continue`)

`BreakStatementSyntax.Name` and `ContinueStatementSyntax.Name`
(`Syntax-5.10.0.xml:1296` and `:1307`). Contact points:

1. `Linking/LinkerLinkingStep.CountLabelUsesWalker.cs:24-31` — must also count
   `VisitBreakStatement`/`VisitContinueStatement` where `node.Name != null`. Without this the counter is
   too low and `RemoveTrivialLabelRewriter` may delete a label that a labeled `break` still targets.
2. `Linking/LinkerAnalysisStep.SemanticBodyAnalyzer.cs:254-391` —
   `DiscoverExitFlowingStatements` treats `LabeledStatement` as a control statement (line 330) but knows
   nothing about `break L;`; a labeled break jumps *out* of an enclosing loop, so a `return` that is
   currently classified as exit-flowing may no longer be.
3. `Linking/Substitution/ReturnStatementSubstitution.cs:86,104,154` — the generated
   `BreakStatement( Token( SyntaxKind.BreakKeyword ), Token( ..., SyntaxKind.SemicolonToken, ... ) )`
   uses the two-argument overload, which yields `Name == null`. This is still correct (the linker's
   `break` must bind to the innermost switch section), but the call must be re-checked if the factory
   overload set changes.
4. `Linking/LinkerLinkingStep.CleanupBodyRewriter.cs` block flattening
   (`AddFlattenedBlockStatements`) hoists statements out of a generated block; a labeled `break` inside
   the hoisted statements keeps its meaning only because label names are unique per method.

---

## 5. Places that would silently do the wrong thing

Ordered by severity. "Silent" means no exception, no diagnostic, and output that either compiles or is
simply not transformed.

### 5.1 `AspectReferenceResolver.ResolveExpressionTarget` classifies an unknown assignment as a read

`Linking/AspectReferenceResolver.cs:832-852`. The property and field arms list thirteen assignment
`SyntaxKind`s explicitly; the fall-through is
`(SymbolKind.Property, _) => AspectReferenceTargetKind.PropertyGetAccessor` (line 842) and
`(SymbolKind.Field, _) => AspectReferenceTargetKind.PropertyGetAccessor` (line 852). If C# 15 or a later
wave introduces a new assignment operator kind, an aspect's write to an overridden property resolves to
the **getter** semantic. The linker then links a read where the aspect wrote, and the generated code
compiles. This is the single highest-risk silent failure in the subsystem.

### 5.2 A new type declaration is never linked

`Linking/LinkerLinkingStep.LinkingRewriter.cs` has no `VisitUnionDeclaration`, so
`GetMembersForTypeDeclaration` is never called for a union and `LinkerRewritingDriver.RewriteMember` is
never invoked for its members. `SafeSyntaxRewriter` recurses generically and returns the members
unchanged. Symmetrically, `LinkerInjectionStep.Rewriter.VisitMember:1132`
(`_ => Singleton( this.Visit( member )! )`) means a nested union receives no injections and no
`AddInjectionsOnPosition` call. An aspect applied to a member of a union would therefore produce a
compilation where the override methods exist (`__Foo__Override__Aspect1`) but the original member still
contains the original body: the aspect silently does nothing.

### 5.3 `CountLabelUsesWalker` under-counts label references

`Linking/LinkerLinkingStep.CountLabelUsesWalker.cs:24-31` counts only `goto`. With labeled `break`, a
label referenced by `goto L;` once *and* by `break L;` elsewhere has `counter == 1`, so
`RemoveTrivialLabelRewriter` (`:81-118`) deletes both the `goto` and the `L:` label. The remaining
`break L;` no longer resolves. This produces a broken *generated* compilation rather than a linker
exception, and it only fires in design-time / `CodeFormattingOptions.Formatted` mode
(`CleanupRewriter.cs:68`), so it would not reproduce in a plain build.

### 5.4 `LexicalScopeFactory` computes a scope for the wrong type

`Metalama.Framework.Engine/Utilities/Roslyn/SyntaxExtensions.cs:113-120` (`GetDeclaringType`) does not
recognise `ExtensionBlockDeclaration`, and would not recognise `UnionDeclaration`. It walks to the
*parent*. `LexicalScopeFactory.CreateLexicalScope` (`Linking/LexicalScopeFactory.cs:190-197`) then seeds
the identifier set from the wrong type declaration and calls `GetIdentifiersInTypeScope` on it. Names
generated by `TemplateLexicalScope.GetUniqueIdentifier` may then collide with names declared in the
inner type. For a *top-level* union the same path throws (`typeDeclarationSyntax.AssertNotNull()`,
line 197), which at least fails loudly.

### 5.5 `LexicalScopeFactory.Visitor` misses a new binding form

`Linking/LexicalScopeFactory.Visitor.cs:31-108` enumerates every construct that introduces a name:
`LocalFunctionStatement` (31), `VariableDeclarator` (38), `Parameter` (45), `TypeParameter` (47),
`SingleVariableDesignation` (49), `FromClause` (51), `QueryContinuation` (58), `LetClause` (65),
`JoinClause` (72), `JoinIntoClause` (79), `LabeledStatement` (86), `ForEachStatement` (93),
`CatchDeclaration` (100). A new binding form is simply absent from the set, so
`GetUniqueIdentifier` can hand out a name that is already in scope. The result compiles if the
shadowing is legal, and silently changes which variable the template body reads.

### 5.6 `DiscoverExitFlowingStatements` has no default arm

`Linking/LinkerAnalysisStep.SemanticBodyAnalyzer.cs:268-365`. An unknown statement wrapper is not
recorded as exit-flowing, so a `return` inside it is classified `ReturnStatementProperties( false, false )`
(line 192 or 205) and gets the T1/T3 treatment (return variable plus `goto` plus label) instead of the
simpler T2/T4. That direction is conservative and correct, but the failure is invisible and shows up
only as worse generated code. The opposite direction (a construct that stops being exit-flowing, such as
an enclosing loop that a labeled `break` can leave) would be incorrect and equally invisible.

### 5.7 `AspectReferenceWalker` drops unresolvable references

`Linking/LinkerAnalysisStep.AspectReferenceWalker.cs:108-126`. The comment at line 117 says it:
*"Otherwise we will skip this reference completely, which will cause it not to be transformed."* If a
new expression form makes `GetSymbolInfo` return zero or several candidates, the aspect reference is
silently left in the output. Combined with the fast path at lines 75-94
(`DocumentationCommentId.GetFirstSymbolForDeclarationId`), which is deliberately bypassed for interface
members and helper methods, this is a two-branch resolution with a silent fall-through.

### 5.8 The injected-node post-processing switch has no default

`Linking/LinkerInjectionStep.Rewriter.cs:578-670`. An injected member whose syntax kind is not one of the
five listed is added to the target list verbatim (line 673) and receives no nested injections, no
member-level transformations and no injected interfaces.

### 5.9 `LinkerInjectedMemberComparer.GetKindOrder` buckets unknown kinds together

`Linking/LinkerInjectionStep.LinkerInjectedMemberComparer.cs:194` returns `10` for any
`DeclarationKind` not in the table. Two injected members of two different new kinds compare equal on
kind and fall through to name, signature and accessibility comparison. Output member *order* changes,
which breaks the aspect-test baselines rather than the code: noisy, not dangerous, but easy to
misdiagnose.

### 5.10 `IteratorHelper.IsIteratorMethod` returns `false` for an unrecognised declaration

`Metalama.Framework.Engine/CodeModel/Helpers/IteratorHelper.cs:59-65`, `_ => false`. A method whose
declaring syntax is a form the switch does not list is treated as a non-iterator. `MethodInliner` and
`AsyncMethodInliner` then consider it inlineable (`Inlining/MethodInliner.cs:20`,
`Inlining/AsyncMethodInliner.cs:91`), and the linker inlines a `yield`-bearing body into a caller that
is not a state machine. The result is a compile error in the generated code, not a linker exception,
and it points at the generated file rather than at the cause.

### 5.11 `LinkerSyntaxHelper.IsUnsupportedMemberSyntax` is a two-case whitelist

`Linking/LinkerSyntaxHelper.cs:16-23` returns `false` for everything except an
`UnknownAccessorDeclaration` in a property or an indexer.
`LinkerRewritingDriver.RewriteMember:449-452` uses it as the "leave this member alone" gate. Any *other*
malformed or unrecognised member therefore proceeds into the `symbol.Kind switch` at line 468 and throws
an `AssertionFailedException`: a crash rather than a graceful skip.

### 5.12 `SymbolReferenceFinder.BodyWalker` indexes only identifiers and invocations

`Linking/LinkerAnalysisStep.SymbolReferenceFinder.cs:209,220`. The index backs three analyses
(caller-attribute fix-ups, get-only auto-property redirection, event-field raise redirection: see the
comment block at lines 23-30). A member reference expressed through a new syntax form that is not an
`IdentifierNameSyntax` is invisible to all three, and the corresponding fix-up silently does not happen.
This is exactly why `field` needed its own `AutoPropertyBodyWalker`.

### 5.13 The linker reports almost nothing to the user

`Linking/AspectLinkerDiagnosticDescriptors.cs` defines only three diagnostics in the reserved range
650-699: `LAMA0650 CannotInvokeAnotherInstanceBaseRequired`,
`LAMA0651 CannotUseProceedWithSynthesizedRecordMember`, `LAMA0699 DeclarationMustBeInlined`. Every other
unexpected shape is an `AssertionFailedException` or a `NotSupportedException`. There is no diagnostic
that says "the linker met a construct it does not understand", so any new language construct either
crashes with an internal-error message or is silently ignored. `LAMA0699` is raised only from
`LinkerAnalysisStep.VerifyUnsupportedInlineability` (`LinkerAnalysisStep.cs:838-909`), and only for
indexers and constructors.

---

## 6. Design documents

- `docs/linker-overview.md` — the three steps, aspect-reference orders (Base/Previous/Current/Final),
  `IntermediateSymbolSemanticKind`, and the T1-T4 return-statement transformations plus Algorithms 1
  and 2. The section "Special / Primary constructors" describes the deconstruction of a primary
  constructor.
- `docs/linker-architecture.md` — the transformation taxonomy table (`IIntroduceDeclarationTransformation`,
  `IReplaceMemberTransformation`, `IOverrideDeclarationTransformation`, `IInjectMemberTransformation`,
  `IMemberLevelTransformation`, `IInsertStatementTransformation`), the seven-step indexing order in
  `LinkerInjectionStep.ExecuteAsync`, the two-dictionary pattern in `TransformationCollection`, the
  source-versus-introduced constructor invariant, and `MemberLevelTransformations.Sort()` deduplication.
  The "Partial constructor handling (Roslyn 5.0+)" note and the "Primary Constructor Handling" section
  are the C# 14 additions.
- `docs/linker-inlining.md` (1036 lines) — the detailed inlining mechanics.
- `docs/linker-callsite.md` — `OnInitialized` call-site advice and cross-project propagation.

None of these were updated by the C# 14 commits; all four describe the pipeline in terms of the
declaration kinds that existed before extension blocks, `field` and partial members. Any C# 15 work
should budget for updating `linker-architecture.md` at minimum.
