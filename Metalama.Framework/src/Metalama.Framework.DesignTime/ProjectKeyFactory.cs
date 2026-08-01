// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Caching;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace Metalama.Framework.DesignTime;

/// <summary>
/// Represents a unique project in a solution. The implementation is optimized to be cheaply computed from a Compilation,
/// because a Compilation does not hold a reference to its project.
/// </summary>
public static class ProjectKeyFactory
{
    private static readonly WeakCache<Compilation, ProjectKey> _compilationCache = new( isStaticCache: true );
    private static readonly WeakCache<Microsoft.CodeAnalysis.Project, ProjectKey?> _projectCache = new( isStaticCache: true );
    private static readonly WeakCache<ParseOptions, StrongBox<ulong>> _preprocessorSymbolHashCodeCache = new( isStaticCache: true );

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

    /// <summary>
    /// The prefix of the compilation symbol that identifies a project, defined by <c>Metalama.Framework.targets</c>
    /// from the project path, target framework, configuration and platform.
    /// </summary>
    private const string _projectDiscriminatorSymbolPrefix = "METALAMA_PROJECT_";

    /// <summary>
    /// Returns the hash that distinguishes one project from another within a <see cref="ProjectKey"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only the <c>METALAMA_PROJECT_*</c> symbols are hashed when the project defines at least one of them, because
    /// those symbols already encode everything that identifies the project. Hashing the other symbols would make the
    /// key depend on data that identifies nothing: the addition or the removal of an unrelated <c>#define</c>, or a
    /// conditional assignment to <c>DefineConstants</c>, would change the key of a project that has not otherwise
    /// changed. Such a change invalidates the design-time pipeline cache and makes the project unrecognizable to the
    /// processes that route requests by key.
    /// </para>
    /// <para>
    /// Every symbol is hashed when no discriminator is present, which is the case for a project built by a version of
    /// Metalama older than 2026.1 and for a host that builds its own compilations, such as a test harness. The previous
    /// behavior is therefore preserved wherever the discriminator cannot be relied upon. See issue #1749.
    /// </para>
    /// </remarks>
    private static StrongBox<ulong> GetPreprocessorSymbolHashCode( ParseOptions parseOptions )
    {
        var symbolNames = parseOptions.PreprocessorSymbolNames;

        // The Roslyn implementation of PreprocessorSymbolNames is an ImmutableArray, which can be enumerated as a span
        // and therefore without any allocation. This method is on a performance-sensitive path.
        if ( symbolNames is ImmutableArray<string> immutableArray )
        {
            return ComputeSymbolHashCode( immutableArray.AsSpan() );
        }

        return ComputeSymbolHashCode( symbolNames as string[] ?? symbolNames.ToArray() );
    }

    /// <summary>
    /// Computes the value returned by <see cref="GetPreprocessorSymbolHashCode"/> from the symbols of the project.
    /// </summary>
    private static StrongBox<ulong> ComputeSymbolHashCode( ReadOnlySpan<string> symbolNames )
    {
        var discriminatorCount = 0;

        foreach ( var symbolName in symbolNames )
        {
            if ( IsDiscriminator( symbolName ) )
            {
                discriminatorCount++;
            }
        }

        var hashedCount = discriminatorCount > 0 ? discriminatorCount : symbolNames.Length;

        if ( hashedCount == 0 )
        {
            return new StrongBox<ulong>( 0 );
        }

        // ProjectKey is a cross-process identifier, therefore the hasher must be a robust one.
        using var hashHandle = HashUtilities.AllocateHasher();
        var hasher = hashHandle.Value;

        if ( hashedCount == 1 )
        {
            // The usual case, in which the project defines exactly one discriminator symbol. Neither a buffer nor a
            // sort is required.
            foreach ( var symbolName in symbolNames )
            {
                if ( discriminatorCount == 0 || IsDiscriminator( symbolName ) )
                {
                    hasher.Append( symbolName );

                    break;
                }
            }
        }
        else
        {
            var buffer = ArrayPool<string>.Shared.Rent( hashedCount );

            try
            {
                var index = 0;

                foreach ( var symbolName in symbolNames )
                {
                    if ( discriminatorCount == 0 || IsDiscriminator( symbolName ) )
                    {
                        buffer[index++] = symbolName;
                    }
                }

                // The symbols are sorted so that the key of a project does not depend on the order in which they happen
                // to be supplied. Without this, a single project could yield two different keys, which is as harmful as
                // two projects yielding a single key. CompileTimeCompilationBuilder.ComputeSourceHash sorts them for the
                // same reason.
                Array.Sort( buffer, 0, hashedCount, StringComparer.Ordinal );

                for ( var i = 0; i < hashedCount; i++ )
                {
                    hasher.Append( buffer[i] );
                }
            }
            finally
            {
                // The array is cleared on return because it would otherwise keep the strings alive.
                ArrayPool<string>.Shared.Return( buffer, clearArray: true );
            }
        }

        var hashCode = hasher.GetCurrentHashAsUInt64();

        // Zero is reserved for the absence of a symbol.
        return new StrongBox<ulong>( hashCode == 0 ? 1 : hashCode );
    }

    private static bool IsDiscriminator( string symbolName )
        => symbolName.StartsWith( _projectDiscriminatorSymbolPrefix, StringComparison.Ordinal );

    internal static ProjectKey FromCompilation( Compilation compilation ) => _compilationCache.GetOrAdd( compilation, FromCompilationCore );

    /// <summary>
    /// Gets the <see cref="ProjectKey"/> of a <see cref="Compilation"/>, unless the compilation has no assembly name
    /// and therefore no identity.
    /// </summary>
    /// <remarks>
    /// Roslyn allows a <see cref="Compilation"/> to have a null or empty <see cref="Compilation.AssemblyName"/>, and such
    /// compilations do reach us as project references at design time. They have no usable identity: any two of them would
    /// produce the same <see cref="ProjectKey"/>.
    /// </remarks>
    internal static bool TryFromCompilation( Compilation compilation, [NotNullWhen( true )] out ProjectKey? projectKey )
    {
        if ( string.IsNullOrEmpty( compilation.AssemblyName ) )
        {
            projectKey = null;

            return false;
        }

        projectKey = FromCompilation( compilation );

        return true;
    }

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