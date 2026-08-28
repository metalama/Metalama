// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Analyzers.Durability;
using Metalama.Framework.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Metalama.Framework.Analyzers.Tests;

/// <summary>
/// Tests of the <c>MetalamaDurableType</c> and <c>MetalamaNonDurableType</c> MSBuild items, by which a project
/// declares the durability of a type it does not own.
/// </summary>
/// <remarks>
/// These tests cover the side the analyzer owns, that is, reading the two compiler-visible properties. They cannot
/// cover the build target that joins the items into those properties, which lives in
/// <c>Metalama.CompilerVisibleProperties.props</c>. Only a build of a project that consumes the package exercises
/// that, and the target name is the part that was wrong the first time.
/// </remarks>
public sealed class DurableMsBuildTypeListTests
{
    private static readonly MetadataReference[] _references =
    [
        MetadataReference.CreateFromFile( typeof(object).Assembly.Location ),
        MetadataReference.CreateFromFile(
            Path.Combine( Path.GetDirectoryName( typeof(object).Assembly.Location )!, "netstandard.dll" ) ),
        MetadataReference.CreateFromFile(
            Path.Combine( Path.GetDirectoryName( typeof(object).Assembly.Location )!, "System.Runtime.dll" ) ),
        MetadataReference.CreateFromFile(
            Path.Combine( Path.GetDirectoryName( typeof(object).Assembly.Location )!, "System.Text.RegularExpressions.dll" ) ),
        MetadataReference.CreateFromFile( typeof(DurableAttribute).Assembly.Location )
    ];

    private const string _code = """
                                 using Metalama.Framework.Utilities;

                                 [Durable]
                                 class A
                                 {
                                     private Opaque? _opaque;
                                     private Marked? _marked;
                                 }

                                 // Deliberately declared here rather than taken from the base class library, so that
                                 // the test does not depend on a type being absent from WellKnownDurableTypes.
                                 class Opaque { }

                                 [Durable]
                                 class Marked { }
                                 """;

    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(
        string? durableTypes,
        string? nonDurableTypes )
    {
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText( _code, new CSharpParseOptions( LanguageVersion.CSharp12 ) )],
            _references,
            new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary ) );

        var options = new AnalyzerOptions(
            ImmutableArray<AdditionalText>.Empty,
            new TestOptionsProvider( durableTypes, nonDurableTypes ) );

        var diagnostics = await compilation
            .WithAnalyzers( ImmutableArray.Create<DiagnosticAnalyzer>( new DurableContractAnalyzer() ), options )
            .GetAnalyzerDiagnosticsAsync();

        return diagnostics.Where( d => d.Id.StartsWith( "LAMA087", StringComparison.Ordinal ) ).ToImmutableArray();
    }

    [Fact]
    public async Task WithoutTheItem_AnUnknownTypeIsNotDurable()
    {
        var diagnostics = await GetDiagnosticsAsync( null, null );

        Assert.Single( diagnostics );
        Assert.Contains( "Opaque", diagnostics[0].GetMessage(), StringComparison.Ordinal );
    }

    [Fact]
    public async Task MetalamaDurableType_MakesTheTypeDurable()
    {
        var diagnostics = await GetDiagnosticsAsync( "Opaque", null );

        Assert.Empty( diagnostics.Select( d => d.GetMessage() ) );
    }

    [Fact]
    public async Task TheListIsSemicolonSeparatedAndTrimmed()
    {
        var diagnostics = await GetDiagnosticsAsync( " System.String ; Opaque ; ", null );

        Assert.Empty( diagnostics.Select( d => d.GetMessage() ) );
    }

    /// <remarks>
    /// The non-durable list wins, so that a project can override a verdict of the built-in table or of its own
    /// durable list rather than only add to it.
    /// </remarks>
    [Fact]
    public async Task MetalamaNonDurableType_WinsOverTheDurableList()
    {
        var diagnostics = await GetDiagnosticsAsync(
            "Opaque",
            "Opaque" );

        Assert.Single( diagnostics );
        Assert.Contains( "MetalamaNonDurableType", diagnostics[0].GetMessage(), StringComparison.Ordinal );
    }

    /// <remarks>
    /// Without this rule a mistyped entry is a rule that silently never applies, which is the worst outcome for a
    /// mechanism whose whole purpose is to suppress a warning.
    /// </remarks>
    [Fact]
    public async Task AMistypedTypeName_IsReported()
    {
        var diagnostics = await GetDiagnosticsAsync( "System.Text.RegularExpressions.Regexx", null );

        Assert.Contains( diagnostics, d => d.Id == "LAMA0879" );
        Assert.Contains( diagnostics, d => d.GetMessage().Contains( "MetalamaDurableType", StringComparison.Ordinal ) );
    }

    /// <remarks>
    /// A generic type must carry its arity, because the analyzer matches the full metadata name.
    /// </remarks>
    [Fact]
    public async Task AGenericNameWithoutItsArity_IsReported()
    {
        var diagnostics = await GetDiagnosticsAsync( "System.Collections.Generic.List", null );

        Assert.Contains( diagnostics, d => d.Id == "LAMA0879" );
    }

    [Fact]
    public async Task AGenericNameWithItsArity_IsNotReported()
    {
        var diagnostics = await GetDiagnosticsAsync( "System.Collections.Generic.List`1", null );

        Assert.DoesNotContain( diagnostics, d => d.Id == "LAMA0879" );
    }

    [Fact]
    public async Task MetalamaNonDurableType_OverridesAMarkedType()
    {
        var diagnostics = await GetDiagnosticsAsync( null, "Marked" );

        Assert.Equal( 2, diagnostics.Length );
        Assert.Contains( diagnostics, d => d.GetMessage().Contains( "Marked", StringComparison.Ordinal ) );
    }

    /// <summary>
    /// Supplies the two compiler-visible properties that the build would otherwise compute.
    /// </summary>
    private sealed class TestOptionsProvider( string? durableTypes, string? nonDurableTypes ) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new TestOptions( durableTypes, nonDurableTypes );

        public override AnalyzerConfigOptions GetOptions( SyntaxTree tree ) => TestOptions.Empty;

        public override AnalyzerConfigOptions GetOptions( AdditionalText textFile ) => TestOptions.Empty;

        private sealed class TestOptions( string? durableTypes, string? nonDurableTypes ) : AnalyzerConfigOptions
        {
            public static readonly TestOptions Empty = new( null, null );

            public override bool TryGetValue( string key, [NotNullWhen( true )] out string? value )
            {
                value = key switch
                {
                    "build_property.MetalamaDurableTypes" => durableTypes,
                    "build_property.MetalamaNonDurableTypes" => nonDurableTypes,
                    _ => null
                };

                return value != null;
            }
        }
    }
}
