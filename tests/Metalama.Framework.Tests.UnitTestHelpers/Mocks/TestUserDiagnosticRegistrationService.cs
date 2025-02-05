// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.Tests.UnitTestHelpers.Mocks;

public sealed class TestUserDiagnosticRegistrationService : IUserDiagnosticRegistrationService
{
    public TestUserDiagnosticRegistrationService( bool shouldWrapUnsupportedDiagnostics = false )
    {
        this.ShouldWrapUnsupportedDiagnostics = shouldWrapUnsupportedDiagnostics;
    }

    public bool ShouldWrapUnsupportedDiagnostics { get; }

    DesignTimeDiagnosticDefinitions IUserDiagnosticRegistrationService.DiagnosticDefinitions
        => new( ImmutableArray<DiagnosticDescriptor>.Empty, ImmutableArray<SuppressionDescriptor>.Empty );

    public void RegisterDescriptors( DiagnosticManifest diagnosticManifest ) { }
}