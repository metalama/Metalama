// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.ReferenceGraph;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.ReferenceIndex;

public sealed class InboundReferenceIndexTests : UnitTestClass
{
    [Fact]
    public void BaseType()
    {
        var code = new Dictionary<string, string>() { ["A.cs"] = "class A;", ["B.cs"] = "class B : A;", ["C.cs"] = "class C : B;" };

        var result = this.BuildIndex( code, compilation => compilation.Types.OfName( "A" ), ReferenceKinds.BaseType );

        // Checking the index.
        Assert.Single( result.ReferencingSymbols, "B" );

        // Checking symbol resolution.
        Assert.Equal( ["B.cs"], result.Observer.ResolvedSemanticModelNames );
        Assert.Equal( ["A", "B"], result.Observer.ResolvedSymbolNames );
    }

    [Fact]
    public void BaseTypeDerived()
    {
        var code = new Dictionary<string, string>() { ["A.cs"] = "class A;", ["B.cs"] = "class B : A;", ["C.cs"] = "class C : B;" };

        var result = this.BuildIndex( code, compilation => compilation.Types.OfName( "A" ), ReferenceKinds.BaseType, true );

        // Checking the index.
        Assert.Equal<IEnumerable<string>>( ["B", "C"], result.ReferencingSymbols );

        // Checking symbol resolution.
        Assert.Equal( ["B.cs", "C.cs"], result.Observer.ResolvedSemanticModelNames );
        Assert.Equal( ["A", "B", "C"], result.Observer.ResolvedSymbolNames );
    }

    [Fact]
    public void BaseTypeDerivedSealed()
    {
        var code = new Dictionary<string, string>() { ["A.cs"] = "sealed class A;", ["B.cs"] = "class B;", ["C.cs"] = "class C : B;" };

        var result = this.BuildIndex( code, compilation => compilation.Types.OfName( "A" ), ReferenceKinds.BaseType, true );

        // Checking the index.
        Assert.Equal<IEnumerable<string>>( [], result.ReferencingSymbols );

        // Checking symbol resolution.
        Assert.Equal( [], result.Observer.ResolvedSemanticModelNames );
        Assert.Equal( [], result.Observer.ResolvedSymbolNames );
    }

    [Fact]
    public void ImplementedInterface()
    {
        var code = new Dictionary<string, string>() { ["A.cs"] = "interface A;", ["B.cs"] = "class B : A;", ["C.cs"] = "class C : B;" };

        var result = this.BuildIndex( code, compilation => compilation.Types.OfName( "A" ), ReferenceKinds.BaseType );

        // Checking the index.
        Assert.Single( result.ReferencingSymbols, "B" );

        // Checking symbol resolution.
        Assert.Equal( ["B.cs"], result.Observer.ResolvedSemanticModelNames );
        Assert.Equal( ["A", "B"], result.Observer.ResolvedSymbolNames );
    }

    [Fact]
    public void ImplementedInterfaceDerived()
    {
        var code = new Dictionary<string, string>() { ["A.cs"] = "interface A;", ["B.cs"] = "class B : A;", ["C.cs"] = "class C : B;" };

        var result = this.BuildIndex( code, compilation => compilation.Types.OfName( "A" ), ReferenceKinds.BaseType, true );

        // Checking the index.
        Assert.Equal<IEnumerable<string>>( ["B", "C"], result.ReferencingSymbols );

        // Checking symbol resolution.
        Assert.Equal( ["B.cs", "C.cs"], result.Observer.ResolvedSemanticModelNames );
        Assert.Equal( ["A", "B", "C"], result.Observer.ResolvedSymbolNames );
    }

    [Fact]
    public void TypeArgument()
    {
        var code = new Dictionary<string, string>()
        {
            ["A.cs"] = "class A;", ["B.cs"] = "class B : System.Collections.Generic.List<A>;", ["C.cs"] = "class C : B;"
        };

        var result = this.BuildIndex( code, compilation => compilation.Types.OfName( "A" ), ReferenceKinds.TypeArgument );

        // Checking the index.
        Assert.Single( result.ReferencingSymbols, "B" );

        // Checking symbol resolution.
        Assert.Equal( ["B.cs"], result.Observer.ResolvedSemanticModelNames );
        Assert.Equal( ["A", "B"], result.Observer.ResolvedSymbolNames );
    }

    [Fact]
    public void TypeArgumentDerived()
    {
        var code = new Dictionary<string, string>()
        {
            ["A.cs"] = "class A;", ["B.cs"] = "class B : A;", ["C.cs"] = "class C : System.Collections.Generic.List<B>;"
        };

        var result = this.BuildIndex( code, compilation => compilation.Types.OfName( "A" ), ReferenceKinds.TypeArgument, true );

        // Checking the index.
        Assert.Single( result.ReferencingSymbols, "C" );

        // Checking symbol resolution.
        Assert.Equal( ["C.cs"], result.Observer.ResolvedSemanticModelNames );
        Assert.Equal( ["B", "C"], result.Observer.ResolvedSymbolNames );
    }

    [Fact]
    public void TypeArgumentDeep()
    {
        var code = new Dictionary<string, string>()
        {
            ["A.cs"] = "class A;", ["B.cs"] = "class B : System.Collections.Generic.List<System.Collections.Generic.List<A>>;", ["C.cs"] = "class C : B;"
        };

        var result = this.BuildIndex( code, compilation => compilation.Types.OfName( "A" ), ReferenceKinds.TypeArgument );

        // Checking the index.
        Assert.Single( result.ReferencingSymbols, "B" );

        // Checking symbol resolution.
        Assert.Equal( ["B.cs"], result.Observer.ResolvedSemanticModelNames );
        Assert.Equal( ["A", "B"], result.Observer.ResolvedSymbolNames );
    }

    [Fact]
    public void TypeArgumentNullable()
    {
        var code = new Dictionary<string, string>()
        {
            ["A.cs"] = "class A;", ["B.cs"] = "class B : System.Collections.Generic.List<A?>;", ["C.cs"] = "class C : B;"
        };

        var result = this.BuildIndex( code, compilation => compilation.Types.OfName( "A" ), ReferenceKinds.TypeArgument );

        // Checking the index.
        Assert.Single( result.ReferencingSymbols, "B" );

        // Checking symbol resolution.
        Assert.Equal( ["B.cs"], result.Observer.ResolvedSemanticModelNames );
        Assert.Equal( ["A", "B"], result.Observer.ResolvedSymbolNames );
    }

    [Fact]
    public void TypeOf()
    {
        var code = new Dictionary<string, string>()
        {
            ["A.cs"] = "class A;", ["B.cs"] = "class B { object M() => typeof(A); }", ["C.cs"] = "class C { object M() => typeof(B); }"
        };

        var result = this.BuildIndex( code, compilation => compilation.Types.OfName( "A" ), ReferenceKinds.TypeOf );

        // Checking the index.
        Assert.Single( result.ReferencingSymbols, "B.M()" );

        // Checking symbol resolution.
        Assert.Equal( ["B.cs"], result.Observer.ResolvedSemanticModelNames );
        Assert.Equal( ["A", "B.M()"], result.Observer.ResolvedSymbolNames );
    }

    [Fact]
    public void TypeOfDerived()
    {
        var code = new Dictionary<string, string>()
        {
            ["A.cs"] = "class A;", ["B.cs"] = "class B { object M() => typeof(A); }", ["C.cs"] = "class C { object M() => typeof(B); }"
        };

        var result = this.BuildIndex( code, compilation => compilation.Types.OfName( "A" ), ReferenceKinds.TypeOf, true );

        // Checking the index.
        Assert.Equal( ["B.M()", "C.M()"], result.ReferencingSymbols );

        // Checking symbol resolution.
        Assert.Equal( ["B.cs", "C.cs"], result.Observer.ResolvedSemanticModelNames );
        Assert.Equal( ["A", "B", "B.M()", "C.M()"], result.Observer.ResolvedSymbolNames );
    }

    [Fact]
    public void ParameterType()
    {
        var code = new Dictionary<string, string>()
        {
            ["A.cs"] = "class A;", ["B.cs"] = "class B { void M(A a) {} int this[A a] => 0; }", ["C.cs"] = "class C { void M(B b) {} int this[B b] => 0; }"
        };

        var result = this.BuildIndex( code, compilation => compilation.Types.OfName( "A" ), ReferenceKinds.ParameterType );

        // Checking the index.
        Assert.Equal( ["B.M(A):a", "B.this[A]:a"], result.ReferencingSymbols );

        // Checking symbol resolution.
        Assert.Equal( ["B.cs"], result.Observer.ResolvedSemanticModelNames );
        Assert.Equal( ["A", "B.M(A):a", "B.this[A]:a"], result.Observer.ResolvedSymbolNames );
    }

    [Fact]
    public void ParameterTypeDerived()
    {
        var code = new Dictionary<string, string>()
        {
            ["A.cs"] = "class A;", ["B.cs"] = "class B { void M(A a) {} int this[A a] => 0; }", ["C.cs"] = "class C { void M(B b) {} int this[B b] => 0; }"
        };

        var result = this.BuildIndex( code, compilation => compilation.Types.OfName( "A" ), ReferenceKinds.ParameterType, true );

        // Checking the index.
        Assert.Equal( ["B.M(A):a", "B.this[A]:a", "C.M(B):b", "C.this[B]:b"], result.ReferencingSymbols );

        // Checking symbol resolution.
        Assert.Equal( ["B.cs", "C.cs"], result.Observer.ResolvedSemanticModelNames );
        Assert.Equal( ["A", "B", "B.M(A):a", "B.this[A]:a", "C.M(B):b", "C.this[B]:b"], result.Observer.ResolvedSymbolNames );
    }

    [Fact]
    public void TypeConstraint()
    {
        var code = new Dictionary<string, string>()
        {
            ["A.cs"] = "class A;", ["B.cs"] = "class B<T> where T : A;", ["C.cs"] = "class C : A;", ["D.cs"] = "class D<T> where T : C;"
        };

        var result = this.BuildIndex( code, compilation => compilation.Types.OfName( "A" ), ReferenceKinds.TypeConstraint );

        // Checking the index.
        Assert.Equal( ["B<T>"], result.ReferencingSymbols );

        // Checking symbol resolution.
        Assert.Equal( ["B.cs"], result.Observer.ResolvedSemanticModelNames );
        Assert.Equal( ["A", "B<T>"], result.Observer.ResolvedSymbolNames );
    }

    [Fact]
    public void TypeConstraintDerived()
    {
        var code = new Dictionary<string, string>()
        {
            ["A.cs"] = "class A;", ["B.cs"] = "class B<T> where T : A;", ["C.cs"] = "class C : A;", ["D.cs"] = "class D<T> where T : C;"
        };

        var result = this.BuildIndex( code, compilation => compilation.Types.OfName( "A" ), ReferenceKinds.TypeConstraint, true );

        // Checking the index.
        Assert.Equal( ["B<T>", "D<T>"], result.ReferencingSymbols );

        // Checking symbol resolution.
        Assert.Equal( ["B.cs", "D.cs"], result.Observer.ResolvedSemanticModelNames );
        Assert.Equal( ["A", "B<T>", "C", "D<T>"], result.Observer.ResolvedSymbolNames );
    }

    [Fact]
    public void TypeOfInTopLevelStatement()
    {
        var code = new Dictionary<string, string>()
        {
            ["Program.cs"] = "_ = typeof(A);", ["A.cs"] = "class A;", ["B.cs"] = "class B { object M() => typeof(A); }"
        };

        var result = this.BuildIndex(
            code,
            compilation => compilation.Types.OfName( "A" ),
            ReferenceKinds.TypeOf,
            outputKind: OutputKind.ConsoleApplication );

        // Checking the index: both the top-level statement (synthetic entry point) and B.M() should reference A via typeof.
        Assert.Equal( 2, result.ReferencingSymbols.Count );
        Assert.Contains( "B.M()", result.ReferencingSymbols );
        Assert.Contains( result.ReferencingSymbols, s => s != "B.M()" );
    }

    [Fact]
    public void ObjectCreationInExpressionBodiedProperty()
    {
        // Expression-bodied property and expression-bodied getter are semantically equivalent.
        // Both should report the getter accessor as the referencing symbol.
        var code = new Dictionary<string, string>()
        {
            ["A.cs"] = "class A { }",
            ["B.cs"] = "class B { A ExpressionBodied => new A(); }",
            ["C.cs"] = "class C { A GetterExpressionBodied { get => new A(); } }"
        };

        var result = this.BuildIndex( code, compilation => compilation.Types.OfName( "A" ), ReferenceKinds.ObjectCreation );

        // Both should report the getter accessor (get_*), not the property itself.
        Assert.Equal( 2, result.ReferencingSymbols.Count );
        Assert.All( result.ReferencingSymbols, s => Assert.Contains( ".get", s ) );
    }

    [Fact]
    public void ObjectCreationInExpressionBodiedIndexer()
    {
        // Expression-bodied indexer and indexer with expression-bodied getter are semantically equivalent.
        // Both should report the getter accessor as the referencing symbol.
        var code = new Dictionary<string, string>()
        {
            ["A.cs"] = "class A { }", ["B.cs"] = "class B { A this[int i] => new A(); }", ["C.cs"] = "class C { A this[int i] { get => new A(); } }"
        };

        var result = this.BuildIndex( code, compilation => compilation.Types.OfName( "A" ), ReferenceKinds.ObjectCreation );

        // Both should report the getter accessor, not the indexer itself.
        Assert.Equal( 2, result.ReferencingSymbols.Count );
        Assert.All( result.ReferencingSymbols, s => Assert.Contains( ".this", s ) );
    }

    [Fact]
    public void InvocationOfExtensionMethodValidatedThroughExtensionBlock()
    {
        // A validator registered on an extension block must see the invocations of the members of that block.
        // The extension block cannot be named in source, so it enters the index only as the declaring type of
        // one of its members, which requires the reference indexer to descend into the referenced declaring type.
        var code = new Dictionary<string, string>()
        {
            ["A.cs"] = "static class A { extension(string s) { public int GetDoubleLength() => s.Length * 2; } }",
            ["B.cs"] = "class B { int M(string s) => s.GetDoubleLength(); }"
        };

        var result = this.BuildIndex(
            code,
            compilation => compilation.Types.OfName( "A" ).SelectMany( t => t.ExtensionBlocks ),
            ReferenceKinds.Invocation );

        Assert.Single( result.ReferencingSymbols, "B.M(string)" );
    }

    /// <summary>
    /// Verifies that an invocation of a generic method with explicit type arguments is indexed. The method name is then a generic name
    /// and not an identifier name.
    /// </summary>
    [Fact]
    public void GenericMethodInvocation()
    {
        var code = new Dictionary<string, string>()
        {
            ["A.cs"] = "class A { public static void M<T>() {} }", ["B.cs"] = "class B { void N() => A.M<int>(); }"
        };

        var result = this.BuildIndex( code, GetMethodsOfA( "M" ), ReferenceKinds.Invocation );

        Assert.Equal( ["B.N()"], result.ReferencingSymbols );
    }

    [Fact]
    public void ConditionalGenericMethodInvocation()
    {
        var code = new Dictionary<string, string>()
        {
            ["A.cs"] = "class A { public void M<T>() {} }", ["B.cs"] = "class B { void N( A a ) => a?.M<int>(); }"
        };

        var result = this.BuildIndex( code, GetMethodsOfA( "M" ), ReferenceKinds.Invocation );

        Assert.Equal( ["B.N(A)"], result.ReferencingSymbols );
    }

    [Fact]
    public void QualifiedGenericTypeName()
    {
        var code = new Dictionary<string, string>()
        {
            ["G.cs"] = "namespace N { class G<T>; }", ["B.cs"] = "class B { object M() => typeof(N.G<int>); }"
        };

        var result = this.BuildIndex( code, compilation => compilation.Types.OfName( "G" ), ReferenceKinds.TypeOf );

        Assert.Equal( ["B.M()"], result.ReferencingSymbols );
    }

    [Fact]
    public void GenericAttribute()
    {
        var code = new Dictionary<string, string>()
        {
            ["GA.cs"] = "class GA<T> : System.Attribute;", ["B.cs"] = "[GA<int>] class B;"
        };

        var result = this.BuildIndex( code, compilation => compilation.Types.OfName( "GA" ), ReferenceKinds.AttributeType );

        Assert.Equal( ["B"], result.ReferencingSymbols );
    }

    [Fact]
    public void InvocationInCollectionExpressionElement()
    {
        var code = new Dictionary<string, string>()
        {
            ["A.cs"] = "class A { public static int F() => 0; }", ["B.cs"] = "class B { int[] M() => [A.F()]; }"
        };

        var result = this.BuildIndex( code, GetMethodsOfA( "F" ), ReferenceKinds.Invocation );

        Assert.Equal( ["B.M()"], result.ReferencingSymbols );
    }

    [Fact]
    public void InvocationInSpreadElement()
    {
        var code = new Dictionary<string, string>()
        {
            ["A.cs"] = "class A { public static int[] F() => []; }", ["B.cs"] = "class B { int[] M() => [..A.F()]; }"
        };

        var result = this.BuildIndex( code, GetMethodsOfA( "F" ), ReferenceKinds.Invocation );

        Assert.Equal( ["B.M()"], result.ReferencingSymbols );
    }

    [Fact]
    public void InvocationInConstructorInitializerArgument()
    {
        var code = new Dictionary<string, string>()
        {
            ["A.cs"] = "class A { public static int F() => 0; }",
            ["B.cs"] = "class Base { public Base( int x ) {} } class B : Base { public B() : base( A.F() ) {} }"
        };

        var result = this.BuildIndex( code, GetMethodsOfA( "F" ), ReferenceKinds.Invocation );

        Assert.Equal( ["B.B()"], result.ReferencingSymbols );
    }

    [Fact]
    public void InvocationInArraySize()
    {
        var code = new Dictionary<string, string>()
        {
            ["A.cs"] = "class A { public static int F() => 0; }", ["B.cs"] = "class B { int[] M() => new int[A.F()]; }"
        };

        var result = this.BuildIndex( code, GetMethodsOfA( "F" ), ReferenceKinds.Invocation );

        Assert.Equal( ["B.M()"], result.ReferencingSymbols );
    }

    /// <summary>
    /// Verifies that the receiver of an assigned member, and the arguments of an assigned indexer, are indexed once. The walker used to visit them
    /// a second time after the assignment target.
    /// </summary>
    [Fact]
    public void AssignmentReceiverInvocationIndexedOnce()
    {
        var code = new Dictionary<string, string>()
        {
            ["A.cs"] = "class A { public static C F() => new(); public static int G() => 0; }",
            ["C.cs"] = "class C { public int P { get; set; } public int this[int i] { get => 0; set {} } }",
            ["B.cs"] = "class B { void M() { A.F().P = 1; A.F()[A.G()] = 2; } }"
        };

        var result = this.BuildIndex( code, GetMethodsOfA( "F", "G" ), ReferenceKinds.Invocation );

        var nodeCounts = result.Index.ReferencedSymbols
            .SelectMany( s => s.References )
            .ToDictionary( r => r.ReferencedSymbol.Name, r => r.Nodes.Count );

        Assert.Equal( 2, nodeCounts["F"] );
        Assert.Equal( 1, nodeCounts["G"] );
    }

    /// <summary>
    /// Verifies that the indexer of an element-access assignment is indexed as an assignment, and that its receiver is not.
    /// </summary>
    [Fact]
    public void ElementAccessAssignmentKinds()
    {
        var code = new Dictionary<string, string>()
        {
            ["C.cs"] = "class C { public int this[int i] { get => 0; set {} } }", ["B.cs"] = "class B { C c = new(); void M() { this.c[0] = 1; } }"
        };

        var indexerResult = this.BuildIndex(
            code,
            compilation => compilation.Types.OfName( "C" ).SelectMany( t => t.Indexers ),
            ReferenceKinds.Assignment );

        Assert.Equal( ["B.M()"], indexerResult.ReferencingSymbols );

        var fieldResult = this.BuildIndex(
            code,
            compilation => compilation.Types.OfName( "B" ).SelectMany( t => t.Fields.OfName( "c" ) ),
            ReferenceKinds.All );

        // The index also contains the indexer, whose identifier is not filtered, so the references of the field are selected by name.
        var fieldNodeKinds = fieldResult.Index.ReferencedSymbols
            .Where( s => s.ReferencedSymbol.Name == "c" )
            .SelectMany( s => s.References )
            .SelectMany( r => r.Nodes )
            .Select( n => n.ReferenceKind )
            .ToReadOnlyList();

        Assert.NotEmpty( fieldNodeKinds );
        Assert.DoesNotContain( ReferenceKinds.Assignment, fieldNodeKinds );
    }

    /// <summary>
    /// Verifies that an invocation of a classic extension method in reduced form is keyed by the static method, so that a validator of the
    /// static method sees it.
    /// </summary>
    [Fact]
    public void ReducedExtensionInvocationKeyedByStaticMethod()
    {
        var code = new Dictionary<string, string>()
        {
            ["A.cs"] = "static class A { public static int Twice( this int x ) => x * 2; }", ["B.cs"] = "class B { int M( int i ) => i.Twice(); }"
        };

        var result = this.BuildIndex( code, GetMethodsOfA( "Twice" ), ReferenceKinds.Invocation );

        Assert.Equal( ["B.M(int)"], result.ReferencingSymbols );
    }

    /// <summary>
    /// Verifies that every nested type of a record is visited when the walker does not descend into members. The walker used to stop after the
    /// first nested type.
    /// </summary>
    [Fact]
    public void NestedTypesOfRecordAllVisited()
    {
        var code = new Dictionary<string, string>() { ["A.cs"] = "class A;", ["R.cs"] = "record R { class N1 : A; class N2 : A; }" };

        var result = this.BuildIndex( code, compilation => compilation.Types.OfName( "A" ), ReferenceKinds.BaseType );

        Assert.Equal( ["R.N1", "R.N2"], result.ReferencingSymbols );
    }

    private static Func<ICompilation, IEnumerable<IDeclaration>> GetMethodsOfA( params string[] names )
        => compilation => compilation.Types.OfName( "A" ).SelectMany( t => t.Methods ).Where( m => names.Contains( m.Name ) );

    // TODO: other reference kinds.

#if ROSLYN_5_11_0_OR_GREATER && NET7_0_OR_GREATER

    // The union is a C# 15 feature, so only the latest Roslyn variant parses it. The test is further restricted to
    // .NET 7 and later, because the compiler emits CompilerFeatureRequiredAttribute on the members of a union and
    // .NET Framework does not declare that type.

    /// <summary>
    /// The declarations that the compiler requires of a union. No target framework declares them yet, and the
    /// compiler reports CS0656 when it cannot find them, so the compilation declares them.
    /// </summary>
    private const string _unionSupportCode = """
                                             #nullable enable

                                             namespace System.Runtime.CompilerServices
                                             {
                                                 [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct )]
                                                 public sealed class UnionAttribute : Attribute { }

                                                 public interface IUnion
                                                 {
                                                     object? Value { get; }
                                                 }
                                             }
                                             """;

    /// <summary>
    /// Verifies that the reference from a union declaration to one of its case types is indexed under
    /// <see cref="ReferenceKinds.UnionCaseType"/>, and that its origin is the union and not the case parameter, which
    /// declares no symbol. See finding PR-12 of issue #1946.
    /// </summary>
    [Fact]
    public void UnionCaseType()
    {
        var code = new Dictionary<string, string>
        {
            ["Support.cs"] = _unionSupportCode, ["A.cs"] = "record A;", ["B.cs"] = "record B;", ["C.cs"] = "record C;", ["U.cs"] = "union U( A, B );"
        };

        var result = this.BuildIndex( code, compilation => compilation.Types.OfName( "A" ), ReferenceKinds.UnionCaseType );

        Assert.Single( result.ReferencingSymbols, "U" );

        // A type that the union does not list is not referenced by it.
        var unreferenced = this.BuildIndex( code, compilation => compilation.Types.OfName( "C" ), ReferenceKinds.UnionCaseType );

        Assert.Empty( unreferenced.ReferencingSymbols );
    }

    /// <summary>
    /// Verifies that the case types of a union are not reported under <see cref="ReferenceKinds.ParameterType"/>,
    /// which is the kind that the parameter list of a primary constructor produces. The two kinds must stay apart, so
    /// that an architecture rule can require one without matching the other.
    /// </summary>
    [Fact]
    public void UnionCaseTypeIsNotAParameterType()
    {
        var code = new Dictionary<string, string>
        {
            ["Support.cs"] = _unionSupportCode, ["A.cs"] = "record A;", ["B.cs"] = "record B;", ["U.cs"] = "union U( A, B );"
        };

        var result = this.BuildIndex( code, compilation => compilation.Types.OfName( "A" ), ReferenceKinds.ParameterType );

        Assert.Empty( result.ReferencingSymbols );
    }

#endif

    private (InboundReferenceIndex Index, ReferenceIndexObserver Observer, IReadOnlyCollection<string> ReferencingSymbols ) BuildIndex(
        Dictionary<string, string> code,
        Func<ICompilation, IEnumerable<IDeclaration>> getDeclarations,
        ReferenceKinds referenceKinds,
        bool includeDerivedTypes = false,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary )
    {
        var observer = new ReferenceIndexObserver();
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddProjectService( observer );
        additionalServices.AddProjectService( SymbolClassificationService.CreateTestInstance() );
        using var testContext = this.CreateTestContext( additionalServices );
        var roslynCompilation = testContext.CreateCSharpCompilation( code, outputKind: outputKind );
        var compilation = testContext.CreateCompilationModel( roslynCompilation );

        List<ReferenceIndexerRequirements> validators = new();

        foreach ( var declaration in getDeclarations( compilation ) )
        {
            validators.Add( ReferenceIndexerRequirements.Create( declaration, referenceKinds, includeDerivedTypes ) );
        }

        var builder = new InboundReferenceIndexBuilder(
            testContext.ServiceProvider,
            new ReferenceIndexerOptions( validators ),
            SymbolEqualityComparer.Default );

        foreach ( var syntaxTree in compilation.PartialCompilation.SyntaxTreeCollection )
        {
            builder.IndexSyntaxTree( syntaxTree, compilation.CompilationContext.SemanticModelProvider );
        }

        var index = builder.ToReadOnly();

        var references = index.ReferencedSymbols.SelectMany( s => s.References )
            .Select( r => r.ReferencingSymbol.ToTestName() )
            .OrderBy( x => x )
            .ToReadOnlyList();

        return (index, observer, references);
    }
}