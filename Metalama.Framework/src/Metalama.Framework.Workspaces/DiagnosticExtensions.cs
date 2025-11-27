// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Introspection;

namespace Metalama.Framework.Workspaces;

/// <summary>
/// Extension methods for <see cref="IIntrospectionDiagnostic"/>.
/// </summary>
/// <seealso cref="IIntrospectionDiagnostic"/>
/// <seealso href="@introspection-api"/>
[PublicAPI]
public static class DiagnosticExtensions
{
    /// <summary>
    /// Formats an <see cref="IIntrospectionDiagnostic"/> as a string in the format used by <c>dotnet build</c> or <c>msbuild</c>.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to format.</param>
    /// <returns>A formatted string suitable for displaying in build output.</returns>
    public static string FormatAsBuildDiagnostic( this IIntrospectionDiagnostic diagnostic )
        => diagnostic switch
        {
            { FilePath: not null, Line: null }
                => $"{diagnostic.FilePath}: {diagnostic.Severity.ToString().ToLowerInvariant()} {diagnostic.Id}: {diagnostic.Message}",
            { FilePath: not null, Line: not null }
                => $"{diagnostic.FilePath}({diagnostic.Line}): {diagnostic.Severity.ToString().ToLowerInvariant()} {diagnostic.Id}: {diagnostic.Message}",
            _ => $"{diagnostic.Severity.ToString().ToLowerInvariant()} {diagnostic.Id}: {diagnostic.Message}"
        };
}