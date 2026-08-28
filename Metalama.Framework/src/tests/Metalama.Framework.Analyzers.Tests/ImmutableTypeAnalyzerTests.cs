// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Xunit;

namespace Metalama.Framework.Analyzers.Tests;

/// <summary>
/// The rule table of <see cref="ImmutableContractAnalyzer"/>, one fact per row.
/// </summary>
public sealed class ImmutableTypeAnalyzerTests : ImmutableAnalyzerTestBase
{
    // ------------------------------------------------------------------------------------------------------------
    // LAMA0880 and LAMA0881: writeable members. One predicate produces both.
    // ------------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task WriteableField_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            _prologue + "[ImmutableObject(true)] class C { private int _count; }",
            "LAMA0880" );

        Assert.Contains( "must be written in an immutable style", message, StringComparison.Ordinal );
        Assert.Contains( "_count", message, StringComparison.Ordinal );
        Assert.Contains( "readonly", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task ReadOnlyField_IsNotReported()
        => await AssertNoDiagnosticAsync( _prologue + "[ImmutableObject(true)] class C { private readonly int _count; }" );

    [Fact]
    public async Task StaticField_IsNotReported()
        => await AssertNoDiagnosticAsync( _prologue + "[ImmutableObject(true)] class C { private static int _count; }" );

    [Fact]
    public async Task ConstField_IsNotReported()
        => await AssertNoDiagnosticAsync( _prologue + "[ImmutableObject(true)] class C { private const int Count = 1; }" );

    [Fact]
    public async Task AutomaticPropertyWithSetter_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            _prologue + "[ImmutableObject(true)] class C { public int Count { get; set; } }",
            "LAMA0881" );

        Assert.Contains( "must be written in an immutable style", message, StringComparison.Ordinal );
        Assert.Contains( "Count", message, StringComparison.Ordinal );
        Assert.Contains( "init", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task AutomaticPropertyWithInit_IsNotReported()
        => await AssertNoDiagnosticAsync( _prologue + "[ImmutableObject(true)] class C { public int Count { get; init; } }" );

    [Fact]
    public async Task GetOnlyProperty_IsNotReported()
        => await AssertNoDiagnosticAsync( _prologue + "[ImmutableObject(true)] class C { public int Count { get; } }" );

    /// <remarks>
    /// The diagnostic must land on the property that the author wrote, not on the compiler-generated backing field,
    /// whose name is unspeakable.
    /// </remarks>
    [Fact]
    public async Task PropertyDiagnostic_IsReportedOnTheProperty()
    {
        var code = _prologue + "[ImmutableObject(true)] class C { public int Count { get; set; } }";
        var diagnostics = await GetDiagnosticsAsync( code );

        var span = Assert.Single( diagnostics ).Location.SourceSpan;

        Assert.Equal( "Count", code.Substring( span.Start, span.Length ) );
    }

    // ------------------------------------------------------------------------------------------------------------
    // LAMA0882: member types that are mutable.
    // ------------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData( "List<int>" )]
    [InlineData( "Dictionary<string, int>" )]
    [InlineData( "HashSet<int>" )]
    [InlineData( "System.Text.StringBuilder" )]
    [InlineData( "int[]" )]
    [InlineData( "object" )]
    [InlineData( "WeakReference<string>" )]
    [InlineData( "System.Collections.Concurrent.ConcurrentDictionary<string, int>" )]
    public async Task MutableMemberType_IsReported( string type )
        => await AssertSingleDiagnosticAsync(
            _prologue + $"[ImmutableObject(true)] class C {{ private readonly {type} _value = null!; }}",
            "LAMA0882" );

    [Theory]
    [InlineData( "ImmutableArray<string>" )]
    [InlineData( "ImmutableList<int>" )]
    [InlineData( "ImmutableDictionary<string, int>" )]
    [InlineData( "ImmutableQueue<int>" )]
    [InlineData( "ImmutableSortedDictionary<string, int>" )]
    [InlineData( "Guid" )]
    [InlineData( "DateTime" )]
    [InlineData( "TimeSpan" )]
    [InlineData( "Type" )]
    [InlineData( "Uri" )]
    [InlineData( "Version" )]
    [InlineData( "System.Text.RegularExpressions.Regex" )]
    [InlineData( "(string, int)" )]
    [InlineData( "int?" )]
    [InlineData( "Func<int, int>" )]
    [InlineData( "Action" )]
    [InlineData( "string" )]
    [InlineData( "decimal" )]
    public async Task ImmutableMemberType_IsNotReported( string type )
        => await AssertNoDiagnosticAsync(
            _prologue + $"[ImmutableObject(true)] class C {{ private readonly {type} _value = default!; }}" );

    [Fact]
    public async Task EnumMemberType_IsNotReported()
        => await AssertNoDiagnosticAsync(
            _prologue + "enum E { A } [ImmutableObject(true)] class C { private readonly E _value; }" );

    /// <remarks>
    /// The point of requiring deep rather than shallow immutability: the collection itself cannot change, but every
    /// element of it can.
    /// </remarks>
    [Fact]
    public async Task ImmutableCollectionOfMutableElement_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            _prologue + "[ImmutableObject(true)] class C { private readonly ImmutableArray<System.Text.StringBuilder> _v; }",
            "LAMA0882" );

        Assert.Contains( "StringBuilder", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task UnmarkedClassMemberType_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            _prologue + "class Other { public int X; } [ImmutableObject(true)] class C { private readonly Other _v = null!; }",
            "LAMA0882" );

        Assert.Contains( "not marked [ImmutableObject(true)]", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task MarkedClassMemberType_IsNotReported()
        => await AssertNoDiagnosticAsync(
            _prologue
            + "[ImmutableObject(true)] class Other { public readonly int X; } "
            + "[ImmutableObject(true)] class C { private readonly Other _v = null!; }" );

    [Fact]
    public async Task TupleOfMutableElement_IsReported()
        => await AssertSingleDiagnosticAsync(
            _prologue + "[ImmutableObject(true)] class C { private readonly (string Name, List<int> Items) _v; }",
            "LAMA0882" );

    // ------------------------------------------------------------------------------------------------------------
    // LAMA0883 and LAMA0884.
    // ------------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task MutableBaseType_IsReported()
        => await AssertSingleDiagnosticAsync(
            _prologue + "class Base { public int X; } [ImmutableObject(true)] class C : Base { }",
            "LAMA0883" );

    [Fact]
    public async Task AttributeBaseType_IsNotReported()
        => await AssertNoDiagnosticAsync( _prologue + "[ImmutableObject(true)] class C : Attribute { }" );

    [Theory]
    [InlineData( "IEnumerable<int>" )]
    [InlineData( "IReadOnlyList<int>" )]
    public async Task UnannotatedInterfaceMemberType_IsReported( string type )
    {
        var message = await AssertSingleDiagnosticAsync(
            _prologue + $"[ImmutableObject(true)] class C {{ private readonly {type} _v = null!; }}",
            "LAMA0884" );

        Assert.Contains( "every implementation to be immutable", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task MarkedInterfaceMemberType_IsNotReported()
        => await AssertNoDiagnosticAsync(
            _prologue
            + "[ImmutableObject(true)] interface IThing { } "
            + "[ImmutableObject(true)] class C { private readonly IThing _v = null!; }" );

    /// <remarks>
    /// The obligation an interface exports is verified on the implementation, which is what makes marking an
    /// interface worth anything.
    /// </remarks>
    [Fact]
    public async Task ImplementationOfMarkedInterface_IsVerified()
        => await AssertSingleDiagnosticAsync(
            _prologue + "[ImmutableObject(true)] interface IThing { } class Impl : IThing { private int _x; }",
            "LAMA0880" );

    // ------------------------------------------------------------------------------------------------------------
    // Generics.
    // ------------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task GenericDefinition_IsCleanAtItsDeclaration()
        => await AssertNoDiagnosticAsync(
            _prologue + "[ImmutableObject(true)] class Box<T> { private readonly T _value = default!; }" );

    [Fact]
    public async Task GenericConstructedWithMutableArgument_IsReported()
        => await AssertSingleDiagnosticAsync(
            _prologue
            + "[ImmutableObject(true)] class Box<T> { private readonly T _value = default!; } "
            + "[ImmutableObject(true)] class C { private readonly Box<System.Text.StringBuilder> _v = null!; }",
            "LAMA0882" );

    /// <remarks>
    /// A type parameter that no field stores is a phantom, and requiring it to be immutable would reject
    /// <c>IDurableRef{T}</c>, which is the whole reason the stored-parameter computation exists.
    /// </remarks>
    [Fact]
    public async Task GenericWithPhantomArgument_IsNotReported()
        => await AssertNoDiagnosticAsync(
            _prologue
            + "[ImmutableObject(true)] class Tag<T> { private readonly string _id = \"\"; } "
            + "[ImmutableObject(true)] class C { private readonly Tag<System.Text.StringBuilder> _v = null!; }" );

    // ------------------------------------------------------------------------------------------------------------
    // The waiver, the gate, and broken code.
    // ------------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Waiver_SilencesTheRulesAndIsReportedAsInfo()
    {
        var diagnostics = await GetDiagnosticsAsync(
            _prologue
            + "[ImmutableObject(true)] interface IThing { } "
            + "[ImmutableObject(false)] class Impl : IThing { private int _x; private List<int> _y = null!; }" );

        var diagnostic = Assert.Single( diagnostics );

        Assert.Equal( "LAMA0886", diagnostic.Id );
    }

    /// <remarks>
    /// The attribute on a type that no contract reaches is not the author's business, so it is not reported.
    /// </remarks>
    [Fact]
    public async Task WaiverOnAnUnboundType_IsNotReported()
        => await AssertNoDiagnosticAsync( _prologue + "[ImmutableObject(false)] class C { private int _x; }" );

    [Fact]
    public async Task WaivingTypeUsedAsAMemberType_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            _prologue
            + "[ImmutableObject(false)] class Other { public int X; } "
            + "[ImmutableObject(true)] class C { private readonly Other _v = null!; }",
            "LAMA0882" );

        Assert.Contains( "waives the contract", message, StringComparison.Ordinal );
    }

    /// <remarks>
    /// The gate. A project that does not reference Metalama must pay one failed symbol lookup and produce nothing.
    /// </remarks>
    [Fact]
    public async Task WithoutMetalamaReference_NothingIsReported()
        => await AssertNoDiagnosticAsync(
            _prologue + "[ImmutableObject(true)] class C { private int _count; }",
            withMetalamaReference: false );

    [Fact]
    public async Task CodeThatDoesNotCompile_IsNotReported()
        => await AssertNoDiagnosticAsync(
            _prologue + "[ImmutableObject(true)] class C { private readonly NoSuchType _v; }" );

    [Fact]
    public async Task TypeWithoutTheAttribute_IsNotReported()
        => await AssertNoDiagnosticAsync( _prologue + "class C { private int _count; public List<int> Items { get; set; } = null!; }" );

    /// <remarks>
    /// A self-referencing type must terminate. The evaluation never descends into members, so it does, but the test
    /// records the guarantee.
    /// </remarks>
    [Fact]
    public async Task SelfReferencingType_Terminates()
        => await AssertNoDiagnosticAsync(
            _prologue + "[ImmutableObject(true)] class Node { private readonly Node? _next; }" );

    [Fact]
    public async Task MutuallyRecursiveTypes_Terminate()
        => await AssertNoDiagnosticAsync(
            _prologue
            + "[ImmutableObject(true)] class A { private readonly B? _b; } "
            + "[ImmutableObject(true)] class B { private readonly A? _a; }" );
}
