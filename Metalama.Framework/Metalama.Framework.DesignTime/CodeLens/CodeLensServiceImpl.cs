// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.DesignTime.Contracts.CodeLens;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.DesignTime.Preview;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Introspection;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Introspection;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.DesignTime.CodeLens;

internal sealed class CodeLensServiceImpl : PreviewPipelineBasedService, ICodeLensServiceImpl
{
    private static readonly ImmutableArray<CodeLensDetailsHeader> _detailsHeaders = ImmutableArray.Create(
        new CodeLensDetailsHeader( "Aspect Class", "AspectShortName", width: 0.2 ),
        new CodeLensDetailsHeader( "Aspect Target", "TargetDeclaration", width: 0.2 ),
        new CodeLensDetailsHeader( "Aspect Origin", "Origin", width: 0.2 ),
        new CodeLensDetailsHeader( "Transformation", "Transformation", width: 0.4 ) );

    private readonly ILogger _logger;

    public CodeLensServiceImpl( GlobalServiceProvider serviceProvider ) : base( serviceProvider )
    {
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( "CodeLens" );
    }

    private sealed record CodePointData(
        string FilePath,
        ISymbol Symbol,
        DesignTimeAspectPipelineResult PipelineResult );

    private async ValueTask<CodePointData?> GetCodePointDataAsync(
        ProjectKey projectKey,
        SerializableDeclarationId symbolId,
        TestableCancellationToken cancellationToken )
    {
        var project = await this.WorkspaceProvider.GetProjectAsync( projectKey, cancellationToken );

        if ( project == null )
        {
            return null;
        }

        var pipeline = this.PipelineFactory.GetOrCreatePipeline( project, cancellationToken );

        if ( pipeline == null )
        {
            return null;
        }

        var pipelineResult = pipeline.AspectPipelineResult;

        var compilation = await project.GetCompilationAsync( cancellationToken );

        if ( compilation == null )
        {
            this._logger.Trace?.Log( $"Cannot return code lens info for '{projectKey}' because the pipeline has no active compilation." );

            return null;
        }

        var nullableSymbol = symbolId.ResolveToSymbolOrNull( compilation.GetCompilationContext() );

        if ( nullableSymbol == null )
        {
            this._logger.Warning?.Log( $"Cannot return code lens info for symbol '{symbolId}' in '{projectKey}' because the symbol could not be resolved." );

            return null;
        }

        var symbol = nullableSymbol;

        var filePath = symbol.GetPrimarySyntaxReference()?.SyntaxTree.FilePath;

        if ( filePath == null )
        {
            this._logger.Warning?.Log(
                $"Cannot return code lens info for symbol '{symbolId}' in '{projectKey}' because the symbol has no primary syntax tree." );

            return null;
        }

        return new CodePointData( filePath, symbol, pipelineResult );
    }

    public async Task<CodeLensSummary> GetCodeLensSummaryAsync(
        ProjectKey projectKey,
        SerializableDeclarationId symbolId,
        TestableCancellationToken cancellationToken )
    {
        var codePointData = await this.GetCodePointDataAsync( projectKey, symbolId, cancellationToken );

        if ( codePointData == null )
        {
            return CodeLensSummary.NoAspect;
        }

        // Try to get a description from the symbol classifier.
        if ( TryGetSummaryFromSymbolClassifier( codePointData, out var summaryFromSymbolClassifier ) )
        {
            return summaryFromSymbolClassifier;
        }

        // If we have a plain method, display the number of target aspects.
        if ( !codePointData.PipelineResult.SyntaxTreeResults.TryGetValue( codePointData.FilePath, out var syntaxTreeResult ) )
        {
            this._logger.Trace?.Log( $"Cannot return code lens info for symbol '{symbolId}' in '{projectKey}' because there is no result for this symbol." );

            return CodeLensSummary.NotAvailable;
        }

        var aspectInstances = syntaxTreeResult.AspectInstances
            .Where( i => !i.IsSkipped )
            .Where( i => i.TargetDeclarationId == symbolId )
            .Select( i => i.AspectClassFullName )
            .ToReadOnlyList();

        var transformations = syntaxTreeResult.Transformations
            .Where( t => t.TargetDeclarationId == symbolId )
            .Select( t => t.AspectClassFullName )
            .ToReadOnlyList();

        var distinctAspects = aspectInstances.Concat( transformations ).Distinct().ToReadOnlyList();
        var distinctAspectCount = distinctAspects.Count;

        this._logger.Trace?.Log( $"There are {distinctAspectCount} distinct aspect(s) affecting '{codePointData.Symbol}'." );

        var (text, tooltip) = distinctAspectCount switch
        {
            0 => ("0 aspects", "This declaration is not affected by any aspect."),
            1 => ("1 aspect", $"This declaration is affected by the aspect '{distinctAspects.Single()}'."),
            _ => ($"{distinctAspectCount} aspects",
                  $"This declaration is affected by the aspects {string.Join( ", ", distinctAspects.SelectAsReadOnlyList( i => $"'{i}'" ) )}.")
        };

        return new CodeLensSummary( text, tooltip );
    }

    private static bool TryGetSummaryFromSymbolClassifier(
        CodePointData codePointData,
        [NotNullWhen( true )] out CodeLensSummary? summary )
    {
        var symbolClassificationService = codePointData.PipelineResult.Configuration?.ServiceProvider.GetRequiredService<ISymbolClassificationService>();

        if ( symbolClassificationService == null )
        {
            summary = null;

            return false;
        }

        var symbol = codePointData.Symbol;

        string? executionScopeString = null;

        if ( symbolClassificationService.IsTemplate( symbol ) )
        {
            executionScopeString = "template";
        }
        else
        {
            var executionScope = symbolClassificationService.GetExecutionScope( symbol );

            if ( executionScope != ExecutionScope.RunTime )
            {
                if ( executionScope == ExecutionScope.CompileTime )
                {
                    executionScopeString = "compile-time";
                }
                else if ( symbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.AllInterfaces.Any( t => t.Name == nameof(IAspect) ) )
                {
                    {
                        summary = new CodeLensSummary( MetalamaStringFormatter.Format( $"aspect class" ) );

                        return true;
                    }
                }
                else
                {
                    executionScopeString = "both run-time and compile-time";
                }
            }
        }

        if ( executionScopeString != null )
        {
            {
                summary = new CodeLensSummary( MetalamaStringFormatter.Format( $"{executionScopeString} {symbol.Kind}" ) );

                return true;
            }
        }

        summary = null;

        return false;
    }

    public async Task<ICodeLensDetailsTable> GetCodeLensDetailsAsync(
        ProjectKey projectKey,
        SerializableDeclarationId symbolId,
        TestableCancellationToken cancellationToken )
    {
        var codePointData = await this.GetCodePointDataAsync( projectKey, symbolId, cancellationToken );

        if ( codePointData == null )
        {
            return CodeLensDetailsTable.CreateError( "Something went wrong." );
        }

        // Execute the pipeline.
        var preparation = await this.PrepareExecutionAsync( projectKey, codePointData.FilePath, cancellationToken );

        if ( !preparation.Success )
        {
            return CodeLensDetailsTable.CreateError( preparation.ErrorMessages! );
        }

        var pipeline = new IntrospectionAspectPipeline(
            preparation.ServiceProvider.AssertNotNull(),
            null );

        var result = await pipeline.ExecuteAsync( preparation.PartialCompilation!, preparation.Configuration!, cancellationToken );

        // Index aspects and transformations.
        var aspectInstances = result.AspectInstances
            .Where( i => !i.IsSkipped )
            .Where( i => i.TargetDeclaration.TryGetSerializableId( out var id ) && id == symbolId )
            .ToDictionary( i => i, _ => new List<IIntrospectionTransformation>() );

        var transformations = result.Transformations.Where( t => t.TargetDeclaration.TryGetSerializableId( out var id ) && id == symbolId );

        foreach ( var transformation in transformations )
        {
            var advice = transformation.Advice;

            if ( advice.AspectInstance.IsSkipped )
            {
                // Ignore transformations from skipped aspects.
                continue;
            }

            var aspectInstance = advice.AspectInstance;

            if ( !aspectInstances.TryGetValue( aspectInstance, out var transformationList ) )
            {
                aspectInstances[aspectInstance] = transformationList = new List<IIntrospectionTransformation>();
            }

            transformationList.Add( transformation );
        }

        // Create the logical table.
        List<CodeLensDetailsEntry> entries = new();

        static CodeLensDetailsField CreateOriginField( IIntrospectionAspectInstance aspectInstance )
        {
            if ( aspectInstance.Predecessors.IsEmpty )
            {
                // This should not happen.
                return new CodeLensDetailsField( "-" );
            }

            var predecessor = aspectInstance.Predecessors[0];

            FormattableString text = predecessor.Kind switch
            {
                AspectPredecessorKind.Attribute => $"Custom attribute",
                AspectPredecessorKind.Fabric => $"Fabric '{((IIntrospectionFabric) predecessor.Instance).FullName}'",
                AspectPredecessorKind.Inherited => $"Inherited from '{((IIntrospectionAspectInstance) predecessor.Instance).TargetDeclaration}'",
                AspectPredecessorKind.ChildAspect =>
                    $"Child of '{((IIntrospectionAspectInstance) predecessor.Instance).AspectClass}' on '{((IIntrospectionAspectInstance) predecessor.Instance).TargetDeclaration}'",
                AspectPredecessorKind.RequiredAspect =>
                    $"Required by '{((IIntrospectionAspectInstance) predecessor.Instance).AspectClass}' on '{((IIntrospectionAspectInstance) predecessor.Instance).TargetDeclaration}'",
                _ => $""
            };

            return new CodeLensDetailsField( MetalamaStringFormatter.Format( text ) );
        }

        IIntrospectionAspectInstance? previousAspectInstance = null;

        void AddEntry( IIntrospectionAspectInstance aspectInstance, string transformation )
        {
            var aspectClass = aspectInstance.AspectClass.ShortName;
            var targetDeclaration = aspectInstance.TargetDeclaration.ToDisplayString();
            var origin = CreateOriginField( aspectInstance );

            // If we are repeating the previous aspect instance, add empty cells to ease reading.
            if ( previousAspectInstance == aspectInstance )
            {
                entries.Add(
                    new CodeLensDetailsEntry(
                        ImmutableArray.Create(
                            new CodeLensDetailsField( "" ),
                            new CodeLensDetailsField( "" ),
                            new CodeLensDetailsField( "" ),
                            new CodeLensDetailsField( transformation ) ) ) );
            }
            else
            {
                entries.Add(
                    new CodeLensDetailsEntry(
                        ImmutableArray.Create(
                            new CodeLensDetailsField( aspectClass ),
                            new CodeLensDetailsField( targetDeclaration ),
                            origin,
                            new CodeLensDetailsField( transformation ) ) ) );

                previousAspectInstance = aspectInstance;
            }
        }

        // First add aspects without transformations.
        foreach ( var aspectInstance in aspectInstances.Where( i => i.Value.Count == 0 ) )
        {
            var aspectInstanceTransformations =
                aspectInstance.Key.Advice.SelectMany( a => a.Transformations ).Select( t => t.TargetDeclaration ).Distinct().ToReadOnlyList();

            var transformationText = aspectInstanceTransformations.Count switch
            {
                0 => "(The aspect does not provide any transformation.)",
                1 => "(The aspect transforms 1 child declaration.)",
                _ => $"(The aspect transforms {aspectInstanceTransformations.Count} child declarations.)"
            };

            AddEntry( aspectInstance.Key, transformationText );
        }

        foreach ( var (aspectInstance, instanceTransformations) in aspectInstances )
        {
            // Add transformations by execution order.
            foreach ( var transformation in instanceTransformations.OrderBy( t => t.Order ) )
            {
                if ( !transformation.ShouldBeDisplayed() )
                {
                    continue;
                }

                var description = MetalamaStringFormatter.Format( transformation.Description );

                AddEntry( aspectInstance, description );
            }

            // Add child transformations note.
            if ( instanceTransformations.Count > 1 )
            {
                if ( aspectInstance.TargetDeclaration.TryGetSerializableId( out var id ) && id == symbolId )
                {
                    var transformationsOnChildrenCount = aspectInstance.Advice.SelectMany( a => a.Transformations )
                        .Where(
                            t => t.TargetDeclaration.TryGetSerializableId( out var transformedDeclarationId )
                                 && transformedDeclarationId != symbolId )
                        .Select( t => t.TargetDeclaration )
                        .Distinct()
                        .Count();

                    if ( transformationsOnChildrenCount > 0 )
                    {
                        var transformationText = transformationsOnChildrenCount switch
                        {
                            1 => "(The aspect also transforms 1 child declaration.)",
                            _ => $"(The aspect also transforms {transformationsOnChildrenCount} child declarations.)"
                        };

                        AddEntry( aspectInstance, transformationText );
                    }
                }
            }
        }

        return new CodeLensDetailsTable( _detailsHeaders, entries.ToImmutableArray() );
    }
}