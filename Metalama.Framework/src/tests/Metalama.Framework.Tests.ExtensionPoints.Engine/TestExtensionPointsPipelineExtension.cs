// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.ReferenceGraph;
using Metalama.Framework.Tests.ExtensionPoints.Engine;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

[assembly: ExportExtension( typeof(TestExtensionPointsPipelineExtension), ExtensionKinds.Default )]

namespace Metalama.Framework.Tests.ExtensionPoints.Engine;

/// <summary>
/// The pipeline extension of the proof of concept. It uses the public extension points of the engine only.
/// </summary>
public sealed class TestExtensionPointsPipelineExtension : PipelineExtension
{
    /// <summary>
    /// The warning that describes each registration, so that the expected output of an aspect test shows what the extension observed.
    /// </summary>
    internal static DiagnosticDefinition<(string Tag, string Channel, string Origin, string PredecessorKind, string TemplateProvider, int Stage, bool IsSourceStage)>
        RegistrationObserved { get; } = new(
        "TEST0001",
        Severity.Warning,
        "Registration '{0}' through the {1}: origin '{2}', predecessor {3}, template provider {4}, stage {5}, source stage {6}." );

    /// <summary>
    /// The warning that describes each reference that the extension reads from the shared index of source references.
    /// </summary>
    internal static DiagnosticDefinition<(string MethodName, string ReferencingSymbol, string ReferenceKinds, bool IsRestricted)> ReferenceObserved { get; } =
        new( "TEST0002", Severity.Warning, "Reference to '{0}' from '{1}' ({2}), index restricted to declaration roots: {3}." );

    /// <summary>
    /// The warning that lists the names of all the symbols that the shared index of source references contains, so that the expected output of
    /// an aspect test shows that the index contains nothing else than what the extensions requested.
    /// </summary>
    internal static DiagnosticDefinition<string> IndexContent { get; } =
        new( "TEST0005", Severity.Warning, "The shared index contains references to: {0}." );

    public override bool Initialize( PipelineExtensionInitializationContext context )
    {
        context.ServiceBuilder.Add( _ => new TestExtensionPointsService() );
        context.AddDiagnosticDefinitions( [RegistrationObserved, ReferenceObserved, IndexContent] );

        return true;
    }

    /// <summary>
    /// Returns, in the first stage only, one requirement per requested method name for invocations and for the default reference kind, which
    /// includes method groups.
    /// </summary>
    public override SourceIndexRequirements GetSourceIndexRequirements( SourceIndexRequirementsContext context )
    {
        var reports = context.Contributors.OfKind( TestContributorKinds.ReferenceReport ).ToList();

        if ( reports.Count == 0 || context.HighLevelStageIndex != 0 )
        {
            return SourceIndexRequirements.None;
        }

        var requirements = reports
            .Select( r => new ReferenceIndexerRequirements( ReferenceKinds.Invocation | ReferenceKinds.Default, false, DeclarationKind.Method, r.MethodName ) )
            .ToImmutableArray();

        ImmutableArray<SyntaxNode>? roots = reports.All( r => r.DeclarationRoots != null )
            ? reports.SelectMany( r => r.DeclarationRoots!.Value ).ToImmutableArray()
            : null;

        return new SourceIndexRequirements( requirements ) { DeclarationRoots = roots };
    }

    public override async Task ExecuteTransformingContributorsAsync( ExtensionTransformationContext context, CancellationToken cancellationToken )
    {
        ReportRegistrations( context );
        await ReportReferencesAsync( context, cancellationToken );
    }

    private static async Task ReportReferencesAsync( ExtensionTransformationContext context, CancellationToken cancellationToken )
    {
        var methodNames = new HashSet<string>( context.Contributors.OfKind( TestContributorKinds.ReferenceReport ).Select( r => r.MethodName ) );

        if ( methodNames.Count == 0 || !context.IsSourceStage )
        {
            return;
        }

        var index = await context.SourceReferenceIndex.GetIndexAsync( cancellationToken );

        var indexedNames = index.ReferencedSymbols.Select( s => s.ReferencedSymbol.Name ).Distinct().OrderBy( n => n, StringComparer.Ordinal );
        context.Diagnostics.Report( IndexContent.CreateRoslynDiagnostic( Location.None, string.Join( ", ", indexedNames ) ) );

        var references = index.ReferencedSymbols
            .Where( s => s.ReferencedSymbol.Kind == SymbolKind.Method && methodNames.Contains( s.ReferencedSymbol.Name ) )
            .SelectMany( s => s.References.SelectMany( r => r.Nodes.Select( n => (Symbol: s.ReferencedSymbol, Reference: r, Node: n) ) ) )
            .OrderBy( x => x.Node.Syntax.SyntaxTree?.FilePath, StringComparer.Ordinal )
            .ThenBy( x => x.Node.Syntax.SpanStart );

        foreach ( var (symbol, reference, node) in references )
        {
            context.Diagnostics.Report(
                ReferenceObserved.CreateRoslynDiagnostic(
                    node.Syntax.GetLocation(),
                    (symbol.Name, reference.ReferencingSymbol.ToDisplayString(), node.ReferenceKind.ToString(),
                     context.SourceReferenceIndex.IsRestrictedToDeclarationRoots) ) );
        }
    }

    /// <summary>
    /// Reports a diagnostic for each <see cref="TestExtensionPipelineContributor"/> of the stage, in the order of the tags, so that a test can verify what
    /// the extension received.
    /// </summary>
    private static void ReportRegistrations( ExtensionTransformationContext context )
    {
        foreach ( var registration in context.Contributors.OfKind( TestContributorKinds.Registration ).OrderBy( r => r.Tag ) )
        {
            var scope = registration.Scope?.GetTargetOrNull( context.StageFinalCompilation );

            var templateProvider = registration.TemplateProviderMatches switch
            {
                null => "not checked",
                true => "as expected",
                false => "not as expected"
            };

            context.Diagnostics.Report(
                RegistrationObserved.CreateRoslynDiagnostic(
                    scope.GetDiagnosticLocation(),
                    (registration.Tag, registration.Channel, registration.Origin.DiagnosticSourceDescription,
                     registration.Origin.Predecessor.Kind.ToString(), templateProvider, context.HighLevelStageIndex, context.IsSourceStage) ) );
        }
    }
}
