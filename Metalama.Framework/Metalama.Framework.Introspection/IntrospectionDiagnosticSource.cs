// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Introspection;

/// <summary>
/// Enumerates the possible sources (or originators) of an <see cref="IIntrospectionDiagnostic"/>.
/// </summary>
public enum IntrospectionDiagnosticSource
{
    /// <summary>
    /// The diagnostic is produced by Metalama.
    /// </summary>
    Metalama,

    // Resharper disable UnusedMember.Global
    CSharp,

    // Reported by the user using an API
    User,

    // Reported by MSBuild.
    MSBuild
}