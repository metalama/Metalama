# Requirements, interpretations and traceability

> Part of the [call-site interceptors design](README.md). Previous: [01-summary.md](01-summary.md) | Next: [03-background.md](03-background.md). Evidence prefixes and terms: [00-conventions.md](00-conventions.md).

## 2. Requirements, interpretations and traceability

### 2.1 Requirements

| Id | Requirement (paraphrase of the product owner) |
|---|---|
| R1 | Aspects or fabrics can add interceptors: advice applied at the call site. |
| R2 | An interceptor has a scope and a target. The scope is a namespace, a type or a member from which the target is accessed (the calling side). The target is, for now, a method (invocation) or the `await` keyword. Operators are skipped for now. |
| R3 | Aspects add interceptors through the adviser mechanism (`IAdviser`), and fabrics through the amender mechanism (`IAmender`, `IQuery`). |
| R4 | The implementation lives in a premium extension package, `Metalama.Extensions.Interceptors`. The open-source repository receives only extension points, with no interceptor implementation. |
| R5 | Interceptors can share design with reference validators, which share many concepts and already have reference-scanning logic. |
| R6 | The compilation being scanned is the source compilation, not the post-aspect one. |
| R7 | For now, several interceptors on the same call site are forbidden. |
| R8 | An interceptor is an interface that, given an interception context, returns either an existing method or a template with arguments. When a template is supplied, the template container must be supplied too. |
| R9 | Generated or matched interceptor methods must match the intercepted signature exactly, allowing only implicit conversions. In an interceptor, calling `meta.Proceed()` calls the intercepted logic. |
| R10 | Interceptors are deduplicated at linker level in two stages: compute the signature, group by signature, name and container, then choose one representative to implement the method with the template. |
| R11 | For `await` interceptors, any awaitable type could in theory be used. |
| R12 | A possible template container is a local function in the calling member, which allows the interceptor to use the calling member's arguments. |
| R13 | When the interceptor is in the current class hierarchy (calling type or a base type), it can be instance or static. |
| R14 | Interceptor providers can report diagnostics or skip a call site. |

### 2.2 Interpretations

These readings are interpretations. The product owner confirms them in PO40. I11 and I12 come from the product owner.

| Id | Interpretation | Source |
|---|---|---|
| I1 | `meta.Proceed()` invokes the intercepted target: the method to which the original call binds, or the original awaitable for an await expression. It does not invoke another interceptor, because R7 allows only one interceptor per call site in version 1. Composition would bind `meta.Proceed()` to the previous layer (section [16.1](16-future-directions.md#161-composition-of-several-interceptors-on-one-site)). | Lead |
| I2 | The grouping key of R10 (signature, name and container, which this design calls the placement) also includes the proceed target and the implementation identity. Otherwise `Console.WriteLine(string)` and `Debug.WriteLine(string)` could share one body whose `meta.Proceed()` calls only one of them. | Lead |
| I3 | R7 counts interceptors per target at one syntax node. The expression `await F()` has two targets: the invocation `F()` and the `await`. One method interceptor and one await interceptor can both apply. The rewrite nests them: `await IA(IF())`. | Delivery plan |
| I4 | The compilation is an accepted scope, equivalent to the global namespace with all descendant namespaces. It is available to project fabrics and to aspects whose target is the compilation. | Delivery plan |
| I5 | R6 means that call sites in code introduced by aspects are never intercepted. The treatment of files classified as generated code is decision PO12. | Delivery plan |
| I6 | R11 means: the awaited operand may be of any awaitable type, and an interceptor may return any awaitable whose result type converts implicitly to the original result type (section [7.6.1](07-await-interception.md#761-result-compatibility), RC59). For an interceptor that is an `async` method, the task type is the declared task type of an `async` template, or the type that the engine chooses for a non-async template. | Await part, fifth product-owner batch |
| I7 | An await interceptor has two modes. In mode `Await`, the interceptor is `async` and `meta.Proceed()` awaits the original awaitable. In mode `Awaitable`, the interceptor is not `async`, returns an awaitable that the call site awaits, and `meta.Proceed()` returns the original awaitable without awaiting it. | Await part |
| I8 | The origin of an await in a lambda or a local function is the enclosing member, as for invocations in the reference index. Scope matching uses this origin. | Await part |
| I9 | R10 places deduplication at linker level. This design deduplicates in the premium engine, before it calls the open-source transformation factory. The linker receives one synthesized method per group. The reason is that the open-source linker must not contain interceptor semantics (R4), and that names must be known when a method is declared (section [8.7](08-deduplication-and-naming.md#87-names-and-accessibility)). | Lead |
| I10 | R8 requires a template container when a template is supplied. This design reads the term as the declaration in which the generated method is declared, as R12 uses it for a local function, and realizes it as the placement of the interceptor (`InterceptorPlacement`, section [0.3](00-conventions.md#03-terms)). The type that declares the template, which is the template provider, is optional and is chosen by the rules of section [5.3.10](05a-api-registration.md#5310-default-template-provider). | Lead |
| I11 | R2 names a method "(invocation)" as a target. This design reads it as every use of the method that leads to a call without further source code: an invocation, and a method group converted to a delegate or to a function pointer. A reference in `nameof`, in an expression tree, or in a method group that is not converted is not a use. | Product owner, second batch (RC44) |
| I12 | The method target of R2 includes the accessors of properties and events, because accessors are methods (`MethodKind.PropertyGet`, `PropertySet`, `EventAdd`, `EventRemove`, FW27 `Code\MethodKind.cs:29-44`). A use of an accessor is a read, a write, a compound assignment, an increment, a decrement, or `+=` and `-=` on an event. Operators stay out of scope, as R2 states. | Product owner, third batch (RC48) |

### 2.3 Traceability

| Item | Designed in | Open-source work | Premium work | Milestone | Main tests (section [12](12-test-plan.md#12-test-plan)) |
|---|---|---|---|---|---|
| R1 | 5, 9.4, 10.2 to 10.4 | Adviser bridge, hook, factory | Registration API and pipeline extension | M0, M1 | `AdviserExtensionContextTests`, `TransformingPipelineExtensionTests`, premium `Registration/*` |
| R2 | 5.3, 6, 7.1, 9.2, 10.7 | `SourceReferenceIndexService`, `ReferenceKinds.Await` | Scope matching, target selection by declaring type and member names, the type predicate | M0, M1, M5 | `SourceReferenceIndexServiceTests`, premium `TargetMatcherTests`, premium `Scope/*`, `Matching/*`, `Await/*`, `Invocations/Declined/Operator_NotIntercepted.cs` |
| R3 | 5.3.6 to 5.3.8, 9.4 | Adviser bridge | Overloads on `IQuery`, `ITaggedQuery`, `IAdviser`, `ITypeAmender` for the three verbs | M0, M1 | premium `Registration/*` |
| R4 | 1.4, 10 | All primitives are generic | Everything else | M0 to M7 | Test-only proof of concept that compiles without `InternalsVisibleTo` (12.5); API review |
| R5 | 9.2, 10.7 | Shared walker and its fixes; one index of source references per stage | Requirements derived from the member names of the registrations; migration of Validation to the shared index (F20) | M0, M1 | `SharedIndex_BuiltOncePerStage`, `ValidatorsAndInterceptors_ShareBinding`; Validation and Architecture suites unchanged except documented fixes |
| R6 | 9.5.2, 10.3 | `SourceCompilation`, `IsSourceStage` | Scan of the source compilation | M0, M1 | `SourceCompilationTreesAreInputTrees`, `WeaverSplitsStages_LaterStageIsNotSource`, `Scope/IntroducedCode_NotIntercepted.cs` |
| R7 | 9.5.8 | Duplicate-redirection guard in the factory | Conflict detection | M1 | `Conflicts/*`, design-time `Conflict_ReportedAtCallSite` |
| R8 | 5.6, 5.7 | Synthesized method | Result model, binder of existing methods | M1, M2 | `Results/*`, `Templates/*`, `Binding/*` |
| R9 | 5.6.8 to 5.6.10, 5.7.3, 6.4, 6.6, 10.4.5, 10.6 | Custom proceed binding, argument casts, argument lists of redirections with temporaries and discards | Signature derivation, parameter binding (`InterceptorArgument`, canonical binding by name), signature builder, added parameters, the pull guard, one validator for existing and adjusted signatures and their bindings (`InterceptorSignatureValidator`) | M1, M2 | `ExistingMethods/*`, `Binding/*`, `Builder/*`, `Invocations/Parameters/*`, `Templates/Template_Proceed.cs`, runtime tests |
| R10 | 8 | Naming services shared with the factory | Grouping and representative | M2 | `Dedup/*` |
| R11 | 7, 10.6.9 | `Await` kind, await redirection, `AnyAwaitable` | Await model | MA, M5 | `Await/*`, `Runtime/Await_*`, template tests `AnyAwaitable/*` |
| R12 | 6.5.4, 10.4, 10.5.4 | Synthesized local function | Placement rules | M4 | `LocalFunctions/*` |
| R13 | 5.6.3, 5.6.8, 5.6.10, 5.7.2, 6.4.1, 6.5 | Instance synthesized methods, proceed on `this` and on `base`, the call-site mode `MemberOfReceiver` | Receiver-mapping rules R0 to R4, the static or instance choice of the builder, `ReceiverMapping` of the binder, the source `CallerInstance` | M2 (R1, R1x, R3, `CallerInstance`), M3 (R2, R4) | `Invocations/ReceiverMapping/*`, `Binding/CallerInstance_*`, `Hierarchy/*`, `Runtime/BaseCall_NoRecursion.cs`, `Runtime/Invocation_ReceiverFamily_VirtualDispatch.cs` |
| R14 | 5.6.1, 9.5.7, 9.7 | None | Invocation of the providers at compile time, and in the analyzer when PO26 is accepted; `Skip` and `context.Diagnostics` | M1, M6 | `Conflicts/Provider*`, `Results/Skip_WithProviderDiagnostic`, analyzer tests |
| I1 | 5.7.2, 10.6.3 | Proceed binding | Proceed shape | M2 | `Templates/Template_Proceed.cs`, `Runtime/Invocation_ProceedCallsOriginal.cs` |
| I2 | 8.2 | None | Key | M2 | `Dedup/SameSignatureDifferentTarget_TwoMethods.cs` |
| I3 | 7.1 | Nested substitutions | Separate targets | M5 | `Await/Await_OperandIsInterceptedInvocation.cs` |
| I4 | 5.3.2 | None | Scope kinds | M1 | `Registration/ProjectFabric_CompilationScope.cs` |
| I5 | 9.5.4 | None | Scan filters | M1 | `Scope/GeneratedFile_Default.cs`, `Scope/GeneratedFile_Included.cs` |
| I6, I7 | 7.6 | Proceed bindings | Await modes | M5 | `Await/Awaitable_*` |
| I8 | 7.2 | Walker attribution | Scope matching | M5 | `Await/Await_InLambda.cs` |
| I9 | 8.1, 8.8 | Naming services shared with the factory | Grouping before the factory | M2 | `Dedup/*` |
| I10 | 5.3.10, 5.6.3 | None | Placement model, default template provider | M1, M2 | `Placements/*`, `Registration/DelegateIgnoresTemplateProviderRule2.cs` |
| I11 | 5.3.11, 6.2.10, 6.4.12, 10.4.5 | `RedirectMethodReference`, the method-group rewrite of the injection rewriter | Method-reference site model, shapes, signature constraints | M2, M3 | proof of concept `MethodReferences/*`, premium `MethodReferences/*`, `Runtime/MethodReference_*` |
| I12 | 5.3.13, 6.2.11, 6.4.13, 10.4.5, 10.5.6 | `RedirectAccessor`, the accessor proceed bindings, the accessor and compound rewrites of the injection rewriter, `OperatorKind.NullCoalescingAssignment` | `InterceptAccessors`, the accessor-site model, accessor signatures, the compound-site rules | M2, M3 | proof of concept `Accessors/*`, premium `Accessors/*`, `Runtime/Accessor_*` |

### 2.4 Gaps found in the baseline

The baseline covers every requirement. The following gaps were not covered, or only implicitly. The table gives the resolution in this design.

| Gap | Consequence if not addressed | Resolution | Section |
|---|---|---|---|
| G1. Accessibility and modifiers of synthesized methods. | Accidental public API; defensive copies in structs. | `private` in the calling type and for local functions, `private protected` in a base type, `internal` elsewhere; the least restrictive level over the group; `readonly` for instance interceptors called from readonly struct members or on a receiver whose method is readonly. | 6.4.1, 8.7 |
| G2. Layer attribution of methods synthesized for project and namespace fabrics. | Assertion in `AspectReferenceResolver`. | The origin maps these fabrics to the fabric aggregate layer. | 10.2.4 |
| G3. Caller-information arguments at redirected call sites. | Wrong values; a linker assertion when `CallerMemberSubstitution` targets the same node. | Values computed from the source call site are passed explicitly. The call is rewritten before the analysis step, so `CallerMemberSubstitution` never targets the same node (RC39). | 6.7, 10.5.8 |
| G4. Conditional access. | Wrong rewrite or silent decline. | Extension receiver mode. | 6.3 row 42, 10.4.5 |
| G5. Existing C# interceptors. | CS9234 or CS9153 with no Metalama explanation. | LAMA1011, plus the general diagnostic LAMA0662 of fix F11. | 9.5.8, 10.10, 11.2 |
| G6. Incomplete walker fixes. | Missed and duplicated call sites. | Complete list in 10.7. | 10.7 |
| G7. Preview references to synthesized members that the preview drops. | Invalid preview output. | The premium engine does not rewrite a call site whose placement is outside the preview compilation (LAMA1030). | 9.8 |
| G8. Behavior in preview, live templates and introspection. | Unexpected code in user sources. | The hook context exposes the execution scenario. | 9.8 |
| G9. Files classified as generated code. | The build and the IDE disagree. | Not intercepted by default (PO12). | 9.5.4 |
| G10. Proceed when an aspect overrides the intercepted method. | The override is bypassed. | The proceed call is a plain call that binds to the final implementation. | 5.7.2, 10.6.3 |
| G11. Top-level statements. | Silent loss of the rewrite. | The injection rewriter visits global statements, and it keeps the visited members when the compilation unit receives injections. Local functions and the calling type are not placements there in version 1. | 10.5.3 |
| G12. Nullable context and flow attributes of synthesized methods. | New warnings, which become errors with warnings as errors. | Flow attributes copied when their type is available; nullable context part of the key; signature and body generated in the requested nullable context. | 6.4.5, 6.4.7, 10.6.4 |
| G13. Registrations of aspects whose outcome is Error or Ignore. | User confusion. | Documented. | 9.10 |
| G14. WPF precompile stage calls no hook. | Interceptors absent from the markup-compile assembly. | Accepted and documented. | 9.10 |
| G15. Sharing mechanism with Validation. | Packaging uncertainty; the same bodies bound twice. | A shared scan pass (RC36). The extraction of a neutral package was withdrawn (RC47). | 9.2, 10.7.5 |
| G16. A `TransitiveProjectFabric` cannot name a type of the consumer as the placement. | The cross-project scenario cannot be written. | `InterceptorPlacement.CallingType()` and `InterceptorPlacement.GeneratedStaticClass()`. | 5.6.3, 9.9 |
| G17. Calls to members introduced by aspects bind in the IDE but not at build time. | Diagnostics in the IDE that the build does not report. | Phase B ignores targets declared only in generated trees. | 9.7.6 |
| G18. No diagnostic range reserved. | Collisions. | LAMA1000 to 1049 for premium, LAMA0656 to 0669 for open-source primitives. | 9.11 |
| G19. Registration after `BuildAspect` through `builder.Outbound` is silently lost. | A registered interceptor disappears. | Open-source fix F12. | 10.10 |
| G20. The default design-time bucket loses contributors. | Registrations disappear in the IDE. | Open-source fix of 10.8.3. | 10.8.3 |
| G21. Method groups converted to delegates. | `list.Select( Transform )` escapes an interception that `list.Select( x => Transform( x ) )` receives, so two equivalent programs behave differently. | Method-reference sites are intercepted in version 1 (RC44). | 5.3.11, 6.4.12 |
| G22. Property and event accessors of external types. | A property of a referenced assembly cannot be advised, which is the property form of the gap of section [1.2](01-summary.md#12-why). | `InterceptAccessors` (RC48). | 5.3.13, 6.4.13 |
