// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime.Manifest;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Diagnostics;

public static class DiagnosticExtensions
{
    public static string GetLocalizedMessage( this Diagnostic diagnostic ) => diagnostic.GetMessage( MetalamaStringFormatter.Instance );

    /// <summary>
    /// Replaces the syntax tree instance of the diagnostic with the provided one.
    /// </summary>
    /// <remarks>
    /// Assumes that the diagnostic has a location in a different syntax tree with the same path as the provided one.
    /// </remarks>
    public static Diagnostic WithSyntaxTreeInstance( this Diagnostic diagnostic, SyntaxTree tree )
    {
        // It is a hack to use manifest code here, but it already does what we need.
        return new CompileTimeDiagnosticManifest( diagnostic, new() { { tree.FilePath, 0 } } ).ToDiagnostic( [tree] );
    }
}