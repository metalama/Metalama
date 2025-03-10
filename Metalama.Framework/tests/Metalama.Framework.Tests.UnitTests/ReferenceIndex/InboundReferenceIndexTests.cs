// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
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

    // TODO: other reference kinds.

    private (InboundReferenceIndex Index, ReferenceIndexObserver Observer, IReadOnlyCollection<string> ReferencingSymbols ) BuildIndex(
        Dictionary<string, string> code,
        Func<ICompilation, IEnumerable<IDeclaration>> getDeclarations,
        ReferenceKinds referenceKinds,
        bool includeDerivedTypes = false )
    {
        var observer = new ReferenceIndexObserver();
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddProjectService( observer );
        additionalServices.AddProjectService( SymbolClassificationService.CreateTestInstance() );
        using var testContext = this.CreateTestContext( additionalServices );
        var compilation = testContext.CreateCompilationModel( code );

        List<ReferenceIndexerRequirements> validators = new();

        foreach ( var declaration in getDeclarations( compilation ) )
        {
            validators.Add( ReferenceIndexerRequirements.Create( declaration, referenceKinds, includeDerivedTypes ) );
        }

        var builder = new InboundReferenceIndexBuilder(
            testContext.ServiceProvider,
            new ReferenceIndexerOptions( validators ),
            SymbolEqualityComparer.Default );

        foreach ( var syntaxTree in compilation.PartialCompilation.SyntaxTrees.Values )
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

internal static class SymbolFormatter
{
    public static string ToTestName( this ISymbol symbol )
        => symbol switch
        {
            IParameterSymbol p => p.ContainingSymbol.ToDisplayString( SymbolDisplayFormat.CSharpShortErrorMessageFormat ) + ":" + p.Name,
            _ => symbol.ToDisplayString( SymbolDisplayFormat.CSharpShortErrorMessageFormat )
        };
}