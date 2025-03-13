// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Engine.AdditionalOutputs;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Pipeline.CompileTime;

/// <summary>
/// The implementation of <see cref="AspectPipeline"/> used at compile time.
/// </summary>
public class CompileTimeAspectPipeline : AspectPipeline
{
    public CompileTimeAspectPipeline(
        ProjectServiceProvider serviceProvider,
        ExecutionScenario? executionScenario = null ) : base(
        serviceProvider,
        executionScenario ?? ExecutionScenario.CompileTime ) { }

    protected override SyntaxGenerationOptions GetSyntaxGenerationOptions()
    {
        var projectOptions = this.ServiceProvider.GetRequiredService<IProjectOptions>();

        return new SyntaxGenerationOptions( projectOptions.CodeFormattingOptions );
    }

    private bool VerifyLanguageVersion( Compilation compilation, IDiagnosticAdder diagnosticAdder )
    {
        // Note that Roslyn does not properly set the language version at design time, so we don't check the language version
        // in other pipelines.

        var languageVersion = ((CSharpParseOptions?) compilation.SyntaxTrees.FirstOrDefault()?.Options)?.LanguageVersion.MapSpecifiedToEffectiveVersion()
                              ?? SupportedCSharpVersions.Default;

        if ( languageVersion == LanguageVersion.Preview )
        {
            if ( !this.ProjectOptions.AllowPreviewLanguageFeatures )
            {
                diagnosticAdder.Report(
                    GeneralDiagnosticDescriptors.PreviewCSharpVersionNotSupported.CreateRoslynDiagnostic(
                        null,
                        SupportedCSharpVersions.FormatSupportedVersions() ) );

                return false;
            }
        }
        else if ( !SupportedCSharpVersions.All.Contains( languageVersion ) )
        {
            diagnosticAdder.Report(
                GeneralDiagnosticDescriptors.CSharpVersionNotSupported.CreateRoslynDiagnostic(
                    null,
                    (languageVersion.ToDisplayString(), "LangVersion", SupportedCSharpVersions.FormatSupportedVersions()) ) );

            return false;
        }

        return true;
    }

    public async Task<FallibleResult<CompileTimeAspectPipelineResult>> ExecuteAsync(
        Action<Diagnostic>? reportDiagnostic,
        Action<ScopedSuppression>? reportSuppression,
        Compilation compilation,
        ImmutableArray<ManagedResource> resources,
        TestableCancellationToken cancellationToken = default )
    {
        reportDiagnostic ??= _ => { };

        var compilationContext = this.ServiceProvider.GetRequiredService<ClassifyingCompilationContextFactory>().GetInstance( compilation );
        var partialCompilation = PartialCompilation.CreateComplete( compilation );

        // Skip if Metalama has been disabled for this project.
        if ( !this.ProjectOptions.IsFrameworkEnabled )
        {
            return new CompileTimeAspectPipelineResult(
                ImmutableArray<SyntaxTreeTransformation>.Empty,
                ImmutableArray<ManagedResource>.Empty,
                partialCompilation,
                ImmutableArray<AdditionalCompilationOutputFile>.Empty,
                ImmutableArray<ScopedSuppression>.Empty,
                null );
        }

        // Report error if the compilation does not have the METALAMA preprocessor symbol.
        if ( !(compilation.SyntaxTrees.FirstOrDefault()?.Options.PreprocessorSymbolNames.Contains( "METALAMA" ) ?? false) )
        {
            reportDiagnostic( GeneralDiagnosticDescriptors.MissingMetalamaPreprocessorSymbol.CreateRoslynDiagnostic( null, default ) );

            return default;
        }

        // Validate the code (some validations are not done by the template compiler).
        var isTemplatingCodeValidatorSuccessful = await TemplatingCodeValidator.ValidateAsync(
            this.ServiceProvider,
            compilationContext,
            reportDiagnostic,
            reportSuppression,
            cancellationToken );

        if ( !isTemplatingCodeValidatorSuccessful )
        {
            return default;
        }

        var diagnosticAdder = new DiagnosticAdderAdapter( reportDiagnostic );

        if ( !this.VerifyLanguageVersion( compilation, diagnosticAdder ) )
        {
            return default;
        }

        // Initialize the pipeline and generate the compile-time project.
        if ( !this.TryInitialize( diagnosticAdder, partialCompilation.Compilation, null, cancellationToken, out var configuration ) )
        {
            return default;
        }

        // Run the pipeline.
        return await this.ExecuteCoreAsync(
            diagnosticAdder,
            partialCompilation,
            resources,
            configuration,
            cancellationToken );
    }

    private async Task<FallibleResult<CompileTimeAspectPipelineResult>> ExecuteCoreAsync(
        IDiagnosticAdder diagnosticAdder,
        PartialCompilation compilation,
        ImmutableArray<ManagedResource> resources,
        AspectPipelineConfiguration configuration,
        TestableCancellationToken cancellationToken )
    {
        try
        {
            // Execute the pipeline.
            var result = await this.ExecuteAsync( compilation, diagnosticAdder, configuration, cancellationToken );

            if ( !result.IsSuccessful )
            {
                return default;
            }

            var resultPartialCompilation = result.Value.LastCompilation;

            // Format the output.
            if ( this.ProjectOptions.CodeFormattingOptions == CodeFormattingOptions.Formatted || this.ProjectOptions.WriteHtml )
            {
                if ( !this.ProjectOptions.IsTest )
                {
                    diagnosticAdder.Report( GeneralDiagnosticDescriptors.CodeFormattingEnabled.CreateRoslynDiagnostic( null, default ) );
                }

                var codeFormatter = this.ServiceProvider.GetRequiredService<CodeFormatter>();

                // ReSharper disable once AccessToModifiedClosure
                resultPartialCompilation = await codeFormatter.FormatAsync( resultPartialCompilation, cancellationToken );
            }

            // Write HTML (used only when building projects for documentation).
            if ( this.ProjectOptions.WriteHtml )
            {
                // We must simulate add the syntax trees added at design time to avoid some C# linking errors.
                var introducedSyntaxTrees = await DesignTimeSyntaxTreeGenerator.GenerateDesignTimeSyntaxTreesAsync(
                    this.ServiceProvider,
                    compilation,
                    result.Value.FirstCompilationModel.AssertNotNull(),
                    result.Value.LastCompilationModel,
                    result.Value.Transformations.OfType<ITransformation>(),
                    new UserDiagnosticSink( configuration.ServiceProvider ),
                    cancellationToken );

                var compilationWithDesignTimeTrees = (PartialCompilation)
                    compilation
                        .AddSyntaxTrees( introducedSyntaxTrees.SelectAsReadOnlyCollection( t => t.GeneratedSyntaxTree ) );

                await HtmlCodeWriter.WriteAllDiffAsync(
                    this.ProjectOptions,
                    this.ServiceProvider,
                    compilationWithDesignTimeTrees,
                    resultPartialCompilation,
                    result.Value.Diagnostics.ReportedDiagnostics );
            }

            // Add managed resources.
            ImmutableArray<ManagedResource> additionalResources;

            if ( resultPartialCompilation.Resources.IsDefaultOrEmpty )
            {
                additionalResources = ImmutableArray<ManagedResource>.Empty;
            }
            else
            {
                additionalResources = resultPartialCompilation.Resources.Where( r => !resources.Contains( r ) ).ToImmutableArray();
            }

            if ( configuration.CompileTimeProject is { IsEmpty: false } )
            {
                additionalResources = additionalResources.Add( configuration.CompileTimeProject.ToResource() );
            }

            // Create a manifest for transitive aspects and validators.
            var inheritableOptions =
                result.Value.FirstCompilationModel.AssertNotNull()
                    .HierarchicalOptionsManager.AssertNotNull()
                    .GetInheritableOptions( result.Value.LastCompilationModel, false )
                    .ToImmutableDictionary();

            var annotations = result.Value.LastCompilationModel.GetExportedAnnotations();

            // Execute validators.
            IReadOnlyList<ITransitivePipelineContributor> transitiveContributors = result.Value.TransitiveContributors;

            if ( result.Value.ExternallyInheritableAspects.Length > 0 || transitiveContributors.Count > 0 || inheritableOptions.Count > 0
                 || !annotations.IsEmpty )
            {
                var pipelineExtensions = configuration.Extensions;

                var manifestExtensions = pipelineExtensions.SelectMany( e => e.GetTransitiveManifestExtensions( transitiveContributors ) )
                    .ToImmutableArray();

                var inheritedAspectsManifest = TransitiveAspectsManifest.Create(
                    result.Value.ExternallyInheritableAspects.Select( i => new InheritableAspectInstance( i ) )
                        .ToImmutableArray(),
                    manifestExtensions,
                    inheritableOptions,
                    annotations );

                var resource = inheritedAspectsManifest.ToResource( configuration.ServiceProvider, resultPartialCompilation.CompilationContext );
                additionalResources = additionalResources.Add( resource );
            }

            var resultingCompilation =
                (PartialCompilation) await RunTimeAssemblyRewriter.RewriteAsync( resultPartialCompilation, configuration.ServiceProvider );

            var syntaxTreeTransformations = resultingCompilation.ToTransformations();

            return new CompileTimeAspectPipelineResult(
                syntaxTreeTransformations,
                additionalResources,
                resultingCompilation,
                result.Value.AdditionalCompilationOutputFiles,
                result.Value.Diagnostics.DiagnosticSuppressions,
                configuration );
        }
        catch ( DiagnosticException exception ) when ( exception.InSourceCode )
        {
            foreach ( var diagnostic in exception.Diagnostics )
            {
                diagnosticAdder.Report( diagnostic );
            }

            return default;
        }
    }

    private protected override LowLevelPipelineStage CreateLowLevelStage( PipelineStageConfiguration configuration )
    {
        var partData = configuration.AspectLayers.Single();

        return new LowLevelPipelineStage( configuration.Weaver!, partData.AspectClass );
    }

    private protected override HighLevelPipelineStage CreateHighLevelStage(
        PipelineStageConfiguration configuration,
        CompileTimeProject compileTimeProject )
        => new LinkerPipelineStage( configuration.AspectLayers );
}