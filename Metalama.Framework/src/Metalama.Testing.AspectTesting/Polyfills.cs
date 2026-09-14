// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Metalama.Testing.AspectTesting;

/// <summary>
/// Provides the source code of the polyfills, that is, of the system types that the reference assemblies of a target
/// framework do not declare and that PostSharp.Engineering compiles into the projects of this repository.
/// </summary>
/// <remarks>
/// <para>
/// The test runner compiles the test source files into a compilation of its own, whose references are the references
/// of the test project. The polyfills that the test project itself compiles are therefore not visible to a test,
/// because the assembly of the test project is not one of those references. A test that needs a polyfill requests it
/// by name with the <c>@IncludePolyfill</c> option, and the test runner adds the same source file to the test
/// compilation.
/// </para>
/// <para>
/// The source files are the ones that PostSharp.Engineering ships, embedded in this assembly as managed resources.
/// Each of them is guarded by the condition of the target framework that lacks the type, so a polyfill whose type the
/// reference assemblies of the test project already declare compiles to nothing.
/// </para>
/// </remarks>
internal static class Polyfills
{
    /// <summary>
    /// The part of the name of a managed resource that the <c>Link</c> metadata of the resource item produces. The
    /// name cannot be matched from its beginning, because it starts with the name of the assembly and that name
    /// contains the Roslyn version.
    /// </summary>
    private const string _resourceNameInfix = "_Resources_.";

    private static readonly Lazy<ImmutableDictionary<string, string>> _sources = new( GetSources );

    /// <summary>
    /// Gets the name of every polyfill, which is the name of its source file without the extension.
    /// </summary>
    public static IEnumerable<string> Names => _sources.Value.Keys;

    /// <summary>
    /// Gets the source code of the polyfill of a given name, or <c>null</c> if there is no polyfill of that name.
    /// </summary>
    public static string? GetSourceOrNull( string name ) => _sources.Value.TryGetValue( name, out var source ) ? source : null;

    private static ImmutableDictionary<string, string> GetSources()
    {
        var assembly = typeof(Polyfills).Assembly;

        var sources = ImmutableDictionary.CreateBuilder<string, string>( StringComparer.OrdinalIgnoreCase );

        foreach ( var resourceName in assembly.GetManifestResourceNames() )
        {
            var infixIndex = resourceName.IndexOf( _resourceNameInfix, StringComparison.Ordinal );

            if ( infixIndex < 0 )
            {
                continue;
            }

            var name = resourceName.Substring( infixIndex + _resourceNameInfix.Length );

            using var reader = new StreamReader( assembly.GetManifestResourceStream( resourceName )! );

            sources.Add( name, reader.ReadToEnd() );
        }

        if ( sources.Count == 0 )
        {
            throw new InvalidOperationException( "The polyfill resources were not found in the test framework assembly." );
        }

        return sources.ToImmutable();
    }
}
