// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Aspects;

public sealed class IncrementalAspectRepositoryTests : UnitTestClass
{
    [Fact]
    public void HasAspect_TypeNotInPartialCompilation_ThrowsWithPartialCompilationMessage()
    {
        using var testContext = this.CreateTestContext();

        var code = new Dictionary<string, string>
        {
            ["ClassA.cs"] = "public class ClassA { }",
            ["ClassB.cs"] = "public class ClassB { }"
        };

        var compilation = testContext.CreateCSharpCompilation( code );

        // Create a partial compilation that only includes ClassA.
        var syntaxTreeA = compilation.SyntaxTrees.Single( t => t.FilePath == "ClassA.cs" );
        var partialCompilation = PartialCompilation.CreatePartial( compilation, syntaxTreeA );

        // Create a CompilationModel from the partial compilation.
        var project = new ProjectModel( compilation, testContext.ServiceProvider );
        var compilationModel = CompilationModel.CreateInitialInstance( project, partialCompilation );

        // Get ClassB — it's in the project but NOT in the partial compilation.
        var classBSymbol = compilation.GetTypeByMetadataName( "ClassB" )!;
        var classB = compilationModel.Factory.GetNamedType( classBSymbol );

        // Calling HasAspect on ClassB should throw because it's not in the partial compilation.
        var repository = compilationModel.AspectRepository;

        var ex = Assert.Throws<InvalidOperationException>( () => repository.HasAspect( classB, typeof(object) ) );

        // Verify the error message mentions "partial compilation" (not "current project").
        Assert.Contains( "partial compilation", ex.Message );
        Assert.Contains( "ICompilation.IsPartial", ex.Message );

        // Verify the error message does NOT suggest checking IsDesignTime (issue #755).
        Assert.DoesNotContain( "IsDesignTime", ex.Message );
    }

    [Fact]
    public void GetAspectInstances_TypeNotInPartialCompilation_ThrowsWithPartialCompilationMessage()
    {
        using var testContext = this.CreateTestContext();

        var code = new Dictionary<string, string>
        {
            ["ClassA.cs"] = "public class ClassA { }",
            ["ClassB.cs"] = "public class ClassB { }"
        };

        var compilation = testContext.CreateCSharpCompilation( code );

        var syntaxTreeA = compilation.SyntaxTrees.Single( t => t.FilePath == "ClassA.cs" );
        var partialCompilation = PartialCompilation.CreatePartial( compilation, syntaxTreeA );

        var project = new ProjectModel( compilation, testContext.ServiceProvider );
        var compilationModel = CompilationModel.CreateInitialInstance( project, partialCompilation );

        var classBSymbol = compilation.GetTypeByMetadataName( "ClassB" )!;
        var classB = compilationModel.Factory.GetNamedType( classBSymbol );

        var repository = compilationModel.AspectRepository;

        var ex = Assert.Throws<InvalidOperationException>( () => repository.GetAspectInstances( classB ) );

        Assert.Contains( "partial compilation", ex.Message );
        Assert.Contains( "ICompilation.IsPartial", ex.Message );
        Assert.DoesNotContain( "IsDesignTime", ex.Message );
    }

    [Fact]
    public void HasAspect_TypeInPartialCompilation_DoesNotThrow()
    {
        using var testContext = this.CreateTestContext();

        var code = new Dictionary<string, string>
        {
            ["ClassA.cs"] = "public class ClassA { }",
            ["ClassB.cs"] = "public class ClassB { }"
        };

        var compilation = testContext.CreateCSharpCompilation( code );

        var syntaxTreeA = compilation.SyntaxTrees.Single( t => t.FilePath == "ClassA.cs" );
        var partialCompilation = PartialCompilation.CreatePartial( compilation, syntaxTreeA );

        var project = new ProjectModel( compilation, testContext.ServiceProvider );
        var compilationModel = CompilationModel.CreateInitialInstance( project, partialCompilation );

        // ClassA IS in the partial compilation, so this should not throw.
        var classASymbol = compilation.GetTypeByMetadataName( "ClassA" )!;
        var classA = compilationModel.Factory.GetNamedType( classASymbol );

        var repository = compilationModel.AspectRepository;

        // Should not throw — ClassA is in the partial compilation.
        var result = repository.HasAspect( classA, typeof(object) );
        Assert.False( result );
    }
}
