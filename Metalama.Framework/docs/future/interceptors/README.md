# Call-site interceptors: design

> Status: design document only. The feature is not implemented, and no code of the open-source repository implements it. The document is stored in the open-source repository under `Metalama.Framework/docs/future/interceptors/`, the folder that holds designs of future features that are not implemented and may never be implemented (`Metalama.Framework/docs/future/README.md`). It was merged by the lead architect from seven part designs on 2026-09-24, and revised on the same day after an adversarial review by seven reviewers (Appendix C) and after a product-owner review and its follow-up on receiver mapping, the signature builder, defaults and `params` (Appendix D). A second product-owner batch on the same day changed the terminology, the linker rewrite, the template-side access and the expressions of the contexts, made method references converted to delegates part of version 1, added an in-repository proof of concept, and added the future directions of section [16](16-future-directions.md#16-future-directions) (Appendix D, second batch). A third product-owner batch on 2026-09-25 replaced the reference predicates with target selection by declaring type and member names, withdrew the extraction of `Metalama.Extensions.References`, added the interception of property and event accessors (`InterceptAccessors`), and added sketches for indexers and operators (Appendix D, third batch). A fourth product-owner batch on the same day changed the terminology: the container of a synthesized interceptor became its placement, the sites gained an origin and a destination, and inbound interception became target-side registration (Appendix D, fourth batch). A fifth product-owner batch on the same day removed the awaited type from await registrations, relaxed the result type of await interceptors, restricted await templates to template names, and added the placement `InterceptorPlacement.BaseMostAccessibleType()` (Appendix D, fifth batch). A sixth product-owner batch on the same day replaced `IMethodInterceptorBuilder` with a parameter-binding API shared by existing and synthesized interceptors, added parameters whose values the provider supplies for each site, the pull of values from the origin member with a guard, the caller's instance as a binding source, the open-source template type `AnyAwaitable`, and new sketches of section [16](16-future-directions.md#16-future-directions) (Appendix D, sixth batch).
> Target line: 2027.0 (`develop/2027.0`). The 2026.1 worktree is the reference code base for most line numbers of the linker and the premium engine; section [0.2](00-conventions.md#02-evidence-prefixes) lists where the lines come from.
> Audience: the Metalama engineers who implement the feature, and the product owner who takes the decisions of section [15](15-decisions.md#15-decisions-needed-from-the-product-owner).

This folder holds the design of call-site interceptors, split into one document per part. Start with the [overview](overview.md). The documents keep the section numbers of the original single document, so a reference such as "section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping)" or "full 6.4.1" has the same meaning in every document, and it links to the right place.

## Reading paths

| Reader | Documents |
|---|---|
| Product owner | [overview.md](overview.md), [01-summary.md](01-summary.md), [15-decisions.md](15-decisions.md), [16-future-directions.md](16-future-directions.md) |
| Premium engine implementer | [05a](05a-api-registration.md), [05b](05b-api-providers-contexts-results.md), [05c](05c-api-templates.md), [05d](05d-api-samples.md), [06a](06a-call-site-model.md), [06b](06b-signatures-and-validation.md), [07](07-await-interception.md), [08](08-deduplication-and-naming.md), [09](09-premium-engine.md) |
| Open-source implementer | [03-background.md](03-background.md), [10a](10a-oss-bridge-hook-factory.md), [10b](10b-oss-linker-and-templates.md), [10c](10c-oss-reference-graph-design-time.md), [12-test-plan.md](12-test-plan.md) |
| Reviewer | [appendix-b-weak-spots.md](appendix-b-weak-spots.md), [appendix-c-review-log.md](appendix-c-review-log.md), [appendix-d-product-owner-reviews.md](appendix-d-product-owner-reviews.md) |

## Documents

| Document | Content | Lines |
|---|---|---|
| [overview.md](overview.md) | Condensed overview for the product owner and the engineering leads. | 809 |
| [00-conventions.md](00-conventions.md) | Conventions, evidence prefixes and terms | 98 |
| [01-summary.md](01-summary.md) | Summary | 221 |
| [02-requirements.md](02-requirements.md) | Requirements, interpretations and traceability | 99 |
| [03-background.md](03-background.md) | Background: the existing architecture | 113 |
| [04-mechanism.md](04-mechanism.md) | Mechanism decision | 32 |
| [05a-api-registration.md](05a-api-registration.md) | User-facing API: overview and registration | 1013 |
| [05b-api-providers-contexts-results.md](05b-api-providers-contexts-results.md) | User-facing API: providers, contexts, results and binding | 1737 |
| [05c-api-templates.md](05c-api-templates.md) | User-facing API: template side | 260 |
| [05d-api-samples.md](05d-api-samples.md) | User-facing API: samples | 1318 |
| [06a-call-site-model.md](06a-call-site-model.md) | Call-site semantics: model and rules table | 646 |
| [06b-signatures-and-validation.md](06b-signatures-and-validation.md) | Call-site semantics: signatures, placements, validation and caller information | 1063 |
| [07-await-interception.md](07-await-interception.md) | Await interception | 491 |
| [08-deduplication-and-naming.md](08-deduplication-and-naming.md) | Deduplication and naming | 160 |
| [09-premium-engine.md](09-premium-engine.md) | Premium engine architecture | 1075 |
| [10a-oss-bridge-hook-factory.md](10a-oss-bridge-hook-factory.md) | Open-source extension points: adviser bridge, pipeline hook and factory | 1154 |
| [10b-oss-linker-and-templates.md](10b-oss-linker-and-templates.md) | Open-source extension points: linker and template engine | 674 |
| [10c-oss-reference-graph-design-time.md](10c-oss-reference-graph-design-time.md) | Open-source extension points: reference graph, design time, helpers and file list | 489 |
| [11-delivery-plan.md](11-delivery-plan.md) | Delivery plan | 163 |
| [12-test-plan.md](12-test-plan.md) | Test plan | 447 |
| [13-documentation-plan.md](13-documentation-plan.md) | Documentation plan | 61 |
| [14-risks.md](14-risks.md) | Risk register | 52 |
| [15-decisions.md](15-decisions.md) | Decisions needed from the product owner | 86 |
| [16-future-directions.md](16-future-directions.md) | Future directions | 335 |
| [appendix-a-evidence.md](appendix-a-evidence.md) | Appendix A. Evidence index | 19 |
| [appendix-b-weak-spots.md](appendix-b-weak-spots.md) | Appendix B. Weak spots | 51 |
| [appendix-c-review-log.md](appendix-c-review-log.md) | Appendix C. Review log | 168 |
| [appendix-d-product-owner-reviews.md](appendix-d-product-owner-reviews.md) | Appendix D. Product-owner reviews | 149 |

## Section index

| Section | Document |
|---|---|
| [0. How to read this document](00-conventions.md#0-how-to-read-this-document) | 00-conventions.md |
| [0.1 Structure](00-conventions.md#01-structure) | 00-conventions.md |
| [0.2 Evidence prefixes](00-conventions.md#02-evidence-prefixes) | 00-conventions.md |
| [0.3 Terms](00-conventions.md#03-terms) | 00-conventions.md |
| [0.4 Terminology aligned with Roslyn interceptors](00-conventions.md#04-terminology-aligned-with-roslyn-interceptors) | 00-conventions.md |
| [1. Summary](01-summary.md#1-summary) | 01-summary.md |
| [1.1 What](01-summary.md#11-what) | 01-summary.md |
| [1.2 Why](01-summary.md#12-why) | 01-summary.md |
| [1.3 How](01-summary.md#13-how) | 01-summary.md |
| [1.4 What the open-source repository receives](01-summary.md#14-what-the-open-source-repository-receives) | 01-summary.md |
| [1.5 What the premium package implements](01-summary.md#15-what-the-premium-package-implements) | 01-summary.md |
| [1.6 Main decisions](01-summary.md#16-main-decisions) | 01-summary.md |
| [1.7 Resolved design conflicts](01-summary.md#17-resolved-design-conflicts) | 01-summary.md |
| [1.8 Challenges to the baseline that this design adopts](01-summary.md#18-challenges-to-the-baseline-that-this-design-adopts) | 01-summary.md |
| [1.9 Top decisions for the product owner](01-summary.md#19-top-decisions-for-the-product-owner) | 01-summary.md |
| [2. Requirements, interpretations and traceability](02-requirements.md#2-requirements-interpretations-and-traceability) | 02-requirements.md |
| [2.1 Requirements](02-requirements.md#21-requirements) | 02-requirements.md |
| [2.2 Interpretations](02-requirements.md#22-interpretations) | 02-requirements.md |
| [2.3 Traceability](02-requirements.md#23-traceability) | 02-requirements.md |
| [2.4 Gaps found in the baseline](02-requirements.md#24-gaps-found-in-the-baseline) | 02-requirements.md |
| [3. Background: the existing architecture](03-background.md#3-background-the-existing-architecture) | 03-background.md |
| [3.1 Pipeline stages and extension hooks](03-background.md#31-pipeline-stages-and-extension-hooks) | 03-background.md |
| [3.2 Premium reference validators](03-background.md#32-premium-reference-validators) | 03-background.md |
| [3.3 Reference index](03-background.md#33-reference-index) | 03-background.md |
| [3.4 Linker and call-site advice](03-background.md#34-linker-and-call-site-advice) | 03-background.md |
| [3.5 Templates and proceed](03-background.md#35-templates-and-proceed) | 03-background.md |
| [3.6 Advising channels](03-background.md#36-advising-channels) | 03-background.md |
| [3.7 Design time](03-background.md#37-design-time) | 03-background.md |
| [3.8 Roslyn fork and compiler order](03-background.md#38-roslyn-fork-and-compiler-order) | 03-background.md |
| [4. Mechanism decision](04-mechanism.md#4-mechanism-decision) | 04-mechanism.md |
| [4.1 Options](04-mechanism.md#41-options) | 04-mechanism.md |
| [4.2 Comparison](04-mechanism.md#42-comparison) | 04-mechanism.md |
| [4.3 Decision](04-mechanism.md#43-decision) | 04-mechanism.md |
| [5. User-facing API of the premium package](05a-api-registration.md#5-user-facing-api-of-the-premium-package) | 05a-api-registration.md |
| [5.1 The API at a glance](05a-api-registration.md#51-the-api-at-a-glance) | 05a-api-registration.md |
| [5.2 Seam between the public API and the engine](05a-api-registration.md#52-seam-between-the-public-api-and-the-engine) | 05a-api-registration.md |
| [5.3 Registration](05a-api-registration.md#53-registration) | 05a-api-registration.md |
| [5.4 Interceptor provider interfaces](05b-api-providers-contexts-results.md#54-interceptor-provider-interfaces) | 05b-api-providers-contexts-results.md |
| [5.5 Interception contexts](05b-api-providers-contexts-results.md#55-interception-contexts) | 05b-api-providers-contexts-results.md |
| [5.6 Result model](05b-api-providers-contexts-results.md#56-result-model) | 05b-api-providers-contexts-results.md |
| [5.7 Template-side API](05c-api-templates.md#57-template-side-api) | 05c-api-templates.md |
| [5.8 Samples](05d-api-samples.md#58-samples) | 05d-api-samples.md |
| [6. Call-site semantics and signature derivation for invocations](06a-call-site-model.md#6-call-site-semantics-and-signature-derivation-for-invocations) | 06a-call-site-model.md |
| [6.1 Where it runs, and components](06a-call-site-model.md#61-where-it-runs-and-components) | 06a-call-site-model.md |
| [6.2 The call-site model](06a-call-site-model.md#62-the-call-site-model) | 06a-call-site-model.md |
| [6.3 Rules table](06a-call-site-model.md#63-rules-table) | 06a-call-site-model.md |
| [6.4 Signature derivation](06b-signatures-and-validation.md#64-signature-derivation) | 06b-signatures-and-validation.md |
| [6.5 Placement admissibility](06b-signatures-and-validation.md#65-placement-admissibility) | 06b-signatures-and-validation.md |
| [6.6 Signature validation: existing methods and adjusted signatures (R9)](06b-signatures-and-validation.md#66-signature-validation-existing-methods-and-adjusted-signatures-r9) | 06b-signatures-and-validation.md |
| [6.7 Caller-information materialization](06b-signatures-and-validation.md#67-caller-information-materialization) | 06b-signatures-and-validation.md |
| [7. Await interception](07-await-interception.md#7-await-interception) | 07-await-interception.md |
| [7.1 Which await sites are targets](07-await-interception.md#71-which-await-sites-are-targets) | 07-await-interception.md |
| [7.2 Site analysis](07-await-interception.md#72-site-analysis) | 07-await-interception.md |
| [7.3 Awaitable kinds and resumption classes](07-await-interception.md#73-awaitable-kinds-and-resumption-classes) | 07-await-interception.md |
| [7.4 Runtime rules and evaluation of the rewrite shapes](07-await-interception.md#74-runtime-rules-and-evaluation-of-the-rewrite-shapes) | 07-await-interception.md |
| [7.5 The adaptive rewrite (challenge to B8, adopted)](07-await-interception.md#75-the-adaptive-rewrite-challenge-to-b8-adopted) | 07-await-interception.md |
| [7.6 Modes](07-await-interception.md#76-modes) | 07-await-interception.md |
| [7.7 Task type of an async interceptor and target frameworks](07-await-interception.md#77-task-type-of-an-async-interceptor-and-target-frameworks) | 07-await-interception.md |
| [7.8 Semantics preserved and changed by mode Await](07-await-interception.md#78-semantics-preserved-and-changed-by-mode-await) | 07-await-interception.md |
| [7.9 Template model](07-await-interception.md#79-template-model) | 07-await-interception.md |
| [7.10 Existing methods and placements for awaits](07-await-interception.md#710-existing-methods-and-placements-for-awaits) | 07-await-interception.md |
| [7.11 Internal components](07-await-interception.md#711-internal-components) | 07-await-interception.md |
| [8. Deduplication and naming](08-deduplication-and-naming.md#8-deduplication-and-naming) | 08-deduplication-and-naming.md |
| [8.1 Two stages](08-deduplication-and-naming.md#81-two-stages) | 08-deduplication-and-naming.md |
| [8.2 The key](08-deduplication-and-naming.md#82-the-key) | 08-deduplication-and-naming.md |
| [8.3 Canonical types and type parameters](08-deduplication-and-naming.md#83-canonical-types-and-type-parameters) | 08-deduplication-and-naming.md |
| [8.4 Proceed target identity](08-deduplication-and-naming.md#84-proceed-target-identity) | 08-deduplication-and-naming.md |
| [8.5 Implementation identity and the user contract](08-deduplication-and-naming.md#85-implementation-identity-and-the-user-contract) | 08-deduplication-and-naming.md |
| [8.6 Representative order](08-deduplication-and-naming.md#86-representative-order) | 08-deduplication-and-naming.md |
| [8.7 Names and accessibility](08-deduplication-and-naming.md#87-names-and-accessibility) | 08-deduplication-and-naming.md |
| [8.8 Where deduplication happens](08-deduplication-and-naming.md#88-where-deduplication-happens) | 08-deduplication-and-naming.md |
| [9. Premium engine architecture](09-premium-engine.md#9-premium-engine-architecture) | 09-premium-engine.md |
| [9.1 Assemblies, packaging and licensing](09-premium-engine.md#91-assemblies-packaging-and-licensing) | 09-premium-engine.md |
| [9.2 Sharing with Metalama.Extensions.Validation (R5)](09-premium-engine.md#92-sharing-with-metalamaextensionsvalidation-r5) | 09-premium-engine.md |
| [9.3 Engine components](09-premium-engine.md#93-engine-components) | 09-premium-engine.md |
| [9.4 Registration](09-premium-engine.md#94-registration) | 09-premium-engine.md |
| [9.5 Compile-time run](09-premium-engine.md#95-compile-time-run) | 09-premium-engine.md |
| [9.6 Cost model and shared binding](09-premium-engine.md#96-cost-model-and-shared-binding) | 09-premium-engine.md |
| [9.7 Design time](09-premium-engine.md#97-design-time) | 09-premium-engine.md |
| [9.8 Preview, live templates and introspection](09-premium-engine.md#98-preview-live-templates-and-introspection) | 09-premium-engine.md |
| [9.9 Cross-project interception](09-premium-engine.md#99-cross-project-interception) | 09-premium-engine.md |
| [9.10 Pipeline edge cases](09-premium-engine.md#910-pipeline-edge-cases) | 09-premium-engine.md |
| [9.11 Diagnostics catalog](09-premium-engine.md#911-diagnostics-catalog) | 09-premium-engine.md |
| [10. Open-source extension points](10a-oss-bridge-hook-factory.md#10-open-source-extension-points) | 10a-oss-bridge-hook-factory.md |
| [10.1 Overview](10a-oss-bridge-hook-factory.md#101-overview) | 10a-oss-bridge-hook-factory.md |
| [10.2 Adviser bridge (B2a)](10a-oss-bridge-hook-factory.md#102-adviser-bridge-b2a) | 10a-oss-bridge-hook-factory.md |
| [10.3 Transforming pipeline hook (B2b)](10a-oss-bridge-hook-factory.md#103-transforming-pipeline-hook-b2b) | 10a-oss-bridge-hook-factory.md |
| [10.4 Extension transformation factory (B2c)](10a-oss-bridge-hook-factory.md#104-extension-transformation-factory-b2c) | 10a-oss-bridge-hook-factory.md |
| [10.5 Linker changes](10b-oss-linker-and-templates.md#105-linker-changes) | 10b-oss-linker-and-templates.md |
| [10.6 Template engine changes (B2d)](10b-oss-linker-and-templates.md#106-template-engine-changes-b2d) | 10b-oss-linker-and-templates.md |
| [10.7 Reference graph (B2e)](10c-oss-reference-graph-design-time.md#107-reference-graph-b2e) | 10c-oss-reference-graph-design-time.md |
| [10.8 Design-time plumbing (B2f)](10c-oss-reference-graph-design-time.md#108-design-time-plumbing-b2f) | 10c-oss-reference-graph-design-time.md |
| [10.9 Small public helpers (B2g)](10c-oss-reference-graph-design-time.md#109-small-public-helpers-b2g) | 10c-oss-reference-graph-design-time.md |
| [10.10 Other open-source fixes found on the way](10c-oss-reference-graph-design-time.md#1010-other-open-source-fixes-found-on-the-way) | 10c-oss-reference-graph-design-time.md |
| [10.11 Open-source file list and size](10c-oss-reference-graph-design-time.md#1011-open-source-file-list-and-size) | 10c-oss-reference-graph-design-time.md |
| [11. Delivery plan](11-delivery-plan.md#11-delivery-plan) | 11-delivery-plan.md |
| [11.1 Principles](11-delivery-plan.md#111-principles) | 11-delivery-plan.md |
| [11.2 Fix track](11-delivery-plan.md#112-fix-track) | 11-delivery-plan.md |
| [11.3 Milestones](11-delivery-plan.md#113-milestones) | 11-delivery-plan.md |
| [11.4 Dependencies and parallel tracks](11-delivery-plan.md#114-dependencies-and-parallel-tracks) | 11-delivery-plan.md |
| [11.5 Mapping to release trains](11-delivery-plan.md#115-mapping-to-release-trains) | 11-delivery-plan.md |
| [11.6 Engineering conventions](11-delivery-plan.md#116-engineering-conventions) | 11-delivery-plan.md |
| [12. Test plan](12-test-plan.md#12-test-plan) | 12-test-plan.md |
| [12.1 Principles](12-test-plan.md#121-principles) | 12-test-plan.md |
| [12.2 Open-source unit tests (`Metalama.Framework.Tests.UnitTests`)](12-test-plan.md#122-open-source-unit-tests-metalamaframeworktestsunittests) | 12-test-plan.md |
| [12.3 Open-source linker tests (`Metalama.Framework.Tests.LinkerTests`)](12-test-plan.md#123-open-source-linker-tests-metalamaframeworktestslinkertests) | 12-test-plan.md |
| [12.4 Open-source template tests (`Metalama.Framework.Tests.TemplateTests`)](12-test-plan.md#124-open-source-template-tests-metalamaframeworkteststemplatetests) | 12-test-plan.md |
| [12.5 In-repository proof of concept of interceptors](12-test-plan.md#125-in-repository-proof-of-concept-of-interceptors) | 12-test-plan.md |
| [12.6 Regression tests of the fix track](12-test-plan.md#126-regression-tests-of-the-fix-track) | 12-test-plan.md |
| [12.7 Premium aspect tests (`Metalama.Extensions.Interceptors.AspectTests` and `.5.0.0`)](12-test-plan.md#127-premium-aspect-tests-metalamaextensionsinterceptorsaspecttests-and-500) | 12-test-plan.md |
| [12.8 Premium runtime-execution tests (`Runtime/`)](12-test-plan.md#128-premium-runtime-execution-tests-runtime) | 12-test-plan.md |
| [12.9 Semantic-equivalence corpus test](12-test-plan.md#129-semantic-equivalence-corpus-test) | 12-test-plan.md |
| [12.10 Premium unit and analyzer tests (`Metalama.Extensions.Interceptors.UnitTests` and `.5.0.0`)](12-test-plan.md#1210-premium-unit-and-analyzer-tests-metalamaextensionsinterceptorsunittests-and-500) | 12-test-plan.md |
| [12.11 Preview tests](12-test-plan.md#1211-preview-tests) | 12-test-plan.md |
| [12.12 Memory-leak tests](12-test-plan.md#1212-memory-leak-tests) | 12-test-plan.md |
| [12.13 License-failure test](12-test-plan.md#1213-license-failure-test) | 12-test-plan.md |
| [12.14 Cross-project and multi-stage tests](12-test-plan.md#1214-cross-project-and-multi-stage-tests) | 12-test-plan.md |
| [12.15 Performance benchmark](12-test-plan.md#1215-performance-benchmark) | 12-test-plan.md |
| [12.16 Test matrix](12-test-plan.md#1216-test-matrix) | 12-test-plan.md |
| [13. Documentation plan](13-documentation-plan.md#13-documentation-plan) | 13-documentation-plan.md |
| [13.1 Conceptual chapter](13-documentation-plan.md#131-conceptual-chapter) | 13-documentation-plan.md |
| [13.2 Samples](13-documentation-plan.md#132-samples) | 13-documentation-plan.md |
| [13.3 API reference](13-documentation-plan.md#133-api-reference) | 13-documentation-plan.md |
| [13.4 Updates to existing articles](13-documentation-plan.md#134-updates-to-existing-articles) | 13-documentation-plan.md |
| [13.5 Metalama.Migration](13-documentation-plan.md#135-metalamamigration) | 13-documentation-plan.md |
| [13.6 Engineering documents](13-documentation-plan.md#136-engineering-documents) | 13-documentation-plan.md |
| [14. Risk register](14-risks.md#14-risk-register) | 14-risks.md |
| [15. Decisions needed from the product owner](15-decisions.md#15-decisions-needed-from-the-product-owner) | 15-decisions.md |
| [15.1 Top decisions](15-decisions.md#151-top-decisions) | 15-decisions.md |
| [15.2 Other decisions](15-decisions.md#152-other-decisions) | 15-decisions.md |
| [16. Future directions](16-future-directions.md#16-future-directions) | 16-future-directions.md |
| [16.1 Composition of several interceptors on one site](16-future-directions.md#161-composition-of-several-interceptors-on-one-site) | 16-future-directions.md |
| [16.2 Delegate-based handler interceptors](16-future-directions.md#162-delegate-based-handler-interceptors) | 16-future-directions.md |
| [16.3 Other method uses without invocation syntax](16-future-directions.md#163-other-method-uses-without-invocation-syntax) | 16-future-directions.md |
| [16.4 Accessor mode for inaccessible targets](16-future-directions.md#164-accessor-mode-for-inaccessible-targets) | 16-future-directions.md |
| [16.5 Indexers](16-future-directions.md#165-indexers) | 16-future-directions.md |
| [16.6 Operators](16-future-directions.md#166-operators) | 16-future-directions.md |
| [16.7 Target-side registration](16-future-directions.md#167-target-side-registration) | 16-future-directions.md |
| [16.8 The Proceed and PackedArguments sources: interception without a template](16-future-directions.md#168-the-proceed-and-packedarguments-sources-interception-without-a-template) | 16-future-directions.md |
| [16.9 Pull strategies at the registration and at the site](16-future-directions.md#169-pull-strategies-at-the-registration-and-at-the-site) | 16-future-directions.md |
| [16.10 Caller-side argument providers](16-future-directions.md#1610-caller-side-argument-providers) | 16-future-directions.md |
| [16.11 Functions and locals that enclose a site](16-future-directions.md#1611-functions-and-locals-that-enclose-a-site) | 16-future-directions.md |
| [Appendix A. Evidence index](appendix-a-evidence.md#appendix-a-evidence-index) | appendix-a-evidence.md |
| [Appendix B. Weak spots that remain after the review](appendix-b-weak-spots.md#appendix-b-weak-spots-that-remain-after-the-review) | appendix-b-weak-spots.md |
| [Appendix C. Review log](appendix-c-review-log.md#appendix-c-review-log) | appendix-c-review-log.md |
| [Appendix D. Product-owner reviews of 2026-09-24 and 2026-09-25](appendix-d-product-owner-reviews.md#appendix-d-product-owner-reviews-of-2026-09-24-and-2026-09-25) | appendix-d-product-owner-reviews.md |
