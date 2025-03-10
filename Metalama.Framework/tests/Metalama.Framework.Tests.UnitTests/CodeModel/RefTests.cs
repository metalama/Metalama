// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Testing.UnitTesting;
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
}