# Background: the existing architecture

> Part of the [call-site interceptors design](README.md). Previous: [02-requirements.md](02-requirements.md) | Next: [04-mechanism.md](04-mechanism.md). Evidence prefixes and terms: [00-conventions.md](00-conventions.md).

## 3. Background: the existing architecture

This section describes only what exists today. Every item is EXISTING.

### 3.1 Pipeline stages and extension hooks

Extensions are discovered through the assembly-level `ExportExtensionAttribute` (SDK27 `Extensibility\ExportExtensionAttribute.cs:21-47`). The loader reads the `MetalamaExtensionAssembly` items and filters them by target framework and Roslyn version (ENG26 `Extensibility\ExtensionLoader.cs:26-30`; `ExtensionLoaderBase.cs:29-38`). Built-in extensions are appended after the user extensions (ENG26 `Pipeline\AspectPipeline.cs:442-487`). `PipelineExtension.Initialize` runs before `ServiceBuilder.Build`, so services registered there are visible from compile-time user code through `IProject.ServiceProvider` (ENG26 `Pipeline\AspectPipeline.cs:247-287`; `CodeModel\ProjectModel.cs:95`).

The hooks of `PipelineExtension` run at these moments:

| Moment | Hook | Inputs | Output | Evidence |
|---|---|---|---|---|
| After `BuildAspect`, for outcomes other than Error and Ignore | `ExecuteContributorsAsync` | All contributors of the aspect, the compilation before the aspect | Diagnostics merged into the aspect result | ENG26 `Aspects\AspectDriver.cs:277-306` |
| Once per pipeline execution, for fabric contributors | `ExecuteContributorsAsync` | The initial compilation | Diagnostics | ENG26 `Pipeline\AspectPipeline.cs:709-737` |
| End of each high-level stage, compile time, before the linker | `ExecutePipelineContributorsAsync` | Stage contributors, first and last compilation of the stage | Transitive contributors and diagnostics only | ENG27 `Pipeline\CompileTime\LinkerPipelineStage.cs:44-56`; ENG26 `Extensibility\ExtensionPipelineContributorsResult.cs:12-18` |
| End of each design-time stage, only when an extension contributor exists | `ExecuteDesignTimePipelineContributorsAsync` | Same | Transitive contributors, filed by syntax tree | ENG26 `Pipeline\DesignTime\DesignTimePipelineStage.cs:33-58` |
| Each analyzed file, design time | `AnalyzeSemanticModel` | Configuration, semantic model, design-time extension collection, aspect repository | Diagnostics | DT27 `DiagnosticAnalysis\TheDiagnosticAnalyzer.cs:191-211` |
| Writing a manifest, compile time | `GetTransitiveManifestExtensions` | Transitive contributors | Manifest extensions | ENG26 `Pipeline\CompileTime\CompileTimeAspectPipeline.cs:333-351` |
| Reading a referenced manifest | `GetPipelineContributorsFromTransitiveManifest` | Manifest | Contributors | ENG26 `Aspects\TransitivePipelineContributorSource.cs:55, 203-209` |

Facts that constrain the design:

- The linker input is built only from the aspect transformations of the stage. Extensions have no channel to the linker (ENG27 `Pipeline\CompileTime\LinkerPipelineStage.cs:59-66`).
- The stage code carries a TODO that says that validators run once per high-level stage (ENG27 `Pipeline\CompileTime\LinkerPipelineStage.cs:42`). A later stage starts from a model built from the linked output of the previous stage (ENG26 `Pipeline\HighLevelPipelineStage.cs:35-44`; `Pipeline\AspectPipelineResult.cs:63-77`).
- The first high-level stage starts from the initial model of the source `PartialCompilation` (ENG26 `Pipeline\AspectPipeline.cs:744-750`; `Pipeline\PipelineStepsState.cs:76`).
- Fabric contributors are stored in the pipeline configuration and replayed in every stage (ENG26 `Pipeline\AspectPipeline.cs:639-644`; `Pipeline\PipelineStepsState.cs:99`).
- Extension contributors of an aspect with outcome Error or Ignore are discarded (ENG26 `Pipeline\ExecuteAspectLayerPipelineStep.cs:135-147`).
- Extension contributors accumulate in a `ConcurrentLinkedList`, so their order is not deterministic (ENG26 `Pipeline\PipelineStepsState.cs:46, 492-498`).
- Preview, live templates and introspection use `LinkerPipelineStage` (ENG26 `Pipeline\DesignTime\PreviewAspectPipeline.cs:31-34`; `Pipeline\LiveTemplates\LiveTemplateAspectPipeline.cs:79-82`; `Introspection\IntrospectionAspectPipeline.cs:32-33`). The WPF precompile stage and the low-level stages call no extension hook (ENG26 `Pipeline\CompileTime\WpfPrecompilePipelineStage.cs:29-85`).
- Low-level weavers come only from compile-time plug-ins (ENG26 `Pipeline\AspectPipeline.cs:489-509`).

### 3.2 Premium reference validators

Reference validators are the closest existing feature (R5).

- The public API resolves an internal `IProjectService` from `query.Project.ServiceProvider` (P26 `Metalama.Extensions.Validation\ReferenceValidationQueryExtensions.cs:29-37`; `IReferenceValidationQueryService.cs:12`). The engine registers the implementation in `Initialize` (P26 `Metalama.Extensions.Validation.Engine\ValidationPipelineExtension.cs:28-40`).
- Registration adds an `IValidatorSource` contributor to the query owner. A delegate must point to a uniquely named method of the owner type (P26 `Queries\ValidationReferenceValidationQueryService.cs:84-111`).
- Factory-based validators are created lazily per selected declaration through `UserCodeInvoker`, and the tag is consumed at that point (P26 `Queries\DynamicReferenceValidatorQuerySource.cs:40-47`).
- Aspects register validators through `builder.Outbound`, which is an `IQuery`, not through `IAdviser` (P26 `Metalama.Extensions.Architecture\Aspects\ExperimentalAttribute.cs:34`; FW27 `Aspects\IAspectBuilder.cs:240`).
- At compile time, the runner indexes every tree of the source compilation concurrently with an `InboundReferenceIndexBuilder`, and binds the source compilation to the final aspect repository (P26 `ReferenceValidatorRunner.cs:59-88`; `ValidationRunner.cs:52`).
- Validators are invoked once per granularity group (P26 `ReferenceValidatorRunner.cs:113-146`). `ReferenceEnd` throws when code reads a finer level than the declared granularity (P26 `Metalama.Extensions.Validation\ReferenceEnd.cs:81-89`).
- At design time, the pipeline converts validators into durable `DesignTimeReferenceValidatorInstance` objects filed by syntax tree, and the analyzer runs them per semantic model (P26 `DesignTimeReferenceValidatorInstance.cs:36`; `DesignTimeReferenceValidatorRunner.cs:24, 37-51`).
- Transitive validators are serialized into the manifest and recreated in consumers from an `IRef` and a method name (P26 `TransitiveValidatorInstance.cs:81-96`).
- Licensing is enforced only at MSBuild time. The `VerifyMetalamaLicense` task consumes one `MetalamaExtensionLicenseRequirement` per `MetalamaPremiumComponent` item, whatever its name (P26 `Metalama.Licensing\build\Metalama.Licensing.targets:36-50`; P27 `Metalama.Licensing\build\Metalama.Licensing.targets:82-90`; `Metalama.Licensing.BuildTasks\VerifyMetalamaLicense.cs:80-85`). The eligible products are defined by `MetalamaExtensionLicenseRequirement`, not by the targets (SharpCrafters.Backstage `Metalama.Backstage\Licensing\MetalamaExtensionLicenseRequirement.cs:19-30`).

Defects found in the validator code are listed as fixes F4, F5, F7 and F8 in section [11.2](11-delivery-plan.md#112-fix-track).

### 3.3 Reference index

- The walker records an invocation on the simple-name node of the invoked expression, with `ReferenceKinds.Invocation` (ENG26 `ReferenceGraph\ReferenceIndexWalker.cs:103, 137-152`). `ReferenceKinds.Invocation` also covers delegate invocations (FW27 `Code\ReferenceKinds.cs:114-117`).
- A method group that is not the expression of an invocation, such as `Transform` in `list.Select( Transform )`, the operand of `new Action( M )`, the right operand of `e += M`, or the operand of `&M`, is a standalone identifier or member access. The walker records it with the current reference kind, which is `ReferenceKinds.Default` outside an assignment target or a `nameof` argument (ENG27 `ReferenceGraph\ReferenceIndexWalker.cs:35, 103, 110-113, 811-842, 888-897`). The symbol is the method that the conversion selects, because `GetSymbolInfo` returns it for a converted method group (line 822).
- Roslyn represents a method group converted to a delegate as an `IDelegateCreationOperation` whose `Target` is an `IMethodReferenceOperation`, and a method group converted to a function pointer as an `IAddressOfOperation` over an `IMethodReferenceOperation`. The syntax of the `IMethodReferenceOperation` is the method-group expression (RC `Operations\CSharpOperationFactory.cs:1068-1080, 1191-1209`; RC `..\Core\Portable\Generated\Operations.Generated.cs:964, 2026-2031, 2119`). An event subscription `e += M` is an `IEventAssignmentOperation` whose `HandlerValue` is that delegate creation, with `Adds` true for `+=` and false for `-=` (same file, lines 1582-1590).
- Symbols are normalized to `OriginalDefinition` (ENG26 `ReferenceGraph\ReferenceIndexBuilder.cs:20-21`). A reduced extension call is keyed by `ReducedExtensionMethodSymbol`, whose `OriginalDefinition` is itself (RC `Symbols\ReducedExtensionMethodSymbol.cs:377-380`).
- Calls in lambdas and local functions are attributed to the enclosing member. Initializers are attributed to the field, property or event. References in compile-time types are not indexed (ENG26 `ReferenceGraph\ReferenceIndexWalker.cs:297-318, 858-885`).
- The walker filters by identifier before binding, and it creates the semantic model lazily per tree (ENG27 `ReferenceGraph\ReferenceIndexWalker.cs:814-816, 948-960`; `ReferenceIndexerOptions.cs:191-209`). The identifier filter applies to a reference kind unless the requirement removes that kind from the filtered kinds. Only a requirement for an assembly-level declaration kind (`Compilation` or `AssemblyReference`) does so (ENG27 `ReferenceGraph\ReferenceIndexerOptions.cs:80-84, 107-125`).
- The walker has no `VisitAwaitExpression`, no `VisitGenericName`, and does not visit collection-expression elements, constructor-initializer arguments or array rank sizes (ENG27 `ReferenceGraph\ReferenceIndexWalker.cs:524-535, 674-702`).
- An internal `OutboundReferenceIndexBuilder` exists and is used only by introspection (ENG27 `ReferenceGraph\OutboundReferenceIndexBuilder.cs:18, 52-66`; `Introspection\References\ProjectReferenceGraph.cs:103`).
- `ReferenceKinds` bit 27 is `UnionCaseType` in 2027.0 (FW27 `Code\ReferenceKinds.cs:176`).

### 3.4 Linker and call-site advice

- The linker has three sequential steps: injection, analysis and linking. Its input, `AspectLinkerInput`, is internal (ENG26 `Linking\AspectLinker.cs:102-117`; `Linking\AspectLinkerInput.cs:15, 35-71`).
- All code-model versions of a stage share one `PartialCompilation` (ENG26 `CodeModel\CompilationModel.cs:333`). A source node found in the stage-input compilation is therefore the object that `LinkerInjectionStep.Rewriter` visits (ENG26 `Linking\LinkerInjectionStep.cs:335-366`).
- A call-site advice mechanism exists for `[OnInitialized]`. It walks the intermediate compilation, indexes call sites by the containing semantic body, and applies `SyntaxNodeSubstitution` objects. `CallSiteAdviceInfo` is the documented place to add new kinds (DOCS26 `linker-callsite.md:1-22, 174-188`; ENG26 `Linking\CallSiteAdviceInfo.cs:7-21`).
- Substitutions are keyed by the pair of the inlining context and the intermediate node. `AddSubstitution` throws on any conflict except redirection versus aspect-reference override (ENG26 `Linking\LinkerAnalysisStep.SubstitutionGenerator.cs:826-861`).
- The substituting rewriter applies substitutions bottom-up. `Substitute` receives the detached node with its children already rewritten, and works on syntax only (ENG26 `Linking\LinkerRewritingDriver.SubstitutingRewriter.cs:25-43`; `Linking\Substitution\SyntaxNodeSubstitution.cs:22-34`).
- `OnInitialized` substitutions are registered only for non-inlined bodies (ENG27 `Linking\LinkerAnalysisStep.SubstitutionGenerator.cs:125`). The finder skips expression-bodied properties, primary-constructor base arguments and top-level statements (ENG26 `Linking\LinkerAnalysisStep.OnInitializedCallSiteFinder.cs:94-144`).
- Several linker paths copy intermediate initializers raw when they move them (ENG26 `Linking\LinkerRewritingDriver.Constructors.cs:267, 275, 286, 382, 387`; `LinkerRewritingDriver.Properties.cs:137, 180`).
- The injection rewriter does not visit field declarators, event-field declarators or the base list of a type (ENG26 `Linking\LinkerInjectionStep.Rewriter.cs:369, 1397-1473, 1807-1846`).
- The linking rewriter handles only type declarations. Top-level statements are never substituted (ENG26 `Linking\LinkerLinkingStep.LinkingRewriter.cs:37-140`).
- `CallerMemberSubstitution` fixes `CallerMemberName` arguments for calls from overridden source bodies (ENG26 `Linking\LinkerAnalysisStep.cs:1151-1240`; `Linking\Substitution\CallerMemberSubstitution.cs:40-66`).
- An aggregation precedent exists for inserted statements: `IAggregatableInsertStatementTransformation.AggregateKey` (ENG26 `Transformations\IInsertStatementTransformation.cs:52-60`).
- Linker, transformation and template types are internal. `InternalsVisibleTo` is granted only to test projects (ENG26 `Metalama.Framework.Engine.csproj:42-49`).

### 3.5 Templates and proceed

- `TemplateExpansionContext` accepts a pluggable `Func<TemplateKind, IUserExpression>` proceed provider, which is called lazily with the effective template kind (ENG26 `Templating\TemplateExpansionContext.cs:42, 169-200, 744-752`; `TemplateExpansionContext.ProceedUserExpression.cs:29-36, 65`).
- `ProceedHelper` decides awaiting, `__AsyncVoidMethod` wrapping and buffering from the method that it receives, and reports that method's return type as the proceed type (ENG26 `Transformations\ProceedHelper.cs:26-113`).
- `ForOverride` binds run-time template parameters by name, then by ordinal from index 0. `ForIntroduction` binds by index (ENG26 `Advising\TemplateBindingHelper.cs:61-103, 569-641`). A leading receiver parameter breaks both.
- Template selection is an instance method of `AdviceFactory<T>` and depends on the target's `IsAsync` and on `UseAsyncTemplateForAnyAwaitable` (ENG26 `Advising\AdviceFactory.cs:212-338`, line 261).
- Template-internal local functions keep `meta.Target` on the enclosing member (ENG26 `Templating\TemplateExpansionContext.cs:268-281, 757`).
- Compile-time code reads the expansion's service provider through `MetalamaExecutionContext.Current.ServiceProvider` (ENG26 `Utilities\UserCode\UserCodeExecutionContext.cs:352`; FW27 `Project\IExecutionContext.cs:27`).
- The static class `meta` resolves the internal `IMetaApi` of the current expansion through `MetalamaExecutionContext.CurrentInternal.MetaApi` (FW27 `Aspects\meta.cs:60`; `Aspects\IMetaApi.cs:16`). A called template gets a `TemplateExpansionContext` built from its caller's context (ENG27 `Templating\TemplateExpansionContext.cs:283-297, 759-760`), and the base constructor copies the caller's `MetaApi` (ENG27 `Utilities\UserCode\UserCodeExecutionContext.cs:254-269`).
- The template compiler classifies the members of `meta` by name. A name that is not special-cased gets `MetaMemberKind.Default` (ENG27 `Templating\TemplateMemberClassifier.cs:107-131`). Extension blocks declared in a compile-time type can be called from templates (TST27 `Metalama.Framework.Tests.AspectTests\Tests\Aspects\CSharp14\ExtensionMembers\ExtensionMembers_CompileTimeExtensionMembers.cs`, issue #1932).
- Source expressions of the code model are `SourceUserExpression` objects, which implement the public `ISourceExpression` (ENG27 `Templating\Expressions\SourceUserExpression.cs:18`; FW27 `Code\ISourceExpression.cs:10-32`). `SourceField`, `SourceProperty` and `SourceEvent` create them for initializers (ENG27 `CodeModel\Source\SourceField.cs:149`; `SourceProperty.cs:123`; `SourceEvent.cs:203`). When the target type differs, `ToSyntax` adds a cast; otherwise it returns the source node itself (ENG27 `Templating\Expressions\SourceUserExpression.cs:31-41`).
- `ConfigureAwaitUserExpression.Type` looks up `ConfigureAwait(bool)` with a null-forgiving operator (ENG27 `Templating\TemplateExpansionContext.ConfigureAwaitUserExpression.cs:31-39`).
- `LexicalScopeFactory` returns an empty scope for builders in a type without syntax (ENG27 `Linking\LexicalScopeFactory.cs:115-119`, marked "TODO: Temp hack").

### 3.6 Advising channels

- `IAdviser` has six implementations. Every advice verb reaches the engine by casting to the internal `IAdviserInternal` and passing `adviser.Target` explicitly (FW27 `Aspects\AdviserExtensions.cs:1912`; `Advising\IAdviserInternal.cs:7-10`).
- `AdviceFactory<T>` is internal and has no owner. For a type fabric, the factory is derived from the aggregate aspect builder, so `AdviceFactory.AddAspect` attributes child aspects to the aggregate `FabricAspect<T>` instance (ENG27 `Fabrics\TypeFabricDriver.cs:143`; `Advising\AdviceFactory.cs:2325`).
- `Adviser<T>.With` creates a new adviser around the same factory. Its containment test compares the declaration with itself, so it always passes (FW27 `Aspects\AdviserExtensions.cs:2106-2113`, line 2109).
- A premium type cannot implement `IAdviceResult`, which is `[InternalImplement]` (FW26 `Advising\IAdviceResult.cs:57-58`).
- `ITypeAmender` implements both `IAmender<INamedType>` and `IAdviser<INamedType>` (FW27 `Fabrics\ITypeAmender.cs:34`). The framework resolves the resulting ambiguity for `AddAspect` with `ITypeAmender` overloads (FW27 `Aspects\AdviserExtensions.cs:1944-1967`).
- `IQueryImpl.InvokeAsync` reports a diagnostic for any selected declaration that is external, or that is not contained in the containing declaration of the query root, which is the declaration on which the query was created. The containing declaration is the namespace itself for a namespace root, and the closest named type for a type or member root. A compilation root is not checked (ENG27 `Queries\Query.cs:470-492`).

### 3.7 Design time

- `SplitResultsByTree` converts each contributor with `ToDesignTime()` and files it by `DocumentKey`. A document of the project that is not dirty is skipped, so the earlier result survives (issue #1768). A default key goes to a default bucket (DT27 `Pipeline\DesignTimeAspectPipelineResult.cs:545-610`).
- The design-time manifest serializes the design-time form of every own extension (ENG27 `Extensibility\DesignTimeAspectPipelineResultExtensionCollection.cs:82-85`). Any own extension makes `HasTransitiveAspectManifestContent` true (DT27 `Pipeline\DesignTimeAspectPipelineResult.cs:801-805`).
- Design-time validators of referenced projects are merged into the collection, keyed by the validated symbol (ENG27 `Extensibility\DesignTimeAspectPipelineResultExtensionCollection.cs:53-78, 120-128`).
- The analysis process is long-lived. Roslyn creates a new `Compilation` for each keystroke. Long-lived objects must not reference compilations, syntax trees, symbols or code models (DOCS27 `design-time-memory.md:28-47, 269-305`). `PipelineExtension` and `IDesignTimePipelineResultExtension` carry `[Durable]` (ENG26 `Extensibility\PipelineExtension.cs:22`; `Extensibility\IDesignTimePipelineResultExtension.cs:44-48`).
- `UserCodeRetentionAnalyzer` walks the design-time form of every compile-time transitive contributor (ENG27 `Pipeline\UserCodeRetentionAnalyzer.cs:66`; ENG26 `Pipeline\UserCodeRetentionAnalyzer.cs:196, 224-253`).

### 3.8 Roslyn fork and compiler order

- The 2027.0 fork is based on Roslyn 5.11.0 and contains the unmodified C# interceptor implementation (prior-art analysis; Metalama.Compiler `eng\Versions.props:23`).
- The fork runs Metalama before source generators. Generators receive the transformed compilation (Metalama.Compiler `src\Compilers\Core\Portable\CommandLine\CommonCompiler.cs:1522-1703, 1706-1717`). Metalama never sees generator output at build time.
- Caller-information values are computed from the tree that is compiled, which is the transformed tree (RC `Binder\Binder_Invocation.cs:1716-1733`).
- The linker replaces a tree only when its root changed (ENG26 `Linking\LinkerInjectionStep.cs:352-358`). Any file that Metalama modifies invalidates the content-hash locations of `[InterceptsLocation]` attributes that target it (CS9234).
- Nodes without a source mapping receive hidden sequence points (Metalama.Compiler `src\Compilers\CSharp\Portable\CodeGen\CodeGenerator.cs:536-567`).
