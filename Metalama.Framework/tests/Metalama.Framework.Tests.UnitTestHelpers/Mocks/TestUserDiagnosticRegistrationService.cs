// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Tests.UnitTestHelpers.Mocks;

public sealed class TestUserDiagnosticRegistrationService : IUserDiagnosticRegistrationService
{
    public TestUserDiagnosticRegistrationService( bool shouldWrapUnsupportedDiagnostics = false )
    {
        this.ShouldWrapUnsupportedDiagnostics = shouldWrapUnsupportedDiagnostics;
    }

    public bool ShouldWrapUnsupportedDiagnostics { get; }

    public List<DiagnosticManifest> RegisteredManifests { get; } = new();

    DesignTimeDiagnosticDefinitions IUserDiagnosticRegistrationService.DiagnosticDefinitions
        => new( ImmutableArray<DiagnosticDescriptor>.Empty, ImmutableArray<SuppressionDescriptor>.Empty );

    public void RegisterDescriptors( DiagnosticManifest diagnosticManifest ) => this.RegisteredManifests.Add( diagnosticManifest );
}