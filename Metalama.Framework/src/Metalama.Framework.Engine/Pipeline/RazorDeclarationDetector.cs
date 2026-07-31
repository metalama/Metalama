// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System;
using System.IO;
using System.Linq;

namespace Metalama.Framework.Engine.Pipeline;

/// <summary>
/// Detects whether the current compiler invocation is Razor's <c>RazorCompileComponentDeclaration</c> pass, i.e. the
/// legacy (non-source-generator) build of a Razor/Blazor project (<c>UseRazorSourceGenerator=false</c>) that produces a
/// throwaway reference assembly consumed only by the Razor code generator's type resolver.
/// </summary>
/// <remarks>
/// This pass cannot be identified via the <c>MetalamaCompilationScenario</c> MSBuild property (as the WPF
/// <c>MarkupCompilePass1</c> pass is), because its <c>Csc</c> invocation forwards no <c>/analyzerconfig</c> and
/// therefore no <c>build_property.*</c> option reaches the compiler. It is instead identified by the Razor SDK output
/// convention: the pass writes to (and generates sources under) <c>$(IntermediateOutputPath)RazorDeclaration\</c>
/// (<c>Sdk.Razor.CurrentVersion.targets</c>, <c>_RazorComponentDeclarationOutputPath</c>). See issue #1741.
/// </remarks>
internal static class RazorDeclarationDetector
{
    /// <summary>
    /// The Razor SDK intermediate folder that both the generated declaration sources and the output assembly of the
    /// <c>RazorCompileComponentDeclaration</c> pass live under.
    /// </summary>
    private const string _razorDeclarationFolderName = "RazorDeclaration";

    public static bool IsRazorDeclaration( ITransformerContext context )
    {
        // TODO (Metalama.Compiler#197): once TransformerContext exposes the output path, prefer
        // IsRazorDeclarationOutputPath(context.OutputPath), which is the robust, convention-based signal. Until then,
        // detect from the generated declaration source paths, which live under the same RazorDeclaration folder.
        return IsRazorDeclarationCompilation( context.Compilation );
    }

    /// <summary>
    /// Robust detection from the compiler output path (available once Metalama.Compiler#197 lands): the declaration
    /// pass writes its assembly to <c>...\RazorDeclaration\&lt;assembly&gt;.dll</c>.
    /// </summary>
    public static bool IsRazorDeclarationOutputPath( string? outputPath )
    {
        if ( string.IsNullOrEmpty( outputPath ) )
        {
            return false;
        }

        var directory = Path.GetFileName( Path.GetDirectoryName( outputPath ) );

        return string.Equals( directory, _razorDeclarationFolderName, StringComparison.OrdinalIgnoreCase );
    }

    /// <summary>
    /// Interim detection from the compilation's syntax-tree paths: the pass compiles generated <c>*.razor.g.cs</c>
    /// declaration stubs located under the <c>RazorDeclaration</c> intermediate folder.
    /// </summary>
    private static bool IsRazorDeclarationCompilation( Compilation compilation )
        => compilation.SyntaxTrees.Any( tree => HasRazorDeclarationDirectorySegment( tree.FilePath ) );

    private static bool HasRazorDeclarationDirectorySegment( string? filePath )
    {
        if ( string.IsNullOrEmpty( filePath ) )
        {
            return false;
        }

        foreach ( var segment in filePath.Split( '\\', '/' ) )
        {
            if ( string.Equals( segment, _razorDeclarationFolderName, StringComparison.OrdinalIgnoreCase ) )
            {
                return true;
            }
        }

        return false;
    }
}
