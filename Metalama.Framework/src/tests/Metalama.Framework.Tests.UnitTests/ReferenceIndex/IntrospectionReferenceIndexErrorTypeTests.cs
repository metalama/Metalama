// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.ReferenceGraph;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Comparers;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.ReferenceIndex;

/// <summary>
/// Verifies that the inbound reference index of the introspection API is built, and is complete, when the code contains
/// error types, unresolved symbols, or names that are ambiguous when accessibility is ignored. See issue #2415.
/// </summary>
/// <remarks>
/// <para>
/// The index is built with the options and the comparer of <c>ProjectReferenceGraph</c>: <see cref="ReferenceIndexerOptions.All"/>
/// and <see cref="StructuralSymbolComparer.Default"/>. An exception that the comparer throws while one syntax tree is indexed
/// discards the whole index. Before issue #2415 was fixed, the comparer threw for an error type.
/// </para>
/// <para>
/// Each case is the declaration of a class <c>B</c> whose method <c>M()</c> invokes <c>A.F()</c>. Another member of <c>B</c>
/// uses the type <c>Thing</c> or a related type in a specific construct. Each test verifies that the invocation of <c>A.F()</c>
/// is indexed, which is the case only when the index is built.
/// </para>
/// <para>
/// The cases run in two configurations. In the first one, two referenced assemblies declare the types of the namespace
/// <c>Lib</c>, which are internal in one assembly and public in the other. The code compiles. In a semantic model that ignores
/// accessibility, each name of <c>Lib</c> is ambiguous. This is the situation of the <c>Xunit.Assert</c> type that the
/// <c>Microsoft.Reactive.Testing</c> package contains. Before issue #2415 was fixed, the indexer used such a semantic model.
/// In the second configuration, the namespace <c>Lib</c> does not exist, so the code does not compile. The introspection API
/// also indexes such code.
/// </para>
/// </remarks>
public sealed class IntrospectionReferenceIndexErrorTypeTests : UnitTestClass
{
    private const string _codeOfA = "class A { public static int F() => 0; }";

    private const string _header = """
                                   #pragma warning disable CS0067, CS0169, CS0219, CS0649, CS8321
                                   using Lib;
                                   using System;
                                   using System.Collections.Generic;

                                   """;

    /// <summary>
    /// The declarations of the namespace <c>Lib</c>, where <c>ACCESSIBILITY</c> is replaced by <c>internal</c> or <c>public</c>.
    /// </summary>
    private const string _libraryCode = """
                                        namespace Lib
                                        {
                                            ACCESSIBILITY static class Assert
                                            {
                                                public static void Equal<T>( T a, T b ) {}
                                            }

                                            ACCESSIBILITY class Thing
                                            {
                                                public Thing() {}
                                                public Thing( int x ) {}
                                                public static System.Collections.Generic.List<int> Items { get; } = new();
                                                public static void Do<T>( T a ) {}
                                                public static Thing operator +( Thing a, Thing b ) => a;
                                                public void Run() {}
                                                public virtual void Virtual() {}
                                                public int this[ int i ] => i;
                                            }

                                            ACCESSIBILITY class GenericThing<T>
                                            {
                                                public virtual void Virtual() {}
                                            }

                                            ACCESSIBILITY interface IThing
                                            {
                                                void Run();
                                            }

                                            ACCESSIBILITY struct ThingStruct {}

                                            ACCESSIBILITY delegate void ThingHandler();

                                            ACCESSIBILITY sealed class ThingAttribute : System.Attribute {}

                                            ACCESSIBILITY static class ThingExtensions
                                            {
                                                public static void Extend( this Thing t ) {}
                                            }
                                        }
                                        """;

    /// <summary>
    /// Gets the cases that are valid code when <c>Lib</c> is referenced.
    /// </summary>
    public static TheoryData<string, string> CommonCases
        => new()
        {
            // The case of issue #2415.
            {
                "ConditionalOfCollectionExpressions",
                "class B { void M() { A.F(); } void N( string? s, List<string> actual ) => Assert.Equal( s is null ? [] : [s], actual ); }"
            },
            { "CollectionExpressionArgument", "class B { void M() { A.F(); } void N() => Assert.Equal( [], new List<int>() ); }" },
            { "CollectionExpressionOfType", "class B { void M() { A.F(); } void N() { List<Thing> l = []; } }" },
            { "CollectionExpressionWithSpread", "class B { void M() { A.F(); } void N() { List<int> l = [..Thing.Items]; } }" },
            { "ObjectCreation", "class B { void M() { A.F(); } void N() { var t = new Thing(); } }" },
            { "ObjectCreationWithArgument", "class B { void M() { A.F(); } void N() { var t = new Thing( 1 ); } }" },
            { "ImplicitObjectCreation", "class B { void M() { A.F(); } void N() { Thing t = new(); } }" },
            { "StructDefault", "class B { void M() { A.F(); } void N() { ThingStruct s = default; } }" },
            { "LocalType", "class B { void M() { A.F(); } void N() { Thing? t = null; } }" },
            { "ParameterType", "class B { void M() { A.F(); this.N( null! ); } void N( Thing t ) {} }" },
            { "ReturnType", "class B { void M() { A.F(); this.N(); } Thing? N() => null; }" },
            { "FieldType", "class B { Thing? _f; void M() { A.F(); _ = this._f; } }" },
            { "PropertyType", "class B { Thing? P => null; void M() { A.F(); _ = this.P; } }" },
            { "EventType", "class B { event ThingHandler? E; void M() { A.F(); this.E?.Invoke(); } }" },
            { "ArrayCreation", "class B { void M() { A.F(); } void N() { var a = new Thing[1]; } }" },
            { "ArrayParameter", "class B { void M() { A.F(); this.N( [] ); } void N( Thing[] a ) {} }" },
            { "ArrayReturnType", "class B { void M() { A.F(); this.N(); } Thing[] N() => []; }" },
            { "GenericTypeArgument", "class B { void M() { A.F(); } void N() { var l = new List<Thing>(); } }" },
            { "GenericTypeArgumentOfParameter", "class B { void M() { A.F(); this.N( null! ); } void N( List<Thing> l ) {} }" },
            { "GenericMethodArgument", "class B { void M() { A.F(); this.N<Thing>(); } void N<T>() {} }" },
            { "GenericMethodArgumentInferred", "class B { void M() { A.F(); this.N( new Thing() ); } void N<T>( T t ) {} }" },
            { "TypeConstraint", "class B { void M() { A.F(); } void N<T>() where T : Thing {} }" },
            { "TypeOf", "class B { void M() { A.F(); } void N() => _ = typeof(Thing); }" },
            { "Cast", "class B { void M() { A.F(); } void N( object o ) => _ = (Thing) o; }" },
            { "As", "class B { void M() { A.F(); } void N( object o ) => _ = o as Thing; }" },
            { "Is", "class B { void M() { A.F(); } void N( object o ) => _ = o is Thing; }" },
            { "IsPattern", "class B { void M() { A.F(); } void N( object o ) { if ( o is Thing t ) {} } }" },
            { "SwitchPattern", "class B { void M() { A.F(); } int N( object o ) => o switch { Thing => 1, _ => 0 }; }" },
            { "NameOf", "class B { void M() { A.F(); } void N() => _ = nameof(Thing); }" },
            { "NameOfMember", "class B { void M() { A.F(); } void N() => _ = nameof(Thing.Run); }" },
            { "Default", "class B { void M() { A.F(); } void N() => _ = default(Thing); }" },
            { "Tuple", "class B { void M() { A.F(); } void N() { (Thing?, int) t = default; } }" },
            { "TupleElementName", "class B { void M() { A.F(); } void N() { (Thing? Name, int Count) t = default; } }" },
            { "Nullable", "class B { void M() { A.F(); } void N() { ThingStruct? s = null; } }" },
            { "StaticMethod", "class B { void M() { A.F(); } void N() => Thing.Do( 1 ); }" },
            { "StaticProperty", "class B { void M() { A.F(); } void N() => _ = Thing.Items; }" },
            { "InstanceMethod", "class B { void M() { A.F(); } void N( Thing t ) => t.Run(); }" },
            { "Indexer", "class B { void M() { A.F(); } void N( Thing t ) => _ = t[0]; }" },
            { "Operator", "class B { void M() { A.F(); } void N( Thing a, Thing b ) => _ = a + b; }" },
            { "ExtensionMethod", "class B { void M() { A.F(); } void N( Thing t ) => t.Extend(); }" },
            { "Attribute", "class B { void M() { A.F(); } [Thing] void N() {} }" },
            { "LambdaParameter", "class B { void M() { A.F(); } void N() { Action<Thing> a = t => t.Run(); } }" },
            { "ExplicitLambdaParameter", "class B { void M() { A.F(); } void N() { var a = ( Thing t ) => t.Run(); } }" },
            { "LocalFunction", "class B { void M() { A.F(); } void N() { void L( Thing t ) {} } }" },
            { "DelegateCreation", "class B { void M() { A.F(); } void N() { ThingHandler h = this.M; } }" },
            { "BaseType", "class B : Thing { void M() { A.F(); } }" },
            { "BaseTypeReferenced", "class B : Thing { void M() { A.F(); } } class C { void N( B b ) => _ = b; }" },
            { "GenericBaseType", "class B : GenericThing<int> { void M() { A.F(); } }" },
            { "BaseTypeWithErrorTypeArgument", "class B : List<Thing> { void M() { A.F(); } }" },
            { "ImplementedInterface", "class B : IThing { public void Run() {} void M() { A.F(); } }" },
            { "ExplicitInterfaceImplementation", "class B : IThing { void IThing.Run() {} void M() { A.F(); } }" },
            { "OverrideMethod", "class B : Thing { public override void Virtual() {} void M() { A.F(); this.Virtual(); } }" },
            { "OverrideMethodOfGenericBase", "class B : GenericThing<Thing> { public override void Virtual() {} void M() { A.F(); this.Virtual(); } }" },
            { "ImplicitBaseConstructor", "class B : Thing { public B() {} void M() { A.F(); _ = new B(); } }" },
            { "ExplicitBaseConstructor", "class B : Thing { public B() : base( 1 ) {} void M() { A.F(); _ = new B(); } }" },
            { "PrimaryConstructorBase", "class B() : Thing( 1 ) { void M() { A.F(); } }" },
            { "NestedTypeBase", "class B { class Nested : Thing {} void M() { A.F(); _ = new Nested(); } }" },
            { "Using", "class B { void M() { A.F(); } void N( Thing t ) { using var d = t as IDisposable; } }" },
            { "Foreach", "class B { void M() { A.F(); } void N( List<Thing> l ) { foreach ( var t in l ) {} } }" },
            { "Catch", "class B { void M() { A.F(); } void N() { try {} catch ( Exception e ) when ( e.Data is Thing ) {} } }" },
            { "Throw", "class B { void M() { A.F(); } void N( Thing t ) => throw new InvalidOperationException( t.ToString() ); }" },
            { "StringInterpolation", "class B { void M() { A.F(); } void N( Thing t ) => _ = $\"{t}\"; }" },
            { "Query", "class B { void M() { A.F(); } void N( List<Thing> l ) => _ = System.Linq.Enumerable.Select( l, t => t ); }" },
            { "ConditionalAccess", "class B { void M() { A.F(); } void N( Thing? t ) => t?.Run(); }" },
            { "Coalesce", "class B { void M() { A.F(); } void N( Thing? t ) => _ = t ?? new Thing(); }" },
            { "Conditional", "class B { void M() { A.F(); } void N( bool b ) => _ = b ? new Thing() : null; }" },
            { "Await", "class B { void M() { A.F(); } async System.Threading.Tasks.Task<Thing?> N() { await System.Threading.Tasks.Task.Yield(); return null; } }" },
            { "Yield", "class B { void M() { A.F(); } IEnumerable<Thing> N() { yield break; } }" }
        };

    /// <summary>
    /// Gets the cases that are possible only in code that does not compile.
    /// </summary>
    public static TheoryData<string, string> ErroneousCodeCases
        => new()
        {
            { "UnknownMethod", "class B { void M() { A.F(); } void N() => Unknown(); }" },
            { "UnknownMember", "class B { void M() { A.F(); } void N() => A.Unknown(); }" },
            { "UnknownType", "class B { void M() { A.F(); } void N() { Unknown x = default; } }" },
            { "UnknownGenericType", "class B { void M() { A.F(); } void N() { Unknown<int> x = default; } }" },
            { "UnknownNamespace", "class B { void M() { A.F(); } void N() { Unknown.Namespace.Type x = default; } }" },
            { "AmbiguousOverload", "class B { void M() { A.F(); this.N( null, null ); } void N( object a, string b ) {} void N( string a, object b ) {} }" },
            { "WrongArgumentCount", "class B { void M() { A.F(); this.N( 1, 2 ); } void N( int a ) {} }" },
            { "WrongArgumentType", "class B { void M() { A.F(); this.N( \"\" ); } void N( int a ) {} }" },
            { "CollectionExpressionWithoutTargetType", "class B { void M() { A.F(); } void N() { var x = []; } }" },
            { "CollectionExpressionOfUnknownType", "class B { void M() { A.F(); } void N() { Unknown x = []; } }" },
            { "CollectionExpressionInUnknownInvocation", "class B { void M() { A.F(); } void N() => Unknown( [] ); }" },
            { "UnknownArrayCreation", "class B { void M() { A.F(); } void N() { var a = new Unknown[1]; } }" },
            { "UnknownArrayInitializer", "class B { void M() { A.F(); } void N() { var a = new[] { Unknown }; } }" },
            { "ImplicitArrayOfUnknown", "class B { void M() { A.F(); } void N( Unknown u ) { var a = new[] { u }; } }" },
            { "UnknownPointer", "class B { void M() { A.F(); } unsafe void N() { Unknown* p = null; } }" },
            { "PointerOfManagedType", "class B { void M() { A.F(); } unsafe void N() { B* p = null; } }" },
            { "UnknownFunctionPointer", "class B { void M() { A.F(); } unsafe void N() { delegate*<Unknown, void> p = null; } }" },
            { "UnknownBaseType", "class B : Unknown { void M() { A.F(); } }" },
            { "UnknownBaseTypeReferenced", "class B : Unknown { void M() { A.F(); } } class C { void N( B b ) => _ = b; }" },
            { "UnknownInterface", "class B : IUnknown { void M() { A.F(); } }" },
            { "UnknownGenericBaseType", "class B : Unknown<int> { void M() { A.F(); } }" },
            { "OverrideWithoutBase", "class B : Unknown { public override void Virtual() {} void M() { A.F(); this.Virtual(); } }" },
            { "BaseConstructorOfUnknownBase", "class B : Unknown { public B() : base( 1 ) {} void M() { A.F(); _ = new B(); } }" },
            { "PrimaryConstructorOfUnknownBase", "class B() : Unknown( 1 ) { void M() { A.F(); } }" },
            { "UnknownReturnTypeCalled", "class B { void M() { A.F(); this.N().Run(); } Unknown N() => default; }" },
            { "UnknownParameterTypeCalled", "class B { void M() { A.F(); this.N( default ); } void N( Unknown u ) {} }" },
            { "UnknownTypeArgumentCalled", "class B { void M() { A.F(); this.N<Unknown>(); } void N<T>() {} }" },
            { "UnknownAttribute", "class B { void M() { A.F(); } [Unknown] void N() {} }" },
            { "UnknownAttributeArgument", "class B { void M() { A.F(); } [Obsolete( Unknown )] void N() {} }" },
            { "UnknownTypeOf", "class B { void M() { A.F(); } void N() => _ = typeof(Unknown); }" },
            { "UnknownNameOf", "class B { void M() { A.F(); } void N() => _ = nameof(Unknown); }" },
            { "UnknownLambdaParameter", "class B { void M() { A.F(); } void N() { var a = ( Unknown u ) => u; } }" },
            { "UnknownTupleElement", "class B { void M() { A.F(); } void N() { (Unknown, int) t = default; } }" },
            { "UnknownTypeConstraint", "class B { void M() { A.F(); } void N<T>() where T : Unknown {} }" },
            { "UnknownEventType", "class B { event Unknown? E; void M() { A.F(); this.E?.Invoke(); } }" },
            { "UnknownPropertyType", "class B { Unknown P => default; void M() { A.F(); _ = this.P; } }" },
            { "UnknownFieldType", "class B { Unknown _f; void M() { A.F(); _ = this._f; } }" },
            { "UnknownIndexerParameter", "class B { int this[ Unknown u ] => 0; void M() { A.F(); _ = this[default]; } }" },
            { "UnknownOperatorParameter", "class B { public static B operator +( B a, Unknown b ) => a; void M() { A.F(); _ = this + default; } }" },
            { "UnknownConstructorParameter", "class B { public B( Unknown u ) {} void M() { A.F(); _ = new B( default ); } }" },
            { "UnknownDelegateParameter", "delegate void D( Unknown u ); class B { void M() { A.F(); D d = _ => {}; d( default ); } }" },
            { "UnknownExtensionReceiver", "static class B { static void M() { A.F(); } static void N( this Unknown u ) {} static void O( Unknown u ) => u.N(); }" },
            { "UnknownExtensionBlockReceiver", "static class B { extension( Unknown u ) { void N() {} } static void M() { A.F(); } }" },
            { "UnknownRecordParameter", "record B( Unknown U ) { void M() { A.F(); _ = this.U; } }" },
            { "UnknownGenericArgumentOfInheritedType", "class B : List<Unknown> { void M() { A.F(); this.Add( default ); } }" },
            { "UnknownOverrideReturnType", "class B { public override Unknown ToString() => default; void M() { A.F(); _ = this.ToString(); } }" },
            { "UnknownPartialMethod", "partial class B { partial void N( Unknown u ); void M() { A.F(); this.N( default ); } }" },
            { "DuplicateMember", "class B { void M() { A.F(); } void M() { A.F(); } }" },
            { "MissingSemicolon", "class B { void M() { A.F() } }" },
            { "MissingClosingBrace", "class B { void M() { A.F(); }" }
        };

    /// <summary>
    /// Verifies that the index is built when the names of <c>Lib</c> are ambiguous in a semantic model that ignores
    /// accessibility.
    /// </summary>
    [Theory]
    [MemberData( nameof(CommonCases) )]
    public void AmbiguousTypeName( string caseName, string codeOfB )
    {
        _ = caseName;

        this.VerifyIndexOfInvocationOfF( codeOfB, referencesLibrary: true );
    }

    /// <summary>
    /// Verifies that the index is built when the names of <c>Lib</c> cannot be resolved.
    /// </summary>
    [Theory]
    [MemberData( nameof(CommonCases) )]
    public void UnresolvedTypeName( string caseName, string codeOfB )
    {
        _ = caseName;

        this.VerifyIndexOfInvocationOfF( codeOfB, referencesLibrary: false );
    }

    /// <summary>
    /// Verifies that the index is built for code that contains a construct that is valid only in code that does not compile.
    /// </summary>
    [Theory]
    [MemberData( nameof(ErroneousCodeCases) )]
    public void ErroneousCode( string caseName, string codeOfB )
    {
        _ = caseName;

        this.VerifyIndexOfInvocationOfF( codeOfB, referencesLibrary: false );
    }

    /// <summary>
    /// Verifies that, when the names of <c>Lib</c> are ambiguous in a semantic model that ignores accessibility, the reference
    /// to the public declaration of <c>Lib</c>, to which the compiler binds the name, is indexed.
    /// </summary>
    /// <remarks>
    /// A reference that the semantic model of the indexer cannot bind is not indexed. The index is then incomplete, and
    /// nothing in the result of the introspection API indicates it.
    /// </remarks>
    [Theory]
    [MemberData( nameof(CommonCases) )]
    public void AmbiguousTypeNameIsIndexed( string caseName, string codeOfB )
    {
        _ = caseName;

        this.BuildIndex(
            _codeOfA,
            codeOfB,
            referencesLibrary: true,
            ( index, _ ) =>
            {
                var referencedPublicSymbols = index.ReferencedSymbols
                    .Where( s => s.References.Any() && s.ReferencedSymbol.ContainingAssembly?.Name == "PublicLib" )
                    .Select( s => s.ReferencedSymbol.ToDisplayString() )
                    .ToList();

                Assert.NotEmpty( referencedPublicSymbols );
            } );
    }

    /// <summary>
    /// Verifies that a reference to an inaccessible member, which the compiler reports as an error, is indexed.
    /// </summary>
    /// <remarks>
    /// The semantic model of the indexer respects accessibility, so the name binds to no symbol, and the inaccessible member
    /// is only a candidate.
    /// </remarks>
    [Theory]
    [InlineData( "class B { void M() { A.F(); A.G(); } }" )]
    [InlineData( "class B { void M() { A.F(); _ = nameof(A.G); } }" )]
    [InlineData( "class B { void M() { A.F(); System.Func<int> f = A.G; } }" )]
    public void InaccessibleMember( string codeOfB )
        => this.BuildIndex(
            "class A { public static int F() => 0; private static int G() => 0; }",
            codeOfB,
            referencesLibrary: false,
            ( index, compilation ) =>
            {
                var methodG = compilation.Types.OfName( "A" ).Single().Methods.OfName( "G" ).Single().GetSymbol().AssertNotNull();

                Assert.True( index.TryGetInboundReferences( methodG, out var referencedSymbolInfo ) );
                Assert.Equal( ["B.M()"], referencedSymbolInfo.References.Select( r => r.ReferencingSymbol.ToTestName() ) );
            } );

    /// <summary>
    /// Builds the index of the introspection API for <c>A</c> and the given declaration of <c>B</c>, and verifies that the
    /// invocation of <c>A.F()</c> from <c>B.M()</c> is indexed.
    /// </summary>
    private void VerifyIndexOfInvocationOfF( string codeOfB, bool referencesLibrary )
        => this.BuildIndex(
            _codeOfA,
            codeOfB,
            referencesLibrary,
            ( index, compilation ) =>
            {
                var methodF = compilation.Types.OfName( "A" ).Single().Methods.OfName( "F" ).Single().GetSymbol().AssertNotNull();

                Assert.True( index.TryGetInboundReferences( methodF, out var referencedSymbolInfo ) );
                Assert.Contains( "B.M()", referencedSymbolInfo.References.Select( r => r.ReferencingSymbol.ToTestName() ) );
            } );

    /// <summary>
    /// Builds the index of the introspection API for the given declarations of <c>A</c> and <c>B</c>, and passes it to
    /// <paramref name="verify"/> while the test context is alive.
    /// </summary>
    private void BuildIndex( string codeOfA, string codeOfB, bool referencesLibrary, Action<InboundReferenceIndex, CompilationModel> verify )
    {
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddProjectService( SymbolClassificationService.CreateTestInstance() );
        using var testContext = this.CreateTestContext( additionalServices );

        var code = new Dictionary<string, string> { ["A.cs"] = codeOfA, ["B.cs"] = _header + codeOfB };

        IEnumerable<MetadataReference>? additionalReferences = null;

        if ( referencesLibrary )
        {
            additionalReferences =
            [
                testContext.CreateCSharpCompilation( _libraryCode.Replace( "ACCESSIBILITY", "internal" ), assemblyName: "InternalLib" )
                    .ToMetadataReference(),
                testContext.CreateCSharpCompilation( _libraryCode.Replace( "ACCESSIBILITY", "public" ), assemblyName: "PublicLib" )
                    .ToMetadataReference()
            ];
        }

        // When the library is referenced, the code must compile, so that the test covers the semantic model of the indexer
        // and not a compilation error.
        var roslynCompilation = testContext.CreateCSharpCompilation( code, ignoreErrors: !referencesLibrary, additionalReferences: additionalReferences );
        var compilation = testContext.CreateCompilationModel( roslynCompilation );

        var builder = new InboundReferenceIndexBuilder( testContext.ServiceProvider, ReferenceIndexerOptions.All, StructuralSymbolComparer.Default );

        foreach ( var syntaxTree in compilation.PartialCompilation.SyntaxTreeCollection )
        {
            builder.IndexSyntaxTree( syntaxTree, compilation.CompilationContext.SemanticModelProvider, TestContext.Current.CancellationToken );
        }

        verify( builder.ToReadOnly(), compilation );
    }
}
