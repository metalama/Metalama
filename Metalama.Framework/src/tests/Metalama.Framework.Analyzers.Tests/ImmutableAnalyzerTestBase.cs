// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Metalama.Framework.Utilities;
using Xunit;

namespace Metalama.Framework.Analyzers.Tests;

/// <summary>
/// The harness shared by the tests of <see cref="ImmutableContractAnalyzer"/>, which compiles a fragment of source in
/// memory and runs the analyzer over it.
/// </summary>
/// <remarks>
/// Modelled on <see cref="DurableAnalyzerTestBase"/>, and separate from it because the two analyzers gate on
/// different types and filter on different identifier prefixes.
/// </remarks>
public abstract class ImmutableAnalyzerTestBase
{
    private static readonly MetadataReference[] _references = CreateReferences( withMetalama: true );
    private static readonly MetadataReference[] _referencesWithoutMetalama = CreateReferences( withMetalama: false );

    private static MetadataReference[] CreateReferences( bool withMetalama )
    {
        var runtimeDirectory = Path.GetDirectoryName( typeof(object).Assembly.Location )!;

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile( typeof(object).Assembly.Location ),
            MetadataReference.CreateFromFile( Path.Combine( runtimeDirectory, "netstandard.dll" ) ),
            MetadataReference.CreateFromFile( Path.Combine( runtimeDirectory, "System.Runtime.dll" ) ),
            MetadataReference.CreateFromFile( Path.Combine( runtimeDirectory, "System.Collections.dll" ) ),
            MetadataReference.CreateFromFile( Path.Combine( runtimeDirectory, "System.Collections.Concurrent.dll" ) ),
            MetadataReference.CreateFromFile( Path.Combine( runtimeDirectory, "System.Linq.dll" ) ),
            MetadataReference.CreateFromFile( Path.Combine( runtimeDirectory, "System.Threading.dll" ) ),
            MetadataReference.CreateFromFile( Path.Combine( runtimeDirectory, "System.Text.RegularExpressions.dll" ) ),

            // System.Uri is type-forwarded, and GetTypeByMetadataName does not follow a forwarder to an assembly the
            // compilation does not reference. Without this, every test that names a Uri passes for the wrong reason:
            // an unresolved type is an error type, which rule 0 reports as immutable.
            MetadataReference.CreateFromFile( typeof(Uri).Assembly.Location ),

            // For ImmutableTypeAttribute, which is the marker of the contract.
            MetadataReference.CreateFromFile( typeof(ImmutableTypeAttribute).Assembly.Location ),

            MetadataReference.CreateFromFile( typeof(ImmutableArray).Assembly.Location ),

            // For the Roslyn types that the table classifies as mutable.
            MetadataReference.CreateFromFile( typeof(SyntaxNode).Assembly.Location ),
            MetadataReference.CreateFromFile( typeof(CSharpSyntaxNode).Assembly.Location )
        };

        if ( withMetalama )
        {
            // IAspect is the gate: without it the analyzer registers nothing.
            references.Add( MetadataReference.CreateFromFile( typeof(Metalama.Framework.Aspects.IAspect).Assembly.Location ) );
        }

        return references.ToArray();
    }

    /// <summary>
    /// Compiles a fragment and returns the diagnostics that this analyzer produced, in a stable order.
    /// </summary>
    /// <param name="code">The source to compile. It is not required to be free of compilation errors.</param>
    /// <param name="withMetalamaReference">
    /// Whether the compilation references the assembly that declares <c>IAspect</c>. Passing <c>false</c> exercises
    /// the gate that makes the analyzer free for a project that does not use Metalama.
    /// </param>
    private protected static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(
        string code,
        bool withMetalamaReference = true )
    {
        var compilation = CreateCompilation( code, withMetalamaReference );

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>( new ImmutableContractAnalyzer() ) );

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        return diagnostics
            .Where( d => d.Id.StartsWith( "LAMA088", StringComparison.Ordinal ) )
            .OrderBy( d => d.Location.SourceSpan.Start )
            .ToImmutableArray();
    }

    /// <summary>
    /// Asserts that the fragment produces exactly one diagnostic, with the expected identifier, and returns its
    /// message so that a test may assert on the path.
    /// </summary>
    private protected static async Task<string> AssertSingleDiagnosticAsync( string code, string expectedId )
    {
        var diagnostics = await GetDiagnosticsAsync( code );

        Assert.Single( diagnostics );
        Assert.Equal( expectedId, diagnostics[0].Id );

        return diagnostics[0].GetMessage();
    }

    /// <summary>
    /// Asserts that the fragment produces no diagnostic at all. This is the assertion that matters most: a rule that
    /// fires where it should not is worse than one that stays silent.
    /// </summary>
    private protected static async Task AssertNoDiagnosticAsync( string code, bool withMetalamaReference = true )
    {
        var diagnostics = await GetDiagnosticsAsync( code, withMetalamaReference );

        Assert.Empty( diagnostics.Select( d => d.GetMessage() ) );
    }

    /// <summary>
    /// Creates a compilation with the standard references, for a test that inspects symbols rather than diagnostics.
    /// </summary>
    private protected static CSharpCompilation CreateCompilation( string code, bool withMetalamaReference = true )
        => CSharpCompilation.Create(
            "TestAssembly",
            new[] { CSharpSyntaxTree.ParseText( code, new CSharpParseOptions( LanguageVersion.CSharp12 ) ) },
            withMetalamaReference ? _references : _referencesWithoutMetalama,
            new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary ) );

    /// <summary>
    /// The prologue that every fragment needs: the marker, and a type that carries the contract so that the fragments
    /// do not each have to declare one.
    /// </summary>
    private protected const string _prologue = """
                                             using System;
                                             using System.Collections.Generic;
                                             using System.Collections.Immutable;
                                             using Metalama.Framework.Utilities;

                                             """;
}
