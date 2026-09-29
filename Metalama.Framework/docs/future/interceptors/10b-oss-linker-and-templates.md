# Open-source extension points: linker and template engine

> Part of the [call-site interceptors design](README.md). Previous: [10a-oss-bridge-hook-factory.md](10a-oss-bridge-hook-factory.md) | Next: [10c-oss-reference-graph-design-time.md](10c-oss-reference-graph-design-time.md). Evidence prefixes and terms: [00-conventions.md](00-conventions.md).

### 10.5 Linker changes

Line numbers in this section come from the 2026.1 engine (ENG26). Section [0.2](00-conventions.md#02-evidence-prefixes) lists the offsets of 2027.0.

#### 10.5.1 Internal model and threading

```csharp
/// <summary>Holds the services required by <see cref="ExtensionTransformationFactory"/>.</summary>
internal sealed record ExtensionTransformationFactoryContext(
    ProjectServiceProvider ServiceProvider,
    CompilationModel FinalCompilation,
    IReadOnlyList<OrderedAspectLayer> AspectLayers,
    LinkerNamingServices Naming );

/// <summary>Holds the linker input produced by extensions.</summary>
internal sealed class ExtensionLinkerInput
{
    public static ExtensionLinkerInput Empty { get; }

    public ImmutableArray<ITransformation> Transformations { get; }

    /// <summary>
    /// Gets the requested call-site redirections, keyed by the syntax tree of the stage's partial compilation and then by
    /// the source node, with reference equality on nodes.
    /// </summary>
    public IReadOnlyDictionary<SyntaxTree, IReadOnlyDictionary<SyntaxNode, CallSiteRedirection>> CallSiteRedirections { get; }

    public LinkerNamingServices? NamingServices { get; }

    /// <summary>Gets the shared state that records template-expansion failures of synthesized methods.</summary>
    public ExtensionExpansionState ExpansionState { get; }
}

/// <summary>Injects a synthesized method into a type. The body is expanded from a template with a custom proceed binding.</summary>
internal sealed class SynthesizedMethodTransformation : IntroduceDeclarationTransformation<MethodBuilderData>
{
    public SynthesizedMethodTransformation(
        AspectLayerInstance aspectLayerInstance,
        MethodBuilderData method,
        BoundTemplateMethod template,
        TemplateProvider templateProvider,
        ExtensionProceedBinding proceed,
        ProceedMultiplicity proceedMultiplicity,
        ImmutableArray<IMetaExtension> metaExtensions,
        bool? nullableAnnotationsEnabled,
        ExtensionExpansionState expansionState );

    public override TransformationObservability Observability => TransformationObservability.None;

    public override IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context );
}

/// <summary>Represents a transformation that appends a local function to the root block of a source member body.</summary>
internal interface IInjectLocalFunctionTransformation : ISyntaxTreeTransformation
{
    /// <summary>Gets the source declaration whose body receives the function.</summary>
    SyntaxNode HostDeclaration { get; }

    IFullRef<IMethodBase> HostMember { get; }

    /// <summary>Returns the local function, or <c>null</c> when the template expansion failed.</summary>
    LocalFunctionStatementSyntax? GetLocalFunction( MemberInjectionContext context );
}

internal sealed class SynthesizedLocalFunctionTransformation : BaseSyntaxTreeTransformation, IInjectLocalFunctionTransformation
{
    public override IFullRef<IDeclaration> TargetDeclaration => this.HostMember;

    public override TransformationObservability Observability => TransformationObservability.None;
}

internal enum CallSiteRedirectionKind { Invocation, Await, MethodReference, FunctionPointerReference, Accessor }

internal enum CallSiteCalleeForm { StaticMember, InstanceMemberOnThis, InstanceMemberOfReceiver, LocalFunction, Extension, Wrapper }

/// <summary>
/// Describes a requested rewrite of a source call site. All syntax is computed when the request is created, so that the
/// injection rewriter emits the final call from syntax only.
/// </summary>
internal sealed class CallSiteRedirection
{
    public int Id { get; }                                           // Sequential; orders the completeness diagnostics.
    public ExpressionSyntax SourceNode { get; }
    public CallSiteRedirectionKind Kind { get; }
    public CallSiteReceiverMode ReceiverMode { get; }
    public CallSiteCalleeForm CalleeForm { get; }
    public ExpressionSyntax Callee { get; }                          // global::N.C.I<int>, this.I, I, or the bare name for extensions.
    public SimpleNameSyntax CalleeName { get; }
    public ImmutableArray<ArgumentSyntax> ExtraArguments { get; }
    public ImmutableDictionary<string, string> ArgumentNameMap { get; }  // Renamed named arguments.
    public TypeSyntax? ResultCast { get; }
    public bool AppendConfigureAwaitFalse { get; }
    public WrapperSyntax? Wrapper { get; }                           // Method references with a FirstArgument mode only: the
                                                                     // delegate type, the receiver type, the parameter list of
                                                                     // the inner lambda, and the reference kind of the receiver.
    public AccessorRewrite? Accessor { get; }                        // Accessor sites only: the callee syntax of the get use and of
                                                                     // the set, add or remove use (null for a use that stays a plain
                                                                     // access), the shape and operator of the site, the context,
                                                                     // the reserved names of the temporaries, and the statement to
                                                                     // replace in a statement context.
    public SyntaxAnnotation GeneratedCodeAnnotation { get; }
    public ITransformation? TargetTransformation { get; }            // Null when the target is an existing method.
    public string Description { get; }
}

/// <summary>Builds the final call syntax of a redirection from the visited source node.</summary>
internal static class CallSiteRewriter
{
    /// <param name="visitedNode">The source node after the injection rewriter visited its children, so that nested
    /// redirections are already applied.</param>
    public static ExpressionSyntax Rewrite( ExpressionSyntax visitedNode, CallSiteRedirection redirection );
}
```

`SynthesizedMethodTransformation` derives from `IntroduceDeclarationTransformation<MethodBuilderData>`, so the existing injection machinery handles it without new branches: registration of the builder (ENG26 `Linking\LinkerInjectionStep.cs:443-459`), creation of the syntax generation context of the insertion point (lines 540-567), insertion at `InsertPosition(Within, container)` (ENG26 `Linking\LinkerInjectionStep.Rewriter.cs:400-411, 651-655`), reachability and aspect-reference scanning (ENG26 `Linking\LinkerAnalysisStep.ReachabilityAnalyzer.cs:100-139`; `LinkerAnalysisStep.AspectReferenceCollector.cs:236-238`), and linking as an introduced member (ENG26 `Linking\LinkerRewritingDriver.cs:414-418`).

Threading changes:

| File | Change |
|---|---|
| ENG26 `Linking\AspectLinkerInput.cs:35-71` | Add `ExtensionLinkerInput Extensions { get; }` and an optional constructor parameter (default `ExtensionLinkerInput.Empty`). The linker test harness (TST26 `Metalama.Framework.Tests.LinkerTests\Runner\LinkerTestInputBuilder.cs:106-111`) keeps compiling and gains an overload. |
| ENG26 `Linking\LinkerInjectionStepOutput.cs:87-150` | Add `ExtensionExpansionState ExpansionState`. |

`CallSiteAdviceInfo` and `LinkerAnalysisStepOutput` do not change. An earlier version added `HasCallSiteRedirections` to the first and a `CallSiteRedirectionPlan` to the second, for the collector and the check after linking that RC39 removed.

#### 10.5.2 Data flow and verification of the injection-time rewrite

Data flow (RC39):

1. The premium engine calls `RedirectInvocation`, `RedirectAwait` or `RedirectMethodReference`. The factory validates the node, computes the callee syntax, the extra arguments and the argument rename map with `CompilationContext.GetSyntaxGenerationContext( options, callSiteNode )`, creates a `CallSiteRedirection` with a sequential identifier, and adds it to the dictionary of the tree.
2. `LinkerInjectionStep` passes the dictionary of each tree to the `Rewriter` of that tree. With an observability filter (preview), it removes the redirections whose `TargetTransformation` is not in the observable closure of transformations (ENG27 `Linking\LinkerInjectionStep.cs:164-172, 202-221`). The call site then stays unchanged.
3. When the rewriter visits a source `InvocationExpressionSyntax`, `AwaitExpressionSyntax`, method-group expression (`IdentifierNameSyntax`, `GenericNameSyntax` or `MemberAccessExpressionSyntax`), property or event access, `AssignmentExpressionSyntax`, or increment or decrement (`PrefixUnaryExpressionSyntax`, `PostfixUnaryExpressionSyntax`) that is a key of the dictionary, it visits the children first and then returns the final expression built by `CallSiteRewriter.Rewrite`. For an accessor site that needs a temporary in a statement context, the rewriter replaces the enclosing `ExpressionStatementSyntax` with a block (section [10.5.6](#1056-syntax-of-the-rewritten-call)). It records the redirection as applied.
4. At the end of the tree, the rewriter compares the applied redirections with the dictionary of the tree and reports LAMA0660 for each redirection that it did not apply (section [10.5.9](#1059-completeness-verification)).
5. The analysis step and the linking step see the rewritten calls as ordinary code of the intermediate compilation. They need no change for redirections.

Verification (performed on the 2027.0 code for the second product-owner batch). The question was whether the rewrite can happen in the injection rewriter, before the linker analyzes, moves and inlines bodies. The findings are:

| # | Question | Finding | Evidence |
|---|---|---|---|
| a | Does `I(...)` bind in the intermediate compilation? | Yes. The injection step first indexes every transformation, and `IndexInjectTransformation` calls `GetInjectedMembers`, which expands the templates of synthesized methods. Only then does it run the rewriter on every tree, and it builds the intermediate compilation from the rewritten trees and the injected members. Names are allocated at declaration time (RC15), so the rewriter has the final name as syntax. A local function is appended by the same rewriter pass, in the body that contains the call site (section [10.5.4](#1054-local-function-injection)). | ENG27 `Linking\LinkerInjectionStep.cs:235` (indexing), `:578` (`GetInjectedMembers`), `:354-387` (rewriting and intermediate compilation) |
| b1 | Does any linker step re-derive code from the original source nodes? | No. The analysis step works on the intermediate compilation. The linking step builds declarations from intermediate symbols and syntax. The source compilation model is used only for syntax generation contexts and for the derived types of `IInitializable`. | ENG27 `Linking\LinkerLinkingStep.cs:89, 97`; `Linking\LinkerAnalysisStep.cs:180, 187, 287` |
| b2 | Promoted fields | Named exception. `PromoteFieldTransformation` copies the field's `InitializerExpression` to the new property (ENG27 `AdviceImpl\Introduction\PromoteFieldTransformation.cs:58`). For a source field, this is a `SourceUserExpression` over the source node (ENG27 `CodeModel\Source\SourceField.cs:149`), so the property initializer is generated from the source node, not from the rewritten tree. The injection step marks the field declarator as removed syntax (ENG27 `Linking\LinkerInjectionStep.cs:489-497`), and the rewriter skips removed declarators (ENG27 `Linking\LinkerInjectionStep.Rewriter.cs:1449, 1484`). The premium engine therefore keeps `NonInterceptableReason.PromotedFieldInitializer` (section [6.2.3](06a-call-site-model.md#623-limitations)). The completeness check reports LAMA0660 if such a redirection is requested anyway, because the rewriter never visits the declarator. | as stated |
| b3 | `SourceUserExpression` and templates that embed source expressions | Named exception. A template that emits `InitializerExpression` of a source field, property or event emits the source node, which contains the original call (ENG27 `CodeModel\Source\SourceProperty.cs:123`; `SourceEvent.cs:203`; `Templating\Expressions\SourceUserExpression.cs:31-41`). The source declaration itself is still rewritten, so the copy is an additional, unrewritten call in code introduced by an aspect. R6 excludes introduced code from interception, so this is the specified behavior, and it is documented. The annotation design had the same behavior: the source node never carried an annotation. The inspection-only expressions of the interception contexts cannot be emitted (LAMA0297), so they add no new case. | as stated |
| b4 | Does the preview or any analysis step need the original call in the intermediate compilation? | No analysis step needs it: the analysis step collects aspect references from annotated nodes, which a source call never is, and caller-member references from invocations of methods declared in overridden types (item c). The preview needs every rewritten call to bind, which the removal of step 2 of the data flow guarantees. The earlier version relied on the original call being present for the preview only (section [10.5.10](#10510-preview-and-partial-compilations)). | ENG27 `Linking\LinkerAnalysisStep.cs:1251-1286`; `Linking\LinkerInjectionStep.cs:164-172, 202-221` |
| c | `CallerMemberSubstitution` | Not harmful, and no skip rule is needed. `GetCallerAttributeReferencesAsync` finds invocations, in override targets, of methods whose caller-member parameters are omitted, and only for methods of types that contain overridden members (ENG27 `Linking\LinkerAnalysisStep.cs:1251-1286, 1301-1320, 1327-1373`). A rewritten call passes its caller-information values explicitly (section [6.7](06b-signatures-and-validation.md#67-caller-information-materialization)), so none is omitted. The call of the intercepted method inside a synthesized interceptor is in an injected member, which is not an override target, so it is skipped (line 1301). When an existing-method interceptor with an omitted caller-member parameter is declared in a type that contains overridden members, the substitution supplies the name of the source member, which is the correct value. | as stated |
| d | Positions that the rewriter does not visit today | Four changes are needed. Field declarators and event-field declarators are not visited (ENG27 `Linking\LinkerInjectionStep.Rewriter.cs:1434-1510, 1836-1875`). The base list of a type, which holds primary-constructor base arguments, is not visited (lines 374-469). The compilation unit and namespace visitors replace the visited members with the original members when they add injections (lines 1903-1907, 1927, 1946-1948). Method bodies, accessor bodies, expression bodies, property initializers, constructor initializers, local functions, lambdas and top-level statements are visited through the base visitor calls (lines 1523-1527, 1594-1598, 1693-1697, 1801-1804, and the base visit of the compilation unit). The rewriter has no invocation or await visitor today. Section [10.5.3](#1053-injection-step-and-rewriter) specifies the changes. | as stated |
| e | Inlining | A rewritten call inside a source body is ordinary syntax. The inliner inlines a body only for a canonical invocation, whose arguments are exactly the parameters of the method, so parameter names do not change (ENG27 `Linking\Inlining\InlinerHelper.cs:20-40`). The inlining substitution renames labels only (ENG27 `Linking\Substitution\InliningSubstitution.LabelRenamingRewriter.cs:16-31`). `this` keeps its meaning, because the body stays in the same type. The rewritten call therefore keeps its meaning after inlining, like any other source call. | as stated |
| f | `OnInitialized` call-site wrapping | No interaction. The finder walks all trees of the intermediate compilation for object creations and `with` expressions of initializable types (ENG27 `Linking\LinkerAnalysisStep.OnInitializedCallSiteFinder.cs:21-60`). An object creation that is an argument of a rewritten call is still a node of the intermediate tree, and its substitution applies to it. A rewritten call is an invocation, not an object creation, so it is never wrapped. | as stated |
| g | Completeness without annotations | A per-tree record of the applied redirections, compared with the dictionary of the tree at the end of the rewrite. The rewriter records a redirection only when it returns the rewritten node. The known paths that discard a visited node are the compilation unit and namespace visitors, which the changes of item d fix. The check cannot see a rewritten node that the linking step drops later. The linking step drops intermediate code in two cases: the body of an override target whose default semantic is unreachable, by design (ENG27 `Linking\LinkerRewritingDriver.Methods.cs:60-65`); and the initializer of a property that is not an override target but has substitutions in an accessor body, an existing defect that fix F19 repairs (ENG27 `Linking\LinkerRewritingDriver.Properties.cs:216, 298`). | as stated |
| h | Method-reference sites (decision 8 of the second batch) | Sound, with the same named exceptions. A method group is an expression node of a source body or initializer, like an invocation, so every argument of items a to g applies. The rewriter has no visitor for simple names, generic names or member accesses today (ENG27 `Linking\LinkerInjectionStep.Rewriter.cs`: its overrides are type, member, parameter, type-parameter, accessor, compilation-unit and namespace visitors), so three visitors are added. No step of the linker reads source method groups: the analysis step keys caller-member references on invocations and `OnInitialized` sites on object creations (items c and f). A rewritten method group of an existing or synthesized method binds in the intermediate compilation for the reason of item a. The wrapper is ordinary syntax: a cast, a static lambda and a nested lambda, which the inliner and the linking step copy as any source lambda. | ENG27 `Linking\LinkerInjectionStep.Rewriter.cs:316-1955`; `Linking\LinkerAnalysisStep.cs:1251-1286`; `Linking\LinkerAnalysisStep.OnInitializedCallSiteFinder.cs:21-60` |

| i | Accessor sites (third product-owner batch) | Sound, with the same named exceptions. A property or event access, an assignment, a compound assignment and an increment are expression nodes of a source body or initializer, so the arguments of items a to g apply. The block of a statement context replaces an `ExpressionStatementSyntax`, which is a node of the same body. The analysis step keys caller-member references on invocations, so it treats the rewritten accessor calls like any invocation of an interceptor (item c). The rewriter has no visitor for assignments, unary expressions and expression statements today, so these visitors are added (section [10.5.3](#1053-injection-step-and-rewriter)). | ENG27 `Linking\LinkerInjectionStep.Rewriter.cs:316-1955` |

Conclusion: the injection-time rewrite is adopted, for call sites, awaits, method-reference sites and accessor sites, with three named exceptions. Promoted field initializers stay a limitation that the premium engine detects (item b2). Source expressions that a template copies keep the original call, as specified by R6 (item b3). In the preview, a redirection whose synthesized target is removed by the observability filter is dropped (item b4). The annotation, the analysis-step collector, `CallSiteRedirectionSubstitution`, the table of raw-copy rewrites, the skip rule of `CallerMemberSubstitution`, the check after linking and LAMA0661 are removed.

#### 10.5.3 Injection step and rewriter

| Location | Change |
|---|---|
| ENG26 `Linking\LinkerInjectionStep.cs:72-75` | `var naming = input.Extensions.NamingServices ?? new LinkerNamingServices( input.FinalCompilationModel );` and use its helper provider, name provider and lexical-scope factory. |
| after line 172 | With an observability filter, remove from `input.Extensions.CallSiteRedirections` the redirections whose `TargetTransformation` is not in `syntaxTreeTransformations` (section [10.5.10](#10510-preview-and-partial-compilations)). A redirection whose target transformation failed to expand is also removed, because the template error is already reported. Expansion happens during indexing, before the rewriter runs (ENG27 `Linking\LinkerInjectionStep.cs:235, 578`), so the failure is known. |
| loop at lines 114-149 | After `IndexInjectTransformation`, call `IndexInjectLocalFunctionTransformation`, which creates a `MemberInjectionContext` for the host body, calls `GetLocalFunction`, and records the statement with `transformationCollection.AddInjectedLocalFunction` and `AddSourceMemberWithInjectedCode`. |
| lines 344-349 (ENG27 354-365) | Pass the dictionary of `initialSyntaxTree`, or `null`, to the `Rewriter` constructor. After `rewriter.Visit( oldRoot )`, report LAMA0660 for each redirection of the tree that the rewriter did not apply (section [10.5.9](#1059-completeness-verification)). |
| lines 377-387 | Pass `transformationCollection.SourceMembersWithInjectedCode` to `LinkerInjectionRegistry`, which translates them to intermediate symbols (ENG26 `Linking\LinkerInjectionRegistry.cs:89-112`). |
| lines 396-406 | Add `input.Extensions.ExpansionState` to the output. |

`TransformationCollection` (ENG26 `Linking\LinkerInjectionStep.TransformationCollection.cs`) gains `AddInjectedLocalFunction`, `TryGetInjectedLocalFunctions`, `AddSourceMemberWithInjectedCode`, and `SourceMembersWithInjectedCode`, which replaces `ConstructorsWithInsertedStatements` (line 75). `LinkerInjectionRegistry` gains `TryGetIntermediateSyntaxTree` and `GetSourceMembersWithInjectedCode()` (generalizing line 560).

Rewrite of call sites in the injection rewriter (new overrides; the rewriter has no invocation or await visitor today, ENG27 `Linking\LinkerInjectionStep.Rewriter.cs`):

```csharp
public override SyntaxNode? VisitInvocationExpression( InvocationExpressionSyntax node )
{
    // Visit the children first, so that a redirected call in an argument is already rewritten.
    var visited = (InvocationExpressionSyntax) base.VisitInvocationExpression( node )!;

    if ( this._callSites != null && this._callSites.TryGetValue( node, out var redirection ) )
    {
        this._appliedCallSites.Add( redirection );

        return CallSiteRewriter.Rewrite( visited, redirection );
    }

    return visited;
}

public override SyntaxNode? VisitAwaitExpression( AwaitExpressionSyntax node )
{
    // Same pattern. For `await F()` with both targets redirected, the operand is rewritten first: `await IA( IF() )`.
}
```

Accessor sites use the overrides `VisitAssignmentExpression`, `VisitPrefixUnaryExpression` and `VisitPostfixUnaryExpression` for the nodes of writes, compound assignments, `??=`, event subscriptions, increments and decrements, the member-access and name overrides below for reads, and `VisitExpressionStatement`, which returns the block of a statement-context site when its expression is a key of the dictionary. The factory never registers both an expression statement form and an expression form for one node.

Method-reference sites use three more overrides, `VisitIdentifierName`, `VisitGenericName` and `VisitMemberAccessExpression`, with the same pattern. A method-group key is never the expression of an invocation, because such a name is a call site and not a method group (section [6.2.10](06a-call-site-model.md#6210-method-reference-sites)), so the invocation visitor and the method-group visitors never compete for one node. When the tree has no dictionary, each override returns the base result after one null check, so trees without method-reference sites pay nothing more.

The rewriter visits original nodes and returns rewritten nodes, so `node` is the source node and the lookup is by reference. A rewriter instance processes one tree (ENG27 `Linking\LinkerInjectionStep.cs:354-365`), so `_appliedCallSites` needs no synchronization.

Descending where the rewriter does not descend today, only when `_callSites` is not null:

| Location (ENG27 `Linking\LinkerInjectionStep.Rewriter.cs`) | Change |
|---|---|
| `VisitFieldDeclarationCore`, lines 1434-1510 | Visit each declarator that is not removed syntax, in both branches (lines 1447-1470 and 1482-1490), and rebuild the declaration when a declarator changed. A removed declarator, which belongs to a promoted field, is not visited, so its redirections are reported by the completeness check. |
| `VisitEventFieldDeclarationCore`, lines 1836-1875 | Same pattern, in both branches (lines 1848-1866 and 1868-1873). |
| `VisitTypeDeclaration`, lines 374-469 | Visit the argument list of a `PrimaryConstructorBaseTypeSyntax` of the original base list before `ApplyMemberLevelTransformationsToPrimaryConstructor` (line 385), which then starts from the visited list. |
| `VisitCompilationUnit`, lines 1903-1907; `VisitNamespaceDeclaration`, line 1927; `VisitFileScopedNamespaceDeclaration`, lines 1946-1948 | Use the members of the visited node instead of the original members when injections exist. This fixes an existing defect that discards the rewrites of members when root or namespace injections exist (fix F15). |

Method bodies, accessor bodies, expression bodies, property initializers, constructor initializers, local functions, lambdas and top-level statements are already visited through the base visitor calls (lines 1527, 1598, 1627, 1697, 1757, 1804, and the base visit of the compilation unit at lines 1904 and 1911). Attribute arguments, parameter default values and enumeration member values are constant expressions, so they contain no call site that the premium engine redirects (`nameof` is never presented, section [6.2.2](06a-call-site-model.md#622-silent-refusals)).

The syntax of the final call is built by `CallSiteRewriter` (section [10.5.6](#1056-syntax-of-the-rewritten-call)). It carries the generated-code annotation of the origin on the callee and on the extra arguments, as the earlier substitution did (section [10.4.7](10a-oss-bridge-hook-factory.md#1047-attribution-and-ordering)). The transformed call is part of the intermediate tree, so the analysis step, the linking step and the cleanup rewriter treat it as source code of the member.

#### 10.5.4 Local-function injection

The rewriter appends local functions to the root block of the source body, after the base visit and before `InjectStatementsIntoMemberDeclaration`. The lookup key is the original declaration node.

| Host | Location | Transformation |
|---|---|---|
| method | `VisitMethodDeclarationCore`, after line 1569 | Block body: add statements. Expression body: `{ return expr; functions }` or `{ expr; functions }` depending on `DoReturnStatementsRequireArgument()` (ENG26 `Linking\DeclarationExtensions.cs:35`); `throw` expressions become `throw` statements. |
| constructor | `VisitConstructorDeclarationCore`, after line 1490 | Body or expression body (void). The constructor initializer is not a host position. |
| operator | `VisitOperatorDeclarationCore`, after line 1598 | As a method. |
| conversion operator, finalizer | new cases in `VisitMember` (lines 1124-1137) | As a method. |
| accessor | `VisitAccessorDeclaration`, after line 1775 | `get` returns a value; `set`, `init`, `add`, `remove` are void. |
| expression-bodied property or indexer | `VisitPropertyDeclarationCore` after 1668, `VisitIndexerDeclarationCore` after 1728 | Convert `=> expr` to `{ get { return expr; functions } }` (PO35). |

The function is placed after the last statement. Body analysis ignores local-function statements when it looks for the last flow statement (ENG26 `Linking\LinkerAnalysisStep.SemanticBodyAnalyzer.cs:367-385, 425-440`). When entry statements wrap the body (ENG26 `Linking\LinkerInjectionStep.Rewriter.cs:957-973`), the source block becomes a flattenable block, so after flattening the function is still in scope of every source statement. The local function moves with the body when the linker inlines it or emits it as `X_Source`.

Code in the local function may contain aspect references. The host body is not an injected member, so `SourceMembersWithInjectedCode` feeds the aspect-reference scan (ENG26 `Linking\LinkerAnalysisStep.AspectReferenceCollector.cs:289-305`) and the non-discardable semantics (ENG26 `Linking\LinkerAnalysisStep.cs:113-126`), as `ConstructorsWithInsertedStatements` does today.

#### 10.5.5 Analysis step

The analysis step needs no change for redirections. It analyzes the intermediate compilation, which already contains the rewritten calls, and treats them as ordinary calls of source bodies: reachability, inlining decisions, aspect references and the `OnInitialized` finder see an invocation of the interceptor method (section [10.5.2](#1052-data-flow-and-verification-of-the-injection-time-rewrite), items c and f).

An earlier version added a collector here, `LinkerAnalysisStep.CallSiteRedirectionCollector`, which found annotated nodes, resolved their context with a shared `CallSiteContextResolver`, and registered one substitution per node and inlining context. RC39 removed it, together with the context kinds (`MethodBody`, `MemberInitializer`, `ConstructorInitializer`, `PrimaryConstructorBaseArguments`, `Unsupported`) that the substitutions needed. The redirections of an unreachable override-target body are dropped with the body by the linking step, as the earlier version did explicitly (ENG27 `Linking\LinkerRewritingDriver.Methods.cs:60-65`).

#### 10.5.6 Syntax of the rewritten call

`CallSiteRewriter.Rewrite` (section [10.5.1](#1051-internal-model-and-threading)) receives the visited node, whose children the injection rewriter already rewrote. Nested redirections therefore compose, for example `await I2( I1( a ) )`, or `I1( X.I2 )` for a method group passed to an intercepted call.

Method-reference redirections replace the method-group expression:

| Receiver mode | Source method group | Result |
|---|---|---|
| `Drop`, static target | `M`, `C.M`, `M<T>` | `global::X.I` or `global::X.I<T>`; a local function: `I` |
| `Drop`, instance target on `this` | `M`, `this.M`, `base.M` | `this.I` |
| `MemberOfReceiver` | `r.M`, `this.M`, `M` | `r.I`, `this.I` |
| `ExtensionReceiver` | `r.M`, `x.Ext` | `r.I`, `x.I` |
| `FirstArgument`, `FirstArgumentByRef` | `r.M`, `x.Ext` | `((global::System.Func<TR, D>) ( static receiver => { null check; return ( P a ) => global::X.I( receiver, a ); } ))( r )`, with `ref receiver` for `FirstArgumentByRef` (section [6.4.12](06b-signatures-and-validation.md#6412-method-reference-sites)) |
| `Drop`, function pointer | `&C.M`, whose operand is `C.M` | the operand becomes `global::X.I`, so the site is `&global::X.I` |

The rewritten method group keeps the trivia of the source node. The receiver `r` keeps its source syntax and is evaluated where the source evaluated it. The rules below are those of the earlier `CallSiteRedirectionSubstitution`, which RC39 replaced. They are syntax-only, as the substitution was.

| Receiver mode | Source shape | Result |
|---|---|---|
| `Drop` | `T.M(args)`, `M(args)` (static target), `base.M(args)` (virtual target) | `Callee(args, extra)` |
| `FirstArgument` | `r.M(args)` / `M(args)` or `this.M(args)` / `base.M(args)` (non-virtual target) / `p->M(args)` | `Callee(r, args, extra)` / `Callee(this, args, extra)` / `Callee(this, args, extra)` / `Callee(*p, args, extra)` |
| `FirstArgumentByRef` | `r.M(args)` / `M(args)` / `p->M(args)` | `Callee(ref r, args, extra)` / `Callee(ref this, args, extra)` / `Callee(ref *p, args, extra)` |
| `FirstArgumentByIn` | `r.M(args)` / `M(args)` | `Callee(in r, args, extra)` / `Callee(in this, args, extra)` |
| `ExtensionReceiver` | `r.M(args)` | `r.I(args, extra)` |
| `ExtensionReceiver` | `.M(args)` inside `a?....` | `.I(args, extra)` |
| `ExtensionReceiver` | `M(args)` | `this.I(args, extra)` |
| `MemberOfReceiver` | `r.M(args)` | `r.I(args, extra)` |
| `MemberOfReceiver` | `.M(args)` inside `a?....` | `.I(args, extra)` |
| `MemberOfReceiver` | `M(args)` or `this.M(args)` | `this.I(args, extra)` |

| Target | Callee |
|---|---|
| static method (synthesized or existing) | `global::N.C<TArgs>.I<TMethodArgs>` with simplifier annotation |
| instance method in the calling hierarchy (modes `Drop` and `FirstArgument*`) | `this.I<TMethodArgs>` (the explicit `this` avoids capture by a local named `I`) |
| instance method of the receiver's hierarchy (`MemberOfReceiver`) | `I<TMethodArgs>` as a member name of the kept receiver |
| local function | `I<TMethodArgs>` |
| extension method (`ExtensionReceiver`) | `I<TMethodArgs>` as a member name |

Named arguments are renamed with `ArgumentNameMap`. The receiver is inserted positionally, so a following named argument keeps its meaning. Extra arguments are appended as named arguments, which is valid after any argument list. Trivia of the visited node is kept. The callee and the extra arguments carry the origin's generated-code annotation.

Await rule: `await e` becomes `await Callee(e, extra)`, or `await Callee(e, extra).ConfigureAwait(false)` when requested. The `await` keyword and its trivia are kept.

Accessor rule. `Get(x)` and `Set(x, v)` denote the callee of the get use and of the set use with the receiver mode of the request, for example `Cn.Ig(x)`, `x.Ig()` or `this.Ig(x)`; a use without redirection is the plain access `x.P` or `x.P = v`. `op` is the operator of the site with the explicit conversion of the compound assignment, and `t` and `old` are the names that the factory reserved.

| Site | Without temporary | Statement context, with temporary | Expression context, with temporary |
|---|---|---|---|
| `r.P` | `Get(r)` | not applicable | not applicable |
| `r.P = v` | `Set(r, v)` | not applicable | not applicable |
| `r.E += h`, `r.E -= h` | `Add(r, h)`, `Remove(r, h)` | not applicable | not applicable |
| `r.P op= v`, `++r.P`, `r.P++` (value not used) | `Set(r, Get(r) op v)` | `{ var t = r; Set(t, Get(t) op v); }`; struct variable: `{ ref var t = ref r; ... }` | `r is var t ? Set(t, Get(t) op v) : default!` |
| `r.P++` (value used) | `Get(r) is var old && Set(r, old + 1) is var _ ? old : default!` | not applicable (the value is used) | `r is var t && Get(t) is var old && Set(t, old + 1) is var _ ? old : default!` |
| `r.P ??= v` | `Get(r) is { } c ? c : Set(r, v)` (value used), `if ( Get(r) is null ) Set(r, v);` (statement) | `{ var t = r; if ( Get(t) is null ) Set(t, v); }` | `r is var t ? ( Get(t) is { } c ? c : Set(t, v) ) : default!` |
| `r?.P`, `r?.P = v` | `r?.Ig()`, `r?.Is(v)` (modes `ExtensionReceiver` and `MemberOfReceiver`) | not applicable | not applicable |

In the context `DiscardedExpression`, the expression form is written as `_ = <form>`, because a conditional expression is not a statement expression (CS0201). The value of the statement form is never used. The block of the statement context carries the trivia of the replaced statement. The pattern variables and the locals get the names that the factory reserved with the lexical scope of the host, because the expression variables of an expression statement have the scope of the enclosing block, and two sites of one block must not declare the same name.

When `ResultCast` is set, the rewriter writes `((T)(result))`. The cast is part of the compiled code, because `CodeFormatter` runs only when formatted output is requested (ENG27 `Pipeline\CompileTime\CompileTimeAspectPipeline.cs:243`), and the simplifier annotations only shorten the formatted output. The rewrite is syntax-only, so it cannot tell whether the value is consumed. The caller of the factory decides it with the semantic model of the call site and sets `ResultCast` only when the value is used: not when the call site is the expression of an `ExpressionStatementSyntax`, an element of the initializer or incrementor list of a `for` statement, or the expression body of a member, lambda or local function whose return type is `void`, `Task` or `ValueTask` for an async lambda. A cast is not a valid statement expression (CS0201). The factory rejects a `ResultCast` inside a conditional access (section [10.4.8](10a-oss-bridge-hook-factory.md#1048-validation-performed-by-the-factory)).

#### 10.5.7 Linking step

The linking step needs no change for redirections. The paths that copy intermediate expressions raw now copy the rewritten call:

| Location | Expression that already contains the rewritten call |
|---|---|
| ENG26 `Linking\LinkerRewritingDriver.Constructors.cs:267, 275, 286` | field, event-field and property initializers moved into the auxiliary constructor of a removed primary constructor |
| same file, line 382 | primary-constructor base arguments moved into `: base(...)` |
| same file, line 387 | constructor initializer |
| ENG26 `Linking\LinkerRewritingDriver.Properties.cs:137, 180` | initializer copied into the `_Source` property |
| ENG26 `Linking\LinkerRewritingDriver.Types.cs:18-44, 64-100` | base list of a type that keeps its primary constructor |

Expression-bodied property getters and top-level statements need no linking-step change either: the injection rewriter visits them (section [10.5.3](#1053-injection-step-and-rewriter)). An earlier version generalized `RewriteInitializer` into `RewriteDeclarationExpression`, added an `ArrowExpressionClause` case to `GetBodyRootNode`, and added `VisitGlobalStatement` to the `LinkingRewriter`, so that substitutions could reach these positions. RC39 removed these changes from the interceptor work. Fix F3 (section [10.5.11](#10511-follow-up-oninitialized-call-site-gaps)) still needs some of them for `OnInitialized`.

One existing defect of the linking step can still drop a rewritten call: `GetLinkedDeclaration` sets `initializer: null` for a property that is not an override target but has substitutions in an accessor body, which is possible for a C# 14 semi-automatic property such as `string P { get; set => field = Normalize(value); } = Compute();` (ENG27 `Linking\LinkerRewritingDriver.Properties.cs:216, 298`). With RC39, a redirection never causes this path, because it registers no substitution, but another substitution in the accessor body does, and then the whole initializer is lost. Fix F19 of section [11.2](11-delivery-plan.md#112-fix-track) keeps the initializer when `isOverrideOrOverrideTarget` is false.

#### 10.5.8 Interaction with CallerMemberSubstitution

No rule is needed. `GetCallerAttributeReferencesAsync` searches the intermediate compilation after the injection step (ENG27 `Linking\LinkerAnalysisStep.cs:229-233, 1251-1373`). It sees the rewritten call, whose caller-information arguments are explicit (section [6.7](06b-signatures-and-validation.md#67-caller-information-materialization)), so it registers no `CallerMemberSubstitution` for it. The call of the intercepted method inside a synthesized interceptor is in an injected member, which is not an override target, so the search skips it (line 1301). An earlier version had to skip annotated invocations, because the redirection and `CallerMemberSubstitution` were two substitutions of the same node, and `AddSubstitution` throws on such a conflict (ENG26 `Linking\LinkerAnalysisStep.SubstitutionGenerator.cs:847-852`).

#### 10.5.9 Completeness verification

Each `Rewriter` records the redirections that it applied (section [10.5.3](#1053-injection-step-and-rewriter)). After `rewriter.Visit( oldRoot )`, the injection step compares the record with the dictionary of the tree and reports LAMA0660 for each redirection that was not applied, in the order of the redirection identifiers. The location is the source call site. The diagnostic is an error, because a missing rewrite changes program behavior without notice (PO34). The check costs one set lookup per redirection.

The record is added only when the rewriter returns the rewritten node, and the visitors that discarded visited members are fixed (fix F15), so a redirection counts as applied only when the rewritten call reaches the intermediate tree. The check does not see a node that the linking step drops later. The linking step drops intermediate code in two cases only: the body of an unreachable override target, by design (ENG27 `Linking\LinkerRewritingDriver.Methods.cs:60-65`), and the initializer of the existing defect of section [10.5.7](#1057-linking-step), which fix F19 repairs.

LAMA0661 (a call site in a context that the linker cannot rewrite) is withdrawn. Its contexts were attribute arguments, parameter default values and enumeration member values, which are constant expressions and contain no redirected call site. Any other call site that the rewriter does not reach is reported with LAMA0660.

The premium engine does not request a redirection for a call site in the initializer of a field whose `IField.OverridingProperty` is not null in `ExtensionTransformationContext.StageFinalCompilation` (FW27 `Code\IField.cs:61`). The promoted property re-emits the initializer from a `SourceUserExpression` over the source node (ENG27 `AdviceImpl\Introduction\PromoteFieldTransformation.cs:58`), and the rewriter never visits the removed field declarator. The engine presents such a call site with `NonInterceptableReason.PromotedFieldInitializer` and reports LAMA1012 when the provider does not skip it (section [6.2.3](06a-call-site-model.md#623-limitations)). LAMA0660 is the safety net if such a redirection is requested anyway.

#### 10.5.10 Preview and partial compilations

The rewrite now happens before the intermediate compilation exists, so a rewritten call must bind in it. With an observability filter, the injection step keeps only the transformations of observed canonical declarations (ENG27 `Linking\LinkerInjectionStep.cs:164-172, 202-221`). A synthesized method whose placement is not observed, such as a method of the generated static class, which is declared in a new tree, is therefore not injected. The injection step removes every redirection whose `TargetTransformation` is not kept (section [10.5.3](#1053-injection-step-and-rewriter)), and the call site stays unchanged. An existing-method target needs no rule, because the method exists in the compilation.

- A synthesized member whose placement is in the previewed file is kept by the existing rule. A local function is in the host's tree, which is the call-site tree.
- Removed redirections are not expected by the completeness check.
- The premium engine additionally avoids redirecting call sites whose placement is outside the partial compilation, and it reports LAMA1030 for them (section [9.8](09-premium-engine.md#98-preview-live-templates-and-introspection)). In the preview, it treats the generated static class as outside the partial compilation.

An earlier version rewrote call sites in the linking step, so the preview showed rewritten call sites whose target in the generated static class was missing. The preview now shows these call sites unchanged, with LAMA1030. This is still the best-effort behavior of PO27.

#### 10.5.11 Follow-up: OnInitialized call-site gaps

The annotation design shared its plumbing with the `OnInitialized` call-site advice. RC39 removed that plumbing from the interceptor work, so fix F3 builds the parts it needs on its own, in a separate pull request with its own baselines:

| Gap | Fix, no longer shared with interceptors |
|---|---|
| Call sites in inlined source bodies get no substitution (ENG27 `Linking\LinkerAnalysisStep.SubstitutionGenerator.cs:125`). | Register `OnInitialized` references in `ProcessInliningSpecification`. |
| Call sites in local functions are keyed by the local-function symbol (ENG26 `Linking\LinkerAnalysisStep.OnInitializedCallSiteFinder.cs:124-128`). | A context resolver that attributes local functions and lambdas to the enclosing member. |
| Expression-bodied properties, primary-constructor base arguments and top-level statements are skipped (lines 94-144). | The context resolver, an `ArrowExpressionClause` case in `GetBodyRootNode` (ENG26 `Linking\LinkerRewritingDriver.cs:220-258`), and `VisitGlobalStatement` in the `LinkingRewriter`. |
| Constructor initializers are never visited (ENG26 `Linking\LinkerRewritingDriver.cs:227`). | A `RewriteDeclarationExpression` helper, generalized from `RewriteInitializer` (ENG26 `Linking\LinkerRewritingDriver.Initializers.cs:26-45`), applied to constructor initializers. |
| Moved initializers are copied raw. | The same helper on every raw-copy path of the table of section [10.5.7](#1057-linking-step). |

Two parts of the interceptor work still help `OnInitialized`: fix F15, which keeps the visited members of compilation units and namespaces, and fix F19, which keeps the initializer of a semi-automatic property.

### 10.6 Template engine changes (B2d)

#### 10.6.1 Selection at declaration time

The template is resolved and bound when `DeclareMethod` is called, so that template errors reach the premium engine as exceptions it can report at the representative call site. Expansion still happens in `LinkerInjectionStep`, like every other body template.

```csharp
/// <summary>Selects the method template (default, async or iterator variant) and its interpreted kind for a target method.</summary>
internal static class MethodTemplateSelection
{
    public static TemplateMemberRef Select( TemplateClass templateClass, IMethod targetMethod, in MethodTemplateSelector selector );
}
```

The helper is extracted from `AdviceFactory<T>.SelectMethodTemplate` (ENG26 `Advising\AdviceFactory.cs:212-338`; ENG27 `Advising\AdviceFactory.cs:240`), which then calls it with no behavior change. The factory resolves the template:

1. `templateProvider = request.Template.TemplateProvider.IsNull ? origin.DefaultTemplateProvider : request.Template.TemplateProvider`.
2. `templateClass = serviceProvider.GetRequiredService<TemplateClassProvider>().Get( templateProvider )` (ENG26 `Aspects\TemplateClassProvider.cs:11`).
3. `templateRef = MethodTemplateSelection.Select( templateClass, builder, selector )`. The builder's `IsAsync` is false at this point, so the template is interpreted as async only when the selector sets `UseAsyncTemplateForAnyAwaitable` and the return type is awaitable with a method builder (ENG26 `Advising\AdviceFactory.cs:261`). The `async` modifier of the intercepted method is irrelevant.
4. `template = templateRef.GetTemplateMember<IMethod>( finalCompilation, serviceProvider, templateProvider, ObjectReaderFactory.GetReader( tags ) )` (ENG26 `Advising\TemplateMemberRef.cs:39-106`).

Await interceptors use no user selector (decision PO60, RC60). The premium engine receives a template name and builds the selector itself: the name as the default template, no alternative template, and `UseAsyncTemplateForAnyAwaitable` in mode `Await` only (section [7.9.1](07-await-interception.md#791-accepted-template-shapes)). No variant is therefore selected. The `async` interpretation comes from the template symbol. `TemplateMember.GetEffectiveKind` returns `TemplateKind.Async` when a default template is declared `async` (ENG27 `Advising\TemplateMember.cs:80-122`), and `MustInterpretAsAsyncTemplate` returns `true` for such a template, and for a non-async default template that the flag interprets as async (ENG27 `Advising\TemplateExtensions.cs:113-115`). The proceed provider receives the effective kind (ENG27 `Templating\TemplateExpansionContext.ProceedUserExpression.cs:33`), so `ProceedBinding.AwaitParameter` gives `p` for an `async` template and `(await p)` for a non-async template (section [10.6.3](#1063-proceed-binding)). No change of the helper is needed for awaits.

A public helper exposes these steps to the premium engine. `MethodTemplateExists` serves the registration check (LAMA1005) and needs no builder. `CanImplement` serves the compile-time binding check (LAMA1016). It creates a temporary `MethodBuilder` to run the signature callback, so it needs the origin of the registration and a `CompilationModel` produced by the pipeline. A `MethodBuilder` requires an `AspectLayerInstance` and therefore an aspect instance (ENG27 `CodeModel\Introductions\Builders\MethodBuilder.cs:43-49`; `Aspects\AspectLayerInstance.cs:14-19`). At design time, Phase B has no pipeline origin, so the design-time part of LAMA1016 is limited to `MethodTemplateExists` and to a check of the run-time parameter count against the template symbol.

```csharp
/// <summary>Resolves and checks method templates for extensions, without expanding them.</summary>
public static class ExtensionTemplateServices
{
    /// <summary>Determines whether a template provider declares a method template with the given name.</summary>
    public static bool MethodTemplateExists( ProjectServiceProvider serviceProvider, TemplateProvider templateProvider, string templateName );

    /// <summary>
    /// Gets whether a method template is declared <c>async</c>, and its declared return type translated into
    /// <paramref name="compilation"/>, without expanding the template. The type keeps <c>dynamic</c> type arguments.
    /// </summary>
    public static bool TryGetMethodTemplateShape(
        ProjectServiceProvider serviceProvider,
        TemplateProvider templateProvider,
        string templateName,
        ICompilation compilation,
        out bool isAsync,
        [NotNullWhen( true )] out IType? declaredReturnType );

    /// <summary>
    /// Checks that a method template can implement a method with the given signature, and returns the reason when it cannot.
    /// The signature is described with the same callback as <see cref="SynthesizedMethodRequest.BuildSignature"/>.
    /// The compilation must be a <c>CompilationModel</c> produced by the current pipeline.
    /// </summary>
    public static bool CanImplement(
        ProjectServiceProvider serviceProvider,
        ExtensionContributionOrigin origin,
        ICompilation compilation,
        TemplateProvider templateProvider,
        in MethodTemplateSelector selector,
        INamedType containingType,
        Action<IMethodBuilder> buildSignature,
        int hiddenLeadingParameterCount,
        int nameOnlyTrailingParameterCount,
        object? arguments,
        [NotNullWhen( false )] out string? reason );
}
```

`TryGetMethodTemplateShape` serves stage 1 of await interceptors, which derives `T_I` from the declared return type of an `async` template (section [7.9.1](07-await-interception.md#791-accepted-template-shapes)) at compile time and in Phase B, without a pipeline origin. It reads the template symbol and translates the type with `TryForCompilation`, as the binding does (ENG27 `Advising\TemplateBindingHelper.cs:750-771`). It is new public API of the open-source engine, which the product owner accepted under PO2 (PO71), and it contains no interceptor concept. For a template declared with `AnyAwaitable` or `AnyAwaitable<T>`, it returns that type, and the premium engine chooses the task type (section [7.9.1](07-await-interception.md#791-accepted-template-shapes)).

#### 10.6.2 Binder with hidden leading parameters

New method in ENG26 `Advising\TemplateBindingHelper.cs`:

```csharp
/// <summary>
/// Binds a method template to a synthesized method. Run-time template parameters are bound by name among all parameters of
/// the target, then by ordinal among the parameters that follow the hidden leading parameters and precede the name-only
/// trailing parameters. Run-time type parameters are bound by ordinal. Types are verified with the same rules as for
/// overrides.
/// </summary>
public static BoundTemplateMethod ForSynthesizedMethod(
    this TemplateMember<IMethod> template,
    IMethod targetMethod,
    int hiddenLeadingParameterCount,
    int nameOnlyTrailingParameterCount = 0,
    IObjectReader? arguments = null );
```

The non-operator branch of `ForOverride` (ENG26 `Advising\TemplateBindingHelper.cs:569-641`) moves into a private helper with an ordinal offset. `ForOverride` calls it with offset 0 (no behavior change). A new binder is needed because `ForIntroduction` maps by index (lines 70-102), so template parameter 0 would bind to the receiver, and the ordinal fallback of `ForOverride` starts at 0 (lines 585-587). The name-only trailing parameters serve the added parameters of section [5.6.9](05b-api-providers-contexts-results.md#569-added-parameters-and-pulled-values): a template binds them by name only, so that a template parameter never binds to an added parameter by position (RC65). `SynthesizedMethodTemplate` carries the two counts, and `ExtensionTemplateServices.CanImplement` takes them.

#### 10.6.3 Proceed binding

`TemplateExpansionContext` needs no change. Its internal constructor already takes a proceed provider (ENG26 `Templating\TemplateExpansionContext.cs:169-200`), and `ProceedUserExpression.Validate` already validates the `ProceedAsync` and iterator variants against `meta.Target`, which is the synthesized method (ENG26 `Templating\TemplateExpansionContext.ProceedUserExpression.cs:38-63`).

```csharp
/// <summary>Holds a proceed binding whose declarations are translated into the final compilation.</summary>
internal sealed class ExtensionProceedBinding
{
    public static ExtensionProceedBinding Create( ProceedBinding binding, IMethod synthesizedMethod, CompilationModel finalCompilation );
}

/// <summary>Creates the expression of <c>meta.Proceed()</c> for a synthesized method.</summary>
internal static class ExtensionProceedExpressionFactory
{
    public static IUserExpression Create(
        ExtensionProceedBinding binding,
        IMethod synthesizedMethod,
        TemplateKind templateKind,
        SyntaxGenerationContext generationContext );
}
```

| Binding | Syntax before wrapping | Natural type | Wrapping |
|---|---|---|---|
| `InvokeStatic` | `global::N.T.M<TArgs>(args)` with simplifier annotation, or `T.M(args)` when the receiver type is a type parameter | return type of the constructed method | `ProceedHelper.CreateProceedDynamicExpression( ctx, syntax, kind, synthesizedMethod, naturalType )` |
| `InvokeOnParameter` | `p.M<TArgs>(args)` | same | same |
| `InvokeOnThis` | `this.M<TArgs>(args)` | same | same |
| `InvokeOnBase` | `base.M<TArgs>(args)` | same | same |
| `AwaitParameter`, effective template kind Default (a non-async template) | `(await p)`, or `await p` when the result is `void` | `AwaitResultType` | none; the synthesized method is async |
| `AwaitParameter`, effective template kind Async (an `async` template) | `p` | parameter type | none; the template awaits it |
| `Parameter` | `p` | parameter type | none |
| `None` | throws `InvalidOperationException`, which `UserCodeInvoker` reports as a template error | | |

Arguments are the parameter identifiers of `ArgumentParameterIndices`, with `ref`, `out` or `in` according to the parameter `RefKind`, and the casts of `ArgumentCasts`, for example `(object)p` for a `dynamic` parameter and `(string)p` for a parameter widened to `object`. The expression carries a type annotation, as `SyntaxUserExpression` does, so that `CreateReturnStatementDefault` does not insert a cast (ENG26 `Templating\TemplateExpansionContext.cs:455-495`).

The invocation is built as plain syntax, without aspect-reference annotations and without the invoker API. The source call site is a plain call that the linker does not rewrite, so a plain call in the interceptor has the same semantics: the final implementation, with virtual dispatch (G10). An invoker would add an aspect reference of the synthesized method's layer and could resolve to base or current semantics when the intercepted method is in the same type (ENG26 `CodeModel\Invokers\Invoker.cs:36-68`).

Change in ENG26 `Transformations\ProceedHelper.cs:26-30, 179-188`: `CreateProceedExpression` gets an optional `IType? invocationType`. When it is given, it replaces `overriddenMethod.ReturnType` as the reported result type and in the awaited result of the Default-and-async branch (lines 68-96). The `IsAsync` and iterator decisions still use `overriddenMethod`, which is the synthesized method, never the intercepted one. An intercepted `async void` method is therefore never wrapped in `__AsyncVoidMethod`. Existing callers pass nothing and are unchanged.

#### 10.6.4 Expansion

`SynthesizedMethodTransformation.GetInjectedMembers` builds a `MetaApi.ForMethod` with the final compilation as meta compilation (the placement may be a type introduced by any aspect of the stage) and `AdviceKind.IntroduceMethod` (the public enum is closed, FW26 `Advising\AdviceKind.cs:18-50`). It creates a `TemplateExpansionContext` with:

- the service provider of the injection context, unchanged;
- `MetaApiProperties` that carry the elements of `MetaExtensions` (section [10.6.6](#1066-meta-extensions-per-expansion-extension-data));
- the lexical scope of the method (or of the host for a local function);
- the proceed provider `kind => ExtensionProceedExpressionFactory.Create( proceed, method, kind, syntaxGenerationContext )`;
- `MetaApiProperties` that receive the aspect instance of the origin only when the origin is an aspect. For a fabric origin, they receive `null`, so `meta.AspectInstance` throws `InvalidOperationException`, as section [5.7.6](05c-api-templates.md#576-other-meta-members) states (ENG27 `Templating\MetaModel\MetaApi.cs:162-163`). The synthetic or aggregate fabric aspect instance is used only for `AspectLayerInstance`, ordering and attribution.

It expands with `TemplateDriver.TryExpandDeclaration` (ENG26 `Templating\TemplateDriver.cs:48-98`), marks the transformation failed on error, and emits a `MethodDeclaration` with `AdviceSyntaxGenerator.GetAttributeLists`, `GetSyntaxModifierList`, and the return type, type parameters, parameters and constraints of `ContextualSyntaxGenerator`.

The transformation generates the signature, the template body and the proceed expression with its own syntax generation context. The null-awareness of that context decides whether `?` annotations and `!` suppressions are emitted (ENG27 `SyntaxGeneration\ContextualSyntaxGenerator.cs:635-638, 653-656, 1100-1103`). When `NullableAnnotationsEnabled` is set, the transformation creates the context with `CompilationContext.GetSyntaxGenerationContext( options, isPartial: false, isNullOblivious: !NullableAnnotationsEnabled, endOfLine )`. When it is null, it uses the nullable context of the insertion point, read from the semantic model of the insertion tree, or the project default for a new tree. It does not use the context of `TransformationHelper.GetSyntaxGenerationContext` for a new tree, because that context is always null-aware (ENG27 `Transformations\TransformationHelper.cs:136-140`). When the requested context differs from the actual context of the insertion point, the member is wrapped in `#nullable enable` or `#nullable disable`, followed by `#nullable restore`. The same rule applies to local functions.

For a local function, `meta.Target.Method` is the builder-based description of the local function, whose `MethodKind` is `LocalFunction` (RC26). `LocalFunctionInfo` is not used: a root expansion whose `MetaApi.Method` is the local function already gets the correct return type and async handling from `CreateReturnStatement` (ENG26 `Templating\TemplateExpansionContext.cs:388-430`). `meta.This` is available when the host is an instance member, because the builder's `IsStatic` mirrors the host. A template reaches the host's parameters through compile-time values, for example `hostParameter.Value`, which emits the parameter identifier and is captured by the local function. Invoking the local function recursively through `meta.Target.Method.Invoke()` is not supported, because the method invoker throws `CannotProvideInstanceForLocalFunction` when the invoked local function has a receiver (ENG27 `CodeModel\Invokers\MethodInvoker.cs:175-181`), and the builder has the implicit receiver of its host. This is documented. A host that is a member of a C# 14 extension block is rejected by the factory (section [10.4.8](10a-oss-bridge-hook-factory.md#1048-validation-performed-by-the-factory)).

Template diagnostics are located at `TargetDeclaration.GetDiagnosticLocation()`, which for a builder is the placement. Moving them to the representative call site would need a location override in `TemplateExpansionContext` (PO36).

#### 10.6.5 Proceed multiplicity

When `ProceedMultiplicity` is `AtMostOnce`, `ExtensionProceedExpressionFactory` adds an annotation to the identifier of the parameter inside each emitted proceed expression. The identifier is annotated, and not the enclosing `(await p)`, because the expansion can remove the outer parentheses (ENG27 `Utilities\Roslyn\SyntaxExtensions.cs:100-104`; `Templating\TemplateSyntaxFactoryImpl.cs:222-247`). After `TryExpandDeclaration`, the transformation counts the annotated nodes in the expanded body and checks that none has a loop statement, a lambda, an anonymous method or a local function between itself and the body. Violations are reported with LAMA0296 (Error) at the placement, with the representative's description, and the transformation is marked failed.

#### 10.6.6 Meta extensions (per-expansion extension data)

PROPOSED public API in `Metalama.Framework` (RC40). It is generic: it names no interceptor concept (R4).

```csharp
namespace Metalama.Framework.Aspects;

/// <summary>
/// Marks a compile-time object that an extension attaches to a template expansion. Templates read it with
/// <see cref="meta.GetExtension{T}"/> or <see cref="meta.TryGetExtension{T}"/>.
/// </summary>
/// <remarks>
/// Only the engine attaches extensions, on behalf of a pipeline extension that requests the expansion. A called template
/// sees the extensions of its caller. An ordinary aspect template has no extension.
/// </remarks>
[CompileTime]
public interface IMetaExtension { }

public static class meta   // Existing class (FW27 Aspects\meta.cs:57). The new members are added to it.
{
    /// <summary>
    /// Gets the extension of type <typeparamref name="T"/> that is attached to the current template expansion.
    /// </summary>
    /// <exception cref="InvalidOperationException">No extension of type <typeparamref name="T"/> is attached to the current
    /// template expansion. The message names the type, and states that the extension is available only in templates that
    /// the extension expands, for example an interceptor template.</exception>
    public static T GetExtension<T>()
        where T : class, IMetaExtension;

    /// <summary>
    /// Gets the extension of type <typeparamref name="T"/> that is attached to the current template expansion, and returns
    /// <c>false</c> when there is none.
    /// </summary>
    public static bool TryGetExtension<T>( [NotNullWhen( true )] out T? extension )
        where T : class, IMetaExtension;
}
```

`meta` is not declared `partial` today, and it does not need to be: the members are added to the existing declaration. `NotNullWhenAttribute` is already imported by `meta.cs` (FW27 `Aspects\meta.cs:11`).

Implementation:

- The internal `IMetaApi` (FW27 `Aspects\IMetaApi.cs:16`) gains `IReadOnlyList<IMetaExtension> Extensions { get; }`. `meta.GetExtension<T>()` reads it through `CurrentContext`, which resolves `MetalamaExecutionContext.CurrentInternal.MetaApi` (FW27 `Aspects\meta.cs:60`), and returns the first element that is an instance of `T`.
- `MetaApiProperties` (ENG27 `Templating\MetaModel\MetaApiProperties.cs:19-57`) gains an `ImmutableArray<IMetaExtension> Extensions` constructor parameter with the default empty value. `MetaApi` exposes it as `IMetaApi.Extensions`. Every existing creation of `MetaApiProperties` passes nothing, so ordinary aspect templates have no extension.
- `SynthesizedMethodTransformation` passes `SynthesizedMethodTemplate.MetaExtensions` to the `MetaApiProperties` of its expansion (section [10.6.4](#1064-expansion)). The factory rejects two extensions of the same type with `ArgumentException`.
- Inheritance by called templates. `meta.InvokeTemplate` and the called-template syntax create the context of the called template with `TemplateExpansionContext.ForTemplate` (ENG27 `Templating\TemplateExpansionContext.cs:759-760`; `Templating\TemplateSyntaxFactoryImpl.cs:755, 812`). That constructor calls the copy constructor of `UserCodeExecutionContext` (ENG27 `Templating\TemplateExpansionContext.cs:283-297`), which copies the `MetaApi` of the caller (ENG27 `Utilities\UserCode\UserCodeExecutionContext.cs:254-269`). Template-internal local functions use the same copy (`ForLocalFunction`, line 757). A called template therefore sees the same `MetaApi`, and the same extensions, with no further change. The rule to specify is that a called template inherits the extensions of its caller, and that a future path that creates a new `MetaApi` for a called template must copy them.

Template compiler, to verify in M2. The template compiler special-cases members of `meta` by name. `GetMetaMemberKind` returns `MetaMemberKind.Default` for a member whose name it does not list (ENG27 `Templating\TemplateMemberClassifier.cs:107-131`). `GetExtension<T>` and `TryGetExtension<T>` therefore get the default treatment of `meta` members. Their return types are compile-time types, because `IMetaExtension` is `[CompileTime]`, so the expected classification is a compile-time call. M2 verifies this with template tests, including `TryGetExtension` with an `out var` declaration in a compile-time `if` condition, and adds a `MetaMemberKind` value only if a test fails.

An earlier version passed `SynthesizedMethodTemplate.ExpansionServices` to the service provider of the expansion with `WithService( service, allowOverride: true )`, and the premium accessors read an internal service through `MetalamaExecutionContext.Current.ServiceProvider` (RC12). That version rejected an extension slot on `MetaApiProperties`, because `IMetaApi` is internal and a public accessor on `meta` would have been interceptor-shaped. The meta extension mechanism removes both objections: the accessor is generic, and the premium package adds its interceptor-shaped accessors as C# 14 extension properties of `meta` in its own assembly (section [5.7.3](05c-api-templates.md#573-metamethodinterception-and-metaawaitinterception)).

#### 10.6.7 ConfigureAwait fix

`ConfigureAwaitUserExpression.Type` must not use the null-forgiving operator (ENG27 `Templating\TemplateExpansionContext.ConfigureAwaitUserExpression.cs:31-39`). When the expression type is not an `INamedType`, or when `OfExactSignature( "ConfigureAwait", [bool] )` returns null, it throws `TemplatingDiagnosticDescriptors.ConfigureAwaitNotAvailableOnProceed.CreateException( typeName )`, the new LAMA0295. The fix also benefits override templates (fix F9). The same diagnostic serves `AnyAwaitable.ConfigureAwait` (section [10.6.9](#1069-anyawaitable)), so its message names the expression on which `ConfigureAwait` is called.

#### 10.6.8 Inspection-only source expressions

The interception contexts expose source expressions as `IExpression` (section [5.5.2](05b-api-providers-contexts-results.md#552-methodinterceptioncontext-and-invocationargument), RC43). The code model creates source expressions with the internal `SourceUserExpression` (ENG27 `Templating\Expressions\SourceUserExpression.cs:18-24`), which extensions cannot create. PROPOSED public factory in `Metalama.Framework.Engine`:

```csharp
namespace Metalama.Framework.Engine.Templating;

/// <summary>Creates expressions of the code model that wrap source syntax.</summary>
public static class SourceExpressionFactory
{
    /// <summary>
    /// Creates an <see cref="ISourceExpression"/> over a source expression, for inspection by compile-time code. The
    /// expression cannot be emitted in generated code: an attempt to do so during a template expansion reports LAMA0297.
    /// </summary>
    /// <param name="expression">An expression of a syntax tree of the compilation of <paramref name="type"/>.</param>
    /// <param name="type">The type of the expression, in the compilation that contains the syntax tree.</param>
    public static ISourceExpression CreateInspectionOnly( ExpressionSyntax expression, IType type );
}
```

The factory returns a `SourceUserExpression` created with a new internal flag, `IsInspectionOnly`. `AsSyntaxNode`, `AsString`, `AsFullString`, `AsTypedConstant` and `Type` behave as for any source expression (FW27 `Code\ISourceExpression.cs:15-31`). `ToSyntax` throws a `DiagnosticException` with LAMA0297 when the flag is set. The template driver reports the exception as a template error, and the expansion fails. `IsAssignable` is `false`.

The reasons for the refusal are specific to an expression that is already evaluated somewhere. A second evaluation can have side effects. The expression can reference locals and parameters that do not exist where it would be emitted. Its identity is per call site, so a template that depends on it cannot be shared by a group. The factory states only the generic rule, inspection without emission, so it names no interceptor concept (R4).

#### 10.6.9 AnyAwaitable

PROPOSED open-source feature of the template language, decided by the product owner on 2026-09-25 (PO66, RC67). It fixes GitHub issue metalama/Metalama#919, "AnyAwaitable", which follows the discussion https://github.com/orgs/metalama/discussions/176, "Wanted: Support for AnyAwaitable in templates". Today, an `async` template must be declared with a concrete task type, such as `Task<dynamic?>` or `ValueTask<dynamic?>`, because C# has no interface for awaitables: the await pattern is structural (`GetAwaiter`, `IsCompleted`, `GetResult`). The feature is generic (R4). It is useful to override templates, and it is a prerequisite of the await interceptors of M5. It does not depend on M0 to M2.

```csharp
namespace Metalama.Framework.Aspects;

/// <summary>
/// Represents any awaitable type without a result in a template. A template declared <c>async AnyAwaitable</c> can be applied
/// to any awaitable type whose awaiter has no result.
/// </summary>
/// <remarks>
/// The type exists only so that templates compile as C#. The template engine replaces it with the concrete type during the
/// expansion. Every member throws <see cref="InvalidOperationException"/> when it is executed, and a use outside a template
/// is reported with LAMA0298.
/// </remarks>
[AsyncMethodBuilder( typeof(AnyAwaitableMethodBuilder) )]
public readonly struct AnyAwaitable
{
    public Awaiter GetAwaiter();

    /// <summary>Emits a call to <c>ConfigureAwait</c> on the concrete type. The expansion reports LAMA0295 when the concrete type has no such method.</summary>
    public AnyAwaitable ConfigureAwait( bool continueOnCapturedContext );

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        public bool IsCompleted { get; }

        public void GetResult();

        public void OnCompleted( Action continuation );

        public void UnsafeOnCompleted( Action continuation );
    }
}

/// <summary>
/// Represents any awaitable type whose result has the type <typeparamref name="T"/> in a template. The standard form is
/// <c>async AnyAwaitable&lt;dynamic?&gt;</c>.
/// </summary>
[AsyncMethodBuilder( typeof(AnyAwaitableMethodBuilder<>) )]
public readonly struct AnyAwaitable<T>
{
    public Awaiter GetAwaiter();

    public AnyAwaitable<T> ConfigureAwait( bool continueOnCapturedContext );

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        public bool IsCompleted { get; }

        public T GetResult();

        public void OnCompleted( Action continuation );

        public void UnsafeOnCompleted( Action continuation );
    }
}

/// <summary>The method builder that lets <c>async AnyAwaitable</c> compile. It is never executed.</summary>
[EditorBrowsable( EditorBrowsableState.Never )]
public struct AnyAwaitableMethodBuilder { /* Create, Task, Start, SetStateMachine, SetResult, SetException, AwaitOnCompleted, AwaitUnsafeOnCompleted */ }

/// <summary>The method builder that lets <c>async AnyAwaitable&lt;T&gt;</c> compile. It is never executed.</summary>
[EditorBrowsable( EditorBrowsableState.Never )]
public struct AnyAwaitableMethodBuilder<T> { /* The same members, with SetResult( T result ) and a Task property of type AnyAwaitable<T>. */ }
```

Rules:

- The types exist only so that templates compile as C#. Every member throws `InvalidOperationException` when it is executed. The method builders are never executed: they exist only because C# accepts an `async` method whose return type is a task-like type, which is a type with an `[AsyncMethodBuilder]` attribute and a builder of the expected shape.
- A use of `AnyAwaitable` or `AnyAwaitable<T>` in run-time code outside a template is reported with LAMA0298, an error. The check extends `TemplatingCodeValidator`, which already reports LAMA0233 for template-only compile-time symbols used outside a template (ENG27 `Templating\TemplatingCodeValidator.Visitor.cs:149-205`). `AnyAwaitable` is run-time typed, so that check does not cover it, and a new rule is needed.
- The template compiler classifies `AnyAwaitable` as a run-time type that is allowed only in templates, like `dynamic`. It is allowed as the return type of a template, as the type of a run-time template parameter, and as the type of an expression in the body. A local declared with the type is emitted with `var`, as for `dynamic`. A position that would emit the type name, such as a cast, `typeof` or a type argument, is reported with LAMA0298.
- `dynamic` as a type argument. The template compiler accepts `dynamic` as a type argument only for a list of well-known types, matched by name: `Task`, `ConfiguredTaskAwaitable`, `ValueTask`, `IEnumerable`, `IEnumerator`, `IAsyncEnumerable`, `ConfiguredCancelableAsyncEnumerable` and `IAsyncEnumerator` (ENG27 `CompileTime\SymbolClassifier.cs:620-636`). Any other generic type reports LAMA0227. `AnyAwaitable` must be added to this list, or `AnyAwaitable<dynamic?>` is refused.
- The standard form is `async AnyAwaitable<dynamic?>`. The non-generic `AnyAwaitable` is for templates that never return a value, because C# forbids `return value;` in an `async` method whose non-generic task-like type has no result (CS1997).
- A template that returns a value and is applied to a target whose result is `void` uses the existing behavior: the expander turns the returned expression into a statement and emits a bare `return`, as it does for `Task<dynamic?>` templates (ENG27 `Templating\TemplateExpansionContext.cs:393-407`, `CreateReturnStatementVoid` at lines 606-650).
- Template binding. `AnyAwaitable<dynamic>` is accepted for any awaitable target and `AnyAwaitable` for any awaitable target whose result is `void`, with the rules that exist for `Task<dynamic>` and `Task` (ENG27 `Advising\TemplateBindingHelper.cs:803-822`). The target keeps its own return type.
- `AnyAwaitable.ConfigureAwait( bool )` emits `ConfigureAwait` on the concrete type. When that type has no `ConfigureAwait(bool)` method, the expansion reports LAMA0295, with the fix of section [10.6.7](#1067-configureawait-fix).
- `await meta.Proceed()` keeps working in an `AnyAwaitable` template, because `meta.Proceed()` returns `dynamic`.
- The nested awaiter types avoid a generic `Awaiter` name in the namespace `Metalama.Framework.Aspects`. The prefix `Any` avoids a collision with `UnityEngine.Awaitable`.
- `UseAsyncTemplateForAnyAwaitable` of `MethodTemplateSelector` stays (ENG27 `Advising\AdviceFactory.cs:262`). The documentation presents `AnyAwaitable` as the preferred form.

Expansion:

| Template | Target | Generated method |
|---|---|---|
| Override, `async AnyAwaitable<dynamic?> T()` | `Task<int> M()` | `async Task<int> M()` |
| Override, `async AnyAwaitable<dynamic?> T()` | `ValueTask<string> M()` | `async ValueTask<string> M()` |
| Override, `async AnyAwaitable<dynamic?> T()` | A custom task-like type with a method builder, `UniTask<T> M()` | `async UniTask<T> M()` |
| Override, `async AnyAwaitable T()` | `Task M()` or `ValueTask M()` | `async Task M()` or `async ValueTask M()` |
| Override, `async AnyAwaitable<dynamic?> T()` | An awaitable type without a method builder, `ConfiguredTaskAwaitable<T> M()` | Error: C# does not accept an `async` method with this return type. |
| Await interceptor, `async AnyAwaitable<dynamic?> T()` | An await of `A` with the result `R` | `async ValueTask<R> I( A awaitable )` by default; `Task<R>` through `AwaitRewriteOptions.TaskKind` or the fallback of section [7.7](07-await-interception.md#77-task-type-of-an-async-interceptor-and-target-frameworks) |
| Await interceptor in mode `Awaitable`, non-async `AnyAwaitable<dynamic?> T()` | An await of `A` | `T_I I( A awaitable )`, with `T_I` the type passed to `WithAwaitRewriteOptions`, or `A`. A pass-through `return meta.Proceed();` returns `A`. |
| A run-time template parameter `AnyAwaitable<dynamic?> awaitable` | An await interceptor | The parameter binds to the awaited operand. |

Open points, recorded for the milestone MA:

- The addition of `AnyAwaitable` to the list of `SymbolClassifier` is required. The verification asked by the product owner found that the list is matched by name and does not contain it.
- The netstandard2.0 build of `Metalama.Framework` needs `AsyncMethodBuilderAttribute`. The C# compiler recognizes the attribute by its full name. The netstandard2.0 build references `Microsoft.Bcl.AsyncInterfaces` (FW27 `Metalama.Framework.csproj:55`), which is expected to bring `System.Threading.Tasks.Extensions`, where the attribute is declared for netstandard2.0. This was not verified; an internal copy of the attribute is the fallback ([Appendix B](appendix-b-weak-spots.md#appendix-b-weak-spots-that-remain-after-the-review)).
- Templates at the level of the awaiter, which would intercept `GetAwaiter`, `IsCompleted` and `GetResult` through a type such as `AnyAwaiter`, stay in section [16](16-future-directions.md#16-future-directions).
