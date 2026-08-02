// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Metalama.Framework.Engine.Utilities;

public static class SourceGeneratorHelper
{
    /// <summary>
    /// The path segment that Roslyn inserts into the path of every document produced by the Metalama source generator,
    /// and by which such documents are recognized.
    /// </summary>
    public const string GeneratedFilePathSegment = "Metalama.Framework.CompilerExtensions.MetalamaSourceGenerator";

    private static readonly char[] _pathSeparators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    public static bool IsGeneratedFile( SyntaxTree syntaxTree )
        => syntaxTree.FilePath.AnySegmentEquals( _pathSeparators, GeneratedFilePathSegment );

    internal static bool IsGeneratedSymbol( ISymbol symbol )
    {
        if ( symbol.DeclaringSyntaxReferences.IsEmpty )
        {
            if ( symbol.Kind == SymbolKind.Method && symbol is IMethodSymbol { AssociatedSymbol: { } associatedSymbol } )
            {
                return IsGeneratedSymbol( associatedSymbol );
            }

            if ( symbol.ContainingSymbol != null )
            {
                return IsGeneratedSymbol( symbol.ContainingSymbol );
            }
            else
            {
                return false;
            }
        }
        else
        {
            return symbol.DeclaringSyntaxReferences.All( r => IsGeneratedFile( r.SyntaxTree ) );
        }
    }
}