// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.SourceGeneration;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.EndToEnd;

public sealed class SourceGeneratorTests : UnitTestClass
{
    [Fact]
    public void RemovingTargetRemovesGenerated()
    {
        const string aspect = """
                              using Metalama.Framework.Aspects;

                              #pragma warning disable CS0169 // The field is never used

                              class Aspect : TypeAspect
                              {
                                  [Introduce]
                                  int i;
                              }
                              """;

        const string target = """
                              [Aspect]
                              partial class C;
                              """;

        const string usage = """
                             class D
                             {
                                 void M() => new C();
                             }
                             """;

        var code = new Dictionary<string, string> { ["aspect.cs"] = aspect, ["c.cs"] = target, ["d.cs"] = usage };

        using var testContext = this.CreateTestContext();

        var generator = new AnalysisProcessSourceGenerator( testContext.ServiceProvider.Global );
        GeneratorDriver generatorDriver = CSharpGeneratorDriver.Create( generator );

        var compilation = testContext.CreateCSharpCompilation( code, assemblyName: "test" );

        generatorDriver = generatorDriver.RunGeneratorsAndUpdateCompilation( compilation, out var outputCompilation, out var diagnostics );

        Assert.Empty( diagnostics );
        Assert.Empty( outputCompilation.GetDiagnostics() );

        code.Remove( "c.cs" );

        var updatedCompilation = testContext.CreateCSharpCompilation( code, assemblyName: "test", ignoreErrors: true );

        generatorDriver.RunGeneratorsAndUpdateCompilation( updatedCompilation, out outputCompilation, out diagnostics );

        Assert.Empty( diagnostics );
        var error = Assert.Single( outputCompilation.GetDiagnostics() );

        // d.cs(3,21): error CS0246: The type or namespace name 'C' could not be found (are you missing a using directive or an assembly reference?)
        Assert.Equal( "CS0246", error.Id );
    }
}