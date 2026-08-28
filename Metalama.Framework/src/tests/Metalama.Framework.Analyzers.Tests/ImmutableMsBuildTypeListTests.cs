// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Metalama.Framework.Utilities;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Metalama.Framework.Analyzers.Tests;

/// <summary>
/// Tests of the <c>MetalamaImmutableType</c>, <c>MetalamaMutableType</c> and <c>MetalamaImmutableContractType</c>
/// MSBuild items, by which a project declares the immutability of a type it does not own.
/// </summary>
/// <remarks>
/// These tests cover the side the analyzer owns, that is, reading the three compiler-visible properties. They cannot
/// cover the build target that joins the items into those properties, which lives in
/// <c>Metalama.CompilerVisibleProperties.props</c>. Only a build of a project that consumes the package exercises
/// that, and the target name is the part that was wrong the first time.
/// </remarks>
public sealed class ImmutableMsBuildTypeListTests
{
    private static readonly MetadataReference[] _references =
    [
        MetadataReference.CreateFromFile( typeof(object).Assembly.Location ),
        MetadataReference.CreateFromFile(
            Path.Combine( Path.GetDirectoryName( typeof(object).Assembly.Location )!, "netstandard.dll" ) ),
        MetadataReference.CreateFromFile(
            Path.Combine( Path.GetDirectoryName( typeof(object).Assembly.Location )!, "System.Runtime.dll" ) ),
        MetadataReference.CreateFromFile(
            Path.Combine( Path.GetDirectoryName( typeof(object).Assembly.Location )!, "System.Collections.dll" ) ),
        MetadataReference.CreateFromFile(
            Path.Combine( Path.GetDirectoryName( typeof(object).Assembly.Location )!, "System.Text.RegularExpressions.dll" ) ),
        MetadataReference.CreateFromFile( typeof(ImmutableTypeAttribute).Assembly.Location ),
        MetadataReference.CreateFromFile( typeof(Metalama.Framework.Aspects.IAspect).Assembly.Location )
    ];

    private const string _code = """
                                 using Metalama.Framework.Utilities;

                                 [ImmutableType]
                                 class A
                                 {
                                     private readonly Opaque? _opaque;
                                     private readonly Marked? _marked;
                                 }

                                 // Deliberately declared here rather than taken from the base class library, so that
                                 // the test does not depend on a type being absent from WellKnownImmutableTypes.
                                 class Opaque { }

                                 [ImmutableType]
                                 class Marked { }
                                 """;

    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(
        string? immutableTypes,
        string? mutableTypes,
        string? contractTypes = null,
        string? code = null,
        string? enforce = null )
    {
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText( code ?? _code, new CSharpParseOptions( LanguageVersion.CSharp12 ) )],
            _references,
            new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary ) );

        var options = new AnalyzerOptions(
            ImmutableArray<AdditionalText>.Empty,
            new TestOptionsProvider( immutableTypes, mutableTypes, contractTypes, enforce ) );

        var diagnostics = await compilation
            .WithAnalyzers( ImmutableArray.Create<DiagnosticAnalyzer>( new ImmutableContractAnalyzer() ), options )
            .GetAnalyzerDiagnosticsAsync();

        return diagnostics.Where( d => d.Id.StartsWith( "LAMA088", StringComparison.Ordinal ) ).ToImmutableArray();
    }

    [Fact]
    public async Task WithoutTheItem_AnUnknownTypeIsMutable()
    {
        var diagnostics = await GetDiagnosticsAsync( null, null );

        Assert.Single( diagnostics );
        Assert.Contains( "Opaque", diagnostics[0].GetMessage(), StringComparison.Ordinal );
    }

    [Fact]
    public async Task MetalamaImmutableType_MakesTheTypeImmutable()
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
    /// The mutable list wins, so that a project can override a verdict of the built-in table or of its own immutable
    /// list rather than only add to it.
    /// </remarks>
    [Fact]
    public async Task MetalamaMutableType_WinsOverTheImmutableList()
    {
        var diagnostics = await GetDiagnosticsAsync( "Opaque", "Opaque" );

        Assert.Single( diagnostics );
        Assert.Contains( "MetalamaMutableType", diagnostics[0].GetMessage(), StringComparison.Ordinal );
    }

    [Fact]
    public async Task MetalamaMutableType_OverridesAMarkedType()
    {
        var diagnostics = await GetDiagnosticsAsync( null, "Marked" );

        Assert.Equal( 2, diagnostics.Length );
        Assert.Contains( diagnostics, d => d.GetMessage().Contains( "Marked", StringComparison.Ordinal ) );
    }

    /// <remarks>
    /// Without this rule a mistyped entry is a rule that silently never applies, which is the worst outcome for a
    /// mechanism whose whole purpose is to suppress a warning.
    /// </remarks>
    [Fact]
    public async Task AMistypedTypeName_IsReported()
    {
        var diagnostics = await GetDiagnosticsAsync( "System.Text.RegularExpressions.Regexx", null );

        Assert.Contains( diagnostics, d => d.Id == "LAMA0885" );
        Assert.Contains( diagnostics, d => d.GetMessage().Contains( "MetalamaImmutableType", StringComparison.Ordinal ) );
    }

    [Fact]
    public async Task AGenericNameWithoutItsArity_IsReported()
    {
        var diagnostics = await GetDiagnosticsAsync( "System.Collections.Generic.List", null );

        Assert.Contains( diagnostics, d => d.Id == "LAMA0885" );
    }

    [Fact]
    public async Task AGenericNameWithItsArity_IsNotReported()
    {
        var diagnostics = await GetDiagnosticsAsync( "System.Collections.Generic.List`1", null );

        Assert.DoesNotContain( diagnostics, d => d.Id == "LAMA0885" );
    }

    /// <summary>
    /// The item by which Metalama.Premium, or a project, names a base type whose implementations must be immutable
    /// without that type declaring the marker itself.
    /// </summary>
    [Fact]
    public async Task MetalamaImmutableContractType_BindsTheImplementations()
    {
        const string code = """
                            interface IValidator { }

                            class MyValidator : IValidator
                            {
                                public int Count;
                            }
                            """;

        var withoutTheItem = await GetDiagnosticsAsync( null, null, null, code );

        Assert.Empty( withoutTheItem.Select( d => d.GetMessage() ) );

        var withTheItem = await GetDiagnosticsAsync( null, null, "IValidator", code );

        Assert.Single( withTheItem );
        Assert.Equal( "LAMA0880", withTheItem[0].Id );
    }

    /// <summary>
    /// The switch that turns the whole contract off for a project.
    /// </summary>
    /// <remarks>
    /// The contract is written for user code. A project that implements the framework itself declares code-model
    /// builders and other types whose mutability is deliberate, and verifies them by its own tests instead. The
    /// durability contract deliberately has no equivalent switch, because durability is hardest to get right
    /// precisely in framework code.
    /// </remarks>
    [Fact]
    public async Task MetalamaEnforceImmutabilityContractFalse_SilencesEverything()
    {
        var enforced = await GetDiagnosticsAsync( null, null );

        Assert.NotEmpty( enforced );

        var notEnforced = await GetDiagnosticsAsync( null, null, null, null, "false" );

        Assert.Empty( notEnforced.Select( d => d.GetMessage() ) );
    }

    [Theory]
    [InlineData( "true" )]
    [InlineData( "True" )]
    [InlineData( "" )]
    [InlineData( "not-a-boolean" )]
    public async Task AnyOtherValueOfTheSwitch_LeavesTheContractEnforced( string value )
    {
        var diagnostics = await GetDiagnosticsAsync( null, null, null, null, value );

        Assert.NotEmpty( diagnostics );
    }

    /// <summary>
    /// Supplies the compiler-visible properties that the build would otherwise compute.
    /// </summary>
    private sealed class TestOptionsProvider(
        string? immutableTypes,
        string? mutableTypes,
        string? contractTypes,
        string? enforce ) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new TestOptions( immutableTypes, mutableTypes, contractTypes, enforce );

        public override AnalyzerConfigOptions GetOptions( SyntaxTree tree ) => TestOptions.Empty;

        public override AnalyzerConfigOptions GetOptions( AdditionalText textFile ) => TestOptions.Empty;

        private sealed class TestOptions(
            string? immutableTypes,
            string? mutableTypes,
            string? contractTypes,
            string? enforce ) : AnalyzerConfigOptions
        {
            public static readonly TestOptions Empty = new( null, null, null, null );

            public override bool TryGetValue( string key, [NotNullWhen( true )] out string? value )
            {
                value = key switch
                {
                    "build_property.MetalamaImmutableTypes" => immutableTypes,
                    "build_property.MetalamaMutableTypes" => mutableTypes,
                    "build_property.MetalamaImmutableContractTypes" => contractTypes,
                    "build_property.MetalamaEnforceImmutabilityContract" => enforce,
                    _ => null
                };

                return value != null;
            }
        }
    }
}
