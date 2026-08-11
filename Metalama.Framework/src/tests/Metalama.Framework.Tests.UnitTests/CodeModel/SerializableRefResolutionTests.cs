// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Testing.UnitTesting;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// The tests that create a reference from an identifier and resolve it, instead of converting a declaration to a
/// durable reference.
/// </summary>
/// <remarks>
/// These tests do not use <see cref="IDurableRefFactory"/>, so the representation selected for the project has no
/// effect on them, and they run once. <see cref="RefTests"/> contains the tests that use that service, and runs them
/// once per representation.
/// </remarks>
public sealed class SerializableRefResolutionTests : UnitTestClass
{
    [Fact]
    public void CompilationRef()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "/* nothing */" );

        var compilationRef = compilation.ToRef();
        var resolved = compilationRef.GetTarget( compilation );

        Assert.Same( compilation, resolved );
    }

    [Fact]
    public void CompilationSymbolId()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "/* nothing */" );
        var symbolId = SymbolId.Create( compilation.Symbol );
        var resolvedSymbol = symbolId.Resolve( compilation.RoslynCompilation ).AssertNotNull();
        var resolvedDeclaration = compilation.Factory.GetCompilationElement( resolvedSymbol );

        Assert.Same( compilation, resolvedDeclaration );
    }

    [Fact]
    public void ReferencedAssemblySymbol()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "/* nothing */" );

        var assemblyRefSymbol = compilation.Factory.GetTypeByReflectionType( typeof(string) ).GetSymbol();
        var assemblyRefRef = SymbolId.Create( assemblyRefSymbol );
        _ = assemblyRefRef.Resolve( compilation.RoslynCompilation );
    }

    /// <summary>
    /// Verifies that a reference created from an identifier is identifier-based in every execution scenario.
    /// </summary>
    /// <remarks>
    /// These methods are the entry points used by the deserializer. Their argument is an identifier and not a
    /// declaration, so there is no reference to store. A reference is also read in a compilation other than the one
    /// that wrote it, so storing a reference would be incorrect.
    /// </remarks>
    [Fact]
    public void RefsBuiltFromAnIdentifierNeverReachACompilation()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( RefTestFixtures.GenericTypesCode );

        var type = RefTestFixtures.GetTestType( compilation, "Plain" );

        var fromDeclarationId = (IDurableRefImpl) DurableRefFactory.FromDeclarationId<INamedType>( type.GetSerializableId() );
        var fromTypeId = (IDurableRefImpl) DurableRefFactory.FromTypeId<INamedType>( type.GetSerializableTypeId() );

        Assert.False( fromDeclarationId.ReachesCompilation );
        Assert.False( fromTypeId.ReachesCompilation );
    }

    /// <summary>
    /// A durable reference whose id resolves to nothing in the current compilation, which happens when the referenced
    /// project changed since its manifest was written, must not throw (issue #1748).
    /// </summary>
    [Fact]
    public void GetPrimarySyntaxTreeOfUnresolvableDurableRef()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "/* nothing */" );

        var durableRef = new DeclarationIdRef<INamedType>( new SerializableDeclarationId( "T:ThereIsNoSuchType" ) );

        Assert.Null( durableRef.GetPrimarySyntaxTree( compilation.CompilationContext ) );
    }

    /// <summary>
    /// The code that <see cref="OldFormatIdentifiersStillResolve"/> resolves its hardcoded identifiers against.
    /// </summary>
    private const string _backwardCompatibilityCode = """
                                                      namespace Ns
                                                      {
                                                          public class C<T>
                                                          {
                                                              public int Field;

                                                              public int M( string p ) => 0;
                                                          }

                                                          public class Plain { }
                                                      }
                                                      """;

    /// <summary>
    /// Verifies that the identifiers written by an earlier version still resolve.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A durable reference to a type is now written as a <see cref="SerializableTypeId"/>, so the identifier of a named
    /// type changed from the documentation form <c>T:Ns.Plain</c> to the type form <c>Y:global::Ns.Plain!</c>. These
    /// identifiers are written into the transitive manifest, which one version of Metalama writes and another reads,
    /// so the old form has to keep resolving. The literals below are hardcoded on purpose: computing them from the
    /// current code would test nothing, because it would produce the new form.
    /// </para>
    /// <para>
    /// The identifiers of declarations that are not types are unchanged, and are included so that a future change to
    /// the format is measured against all of them rather than against types alone. See issue #1797.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData( "T:Ns.Plain", "Plain" )]
    [InlineData( "T:Ns.C`1", "C<T>" )]
    [InlineData( "M:Ns.C`1.M(System.String)", "M" )]
    [InlineData( "F:Ns.C`1.Field", "Field" )]
    [InlineData( "M:Ns.C`1.M(System.String);Parameter;0", "p" )]
    [InlineData( "T:Ns.C`1;TypeParameter;0", "T" )]
    public void OldFormatIdentifiersStillResolve( string id, string expectedName )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _backwardCompatibilityCode );

        var resolved = new SerializableDeclarationId( id ).ResolveToDeclaration( compilation );

        Assert.NotNull( resolved );
        Assert.Equal( expectedName, resolved is INamedType namedType ? namedType.ToDisplayString() : ((INamedDeclaration) resolved!).Name );
    }

    /// <summary>
    /// Verifies that a durable reference built from an identifier of the old form resolves, which is the route the
    /// deserializer takes when it reads a manifest written by an earlier version.
    /// </summary>
    [Fact]
    public void DurableRefFromAnOldFormatTypeIdentifierResolves()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _backwardCompatibilityCode );

        var durableRef = DurableRefFactory.FromDeclarationId<INamedType>( new SerializableDeclarationId( "T:Ns.Plain" ) );

        Assert.Equal( "Plain", durableRef.GetTarget( compilation ).ToDisplayString() );
    }

    /// <summary>
    /// Verifies that the durable form used by <c>Query.CreateBaseTypeResolver</c> round-trips every shape of type that
    /// <c>SelectTypesDerivedFrom( INamedType )</c> accepts.
    /// </summary>
    /// <remarks>
    /// That method converts the type it is given to a durable reference so that the query, which may outlive the
    /// compilation by an entire editing session, does not pin it (issue #1799). The type comes from user code, so the
    /// conversion has to survive every shape the signature accepts, not only the plain named type that the first
    /// version of the change was written against.
    /// </remarks>
    [Theory]
    [InlineData( "Plain" )]
    [InlineData( "Generic" )]
    [InlineData( "Nested" )]
    [InlineData( "Constructed" )]
    [InlineData( "External" )]
    public void DurableTypeIdRefResolvesToAnEquivalentType( string kind )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( RefTestFixtures.GenericTypesCode );

        var type = RefTestFixtures.GetTestType( compilation, kind );

        var resolved = DurableRefFactory.FromTypeId<INamedType>( type.GetSerializableTypeId() ).GetTarget( compilation );

        Assert.Equal( type.ToDisplayString(), resolved.ToDisplayString() );
    }

    /// <summary>
    /// Verifies that a durable reference to a type that an aspect introduced into the global namespace resolves back
    /// to that type.
    /// </summary>
    /// <remarks>
    /// The resolution of an identifier starts in the namespace tree merged over the compilation and its references,
    /// whereas an aspect introduces a type into the tree of <see cref="IAssembly.GlobalNamespace"/>. The global
    /// namespace has a distinct declaration in each tree, so the introduced type is added to the collections of both.
    /// See issue #1825.
    /// </remarks>
    [Fact]
    public void DurableTypeIdRefToTypeIntroducedIntoGlobalNamespaceResolves()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "class Outer;" ).CreateMutableClone();

        var introducedType = RefTestFixtures.IntroduceType( compilation, compilation.GlobalNamespace, "Introduced" );

        var durableRef = DurableRefFactory.FromTypeId<INamedType>( introducedType.GetSerializableTypeId() );

        Assert.Same( introducedType, durableRef.GetTarget( compilation ) );
    }

    /// <summary>
    /// Verifies that a durable reference to a type that an aspect introduced into a namespace that the aspect also
    /// introduced resolves back to that type.
    /// </summary>
    /// <remarks>
    /// This is the shape of the metrics sample, whose aspect introduces its type into a namespace of its own. The
    /// introduced namespace is added to the global namespace of both trees, and the introduced type is added to the
    /// single collection of that namespace, which has one declaration. See issue #1825.
    /// </remarks>
    [Fact]
    public void DurableTypeIdRefToTypeIntroducedIntoIntroducedNamespaceResolves()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "class Outer;" ).CreateMutableClone();

        var introducedNamespace = RefTestFixtures.IntroduceNamespace( compilation, compilation.GlobalNamespace, "Introduced" );
        var introducedType = RefTestFixtures.IntroduceType( compilation, introducedNamespace, "Companion" );

        var durableRef = DurableRefFactory.FromTypeId<INamedType>( introducedType.GetSerializableTypeId() );

        Assert.Same( introducedType, durableRef.GetTarget( compilation ) );
    }

    /// <summary>
    /// Verifies that a durable reference to a type that an aspect introduced into a namespace which a referenced
    /// assembly declares as well resolves back to that type.
    /// </summary>
    /// <remarks>
    /// The namespace has two constituents, so Roslyn creates a merged namespace, and that namespace has a declaration
    /// in each tree. The type is introduced into the declaration of the tree of <see cref="IAssembly.GlobalNamespace"/>
    /// and must also be added to the declaration of the merged tree. A namespace declared by this compilation alone
    /// would not cover this case, because Roslyn then returns the single constituent and one declaration exists.
    /// <c>System</c> is used because every compilation references an assembly that declares it. See issue #1825.
    /// </remarks>
    [Fact]
    public void DurableTypeIdRefToTypeIntroducedIntoMergedNamespaceResolves()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "namespace System { class Outer; }" ).CreateMutableClone();

        var mergedNamespace = compilation.GlobalNamespace.GetDescendant( "System" ).AssertNotNull();
        var introducedType = RefTestFixtures.IntroduceType( compilation, mergedNamespace, "Companion" );

        var durableRef = DurableRefFactory.FromTypeId<INamedType>( introducedType.GetSerializableTypeId() );

        Assert.Same( introducedType, durableRef.GetTarget( compilation ) );
    }
}
