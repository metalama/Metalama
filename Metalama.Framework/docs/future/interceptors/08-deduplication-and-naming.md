# Deduplication and naming

> Part of the [call-site interceptors design](README.md). Previous: [07-await-interception.md](07-await-interception.md) | Next: [09-premium-engine.md](09-premium-engine.md). Evidence prefixes and terms: [00-conventions.md](00-conventions.md).

## 8. Deduplication and naming

### 8.1 Two stages

R10 asks for a two-stage implementation. This design implements it as follows:

1. Stage 1, per call site (sections [6](06a-call-site-model.md#6-call-site-semantics-and-signature-derivation-for-invocations) and [7](07-await-interception.md#7-await-interception)): after the user provider has returned a template result, the engine computes the admissible placement, the receiver mapping and the interceptor signature, invokes the `configure` function of the result and validates the adjusted signature, then computes the proceed target, the rewrite plan and an `InterceptorRequestKey`. No template is expanded. Stage 1 also runs at design time.
2. Stage 2, per compilation, compile time only: the engine groups the requests by key, chooses a representative per group in deterministic order, declares one synthesized method per group through the open-source factory, and requests one redirection per call site.

Deduplication happens in the premium engine before the factory is called. The linker has no aggregate key for synthesized members (section [8.8](#88-where-deduplication-happens)).

### 8.2 The key

```csharp
/// <summary>
/// Identifies one synthesized interceptor. Two requests with equal keys are implemented by one method.
/// The key lives for one pipeline run and is never persisted.
/// </summary>
internal sealed class InterceptorRequestKey : IEquatable<InterceptorRequestKey>
{
    // Where the method is generated.
    public PlacementIdentity Placement { get; }          // Type definition, introduced-type reference, generated static class,
                                                         // or local-function host (member definition and body node).
                                                         // For BaseMostAccessibleType(), the definition of the type that
                                                         // the walk of section 6.5.6 selected.
    public InterceptorMethodKind MethodKind { get; }     // Static, StaticExtension, Instance, InstanceReadOnly, LocalFunction.
    public InterceptorReceiverMapping ReceiverMapping { get; } // The rule R0 to R4 of section 6.4.1.
    public string RequestedName { get; }                 // The default name, or the name set by the configure function.

    // What meta.Proceed() calls (interpretation I2).
    public ProceedTargetKey ProceedTarget { get; }

    // The complete signature after the adjustments of the configure function, compared defensively. It includes the
    // requested accessibility, the added parameters, the widened parameter types and the default mode.
    public InterceptorSignature Signature { get; }

    // Whether the signature satisfies the rule of section 6.4.12. It is computed from Signature and ProceedTarget for
    // every request. It also selects the attribute policy of section 6.4.5. A method-reference site requires true.
    public bool IsMethodGroupConvertible { get; }

    // How the body is produced (interpretation I2, refined by RC13).
    public ImplementationKey Implementation { get; }
}

/// <summary>Identity of the intercepted target as seen from the interceptor body.</summary>
internal abstract record ProceedTargetKey;

internal sealed record InvocationProceedTargetKey(
    IMethodSymbol TargetDefinition,                      // Compared with SymbolEqualityComparer.Default. The accessor for an accessor interceptor.
    MethodKind AccessorKind,                             // Ordinary for a method; PropertyGet, PropertySet, EventAdd or EventRemove.
    ImmutableArray<SignatureType> Slots,                 // Closed types or interceptor type parameters, in slot order.
    ProceedCalleeKind CalleeKind,                        // Section 6.4.9.
    ReceiverPassingMode PassingMode,
    SignatureType? ReceiverType,
    InvocationDispatchKind Dispatch ) : ProceedTargetKey;

internal sealed record AwaitProceedTargetKey(
    AwaitInterceptionMode Mode,
    SignatureType AwaitableType,                         // A.
    SignatureType ResultType,                            // R.
    SignatureType ReturnType ) : ProceedTargetKey;       // T_I.

/// <summary>Identity of the code that implements the interceptor.</summary>
internal readonly record struct ImplementationKey(
    object Owner,                                        // The aspect instance or the fabric instance, compared by reference.
    TemplateMemberIdentity Template,                     // Template class type, member name, selected kind (default, async, enumerable).
    object ProviderIdentity,                             // Provider instance (by reference) or provider type (FromType).
    object? Arguments,                                   // Compared with TemplateArgumentComparer.
    object? Tags );                                      // Compared with TemplateArgumentComparer.
```

The receiver-mapping rule is part of the key, because it changes the parameter list, the method kind and the meaning of `meta.This` and `meta.Proceed()`. The syntactic receiver kind (implicit `this`, explicit `this`, expression, conditional access) belongs to the rewrite plan, not to the body, so it is not part of the key. Call sites with the same rule, the same passing mode and the same callee kind share one method. For example, `M()`, `this.M()` and `x.M()` share one interceptor when they follow the same rule and their receivers have the same passing mode. A direct call site and a conditional-access call site share one extension-form interceptor under R1x, and one instance interceptor under R2 (section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping)). A call site whose condition R2a fails uses R3 or R1, and therefore belongs to another group than the call sites of the same placement that use R2.

Method-reference sites use the same key. The kind of use is not part of the key, because the interceptor method is the same for a call and for a method reference. `IsMethodGroupConvertible` is a function of the signature, so a call site and a method-reference site with the same signature share one method, and the group key still states that the method must stay method-group convertible (RC44). The wrapper of section [6.4.12](06b-signatures-and-validation.md#6412-method-reference-sites) is part of the rewrite plan of one site, not of the method, so it does not split a group. In `list.Select( x => Transform( x ) )` and `list.Select( Transform )`, the two sites share one interceptor (sample 11).

Accessor interceptors use the same key, with the accessor as `TargetDefinition` and the accessor kind in `AccessorKind`. The kind is stated explicitly, although the accessor implies it, so that a getter and a setter of one property never share a key through a signature that happens to be equal. The shape of the site (read, write, compound assignment, increment, `??=`) and the assignment operator are not part of the key: they belong to the rewrite plan of the site, so one getter interceptor serves every read of a group, and one setter interceptor every write.

The placement identity of `BaseMostAccessibleType()` is the type that the walk selected, not the calling type and not the factory (section [6.5.6](06b-signatures-and-validation.md#656-base-most-accessible-type), RC61). Sites in `OrderService` and `InvoiceService` whose walks both select `ServiceBase` therefore have the same placement identity as sites that request `InType( ServiceBase )`, and they share one method when the rest of the key is equal. A site whose walk stops at its own calling type has that type as its placement identity.

The adjustments of a `configure` function are part of the key through `RequestedName` and `Signature`. Two call sites whose functions produce the same adjusted signature share one method, even when the functions are different objects. The function itself is not part of the key.

Bindings are site data and are never part of the key (decision PO62, RC63). The source of each parameter, the argument plan with its temporaries and discards, and the values of the added parameters belong to the rewrite plan of the site (section [6.4.10](06b-signatures-and-validation.md#6410-rewrite-plan)). The added parameters themselves, with their names and types, are part of `Signature`, and a group with added parameters uses the `Materialized` mode and packs `params` (section [6.4.4](06b-signatures-and-validation.md#644-defaults-and-params), rule j). Two sites that pass different pulled values, different caller information or different callers' instances therefore share one method. Existing methods are not grouped, so their bindings never meet the key.

### 8.3 Canonical types and type parameters

- Closed types are compared with `SymbolEqualityComparer.IncludeNullability`, which uses `TypeCompareKind.ConsiderEverything2` (RC `..\Core\Portable\Symbols\SymbolEqualityComparer.cs:22-35`). It separates nullability, tuple element names and `dynamic` from `object`. Each of these produces a different declaration, and mixing them would move warnings to the call sites or break code after an await (for example `(await GetAsync()).a` with a named tuple).
- `ConversionKind.Identical` is not used, because it ignores tuple names and nullability.
- Interceptor-owned type parameters are compared by ordinal, together with their copied constraints.
- Type parameters visible at the placement are closed symbols. They are equal only for call sites that share the placement or the host, which the key also compares.
- The signature is a function of the proceed target, the placement and the modes. It is compared in full as a defensive measure. The internal `SignatureTypeComparer` is not used (RC22).

### 8.4 Proceed target identity

The invocation proceed target is identified by the definition symbol plus the slot pattern, not by the constructed `IMethodSymbol` (challenge to B6, adopted). Two call sites in different generic methods whose slots are both lifted share one interceptor, while `List<int>.Add` and `List<string>.Add` with closed slots produce two. Symbols of the source compilation are stable within one run, so no durable reference is needed.

Per interpretation I2, two targets with the same signature never share a key: `Console.WriteLine(string)` and `Debug.WriteLine(string)` produce two interceptors.

The await proceed target is determined by the mode, by `A`, by `R`, and by `T_I`, which depends on the task kind of the options, on the declared task type of an `async` template (section [7.9.1](07-await-interception.md#791-accepted-template-shapes)), on the awaitable type given to `WithAwaitRewriteOptions`, and on the `ValueTask` detection of section [7.7](07-await-interception.md#77-task-type-of-an-async-interceptor-and-target-frameworks). The template kind, `async` or not, is part of the implementation identity through the template member. The call-site adaptation (the `ConfigureAwait(false)` suffix and the result cast) is not part of the key.

### 8.5 Implementation identity and the user contract

The template identity is the template member plus the selected template kind. The provider identity follows `TemplateProvider` (FW27 `Aspects\TemplateProvider.cs:36-109`): the provider object compared by reference when the provider is an instance, and the provider type when it was created with `FromType<T>()`.

The registering owner is part of the key (RC13). Templates can observe the owner: `meta.AspectInstance` (FW27 `Aspects\meta.cs:341`) and `meta.Tags`, which merges `IAspectBuilder.Tags` (FW27 `Aspects\IAspectBuilder.cs:149-166`), differ between owners. The template runs on `TemplateProvider.Object` (ENG27 `Templating\TemplateDriver.cs:65`), so fields of the owning aspect can change the generated code, and `FromType` lets different owners share a provider.

Template arguments and tags are compared by value with `TemplateArgumentComparer`:

- null, primitives, strings and enumerations with `Equals`;
- `System.Type` with `Equals`;
- `IType` with the compilation's type comparer, including nullability;
- `IDeclaration` and `IRef<T>` by declaration identity;
- arrays and other non-string sequences element by element;
- anonymous-type objects, records and `IReadOnlyDictionary<string, object?>` member by member;
- anything else by reference.

The hash code is consistent with these rules. A value without value equality prevents sharing, which is safe (PO20).

The user contract, documented in the article on templates:

1. The body of a synthesized interceptor depends only on the template, the template provider, the owner, the template arguments and tags, and the group-invariant facts exposed by `meta.MethodInterception` and `meta.AwaitInterception`.
2. Call-site-specific data must be passed as a template argument. It then splits the group automatically.
3. To share interceptor bodies across aspect instances, register from a fabric or from an aspect on a namespace or the compilation.

### 8.6 Representative order

Members of a group are ordered by this key (RC14):

1. The path of the syntax tree relative to the project directory, computed with `Path.GetRelativePath` also for files outside that directory (the result then starts with `../`), with every directory separator replaced by `/`, compared with `StringComparer.Ordinal`. Only a file on another volume than the project directory keeps its full path. Its order then depends on the machine, and the documentation states it.
2. The span start of the call site.
3. The span length of the call site.

The first member is the representative. Its registration provides the contribution origin used to declare the synthesized method, and its call site is the location of template-expansion diagnostics reported by the engine. The normalization makes the representative identical on Windows and Linux, where absolute paths and separators differ.

### 8.7 Names and accessibility

Names are allocated when the engine calls `ExtensionTransformationFactory.DeclareMethod`, by the linker's own naming services, which the pipeline stage creates once and shares with the linker (RC15, section [10.4.6](10a-oss-bridge-hook-factory.md#1046-names-and-lexical-scopes)). Consequences:

- `meta.Target.Method.Name` equals the emitted name, because builder data copies the name when the builder is frozen (ENG26 `CodeModel\Introductions\BuilderData\NamedDeclarationBuilderData.cs:20`).
- The call-site substitution receives the final name as syntax, with no symbol lookup.
- The engine calls `DeclareMethod` once per group, in representative order, so names are deterministic.
- Groups never overload each other. Two groups in one placement with the same requested name get distinct names with numeric suffixes in representative order. The name provider also avoids the names of inherited members and of members of derived types, to prevent hiding and capture by overload resolution.

Accessibility is computed per group. For each call site under R0, R1, R1x, R3 and R4: `private` when the calling type is the placement or nested in it; `private protected` when the calling type derives from the placement; `internal` otherwise. Under R2, the method is called through the receiver, so the rules of condition R2b apply: `private` when the calling type is the placement or nested in it; `private protected` when the calling type derives from the placement and the receiver type is the calling type or derives from it; `internal` otherwise. The method gets the least restrictive level over the group. The same rules apply to the type that `BaseMostAccessibleType()` selects: `private` when it is the calling type, and `private protected` when it is a base type of the calling type, with the condition on the receiver under R2 (section [6.5.6](06b-signatures-and-validation.md#656-base-most-accessible-type)). When a `configure` function requested a level, the method gets that level, which is the same for the whole group because it is part of the key, and E15 has already checked it at each call site (section [6.6](06b-signatures-and-validation.md#66-signature-validation-existing-methods-and-adjusted-signatures-r9)). It must then be consistent with every closed type of the signature (CS0051). If it is not, the engine reports LAMA1017 at the representative and generates nothing for the group; its call sites are not rewritten.

### 8.8 Where deduplication happens

Deduplication happens in the premium engine, before the factory is called. The linker has no aggregate key for synthesized members. R10 places deduplication at linker level, so this is an interpretation (I9), which the product owner confirms in PO40. Reasons:

1. The key contains facts that only the premium engine knows: the proceed target, the dispatch kind, the receiver-mapping rule, the receiver passing mode, the adjusted signature and the implementation identity. An opaque key in the linker would add a second grouping with no information.
2. `IAggregatableInsertStatementTransformation` exists because statement insertions are produced by different aspects during aspect execution, without a global view (ENG26 `Transformations\IInsertStatementTransformation.cs:52-60`). The interceptor hook runs after all aspects and fabrics of the stage, so the premium engine has the global view.
3. Names are allocated at declaration time, so the group must be known before the call.
4. The linker stays free of interceptor semantics, as R4 requires.

Determinism:

- The engine calls `DeclareMethod` once per group in representative order, and `RedirectInvocation`, `RedirectMethodReference`, `RedirectAccessor` or `RedirectAwait` for each site in source order.
- Names are allocated in that order by `LinkerInjectionNameProvider` and `TemplateLexicalScope`, both single-threaded in the hook.
- Each extension transformation has a unique third ordering index (section [10.4.7](10a-oss-bridge-hook-factory.md#1047-attribution-and-ordering)), so `TransformationLinkerOrderComparer` has no ties among them.
- Injected members of one type are inserted in sorted order during `TransformationCollection.FinalizeAsync` (ENG26 `Linking\LinkerInjectionStep.cs:226-228`).
