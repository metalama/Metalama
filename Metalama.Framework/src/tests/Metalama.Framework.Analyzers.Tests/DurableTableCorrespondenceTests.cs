// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using Xunit;

namespace Metalama.Framework.Analyzers.Tests;

/// <summary>
/// Asserts that the tables of <see cref="DurableContractAnalyzer"/> agree with
/// <c>UserCodeRetentionPolicy.IsPinning</c> in <c>Metalama.Framework.Engine</c>, which decides the same question at
/// run time for the <c>MetalamaDiagnoseMemoryLeaks</c> diagnostic.
/// </summary>
/// <remarks>
/// <para>
/// The two must be kept in correspondence. A user who sees a warning from one and nothing from the other on the same
/// object learns only that one of them is wrong. The analyzer project cannot reference the engine, so the lists
/// cannot be shared in code and are held together here instead.
/// </para>
/// <para>
/// <c>IsPinning</c> takes an instance and is <c>internal</c>, so it cannot be called from here, and most of the types
/// it names cannot be constructed in a test. The test therefore reads its <b>source</b>, which is embedded in this
/// assembly by the project file, and compares the set of types its patterns name against the set this test expects.
/// Embedding the file rather than copying it means that moving or renaming it breaks the build, which is itself the
/// signal that this test needs attention.
/// </para>
/// </remarks>
public sealed class DurableTableCorrespondenceTests : DurableAnalyzerTestBase
{
    /// <summary>
    /// The types named by the patterns of <c>IsPinning</c>, and the metadata name each one denotes.
    /// </summary>
    /// <remarks>
    /// When this test fails because a name is missing or unexpected, the fix is to change the tables in
    /// <c>WellKnownDurableTypes</c> and this map together, not to change this map alone.
    /// </remarks>
    /// <remarks>
    /// The expected fragment is the reason string carried by the table entry that should match. Asserting it, rather
    /// than only that a diagnostic appears, is what makes this test able to detect a wrong entry: a name whose
    /// namespace is misspelled still produces a diagnostic, because an unknown type is not durable either, but the
    /// reason is then "the type is not marked [Durable]" instead of the explanation the table was written to give.
    /// That is exactly the defect this test found when it was written, in the entry for <c>CompilationContext</c>.
    /// </remarks>
    private static readonly (string PatternName, string MetadataName, string ExpectedReason)[] _pinningTypes =
    [
        ("Compilation", "Microsoft.CodeAnalysis.Compilation", "pins every syntax tree of the project"),
        ("SyntaxTree", "Microsoft.CodeAnalysis.SyntaxTree", "pins the full text and the green node tree"),
        ("SemanticModel", "Microsoft.CodeAnalysis.SemanticModel", "a SemanticModel is bound to its compilation"),
        ("SyntaxNode", "Microsoft.CodeAnalysis.SyntaxNode", "reaches the tree that contains it"),
        ("ISymbol", "Microsoft.CodeAnalysis.ISymbol", "a symbol of the source of a compilation reaches that compilation"),
        ("CompilationModel", "Metalama.Framework.Engine.CodeModel.CompilationModel", "a CompilationModel holds its compilation"),
        ("PartialCompilation", "Metalama.Framework.Engine.CodeModel.PartialCompilation", "a PartialCompilation holds its compilation"),
        ("CompilationContext", "Metalama.Framework.Engine.Services.CompilationContext", "a CompilationContext holds its compilation"),
        ("IDeclaration", "Metalama.Framework.Code.IDeclaration", "a code model element reaches its CompilationModel"),
        ("IType", "Metalama.Framework.Code.IType", "a code model element reaches its CompilationModel"),
        ("IRef", "Metalama.Framework.Code.IRef", "holds the symbol and the reference factory"),

        // Named by the pattern only to be excluded from it: `IRef => obj is not IDurableRef`.
        ("IDurableRef", "Metalama.Framework.Code.IDurableRef", "")
    ];

    /// <summary>
    /// Reads the embedded source of <c>UserCodeRetentionPolicy</c> and returns the simple type names that the
    /// patterns of <c>IsPinning</c> mention.
    /// </summary>
    private static HashSet<string> GetTypeNamesNamedByIsPinning()
    {
        using var stream = typeof(DurableTableCorrespondenceTests).Assembly
            .GetManifestResourceStream( "UserCodeRetentionPolicy.cs" );

        Assert.NotNull( stream );

        using var reader = new StreamReader( stream! );
        var source = reader.ReadToEnd();

        var root = CSharpSyntaxTree.ParseText( source ).GetRoot();

        var method = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .SingleOrDefault( m => m.Identifier.Text == "IsPinning" );

        Assert.True(
            method != null,
            "UserCodeRetentionPolicy no longer declares a single method named IsPinning. The correspondence between "
            + "its list and the tables of the analyzer has to be re-established by hand." );

        var names = new HashSet<string>( StringComparer.Ordinal );

        // A bare type name in a switch arm, as in 'Compilation or SyntaxTree => true', is parsed as a *constant*
        // pattern rather than a type pattern, because the parser cannot tell a type from a constant. Only a pattern
        // that binds a name, as in 'ISymbol symbol', is a declaration pattern. Both shapes appear in this method, and
        // 'or' combinations are binary patterns whose children are reached by descending.
        foreach ( var node in method!.DescendantNodes() )
        {
            switch ( node )
            {
                case TypePatternSyntax typePattern:
                    names.Add( GetSimpleName( typePattern.Type.ToString() ) );

                    break;

                case DeclarationPatternSyntax declarationPattern:
                    names.Add( GetSimpleName( declarationPattern.Type.ToString() ) );

                    break;

                case ConstantPatternSyntax { Expression: IdentifierNameSyntax or QualifiedNameSyntax } constantPattern:
                    names.Add( GetSimpleName( constantPattern.Expression.ToString() ) );

                    break;
            }
        }

        return names;
    }

    private static string GetSimpleName( string name )
    {
        var lastDot = name.LastIndexOf( '.' );

        return lastDot < 0 ? name : name.Substring( lastDot + 1 );
    }

    /// <remarks>
    /// This is the assertion that catches drift. If a type is added to or removed from <c>IsPinning</c> and the
    /// analyzer tables are not changed with it, this fails and names the difference.
    /// </remarks>
    [Fact]
    public void TheSetOfTypesNamedByIsPinning_IsTheSetThisTestKnowsAbout()
    {
        var actual = GetTypeNamesNamedByIsPinning();
        var expected = _pinningTypes.Select( t => t.PatternName ).ToHashSet( StringComparer.Ordinal );

        var added = actual.Except( expected ).OrderBy( n => n, StringComparer.Ordinal ).ToList();
        var removed = expected.Except( actual ).OrderBy( n => n, StringComparer.Ordinal ).ToList();

        Assert.True(
            added.Count == 0 && removed.Count == 0,
            "UserCodeRetentionPolicy.IsPinning and the tables of the analyzer have drifted apart."
            + (added.Count > 0 ? " Named by IsPinning but unknown here: " + string.Join( ", ", added ) + "." : null)
            + (removed.Count > 0 ? " Known here but no longer named by IsPinning: " + string.Join( ", ", removed ) + "." : null)
            + " Update WellKnownDurableTypes and the map in this test together." );
    }

    /// <remarks>
    /// Every type the run-time walker treats as pinning must be one the analyzer refuses in a durable member, so that
    /// the two diagnostics never contradict each other on the same object. The expected reason is asserted as well,
    /// because a diagnostic alone does not prove that the table entry was the thing that produced it.
    /// </remarks>
    [Theory]
    [MemberData( nameof(PinningTypesExceptDurableRef) )]
    public async Task ATypeThatIsPinningAtRunTime_IsNotDurableForTheAnalyzer( string metadataName, string expectedReason )
    {
        var diagnostics = await GetDiagnosticsAsync(
            "using Metalama.Framework.Utilities; [Durable] class Probe { private global::" + metadataName + "? _field; }" );

        Assert.Single( diagnostics );
        Assert.Equal( "LAMA0870", diagnostics[0].Id );

        var message = diagnostics[0].GetMessage();

        Assert.True(
            message.Contains( expectedReason, StringComparison.Ordinal ),
            "'" + metadataName + "' did not produce the reason recorded in WellKnownDurableTypes. Expected to find \""
            + expectedReason + "\" but the diagnostic was: " + message
            + " A reason of \"the type is not marked [Durable]\" means the table entry does not match this type, "
            + "usually because its namespace changed." );
    }

    public static TheoryData<string, string> PinningTypesExceptDurableRef()
    {
        var data = new TheoryData<string, string>();

        foreach ( var (patternName, metadataName, expectedReason) in _pinningTypes )
        {
            if ( patternName != "IDurableRef" )
            {
                data.Add( metadataName, expectedReason );
            }
        }

        return data;
    }

    /// <remarks>
    /// The inverse of the rule above: <c>IsPinning</c> excludes a durable reference from the references it reports,
    /// and the analyzer must accept one in a durable member.
    /// </remarks>
    [Fact]
    public async Task ADurableReference_IsAcceptedByBoth()
        => await AssertNoDiagnosticAsync(
            "using Metalama.Framework.Utilities; using Metalama.Framework.Code; "
            + "[Durable] class Probe { private IDurableRef<IDeclaration>? _field; }" );

    /// <remarks>
    /// The first of the two deliberate divergences. The walker descends into a diagnostic and reports the syntax tree
    /// it actually finds, so it does not classify the container. The analyzer never sees an instance, so it is
    /// conservative. Asserting the divergence keeps it deliberate rather than accidental.
    /// </remarks>
    [Fact]
    public async Task DiagnosticAndLocation_AreNotDurableForTheAnalyzerAlthoughIsPinningDoesNotNameThem()
    {
        Assert.DoesNotContain( "Diagnostic", GetTypeNamesNamedByIsPinning() );
        Assert.DoesNotContain( "Location", GetTypeNamesNamedByIsPinning() );

        await AssertSingleDiagnosticAsync(
            "using Metalama.Framework.Utilities; [Durable] class Probe { private Microsoft.CodeAnalysis.Diagnostic? _field; }",
            "LAMA0870" );

        await AssertSingleDiagnosticAsync(
            "using Metalama.Framework.Utilities; [Durable] class Probe { private Microsoft.CodeAnalysis.Location? _field; }",
            "LAMA0870" );
    }

    /// <remarks>
    /// The second divergence. <c>IsPinning</c> reports a symbol only when it belongs to the source of a compilation,
    /// because the symbols of a referenced assembly are shared between compilations and keep nothing alive. A
    /// declared type carries no such information, so the analyzer refuses every symbol.
    /// </remarks>
    [Fact]
    public void IsPinning_DistinguishesSourceSymbols_WhereTheAnalyzerCannot()
    {
        using var stream = typeof(DurableTableCorrespondenceTests).Assembly
            .GetManifestResourceStream( "UserCodeRetentionPolicy.cs" );

        using var reader = new StreamReader( stream! );
        var source = reader.ReadToEnd();

        Assert.Contains( "IsFromSource", source, StringComparison.Ordinal );

        Assert.True(
            source.Contains( "ISymbol symbol => IsFromSource( symbol )", StringComparison.Ordinal ),
            "UserCodeRetentionPolicy no longer distinguishes a source symbol from a metadata one. The analyzer "
            + "refuses every symbol because a declared type cannot carry that distinction, and the divergence "
            + "documented in design-time-memory.md has to be revisited." );
    }
}
