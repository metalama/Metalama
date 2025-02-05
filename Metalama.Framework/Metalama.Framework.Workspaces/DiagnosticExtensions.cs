// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Introspection;

namespace Metalama.Framework.Workspaces;

[PublicAPI]
public static class DiagnosticExtensions
{
    /// <summary>
    /// Formats an <see cref="IIntrospectionDiagnostic"/> as a string formatted as a diagnostic of <c>dotnet build</c> or <c>msbuild</c>.
    /// </summary>
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