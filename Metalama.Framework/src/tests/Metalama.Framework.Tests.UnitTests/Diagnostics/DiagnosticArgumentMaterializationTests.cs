// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Utilities;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Diagnostics;

/// <summary>
/// Tests that materializing the compilation-bound arguments of a diagnostic ahead of time does not change the message
/// the diagnostic produces.
/// </summary>
/// <remarks>
/// <para>
/// A diagnostic reported on a declaration used to keep the declaration, and therefore the whole compilation, for as
/// long as the diagnostic was stored, which at design time is the whole editing session (issue #1799). The arguments
/// that can reach a compilation are now formatted when the diagnostic is created rather than when its message is
/// requested.
/// </para>
/// <para>
/// The risk this creates is that formatting early is not the same as formatting late.
/// <see cref="MetalamaStringFormatter"/> passes the format specifier of the composite format string to an argument that
/// implements <see cref="System.IFormattable"/>, so an argument materialized ahead of time would have its specifier
/// applied to a string instead: <c>{0:N2}</c> would lose its grouping and <c>{0:x}</c> would throw. Each test below is
/// a category of argument, and each asserts that the message is the one late formatting would have produced.
/// </para>
/// <para>
/// Two of the categories, <see cref="bool"/> and <see cref="char"/>, are excluded from materialization for economy
/// rather than for correctness, because neither reads the format specifier and the message is the same either way. The
/// tests over them therefore pass under either decision; they are here because the categories belong in the list, not
/// because they discriminate.
/// </para>
/// </remarks>
public sealed class DiagnosticArgumentMaterializationTests : UnitTestClass
{
    private const string _format = "The value is {0}.";

    private static readonly DiagnosticDefinition<bool> _boolean = new( "MY001", Severity.Warning, _format, "Title", "Category" );

    private static readonly DiagnosticDefinition<char> _character = new( "MY002", Severity.Warning, _format, "Title", "Category" );

    private static readonly DiagnosticDefinition<int> _hexadecimal = new( "MY003", Severity.Warning, "The value is {0:x}.", "Title", "Category" );

    private static readonly DiagnosticDefinition<double> _grouped = new( "MY004", Severity.Warning, "The value is {0:N2}.", "Title", "Category" );

    private static readonly DiagnosticDefinition<Severity> _enumeration = new( "MY005", Severity.Warning, _format, "Title", "Category" );

    private static readonly DiagnosticDefinition<string[]> _strings = new( "MY006", Severity.Warning, _format, "Title", "Category" );

    private static readonly DiagnosticDefinition<IDeclaration> _declaration = new( "MY007", Severity.Warning, _format, "Title", "Category" );

    /// <summary>
    /// Installs the implementation of <see cref="MetalamaStringFormatter"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="MetalamaStringFormatter.Instance"/> throws until the static constructor of
    /// <see cref="MetalamaEngineModuleInitializer"/> has run, and no test of this class creates a
    /// <see cref="TestContext"/>, which is what triggers that constructor in the other test classes. Without this
    /// static constructor, the tests below pass only when another test class has already run in the same process,
    /// which the test runner does not guarantee.
    /// </remarks>
    static DiagnosticArgumentMaterializationTests()
    {
        MetalamaEngineModuleInitializer.EnsureInitialized();
    }

    /// <summary>
    /// Returns the message that the diagnostic produces, which is formatted when this method is called and not before.
    /// </summary>
    private static string GetMessage<T>( DiagnosticDefinition<T> definition, T arguments )
        where T : notnull
        => definition.CreateRoslynDiagnostic( null, arguments ).GetMessage();

    /// <summary>
    /// Returns the message that late formatting would have produced, which is the reference every test compares
    /// against.
    /// </summary>
    private static string FormatLate( string format, params object?[] arguments )
        => string.Format( MetalamaStringFormatter.Instance, format, arguments );

    [Fact]
    public void BooleanArgumentIsFormattedAsLateFormattingWould() => Assert.Equal( FormatLate( _format, true ), GetMessage( _boolean, true ) );

    [Fact]
    public void CharacterArgumentIsFormattedAsLateFormattingWould() => Assert.Equal( FormatLate( _format, 'x' ), GetMessage( _character, 'x' ) );

    /// <summary>
    /// The specifier that would throw if it reached a string rather than the number it was written for.
    /// </summary>
    [Fact]
    public void HexadecimalSpecifierReachesTheArgument()
    {
        Assert.Equal( "The value is ff.", GetMessage( _hexadecimal, 255 ) );
    }

    /// <summary>
    /// The specifier that would be silently lost, which is the worse of the two failures because nothing reports it.
    /// </summary>
    [Fact]
    public void GroupingSpecifierReachesTheArgument()
    {
        Assert.Equal( FormatLate( "The value is {0:N2}.", 1234.5678 ), GetMessage( _grouped, 1234.5678 ) );
        Assert.Contains( "1", GetMessage( _grouped, 1234.5678 ), System.StringComparison.Ordinal );
    }

    [Fact]
    public void EnumerationArgumentIsFormattedAsLateFormattingWould()
        => Assert.Equal( FormatLate( _format, Severity.Error ), GetMessage( _enumeration, Severity.Error ) );

    /// <summary>
    /// An array of strings has a presentation of its own, which materializing the array as a whole would lose.
    /// </summary>
    [Fact]
    public void StringArrayArgumentIsFormattedAsLateFormattingWould()
    {
        var value = new[] { "a", "b" };

        Assert.Equal( FormatLate( _format, (object)value ), GetMessage( _strings, value ) );
    }

    /// <summary>
    /// The category the whole mechanism exists for: the argument is materialized, and the message is nevertheless the
    /// one late formatting would have produced.
    /// </summary>
    [Fact]
    public void DeclarationArgumentIsFormattedAsLateFormattingWould()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "class C { void M() {} }" );
        var method = compilation.Types.OfName( "C" ).Single().Methods.OfName( "M" ).Single();

        Assert.Equal( FormatLate( _format, method ), GetMessage( _declaration, method ) );
    }
}
