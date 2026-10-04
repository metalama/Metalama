// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Tests.ExtensionPoints.Engine;
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

    public override bool Initialize( PipelineExtensionInitializationContext context )
    {
        context.ServiceBuilder.Add( _ => new TestExtensionPointsService() );
        context.AddDiagnosticDefinitions( [RegistrationObserved] );

        return true;
    }

    public override Task ExecuteTransformingContributorsAsync( ExtensionTransformationContext context, CancellationToken cancellationToken )
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

        return Task.CompletedTask;
    }
}
