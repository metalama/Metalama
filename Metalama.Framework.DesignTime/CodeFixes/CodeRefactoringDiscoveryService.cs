// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.Services;
using Metalama.Framework.Engine.Configuration;
using Metalama.Framework.Engine.Licensing;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Metalama.Framework.DesignTime.CodeFixes;

/// <summary>
/// Implementation of <see cref="ICodeRefactoringDiscoveryService"/>, which runs in the analysis process.
/// </summary>
public sealed class CodeRefactoringDiscoveryService : ICodeRefactoringDiscoveryService
{
    private readonly ILogger _logger;
    private readonly DesignTimeAspectPipelineFactory _pipelineFactory;
    private readonly DesignTimeConfiguration _licensingConfiguration;
    private readonly WorkspaceProvider _workspaceProvider;

    internal CodeRefactoringDiscoveryService( GlobalServiceProvider serviceProvider )
    {
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( "CodeRefactoring" );
        this._pipelineFactory = serviceProvider.GetRequiredService<DesignTimeAspectPipelineFactory>();
        this._licensingConfiguration = serviceProvider.GetRequiredBackstageService<IConfigurationManager>().Get<DesignTimeConfiguration>();
        this._workspaceProvider = serviceProvider.GetRequiredService<WorkspaceProvider>();
    }

    public async Task<ComputeRefactoringResult> ComputeRefactoringsAsync(
        ProjectKey projectKey,
        string syntaxTreePath,
        TextSpan span,
        CancellationToken cancellationToken )
    {
        var project = await this._workspaceProvider.GetProjectAsync( projectKey, cancellationToken );

        if ( project == null )
        {
            this._logger.Warning?.Log( $"ComputeRefactorings('{projectKey}', '{syntaxTreePath}'): cannot get the project '{projectKey}'." );

            return ComputeRefactoringResult.Empty;
        }

        var pipeline = this._pipelineFactory.GetOrCreatePipeline( project, cancellationToken.ToTestable() );

        if ( pipeline == null )
        {
            this._logger.Warning?.Log( $"ComputeRefactorings('{projectKey}', '{syntaxTreePath}'): cannot get the pipeline for project '{projectKey}'." );

            return ComputeRefactoringResult.Empty;
        }

        var compilation = await this._workspaceProvider.GetCompilationAsync( projectKey, cancellationToken );

        if ( compilation == null )
        {
            this._logger.Warning?.Log( $"ComputeRefactorings('{projectKey}', '{syntaxTreePath}'): cannot get the compilation for project '{projectKey}'." );

            return ComputeRefactoringResult.Empty;
        }

        if ( !compilation.GetIndexedSyntaxTrees().TryGetValue( syntaxTreePath, out var syntaxTree ) )
        {
            this._logger.Warning?.Log(
                $"ComputeRefactorings('{projectKey}', '{syntaxTreePath}'): cannot get the SyntaxTree '{syntaxTreePath}' in project '{projectKey}'." );

            return ComputeRefactoringResult.Empty;
        }

        var node = (await syntaxTree.GetRootAsync( cancellationToken )).FindNode( span );

        var semanticModel = compilation.GetCachedSemanticModel( syntaxTree );

        var symbol = semanticModel.GetDeclaredSymbol( node, cancellationToken );

        switch ( symbol )
        {
            case null:
                if ( node is CompilationUnitSyntax )
                {
                    // For the compilation unit, show aspects for the assembly.
                    symbol = compilation.Assembly;

                    break;
                }
                else
                {
                    return ComputeRefactoringResult.Empty;
                }

            case { Kind: SymbolKind.Alias }:
                // Aliases never have any refactorings.
                return ComputeRefactoringResult.Empty;

            case { Kind: SymbolKind.Namespace }:
                // Namespaces don't have attributes, so show aspects for the assembly instead.
                symbol = compilation.Assembly;

                break;
        }

        var eligibleAspects = pipeline.GetEligibleAspects( compilation, symbol, cancellationToken.ToTestable() );

        var licenseVerifier = pipeline.ServiceProvider.GetService<LicenseVerifier>();

        var menuBuilder = new CodeActionMenuBuilder();

        foreach ( var aspect in eligibleAspects )
        {
            var targetSymbolId = SymbolId.Create( symbol, cancellationToken );

            if ( aspect.EditorExperienceOptions.SuggestAsAddAttribute ?? true )
            {
                var fullTitle = aspect.EditorExperienceOptions.AddAttributeSuggestionTitle ?? $"Add Aspect|Add [{aspect.ShortName}]";

                menuBuilder.AddItem(
                    fullTitle,
                    title =>
                        new AddAspectAttributeCodeActionModel(
                            aspect.FullName,
                            targetSymbolId,
                            syntaxTreePath,
                            title ) );
            }

            if ( aspect.EditorExperienceOptions.SuggestAsLiveTemplate.GetValueOrDefault() && (!this._licensingConfiguration.HideUnlicensedCodeActions
                                                                                              || licenseVerifier == null
                                                                                              || licenseVerifier.VerifyCanApplyCodeFix( aspect )) )
            {
                var fullTitle = aspect.EditorExperienceOptions.LiveTemplateSuggestionTitle ?? $"Apply live template|{aspect.ShortName}";

                menuBuilder.AddItem(
                    fullTitle,
                    title =>
                        new ApplyLiveTemplateCodeActionModel(
                            aspect.FullName,
                            targetSymbolId,
                            syntaxTreePath,
                            title ) );
            }
        }

        return new ComputeRefactoringResult( menuBuilder.Build() );
    }
}