# Call-site semantics: model and rules table

> Part of the [call-site interceptors design](README.md). Previous: [05d-api-samples.md](05d-api-samples.md) | Next: [06b-signatures-and-validation.md](06b-signatures-and-validation.md). Evidence prefixes and terms: [00-conventions.md](00-conventions.md).

## 6. Call-site semantics and signature derivation for invocations

This section specifies the premium component that analyzes one method invocation of the source compilation. For each `InvocationExpressionSyntax` found by the scan, it produces one of two results:

- A silent refusal (`NotACallSiteReason`). The invocation is not a call that interceptors can observe. User providers are never invoked for it.
- A call-site model (`InvocationCallSite`). The model can carry a `CallSiteLimitation`, which the public context exposes as `NonInterceptableReason`. User providers are invoked for every call-site model. When a limitation is present and the provider does not skip the call site, the engine reports LAMA1012 and does not rewrite it.

After the user provider has answered, this component produces, for a template result: the admissibility verdict of the requested placement, the interceptor signature, the rewrite plan and the proceed shape. For an existing-method result, it produces the validation verdict required by R9 and the rewrite plan. The deduplication key built from these results is defined in section [8](08-deduplication-and-naming.md#8-deduplication-and-naming). Await expressions are analyzed in section [7](07-await-interception.md#7-await-interception), which reuses sections [6.5](06b-signatures-and-validation.md#65-placement-admissibility) (placements) and 6.7 (caller information). Method-reference sites, which are method groups converted to a delegate or to a function pointer, are analyzed by a sibling component (section [6.2.10](#6210-method-reference-sites)), reuse the signature derivation of section [6.4](06b-signatures-and-validation.md#64-signature-derivation), and have their own rewrite shapes and signature constraints (section [6.4.12](06b-signatures-and-validation.md#6412-method-reference-sites)). Accessor sites, which are uses of property and event accessors, are analyzed by another sibling component (section [6.2.11](#6211-accessor-sites)), and reuse the receiver mapping, the placements and the validator with the accessor shapes of section [6.4.13](06b-signatures-and-validation.md#6413-accessor-sites).

### 6.1 Where it runs, and components

- Compile time: inside the pipeline hook (section [9.5](09-premium-engine.md#95-compile-time-run)), on the source compilation, per syntax tree, concurrently across trees. The component is stateless and thread-safe.
- Design time: from `PipelineExtension.AnalyzeSemanticModel`, once per semantic model. The results are never stored beyond the request (DOCS27 `design-time-memory.md`).
- The analyzed node is the object that the linker injection rewriter visits later, because all code-model versions of a stage share one `PartialCompilation` (ENG26 `CodeModel\CompilationModel.cs:333`).

All components are PROPOSED and `internal` in `Metalama.Extensions.Interceptors.Engine`:

| File | Type | Responsibility |
|---|---|---|
| `CallSites/InvocationCallSiteAnalyzer.cs` | `InvocationCallSiteAnalyzer` | Entry point. Builds `InvocationCallSite` or returns a `NotACallSiteReason`. |
| `CallSites/InvocationCallSite.cs` | `InvocationCallSite` and its records | The call-site model. |
| `CallSites/MethodReferenceSiteAnalyzer.cs` | `MethodReferenceSiteAnalyzer` | Entry point for a method group. Builds a `MethodReferenceSite` or returns a `NotACallSiteReason` (section [6.2.10](#6210-method-reference-sites)). |
| `CallSites/MethodReferenceSite.cs` | `MethodReferenceSite` | The model of a method-reference site: target, receiver, converted type, kind of use, event subscription. |
| `CallSites/AccessorSiteAnalyzer.cs` | `AccessorSiteAnalyzer` | Entry point for a property or event reference. Builds an `AccessorSite` or returns a `NotACallSiteReason` (section [6.2.11](#6211-accessor-sites)). |
| `CallSites/AccessorSite.cs` | `AccessorSite` | The model of an accessor site: the member, its accessor uses, the shape, the operator, the receiver and the temporary that the receiver needs. |
| `CallSites/ReceiverVariableClassifier.cs` | `ReceiverVariableClassifier` | Classifies a receiver as writable variable, readonly variable or value, with public Roslyn APIs only. |
| `CallSites/OmittedCallDetector.cs` | `OmittedCallDetector` | Replicates the conditional-method and partial-method omission rules. |
| `CallSites/ArgumentValueMaterializer.cs` | `ArgumentValueMaterializer` | Turns the values that Roslyn bound for omitted arguments into syntax. |
| `Signatures/SignatureType.cs` | `SignatureType` | Type tree that can contain interceptor-owned type parameters. |
| `Signatures/ReceiverMappingResolver.cs` | `ReceiverMappingResolver` | Selects the receiver-mapping rule R0 to R4 for a call site and a placement or an existing method, including the speculative binding of condition R2a (section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping)). |
| `Signatures/InterceptorSignatureBuilder.cs` | `InterceptorSignatureBuilder` | Derives the signature, the lifting plan, the rewrite plan and the proceed shape. |
| `Signatures/InterceptorBuilder.cs`, `Signatures/InterceptorMethodBinder.cs`, `Signatures/InterceptorSignatureMethod.cs` | `InterceptorBuilder`, `InterceptorMethodBinder`, `InterceptorSignatureMethod` | Implement `IInterceptorBuilder` and `IInterceptorMethodBinder`, record the adjustments and the bindings of a `configure` or `bind` function, and give the lightweight read-only `IMethod` of stage 1 (sections [5.6.8](05b-api-providers-contexts-results.md#568-parameter-binding-and-the-signature-builder) to [5.6.10](05b-api-providers-contexts-results.md#5610-the-callers-instance-and-method-reference-sites)). |
| `Signatures/ArgumentPlanBuilder.cs`, `Signatures/PulledParameterGuard.cs` | `ArgumentPlanBuilder`, `PulledParameterGuard` | Compute the canonical binding, the argument plan with its temporaries and discards, and the guard of pulled parameters (sections [5.6.8](05b-api-providers-contexts-results.md#568-parameter-binding-and-the-signature-builder), [5.6.9](05b-api-providers-contexts-results.md#569-added-parameters-and-pulled-values), [6.4.10](06b-signatures-and-validation.md#6410-rewrite-plan)). |
| `Signatures/InterceptorSignatureValidator.cs` | `InterceptorSignatureValidator` | Validates an existing method, or a synthesized signature after its `configure` function, against a call site (section [6.6](06b-signatures-and-validation.md#66-signature-validation-existing-methods-and-adjusted-signatures-r9)). |
| `Placements/PlacementAdmissibilityChecker.cs` | `PlacementAdmissibilityChecker` | Checks a requested placement. |

### 6.2 The call-site model

#### 6.2.1 Entry point and order of checks

```csharp
namespace Metalama.Extensions.Interceptors.Engine.CallSites;

/// <summary>
/// Analyzes one invocation of the source compilation and builds the model that interceptor providers observe.
/// </summary>
internal static class InvocationCallSiteAnalyzer
{
    /// <summary>
    /// Returns the call-site model of <paramref name="node"/>, or the reason why the invocation is not a call site.
    /// </summary>
    /// <remarks>
    /// The method is pure and thread-safe. At design time, the result must not be stored beyond the current
    /// analysis request, because it references the semantic model and symbols.
    /// </remarks>
    public static CallSiteAnalysisResult Analyze(
        InvocationExpressionSyntax node,
        SemanticModel semanticModel,
        CallSiteAnalysisOptions options,
        CancellationToken cancellationToken );
}

/// <summary>Result of <see cref="InvocationCallSiteAnalyzer.Analyze"/>. Exactly one of the two members is meaningful.</summary>
internal readonly record struct CallSiteAnalysisResult( InvocationCallSite? CallSite, NotACallSiteReason Reason );

/// <summary>Capabilities of the other subsystems that change what the analysis accepts.</summary>
internal sealed record CallSiteAnalysisOptions(
    bool FactorySupportsScoped,                 // False in version 1 (section 10.4.4).
    bool LinkerSupportsTopLevelStatements,      // True (section 10.5.3).
    bool LinkerSupportsInitializerPositions,    // True (section 10.5.3).
    bool IncludeGeneratedFiles,                 // InterceptionScopeOptions.IncludeGeneratedFiles.
    ISymbolClassificationService SymbolClassification,
    IReadOnlySet<IFieldSymbol>? PromotedFields ); // Fields promoted by aspects; compile time only (section 6.2.3).
```

`Analyze` performs these steps in order. The first step that fails decides the result.

1. If the tree is not a user source tree (design-time generated trees, source-generator output), return `GeneratedCode`. If the tree is classified as generated code and `IncludeGeneratedFiles` is false, return `GeneratedCode` (PO12). If the enclosing type is compile-time or run-time-or-compile-time according to `ISymbolClassificationService` (ENG27 `CompileTime\ISymbolClassificationService.cs:14`), return `CompileTimeCode`. This mirrors `CanIndexSymbol` in the walker (ENG26 `ReferenceGraph\ReferenceIndexWalker.cs:882-888`).
2. `semanticModel.GetOperation(node)` must be an `IInvocationOperation` (section [6.2.2](#622-silent-refusals) lists the other kinds).
3. Target checks: method kind, then omission (section [6.2.9](#629-omitted-calls)).
4. Walk `IOperation.Parent` to the root, collect the enclosing anonymous functions and local functions, and detect expression trees (section [6.2.8](#628-enclosing-function-and-body-context)).
5. Build the target, receiver, arguments, generic context and enclosing context.
6. Compute the limitation (section [6.2.3](#623-limitations)). The first limitation found is kept.

The model is built from `IInvocationOperation`, not from `SemanticModel.GetSymbolInfo`, because of EXISTING Roslyn behavior:

- `CreateBoundCallOperation` exposes the bound method, the constrained-to type, the receiver, the virtual flag and the arguments in evaluation order (RC `Operations\CSharpOperationFactory.cs:447-476`).
- For a classic extension method, the bound method is the static form and the receiver is argument 0 (RC `Binder\Binder_Invocation.cs:1290-1336`). `GetSymbolInfo` returns the reduced form instead.
- Default values, params collections and caller-information values are already materialized as arguments of kind `DefaultValue`, `ParamArray` or `ParamCollection` (RC `Operations\CSharpOperationFactory_Methods.cs:194-384`).

#### 6.2.2 Silent refusals

| Reason | Detection (public API) | Evidence |
|---|---|---|
| `BindingError` | The operation is `IInvalidOperation` or `null`. | RC `Operations\CSharpOperationFactory.cs:455-459` |
| `Dynamic` | The operation is `IDynamicInvocationOperation`. | The walker can still index late-bound calls with one candidate (RC `Compilation\SymbolInfoFactory.cs:25-34`). |
| `NameOf` | The operation is `INameOfOperation`. | ENG26 `ReferenceGraph\ReferenceIndexWalker.cs:137-142` |
| `FunctionPointerInvocation` | The operation is `IFunctionPointerInvocationOperation`. | RC `Operations\CSharpOperationFactory.cs:478-493` |
| `DelegateInvocation` | `TargetMethod.MethodKind == MethodKind.DelegateInvoke`. | The walker records these as `Invocation` (FW27 `Code\ReferenceKinds.cs:117`). |
| `LocalFunctionInvocation` | `TargetMethod.MethodKind == MethodKind.LocalFunction`. | Local functions are not code-model declarations. |
| `ExpressionTree` | An enclosing `IAnonymousFunctionOperation` has a parent that is not `IDelegateCreationOperation`. This covers query expressions over `IQueryable`. | A lambda becomes `IDelegateCreationOperation` only when the target type is a delegate type (RC `Operations\CSharpOperationFactory.cs:1160-1165`). |
| `ConditionalCallOmitted` | Section [6.2.9](#629-omitted-calls). | RC `Symbols\MethodSymbol.cs:524-558` |
| `PartialMethodWithoutImplementation` | `IsPartialDefinition && PartialImplementationPart == null`. | RC `Symbols\Source\SourceOrdinaryMethodSymbol.cs:658-666` |
| `CompileTimeCode`, `GeneratedCode` | Step 1 of section [6.2.1](#621-entry-point-and-order-of-checks). | See section [6.2.1](#621-entry-point-and-order-of-checks). |

Method groups, delegate creations and `&M` never produce an invocation bound to `IInvocationOperation`, so they never reach this list. The method-reference analyzer handles them (section [6.2.10](#6210-method-reference-sites)).

Expression trees are refused silently. An expression tree is data for a query provider. Rewriting it changes the `MethodCallExpression` and breaks providers such as Entity Framework. A warning for every query that contains a matching method would be noise for broad scopes (PO11).

#### 6.2.3 Limitations

A limitation means that the call is a real call site but version 1 cannot rewrite it. The provider sees `context.NonInterceptableReason` and can skip silently. If it returns an interceptor, the engine reports LAMA1012 (warning) and leaves the call site unchanged.

| Limitation (public reason) | Condition | Reason |
|---|---|---|
| `RefReturn` | `TargetMethod.ReturnsByRef` or `ReturnsByRefReadonly`. | Templates cannot return by reference, and introduced methods cannot return by reference today (ENG26 `AdviceImpl\Introduction\IntroduceMethodAdvice.cs:70-80`; `CodeModel\Introductions\Builders\ParameterBuilder.cs:44-61`). |
| `PointerType` | A pointer or function-pointer type occurs in a parameter type, the return type, a type argument or the receiver type. | Synthesized members have no `unsafe` support. |
| `VariableArguments` | `TargetMethod.IsVararg`, or an `__arglist` argument exists. | The argument cannot be forwarded. |
| `UnsupportedDefaultValue` | An omitted argument must be materialized (section [6.4.4](06b-signatures-and-validation.md#644-defaults-and-params)) and its bound value is not a supported shape (section [6.7.1](06b-signatures-and-validation.md#671-rule)). | No faithful syntax exists. |
| `ScopedParameter` | `FactorySupportsScoped` is false, and the signature needs `scoped` or `[UnscopedRef]`. This is the case when (a) the receiver is passed with the `Ref` or `In` mode and the receiver type, the return type or the type of a parameter of the target is ref-like, or (b) a parameter of the target has an explicit scope or `[UnscopedRef]`, as defined below the table. | Dropping them causes ref-safety errors (CS8347, CS8350, CS8352) at the call site. |
| `UnnameableType` | An anonymous type occurs outside a liftable type-argument slot (section [6.4.6](06b-signatures-and-validation.md#646-generic-specialization-and-lifting)). | The type cannot be written and cannot be inferred. |
| `CovariantArrayElementReceiver` | The receiver is an array element whose static type is a type parameter not known to be a value type, and the passing mode would be `Ref`. | The original constrained call uses the `readonly.` prefix, which skips the array type check (RC `CodeGen\EmitAddress.cs:399-436`). A `ref` argument uses a checked `ldelema` and can throw `ArrayTypeMismatchException`. |
| `ConditionalAccessMutableReceiver` | The call is in a conditional access, the receiver is a type parameter not known to be a reference type, and the passing mode would be `Ref` or `In`. | An unconstrained type-parameter receiver is called by reference when the type argument is a value type (RC `CodeGen\EmitExpression.cs:440-500`). An extension method cannot take a receiver of an unconstrained type parameter by reference (CS8337). |
| `ReceiverReassignedByArguments` | The mode is `Ref`, the receiver is a type parameter that is not known to be a value type, and the receiver is not an uncaptured local or value parameter that no argument assigns or passes by reference. | For a reference-type type argument, the original call uses the receiver value that exists before the arguments are evaluated (RC `Lowering\LocalRewriter\LocalRewriter_Call.cs:879-885, 1074-1079`). The interceptor reads the variable through its `ref` parameter after the arguments are evaluated. |
| `PromotedFieldInitializer` | Compile time only. The call is in the initializer of a field whose `IField.OverridingProperty` is not null in `ExtensionTransformationContext.StageFinalCompilation` (FW27 `Code\IField.cs:61`). | The promoted property re-emits the initializer from a `SourceUserExpression` over the source node (ENG27 `AdviceImpl\Introduction\PromoteFieldTransformation.cs:58`), which carries no annotation, so the rewrite would be lost (LAMA0660, section [10.5.9](10b-oss-linker-and-templates.md#1059-completeness-verification)). |
| `RequiresNewerLanguageVersion` | The signature needs a feature that the project language version does not have (`ref readonly` parameters: C# 12; `allows ref struct`: C# 13). | The synthesized declaration would not compile. |
| `MethodReferenceRequiresMaterializedDefaults` | Method-reference sites only. The target uses the `Materialized` default mode (section [6.4.4](06b-signatures-and-validation.md#644-defaults-and-params)). | An interceptor without the special defaults changes the natural function type of `var d = M;`, and the site cannot append arguments (section [6.4.12](06b-signatures-and-validation.md#6412-method-reference-sites)). |
| `MethodReferenceReceiverNotSupported` | Method-reference sites only. No method-group shape of section [6.4.12](06b-signatures-and-validation.md#6412-method-reference-sites) is admissible for the receiver, and the wrapper is not possible: the site is an event subscription, the converted type cannot be named at the site (an anonymous delegate type, or a type that contains an anonymous type), or the receiver is of a ref-like type. When PO51 selects the limitation instead of the wrapper, every site that needs the wrapper gets this limitation. | A wrapper creates a new delegate at each evaluation, so `-=` would never remove the handler; a wrapper must name the delegate type; a lambda cannot capture a ref-like value. |

Below C# 13, a params collection is declared without `params`, because the call site can only use the normal form (RC `Binder\Binder_Invocation.cs:1878-1882`; rules table row 31). It therefore needs no newer language version.

`IParameterSymbol.ScopedKind` returns the effective scope (RC `Symbols\PublicModel\ParameterSymbol.cs:61`). The scope is `ScopedRef` for every `out` parameter under the updated escape rules, and `ScopedValue` for a `params` parameter of a ref-like type (RC `Symbols\Source\SourceParameterSymbol.cs:235-249`). The interceptor parameter receives the same implicit scope from its own declaration. A parameter therefore has an explicit scope only when its `ScopedKind` is not `None` and differs from this implicit scope. It needs `[UnscopedRef]` only when it has that attribute. Only these parameters, and the receiver of case (a), cause the `ScopedParameter` limitation. An `out` argument, as in `int.TryParse( s, out var value )`, does not.

A call that the input compilation already intercepts with `[InterceptsLocation]` is not a limitation. It is a conflict, reported with LAMA1011 (section [9.5.8](09-premium-engine.md#958-conflict-detection-r7-b7), RC6).

#### 6.2.4 Target and dispatch

- `TargetSymbol` is `IInvocationOperation.TargetMethod`: constructed, in static form for a classic extension method, and the extension-block member for a C# 14 extension member (RC `Binder\Binder_Invocation.cs:1237-1257`).
- `TargetDefinition` is `TargetSymbol.OriginalDefinition`, normalized to `PartialDefinitionPart ?? itself`. This matches `DeclarationFactory.GetMethod` (ENG27 `CodeModel\Factories\DeclarationFactory.Symbols.cs:215-224`).
- `MatchKeys` is a lazily computed list for target matching: the definition, every `OverriddenMethod` definition up the chain, and every interface member that the containing type implements with this method (`ContainingType.FindImplementationForInterfaceMember`).

```csharp
/// <summary>The syntactic and semantic category of the invoked method.</summary>
internal enum InvocationTargetKind
{
    Static,
    Instance,
    ClassicExtension,
    ExtensionMemberInstance,
    ExtensionMemberStatic,
    StaticVirtualThroughTypeParameter
}
```

The dispatch uses the public `InvocationDispatchKind` of section [5.5.2](05b-api-providers-contexts-results.md#552-methodinterceptioncontext-and-invocationargument). `Base` is set only when `Instance` is an `IInstanceReferenceOperation` whose syntax is `BaseExpressionSyntax` and the target is virtual, abstract or override. Roslyn gives `base` and `this` the same `InstanceReferenceKind.ContainingTypeInstance` (RC `Operations\CSharpOperationFactory.cs:1376-1392`), so the syntax decides. `IsVirtual` is false for a base call (RC `Operations\CSharpOperationFactory_Methods.cs:114-119`). A `base.M()` call to a non-virtual method is equivalent to `((Base)this).M()` and is modeled as an explicit `this` receiver whose static type is the base type (rules table row 5). It follows rule R1, R2 or R3 of section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping), like any call on `this`.

#### 6.2.5 Receiver

Receiver kinds:

| Internal kind | Public `InvocationReceiverKind` | Syntax of `node.Expression` | Operation |
|---|---|---|---|
| `None` | `None` | Any, for a static target or a static extension member. | `Instance == null`. Roslyn drops type receivers (RC `Operations\CSharpOperationFactory_Methods.cs:98-111`). |
| `TypeParameterStatic` | `TypeParameter` | `T.M()` | `ConstrainedToType != null` (RC `Operations\CSharpOperationFactory.cs:468-476`). |
| `ImplicitThis` | `This` | `IdentifierNameSyntax` or `GenericNameSyntax` | `IInstanceReferenceOperation { IsImplicit: true }` |
| `ExplicitThis` | `This` | `this.M` | `IInstanceReferenceOperation`, syntax `ThisExpressionSyntax` |
| `Base` | `Base` | `base.M` with a virtual target | `IInstanceReferenceOperation`, syntax `BaseExpressionSyntax` |
| `Expression` | `Expression` | `x.M`, and the receiver of a classic extension method or extension member | `Instance` (or argument 0 for a classic extension method) |
| `ConditionalAccess` | `Expression` (with `IsConditionalAccess`) | The leftmost expression of the receiver chain is a `MemberBindingExpressionSyntax` or an `ElementBindingExpressionSyntax` | The chain starts at `IConditionalAccessInstanceOperation`. |
| `PointerIndirection` | `Expression` | `p->M()` | The receiver is a pointer indirection. |

The receiver static type is the type of the receiver operation, after unwrapping the implicit conversion that Roslyn adds for an extension parameter (RC `Binder\Binder_Invocation.cs:1315-1318`). For `ConditionalAccess`, it is the type of `IInvocationOperation.Instance`. That operation is the `IConditionalAccessInstanceOperation`, whose type is the underlying type for `Nullable<T>`, only when the method is called directly on the conditional receiver, as in `a?.M(x)`. In `a?.B.M(x)`, the receiver is the member access `.B`.

#### 6.2.6 Variable classification without internal Roslyn APIs

Roslyn decides whether a struct receiver is copied with `ReceiverIsSubjectToCloning`, which calls the internal `CheckValueKind` (RC `Binder\Binder_Invocation.cs:1424-1436`; `Binder\Binder.ValueChecks.cs:936-1230`). The classifier replicates the receiver-relevant branches with public APIs. Each branch cites the Roslyn code it mirrors.

```csharp
/// <summary>Classification of a receiver expression of a value type or a type parameter.</summary>
internal enum ReceiverVariableKind { NotApplicable, WritableVariable, ReadOnlyVariable, Value }

internal static class ReceiverVariableClassifier
{
    /// <summary>
    /// Classifies <paramref name="receiver"/>. <paramref name="containingMemberOrLambda"/> is the innermost enclosing
    /// symbol, including lambdas and local functions, obtained with SemanticModel.GetEnclosingSymbol.
    /// </summary>
    public static ReceiverVariableKind Classify( IOperation receiver, ISymbol containingMemberOrLambda )
    {
        if ( receiver.Type is { IsReferenceType: true } )
        {
            return ReceiverVariableKind.NotApplicable;
        }

        switch ( receiver )
        {
            case ILocalReferenceOperation { Local: var local }:
                // Binder.ValueChecks.cs:1312-1343 and LocalSymbol.cs:310-325.
                return local.RefKind switch
                {
                    RefKind.Ref => ReceiverVariableKind.WritableVariable,
                    RefKind.RefReadOnly => ReceiverVariableKind.ReadOnlyVariable,
                    _ when local.IsConst || local.IsForEach || local.IsUsing || local.IsFixed => ReceiverVariableKind.ReadOnlyVariable,
                    _ => ReceiverVariableKind.WritableVariable
                };

            case IParameterReferenceOperation { Parameter: var parameter }:
                // Binder.ValueChecks.cs:1402-1450, including captured primary-constructor parameters.
                if ( parameter.RefKind is RefKind.In or RefKind.RefReadOnlyParameter )
                {
                    return ReceiverVariableKind.ReadOnlyVariable;
                }

                if ( IsCapturedPrimaryConstructorParameter( parameter, containingMemberOrLambda ) )
                {
                    var type = parameter.ContainingType;

                    return type.IsValueType && ( type.IsReadOnly || IsEffectivelyReadOnlyMember( containingMemberOrLambda ) )
                        ? ReceiverVariableKind.ReadOnlyVariable
                        : ReceiverVariableKind.WritableVariable;
                }

                return ReceiverVariableKind.WritableVariable;

            case IInstanceReferenceOperation:
                // Binder.ValueChecks.cs:1082-1105. IMethodSymbol.IsReadOnly is IsEffectivelyReadOnly
                // (PublicModel\MethodSymbol.cs:142-148, MethodSymbol.cs:380-382).
                return IsEffectivelyReadOnlyMember( containingMemberOrLambda )
                    ? ReceiverVariableKind.ReadOnlyVariable
                    : ReceiverVariableKind.WritableVariable;

            case IFieldReferenceOperation fieldReference:
                return ClassifyField( fieldReference, containingMemberOrLambda );   // Binder.ValueChecks.cs:1610-1760.

            case IArrayElementReferenceOperation arrayElement:
                // A System.Range index produces a value (Binder.ValueChecks.cs:1207-1225).
                return IsRangeIndex( arrayElement ) ? ReceiverVariableKind.Value : ReceiverVariableKind.WritableVariable;

            case IPointerIndirectionReferenceOperation:
                return ReceiverVariableKind.WritableVariable;

            case IInvocationOperation { TargetMethod: var method }:
                return FromRefKind( method.RefKind );                               // Binder.ValueChecks.cs:1950-1992.

            case IPropertyReferenceOperation { Property: var property }:
                return FromRefKind( property.RefKind );

            case IImplicitIndexerReferenceOperation implicitIndexer:
                return implicitIndexer.IndexerSymbol is IPropertySymbol indexer ? FromRefKind( indexer.RefKind ) : ReceiverVariableKind.Value;

            case IInlineArrayAccessOperation inlineArray:
                return IsRangeArgument( inlineArray ) ? ReceiverVariableKind.Value : Classify( inlineArray.Instance, containingMemberOrLambda );

            case IConditionalOperation { IsRef: true } conditional:
                return Min( Classify( conditional.WhenTrue, containingMemberOrLambda ), Classify( conditional.WhenFalse!, containingMemberOrLambda ) );

            case ISimpleAssignmentOperation { IsRef: true } refAssignment:
                return Classify( refAssignment.Target, containingMemberOrLambda );

            default:
                return ReceiverVariableKind.Value;
        }
    }

    /// <summary>Replicates CheckFieldValueKind and CanModifyReadonlyField (Binder.ValueChecks.cs:1610-1760).</summary>
    private static ReceiverVariableKind ClassifyField( IFieldReferenceOperation reference, ISymbol containing )
    {
        var field = reference.Field;

        switch ( field.RefKind )
        {
            case RefKind.Ref: return ReceiverVariableKind.WritableVariable;
            case RefKind.RefReadOnly: return ReceiverVariableKind.ReadOnlyVariable;
        }

        if ( field.IsReadOnly && !CanModifyReadOnlyField( field, receiverIsThis: reference.Instance is IInstanceReferenceOperation { Syntax: not BaseExpressionSyntax }, containing ) )
        {
            return ReceiverVariableKind.ReadOnlyVariable;
        }

        if ( field.IsStatic || field.ContainingType.IsReferenceType )
        {
            return ReceiverVariableKind.WritableVariable;
        }

        var instanceKind = Classify( reference.Instance!, containing );

        return instanceKind == ReceiverVariableKind.NotApplicable ? ReceiverVariableKind.WritableVariable : instanceKind;
    }
}
```

`CanModifyReadOnlyField` follows `CanModifyReadonlyField` exactly (RC `Binder\Binder.ValueChecks.cs:1704-1760`): same static-ness; the receiver must be `this` for an instance field; the containing types must be equal by `OriginalDefinition`; the containing symbol must be an instance constructor (a static constructor for a static field), an `init` accessor with a `this` receiver, or a field initializer. The containing symbol includes lambdas, so a readonly field used in a lambda inside a constructor is readonly.

#### 6.2.7 Arguments, generic context and passing mode

Passing modes:

```csharp
/// <summary>How the receiver is passed to a static interceptor, a local function or an instance interceptor.</summary>
internal enum ReceiverPassingMode { None, Value, In, Ref }
```

| Receiver | Target method | Mode | Parameter | Argument at call site |
|---|---|---|---|---|
| Reference type, or type parameter known to be a reference type | any | `Value` | `TR receiver` | `r` |
| Concrete struct | `IMethodSymbol.IsReadOnly` is true | `In` | `scoped in S receiver` | `r` (no modifier: an lvalue by reference, an rvalue through a temporary) |
| Concrete struct | declared in `object`, `ValueType` or `Enum`, not overridden by the struct | `Value` | `S receiver` | `r` |
| Concrete struct, writable variable | other | `Ref` | `scoped ref S receiver` | `ref r` |
| Concrete struct, readonly variable or value | other | `Value` | `S receiver` | `r` |
| Type parameter not known to be a reference type, writable variable | any | `Ref` | `scoped ref T receiver` | `ref t` |
| Type parameter, readonly variable or value | any | `Value` | `T receiver` | `t` |
| Classic extension, `this ref` | n/a | `Ref` | `ref X source` | `ref x` |
| Classic extension, `this in` | n/a | `In` | `in X source` | `x` (Roslyn treats the reduced receiver as by-value, RC `Binder\Binder_Invocation.cs:1308-1313`) |
| Classic extension, by value | n/a | `Value` | `X source` | `x` |
| Extension member | n/a | from `ExtensionParameter.RefKind`, as for classic extensions | | |
| Conditional access: `Nullable<T>` receiver, reference-type receiver, readonly variable or value | any | `Value` | `this TR receiver` | receiver stays in the chain |
| Conditional access: concrete non-generic struct receiver that is a writable variable | not readonly, not declared in `object`, `ValueType` or `Enum` | `Ref` | `this ref S receiver` | receiver stays in the chain |
| Conditional access: type-parameter receiver not known to be a reference type, when the mode would be `Ref` or `In` | any | limitation `ConditionalAccessMutableReceiver` | | |

Rationale:

- `Ref` for writable variables with a method that can mutate: mutations must reach the original variable, as they do through the implicit `this` reference of a struct method.
- `Value` for readonly variables and values with such a method: the original call already operates on a defensive copy (RC `Binder\Binder_Invocation.cs:1424-1436`). The copy is taken at the same moment, after the receiver is evaluated and before the call.
- `In` for readonly methods: the original call does not copy, and no mutation is possible. One mode for every variable kind maximizes deduplication.
- `Value` for methods of `object`, `ValueType` and `Enum` on a concrete struct: the call operates on a boxed copy.
- `scoped` on `In` and `Ref` receivers reproduces the implicit `this` of a struct, which is `scoped ref`. Without it, a span returned by the method could not escape at the call site (CS8347). An unscoped `ref` or `in` parameter contributes the ref-safe-to-escape scope of its argument to the safe-to-escape scope of a ref-like return value and to the method-arguments-must-match check, whatever its type is (RC `Binder\Binder.ValueChecks.cs:3019-3029`). The `scoped` modifier can therefore be omitted from a `Ref` or `In` receiver only when the receiver type and the return type are not ref-like and no parameter of the target has a ref-like type. For example, `return a.AsSpan();` on an `ImmutableArray<T>` parameter would become `return I( a );` with an unscoped `in` receiver, and C# would report CS8347. In version 1, `FactorySupportsScoped` is false, so every other call site gets the `ScopedParameter` limitation (section [6.2.3](#623-limitations)).

These modes apply when the receiver is a parameter (rules R1, R1x and R3 of section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping)). Under rules R2 and R4, the receiver is the `this` of the interceptor, and the mode is `None`. C# then passes a struct receiver as the original call does: a variable by reference, and a readonly variable or a value through the same defensive copy, when the interceptor is not `readonly` (RC `Binder\Binder_Invocation.cs:1424-1436`).

Arguments:

```csharp
/// <summary>One entry per parameter of the target (static form for classic extension methods), in parameter order.</summary>
internal sealed record CallSiteArgument(
    IParameterSymbol Parameter,
    ArgumentKind Kind,                  // Explicit, DefaultValue, ParamArray, ParamCollection.
    ArgumentSyntax? Syntax,             // Null for DefaultValue, for the reduced receiver of a classic extension, and for an empty expanded params.
    string? Name,                       // The NameColon of the syntax, when the argument is named.
    bool IsTargetTyped,
    CallerInfoKind CallerInfo,          // None, LineNumber, FilePath, MemberName, ArgumentExpression.
    MaterializedValue? BoundDefault );  // For DefaultValue: the value that Roslyn bound at the source call site.
```

- The syntax comes from `IArgumentOperation.Syntax` (RC `Operations\CSharpOperationFactory_Methods.cs:36-50`).
- `IsTargetTyped` is true when `semanticModel.GetTypeInfo(expr).Type` is null, or when `semanticModel.GetConversion(expr)` (RC `CSharpExtensions.cs:764`) has one of the public flags `IsInterpolatedStringHandler`, `IsCollectionExpression`, `IsObjectCreation`, `IsConditionalExpression`, `IsSwitchExpression`, `IsAnonymousFunction`, `IsMethodGroup`, `IsStackAlloc`, `IsNullLiteral`, `IsDefaultLiteral`, `IsTupleLiteralConversion` or `IsInlineArray` (RC `Binder\Semantics\Conversions\Conversion.cs:651-1049`). Only existing-method validation uses this flag.

Generic context:

- `TypeArgumentSlots`: the ordered type arguments of the target: the containing type chain from outermost to innermost, then the method type arguments. For a C# 14 extension member, the slots are the type parameters of `AssociatedExtensionImplementation` (RC `..\Core\Portable\Symbols\IMethodSymbol.cs:334`). For `TypeParameterStatic` and type-parameter receivers, one extra slot holds the receiver type parameter.
- `FreeTypeParameters`: every `ITypeParameterSymbol` that occurs in a slot, in the receiver type, or in a reachable constraint, grouped by owner.
- `AnonymousTypes`: every type with `ITypeSymbol.IsAnonymousType` in a slot or in the receiver type.
- `FileLocalTypes`: every type with `INamedTypeSymbol.IsFileLocal` anywhere in the signature. `AccessCheck` does not handle file-local types, so this list is checked separately (section [6.5.2](06b-signatures-and-validation.md#652-checks), check C12).

#### 6.2.8 Enclosing function and body context

```csharp
/// <summary>A lambda, anonymous method or local function between the call site and the body of the origin.</summary>
internal sealed record EnclosingFunction(
    EnclosingFunctionKind Kind,          // Lambda, AnonymousMethod, LocalFunction.
    IMethodSymbol Symbol,
    bool IsStatic,
    bool IsAsync,
    ImmutableArray<ITypeParameterSymbol> TypeParameters );

/// <summary>The syntactic context of the call site relative to the origin.</summary>
internal enum BodyContextKind
{
    MethodBody,
    ExpressionBody,
    FieldInitializer, PropertyInitializer, EventFieldInitializer,
    ConstructorInitializer,
    PrimaryConstructorBaseArguments,
    TopLevelStatements
}

/// <summary>Facts about the code that encloses the call site.</summary>
internal sealed record CallSiteEnclosingContext(
    INamedTypeSymbol CallingType,
    ISymbol ReferencingSymbol,           // Same rule as ReferenceIndexWalker.
    IMethodSymbol? HostMethod,           // The non-lambda method that owns the body (null for initializers and primary base arguments).
    BodyContextKind BodyKind,
    ImmutableArray<EnclosingFunction> Functions,   // Innermost first.
    bool IsThisAvailable,
    bool IsInStruct,
    bool IsReadOnlyThis,
    bool IsInUnsafeContext,
    LanguageVersion LanguageVersion,
    NullableContext NullableContext,
    ImmutableArray<IParameterSymbol> NonCapturableHostParameters,
    bool IsThisCapturableByLocalFunction );
```

- `BodyKind` comes from the first matching syntax ancestor outside lambdas and local functions. Attribute arguments, parameter defaults and enum values are constant contexts that can only contain `nameof`, which step 2 refuses.
- `IsThisAvailable` is true when the host is an instance method, the body kind is `MethodBody` or `ExpressionBody`, no enclosing function is static, and no enclosing function exists when the calling type is a struct (CS1673; static lambdas cannot reference `this`).
- `NonCapturableHostParameters` lists the parameters that a local-function placement cannot capture: `ref`, `out` and `in` parameters (CS1628) and ref-like values.
- `InterceptionContext.IsInNestedFunction` is `true` when `Functions` is not empty. `InterceptionContext.CanAccessThis` is `IsThisAvailable`, which also decides the availability of `InterceptorArgument.CallerInstance` (section [5.6.10](05b-api-providers-contexts-results.md#5610-the-callers-instance-and-method-reference-sites)).
- The guard of pulled parameters (section [5.6.9](05b-api-providers-contexts-results.md#569-added-parameters-and-pulled-values)) uses `Functions`. For a parameter `p` of the origin that a pull names, the site is refused when an element of `Functions` declares a parameter named `p`, when an element is static, or when `p` is in `NonCapturableHostParameters` and `Functions` is not empty. The engine then confirms with `SemanticModel.GetSpeculativeSymbolInfo` at the site that the name binds to `p`.

#### 6.2.9 Omitted calls

EXISTING Roslyn rule: a call is omitted when the method is conditional and none of its conditional symbols is defined in the syntax tree of the call, with inheritance through `OverriddenMethod` (RC `Symbols\MethodSymbol.cs:524-558`). A partial method without implementation is always omitted (RC `Symbols\Source\SourceOrdinaryMethodSymbol.cs:658-666`). The tree state is the final directive state of the file, falling back to the parse options (RC `Syntax\CSharpSyntaxTree.cs:170-196`).

`OmittedCallDetector` replicates it:

1. Collect the `ConditionalAttribute` strings of the target and of each overridden method.
2. A symbol is defined in a tree when the last `#define` or `#undef` for it in `root.GetDirectives()` is a `#define`. Without such a directive, it is defined when `CSharpParseOptions.PreprocessorSymbolNames` contains it.
3. The call is omitted when the set is not empty and no symbol of the set is defined in the call-site tree.

The same helper decides whether the syntax tree of a placement defines the symbol (check C11).

#### 6.2.10 Method-reference sites

A method group converted to a delegate or to a function pointer is a method-reference site (section [5.3.11](05a-api-registration.md#5311-kinds-of-method-use)). The index records its simple name with `ReferenceKinds.Default` (section [3.3](03-background.md#33-reference-index)), so the engine adds `Default` to the reference kinds of its index requirements for every method registration, with the same names as for `Invocation` (section [9.5.3](09-premium-engine.md#953-registration-index-and-index-requirements)).

```csharp
namespace Metalama.Extensions.Interceptors.Engine.CallSites;

/// <summary>
/// Analyzes one method group of the source compilation and builds the model that interceptor providers observe.
/// </summary>
internal static class MethodReferenceSiteAnalyzer
{
    /// <summary>
    /// Returns the model of the method group that contains <paramref name="referenceNode"/>, or the reason why it is not a
    /// method-reference site.
    /// </summary>
    /// <param name="referenceNode">The node recorded by the index: a simple name or a generic name.</param>
    public static MethodReferenceAnalysisResult Analyze(
        SimpleNameSyntax referenceNode,
        SemanticModel semanticModel,
        CallSiteAnalysisOptions options,
        CancellationToken cancellationToken );
}

internal readonly record struct MethodReferenceAnalysisResult( MethodReferenceSite? Site, NotACallSiteReason Reason );

internal sealed record MethodReferenceSite(
    ExpressionSyntax Node,                       // The method-group expression: M, M<T>, obj.M, C.M or base.M.
    MethodUseKind Kind,                          // DelegateCreation or FunctionPointer.
    IMethodSymbol TargetSymbol,                  // Constructed; a classic extension method in static form.
    IOperation? Receiver,                        // Null for a static target; the receiver of an extension method group otherwise.
    ReceiverVariableKind ReceiverVariableKind,   // Section 6.2.6.
    ITypeSymbol ConvertedType,                   // The delegate type or the function pointer type.
    bool IsEventSubscription,
    CallSiteLimitation? Limitation );
```

`Analyze` performs these steps in order. The first step that fails decides the result.

1. Step 1 of section [6.2.1](#621-entry-point-and-order-of-checks): generated code and compile-time code are refused silently.
2. The method-group expression is the reference node, or its parent when the node is the `Name` of a `MemberAccessExpressionSyntax`. `semanticModel.GetOperation` of that expression must be an `IMethodReferenceOperation`. It is not one when the name is the expression of an invocation, which is a call site (section [6.2.1](#621-entry-point-and-order-of-checks)), or when it is the operand of `nameof`. The reason is `NotAMethodReference`, which is silent.
3. The parent operation must be an `IDelegateCreationOperation` (kind `DelegateCreation`) or an `IAddressOfOperation` (kind `FunctionPointer`). Otherwise, the reason is `MethodGroupNotConverted`, which is silent.
4. Target checks: `MethodKind.LocalFunction` gives `LocalFunctionInvocation`, and `MethodKind.DelegateInvoke` gives `DelegateInvocation`, as for calls (section [6.2.2](#622-silent-refusals)). The analyzer takes the constructed method from `IMethodReferenceOperation.Method`. A classic extension method is normalized to its static form with `ReducedFrom` when the operation gives the reduced form, as the index does (section [10.7.3](10c-oss-reference-graph-design-time.md#1073-reducedfrom-normalization)).
5. Expression trees: the rule of section [6.2.8](#628-enclosing-function-and-body-context). A method group inside a lambda that is converted to an expression tree gives `ExpressionTree`, which is silent.
6. The receiver is `IMethodReferenceOperation.Instance`, which Roslyn creates from the receiver of the method group, including the receiver of an extension method group (RC `Operations\CSharpOperationFactory_Methods.cs:98-112`). It is classified with the rules of section [6.2.6](#626-variable-classification-without-internal-roslyn-apis). `IsVirtual` and `ConstrainedToType` give the dispatch.
7. `ConvertedType` is the type of the parent operation. `IsEventSubscription` is `true` when the parent of the `IDelegateCreationOperation` is an `IEventAssignmentOperation` whose `HandlerValue` is the delegate creation.
8. Limitations: `RefReturn`, `PointerType` (a pointer type in the signature of the target; the converted function pointer type does not count), `VariableArguments`, `ScopedParameter`, `UnnameableType`, `PromotedFieldInitializer` and `RequiresNewerLanguageVersion`, with the conditions of section [6.2.3](#623-limitations), then `MethodReferenceRequiresMaterializedDefaults`. The limitation `MethodReferenceReceiverNotSupported` is decided after the provider has answered, because it depends on the placement (section [6.4.12](06b-signatures-and-validation.md#6412-method-reference-sites)).

The analyzer does not need to know whether the converted type is the natural function type of the method group. The signature rules of section [6.4.12](06b-signatures-and-validation.md#6412-method-reference-sites) keep the conversion and the natural function type unchanged in every case.

The redirection key is the method-group expression of step 2. It is also the node that the injection rewriter replaces (section [10.5.3](10b-oss-linker-and-templates.md#1053-injection-step-and-rewriter)).

#### 6.2.11 Accessor sites

An accessor site is a use of a property or an event that calls an accessor (section [5.3.13](05a-api-registration.md#5313-accessors)). EXISTING: the walker records a property or event reference on the simple name, with the symbol that `GetSymbolInfo` returns for the name, which is the property or the event (ENG27 `ReferenceGraph\ReferenceIndexWalker.cs:103, 800-845`). The reference kind is `ReferenceKinds.Assignment` for the left operand of every `AssignmentExpressionSyntax`, which includes simple assignments, compound assignments, `??=` and `+=` and `-=` on events (lines 110-113), and the current kind otherwise, which is `ReferenceKinds.Default` for a read and for an increment or a decrement (line 35). The engine therefore adds requirements of the kinds `Default` and `Assignment` with the names of each accessor registration (section [9.5.3](09-premium-engine.md#953-registration-index-and-index-requirements)). The names filter the index before binding, as for methods.

```csharp
namespace Metalama.Extensions.Interceptors.Engine.CallSites;

/// <summary>Analyzes one property or event reference of the source compilation.</summary>
internal static class AccessorSiteAnalyzer
{
    /// <param name="referenceNode">The node recorded by the index: the simple name of the property or the event.</param>
    public static AccessorSiteAnalysisResult Analyze(
        SimpleNameSyntax referenceNode,
        SemanticModel semanticModel,
        CallSiteAnalysisOptions options,
        CancellationToken cancellationToken );
}

internal readonly record struct AccessorSiteAnalysisResult( AccessorSite? Site, NotACallSiteReason Reason );

/// <summary>The model of an accessor site. A compound site has two accessor uses.</summary>
internal sealed record AccessorSite(
    ExpressionSyntax Node,                     // The key: the member access, the assignment, the compound assignment or the increment.
    ISymbol Member,                            // The property or the event, constructed.
    AccessorSiteShape Shape,                   // Read, Write, Compound, IncrementOrDecrement, NullCoalescingAssignment, EventAdd, EventRemove.
    IMethodSymbol? Getter,                     // The accessor of the get use, or null.
    IMethodSymbol? Setter,                     // The accessor of the set, add or remove use, or null.
    OperatorKind AssignmentOperator,
    bool IsPostfix,
    bool IsChecked,
    bool IsResultUsed,
    bool IsConditionalAccess,
    IOperation? Receiver,                      // Null for a static member.
    ReceiverVariableKind ReceiverVariableKind, // Section 6.2.6.
    ReceiverTemporaryKind ReceiverTemporary,   // None, Local, RefLocal or PatternVariable.
    RewriteContextKind Context,                // Statement, DiscardedExpression or ValueExpression.
    CallSiteLimitation? Limitation );
```

`Analyze` performs these steps in order. The first step that fails decides the result.

1. Step 1 of section [6.2.1](#621-entry-point-and-order-of-checks): generated code and compile-time code are refused silently.
2. The access expression is the reference node, or its parent when the node is the `Name` of a `MemberAccessExpressionSyntax` or of a `MemberBindingExpressionSyntax`. `semanticModel.GetOperation` of that expression must be an `IPropertyReferenceOperation` or an `IEventReferenceOperation` (RC `..\Core\Portable\Generated\Operations.Generated.cs:993, 1026`). Otherwise, the reason is `NotAnAccessorUse`, which is silent. A reference in `nameof` has the kind `ReferenceKinds.NameOf`, which the engine does not request. An indexer is not matched in version 1, because the names of a registration never match an indexer (section [16.5](16-future-directions.md#165-indexers)).
3. Expression trees: the rule of section [6.2.8](#628-enclosing-function-and-body-context), silent.
4. The parent operation decides the shape. RC `..\Core\Portable\Generated\Operations.Generated.cs` gives the public interfaces: `ISimpleAssignmentOperation` (line 1477), `ICompoundAssignmentOperation` with `OperatorKind`, `IsChecked`, `OperatorMethod` and `OutConversion` (lines 1502-1535), `IEventAssignmentOperation` with `Adds` (lines 1577-1590), `IObjectOrCollectionInitializerOperation` (line 1719), `IIncrementOrDecrementOperation` with `IsPostfix` and `IsChecked` (lines 2174-2198), `IDeconstructionAssignmentOperation` (line 2248), `ICoalesceAssignmentOperation` (line 3106) and `IWithOperation` (line 3612).

| Parent operation of the reference | Shape and accessor uses | Detail |
|---|---|---|
| `ISimpleAssignmentOperation` whose `Target` is the reference | Write: set | `IsRef` true: limitation `RefReturn`. `IPropertySymbol.SetMethod` null: never presented, because the only such assignment is the assignment of a getter-only auto-property in its own constructor, which C# lowers to a write of the backing field (RC `Lowering\LocalRewriter\LocalRewriter_AssignmentOperator.cs:311-324`). An `init` setter: limitation `InitOnlySetter`, because C# lets only a constructor or an `init` accessor call it on `this` or `base` (RC `Binder\Binder.ValueChecks.cs:2046-2052, 2180-2205`, CS8852), and an interceptor is neither. |
| The same, inside an `IObjectOrCollectionInitializerOperation`, including the initializer of an `IWithOperation` | Write | Limitation `ObjectOrWithInitializer`: the receiver of the reference is an `IInstanceReferenceOperation` of kind `ImplicitReceiver`, and an initializer cannot call a method. |
| `ICompoundAssignmentOperation` | Compound: get and set | `AssignmentOperator` is the compound kind that corresponds to `OperatorKind` (for example `Add` gives `AdditionAssignment`). An instance `OperatorMethod` is a C# 14 user-defined instance compound assignment operator: limitation `InstanceCompoundOperator`. On a class type, such an operator performs no set (RC `Lowering\LocalRewriter\LocalRewriter_CompoundAssignmentOperator.cs:36-79`). On a struct type, it mutates a copy and then sets (RC `Lowering\LocalRewriter\LocalRewriter_UnaryOperator.cs:456-486`); version 1 does not rewrite this shape either. |
| `IIncrementOrDecrementOperation` | Increment or decrement: get and set | `AssignmentOperator` is `Increment` or `Decrement`, and `IsPostfix` is set. An instance `OperatorMethod`: limitation `InstanceCompoundOperator`. |
| `ICoalesceAssignmentOperation` | `??=`: get, and set when the value is null | `AssignmentOperator` is `NullCoalescingAssignment`. |
| `IDeconstructionAssignmentOperation` (the reference is an element of the target tuple) | Write | Limitation `DeconstructionTarget`. |
| `IEventAssignmentOperation` whose `EventReference` is the reference | Add when `Adds` is true, remove otherwise | `AssignmentOperator` is `None`. |
| Any other parent of an `IEventReferenceOperation` | None | Never presented: outside `+=` and `-=`, a field-like event can be used only inside its declaring type, where it denotes the backing field. |
| Any other parent of an `IPropertyReferenceOperation` | Read: get | A `ref`-returning property is presented with `RefReturn` for every shape. |

5. Conditional access. `r?.P` is a read, and `r?.P = v` is a write, which C# 14 allows as a null-conditional assignment (RC `Binder\Binder_Expressions.cs:12344-12348`). The same check covers compound assignments and `??=` in a conditional access. A compound or `??=` site in a conditional access is rewritten in a statement context only; when its value is used, it gets the limitation `ReceiverTemporaryNotPossible`.
6. The receiver is `Instance` of the reference operation, classified with the rules of section [6.2.6](#626-variable-classification-without-internal-roslyn-apis). For a C# 14 extension property, the receiver is the receiver of the extension access, and the accessor is the member of the extension block, whose implementation method `IMethodSymbol.AssociatedExtensionImplementation` gives the static form (RC `..\Core\Portable\Symbols\IMethodSymbol.cs:334`). C# 14 has no extension events (RC `Symbols\Source\SourceMemberContainerSymbol.cs:4840-4880`).
7. Context. `IsResultUsed` is false when the site is the expression of an `ExpressionStatementSyntax`, an element of the initializer or incrementor list of a `for` statement, or the expression body of a member, lambda or local function that returns `void`, as in section [10.5.6](10b-oss-linker-and-templates.md#1056-syntax-of-the-rewritten-call). The context is `Statement` for an expression statement, `DiscardedExpression` for the other positions whose value is not used, and `ValueExpression` otherwise.
8. Temporary. A site with one accessor use needs no temporary, because the rewrite evaluates the receiver once. A compound, increment, decrement or `??=` site needs the receiver twice. No temporary is needed when the receiver is `this`, `base`, a type, a constant, a readonly field of `this` outside a constructor, or a local or a value parameter that the right operand neither assigns nor passes by reference and that no lambda or local function of the right operand captures. Otherwise, the temporary is a local in a statement context, a `ref` local for a struct receiver that is a variable, and a pattern variable in an expression context. A struct receiver that is a variable and needs a temporary in an expression context gets `ReceiverTemporaryNotPossible`, because an expression cannot declare a `ref` local. The same limitation applies in a statement context where C# does not allow a `ref` local: in an async method or an iterator before C# 13, and across an `await` or a `yield return`.
9. The limitations of section [6.2.3](#623-limitations) apply to the accessor: `PointerType`, `UnnameableType`, `ScopedParameter` for a ref-like property type with a `Ref` or `In` receiver, `PromotedFieldInitializer`, and `RequiresNewerLanguageVersion`.

Each accessor use of a site is matched separately against the registrations whose accessor kind equals the kind of the use (section [9.5.5](09-premium-engine.md#955-target-matching)). A compound site can therefore present its get use to one provider and its set use to another, and a use that matches no registration stays a plain access.

Verification of the expression forms. A test program compiled with the .NET 10 SDK (C# 14, nullable annotations enabled) confirmed on 2026-09-25 that the pattern-variable forms of section [6.4.13](06b-signatures-and-validation.md#6413-accessor-sites) compile without warning for `int`, `byte` with the explicit conversion of the compound operator, a nullable value type, a reference type, an unconstrained type parameter, a type parameter with `allows ref struct` and a ref struct property type, and in a field initializer, a constructor initializer, a `for` incrementor (with a discard assignment), a query clause, a lambda and an expression-bodied member. No position of this list refuses pattern variables: the errors that once refused expression variables in field initializers, constructor initializers and query clauses are no longer reported (RC `Errors\ErrorCode.cs:1467-1468`, commented out). Attribute arguments and parameter default values are constant expressions and contain no accessor site that the engine rewrites.

### 6.3 Rules table

Placement codes:

- `S`: a static method in an admissible type (an existing source type, a type introduced by an aspect, the calling type, or the generated static class). Rules R0 and R1 of section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping).
- `X`: a static method in extension form (`this` receiver) in a top-level non-generic static class whose extension methods are in scope at the call site, and whose final name passes the lookup check C5 at every call site of the group. The generated static class of section [5.6.3](05b-api-providers-contexts-results.md#563-interceptorplacement) is in the global namespace, so its extension methods are in scope everywhere, but the check C5 still applies. Rule R1x.
- `V`: an instance method in the static type of the receiver or in one of its source base types, called on the receiver: `r.I(a)`, `this.I(a)` or `r?.I(a)`. Rule R2, with its conditions R2a to R2c.
- `H`: an instance method in the calling type or in a source base type, called on `this`, with the receiver as first parameter. Rule R3, and rule R0 with an instance method.
- `C`: an instance method in the calling type only. Rule R4.
- `L`: a local function in the origin.

`InterceptorPlacement.BaseMostAccessibleType()` adds no code. The engine resolves it to a type before it applies the rules of this table, and the resolved type then gives the code `S`, `V`, `H` or `C` (section [6.5.6](06b-signatures-and-validation.md#656-base-most-accessible-type), RC61).

Every placement is also subject to the checks of section [6.5](06b-signatures-and-validation.md#65-placement-admissibility). In the signature column, `P a` is the list of target parameters and `R` the target return type. The signature column shows the default derivation. A `configure` function can adjust it within the limits of section [5.6.8](05b-api-providers-contexts-results.md#568-parameter-binding-and-the-signature-builder).

| # | Call-site shape | v1 | Interceptor signature | Rewritten call site | Proceed expression | Placements | Rule and evidence |
|---|---|---|---|---|---|---|---|
| 1 | Static method `C.M(a)` or `M(a)` | Yes | `static R I(P a)` | `Cn.I(a)` | `global::C.M(a)` | S, H, L | No receiver operation for a type receiver (RC `Operations\CSharpOperationFactory_Methods.cs:98-111`). |
| 2 | Instance method, reference-type receiver `r.M(a)` | Yes | `static R I([NotNull] TC receiver, P a)`, TC = target containing type; `[NotNull]` only when available (section [6.4.5](06b-signatures-and-validation.md#645-attributes)). V: instance `R I(P a)` in the receiver's hierarchy | `Cn.I(r, a)`; H: `this.I(r, a)`; V: `r.I(a)` | `receiver.M(a)`; V: `this.M(a)` | S, X, V, H, L | The receiver is evaluated before the arguments; virtual dispatch is kept by the proceed call. `[NotNull]` keeps the not-null state that a dereference gives. V requires conditions R2a to R2c (section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping)). |
| 3 | `this.M(a)` or implicit `M(a)` in a class | Yes | as row 2 | `Cn.I(this, a)`; H: `this.I(this, a)`; V: `this.I(a)`; L: `I(a)` | `receiver.M(a)`; V and L: `this.M(a)` | S, X, V, H, L | Implicit `this` is materialized. V applies when the placement is the calling type or a base type in which `this.M(a)` binds to the target (R2a); otherwise H (section [6.4.3](06b-signatures-and-validation.md#643-worked-examples-of-the-receiver-mapping), example 7). |
| 4 | `base.M(a)`, virtual target | Yes | instance `R I(P a)` without receiver parameter | `this.I(a)`; L: `I(a)` | `base.M(a)` | C, L (L only in a class) | Rule R4. `base` binds relative to the enclosing class, and the call is non-virtual (RC `Operations\CSharpOperationFactory_Methods.cs:114-119`). A static interceptor or one in a base type would dispatch virtually and could recurse. |
| 5 | `base.M(a)`, non-virtual target | Yes | as row 2 with TC = target containing type | `Cn.I(this, a)`; V: `this.I(a)` | `receiver.M(a)`; V: `this.M(a)` | S, V, H, L | A non-virtual call through `base` equals a call through `(Base)this`, so the receiver type is the base type, and rules R1, R2 and R3 apply as for any call on `this`. |
| 6 | Struct receiver, writable variable, method not readonly | Yes | `static R I(scoped ref S receiver, P a)`; V: instance `R I(P a)` in `S` | `Cn.I(ref r, a)`; V: `r.I(a)` | `receiver.M(a)`; V: `this.M(a)` | S, V, H, L | Mutations must reach the variable (RC `Binder\Binder.ValueChecks.cs:1312-1343, 1402-1450, 1610-1760`). Under V, C# passes `r` by reference as the `this` of `I`, as for the original call. |
| 7 | Struct receiver, readonly variable, method not readonly | Yes | `static R I(S receiver, P a)`; V: instance `R I(P a)`, not `readonly` | `Cn.I(r, a)`; V: `r.I(a)` | `receiver.M(a)`; V: `this.M(a)` | S, V, H, L | The original operates on a defensive copy (RC `Binder\Binder_Invocation.cs:1424-1436`). Under V, `r.I(a)` takes the same copy. |
| 8 | Struct receiver, value (call result, property, conversion) | Yes | as row 7 | `Cn.I(r, a)`; V: `r.I(a)` | `receiver.M(a)`; V: `this.M(a)` | S, V, H, L | Same as row 7. |
| 9 | Struct receiver, readonly method | Yes | `static R I(scoped in S receiver, P a)`; V: instance `readonly R I(P a)` | `Cn.I(r, a)`; V: `r.I(a)` | `receiver.M(a)`; V: `this.M(a)` | S, V, H, L | `IMethodSymbol.IsReadOnly` is `IsEffectivelyReadOnly` (RC `Symbols\PublicModel\MethodSymbol.cs:142-148`). No copy in either form, because `I` is `readonly` when `M` is. |
| 10 | Struct receiver, method of `object`, `ValueType` or `Enum` | Yes | `static R I(S receiver, P a)` | `Cn.I(r, a)` | `receiver.M(a)` | S, H, L | The call operates on a boxed copy. V does not apply, because the target is not declared in the struct's hierarchy of source types. |
| 11 | `this` in a struct member | Yes | rows 6, 7 or 9 | `Cn.I(ref this, a)` (member not readonly), `Cn.I(this, a)` (readonly member or method); V: `this.I(a)` | `receiver.M(a)`; V: `this.M(a)` | S, V, H, L | `this` is a variable in struct instance members and readonly in readonly members (RC `Binder\Binder.ValueChecks.cs:1082-1105`). Under V, `I` is `readonly` exactly when `M` is (condition R2c), so a readonly host member copies `this` for `I` exactly when it copied `this` for `M`, with the warning CS8656 in both cases (RC `Errors\ErrorCode.cs:1706`). |
| 12 | Type-parameter receiver `t.M()`, T not known to be a reference type | Yes | `static R I<T1>(scoped ref T1 receiver, P a) where T1 : <copied>`, or non-generic when T is visible at the placement | `Cn.I<T>(ref t, a)` | `receiver.M(a)` | S, H, L | Keeps the constrained call. A non-generic parameter would box and lose mutations. V is not used for a type-parameter receiver in version 1 (section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping)). |
| 13 | Type-parameter receiver, T known to be a reference type | Yes | as row 2, TC = target containing type | `Cn.I(t, a)` | `receiver.M(a)` | S, X, H, L | Implicit reference conversion from T. V is not used in version 1. |
| 14 | Type-parameter receiver that is an array element, T not known to be a value type, `Ref` mode | No: `CovariantArrayElementReceiver` | | | | | The original uses `readonly. ldelema` (RC `CodeGen\EmitAddress.cs:399-436`). |
| 15 | Static abstract or static virtual member `T.M(a)` | Yes | `static R I<T1>(P a) where T1 : <copied closure>`, or non-generic when T is visible | `Cn.I<T>(a)` | `T1.M(a)` | S, H, L | `ConstrainedToType` is set (RC `Operations\CSharpOperationFactory.cs:468-476`). Type arguments are always explicit. |
| 16 | Classic extension, reduced form `x.Ext(a)` | Yes | `static R I(X source, A a)`, parameters of the static form | `Cn.I(x, a)` | `global::E.Ext(source, a)` | S, X, H, L | The operation exposes the static form with the receiver as argument 0 (RC `Binder\Binder_Invocation.cs:1290-1336`). |
| 17 | Classic extension, static form `E.Ext(x, a)` | Yes | same as row 16 (same key) | `Cn.I(x, a)` with original names | same | S, X, H, L | Same target, same shape. |
| 18 | Classic extension with `this ref` or `this in` | Yes | `ref X source` or `in X source` | `Cn.I(ref x, a)` or `Cn.I(x, a)` | `global::E.Ext(ref source, a)` or `(source, a)` | S, X (X only for concrete structs), H, L | `ref` receivers are checked as `RefOrOut`; `in` receivers are by value (RC `Binder\Binder_Invocation.cs:1296-1313`). |
| 19 | C# 14 extension member, instance `x.M(a)` | Yes | parameters of `AssociatedExtensionImplementation` (receiver first) | `Cn.I(x, a)` | `global::E.M(receiver, a)` | S, X, H, L | The bound call keeps the receiver (RC `Binder\Binder_Invocation.cs:1237-1257`). Metalama already invokes extension members through the implementation method (ENG26 `CodeModel\Invokers\MethodInvoker.cs:127-129, 286-321`). |
| 20 | C# 14 extension member, static `int.Parse2(s)` | Yes | `static R I(P a)` | `Cn.I(a)` | `global::E.Parse2(a)` | S, H, L | Implementation method in static form. |
| 21 | Generic method, inferred type arguments | Yes | specialized `static R I(P' a)` when every slot is nameable; lifted otherwise (section [6.4.6](06b-signatures-and-validation.md#646-generic-specialization-and-lifting)) | `Cn.I(x)` or `Cn.I<U>(x)` | `global::C.M<int>(a)` or `M<T1>(a)` | S, H, L | The proceed call always passes explicit type arguments. |
| 22 | Generic method, explicit type arguments `M<int>(x)` | Yes | as row 21 | as row 21 | as row 21 | S, H, L | Requires the `VisitGenericName` fix (section [10.7.2](10c-oss-reference-graph-design-time.md#1072-proposed-walker-code)). |
| 23 | Free type parameters of the calling type or method in the signature | Yes | Type parameters of the calling type: not lifted in the calling type, in a type nested in it, or in a local function. Type parameters of the calling method: not lifted in a local function; lifted in every type placement. Other placements: lifted. | `Cn.I<T>(x)` | `M<T1>(a)` | S, H, L | Section [6.4.6](06b-signatures-and-validation.md#646-generic-specialization-and-lifting). |
| 24 | Anonymous types in a type-argument slot | Yes | all method-level slots lifted | `Cn.I(x)` (type arguments omitted) | `M<T1, T2>(a)` | S, H, L | Section [6.4.6](06b-signatures-and-validation.md#646-generic-specialization-and-lifting): the inference problem equals the original one. Anonymous types elsewhere: `UnnameableType`. |
| 25 | Private or protected nested types | Yes | in a slot: lifted when the placement cannot see the type; elsewhere: the placement must see it | `Cn.I<P>(x)` | as row 21 | placements that see the types | `Compilation.IsSymbolAccessibleWithin` (RC `..\Core\Portable\Compilation\Compilation.cs:1647-1672`). |
| 26 | File-local types | Yes | as row 25 | as row 25 | as row 25 | a file-local type (or a type nested in one) declared in the call-site file, or L | File-local visibility is a lookup rule outside `AccessCheck`. C# rejects a file-local type in the signature of a member of a type that is not file-local (CS9051, check C12). |
| 27 | Private or protected target | Yes | as row 1 or 2; protected instance targets use the placement type as receiver type | as row 1 or 2 | as row 1 or 2 | placements that can access the target through the receiver type | CS1540 through the `throughType` argument of `IsSymbolAccessibleWithin`. |
| 28 | `ref`, `out`, `in`, `ref readonly` parameters | Yes | same `RefKind` and identical type | original argument syntax (`ref x`, `out var y`, `in z`, or no modifier) | `ref p`, `out p`, `in p` (also for `ref readonly`) | all | Ref kinds require identity. `out var` keeps its scope because the invocation stays in place. |
| 29 | `scoped` parameters and `[UnscopedRef]` | No in v1: `ScopedParameter` (yes when the factory supports it) | copy the explicit scope defined in section [6.2.3](#623-limitations) and `[UnscopedRef]` | unchanged | unchanged | all | Otherwise CS8350 or CS8352 can appear at the call site. The implicit scope of `out` parameters and of ref-like `params` parameters needs no copy. |
| 30 | `params` array, expanded, empty or normal | Yes | `params T[] values` | unchanged; `M()` stays `I()` | `M(values)` | all | The normal form binds when an array is passed. An empty expanded form stays empty, so the compiler creates the same empty array (section [6.4.4](06b-signatures-and-validation.md#644-defaults-and-params), rule b). |
| 31 | params collection (`ReadOnlySpan<T>`, `List<T>`, a type with `CollectionBuilderAttribute`) | Yes | `params ReadOnlySpan<T> values` (C# 13 or later; below C# 13, without `params`) | unchanged | `M(values)` | all | `params ReadOnlySpan<T>` is implicitly scoped on both sides. A template must not store the collection beyond the call (section [6.4.4](06b-signatures-and-validation.md#644-defaults-and-params), rule h). |
| 32 | Optional parameters with constant defaults | Yes | defaults copied (Declared mode); enumeration, `decimal` and lifted-type defaults as in section [6.4.4](06b-signatures-and-validation.md#644-defaults-and-params), rule d | omitted arguments stay omitted | all parameters passed | all | Section [6.4.4](06b-signatures-and-validation.md#644-defaults-and-params). |
| 33 | Optional parameters with special defaults (`[Optional]` without value, `[DateTimeConstant]`, `[IUnknownConstant]`, `[IDispatchConstant]`) | Yes | no defaults (Materialized mode) | every omitted argument appended as a named argument | all parameters passed | all | Section [6.4.4](06b-signatures-and-validation.md#644-defaults-and-params). `[DateTimeConstant]` always selects the Materialized mode. |
| 34 | Caller-information parameters | Yes | parameters kept, caller attributes removed | omitted values appended as named literals read from the source call | parameter passed explicitly | all | RC `Binder\Binder_Invocation.cs:1612-1621, 1716-1740`; section [6.7](06b-signatures-and-validation.md#67-caller-information-materialization). |
| 35 | Named and out-of-order arguments | Yes | target parameter names and order | original `ArgumentSyntax` list; receiver prepended positionally | positional, in parameter order | all | Arguments are evaluated in textual order; a prepended positional argument keeps non-trailing named arguments in position. |
| 36 | Interpolated-string handler arguments | Yes | handler parameter with the same ref kind and type; `[InterpolatedStringHandlerArgument]` copied, `""` remapped to the receiver name | `Cn.I(sb, $"...")` | `receiver.Append(ref handler)` | all | `""` denotes the instance receiver (RC `Symbols\Source\SourceComplexParameterSymbol.cs:1384-1409`). The handler is built at the rewritten call, like the original. |
| 37 | Ref returns | No: `RefReturn` | | | | | Section [6.2.3](#623-limitations). |
| 38 | Ref struct receiver or `allows ref struct` | Yes when no `scoped` is required; otherwise `ScopedParameter` | rows 6 to 12; lifted type parameters copy `AllowsRefLikeType` | as rows 6 to 12 | as rows 6 to 12 | all (L cannot capture ref-like host parameters) | `this` of a struct is `scoped ref`. |
| 39 | Nullable annotations and flow attributes | Yes | constructed nullability kept; flow attributes copied (section [6.4.5](06b-signatures-and-validation.md#645-attributes)) | unchanged | unchanged | all | Otherwise `TryGetValue`-style call sites get new warnings. |
| 40 | `dynamic` invocation | No: silent (`Dynamic`) | | | | | Section [6.2.2](#622-silent-refusals). |
| 41 | Statically bound call with `dynamic` parameters | Yes | `dynamic` parameters kept | unchanged | `M((object)p)` for each `dynamic` parameter | all | A `dynamic` argument would make the proceed call late-bound. |
| 42 | Conditional access `a?.M(x)`, `a?.B.M(x)`, `a?[i].M(x)` | Yes | `static R I(this TR receiver, P x)`; V: instance `R I(P x)` in the receiver's hierarchy | method name replaced in the chain: `a?.I(x)`, `a?.B.I(x)` | `receiver.M(x)`; V: `this.M(x)` | X, V | Keeps the single evaluation of `a` and the short-circuit of `x`. The receiver is classified like any other receiver (section [6.2.6](#626-variable-classification-without-internal-roslyn-apis)). A `Nullable<T>` receiver is a copy and uses the `Value` mode (RC25). A writable struct variable in the chain, such as the field in `a?.B.M(x)` or the array element in `a?[i].M(x)`, uses `this ref S receiver`. A type-parameter receiver that would need `Ref` or `In` gets `ConditionalAccessMutableReceiver`. Under V, the method name is replaced in the chain in the same way, and the conditional access passes the receiver as the `this` of `I`, as it does for `M`. |
| 43 | Virtual dispatch, target bound to an override | Yes | as row 2 | as row 2 | `receiver.M(a)` or, under V, `this.M(a)` (virtual) | S, X, V, H, L | The target is the statically bound symbol (RC `CodeGen\EmitExpression.cs:1999-2008`). |
| 44 | Interface and default interface methods | Yes | as row 2, TC = interface | as row 2 | `receiver.M(a)` | S, X, H, L | The receiver type is the interface. |
| 45 | `[Conditional]`, call omitted | No: silent | | | | | The call and its argument evaluation do not exist. |
| 46 | `[Conditional]`, call present | Yes | no `[Conditional]` on the interceptor | unchanged | `M(a)` | placements whose tree defines a conditional symbol | Check C11. |
| 47 | Partial method without implementation | No: silent | | | | | RC `Symbols\Source\SourceOrdinaryMethodSymbol.cs:658-666`. |
| 48 | Expression trees, `IQueryable` query expressions | No: silent | | | | | Section [6.2.2](#622-silent-refusals). |
| 49 | Invocations written inside a query expression over `IEnumerable`. The query operators that the clauses call are not call sites (row 65). | Yes | per row | unchanged inside the implicit lambda | per row | S, X, V, H (not in static lambdas), L | Implicit lambdas are delegates. |
| 50 | Method group that is not converted, `nameof`, delegate invocation, local-function call, function-pointer invocation | No: silent | | | | | Section [6.2.2](#622-silent-refusals). A method group converted to a delegate or to a function pointer is a method-reference site of version 1 (rows 69 to 77, section [6.2.10](#6210-method-reference-sites)). |
| 51 | Pointer or function-pointer types in the signature | No: `PointerType` | | | | | Section [6.2.3](#623-limitations). |
| 52 | `p->M()` receiver | Yes | row 6 | `Cn.I(ref *p, a)` | `receiver.M(a)` | S, H, L | A pointer indirection is a writable variable (RC `Binder\Binder.ValueChecks.cs:1041-1060`). The call site is already in an unsafe context. |
| 53 | Unsafe context without pointer types in the signature | Yes | per row | per row | per row | all | No special handling. |
| 54 | Calls in static constructors, and type-initialization timing | Yes | per row | per row | per row | all | A placement other than the calling type that has static state is initialized at a new moment. The generated static class has no static state. Documented. |
| 55 | Field, property and event initializers | Yes | per row | per row | per row | S, X, V | No `this` and no body. The initializer of a field that an aspect promotes to a property gets `PromotedFieldInitializer` (section [6.2.3](#623-limitations)). |
| 56 | Constructor initializer, primary-constructor base arguments | Yes | per row | per row | per row | S, X, V | Local functions of the body are not in scope in the initializer. |
| 57 | Top-level statements | Yes | per row | per row | per row | S, X, V (not the calling type, which is implicit; not L in v1) | The host is the static `<Main>$`. The injection rewriter visits global statements (section [10.5.3](10b-oss-linker-and-templates.md#1053-injection-step-and-rewriter)). |
| 58 | Expression-bodied member | Yes | per row | per row | per row | all; L converts the body to a block | Section [10.5.4](10b-oss-linker-and-templates.md#1054-local-function-injection). |
| 59 | Call inside a lambda or local function | Yes | per row | per row | per row | S, X, V; H when `this` is capturable; L when no enclosing function is static | Section [6.2.8](#628-enclosing-function-and-body-context). |
| 60 | Varargs, `__arglist` | No: `VariableArguments` | | | | | |
| 61 | Call already intercepted by `[InterceptsLocation]` | Error LAMA1011 when a non-skip result remains | | | | | Section [9.5.8](09-premium-engine.md#958-conflict-detection-r7-b7). |
| 62 | `[Obsolete]` or `[Experimental]` target | Yes | attribute copied to the interceptor | unchanged | unchanged | all | Keeps the diagnostic at the call site with the same identifier (section [6.4.5](06b-signatures-and-validation.md#645-attributes)). |
| 63 | Compile-time code, generated trees | No: silent | | | | | Section [6.2.1](#621-entry-point-and-order-of-checks). |
| 64 | Binding errors | No: silent | | | | | |
| 65 | Calls without invocation syntax: `Add` in collection initializers and collection expressions, `GetEnumerator`, `MoveNext` and `Dispose` in `foreach`, `Dispose` in `using`, `Deconstruct`, methods called by query clauses, `GetAwaiter`, `GetPinnableReference`, `Slice` in range access, and the `Append` methods of interpolated string handlers | No: never presented | | | | | A call site is an `InvocationExpressionSyntax` (section [0.3](00-conventions.md#03-terms)). The documentation states that a registration on these methods does not intercept these calls. |
| 66 | Receiver in the hierarchy of the placement type, but `this.M(a)` does not bind to the target inside the placement: `M` is declared only in a type derived from the placement type, or a member of the placement type or of a type between the placement type and the target hides `M` | Yes | H (rule R3) when the calling type is in the hierarchy of the placement type and `this` is available and the receiver type can be named in the placement; otherwise S (rule R1) | `this.I(r, a)` or `Cn.I(r, a)` | `receiver.M(a)` | S, X, H, L; not V | Condition R2a of section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping), which addresses review finding CS-06. Section [6.4.3](06b-signatures-and-validation.md#643-worked-examples-of-the-receiver-mapping), example 7. |
| 67 | Named argument for a `params` parameter in expanded form, with an omitted optional parameter before it: `M( "a", args: 1 )` for `M( string a, string n = "", params object[] args )` | Yes | per rows 30 and 32 | unchanged; appended named arguments follow the named `params` argument: `I( "a", args: 1, n: "C" )` | `M(a, n, args)` | all | Roslyn lets a named argument correspond to the `params` parameter in expanded form, with exactly one element (RC `Binder\Semantics\OverloadResolution\OverloadResolution_ArgsToParameters.cs:283-288, 306-339`). Named arguments may follow it (lines 201-237). Section [6.4.4](06b-signatures-and-validation.md#644-defaults-and-params), rule c. |
| 68 | `params` parameter used in normal form, `M( array )`, with an existing method whose parameter is not `params` | Yes | existing method only | `E( array )` | not applicable | existing methods | E10 accepts the implicit conversion of the array; E11 applies only to the expanded form (section [6.6](06b-signatures-and-validation.md#66-signature-validation-existing-methods-and-adjusted-signatures-r9)). |
| 69 | Method group of a static target converted to a delegate: `list.Select( C.M )`, `Action a = M;`, `var d = M;` | Yes | `static R I(P a)`, identical to the target (section [6.4.12](06b-signatures-and-validation.md#6412-method-reference-sites)) | `Cn.I`; L: `I`; H: `this.I` | `global::C.M(a)` | S, H, L | Rule R0. Method-group conversion rules: RC `Binder\Binder_Conversions.cs:3516-3659`. Under H, `Delegate.Target` becomes the caller instead of `null`. |
| 70 | Static target converted to a function pointer: `&C.M` | Yes | `static R I(P a)`, identical to the target | `&Cn.I` | `global::C.M(a)` | S | C# converts only static methods, and no reduced extension method, to a function pointer (RC `Binder\Binder_Conversions.cs:3612-3630`). Local functions are not used for this kind in version 1. |
| 71 | Instance target, receiver in the hierarchy of the placement: `list.Select( formatter.Format )`, `button.Click += this.OnClick` | Yes | instance `R I(P a)` in the receiver's hierarchy | `r.I`; implicit `this`: `this.I` | `this.M(a)` | V | Rule R2, with conditions R2a to R2c. The receiver is evaluated when the delegate is created and becomes `Delegate.Target`, as in the original. |
| 72 | Instance target, reference-type receiver, R2 not admissible | Yes | `static R I(this TC receiver, P a)` (R1x) | `r.I` | `receiver.M(a)` | X | C# creates a delegate closed over the first argument of an extension method whose `this` parameter has a reference type (CS1113, RC `Binder\Semantics\Conversions\Conversions.cs:340-372`). A null receiver no longer throws when the delegate is created (section [6.4.12](06b-signatures-and-validation.md#6412-method-reference-sites)). |
| 73 | Receiver is the caller's `this` in a class, local-function placement | Yes | local function `R I(P a)`, which captures `this` | `I` | `this.M(a)` | L | Section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping), local functions. |
| 74 | Instance target in any other shape: R1, R3, a value-type receiver without R2 | Yes, with the wrapper of PO51 | as rows 2 to 11 | `((global::System.Func<TR, D>) ( static receiver => ( P a ) => Cn.I( receiver, a ) ))( r )` | `receiver.M(a)` | S, H, L | Section [6.4.12](06b-signatures-and-validation.md#6412-method-reference-sites). Not for an event subscription (`DelegateEqualityRequired`), nor for a converted type that cannot be named or a ref-like receiver (`MethodReferenceReceiverNotSupported`). |
| 75 | Classic extension method group `x.Ext` | Yes | `static R I(this X source, P a)` (R1x) when the type of `source` is a reference type; otherwise the wrapper of row 74 | `x.I`, or the wrapper | `global::E.Ext(source, a)` | X; for the wrapper S, H, L | CS1113, as row 72. |
| 76 | Method group in an expression tree, in `nameof`, not converted, of a local function, or of `Invoke` of a delegate type | No: silent | | | | | Section [6.2.10](#6210-method-reference-sites). |
| 77 | Method-reference site whose target needs the `Materialized` mode | No: `MethodReferenceRequiresMaterializedDefaults` | | | | | Section [6.4.12](06b-signatures-and-validation.md#6412-method-reference-sites). |
| 77a | Method-reference site whose binding is not canonical, whose group has added parameters, or whose signature the builder changed beyond the name, the accessibility and the receiver mapping | Yes, with the lambda wrapper (RC69) | the signature of the group | `( P a ) => Cn.I( a, x: v )`, or the wrapper of row 74 when a receiver must be evaluated at delegate creation | as for a call | S, H, L | Section [5.6.10](05b-api-providers-contexts-results.md#5610-the-callers-instance-and-method-reference-sites). Pulled values and caller information are evaluated at each invocation. For an event subscription: `DelegateEqualityRequired`; for a function pointer: `MethodReferenceReceiverNotSupported`. |
| 78 | Static property read `C.P`, static event `C.E += h` | Yes | `static T I()`; `static void I(THandler handler)` | `Cn.I()`; `Cn.I(h)` | `global::C.P`; `global::C.E += handler` | S, H, L | Rule R0 (section [6.4.13](06b-signatures-and-validation.md#6413-accessor-sites)). |
| 79 | Property read, reference-type receiver `r.P` | Yes | `static T I([NotNull] TC receiver)`; V: instance `T I()` | `Cn.I(r)`; H: `this.I(r)`; V: `r.I()` | `receiver.P`; V: `this.P` | S, X, V, H, L | As row 2. Under V, condition R2a binds `this.P` speculatively in the placement. |
| 80 | Property write whose value is not used, `r.P = v;` | Yes | `static T I(TC receiver, T value)`; V: instance `T I(T value)` | `Cn.I(r, v)`; V: `r.I(v)` | `receiver.P = value`, returned | S, X, V, H, L | The setter interceptor returns the assigned value (RC49). An existing `void` setter is admissible here (E19). |
| 81 | Property write whose value is used, `a = r.P = v`, `F( r.P = v )` | Yes | as row 80 | `a = Cn.I(r, v)` | as row 80 | S, X, V, H, L | The value of a simple assignment is the right operand converted to the property type, which the setter interceptor returns. An existing `void` setter is refused (E19). |
| 82 | Compound assignment `r.P op= v` in a statement | Yes | rows 79 and 80 | `{ var t = r; Is(t, Ig(t) op v); }`; without temporary (section [6.2.11](#6211-accessor-sites), step 8): `Is(r, Ig(r) op v);` | getter and setter proceeds | S, X, V, H, L | Evaluation order kept: receiver, get, right operand, operator, set. The explicit conversion of `ICompoundAssignmentOperation.OutConversion` is written when it is not an identity, for example `(byte)(Ig(t) + 1)`. |
| 83 | Compound assignment whose value is used | Yes | as row 82 | `x = r is var t ? Is(t, Ig(t) op v) : default!` | as row 82 | S, X, V, H, L | Pattern variables evaluate each subexpression once; the `default!` branch is unreachable (section [6.4.13](06b-signatures-and-validation.md#6413-accessor-sites)). In a position that accepts only a statement expression, the form is preceded by `_ = ` (section [10.5.6](10b-oss-linker-and-templates.md#1056-syntax-of-the-rewritten-call)). |
| 84 | `++` and `--` in prefix form, and in postfix form when the value is not used | Yes | as row 82 | as rows 82 and 83, with the new value computed by the `++` or `--` operator of the property type | as row 82 | S, X, V, H, L | For `int`, `Is(t, Ig(t) + 1)`. |
| 85 | Postfix `r.P++` whose value is used | Yes | as row 82 | `x = r is var t && Ig(t) is var old && Is(t, old + 1) is var _ ? old : default!` | as row 82 | S, X, V, H, L | The value of the site is the value read before the increment. |
| 86 | `r.P ??= v` | Yes | as row 82 | statement: `{ var t = r; if ( Ig(t) is null ) Is(t, v); }`; value used: `r is var t ? ( Ig(t) is { } c ? c : Is(t, v) ) : default!` | as row 82 | S, X, V, H, L | The right operand is evaluated only when the value read is null. For a nullable value type, the result of `??=` has the underlying type, and the rewrite unwraps the value returned by the setter (section [6.4.13](06b-signatures-and-validation.md#6413-accessor-sites)). |
| 87 | Compound site with one accessor use intercepted | Yes | the interceptor of that use | only get: `t.P = Ig(t) + v`; only set: `Is(t, t.P + v)` | as row 82 | as rows 79 and 80 | The other accessor stays a plain access on the same temporary (RC50). |
| 88 | Conditional access `r?.P`, `r?.P = v` | Yes | `static T I(this TR receiver)`, `static T I(this TR receiver, T value)`; V: as rows 79 and 80 | `r?.I()`, `r?.I(v)` | as rows 79 and 80 | X, V | As row 42. A compound site in a conditional access is rewritten in a statement: `{ var t = r; if ( t is not null ) Is(t, Ig(t) + v); }`. |
| 89 | Event subscription `r.E += h`, `r.E -= h` | Yes | `static void I([NotNull] TC receiver, THandler handler)`; V: instance `void I(THandler handler)` | `Cn.I(r, h)`; V: `r.I(h)` | `receiver.E += handler`, `receiver.E -= handler` | S, X, V, H, L | `IEventAssignmentOperation.Adds` selects the accessor. |
| 90 | `base.P`, `base.P = v`, `base.E += h` with a virtual member | Yes | instance `T I()`, `T I(T value)`, `void I(THandler handler)` in the calling type | `this.I()`, `this.I(v)`, `this.I(h)`; L: `I()` | `base.P`, `base.P = value`, `base.E += handler` | C, L | Rule R4, as row 4. |
| 91 | Struct receiver that is a writable variable | Yes | setter `static T I(ref S receiver, T value)`; getter with the passing mode of section [6.2.7](#627-arguments-generic-context-and-passing-mode) (`in` for a readonly getter, such as the getter of an auto-property, `ref` otherwise) | `Is(ref s, Ig(s) + 1)` | `receiver.P`, `receiver.P = value` | S, V, H, L | A compound site whose struct receiver needs a temporary uses a `ref` local in a statement, and otherwise gets `ReceiverTemporaryNotPossible` (section [6.2.11](#6211-accessor-sites), step 8). |
| 92 | C# 14 extension property `x.Name`, `x.Name = v` | Yes | parameters of the implementation methods of the accessors (receiver first) | `Cn.I(x)`, `Cn.I(x, v)` | `global::E.get_Name(receiver)`, `global::E.set_Name(receiver, value)` | S, X, H, L | Rule R0, as row 19. The existing invoker writes the same static form (ENG27 `CodeModel\Invokers\FieldOrPropertyInvoker.cs:35-38, 70-101`). |
| 93 | Object and `with` initializers, deconstruction targets, `init` setters, `ref`-returning properties, C# 14 instance compound and increment operators, receivers whose temporary C# cannot declare | No: `ObjectOrWithInitializer`, `DeconstructionTarget`, `InitOnlySetter`, `RefReturn`, `InstanceCompoundOperator`, `ReceiverTemporaryNotPossible` | | | | | Section [6.2.11](#6211-accessor-sites). |
| 94 | Assignment of a getter-only auto-property in its own constructor, a field-like event used as a value in its declaring type, properties and events in `nameof` and in expression trees | No: silent | | | | | Section [6.2.11](#6211-accessor-sites). They do not call an accessor, or they are data. |

In rows 6, 9, 11, 12, 38 and 52, the receiver parameter is shown with `scoped` for the general case. In version 1, the factory cannot declare `scoped`. The modifier is omitted when section [6.2.7](#627-arguments-generic-context-and-passing-mode) allows it, and otherwise the call site gets the `ScopedParameter` limitation.
