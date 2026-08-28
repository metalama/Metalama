// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Xunit;

namespace Metalama.Framework.Analyzers.Tests;

/// <summary>
/// The harness shared by the tests of <see cref="DurableContractAnalyzer"/>, which compiles a fragment of source in
/// memory and runs the analyzer over it.
/// </summary>
/// <remarks>
/// Modelled on <c>KindCheckOptimizationAnalyzerTests</c> in the sibling test project. There is no
/// <c>Microsoft.CodeAnalysis.Testing</c> package in this repository, so the harness is written by hand.
/// </remarks>
public abstract class DurableAnalyzerTestBase
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
            MetadataReference.CreateFromFile( typeof(ImmutableArray).Assembly.Location ),

            // For Compilation, SyntaxTree, SemanticModel and ISymbol, which are the types the rule is written about.
            MetadataReference.CreateFromFile( typeof(SyntaxNode).Assembly.Location ),
            MetadataReference.CreateFromFile( typeof(CSharpSyntaxNode).Assembly.Location )
        };

        if ( withMetalama )
        {
            references.Add( MetadataReference.CreateFromFile( typeof(DurableAttribute).Assembly.Location ) );

            // For CompilationModel, PartialCompilation, CompilationContext and the boundary types, which the
            // correspondence test probes.
            references.Add(
                MetadataReference.CreateFromFile( typeof(Metalama.Framework.Engine.CodeModel.CompilationModel).Assembly.Location ) );

            references.Add(
                MetadataReference.CreateFromFile( typeof(Metalama.Backstage.Diagnostics.ILogger).Assembly.Location ) );

            // ServiceProvider is declared in Metalama.Framework.Sdk, not in the engine.
            references.Add(
                MetadataReference.CreateFromFile( typeof(Metalama.Framework.Engine.Services.ServiceProvider).Assembly.Location ) );
        }

        return references.ToArray();
    }

    /// <summary>
    /// Compiles a fragment and returns the diagnostics that this analyzer produced, in a stable order.
    /// </summary>
    /// <param name="code">The source to compile. It is not required to be free of compilation errors.</param>
    /// <param name="withMetalamaReference">
    /// Whether the compilation references the assembly that declares the attribute. Passing <c>false</c> exercises the
    /// gate that makes the analyzer free for a project that does not use Metalama.
    /// </param>
    private protected static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(
        string code,
        bool withMetalamaReference = true )
    {
        var parseOptions = new CSharpParseOptions( LanguageVersion.CSharp12 );
        var syntaxTree = CSharpSyntaxTree.ParseText( code, parseOptions );

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            withMetalamaReference ? _references : _referencesWithoutMetalama,
            new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary ) );

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>( new DurableContractAnalyzer() ) );

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        return diagnostics
            .Where( d => d.Id.StartsWith( "LAMA087", StringComparison.Ordinal ) )
            .OrderBy( d => d.Location.SourceSpan.Start )
            .ToImmutableArray();
    }

    /// <summary>
    /// Asserts that the fragment produces exactly one diagnostic, with the expected identifier, and returns its
    /// message so that a test may assert on the retention path.
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
    private protected static CSharpCompilation CreateCompilation( string code )
        => CSharpCompilation.Create(
            "ProbeAssembly",
            new[] { CSharpSyntaxTree.ParseText( code, new CSharpParseOptions( LanguageVersion.CSharp12 ) ) },
            _references,
            new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary ) );
}
