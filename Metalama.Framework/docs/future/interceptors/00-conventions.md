# Conventions, evidence prefixes and terms

> Part of the [call-site interceptors design](README.md). Previous: [overview.md](overview.md) | Next: [01-summary.md](01-summary.md). Evidence prefixes and terms: [00-conventions.md](00-conventions.md).

## 0. How to read this document

### 0.1 Structure

| Section | Content |
|---|---|
| 1 | Summary: what, why, how, the split between the open-source repository and the premium package, the main decisions, the resolved conflicts between the part designs, and the challenges to the baseline that this design adopts. |
| 2 | Requirements R1 to R14, interpretations, traceability, and the gaps found in the baseline. |
| 3 | Background: the existing architecture that the feature builds on, with evidence. |
| 4 | Mechanism decision: call-site rewriting in the linker versus C# interceptors. |
| 5 | User-facing API of the premium package, with samples and transformed code. |
| 6 | Call-site semantics and signature derivation for method invocations, method references and accessor sites, including the full rules table. |
| 7 | Await interception. |
| 8 | Deduplication and naming. |
| 9 | Premium engine architecture: packaging, registration, compile-time run, design time, preview, cross-project use, diagnostics catalog. |
| 10 | Open-source extension points, with exact signatures and call sites. |
| 11 | Delivery plan and the independent bug fixes. |
| 12 | Test plan. |
| 13 | Documentation plan. |
| 14 | Risk register. |
| 15 | Decisions needed from the product owner, with recommendations. |
| 16 | Future directions that version 1 does not implement but must not close. |
| A, B, C, D | Evidence index, the weak spots that remain after the review, the review log with the adopted simplifications, and the decisions of the product-owner reviews. |

### 0.2 Evidence prefixes

Every statement about existing code carries an evidence reference. EXISTING marks code that exists today. PROPOSED marks new code or new behavior.

| Prefix | Path |
|---|---|
| FW27 | `X:\src\Metalama-2027.0\Metalama\Metalama.Framework\src\Metalama.Framework` |
| ENG27 | `X:\src\Metalama-2027.0\Metalama\Metalama.Framework\src\Metalama.Framework.Engine` |
| DT27 | `X:\src\Metalama-2027.0\Metalama\Metalama.Framework\src\Metalama.Framework.DesignTime` |
| SDK27 | `X:\src\Metalama-2027.0\Metalama\Metalama.Framework\src\Metalama.Framework.Sdk` |
| TST27 | `X:\src\Metalama-2027.0\Metalama\Metalama.Framework\src\tests` |
| DOCS27 | `X:\src\Metalama-2027.0\Metalama\Metalama.Framework\docs` |
| OSS27ROOT | `X:\src\Metalama-2027.0\Metalama` |
| ENG26 | `X:\src\worktrees\Metalama\issue-migration-metalama-5ff22d\Metalama.Framework\src\Metalama.Framework.Engine` |
| FW26 | `X:\src\worktrees\Metalama\issue-migration-metalama-5ff22d\Metalama.Framework\src\Metalama.Framework` |
| TST26 | `X:\src\worktrees\Metalama\issue-migration-metalama-5ff22d\Metalama.Framework\src\tests` |
| DOCS26 | `X:\src\worktrees\Metalama\issue-migration-metalama-5ff22d\Metalama.Framework\docs` |
| P27 | `X:\src\Metalama-2027.0\Metalama.Premium\src` |
| P26 | `X:\src\Metalama-2026.1\Metalama.Premium\src` |
| RC | `X:\src\Metalama-2027.0\Metalama.Compiler\src\Compilers` (Roslyn 5.11 fork; `CSharp\Portable` unless stated) |
| RCDOCS | `X:\src\Metalama-2027.0\Metalama.Compiler\docs` (feature documents of the Roslyn fork) |
| RT | `dotnet/runtime`, branch `main`, `src/libraries/System.Private.CoreLib/src`, fetched on 2026-09-24 into the session scratchpad |
| DOC | `X:\src\Metalama-2027.0\Metalama.Documentation` |

Line numbers with an ENG26, FW26 or TST26 prefix come from the 2026.1 worktree. The linker files of 2027.0 differ only by small offsets in the areas used here: `ENG27\Linking\LinkerInjectionStep.cs` inserts a 12-line pass after line 222, `ENG27\Linking\LinkerInjectionStep.Rewriter.cs` adds a union visitor after line 324, and `ENG27\Linking\LinkerAnalysisStep.SubstitutionGenerator.cs` removes a conditional block around line 494. The lead verified these facts in 2027.0 for this merge: `ReferenceKinds.UnionCaseType = 1 << 27` (FW27 `Code\ReferenceKinds.cs:176`); the stage code of `ENG27\Pipeline\CompileTime\LinkerPipelineStage.cs:39-66`; the ranges of `ENG27\Diagnostics\Ranges.md`; the free diagnostic identifiers LAMA0295, LAMA0296, LAMA0656 to LAMA0669 and LAMA1000 to LAMA1049; the recursion in `ENG27\CodeModel\Comparers\SignatureTypeComparer.cs:104,207`; `ContributorKind.IsExtension` as an internal init property (ENG27 `Extensibility\ContributorKind.cs:23`); the self-comparison in FW27 `Aspects\AdviserExtensions.cs:2109`; the `methodKind` parameter of the `MethodBuilder` constructor (ENG27 `CodeModel\Introductions\Builders\MethodBuilder.cs:47`); and the type `IServiceProvider<IProjectService>` of `IExecutionContext.ServiceProvider` (FW27 `Project\IExecutionContext.cs:27`).

### 0.3 Terms

| Term | Meaning |
|---|---|
| Call site | An `InvocationExpressionSyntax` or an `AwaitExpressionSyntax` of the source compilation. |
| Method-reference site | A method group of the source compilation that the compiler converts to a delegate or to a function pointer, for example `Transform` in `list.Select( Transform )` (section [5.3.11](05a-api-registration.md#5311-kinds-of-method-use)). |
| Accessor site | A use of a property or an event of the source compilation that calls an accessor: a read, a write, a compound assignment, an increment or a decrement, or `+=` and `-=` on an event (section [5.3.13](05a-api-registration.md#5313-accessors)). A compound assignment is one accessor site with two accessor uses: a get and a set. |
| Site, use site | One occurrence that a registration can intercept: a call, a method reference, an accessor use, or an await. It is a call site, a method-reference site or an accessor site. The rewrite, the grouping and the diagnostics apply to every kind. |
| Scope | Registration level. The calling-side region that the user selects: the compilation, a namespace, a type or a member (R2). A site is in scope when its origin is contained in the scope (section [5.3.2](05a-api-registration.md#532-scope-semantics)). |
| Target selection | Registration level. The declaring type, or a predicate over declaring types, plus the member names of a member registration (section [5.3.3](05a-api-registration.md#533-target-selection-for-members)). An await registration has no target selection: its scope selects the await expressions, and the provider skips the ones that it does not want (section [5.3.4](05a-api-registration.md#534-await-registrations)). |
| Target | The called side: the invoked method, the accessor of a property or an event, or the `await` operator. |
| Origin | Site level. The declaration that contains the site: a member, an accessor, the field or property of an initializer, the type for primary-constructor base arguments, or the entry point for top-level statements. Code in lambdas and local functions belongs to the enclosing member. The contexts expose it as `InterceptionContext.Origin` (section [5.5.1](05b-api-providers-contexts-results.md#551-interceptioncontext)). |
| Destination | Site level. The member that the site uses: the definition of the method to which the site binds, or of the property or event whose accessor it calls. It can be an override of the member that the target selection selected. For an await, the destination is the awaitable type. The contexts expose it as `MethodInterceptionContext.Destination` and `AwaitInterceptionContext.AwaitableType` (section [5.5](05b-api-providers-contexts-results.md#55-interception-contexts)). |
| Calling type | The innermost type that contains the site: the origin itself when the origin is a type, and otherwise the declaring type of the origin. |
| Interceptor provider | The user object or delegate that chooses the interceptor for a site. It receives an interception context and returns a result. It implements `IMethodInterceptorProvider` or `IAwaitInterceptorProvider`. Earlier versions of this document called it the interceptor implementation. |
| Interceptor, interceptor method | The method that the rewritten site calls or references, as in Roslyn: an existing method, or a method synthesized from a template. |
| Intercepted method | The method to which the original call binds. For an accessor site, it is the accessor. |
| Placement | Where a synthesized interceptor is declared: a type (a type placement), the generated static class, or a local function of the origin (a local-function placement). The type of a type placement is the placement type. The placement `BaseMostAccessibleType()` is a type placement whose type the engine computes for each calling type (section [6.5.6](06b-signatures-and-validation.md#656-base-most-accessible-type)). The public type is `InterceptorPlacement` (section [5.6.3](05b-api-providers-contexts-results.md#563-interceptorplacement)). Earlier versions of this document called it the container. |
| Group | A set of sites that share one synthesized interceptor method. |
| Representative | The first site of a group in deterministic order. Its registration provides the contribution origin used to synthesize the method. |
| Source stage | The high-level pipeline stage whose aspects start from the source compilation. |

Origin and destination have the same meaning as the ends of a reference in reference validation (`ReferenceEndRole.Origin` and `ReferenceEndRole.Destination`). The contexts expose them as plain `IDeclaration`, `IMember` and `IType` values, not as `ReferenceEnd`. `ReferenceEnd` belongs to Metalama.Extensions.Validation, on which interceptors do not depend (section [9.2](09-premium-engine.md#92-sharing-with-metalamaextensionsvalidation-r5)). Its contract also throws when code reads a level that is finer than the granularity of the validator. That contract would restrict providers, which always see one site (section [5.5.4](05b-api-providers-contexts-results.md#554-relationship-with-referencevalidationcontext)).

The direction words of validators, inbound and outbound, are not used for interceptors. A registration always selects sites by their origin (the scope) and by their destination (the target selection). A registration that an aspect makes on the target itself is called a target-side registration. It is a future direction (section [16.7](16-future-directions.md#167-target-side-registration)).

R8 asks for a template container. This design realizes it as the placement of the interceptor (interpretation I10). The open-source factory uses the same word: a synthesized method is declared at a `SynthesizedMethodPlacement` (section [10.4.4](10a-oss-bridge-hook-factory.md#1044-synthesized-methods-and-proceed-bindings)), and the premium engine resolves each `InterceptorPlacement` into one.

The contribution origin (`ExtensionContributionOrigin`, section [10.2.3](10a-oss-bridge-hook-factory.md#1023-contribution-origin)) is a different concept. It records the aspect or the fabric that made a contribution, for attribution and ordering. In this document, the word origin without a qualifier means the origin of a site.

### 0.4 Terminology aligned with Roslyn interceptors

The names of this design follow the vocabulary of C# interceptors where the concepts match. In the Roslyn feature document, an interceptor is a method that substitutes a call to an interceptable method with a call to itself, and it declares the locations of the calls that it intercepts (RCDOCS `features\interceptors.md:8`). The location of a call is the simple name that denotes the interceptable method, and the public API that computes it is `GetInterceptableLocation` (RCDOCS `features\interceptors.md:84, 88`).

| Metalama term | Roslyn term | Remark |
|---|---|---|
| Interceptor, interceptor method | Interceptor | The same concept: the method that the call site calls after the substitution. |
| Intercepted method | Interceptable method | Roslyn names the method by its capability. Metalama names it by what happens to it at one call site. |
| Call site | Intercepted call; its location is the interceptable location | Metalama identifies a call site by its syntax node. Roslyn identifies it by a file checksum and a position. |
| Method-reference site | No equivalent | C# interceptors apply only to invocations. A method group converted to a delegate is not an interceptable location. |
| Interceptor provider (`IMethodInterceptorProvider`, `IAwaitInterceptorProvider`) | No equivalent | A C# interceptor is chosen by the attribute that the author writes. A Metalama interceptor is chosen per call site by user code. |
| Registration (`InterceptMethods`, `InterceptAccessors`, `InterceptAwaits`) | `[InterceptsLocation]` attribute | The attribute names one location. A registration selects sites with a scope, a declaring type and member names. |
| Placement (`InterceptorPlacement`) | No equivalent | The author of a C# interceptor declares it in a type of their choice. A Metalama provider chooses, for each site, where a synthesized interceptor is declared. |
| Accessor interceptor | No equivalent | C# interceptors apply only to invocations. A property access is not an interceptable location. |
| Await interceptor | No equivalent | C# interceptors apply only to invocations (section [4.2](04-mechanism.md#42-comparison)). |

Metalama interceptors rewrite call sites in the syntax tree during linking. They are not C# interceptors: Metalama does not emit `[InterceptsLocation]`, and the compiler does not perform the substitution (section [4.3](04-mechanism.md#43-decision)). The shared vocabulary does not imply shared rules. For example, a C# interceptor must match the signature of the interceptable method almost exactly (RCDOCS `features\interceptors.md:149-160`), whereas a Metalama interceptor accepts implicit conversions (R9).
