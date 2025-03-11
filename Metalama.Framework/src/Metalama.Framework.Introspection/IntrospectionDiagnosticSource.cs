// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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