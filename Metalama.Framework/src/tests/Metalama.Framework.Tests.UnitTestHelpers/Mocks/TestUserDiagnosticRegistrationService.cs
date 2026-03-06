// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Tests.UnitTestHelpers.Mocks;

/// <summary>
/// Test implementation of <see cref="IUserDiagnosticRegistrationService"/> that simulates a user profile
/// with configurable initial suppression descriptors. By default, the user profile is empty (simulating
/// a stale profile where newly registered suppressions are not yet available).
/// </summary>
public sealed class TestUserDiagnosticRegistrationService : IUserDiagnosticRegistrationService
{
    private readonly ImmutableArray<SuppressionDescriptor> _initialSuppressionDescriptors;

    /// <summary>
    /// Initializes a new instance with an empty user profile (no pre-registered suppression descriptors).
    /// </summary>
    /// <param name="shouldWrapUnsupportedDiagnostics">When <c>true</c>, unsupported diagnostics are wrapped into known diagnostic IDs (production behavior).</param>
    /// <param name="initialSuppressionDescriptors">Optional suppression descriptors to pre-populate the user profile with, simulating
    /// a user profile that already knows about these suppressions (i.e., after IDE restart).</param>
    public TestUserDiagnosticRegistrationService(
        bool shouldWrapUnsupportedDiagnostics = false,
        ImmutableArray<SuppressionDescriptor> initialSuppressionDescriptors = default )
    {
        this.ShouldWrapUnsupportedDiagnostics = shouldWrapUnsupportedDiagnostics;
        this._initialSuppressionDescriptors = initialSuppressionDescriptors.IsDefault ? ImmutableArray<SuppressionDescriptor>.Empty : initialSuppressionDescriptors;
    }

    public bool ShouldWrapUnsupportedDiagnostics { get; }

    /// <summary>
    /// Gets the list of <see cref="DiagnosticManifest"/> instances that were registered via <see cref="RegisterDescriptors"/>.
    /// Used to verify that the pipeline correctly discovers and registers diagnostics/suppressions.
    /// </summary>
    public List<DiagnosticManifest> ManifestsRegisteredByPipeline { get; } = new();

    DesignTimeDiagnosticDefinitions IUserDiagnosticRegistrationService.DiagnosticDefinitions
        => new( ImmutableArray<DiagnosticDescriptor>.Empty, this._initialSuppressionDescriptors );

    public void RegisterDescriptors( DiagnosticManifest diagnosticManifest ) => this.ManifestsRegisteredByPipeline.Add( diagnosticManifest );
}
