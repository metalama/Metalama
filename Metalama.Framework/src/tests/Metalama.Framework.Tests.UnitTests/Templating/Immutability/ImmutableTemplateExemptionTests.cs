// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Templating.Immutability;

/// <summary>
/// The exemption that keeps the rules off the members an aspect injects into its target.
/// </summary>
/// <remarks>
/// An advice member is code, not state. It is routinely written as <c>{ get; set; }</c> or as a writeable field, the
/// aspect never reads it, and reporting it would be wrong in every case. Without this exemption the rules report
/// essentially every aspect that introduces a property.
/// </remarks>
public sealed class ImmutableTemplateExemptionTests : ImmutabilityTestBase
{
    private const string _aspectPrologue = _prologue + "using Metalama.Framework.Advising;\nusing Metalama.Framework.Aspects;\n";

    [Theory]
    [InlineData( "Template" )]
    [InlineData( "Introduce" )]
    [InlineData( "InterfaceMember" )]
    public async Task AdviceProperty_IsExempt( string attribute )
        => await AssertNoDiagnosticAsync(
            _aspectPrologue + $"[ImmutableType] class C {{ [{attribute}] public int Value {{ get; set; }} }}" );

    [Theory]
    [InlineData( "Template" )]
    [InlineData( "Introduce" )]
    public async Task AdviceField_IsExempt( string attribute )
        => await AssertNoDiagnosticAsync(
            _aspectPrologue + $"[ImmutableType] class C {{ [{attribute}] public int _value; }}" );

    /// <remarks>
    /// The exemption relies on the template information of the symbol classifier, which covers an advice attribute
    /// that a user derives from a framework one.
    /// </remarks>
    [Fact]
    public async Task UserDefinedAdviceAttribute_IsExempt()
        => await AssertNoDiagnosticAsync(
            _aspectPrologue
            + "class MyAdviceAttribute : DeclarativeAdviceAttribute { } "
            + "[ImmutableType] class C { [MyAdvice] public int Value { get; set; } }" );

    /// <remarks>
    /// The exemption must not leak from one member to the whole type.
    /// </remarks>
    [Fact]
    public async Task OrdinaryMemberBesideAnAdviceMember_IsStillReported()
        => await AssertSingleDiagnosticAsync(
            _aspectPrologue
            + "[ImmutableType] class C { [Template] public int Injected { get; set; } public int State { get; set; } }",
            "LAMA0881" );

    /// <remarks>
    /// The case that motivates the override walk: <c>OverrideFieldOrPropertyAspect.OverrideProperty</c> is declared
    /// <c>[Template] public abstract dynamic? { get; set; }</c>, and an override in user code does not repeat the
    /// attribute.
    /// </remarks>
    [Fact]
    public async Task UnattributedOverrideOfAnAdviceMember_IsExempt()
        => await AssertNoDiagnosticAsync(
            _aspectPrologue
            + "[ImmutableType] abstract class Base { [Template] public abstract int Value { get; set; } } "
            + "class Derived : Base { public override int Value { get; set; } }" );

    [Fact]
    public async Task ImplicitImplementationOfAnAdviceMember_IsExempt()
        => await AssertNoDiagnosticAsync(
            _aspectPrologue
            + "[ImmutableType] interface IHasTemplate { [Template] int Value { get; set; } } "
            + "class Impl : IHasTemplate { public int Value { get; set; } }" );

    [Fact]
    public async Task ExplicitImplementationOfAnAdviceMember_IsExempt()
        => await AssertNoDiagnosticAsync(
            _aspectPrologue
            + "[ImmutableType] interface IHasTemplate { [Template] int Value { get; set; } } "
            + "class Impl : IHasTemplate { int IHasTemplate.Value { get; set; } }" );

    /// <remarks>
    /// The property type is mutable, so the exemption is what keeps the member from being reported. An explicit
    /// implementation has a qualified name, so it cannot be matched to the interface member by name.
    /// </remarks>
    [Fact]
    public async Task ExplicitImplementationOfAnAdviceMemberOfMutableType_IsExempt()
        => await AssertNoDiagnosticAsync(
            _aspectPrologue
            + "[ImmutableType] interface IHasTemplate { [Template] List<int> Value { get; set; } } "
            + "class Impl : IHasTemplate { List<int> IHasTemplate.Value { get; set; } }" );

    /// <summary>
    /// Asserts that the framework's own template property, whose overrides in user code carry no attribute, is one
    /// the exemption recognises.
    /// </summary>
    [Fact]
    public void OverridePropertyOfOverrideFieldOrPropertyAspect_CarriesAnAdviceAttribute()
    {
        var property = typeof(OverrideFieldOrPropertyAspect).GetProperty( "OverrideProperty" );

        Assert.NotNull( property );

        Assert.Contains(
            property.GetCustomAttributes( inherit: false ),
            a => a is IAdviceAttribute );
    }
}
