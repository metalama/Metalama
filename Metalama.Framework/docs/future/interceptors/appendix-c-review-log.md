# Appendix C. Review log

> Part of the [call-site interceptors design](README.md). Previous: [appendix-b-weak-spots.md](appendix-b-weak-spots.md) | Next: [appendix-d-product-owner-reviews.md](appendix-d-product-owner-reviews.md). Evidence prefixes and terms: [00-conventions.md](00-conventions.md).

## Appendix C. Review log

Seven reviewers examined the document on 2026-09-24, each through one lens. This appendix records each finding and its disposition. The identifiers are those of the reviewers, with two prefixes added to avoid collisions with the fix track: `LT-` for the reviewer of the linker and template changes, and `PC-` for the reviewer of the plan and of the consistency of the document.

| Id | Lens | Severity | Problem | Disposition | Reason |
|---|---|---|---|---|---|
| CS-01 | C# semantics | Major | Every conditional-access call site used the `Value` mode, which loses mutations of a writable struct variable in the chain and of an unconstrained type-parameter receiver. | Applied | Section [6.2.7](06a-call-site-model.md#627-arguments-generic-context-and-passing-mode) now classifies conditional-access receivers, and the limitation `ConditionalAccessMutableReceiver` covers type-parameter receivers. |
| CS-02 | C# semantics | Major | Section [6.2.7](06a-call-site-model.md#627-arguments-generic-context-and-passing-mode) claimed that `scoped` has no effect on a receiver that is not ref-like, but an unscoped `ref` or `in` parameter narrows the escape scope of a ref-like return value (CS8347). | Applied | The `ScopedParameter` condition of section [6.2.3](06a-call-site-model.md#623-limitations) covers `Ref` and `In` receivers whenever the receiver type, the return type or a parameter type is ref-like. |
| CS-03 | C# semantics | Major | `IParameterSymbol.ScopedKind` returns the effective scope, so every `out` parameter and every ref-like `params` parameter would have caused the `ScopedParameter` limitation. | Applied | Section [6.2.3](06a-call-site-model.md#623-limitations) defines the explicit scope, which row 29, E13 and `InterceptorParameter` use. |
| CS-04 | C# semantics | Major | `ResultCast` produced a cast in statement contexts (CS0201) and inside conditional-access chains. | Applied | The cast is emitted only when the value is used, and a call site inside a conditional access requires an identical return type (E14, sections [7.10.1](07-await-interception.md#7101-existing-method-interceptors), [10.4.8](10a-oss-bridge-hook-factory.md#1048-validation-performed-by-the-factory) and [10.5.6](10b-oss-linker-and-templates.md#1056-syntax-of-the-rewritten-call)). |
| CS-05 | C# semantics | Major | A file-local type in the signature of a member of a type that is not file-local is rejected by C# (CS9051), but the design allowed any container of the call-site file. | Applied | Check C12 requires a local function or a file-local container, and the `preferredTree` placement was removed. |
| CS-06 | C# semantics | Major | Sections [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping) and [6.4.10](06b-signatures-and-validation.md#6410-rewrite-plan) disagreed on whether a `this` receiver is passed, and a proceed call `this.M()` in a base-type container binds in the wrong type. | Applied | A `this` receiver is always passed as a parameter; `InvokeOnThis` and `ProceedReceiverKind.This` were removed. |
| CS-07 | C# semantics | Major | Row 5 (non-virtual `base` call) required a `FirstArgument` rewrite that the factory rejected. | Applied | The factory accepts `FirstArgument` with a `base` receiver of a non-virtual target and writes `this` (sections [10.4.5](10a-oss-bridge-hook-factory.md#1045-call-site-redirections), [10.4.8](10a-oss-bridge-hook-factory.md#1048-validation-performed-by-the-factory) and [10.5.6](10b-oss-linker-and-templates.md#1056-syntax-of-the-rewritten-call)). |
| CS-08 | C# semantics | Major | The `[NotNull]` receiver attribute and the copied flow attributes do not exist on netstandard2.0, or can be inaccessible polyfills of the target's assembly. | Applied | Section [6.4.5](06b-signatures-and-validation.md#645-attributes) emits an attribute only when its type is available and accessible; samples 6 and 8 show `[NotNull]`. |
| CS-09 | C# semantics | Minor | The C5 probe ignored extension methods because it had no container, used the requested name, and missed instance members and closer extension methods. | Applied | C5 uses `LookupSymbols` with the receiver type as container on the final name, through `SynthesizedMethodRequest.IsNameAvailable`. |
| CS-10 | C# semantics | Minor | A type-parameter receiver passed by `ref` observes reassignments made by the arguments, which the original call does not observe. | Applied | New limitation `ReceiverReassignedByArguments`. |
| CS-11 | C# semantics | Minor | Caller-information parameters that only an existing method has had no defined source value. | Applied | Section [6.7.1](06b-signatures-and-validation.md#671-rule) computes them from the source call site. |
| CS-12 | C# semantics | Minor | A `ref readonly` receiver of an existing method passed without a modifier gives CS9192 or CS9193. | Applied | E8 accepts it only for a variable receiver, with `FirstArgumentByIn`. |
| CS-13 | C# semantics | Minor | Calls without invocation syntax (collection initializers, `foreach`, query operators and others) are never intercepted, which the rules table did not state. | Applied | Row 65 added and row 49 clarified. |
| CS-14 | C# semantics | Minor | The `Declared` default mode could copy a constant that C# cannot declare for the parameter type (CS1763). | Applied | Section [6.4.4](06b-signatures-and-validation.md#644-defaults-and-params) restricts `Declared` defaults. |
| CS-15 | C# semantics | Minor | The factory contract forbade the `readonly` modifier that section [6.4.8](06b-signatures-and-validation.md#648-signature-adjustments-of-the-builder) requires. | Applied | `BuildSignature` may set `IsReadOnly`, which section [10.4.8](10a-oss-bridge-hook-factory.md#1048-validation-performed-by-the-factory) validates. |
| CS-16 | C# semantics | Minor | The deduplication key contained the syntactic receiver kind, which split identical bodies. | Applied | The key uses `ProceedCalleeKind` (section [8.2](08-deduplication-and-naming.md#82-the-key)). |
| CS-17 | C# semantics | Minor | A virtual existing method called from a call site in one of its overrides recurses. | Applied | E17 covers overrides. |
| CS-18 | C# semantics | Minor | Params collections were listed as requiring C# 13, although row 31 declares them without `params` below C# 13. | Applied | Removed from `RequiresNewerLanguageVersion`. |
| CS-19 | C# semantics | Minor | Row 23 said that type parameters of the calling method need no lifting in the calling type. | Applied | Row 23 rewritten. |
| AW1 | Await semantics | Major | Unknown resumption also occurs inside the default kinds (non-constant `ConfigureAwait` arguments), and LAMA1020 as an error broke the build of sample 4 and of the corpus. | Applied with changes | LAMA1020 became a warning that leaves the await unchanged; sample 4 and the corpus fabric skip such awaits; PO24 updated. |
| AW2 | Await semantics | Major | Section [5.6.5](05b-api-providers-contexts-results.md#565-rules-for-template-results) selected templates as if `UseAsyncTemplateForAnyAwaitable` were set, which contradicted sections [5.7.2](05c-api-templates.md#572-metaproceed) and [9.5.11](09-premium-engine.md#9511-emission) and sample 5. | Applied | Sections [5.6.5](05b-api-providers-contexts-results.md#565-rules-for-template-results) and [5.7.2](05c-api-templates.md#572-metaproceed) use the selector unchanged. |
| AW3 | Await semantics | Major | The code-model path of `AsyncHelper` does not treat `Task` as having a method builder, so the `Task` fallback fails on a lifted return type. | Applied | New fix F18, section [7.9.4](07-await-interception.md#794-interaction-with-the-existing-async-machinery), and a test. |
| AW4 | Await semantics | Major | `AwaitInterceptorOptions` carried an `IType` into a `[Durable]` registration. | Applied with changes | Resolved with the approach of API-02: the options are durable, and the awaitable type is passed to `WithAwaitOptions`. |
| AW5 | Await semantics | Minor | The extension `GetAwaiter` check had no position for the generated static class and used a lookup without overload resolution. | Applied | Section [7.10.2](07-await-interception.md#7102-placements) uses speculative binding and restricts the generated static class. |
| AW6 | Await semantics | Minor | `(R)(await I(e))` is invalid as an expression statement. | Applied | Merged with CS-04. |
| AW7 | Await semantics | Minor | The baseline-regression test could not be written with the `Resumption` option. | Applied | The test uses an existing method that reproduces the shape of B8. |
| AW8 | Await semantics | Minor | The `WaitAsync` example applied rule 2 where rule 1 applies. | Applied | Section [7.6](07-await-interception.md#76-modes) corrected. |
| AW9 | Await semantics | Minor | The conversion direction for the return type of an `Awaitable` template was reversed. | Applied | Section [7.9.1](07-await-interception.md#791-accepted-template-shapes) corrected. |
| AW10 | Await semantics | Minor | The public remark promised a look-through of any method named `ConfigureAwait`. | Applied | The remark is restricted to the task family. |
| AW11 | Await semantics | Minor | The await proceed target was described as depending only on the mode and on `A`. | Applied | Sections [7.11](07-await-interception.md#711-internal-components) and [8.4](08-deduplication-and-naming.md#84-proceed-target-identity) corrected. |
| AW12 | Await semantics | Minor | A cross-reference pointed to section [7.11](07-await-interception.md#711-internal-components) instead of 7.10.3. | Applied | Corrected. |
| AW13 | Await semantics | Minor | `InterceptedAwait.IsCompleted` had no validity window. | Applied | Valid only before `meta.Proceed()` (sections [5.7.3](05c-api-templates.md#573-metamethodinterception-and-metaawaitinterception) and [5.7.5](05c-api-templates.md#575-rules-that-keep-deduplication-correct)). |
| AW14 | Await semantics | Minor | The at-most-once annotation on `(await p)` can be lost when the expansion removes parentheses. | Applied | The parameter identifier is annotated (section [10.6.5](10b-oss-linker-and-templates.md#1065-proceed-multiplicity)). |
| AW15 | Await semantics | Minor | The exclusion of `await foreach` and `await using` had an imprecise reason and no product-owner decision. | Applied | Reason corrected; decision PO43 added. |
| AW16 | Await semantics | Minor | Section [7.8](07-await-interception.md#78-semantics-preserved-and-changed-by-mode-await) claimed that template code after `meta.Proceed()` always runs where `M` resumes. | Applied | Row corrected. |
| F-OSS-1 | Pipeline and design time | Major | The origin of a static fabric was said to hold a synthetic aspect instance, which would retain the first compilation in the configuration. | Applied | The origin holds only the layer identifier; the factory creates the synthetic instance per stage (sections [10.2.3](10a-oss-bridge-hook-factory.md#1023-contribution-origin), [10.2.4](10a-oss-bridge-hook-factory.md#1024-proposed-internal-changes) and [10.4.7](10a-oss-bridge-hook-factory.md#1047-attribution-and-ordering)). |
| F-OSS-2 | Pipeline and design time | Minor | The list of `IAdviser` implementations included a type that is not an `IAdviser` and omitted `AddAttributeAdviceResult`. | Applied | Table, exceptions and tests corrected. |
| F-OSS-3 | Pipeline and design time | Minor | `ContributorsAddedInStage` was a `[Memo]` auto-property whose value nothing computed. | Applied | Constructor parameter and the list of `PipelineStepsState` specified. |
| F-OSS-4 | Pipeline and design time | Minor | The change to `CompileTimeAspectPipeline.cs:319` had no effect. | Applied | Removed; remarks corrected. |
| F-OSS-5 | Pipeline and design time | Minor | `Await` was excluded only from an exact `ReferenceKinds.All` request. | Applied | Excluded from every mask derived from `All`; unit test added. |
| F-OSS-6 | Pipeline and design time | Minor | Two existing comments state that a dropped manifest implies an empty collection. | Applied | Section [10.8.2](10c-oss-reference-graph-design-time.md#1082-consumers-of-the-flag) updates them. |
| F-OSS-7 | Pipeline and design time | Minor | `CreateLookupKey` was described as allocation-free. | Applied | Wording corrected in sections [9.5.3](09-premium-engine.md#953-registration-index-and-index-requirements), [10.1](10a-oss-bridge-hook-factory.md#101-overview) and [10.9](10c-oss-reference-graph-design-time.md#109-small-public-helpers-b2g). |
| F-OSS-8 | Pipeline and design time | Minor | Section [3.6](03-background.md#36-advising-channels) misstated the containment rule of `InvokeAsync`. | Applied | Corrected. |
| F-OSS-9 | Pipeline and design time | Minor | Section [9.7.1](09-premium-engine.md#971-phase-a-design-time-pipeline) claimed duplicate registrations across design-time stages. | Applied | Replaced together with P7. |
| F-OSS-10 | Pipeline and design time | Minor | Citations of `ReferenceIndexerOptions.cs` pointed beyond the end of the file. | Applied | Corrected. |
| F-OSS-11 | Pipeline and design time | Minor | `MetalamaDiagnoseMemoryLeaks` never sees the descriptors of registrations made by aspects. | Applied | Section [9.7.5](09-premium-engine.md#975-caches-and-memory) requires premium retention tests. |
| LT-F1 | Linker and templates | Major | `ResultCast` relied on the formatter and was invalid in statement contexts and in conditional accesses. | Applied | Merged with CS-04 (section [10.5.6](10b-oss-linker-and-templates.md#1056-syntax-of-the-rewritten-call)). |
| LT-F2 | Linker and templates | Major | The `#nullable` wrap did not match the null-awareness of the syntax generation context. | Applied | Section [10.6.4](10b-oss-linker-and-templates.md#1064-expansion) generates the member in the requested context. |
| LT-F3 | Linker and templates | Major | Extension-form interceptors in the global namespace can be ambiguous with those of a friend assembly (CS0121). | Applied with changes | Covered by the C5 lookup through `IsNameAvailable`, which sees the classes of friend assemblies; the factory rejects `ExtensionReceiver` for a synthesized class outside the global namespace. |
| LT-F4 | Linker and templates | Major | The linker drops the initializer of a semi-automatic property whose accessor has substitutions. | Applied | Fix F19 and a row of section [10.5.7](10b-oss-linker-and-templates.md#1057-linking-step). |
| LT-F5 | Linker and templates | Major | Intercepted calls in initializers of promoted fields produce LAMA0660, an error. | Applied | New limitation `PromotedFieldInitializer`. |
| LT-F6 | Linker and templates | Minor | The `preferredTree` placement needed `MethodBuilder` changes that the design omitted. | Applied with changes | The placement was removed, because a file-local container has all its parts in one file. |
| LT-F7 | Linker and templates | Minor | `CanImplement` was said to work without a builder. | Applied | It takes the origin and runs at compile time only; design time uses a symbol-level check. |
| LT-F8 | Linker and templates | Minor | `meta.AspectInstance` for fabric origins was unspecified. | Applied | `MetaApi` receives `null` for fabric origins (section [10.6.4](10b-oss-linker-and-templates.md#1064-expansion)). |
| LT-F9 | Linker and templates | Minor | Local functions in members of C# 14 extension blocks were not excluded, and the reason why recursive invocation is unsupported was wrong. | Applied | A factory row and a row of section [6.5.3](06b-signatures-and-validation.md#653-availability-by-context) added; reason corrected. |
| LT-F10 | Linker and templates | Minor | `WithServices` has no `allowOverride` parameter. | Applied | `WithService` is called for each service. |
| LT-F11 | Linker and templates | Minor | The `IntroduceMethodAdvice` precedent for `IsAsync` was wrong. | Applied | Step 5 of section [10.4.4](10a-oss-bridge-hook-factory.md#1044-synthesized-methods-and-proceed-bindings) corrected. |
| LT-F12 | Linker and templates | Minor | The anchors of the context resolver were undefined for initializers of local declarations. | Applied | Section [10.5.5](10b-oss-linker-and-templates.md#1055-analysis-step) defines them. |
| P1 | Premium engine | Major | A requirement with `DeclarationKind.Method` and no name still filters by identifier, so a predicate-only registration intercepts nothing. | Applied | `DeclarationKind.Compilation`; verified in ENG27 `ReferenceGraph\ReferenceIndexerOptions.cs:80-125, 191-209`. |
| P2 | Premium engine | Major | The sort key is not unique for registrations made through `IAspectBuilder.With`. | Applied | Sequence per registering instance; assembly name in fabric descriptions. |
| P3 | Premium engine | Major | Phase B models have no hierarchical options manager. | Applied | New helper E3 (section [10.8.6](10c-oss-reference-graph-design-time.md#1086-hierarchical-options-at-design-time)); the pattern of section [9.9.3](09-premium-engine.md#993-conflicts-between-libraries) changed. |
| P4 | Premium engine | Major | Registrations of aspects on a namespace or on the compilation were filed under the tree of a predecessor. | Applied | Filing by the tree of the scope (section [9.7.2](09-premium-engine.md#972-filing-key)). |
| P5 | Premium engine | Major | Phase B treats the interceptors of third-party generators as hand-written. | Applied | LAMA1011 is compile-time only. |
| P6 | Premium engine | Minor | LAMA1009 describes a case that the pipeline cannot produce. | Applied | LAMA1009 removed and an assertion added; verified in ENG27 `Pipeline\AspectPipeline.cs:333-358`. |
| P7 | Premium engine | Minor | The design-time behavior of split pipelines was incomplete and contradicted section [10.8.5](10c-oss-reference-graph-design-time.md#1085-change-s1-multi-stage-design-time-accumulation). | Applied | Section [9.7.1](09-premium-engine.md#971-phase-a-design-time-pipeline) rewritten; change S1 is required. |
| P8 | Premium engine | Minor | Roslyn never analyzes generated files at design time, and the compile-time classifier was unspecified. | Applied | Sections [9.5.4](09-premium-engine.md#954-reading-the-index) and [9.7.6](09-premium-engine.md#976-consistency-between-design-time-and-compile-time). |
| P9 | Premium engine | Minor | Predicate caching and collector diagnostics were not deterministic. | Applied | `Lazy<bool>` values and a sorted sink. |
| P10 | Premium engine | Minor | Absolute paths of files outside the project directory break the deterministic order. | Applied | `Path.GetRelativePath`. |
| P11 | Premium engine | Minor | Scope diagnostics located at the fabric were dropped in partial design-time runs. | Applied | LAMA1001 and LAMA1002 are located at the scope declaration. |
| P12 | Premium engine | Minor | The list of eligible products and its citation were wrong. | Applied | Sections [3.2](03-background.md#32-premium-reference-validators) and [9.1.5](09-premium-engine.md#915-licensing). |
| P13 | Premium engine | Minor | F11 had no diagnostic, and the LAMA1008 message did not fit `DeclaredBy`. | Applied | LAMA0662 added; two message variants. |
| P14 | Premium engine | Minor | The containment rule for namespace origins was misstated. | Applied | Sections [3.6](03-background.md#36-advising-channels), [5.3.9](05a-api-registration.md#539-registration-rules) and [9.9.1](09-premium-engine.md#991-version-1-route-transitiveprojectfabric-b10). |
| P15 | Premium engine | Minor | The synthetic fabric aspect instance would retain a compilation. | Applied with changes | Resolved with the approach of F-OSS-1 rather than with a durable reference. |
| P16 | Premium engine | Minor | Three citations were wrong. | Applied | Corrected. |
| P17 | Premium engine | Minor | LAMA1000 reported through the adviser sink gives the aspect the Error outcome. | Applied | Reported from `ExecuteContributorsAsync`. |
| P18 | Premium engine | Minor | Kind and type filters do not reduce the binding cost of await interceptors. | Applied | Section [9.6](09-premium-engine.md#96-cost-model-and-shared-binding) corrected. |
| API-01 | API and requirements | Major | `[Durable] object? args` and `tags` produce LAMA0872 for anonymous-type arguments. | Applied with changes | The shorthands lose `args` and `tags`; no new analyzer rule is needed. |
| API-02 | API and requirements | Major | `InterceptorDefinition` stored the non-durable `MethodTemplateSelector` and `AwaitInterceptorOptions`. | Applied | `MethodTemplateSelector` receives `[Durable]` (E2); `AwaitableType` moved to `WithAwaitOptions`. |
| REQ-01 | API and requirements | Major | Deduplication in the premium engine reinterprets R10 without a declared interpretation. | Applied | Interpretation I9 and PO40. |
| REQ-02 | API and requirements | Minor | The reading of "template container" in R8 was not declared. | Applied | Interpretation I10. |
| REQ-03 | API and requirements | Minor | I1 misquoted R9. | Applied | I1 rewritten. |
| API-03 | API and requirements | Minor | Three passages misstated the containment rule. | Applied | Corrected. |
| API-04 | API and requirements | Minor | The attribution of accessor code was wrong. | Applied | Section [5.3.2](05a-api-registration.md#532-scope-semantics) corrected. |
| API-05 | API and requirements | Minor | `Stream.Dispose()` is not virtual. | Applied | The example uses `Stream.Close`. |
| API-06 | API and requirements | Minor | `interceptMethod` rejects lambdas at run time while `[Durable]` suggests that they are accepted. | Deferred | The owner-method rule mirrors transitive validators (R5); the choice goes to the API review together with PO8. |
| API-07 | API and requirements | Minor | The adviser path skipped the owner-method check. | Applied | Step 3 of section [9.4.3](09-premium-engine.md#943-adviser-path-aspects-and-type-fabrics). |
| API-08 | API and requirements | Minor | `LocalFunction()` promised access to locals, and to `this` in structs. | Applied | Summary corrected. |
| API-09 | API and requirements | Minor | `AwaitableKind` duplicated `AwaitableKinds`. | Applied | Merged. |
| API-10 | API and requirements | Minor | `InvocationReceiverKind` duplicates most of `InvocationDispatchKind`. | Deferred | Left to the API usability review before M2. |
| API-11 | API and requirements | Minor | `InterceptorBinding` departs from the `IntroductionScope` convention. | Deferred | Left to the API usability review before M2. |
| API-12 | API and requirements | Minor | Several names differ by one or two letters. | Applied with changes | Only the `IMethod` parameter was renamed to `replacementMethod`; the selector names go to the API review. |
| API-13 | API and requirements | Minor | LAMA1003 and the synchronous `ArgumentException` overlapped. | Applied with changes | Finalizers join the `ArgumentException` list; LAMA1003 covers targets that do not resolve. |
| API-14 | API and requirements | Minor | `OfType` with a custom awaitable type matched nothing. | Applied | `OfType` enables `All` for a custom type. |
| API-15 | API and requirements | Minor | Samples 2 and 4 did not isolate grouping by argument value. | Applied | Sample 4 extended; the text of sample 2 corrected. |
| API-16 | API and requirements | Minor | Public members lacked XML documentation, and the documentation referred to design sections. | Applied with changes | Summaries added; the adviser class states that its members share the documentation of the query class. |
| API-17 | API and requirements | Minor | The public surface is large. | Deferred | The proposed cuts go to the API usability review before M2. |
| PC-F01 | Plan and consistency | Major | The release cut contradicted the principle that primitives ship with their consumers. | Applied | Principle and cut aligned. |
| PC-F02 | Plan and consistency | Major | PO2 and section [11.5](11-delivery-plan.md#115-mapping-to-release-trains) disagreed, and `ReferenceKinds.Await` is public Framework API. | Applied | PO2 and section [11.5](11-delivery-plan.md#115-mapping-to-release-trains) rewritten. |
| PC-F03 | Plan and consistency | Major | The placement of fixes in section [11.4](11-delivery-plan.md#114-dependencies-and-parallel-tracks) contradicted the scope of M0. | Applied | Diagram and prose rewritten. |
| PC-F04 | Plan and consistency | Minor | The M4 dependency, the M6 start and the documentation criteria were inconsistent. | Applied with changes | Diagram fixed; the documentation criterion is stated once in section [11.1](11-delivery-plan.md#111-principles). |
| PC-F05 | Plan and consistency | Major | The M2 corpus criterion required awaits and no new warning. | Applied | Corpus split by milestone, with skips. |
| PC-F06 | Plan and consistency | Minor | M1 depended on a helper of M2. | Applied | `MethodTemplateExists` moved to M0. |
| PC-F07 | Plan and consistency | Minor | PO9 was needed before M0, not before M2. | Applied | Corrected. |
| PC-F08 | Plan and consistency | Minor | The size of the fix track was understated. | Applied | Recomputed. |
| PC-F09 | Plan and consistency | Major | The backports of F4 and F8 change validator results in a stable line. | Applied | 2027.0 only. |
| PC-F10 | Plan and consistency | Minor | F10 claimed a recursion for function pointers. | Applied | Corrected. |
| PC-F11 | Plan and consistency | Major | F11 had no design. | Applied | Row of section [10.10](10c-oss-reference-graph-design-time.md#1010-other-open-source-fixes-found-on-the-way) and LAMA0662. |
| PC-F12 | Plan and consistency | Minor | F12 cited PO38. | Applied | PO21. |
| PC-F13 | Plan and consistency | Minor | The optional change M1 had the name of milestone M1. | Applied with changes | Renamed S1, because D1 is already an identifier of section [10.1](10a-oss-bridge-hook-factory.md#101-overview). |
| PC-F14 | Plan and consistency | Minor | Five section references were wrong. | Applied | Corrected. |
| PC-F15 | Plan and consistency | Minor | The traceability table named tests that do not exist. | Applied | Corrected. |
| PC-F16 | Plan and consistency | Minor | Test names used `GetContext`. | Applied | Renamed. |
| PC-F17 | Plan and consistency | Minor | Rules-table rows 44, 49, 60, 61 and 64 were mis-mapped or untested. | Applied | Mapping corrected. |
| PC-F18 | Plan and consistency | Minor | LAMA1003, LAMA1021 and LAMA1032 had no test. | Applied with changes | Tests added; the LAMA1003 test uses an unreferenced type, because finalizers now throw synchronously. |
| PC-F19 | Plan and consistency | Major | The parity test compared diagnostic sets that differ by design. | Applied | Identifiers specific to one side are excluded. |
| PC-F20 | Plan and consistency | Major | The runtime tests would run nothing without the `MainMethod` option. | Applied | `metalamaTests.json` and a runner change in M0; verified in `AspectTestRunner.cs:392` and P27 `tests\metalamaTests.json`. |
| PC-F21 | Plan and consistency | Minor | An analyzer diagnostic was expected in an aspect test. | Applied | Moved to the analyzer unit tests. |
| PC-F22 | Plan and consistency | Minor | Two references to test infrastructure were wrong. | Applied | Corrected. |
| PC-F23 | Plan and consistency | Minor | Release notes and `OnInitialized` baselines were incomplete, and the 2027.0 release-notes article does not exist yet. | Applied | Corrected. |
| PC-F24 | Plan and consistency | Minor | The Migration edit targeted the wrong file. | Applied | Corrected. |
| PC-F25 | Plan and consistency | Major | The schedule ignored the C# 15 linker work of 2027.0. | Applied | Staffing note and risk T27. |
| PC-F26 | Plan and consistency | Minor | The top decisions of sections [1.9](01-summary.md#19-top-decisions-for-the-product-owner) and [15.1](15-decisions.md#151-top-decisions) disagreed. | Applied | PO2, PO9, PO12 and PO26 to PO29 moved to section [15.1](15-decisions.md#151-top-decisions). |
| PC-F27 | Plan and consistency | Minor | The design gave three incompatible routes for F5. | Applied | Section [9.2](09-premium-engine.md#92-sharing-with-metalamaextensionsvalidation-r5) corrected. |

Adopted simplifications:

1. A `this` receiver is always passed as a parameter. The `Drop` mode for `this`, `ProceedBinding.InvokeOnThis` and `ProceedReceiverKind.This` were removed (C# semantics). The follow-up product-owner review reversed this simplification in part ([Appendix D](appendix-d-product-owner-reviews.md#appendix-d-product-owner-reviews-of-2026-09-24-and-2026-09-25), D-H).
2. The deduplication key uses `ProceedCalleeKind` instead of the syntactic receiver kind (C# semantics).
3. Check C5 is a single lookup with the receiver type as container, run on the final name. The alternative check on the namespace imports of the file was dropped (C# semantics).
4. The `scoped` rule is stated once, in section [6.2.3](06a-call-site-model.md#623-limitations) (C# semantics).
5. A result cast is emitted only when the value is used, and an identical return type is required inside a conditional access (C# semantics; linker and templates, in a restricted form).
6. `AwaitInterceptorOptions` is durable and contains no code-model type. The awaitable type is an argument of `WithAwaitOptions` (await semantics; API).
7. `AdviserExtensionContext` lost `Target`, `AspectPredecessor`, `IsDisposed` and `AspectInstance` (pipeline and design time).
8. `IsSourceStage` compares partial compilations (pipeline and design time).
9. The naming services and the factory are created lazily, and the transformation list is not copied when extensions add nothing (pipeline and design time).
10. The change to `CompileTimeAspectPipeline.cs` was dropped, and the forbidden combination of `ContributorKind` flags is rejected in the `init` accessors (pipeline and design time).
11. The `preferredTree` parameter of `SynthesizedMethodPlacement.InType` was removed (linker and templates).
12. `ExtensionTemplateServices.CanImplement` runs at compile time only (linker and templates).
13. LAMA1009 was removed, and stage 0 is asserted to be the source stage (premium engine).
14. The unfiltered invocation requirement uses `DeclarationKind.Compilation`, with no open-source change (premium engine).
15. LAMA1011 is compile-time only (premium engine).
16. The sort-key sequence is allocated per registering instance (premium engine).
17. LAMA1000 of the adviser surface is reported from `ExecuteContributorsAsync`, like the other registration errors (premium engine).
18. Static-fabric sources are processed only in the first design-time stage, with change S1 (premium engine).
19. `AwaitableKind` was merged into `AwaitableKinds` (API).
20. The template shorthands carry no template arguments and no tags (API, in a restricted form).
21. The optional change M1 was renamed S1, and `MethodTemplateExists` moved to M0 (plan).
22. The traceability table cites only test names defined in section [12](12-test-plan.md#12-test-plan) (plan).

Simplifications not adopted:

- Removing `CallSiteReceiverMode.FirstArgumentByIn` (linker and templates): rejected, because a `ref readonly` receiver of an existing method needs it (CS-12).
- Requiring an identical return type for every existing method (linker and templates): rejected, because R9 allows implicit conversions. The restricted form of simplification 5 was adopted.
- Removing `IsSourceStage` (premium engine), or replacing it with `IsFirstHighLevelStage` (pipeline and design time): rejected. The property is a stable contract for extensions that does not depend on the order of the system layers, and the engine asserts its relation to the stage index.
- Dropping the registration shorthands (API): rejected, because PO8 keeps them. Only `args` and `tags` were removed.
- Forwarding a non-constant `ConfigureAwait` argument to the outer await (await semantics): deferred. It adds a rewrite shape and a second evaluation of the argument, and the warning with a skip is sufficient for version 1.
- Restricting `ProceedMultiplicity.AtMostOnce` to mode `Await` (await semantics): deferred, because a template in mode `Awaitable` can still await the returned awaitable twice.
- Stating the two rules of section [7.5](07-await-interception.md#75-the-adaptive-rewrite-challenge-to-b8-adopted) as one sentence (await semantics): not adopted, because the current rules are exact and the change is editorial.
- Owner propagation through scoped state, and scope-restricted indexing through `InboundReferenceIndexBuilder` (pipeline and design time): deferred to the implementation of M0, because both change code that this review did not verify.
- Internal getters on results and containers, and the removal of `AwaitInterceptorTaskKind`, `AwaitConfiguration`, `EnclosingCodeKind` and `SkipWithJustification` (API): deferred to the API usability review before M2.
- Milestone columns in the fix-track and test tables, and a split of section [11.2](11-delivery-plan.md#112-fix-track) (plan): deferred as editorial changes.
