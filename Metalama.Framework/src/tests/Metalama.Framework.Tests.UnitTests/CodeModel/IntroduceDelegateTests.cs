// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Testing.UnitTesting;
using System;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// Tests of the builder of an introduced delegate. See issue #865 and the design document
/// <c>Metalama.Framework/docs/introducing-delegates.md</c>.
/// </summary>
public sealed class IntroduceDelegateTests : UnitTestClass
{
    /// <summary>
    /// Creates a delegate builder in a mutable clone of a compilation.
    /// </summary>
    private static DelegateBuilder CreateDelegateBuilder( CompilationModel compilation, string name = "IntroducedDelegate" )
        => new( null!, compilation.GlobalNamespace, name );

    /// <summary>
    /// Registers a delegate and its <c>Invoke</c> method in the compilation and returns the introduced type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>Invoke</c> method is registered as its own transformation and is emitted by nothing, which is section
    /// 4.2 of <c>Metalama.Framework/docs/introducing-types.md</c>. That registration is what makes the method
    /// visible through <see cref="INamedType.Methods"/>, which is where the facet resolves it.
    /// </para>
    /// </remarks>
    private static INamedType Introduce( CompilationModel compilation, DelegateBuilder builder )
    {
        builder.Freeze();
        compilation.AddTransformation( builder.CreateTransformation() );

        compilation.AddTransformation(
            new IntroduceSynthesizedDeclarationTransformation( null!, builder.InvokeMethodBuilder.BuilderData ) );

        return compilation.Types.OfName( builder.Name ).Single();
    }

    /// <summary>
    /// Verifies that the return parameter of a delegate refuses an input or an output reference kind.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The language lets a return parameter be returned by value, by reference or by read-only reference, and lets
    /// an argument alone be an input or an output one. The refusal is reported by the setter, because a value that
    /// the language does not accept would otherwise produce an error on generated code.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData( RefKind.In )]
    [InlineData( RefKind.Out )]
    public void ReturnParameterRefusesAnInputOrOutputReferenceKind( RefKind refKind )
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = CreateDelegateBuilder( compilation );
        builder.ReturnType = compilation.Factory.GetSpecialType( SpecialType.Int32 );

        Assert.Throws<InvalidOperationException>( () => builder.ReturnParameter.RefKind = refKind );
    }

    /// <summary>
    /// Verifies that the return parameter of a delegate accepts the three reference kinds that the language allows
    /// on a return.
    /// </summary>
    [Theory]
    [InlineData( RefKind.None )]
    [InlineData( RefKind.Ref )]
    [InlineData( RefKind.RefReadOnly )]
    public void ReturnParameterAcceptsTheReferenceKindsOfAReturn( RefKind refKind )
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = CreateDelegateBuilder( compilation );
        builder.ReturnType = compilation.Factory.GetSpecialType( SpecialType.Int32 );
        builder.ReturnParameter.RefKind = refKind;

        Assert.Equal( refKind, builder.ReturnParameter.RefKind );
    }

    /// <summary>
    /// Verifies that an introduced delegate reports the flags of a delegate and that it is a reference type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>IsReferenceType</c> decides what <c>ToNullable</c> produces, so a delegate reported as a value type would
    /// give <c>Nullable&lt;TDelegate&gt;</c>, which is not valid C#. This is the same class of defect as #1840.
    /// </para>
    /// </remarks>
    [Fact]
    public void IntroducedDelegateIsAReferenceType()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "delegate void SourceDelegate();" ).CreateMutableClone();

        var introducedType = Introduce( compilation, CreateDelegateBuilder( compilation ) );
        var sourceType = compilation.Types.OfName( "SourceDelegate" ).Single();

        Assert.Equal( TypeKind.Delegate, introducedType.TypeKind );
        Assert.True( introducedType.IsDelegate );
        Assert.False( introducedType.IsEnum );
        Assert.False( introducedType.IsRecord );

        Assert.Equal( sourceType.IsReferenceType, introducedType.IsReferenceType );
        Assert.Equal( sourceType.ToNullable().IsReferenceType, introducedType.ToNullable().IsReferenceType );
        Assert.Equal( sourceType.ToNullable().IsNullable, introducedType.ToNullable().IsNullable );
    }

    /// <summary>
    /// Verifies that the <c>Invoke</c> method of an introduced delegate is in the code model under that name, which
    /// is what <c>DelegateFacet</c> resolves.
    /// </summary>
    [Fact]
    public void InvokeMethodIsInTheCodeModelUnderItsOwnName()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = CreateDelegateBuilder( compilation, "MyHandler" );
        builder.ReturnType = compilation.Factory.GetSpecialType( SpecialType.Void );
        builder.AddParameter( "value", compilation.Factory.GetSpecialType( SpecialType.Int32 ) );

        var introducedType = Introduce( compilation, builder );

        // The type carries the name the aspect gave, and the method carries the name the language gives.
        Assert.Equal( "MyHandler", introducedType.Name );

        var invokeMethod = introducedType.Methods.OfName( "Invoke" ).Single();

        Assert.Equal( "Invoke", invokeMethod.Name );
        Assert.Equal( Accessibility.Public, invokeMethod.Accessibility );
        Assert.Equal( SpecialType.Void, invokeMethod.ReturnType.SpecialType );
        Assert.Single( invokeMethod.Parameters );
        Assert.Equal( "value", invokeMethod.Parameters[0].Name );
    }

    /// <summary>
    /// Verifies that the facet of an introduced delegate reports the same shape as the facet of the equivalent
    /// delegate read from source.
    /// </summary>
    [Fact]
    public void FacetOfIntroducedDelegateAgreesWithTheFacetOfASourceDelegate()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext
            .CreateCompilationModel( "delegate int SourceDelegate( string text, bool flag );" )
            .CreateMutableClone();

        var builder = CreateDelegateBuilder( compilation );
        builder.ReturnType = compilation.Factory.GetSpecialType( SpecialType.Int32 );
        builder.AddParameter( "text", compilation.Factory.GetSpecialType( SpecialType.String ) );
        builder.AddParameter( "flag", compilation.Factory.GetSpecialType( SpecialType.Boolean ) );

        var introducedType = Introduce( compilation, builder );

        var sourceFacet = compilation.Types.OfName( "SourceDelegate" ).Single().Facets.Delegate;
        var introducedFacet = introducedType.Facets.Delegate;

        Assert.NotNull( sourceFacet );
        Assert.NotNull( introducedFacet );

        Assert.Equal( sourceFacet.InvokeMethod.Name, introducedFacet.InvokeMethod.Name );
        Assert.Equal( sourceFacet.ReturnType.SpecialType, introducedFacet.ReturnType.SpecialType );

        Assert.Equal(
            sourceFacet.Parameters.SelectAsArray( p => p.Name ),
            introducedFacet.Parameters.SelectAsArray( p => p.Name ) );

        Assert.Equal(
            sourceFacet.Parameters.SelectAsArray( p => p.Type.SpecialType ),
            introducedFacet.Parameters.SelectAsArray( p => p.Type.SpecialType ) );
    }

    /// <summary>
    /// Verifies that an event whose type is an introduced delegate reports the <c>Invoke</c> method of that
    /// delegate as its signature, extending
    /// <see cref="TypeFacetTests.SignatureOfIntroducedEventIsTheInvokeMethodOfTheFacet"/> to an introduced type.
    /// </summary>
    [Fact]
    public void SignatureOfEventTypedByAnIntroducedDelegateIsItsInvokeMethod()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "class TargetType;" ).CreateMutableClone();

        var builder = CreateDelegateBuilder( compilation );
        builder.ReturnType = compilation.Factory.GetSpecialType( SpecialType.Void );
        builder.AddParameter( "value", compilation.Factory.GetSpecialType( SpecialType.Int32 ) );

        var introducedDelegate = Introduce( compilation, builder );

        var targetType = compilation.Types.OfName( "TargetType" ).Single();
        var eventBuilder = new EventBuilder( null!, targetType, "ValueChanged", false ) { Type = introducedDelegate };
        eventBuilder.Freeze();
        compilation.AddTransformation( eventBuilder.CreateTransformation() );

        var introducedEvent = targetType.Events.OfName( "ValueChanged" ).Single();

        Assert.Equal( "Invoke", introducedEvent.Signature.Name );
        Assert.Single( introducedEvent.Signature.Parameters );
        Assert.Equal( SpecialType.Int32, introducedEvent.Signature.Parameters[0].Type.SpecialType );
    }

    /// <summary>
    /// Verifies that the type parameters of a delegate belong to the type and not to its <c>Invoke</c> method,
    /// which is what the language declares, and that they may declare variance.
    /// </summary>
    [Fact]
    public void TypeParametersBelongToTheDelegateAndNotToItsInvokeMethod()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = CreateDelegateBuilder( compilation, "Transformer" );

        var input = builder.AddTypeParameter( "TInput" );
        input.Variance = VarianceKind.In;

        var output = builder.AddTypeParameter( "TOutput" );
        output.Variance = VarianceKind.Out;

        builder.ReturnType = output;
        builder.AddParameter( "value", input );

        var introducedType = Introduce( compilation, builder );

        Assert.Equal( 2, introducedType.TypeParameters.Count );
        Assert.Equal( VarianceKind.In, introducedType.TypeParameters[0].Variance );
        Assert.Equal( VarianceKind.Out, introducedType.TypeParameters[1].Variance );

        var invokeMethod = introducedType.Methods.OfName( "Invoke" ).Single();

        Assert.Empty( invokeMethod.TypeParameters );
    }

    /// <summary>
    /// Verifies that the four modifiers that a delegate cannot carry throw a <see cref="NotSupportedException"/>,
    /// which is the rule of section 5.1 of <c>Metalama.Framework/docs/introducing-types.md</c>.
    /// </summary>
    [Fact]
    public void ModifiersThatADelegateDoesNotHaveAreRefused()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = (IDelegateBuilder) CreateDelegateBuilder( compilation );

        Assert.Throws<NotSupportedException>( () => builder.IsStatic = true );
        Assert.Throws<NotSupportedException>( () => builder.IsAbstract = true );
        Assert.Throws<NotSupportedException>( () => builder.IsSealed = false );
        Assert.Throws<NotSupportedException>( () => builder.IsPartial = true );

        // A delegate is implicitly sealed, and the code model reports it the way Roslyn reports a delegate read
        // from source.
        Assert.True( builder.IsSealed );
        Assert.False( builder.IsStatic );
        Assert.False( builder.IsAbstract );
    }

    /// <summary>
    /// Verifies that the builder refuses every operation once it is frozen.
    /// </summary>
    [Fact]
    public void FrozenBuilderRefusesEveryChange()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = CreateDelegateBuilder( compilation );
        builder.Freeze();

        Assert.Throws<InvalidOperationException>( () => builder.ReturnType = compilation.Factory.GetSpecialType( SpecialType.Int32 ) );
        Assert.Throws<InvalidOperationException>( () => builder.AddParameter( "value", compilation.Factory.GetSpecialType( SpecialType.Int32 ) ) );
        Assert.Throws<InvalidOperationException>( () => builder.AddTypeParameter( "T" ) );
    }

    /// <summary>
    /// Verifies that <see cref="INamedType.Facets"/> of a delegate builder throws, while the flags that the
    /// collection dispatches on answer without throwing. See section 5.1 of
    /// <c>Metalama.Framework/docs/introducing-types.md</c>.
    /// </summary>
    [Fact]
    public void FacetsOfDelegateBuilderThrow()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = CreateDelegateBuilder( compilation );

        Assert.True( builder.IsDelegate );
        Assert.Throws<NotSupportedException>( () => builder.Facets );
    }
}
