// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using K4os.Hash.xxHash;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Caching;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace Metalama.Framework.DesignTime;

/// <summary>
/// Represents a unique project in a solution. The implementation is optimized to be cheaply computed from a Compilation,
/// because a Compilation does not hold a reference to its project.
/// </summary>
public static class ProjectKeyFactory
{
    private static readonly WeakCache<Compilation, ProjectKey> _compilationCache = new();
    private static readonly WeakCache<Microsoft.CodeAnalysis.Project, ProjectKey?> _projectCache = new();
    private static readonly WeakCache<ParseOptions, StrongBox<ulong>> _preprocessorSymbolHashCodeCache = new();

    internal static ProjectKey Create( string assemblyName, ParseOptions? parseOptions )
    {
        ulong preprocessorSymbolHashCode;

        bool isMetalamaEnabled;

        if ( parseOptions == null )
        {
            preprocessorSymbolHashCode = 0;
            isMetalamaEnabled = false;
        }
        else
        {
            preprocessorSymbolHashCode = _preprocessorSymbolHashCodeCache.GetOrAdd( parseOptions, GetPreprocessorSymbolHashCode ).Value;
            isMetalamaEnabled = parseOptions.PreprocessorSymbolNames.Contains( "METALAMA" );
        }

        return new ProjectKey( assemblyName, preprocessorSymbolHashCode, isMetalamaEnabled );
    }

    private static StrongBox<ulong> GetPreprocessorSymbolHashCode( ParseOptions parseOptions )
    {
        // ProjectKey is a cross-process identifier so we have to use a robust hasher.
        var hasher = new XXH64();

        var preprocessorSymbolNames = parseOptions.PreprocessorSymbolNames;

        if ( preprocessorSymbolNames is ImmutableArray<string> immutableArray )
        {
            // The Roslyn implementation of PreprocessorSymbolNames is an ImmutableArray, so we allocate less memory.

            if ( immutableArray.IsDefaultOrEmpty )
            {
                return new StrongBox<ulong>( 0 );
            }

            foreach ( var symbol in immutableArray )
            {
                hasher.Update( symbol );
            }
        }
        else
        {
            // This is for forward compatibility.

            var hasHashCode = false;

            foreach ( var symbol in preprocessorSymbolNames )
            {
                hasHashCode = true;
                hasher.Update( symbol );
            }

            if ( !hasHashCode )
            {
                return new StrongBox<ulong>( 0 );
            }
        }

        var hashCode = hasher.Digest();

        if ( hashCode == 0 )
        {
            hashCode = 1;
        }

        return new StrongBox<ulong>( hashCode );
    }

    internal static ProjectKey FromCompilation( Compilation compilation ) => _compilationCache.GetOrAdd( compilation, FromCompilationCore );

    private static ProjectKey FromCompilationCore( Compilation compilation )
    {
        var assemblyName = compilation.AssemblyName.AssertNotNull();

        var syntaxTrees = ((CSharpCompilation) compilation).SyntaxTrees;
        var parseOptions = syntaxTrees.IsDefaultOrEmpty ? null : syntaxTrees[0].Options;

        return Create( assemblyName, parseOptions );
    }

    public static ProjectKey? FromProject( Microsoft.CodeAnalysis.Project project ) => _projectCache.GetOrAdd( project, FromProjectCore );

    private static ProjectKey? FromProjectCore( Microsoft.CodeAnalysis.Project project )
    {
        var assemblyName = project.AssemblyName;

        var parseOptions = project.ParseOptions as CSharpParseOptions;

        if ( parseOptions == null )
        {
            return null;
        }

        return Create( assemblyName, parseOptions );
    }

    internal static ProjectKey CreateTest( string id, bool isMetalamaEnabled = true )
    {
        // We intentionally don't use a zero hash so that we can test serialization roundtrip.
        return new ProjectKey( id, 12345, isMetalamaEnabled );
    }
}