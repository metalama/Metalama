// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Introspection;

/// <summary>
/// Enumerates the possible sources (or originators) of an <see cref="IIntrospectionDiagnostic"/>.
/// </summary>
/// <seealso cref="IIntrospectionDiagnostic"/>
/// <seealso href="@introspection-api"/>
public enum IntrospectionDiagnosticSource
{
    /// <summary>
    /// The diagnostic is produced by Metalama.
    /// </summary>
    Metalama,

    /// <summary>
    /// The diagnostic is produced by the C# compiler.
    /// </summary>

    // Resharper disable UnusedMember.Global
    CSharp,

    /// <summary>
    /// The diagnostic is reported by the user using an API.
    /// </summary>

    // Reported by the user using an API
    User,

    /// <summary>
    /// The diagnostic is reported by MSBuild.
    /// </summary>

    // Reported by MSBuild.
    MSBuild
}