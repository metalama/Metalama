// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace Metalama.Framework.Analyzers.Tests;

/// <summary>
/// Asserts that <see cref="WellKnownImmutableTypes"/> and <see cref="ImmutabilityContext"/> agree with
/// <c>Metalama.Patterns.Immutability</c>, which decides the same question over the Metalama code model.
/// </summary>
/// <remarks>
/// <para>
/// The two must be kept in correspondence. The analyzer project cannot reference the patterns assembly, which needs
/// the engine's options system, so the lists cannot be shared in code and are held together here instead. The test
/// reads the <b>source</b> of the two patterns files, which the project file embeds in this assembly. Embedding them
/// rather than copying them means that moving or renaming either breaks the build, which is itself the signal that
/// this test needs attention.
/// </para>
/// <para>
/// When one of these fails, the fix is almost always to change both sides. A divergence that is deliberate belongs in
/// the numbered list in the header of <see cref="WellKnownImmutableTypes"/> and in a named test at the bottom of this
/// file, so that the next reader finds the reason rather than rediscovering the question.
/// </para>
/// </remarks>
public sealed class ImmutableTableCorrespondenceTests : ImmutableAnalyzerTestBase
{
    private static string ReadEmbeddedSource( string name )
    {
        using var stream = typeof(ImmutableTableCorrespondenceTests).Assembly.GetManifestResourceStream( name );

        Assert.NotNull( stream );

        using var reader = new StreamReader( stream );

        return reader.ReadToEnd();
    }

    /// <summary>
    /// The immutable collections that <c>Metalama.Patterns.Immutability.Fabric</c> registers, read from its source.
    /// </summary>
    private static IReadOnlyList<string> GetPatternsCollectionNames()
    {
        var source = ReadEmbeddedSource( "ImmutabilityFabric.cs" );

        var invocation = CSharpSyntaxTree.ParseText( source )
            .GetRoot()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Single( i => i.Expression.ToString().EndsWith( "SelectReflectionTypes", StringComparison.Ordinal ) );

        return invocation.ArgumentList.Arguments
            .Select( a => a.Expression )
            .OfType<TypeOfExpressionSyntax>()
            .Select( t => ToMetadataName( t.Type.ToString() ) )
            .OrderBy( n => n, StringComparer.Ordinal )
            .ToList();
    }

    /// <summary>
    /// Turns the source form of a generic type, <c>ImmutableDictionary&lt;,&gt;</c>, into its metadata name.
    /// </summary>
    private static string ToMetadataName( string typeOfArgument )
    {
        var angle = typeOfArgument.IndexOf( '<' );

        if ( angle < 0 )
        {
            return typeOfArgument;
        }

        var name = typeOfArgument.Substring( 0, angle );
        var arity = typeOfArgument.Count( c => c == ',' ) + 1;

        return name + "`" + arity;
    }

    /// <summary>
    /// The single most valuable assertion here: the two lists of immutable collections must be identical.
    /// </summary>
    [Fact]
    public void ImmutableCollectionLists_AreIdentical()
    {
        var fromPatterns = GetPatternsCollectionNames();

        var fromAnalyzer = WellKnownImmutableTypes.ImmutableCollectionNames
            .OrderBy( n => n, StringComparer.Ordinal )
            .ToList();

        Assert.Equal( fromPatterns, fromAnalyzer );
    }

    /// <summary>
    /// The intrinsics of rule 1 must be the ones the patterns implementation names.
    /// </summary>
    /// <remarks>
    /// <c>DateTime</c>, <c>IntPtr</c> and <c>UIntPtr</c> are deliberately absent from both: the blanket rule for value
    /// types of namespace <c>System</c> reaches them instead.
    /// </remarks>
    [Fact]
    public void SpecialTypeList_MatchesThePatternsImplementation()
    {
        var source = ReadEmbeddedSource( "ImmutabilityExtensions.cs" );

        var body = Between( source, "public static ImmutabilityKind GetImmutabilityKind", "if ( type is not INamedType" );

        var fromPatterns = Regex.Matches( body, @"SpecialType\.(\w+)" )
            .Select( m => m.Groups[1].Value )
            .OrderBy( n => n, StringComparer.Ordinal )
            .ToList();

        var expected = new[]
            {
                "Boolean", "Byte", "Char", "Decimal", "Double", "Int16", "Int32", "Int64",
                "SByte", "Single", "String", "UInt16", "UInt32", "UInt64"
            }
            .OrderBy( n => n, StringComparer.Ordinal )
            .ToList();

        Assert.Equal( expected, fromPatterns );
    }

    [Fact]
    public void DelegateEnumAndPointer_AreImmutableInThePatternsImplementation()
    {
        var source = ReadEmbeddedSource( "ImmutabilityExtensions.cs" );

        Assert.Contains(
            "TypeKind: TypeKind.Delegate or TypeKind.Enum or TypeKind.Pointer or TypeKind.FunctionPointer",
            source,
            StringComparison.Ordinal );
    }

    /// <summary>
    /// The exclusions of the blanket rule for value types of namespace <c>System</c>.
    /// </summary>
    [Fact]
    public void NonImmutableSystemValueTypeList_MatchesThePatternsImplementation()
    {
        var source = ReadEmbeddedSource( "ImmutabilityExtensions.cs" );

        var body = Between( source, "IsNonImmutableSystemValueType( INamedType", "}" );

        var fromPatterns = Regex.Matches( body, "\"(\\w+)\"" )
            .Select( m => m.Groups[1].Value )
            .OrderBy( n => n, StringComparer.Ordinal )
            .ToList();

        var fromAnalyzer = WellKnownImmutableTypes.NonImmutableSystemValueTypeNames
            .OrderBy( n => n, StringComparer.Ordinal )
            .ToList();

        Assert.Equal( fromPatterns, fromAnalyzer );
    }

    /// <summary>
    /// The shape of the blanket rule itself, so that a change to it fails here.
    /// </summary>
    [Fact]
    public void TheSystemNamespaceRule_IsStillWrittenTheSameWay()
    {
        var source = ReadEmbeddedSource( "ImmutabilityExtensions.cs" );

        Assert.Contains( "IsReferenceType: false", source, StringComparison.Ordinal );
        Assert.Contains( "ContainingNamespace.FullName: \"System\"", source, StringComparison.Ordinal );
        Assert.Contains( "namedType.IsReadOnly", source, StringComparison.Ordinal );
    }

    /// <summary>
    /// Every name in the tables must resolve to a real type.
    /// </summary>
    /// <remarks>
    /// This is the assertion that catches a misspelled namespace or a missing arity, which are otherwise completely
    /// silent: an entry that matches nothing is a rule that never fires, and the type then falls through to the
    /// default verdict, which for a mutable entry is the same answer for the wrong reason.
    /// </remarks>
    [Theory]
    [MemberData( nameof(TableNames) )]
    public void EveryNameInTheTable_ResolvesToAType( string metadataName )
    {
        var compilation = CreateCompilation( "class X;" );

        Assert.NotNull( compilation.GetTypeByMetadataName( metadataName ) );
    }

    public static TheoryData<string> TableNames
    {
        get
        {
            var data = new TheoryData<string>();

            foreach ( var name in WellKnownImmutableTypes.AllNames )
            {
                data.Add( name );
            }

            return data;
        }
    }

    // ------------------------------------------------------------------------------------------------------------
    // The deliberate divergences, one named test each, so that a reader who finds one of them in the code finds the
    // reason here rather than assuming a defect.
    // ------------------------------------------------------------------------------------------------------------

    /// <remarks>
    /// The patterns implementation reaches <c>Nullable{T}</c> through the blanket rule for value types of namespace
    /// <c>System</c> and therefore trusts it whatever <c>T</c> is. Inspecting <c>T</c> costs one line.
    /// </remarks>
    [Fact]
    public async Task NullableIsInspected_AlthoughPatternsTrustsIt()
    {
        await AssertNoDiagnosticAsync( _prologue + "[ImmutableObject(true)] class C { private readonly int? _v; }" );

        await AssertSingleDiagnosticAsync(
            _prologue
            + "struct S { public List<int> Items; } "
            + "[ImmutableObject(true)] class C { private readonly S? _v; }",
            "LAMA0882" );
    }

    /// <remarks>
    /// A faithful port classifies every tuple as mutable, because <c>ValueTuple</c> is excluded from the blanket rule
    /// and is not a <c>readonly struct</c>. As the type of a read-only field a tuple cannot be reassigned, so that
    /// would be a false positive on almost every use.
    /// </remarks>
    [Fact]
    public async Task TuplesAreTransparent_AlthoughPatternsClassifiesThemMutable()
    {
        Assert.Contains( "ValueTuple", WellKnownImmutableTypes.NonImmutableSystemValueTypeNames );

        await AssertNoDiagnosticAsync( _prologue + "[ImmutableObject(true)] class C { private readonly (string, int) _v; }" );
    }

    /// <remarks>
    /// <c>ArraySegment{T}</c> is a <c>readonly struct</c> of namespace <c>System</c>, so the blanket rule would call
    /// it deeply immutable. It wraps an array whose elements can be replaced.
    /// </remarks>
    [Fact]
    public async Task ArraySegmentIsMutable_AlthoughTheBlanketRuleWouldTrustIt()
    {
        var message = await AssertSingleDiagnosticAsync(
            _prologue + "[ImmutableObject(true)] class C { private readonly ArraySegment<int> _v; }",
            "LAMA0882" );

        Assert.Contains( "wraps a mutable array", message, StringComparison.Ordinal );
    }

    /// <remarks>
    /// Not a divergence from the patterns implementation but from the sibling durability rules, which reject a
    /// delegate. Both are right for their own question, and this is asserted again from the other side in
    /// <c>DurabilityImmutabilityDivergenceTests</c>.
    /// </remarks>
    [Fact]
    public async Task DelegatesAreImmutable_AlthoughTheyAreNotDurable()
        => await AssertNoDiagnosticAsync(
            _prologue + "[ImmutableObject(true)] class C { private readonly Func<int, int> _f = x => x; }" );

    private static string Between( string source, string start, string end )
    {
        var startIndex = source.IndexOf( start, StringComparison.Ordinal );

        Assert.True( startIndex >= 0, $"'{start}' was not found in the embedded source." );

        var endIndex = source.IndexOf( end, startIndex, StringComparison.Ordinal );

        Assert.True( endIndex >= 0, $"'{end}' was not found after '{start}' in the embedded source." );

        return source.Substring( startIndex, endIndex - startIndex );
    }
}
