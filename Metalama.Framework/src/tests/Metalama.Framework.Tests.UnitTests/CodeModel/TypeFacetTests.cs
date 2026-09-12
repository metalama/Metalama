// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Testing.UnitTesting;
using System;
using System.Linq;
using Xunit;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// Tests of <see cref="INamedType.Facets"/>, <see cref="INamedType.IsDelegate"/> and <see cref="IDelegateFacet"/>,
/// the facet mechanism and its first facet. See issue #1995 and the design document
/// <c>Metalama.Framework/docs/future/type-facets.md</c>.
/// </summary>
public sealed class TypeFacetTests : UnitTestClass
{
    private const string _code = """
                                 using System;

                                 delegate void VoidHandler( object sender, EventArgs args );

                                 delegate int Transformer<T>( T input, out bool succeeded );

                                 class OrdinaryClass;
                                 struct Struct;
                                 interface IInterface;
                                 enum Enum { Value }

                                 class Holder
                                 {
                                     public Transformer<string> ConstructedDelegate = null!;
                                     public Delegate NotADelegateType = null!;
                                     public MulticastDelegate NotAMulticastDelegateType = null!;
                                 }
                                 """;

    /// <summary>
    /// Verifies that a type that has no facet reports a count of zero, that the typed property of the collection is
    /// <c>null</c>, and that <see cref="INamedType.IsDelegate"/> is <c>false</c> for it.
    /// </summary>
    [Fact]
    public void TypeThatHasNoFacetHasAnEmptyCollection()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( _code );

        foreach ( var typeName in new[] { "OrdinaryClass", "Struct", "IInterface", "Holder" } )
        {
            var type = compilation.Types.OfName( typeName ).Single();

            Assert.False( type.IsDelegate, $"{typeName} should not be a delegate." );
            Assert.Null( type.Facets.Delegate );
            Assert.Empty( type.Facets );

            // The count is read into a local because the invariant is that it agrees with the typed properties, which
            // is a different statement from the emptiness of the enumeration asserted above.
            var facetCount = type.Facets.Count;

            Assert.Equal( 0, facetCount );
        }
    }

    /// <summary>
    /// Verifies implementation guideline 4 of the design document: a type that has no facet returns a shared empty
    /// collection, so that generic code that reads <see cref="INamedType.Facets"/> on every type of a compilation
    /// allocates nothing.
    /// </summary>
    [Fact]
    public void EmptyFacetCollectionIsShared()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( _code );

        var first = compilation.Types.OfName( "OrdinaryClass" ).Single().Facets;
        var second = compilation.Types.OfName( "Struct" ).Single().Facets;

        Assert.Same( first, second );
    }

    /// <summary>
    /// Verifies that a delegate has the delegate facet, that the facet names the <c>Invoke</c> method, and that the
    /// collection contains exactly that facet.
    /// </summary>
    [Fact]
    public void DelegateHasTheDelegateFacet()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( _code );

        var handler = compilation.Types.OfName( "VoidHandler" ).Single();

        Assert.True( handler.IsDelegate );

        var facet = handler.Facets.Delegate;

        Assert.NotNull( facet );
        Assert.Equal( TypeFacetKind.Delegate, facet.FacetKind );
        Assert.Same( handler, facet.Type );

        Assert.Equal( "Invoke", facet.InvokeMethod.Name );
        Assert.Equal( SpecialType.Void, facet.ReturnType.SpecialType );
        Assert.Equal( new[] { "sender", "args" }, facet.Parameters.SelectAsArray( p => p.Name ) );

        // See the comment of TypeThatHasNoFacetHasAnEmptyCollection on why the count is read into a local.
        var facetCount = handler.Facets.Count;

        Assert.Equal( 1, facetCount );
        Assert.Same( facet, Assert.Single( handler.Facets ) );
    }

    /// <summary>
    /// Verifies that the facet reports the signature of the <c>Invoke</c> method rather than a signature recomputed
    /// beside it: the return type and the parameters of the facet are those of the method it names.
    /// </summary>
    [Fact]
    public void FacetSignatureAgreesWithTheInvokeMethod()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( _code );

        var facet = compilation.Types.OfName( "VoidHandler" ).Single().Facets.Delegate;

        Assert.NotNull( facet );
        Assert.Same( facet.InvokeMethod.ReturnType, facet.ReturnType );
        Assert.Same( facet.InvokeMethod.Parameters, facet.Parameters );
    }

    /// <summary>
    /// Verifies the acceptance criterion that the facet of a constructed generic delegate reports the substituted
    /// signature, and not the signature of the generic definition.
    /// </summary>
    [Fact]
    public void FacetOfConstructedGenericDelegateReportsTheSubstitutedSignature()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( _code );

        var constructedDelegate = (INamedType) compilation.Types.OfName( "Holder" )
            .Single()
            .Fields.OfName( "ConstructedDelegate" )
            .Single()
            .Type;

        Assert.True( constructedDelegate.IsDelegate );

        var facet = constructedDelegate.Facets.Delegate;

        Assert.NotNull( facet );
        Assert.Equal( SpecialType.Int32, facet.ReturnType.SpecialType );
        Assert.Equal( SpecialType.String, facet.Parameters[0].Type.SpecialType );
        Assert.Equal( RefKind.Out, facet.Parameters[1].RefKind );

        // The generic definition still reports the unsubstituted signature.
        var definition = constructedDelegate.Definition;

        Assert.Equal( definition.TypeParameters[0], definition.Facets.Delegate!.Parameters[0].Type );
    }

    /// <summary>
    /// Verifies that <see cref="System.Delegate"/> and <see cref="System.MulticastDelegate"/> have no delegate
    /// facet. They are classes rather than delegate types, and neither declares an <c>Invoke</c> method, so they are
    /// the shape on which the converted lookups of the <c>Invoke</c> identifier failed.
    /// </summary>
    [Fact]
    public void DelegateBaseClassesHaveNoFacet()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( _code );

        var holder = compilation.Types.OfName( "Holder" ).Single();

        foreach ( var fieldName in new[] { "NotADelegateType", "NotAMulticastDelegateType" } )
        {
            var type = (INamedType) holder.Fields.OfName( fieldName ).Single().Type;

            Assert.False( type.IsDelegate, $"{type.Name} should not be a delegate." );
            Assert.Null( type.Facets.Delegate );
            Assert.Empty( type.Facets );
        }
    }

    /// <summary>
    /// Verifies section 5.1 of <c>Metalama.Framework/docs/introducing-types.md</c>, which supersedes
    /// implementation guideline 5 of <c>type-facets.md</c>: a builder throws rather than reporting an empty
    /// collection, because it describes a type whose members are not resolvable, so an empty structure would be a
    /// false answer rather than an incomplete one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The flags are asserted beside the exception because they are what keeps the reversal safe. They answer on a
    /// builder without allocating, so a caller that asks what kind a type is keeps working and only a caller that
    /// asks for the structure meets the exception. The two eligibility rules of
    /// <see cref="AdviceKind.OverrideEventInvoke"/> rely on exactly that, and
    /// <see cref="EventOfMalformedDelegateTypeIsNotEligibleForOverrideEventInvoke"/> pins the behaviour they give.
    /// </para>
    /// </remarks>
    [Fact]
    public void FacetsOfBuilderThrow()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = new NamedTypeBuilder( null!, compilation.GlobalNamespace, "IntroducedType", TypeKind.Class );

        Assert.Throws<NotSupportedException>( () => builder.Facets );

        Assert.False( builder.IsDelegate );
        Assert.False( builder.IsEnum );
        Assert.False( builder.IsRecord );
        Assert.False( builder.IsUnion );

        builder.Freeze();
        compilation.AddTransformation( builder.CreateTransformation() );

        // The introduced type reports the facet of its kind, which is section 5.2 of the same document. A class has
        // no facet, so the collection is empty rather than absent.
        var introducedType = compilation.Types.OfName( "IntroducedType" ).Single();

        Assert.Empty( introducedType.Facets );
        Assert.False( introducedType.IsDelegate );
        Assert.False( introducedType.IsEnum );
    }

    /// <summary>
    /// Verifies that <see cref="IEvent.Signature"/> is the <c>Invoke</c> method that the facet names. The property
    /// had four duplicate implementations, which the facet replaces.
    /// </summary>
    [Fact]
    public void EventSignatureIsTheInvokeMethodOfTheFacet()
    {
        const string code = """
                            using System;

                            class C
                            {
                                public event EventHandler FieldLike;

                                public event Action<int> Explicit { add {} remove {} }
                            }
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code, ignoreErrors: true );

        var type = compilation.Types.OfName( "C" ).Single();

        foreach ( var eventName in new[] { "FieldLike", "Explicit" } )
        {
            var declaredEvent = type.Events.OfName( eventName ).Single();
            var facet = declaredEvent.Type.Facets.Delegate;

            Assert.NotNull( facet );
            Assert.Equal( facet.InvokeMethod, declaredEvent.Signature );
        }
    }

    /// <summary>
    /// Verifies that <see cref="IEvent.Signature"/> is the <c>Invoke</c> method that the facet names for an event
    /// that an aspect introduces, both while the builder describes it and after the transformation is applied. Those
    /// are two of the four duplicate implementations that the facet replaces.
    /// </summary>
    [Fact]
    public void SignatureOfIntroducedEventIsTheInvokeMethodOfTheFacet()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "class C;" ).CreateMutableClone();

        var type = compilation.Types.OfName( "C" ).Single();

        // The default type of an event that a builder describes is System.EventHandler.
        var builder = new EventBuilder( null!, type, "IntroducedEvent", isEventField: true );
        builder.Freeze();
        compilation.AddTransformation( builder.CreateTransformation() );

        Assert.Equal( SpecialType.Void, builder.Signature.ReturnType.SpecialType );
        Assert.Equal( builder.Type.Facets.Delegate!.InvokeMethod, builder.Signature );

        var introducedEvent = type.Events.OfName( "IntroducedEvent" ).Single();

        Assert.Equal( introducedEvent.Type.Facets.Delegate!.InvokeMethod, introducedEvent.Signature );
    }

    /// <summary>
    /// Verifies the change of shipped behaviour that this issue carries: the eligibility rule of
    /// <see cref="AdviceKind.OverrideEventInvoke"/>, which inspects the signature of the event, returns
    /// <see cref="EligibleScenarios.None"/> when the type of the event is not a well-formed delegate, where it threw
    /// <see cref="System.InvalidOperationException"/> before the facet existed.
    /// </summary>
    /// <remarks>
    /// The type of the event below is <see cref="System.Delegate"/>, which binds, so the event reaches the code
    /// model, and which declares no <c>Invoke</c> method, so the lookup that the rule performed found no element.
    /// The C# compiler reports CS0066 for that declaration, which is why the compilation ignores errors, but the
    /// same shape reaches the code model without any error from a delegate read from malformed metadata.
    /// </remarks>
    [Fact]
    public void EventOfMalformedDelegateTypeIsNotEligibleForOverrideEventInvoke()
    {
        const string code = """
                            using System;

                            class C
                            {
                                public event Delegate Malformed { add {} remove {} }
                            }
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code, ignoreErrors: true );

        var malformedEvent = compilation.Types.OfName( "C" ).Single().Events.OfName( "Malformed" ).Single();

        Assert.Null( malformedEvent.Type.Facets.Delegate );

        var rule = EligibilityRuleFactory.GetAdviceEligibilityRule( AdviceKind.OverrideEventInvoke );

        Assert.Equal( EligibleScenarios.None, rule.GetEligibility( malformedEvent ) );
    }

    /// <summary>
    /// Verifies that the rule of <see cref="EventOfMalformedDelegateTypeIsNotEligibleForOverrideEventInvoke"/> still
    /// accepts an event whose type is a well-formed delegate, so that the change of behaviour is confined to the
    /// malformed type.
    /// </summary>
    [Fact]
    public void EventOfWellFormedDelegateTypeIsEligibleForOverrideEventInvoke()
    {
        const string code = """
                            using System;

                            class C
                            {
                                public event EventHandler WellFormed { add {} remove {} }
                            }
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );

        var wellFormedEvent = compilation.Types.OfName( "C" ).Single().Events.OfName( "WellFormed" ).Single();

        var rule = EligibilityRuleFactory.GetAdviceEligibilityRule( AdviceKind.OverrideEventInvoke );

        Assert.NotEqual( EligibleScenarios.None, rule.GetEligibility( wellFormedEvent ) );
    }
}
