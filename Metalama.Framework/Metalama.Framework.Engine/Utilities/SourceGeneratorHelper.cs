// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Metalama.Framework.Engine.Utilities;

public static class SourceGeneratorHelper
{
    private static readonly char[] _pathSeparators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    public static bool IsGeneratedFile( SyntaxTree syntaxTree )
        => syntaxTree.FilePath.AnySegmentEquals( _pathSeparators, "Metalama.Framework.CompilerExtensions.MetalamaSourceGenerator" );

    internal static bool IsGeneratedSymbol( ISymbol symbol )
    {
        if ( symbol.DeclaringSyntaxReferences.IsEmpty )
        {
            if ( symbol is IMethodSymbol { AssociatedSymbol: { } associatedSymbol } )
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