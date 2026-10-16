// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Templating.Immutability;

/// <summary>
/// Tests of the <c>MetalamaImmutableType</c> and <c>MetalamaImmutableContractType</c> MSBuild items, by which a
/// project declares the immutability of a type it does not own.
/// </summary>
/// <remarks>
/// These tests cover the side that Metalama owns, that is, reading the three compiler-visible properties. They cannot
/// cover the build target that joins the items into those properties, which lives in
/// <c>Metalama.CompilerVisibleProperties.props</c>. Only a build of a project that consumes the package exercises
/// that, and the target name is the part that was wrong the first time.
/// </remarks>
public sealed class ImmutableMsBuildTypeListTests : ImmutabilityTestBase
{
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

    private Task<ImmutableArray<Diagnostic>> GetDiagnosticsWithItemsAsync(
        string? immutableTypes,
        string? contractTypes = null,
        string? code = null,
        string? enforce = null )
    {
        var properties = new Dictionary<string, string>();

        if ( immutableTypes != null )
        {
            properties["MetalamaImmutableTypes"] = immutableTypes;
        }

        if ( contractTypes != null )
        {
            properties["MetalamaImmutableContractTypes"] = contractTypes;
        }

        if ( enforce != null )
        {
            properties["MetalamaEnforceImmutabilityContract"] = enforce;
        }

        return this.GetDiagnosticsAsync( code ?? _code, properties );
    }

    /// <summary>
    /// Writes a compiler-visible property in the form of the <c>.editorconfig</c> file that the build generates, parses
    /// it with the parser of Roslyn, and returns the value that the analyzer receives.
    /// </summary>
    /// <remarks>
    /// The items are joined with <c>,</c>, which is the separator of the targets of
    /// <c>Metalama.CompilerVisibleProperties.props</c>. The parser of Roslyn treats <c>;</c> and <c>#</c> as the start
    /// of a comment, so a test that sets the property value directly does not detect a separator that the parser
    /// truncates.
    /// </remarks>
    private static string ReadThroughEditorConfig( string propertyName, params string[] items )
    {
        var text = $"is_global = true\nbuild_property.{propertyName} = {string.Join( ",", items )}\n";
        var config = AnalyzerConfig.Parse( text, Path.Combine( Path.GetTempPath(), "Test.GeneratedMSBuildEditorConfig.editorconfig" ) );
        var configSet = AnalyzerConfigSet.Create( ImmutableArray.Create( config ) );

        Assert.True( configSet.GlobalConfigOptions.AnalyzerOptions.TryGetValue( $"build_property.{propertyName}", out var value ) );

        return value;
    }

    [Fact]
    public async Task WithoutTheItem_AnUnknownTypeIsMutable()
    {
        var diagnostics = await this.GetDiagnosticsWithItemsAsync( null );

        Assert.Single( diagnostics );
        Assert.Contains( "Opaque", diagnostics[0].GetMessage(), StringComparison.Ordinal );
    }

    [Fact]
    public async Task MetalamaImmutableType_MakesTheTypeImmutable()
    {
        var diagnostics = await this.GetDiagnosticsWithItemsAsync( "Opaque" );

        Assert.Empty( diagnostics.Select( d => d.GetMessage() ) );
    }

    [Fact]
    public async Task TheListIsSemicolonSeparatedAndTrimmed()
    {
        var diagnostics = await this.GetDiagnosticsWithItemsAsync( " System.String ; Opaque ; " );

        Assert.Empty( diagnostics.Select( d => d.GetMessage() ) );
    }

    [Fact]
    public async Task SeveralMetalamaImmutableTypeItems_AreAllHonoredAfterTheEditorConfigRoundTrip()
    {
        var diagnostics = await this.GetDiagnosticsWithItemsAsync( ReadThroughEditorConfig( "MetalamaImmutableTypes", "System.String", "Opaque" ) );

        Assert.Empty( diagnostics.Select( d => d.GetMessage() ) );
    }

    /// <remarks>
    /// Without this rule a mistyped entry is a rule that silently never applies, which is the worst outcome for a
    /// mechanism whose whole purpose is to suppress a warning.
    /// </remarks>
    [Fact]
    public async Task AMistypedTypeName_IsReported()
    {
        var diagnostics = await this.GetDiagnosticsWithItemsAsync( "System.Text.RegularExpressions.Regexx" );

        Assert.Contains( diagnostics, d => d.Id == "LAMA0885" );
        Assert.Contains( diagnostics, d => d.GetMessage().Contains( "MetalamaImmutableType", StringComparison.Ordinal ) );
    }

    [Fact]
    public async Task AGenericNameWithoutItsArity_IsReported()
    {
        var diagnostics = await this.GetDiagnosticsWithItemsAsync( "System.Collections.Generic.List" );

        Assert.Contains( diagnostics, d => d.Id == "LAMA0885" );
    }

    [Fact]
    public async Task AGenericNameWithItsArity_IsNotReported()
    {
        var diagnostics = await this.GetDiagnosticsWithItemsAsync( "System.Collections.Generic.List`1" );

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

        var withoutTheItem = await this.GetDiagnosticsWithItemsAsync( null, null, code );

        Assert.Empty( withoutTheItem.Select( d => d.GetMessage() ) );

        var withTheItem = await this.GetDiagnosticsWithItemsAsync( null, "IValidator", code );

        Assert.Single( withTheItem );
        Assert.Equal( "LAMA0880", withTheItem[0].Id );
    }

    [Fact]
    public async Task SeveralMetalamaImmutableContractTypeItems_AreAllHonoredAfterTheEditorConfigRoundTrip()
    {
        const string code = """
                            interface IOther { }

                            interface IValidator { }

                            class MyValidator : IValidator
                            {
                                public int Count;
                            }
                            """;

        var diagnostics = await this.GetDiagnosticsWithItemsAsync(
            null,
            ReadThroughEditorConfig( "MetalamaImmutableContractTypes", "IOther", "IValidator" ),
            code );

        Assert.Single( diagnostics );
        Assert.Equal( "LAMA0880", diagnostics[0].Id );
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
        var enforced = await this.GetDiagnosticsWithItemsAsync( null );

        Assert.NotEmpty( enforced );

        var notEnforced = await this.GetDiagnosticsWithItemsAsync( null, null, null, "false" );

        Assert.Empty( notEnforced.Select( d => d.GetMessage() ) );
    }

    [Theory]
    [InlineData( "true" )]
    [InlineData( "True" )]
    [InlineData( "" )]
    [InlineData( "not-a-boolean" )]
    public async Task AnyOtherValueOfTheSwitch_LeavesTheContractEnforced( string value )
    {
        var diagnostics = await this.GetDiagnosticsWithItemsAsync( null, null, null, value );

        Assert.NotEmpty( diagnostics );
    }
}
