// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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