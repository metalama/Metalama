// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Analyzers.Durability;
using Metalama.Framework.Analyzers.Immutability;
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

        // Named by the pattern only to be excluded from it, and only when it reaches no compilation:
        // `IRef => obj is not IDurableRefImpl { ReachesCompilation: false }`. Internal to the engine, so the probe
        // below cannot name it; the entry exists so that the set comparison is complete.
        ("IDurableRefImpl", "Metalama.Framework.Engine.CodeModel.References.IDurableRefImpl", "")
    ];

    /// <summary>
    /// The types named by <c>IsBoundary</c>, at which the run-time walk stops without reporting.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Five of them are also named by <c>IsPinning</c> and are reported before this method is consulted, so they are
    /// covered by <see cref="_pinningTypes"/> and appear here only so that the set comparison is complete. The rest
    /// are project-scoped or process-wide infrastructure through which a chain explains nothing, so the analyzer must
    /// treat them as durable.
    /// </para>
    /// <para>
    /// <c>Probe</c> is <c>false</c> for a type this assembly cannot name because it is internal to the engine. The
    /// table entry is still checked, by resolving its metadata name, which is the drift that matters: an entry whose
    /// namespace or arity is wrong simply never matches, and nothing else would notice.
    /// </para>
    /// </remarks>
    private static readonly (string PatternName, string MetadataName, bool AlsoPinning, bool Probe)[] _boundaryTypes =
    [
        ("ISymbol", "Microsoft.CodeAnalysis.ISymbol", true, false),
        ("Compilation", "Microsoft.CodeAnalysis.Compilation", true, false),
        ("SyntaxTree", "Microsoft.CodeAnalysis.SyntaxTree", true, false),
        ("SemanticModel", "Microsoft.CodeAnalysis.SemanticModel", true, false),
        ("SyntaxNode", "Microsoft.CodeAnalysis.SyntaxNode", true, false),
        ("ServiceProvider", "Metalama.Framework.Engine.Services.ServiceProvider", false, true),
        ("CompileTimeProject", "Metalama.Framework.Engine.CompileTime.CompileTimeProject", false, false),
        ("CompileTimeDomain", "Metalama.Framework.Engine.CompileTime.CompileTimeDomain", false, true),
        ("ITemplateReflectionContext", "Metalama.Framework.Engine.CompileTime.ITemplateReflectionContext", false, false),
        ("ILogger", "Metalama.Backstage.Diagnostics.ILogger", false, true),
        ("ILoggerFactory", "Metalama.Backstage.Diagnostics.ILoggerFactory", false, true),
        ("string", "System.String", false, true),
        ("Type", "System.Type", false, true),
        ("Assembly", "System.Reflection.Assembly", false, true),
        ("Module", "System.Reflection.Module", false, true),
        ("MemberInfo", "System.Reflection.MemberInfo", false, true),
        ("ParameterInfo", "System.Reflection.ParameterInfo", false, true),
        ("Thread", "System.Threading.Thread", false, true),
        ("AppDomain", "System.AppDomain", false, true)
    ];

    private static HashSet<string> GetTypeNamesNamedByIsPinning() => GetTypeNamesNamedBy( "IsPinning" );

    /// <summary>
    /// Reads the embedded source of <c>UserCodeRetentionPolicy</c> and returns the simple type names that the
    /// patterns of the named method mention.
    /// </summary>
    private static HashSet<string> GetTypeNamesNamedBy( string methodName )
    {
        using var stream = typeof(DurableTableCorrespondenceTests).Assembly
            .GetManifestResourceStream( "UserCodeRetentionPolicy.cs" );

        Assert.NotNull( stream );

        using var reader = new StreamReader( stream! );
        var source = reader.ReadToEnd();

        var root = CSharpSyntaxTree.ParseText( source ).GetRoot();

        var method = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .SingleOrDefault( m => m.Identifier.Text == methodName );

        Assert.True(
            method != null,
            "UserCodeRetentionPolicy no longer declares a single method named " + methodName + ". The correspondence "
            + "between its list and the tables of the analyzer has to be re-established by hand." );

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

                // 'IDurableRefImpl { ReachesCompilation: false }' names its type and then constrains a property, which
                // the parser reads as a recursive pattern rather than a type pattern.
                case RecursivePatternSyntax { Type: { } recursiveType }:
                    names.Add( GetSimpleName( recursiveType.ToString() ) );

                    break;

                case ConstantPatternSyntax { Expression: IdentifierNameSyntax or QualifiedNameSyntax } constantPattern:
                    names.Add( GetSimpleName( constantPattern.Expression.ToString() ) );

                    break;

                // 'case ISymbol:' in a switch *statement* is a case label whose value the parser reads as a constant,
                // for the same reason: a type and a constant are indistinguishable at that position.
                case CaseSwitchLabelSyntax { Value: IdentifierNameSyntax or QualifiedNameSyntax } caseLabel:
                    names.Add( GetSimpleName( caseLabel.Value.ToString() ) );

                    break;

                    // 'case string:' needs no case of its own: a keyword cannot be a constant, so the parser makes it
                    // a type pattern, which the first case above already handles. Matching PredefinedTypeSyntax
                    // directly would also collect the 'bool' of the return type and the 'object' of the parameter.
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
            if ( patternName != "IDurableRefImpl" )
            {
                data.Add( metadataName, expectedReason );
            }
        }

        return data;
    }

    /// <remarks>
    /// The same drift assertion for <c>IsBoundary</c>, which the analyzer's boundary entries were taken from.
    /// </remarks>
    [Fact]
    public void TheSetOfTypesNamedByIsBoundary_IsTheSetThisTestKnowsAbout()
    {
        var actual = GetTypeNamesNamedBy( "IsBoundary" );
        var expected = _boundaryTypes.Select( t => t.PatternName ).ToHashSet( StringComparer.Ordinal );

        var added = actual.Except( expected ).OrderBy( n => n, StringComparer.Ordinal ).ToList();
        var removed = expected.Except( actual ).OrderBy( n => n, StringComparer.Ordinal ).ToList();

        Assert.True(
            added.Count == 0 && removed.Count == 0,
            "UserCodeRetentionPolicy.IsBoundary and the tables of the analyzer have drifted apart."
            + (added.Count > 0 ? " Named by IsBoundary but unknown here: " + string.Join( ", ", added ) + "." : null)
            + (removed.Count > 0 ? " Known here but no longer named by IsBoundary: " + string.Join( ", ", removed ) + "." : null)
            + " Update WellKnownDurableTypes and the map in this test together." );
    }

    /// <remarks>
    /// <para>
    /// A boundary that is not also pinning is infrastructure through which a chain explains nothing, so a member of a
    /// durable type may hold one. Asserting that no diagnostic appears is a stronger check than it looks: a table
    /// entry whose name is wrong does not match, the type falls through to "not marked [Durable]", and a diagnostic
    /// appears. That is how the missing entry for the non-generic <c>ServiceProvider</c> was found.
    /// </para>
    /// </remarks>
    [Theory]
    [MemberData( nameof(ProbeableBoundaryTypes) )]
    public async Task ABoundaryThatIsNotPinning_IsDurableForTheAnalyzer( string metadataName )
        => await AssertNoDiagnosticAsync(
            "using Metalama.Framework.Utilities; [Durable] class Probe { private global::" + metadataName + "? _field; }" );

    public static TheoryData<string> ProbeableBoundaryTypes()
    {
        var data = new TheoryData<string>();

        foreach ( var (_, metadataName, alsoPinning, probe) in _boundaryTypes )
        {
            if ( !alsoPinning && probe )
            {
                data.Add( metadataName );
            }
        }

        return data;
    }

    /// <remarks>
    /// The entries the probe above cannot reach, because the type is internal to the engine. Resolving the metadata
    /// name is what catches the drift that matters for a table entry: a wrong namespace or a wrong arity means the
    /// entry never matches, and nothing else notices.
    /// </remarks>
    [Theory]
    [MemberData( nameof(AllCorrespondingTypeNames) )]
    public void EveryTypeNameInTheTables_ResolvesToAType( string metadataName )
    {
        var compilation = CreateCompilation( "class Dummy { }" );

        Assert.True(
            compilation.GetTypeByMetadataName( metadataName ) != null,
            "'" + metadataName + "' matches no type. The entry in WellKnownDurableTypes therefore never matches, so "
            + "the type falls through to the rule for an unmarked type and is reported for the wrong reason, or not "
            + "reported at all when the entry was meant to make it durable." );
    }

    public static TheoryData<string> AllCorrespondingTypeNames()
    {
        var data = new TheoryData<string>();
        var seen = new HashSet<string>( StringComparer.Ordinal );

        foreach ( var name in _pinningTypes.Select( t => t.MetadataName )
                     .Concat( _boundaryTypes.Select( t => t.MetadataName ) ) )
        {
            if ( seen.Add( name ) )
            {
                data.Add( name );
            }
        }

        return data;
    }

    /// <remarks>
    /// <para>
    /// The inverse of the rule above, and the third deliberate divergence. <c>IsPinning</c> excludes a durable
    /// reference only when it reaches no compilation, because since issue #1811 a durable reference of a *batch*
    /// compilation stores the reference it was created from and therefore holds that compilation, which is
    /// deliberate: the compilation lives until the build ends. The run-time walker must report it, because it runs
    /// during a batch compilation.
    /// </para>
    /// <para>
    /// The analyzer must not. It reasons about a declared type and about the design-time lifetime the contract is
    /// written for, where the serialized representation is selected and an <c>IDurableRef</c> reaches nothing.
    /// Typing a member <c>IDurableRef&lt;T&gt;</c> is exactly what this document asks for, so reporting it would
    /// contradict the remedy.
    /// </para>
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
