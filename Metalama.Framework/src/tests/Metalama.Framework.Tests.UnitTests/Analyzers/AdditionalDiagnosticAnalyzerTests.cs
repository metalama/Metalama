// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Analyzers;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Analyzers;

public class AdditionalDiagnosticAnalyzerTests : UnitTestClass
{
    [Fact]
    public void ImplementsDirectly()
    {
        const string code = """
                            using Metalama.Framework.Code;
                            class MyDisplayable : IDisplayable;
                            """;

        this.Test( code, 1 );
    }

    [Fact]
    public void ImplementsIndirectly()
    {
        // IRef<T> does not directly have the attribute, but it inherits from IRef,
        // which has the attribute. 
        const string code = """
                            using using Metalama.Framework.Code;
                            class MyRef : IRef<IType>;
                            """;

        this.Test( code, 1 );
    }

    [Fact]
    public void ImplementsUnmarkedAssembly()
    {
        const string code = """
                            using System;
                            class MyDisplayable : IDisposable;
                            """;

        this.Test( code, 0 );
    }

    [Fact]
    public void ImplementsInSameProject()
    {
        const string code = """
                            using System;
                            using Metalama.Framework.Utilities;

                            [InternalImplement]
                            interface ITheInterface;

                            class TheClass : ITheInterface;
                            """;

        this.Test( code, 0, false );
    }

    private void Test( string code, int expectedDiagnostics, bool ignoreErrors = true )
    {
        using var testContext = this.CreateTestContext();

        var analyzer = new AdditionalDiagnosticAnalyzer();

        var compilation = testContext.CreateCompilation( code, ignoreErrors: ignoreErrors );
        var type = compilation.Types.Single( t => t.TypeKind is TypeKind.Class ).GetSymbol()!;

        var analysisContext = new TestSymbolActionContext( type, compilation.GetRoslynCompilation() );
        analyzer.AnalyzeNamedTypeSymbol( analysisContext );

        Assert.Equal( expectedDiagnostics, analysisContext.Diagnostics.Count );
    }
}