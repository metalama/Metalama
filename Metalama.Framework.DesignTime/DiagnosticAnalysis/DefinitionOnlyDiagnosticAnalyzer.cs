// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.DesignTime.Services;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.DiagnosticAnalysis;

#pragma warning disable RS1001 // Missing diagnostic analyzer attribute.
#pragma warning disable RS1022 // Remove access to our implementation types 

public abstract class DefinitionOnlyDiagnosticAnalyzer : DiagnosticAnalyzer
{
    private protected DesignTimeDiagnosticDefinitions DiagnosticDefinitions { get; }

    protected DefinitionOnlyDiagnosticAnalyzer() : this( DesignTimeServiceProviderFactory.GetSharedServiceProvider() ) { }

    protected DefinitionOnlyDiagnosticAnalyzer( GlobalServiceProvider serviceProvider )
    {
        var userDiagnosticRegistrationService = serviceProvider.GetRequiredService<IUserDiagnosticRegistrationService>();
        this.ShouldWrapUnsupportedDiagnostics = userDiagnosticRegistrationService.ShouldWrapUnsupportedDiagnostics;
        this.DiagnosticDefinitions = userDiagnosticRegistrationService.DiagnosticDefinitions;
    }

    protected bool ShouldWrapUnsupportedDiagnostics { get; }

    static DefinitionOnlyDiagnosticAnalyzer()
    {
        DesignTimeServices.Initialize();
    }

    public override void Initialize( AnalysisContext context )
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.None );
    }

    public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => this.DiagnosticDefinitions.SupportedDiagnosticDescriptors.Values.ToImmutableArray();
}