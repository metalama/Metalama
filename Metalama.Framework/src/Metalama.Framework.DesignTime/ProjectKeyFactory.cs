// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
    /// Only the <c>METALAMA_PROJECT_*</c> symbols are hashed when the project has any. They already encode everything
    /// that identifies the project, so hashing the other symbols would only make the key depend on things that do not
    /// identify anything: adding or removing an unrelated <c>#define</c>, or a conditional <c>DefineConstants</c>,
    /// would change the key of a project that has not otherwise changed. That churns the design-time pipeline cache and
    /// makes a project unrecognizable to the processes that route by key.
    /// </para>
    /// <para>
    /// Every symbol is still hashed when no discriminator is present, which is the case for a project built by a
    /// Metalama older than 2026.1 and for a host that builds its own compilations, such as a test harness. That
    /// preserves the previous behaviour exactly where the discriminator cannot be relied on. See #1749.
    /// </para>
    /// </remarks>
    private static StrongBox<ulong> GetPreprocessorSymbolHashCode( ParseOptions parseOptions )
    {
        // ProjectKey is a cross-process identifier so we have to use a robust hasher.
        using var hashHandle = HashUtilities.AllocateHasher();
        var hasher = hashHandle.Value;

        // Sorted, so that the key of a project does not depend on the order in which the symbols happen to be given.
        // Without this, the same project could yield two different keys, which is as harmful as two projects yielding
        // one key. CompileTimeCompilationBuilder.ComputeSourceHash already sorts them for the same reason.
        var allSymbolNames = parseOptions.PreprocessorSymbolNames
            .OrderBy( x => x, StringComparer.Ordinal )
            .ToReadOnlyList();

        var discriminatorSymbolNames = allSymbolNames
            .Where( x => x.StartsWith( _projectDiscriminatorSymbolPrefix, StringComparison.Ordinal ) )
            .ToReadOnlyList();

        var hashedSymbolNames = discriminatorSymbolNames.Count > 0 ? discriminatorSymbolNames : allSymbolNames;

        if ( hashedSymbolNames.Count == 0 )
        {
            return new StrongBox<ulong>( 0 );
        }

        foreach ( var symbol in hashedSymbolNames )
        {
            hasher.Append( symbol );
        }

        var hashCode = hasher.GetCurrentHashAsUInt64();

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