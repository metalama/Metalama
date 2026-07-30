// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

public sealed class RefTests : UnitTestClass
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
    /// A durable reference is not bound to a compilation, but it still has to answer
    /// <see cref="Engine.CodeModel.References.RefExtensions.GetPrimarySyntaxTree(IRef, CompilationContext)"/> with the same tree the equivalent
    /// full reference gives (issue #1748).
    /// </summary>
    [Fact]
    public void GetPrimarySyntaxTreeOfDurableRefInSource()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "class C { }" );

        var fullRef = compilation.Types.OfName( "C" ).Single().ToRef();
        var durableRef = fullRef.ToDurable();

        Assert.False( durableRef is IFullRef, "The reference is expected not to be bound to a compilation." );

        var expected = fullRef.GetPrimarySyntaxTree( compilation.CompilationContext );
        Assert.NotNull( expected );

        Assert.Same( expected, durableRef.GetPrimarySyntaxTree( compilation.CompilationContext ) );
    }

    /// <summary>
    /// A durable reference to a declaration of a referenced assembly has no syntax tree in the current compilation, so
    /// <see cref="Engine.CodeModel.References.RefExtensions.GetPrimarySyntaxTree(IRef, CompilationContext)"/> returns <c>null</c> rather than
    /// throwing (issue #1748).
    /// </summary>
    [Fact]
    public void GetPrimarySyntaxTreeOfDurableRefInMetadata()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "/* nothing */" );

        var durableRef = compilation.Factory.GetTypeByReflectionType( typeof(string) ).ToRef().ToDurable();

        Assert.Null( durableRef.GetPrimarySyntaxTree( compilation.CompilationContext ) );
    }

    /// <summary>
    /// A durable reference whose id resolves to nothing in the current compilation, which happens when the referenced
    /// project changed since its manifest was written, must not throw either (issue #1748).
    /// </summary>
    [Fact]
    public void GetPrimarySyntaxTreeOfUnresolvableDurableRef()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "/* nothing */" );

        var durableRef = new DeclarationIdRef<INamedType>( new SerializableDeclarationId( "T:ThereIsNoSuchType" ) );

        Assert.Null( durableRef.GetPrimarySyntaxTree( compilation.CompilationContext ) );
    }
}