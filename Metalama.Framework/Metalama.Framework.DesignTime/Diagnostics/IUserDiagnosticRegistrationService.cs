// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Metalama.Framework.Services;

namespace Metalama.Framework.DesignTime.Diagnostics;

[PublicAPI]
public interface IUserDiagnosticRegistrationService : IGlobalService
{
    /// <summary>
    /// Gets a value indicating whether unsupported diagnostics should be wrapped into a known diagnostic.
    /// This property is <c>true</c> in productions scenarios and generally <c>false</c> in test scenarios.
    /// </summary>
    bool ShouldWrapUnsupportedDiagnostics { get; }

    DesignTimeDiagnosticDefinitions DiagnosticDefinitions { get; }

    /// <summary>
    /// Inspects a <see cref="DesignTimePipelineExecutionResult"/> and compares the reported or suppressed diagnostics to the list of supported diagnostics
    /// and suppressions from the user profile. If some items are not supported in the user profile, add them to the user profile. 
    /// </summary>
    void RegisterDescriptors( DiagnosticManifest diagnosticManifest );
}