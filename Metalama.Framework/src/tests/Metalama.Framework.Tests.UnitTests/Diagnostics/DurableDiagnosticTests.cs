// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Utilities;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Globalization;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Diagnostics;

/// <summary>
/// Tests <see cref="DurableDiagnostic"/>, which stores a diagnostic without the syntax tree of its location and binds
/// it to a tree again when it is reported.
/// </summary>
/// <remarks>
/// <para>
/// The design-time pipeline keeps the diagnostics of a file for as long as the file is not analysed again, which at
/// design time is the whole editing session. A diagnostic that held its location held the syntax tree of the run that
/// reported it, and through nothing else: a location is the only part of a diagnostic that names a tree.
/// </para>
/// <para>
/// What the conversion must not lose is what the editor shows. Each test below is one such part: the file, the line
/// and column, the span, the message, the severity and the properties that the code fix provider reads.
/// </para>
/// </remarks>
public sealed class DurableDiagnosticTests : UnitTestClass
{
    private static readonly DiagnosticDefinition<string> _definition =
        new( "MY001", Severity.Warning, "The value is {0}.", "Title", "Category" );

    private const string _code = """
                                 class C
                                 {
                                     void M() { }
                                 }
                                 """;

    /// <remarks>
    /// <see cref="MetalamaStringFormatter.Instance"/> throws until the static constructor of
    /// <see cref="MetalamaEngineModuleInitializer"/> has run, and no test of this class creates a
    /// <see cref="TestContext"/>. Without this, the tests pass only when another class has already run in the process.
    /// </remarks>
    static DurableDiagnosticTests()
    {
        MetalamaEngineModuleInitializer.EnsureInitialized();
    }

    private static SyntaxTree CreateSyntaxTree( string code = _code, string path = "Class1.cs" )
        => CSharpSyntaxTree.ParseText( code, path: path );

    /// <summary>
    /// Creates a diagnostic of Metalama, located on the declaration of the method of <see cref="_code"/>.
    /// </summary>
    private static Diagnostic CreateDiagnostic( SyntaxTree syntaxTree, string argument = "x" )
        => _definition.CreateRoslynDiagnostic( Location.Create( syntaxTree, GetMethodSpan( syntaxTree ) ), argument );

    private static TextSpan GetMethodSpan( SyntaxTree syntaxTree )
    {
        var index = syntaxTree.ToString().IndexOf( "void M", System.StringComparison.Ordinal );

        return new TextSpan( index, "void M".Length );
    }

    #region Detaching

    [Fact]
    public void TheStoredDiagnosticHoldsNoSyntaxTree()
    {
        var durable = DurableDiagnostic.Create( CreateDiagnostic( CreateSyntaxTree() ) );

        Assert.Null( durable.Location.SourceTree );
        Assert.Equal( LocationKind.ExternalFile, durable.Location.Kind );
    }

    /// <remarks>
    /// The part the editor uses to place the squiggle. An external location carries it, which is why the file path and
    /// the line and column span are what the conversion stores.
    /// </remarks>
    [Fact]
    public void TheFileAndTheLineAndColumnSpanSurviveTheConversion()
    {
        var syntaxTree = CreateSyntaxTree();
        var diagnostic = CreateDiagnostic( syntaxTree );

        var durable = DurableDiagnostic.Create( diagnostic );

        Assert.Equal( diagnostic.Location.GetLineSpan(), durable.Location.GetLineSpan() );
        Assert.Equal( "Class1.cs", durable.Location.GetLineSpan().Path );
    }

    [Fact]
    public void TheTextSpanSurvivesTheConversion()
    {
        var diagnostic = CreateDiagnostic( CreateSyntaxTree() );

        Assert.Equal( diagnostic.Location.SourceSpan, DurableDiagnostic.Create( diagnostic ).Location.SourceSpan );
    }

    [Fact]
    public void TheIdentifierAndTheMessageSurviveTheConversion()
    {
        var diagnostic = CreateDiagnostic( CreateSyntaxTree(), "the argument" );

        var durable = DurableDiagnostic.Create( diagnostic );

        Assert.Equal( "MY001", durable.Id );
        Assert.Equal( "The value is the argument.", durable.GetMessage( CultureInfo.InvariantCulture ) );
    }

    /// <remarks>
    /// A diagnostic with no location has nothing to detach, and the conversion has to leave it alone rather than turn
    /// <see cref="Location.None"/> into an external location naming an empty path.
    /// </remarks>
    [Fact]
    public void ADiagnosticWithoutALocationIsUnchanged()
    {
        var durable = DurableDiagnostic.Create( _definition.CreateRoslynDiagnostic( null, "x" ) );

        Assert.Equal( LocationKind.None, durable.Location.Kind );
        Assert.Equal( "The value is x.", durable.GetMessage( CultureInfo.InvariantCulture ) );
    }

    #endregion

    #region Reattaching

    [Fact]
    public void ReportingBindsTheLocationToTheGivenTree()
    {
        var syntaxTree = CreateSyntaxTree();
        var diagnostic = CreateDiagnostic( syntaxTree );

        var reattached = DurableDiagnostic.Create( diagnostic ).ToDiagnostic( syntaxTree );

        Assert.Same( syntaxTree, reattached.Location.SourceTree );
        Assert.Equal( diagnostic.Location.SourceSpan, reattached.Location.SourceSpan );
        Assert.Equal( diagnostic.Location.GetLineSpan(), reattached.Location.GetLineSpan() );
    }

    /// <remarks>
    /// The tree of a later version of the same file is a different object, and binding to it is the point: the result
    /// is carried forward across edits of other files, so the tree it is reported against is never the one it was
    /// produced from.
    /// </remarks>
    [Fact]
    public void ReportingBindsToATreeOfALaterVersionOfTheFile()
    {
        var durable = DurableDiagnostic.Create( CreateDiagnostic( CreateSyntaxTree() ) );

        var laterTree = CreateSyntaxTree();
        var reattached = durable.ToDiagnostic( laterTree );

        Assert.Same( laterTree, reattached.Location.SourceTree );
    }

    [Fact]
    public void ReportingWithoutATreeKeepsTheExternalLocation()
    {
        var durable = DurableDiagnostic.Create( CreateDiagnostic( CreateSyntaxTree() ) );

        Assert.Null( durable.ToDiagnostic( null ).Location.SourceTree );
    }

    /// <remarks>
    /// The guard against a span that no longer fits. Binding it would throw, and a location pointing into a text that
    /// has changed under it would be wrong even if it did not.
    /// </remarks>
    [Fact]
    public void ReportingAgainstAShorterTreeKeepsTheExternalLocation()
    {
        var durable = DurableDiagnostic.Create( CreateDiagnostic( CreateSyntaxTree() ) );

        var shorterTree = CreateSyntaxTree( "class C;" );
        var reattached = durable.ToDiagnostic( shorterTree );

        Assert.Null( reattached.Location.SourceTree );
        Assert.Equal( LocationKind.ExternalFile, reattached.Location.Kind );
    }

    #endregion

    #region What the consumers read

    [Fact]
    public void TheSeveritySurvivesTheConversion()
    {
        var diagnostic = CreateDiagnostic( CreateSyntaxTree() );

        Assert.Equal( DiagnosticSeverity.Warning, DurableDiagnostic.Create( diagnostic ).ToDiagnostic( null ).Severity );
    }

    /// <remarks>
    /// The effective severity of a diagnostic may differ from the default of its descriptor, and it is the effective
    /// one the editor shows.
    /// </remarks>
    [Fact]
    public void AnOverriddenSeveritySurvivesTheConversion()
    {
        var syntaxTree = CreateSyntaxTree();

        var descriptor = new DiagnosticDescriptor( "MY100", "Title", "Message", "Category", DiagnosticSeverity.Warning, true );

        var diagnostic = Diagnostic.Create(
            descriptor,
            Location.Create( syntaxTree, GetMethodSpan( syntaxTree ) ),
            DiagnosticSeverity.Error,
            null,
            null );

        Assert.Equal( DiagnosticSeverity.Error, DurableDiagnostic.Create( diagnostic ).ToDiagnostic( syntaxTree ).Severity );
    }

    /// <remarks>
    /// The code fix provider of the design-time package reads the properties of a diagnostic to find the fix that goes
    /// with it, so losing them would silently remove the code fixes.
    /// </remarks>
    [Fact]
    public void ThePropertiesSurviveTheConversion()
    {
        var syntaxTree = CreateSyntaxTree();

        var diagnostic = _definition.CreateRoslynDiagnostic(
            Location.Create( syntaxTree, GetMethodSpan( syntaxTree ) ),
            "x",
            properties: ImmutableDictionary<string, string?>.Empty.Add( "TheKey", "TheValue" ) );

        var reattached = DurableDiagnostic.Create( diagnostic ).ToDiagnostic( syntaxTree );

        Assert.Equal( "TheValue", reattached.Properties["TheKey"] );
    }

    #endregion

    #region A diagnostic of another source

    /// <remarks>
    /// A diagnostic that Metalama did not create holds its message arguments where no public member exposes them, so
    /// the conversion formats the message instead of copying the arguments. What this asserts is that the arguments
    /// are not simply dropped, which would leave the placeholders in the message.
    /// </remarks>
    [Fact]
    public void TheMessageOfAForeignDiagnosticIsFormattedDuringTheConversion()
    {
        var syntaxTree = CreateSyntaxTree();

        var descriptor = new DiagnosticDescriptor( "XY001", "Title", "The value is {0}.", "Category", DiagnosticSeverity.Warning, true );

        var diagnostic = Diagnostic.Create( descriptor, Location.Create( syntaxTree, GetMethodSpan( syntaxTree ) ), "the argument" );

        var durable = DurableDiagnostic.Create( diagnostic );

        Assert.Equal( "The value is the argument.", durable.GetMessage( CultureInfo.InvariantCulture ) );
        Assert.Equal( "XY001", durable.Id );
    }

    /// <remarks>
    /// The message of a foreign diagnostic is stored as a formatted string, and a message that happened to contain a
    /// brace would be formatted a second time if it were stored as a composite format string.
    /// </remarks>
    [Fact]
    public void AFormattedMessageContainingABraceIsNotFormattedAgain()
    {
        var syntaxTree = CreateSyntaxTree();

        var descriptor = new DiagnosticDescriptor( "XY002", "Title", "The value is {0}.", "Category", DiagnosticSeverity.Warning, true );

        var diagnostic = Diagnostic.Create( descriptor, Location.Create( syntaxTree, GetMethodSpan( syntaxTree ) ), "{not a placeholder}" );

        Assert.Equal(
            "The value is {not a placeholder}.",
            DurableDiagnostic.Create( diagnostic ).GetMessage( CultureInfo.InvariantCulture ) );
    }

    #endregion

    #region The assertion on the arguments

    /// <remarks>
    /// <para>
    /// The check that runs in a debug build only. An argument that reaches a compilation is what the materialization
    /// performed when the diagnostic is created exists to prevent, and that materialization recognizes a list of
    /// types. This asserts that an argument the list does not cover is reported rather than stored.
    /// </para>
    /// <para>
    /// The argument is a syntax tree wrapped in an object the list does not name, so it is not materialized and the
    /// walk has to find it through a field. A test that passed the tree directly would pass even if the walk followed
    /// nothing.
    /// </para>
    /// </remarks>
    [Fact]
    public void AnArgumentThatReachesACompilationIsReported()
    {
#if DEBUG
        var syntaxTree = CreateSyntaxTree();

        var descriptor = new DiagnosticDescriptor(
            "MY200",
            "Title",
            new NonLocalizedString( "The value is {0}.", [new Holder( syntaxTree )] ),
            "Category",
            DiagnosticSeverity.Warning,
            true );

        var diagnostic = Diagnostic.Create( descriptor, Location.Create( syntaxTree, GetMethodSpan( syntaxTree ) ) );

        var exception = Assert.Throws<AssertionFailedException>( () => DurableDiagnostic.Create( diagnostic ) );

        Assert.Contains( "MY200", exception.Message, System.StringComparison.Ordinal );
#endif
    }

    [Fact]
    public void AnArgumentThatReachesNoCompilationIsAccepted()
    {
        var syntaxTree = CreateSyntaxTree();

        var descriptor = new DiagnosticDescriptor(
            "MY201",
            "Title",
            new NonLocalizedString( "The value is {0}.", [new Holder( null )] ),
            "Category",
            DiagnosticSeverity.Warning,
            true );

        var diagnostic = Diagnostic.Create( descriptor, Location.Create( syntaxTree, GetMethodSpan( syntaxTree ) ) );

        Assert.Equal( "MY201", DurableDiagnostic.Create( diagnostic ).Id );
    }

    /// <summary>
    /// An object that the materialization does not recognize, so that the walk has to reach the syntax tree through
    /// its field rather than find it among the arguments.
    /// </summary>
    private sealed class Holder
    {
#pragma warning disable IDE0052 // The field is never read: holding the tree is its entire purpose.
        private readonly SyntaxTree? _syntaxTree;
#pragma warning restore IDE0052

        public Holder( SyntaxTree? syntaxTree )
        {
            this._syntaxTree = syntaxTree;
        }

        public override string ToString() => "a holder";
    }

    #endregion
}
