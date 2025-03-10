// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime;
using Metalama.Framework.DesignTime.Pipeline.Dependencies;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

public sealed partial class DependencyCollectorTests
{
    [Fact]
    public void AddOneSyntaxTreeDependency()
    {
        var projectKey = ProjectKeyFactory.CreateTest( "DependentAssembly" );
        var dependencies = new BaseDependencyCollector( new TestProjectVersion( projectKey ) );
        const ulong hash = 54;

        const string dependentFilePath = "dependent.cs";
        const string masterFilePath = "master.cs";
        dependencies.AddSyntaxTreeDependency( dependentFilePath, projectKey, masterFilePath, hash );

        Assert.Equal( dependentFilePath, dependencies.DependenciesByDependentFilePath[dependentFilePath].DependentFilePath );

        Assert.Equal(
            hash,
            dependencies.DependenciesByDependentFilePath[dependentFilePath]
                .DependenciesByMasterProject.Values.Single()
                .MasterFilePathsAndHashes[masterFilePath] );
    }

    [Fact]
    public void AddDuplicateSyntaxTreeDependency()
    {
        var projectKey = ProjectKeyFactory.CreateTest( "DependentAssembly" );
        var dependencies = new BaseDependencyCollector( new TestProjectVersion( projectKey ) );

        const ulong hash = 54;

        const string dependentFilePath = "dependent.cs";
        const string masterFilePath = "master.cs";
        dependencies.AddSyntaxTreeDependency( dependentFilePath, projectKey, masterFilePath, hash );
        dependencies.AddSyntaxTreeDependency( dependentFilePath, projectKey, masterFilePath, hash );

        Assert.Equal( dependentFilePath, dependencies.DependenciesByDependentFilePath[dependentFilePath].DependentFilePath );

        Assert.Equal(
            hash,
            dependencies.DependenciesByDependentFilePath[dependentFilePath]
                .DependenciesByMasterProject.Values.Single()
                .MasterFilePathsAndHashes[masterFilePath] );
    }

    [Fact]
    public void CollectSyntaxTreeDependenciesWithinProject()
    {
        using var testContext = this.CreateTestContext();

        var code = new Dictionary<string, string>
        {
            ["Class1.cs"] = "public class Class1 { }",
            ["Class2.cs"] = "public class Class2 { }",
            ["Class3.cs"] = "public class Class3 : Class2 { }",
            ["Interface1.cs"] = "public interface Interface1 { }",
            ["Interface2.cs"] = "public interface Interface2 : Interface1 { }",
            ["Interface3.cs"] = "public interface Interface3 : Interface2 { }",
            ["Class4.cs"] = "public class Class4 : Class3, Interface3 { }"
        };

        var compilation = testContext.CreateCSharpCompilation( code );

        var dependencyCollector = new DependencyCollector( testContext.ServiceProvider, new TestProjectVersion( compilation ) );

        var partialCompilation = PartialCompilation.CreatePartial( compilation, compilation.SyntaxTrees );
        partialCompilation.DerivedTypes.PopulateDependencies( dependencyCollector );

        var actualDependencies = string.Join(
            Environment.NewLine,
            dependencyCollector.EnumerateSyntaxTreeDependencies().Select( x => $"'{x.MasterFilePath}'->'{x.DependentFilePath}'" ).OrderBy( x => x ) );

        const string expectedDependencies = @"'Class2.cs'->'Class3.cs'
'Class2.cs'->'Class4.cs'
'Class3.cs'->'Class4.cs'
'Interface1.cs'->'Class4.cs'
'Interface1.cs'->'Interface2.cs'
'Interface1.cs'->'Interface3.cs'
'Interface2.cs'->'Class4.cs'
'Interface2.cs'->'Interface3.cs'
'Interface3.cs'->'Class4.cs'";

        AssertEx.EolInvariantEqual( expectedDependencies, actualDependencies );
    }

    [Fact]
    public void CollectSyntaxTreeDependenciesAcrossProject()
    {
        using var testContext = this.CreateTestContext();

        var code1 = new Dictionary<string, string>
        {
            ["Interface1.cs"] = "public interface Interface1 { }",
            ["Interface2.cs"] = "public interface Interface2 : Interface1 { }",
            ["Interface3.cs"] = "public interface Interface3 : Interface2 { }"
        };

        var compilation1 = testContext.CreateCSharpCompilation( code1 );

        var code2 = new Dictionary<string, string>
        {
            ["Class1.cs"] = "public class Class1 { }",
            ["Class2.cs"] = "public class Class2 { }",
            ["Class3.cs"] = "public class Class3 : Class2 { }",
            ["Class4.cs"] = "public class Class4 : Class3, Interface3 { }"
        };

        var compilation2 = testContext.CreateCSharpCompilation( code2, additionalReferences: new[] { compilation1.ToMetadataReference() } );

        var dependencyCollector = new DependencyCollector(
            testContext.ServiceProvider,
            new TestProjectVersion( compilation2 ) );

        var partialCompilation = PartialCompilation.CreatePartial( compilation2, compilation2.SyntaxTrees );
        partialCompilation.DerivedTypes.PopulateDependencies( dependencyCollector );

        var actualDependencies = string.Join(
            "\r\n",
            dependencyCollector.EnumerateSyntaxTreeDependencies().Select( x => $"'{x.MasterFilePath}'->'{x.DependentFilePath}'" ).OrderBy( x => x ) );

        const string expectedDependencies = @"'Class2.cs'->'Class3.cs'
'Class2.cs'->'Class4.cs'
'Class3.cs'->'Class4.cs'
'Interface1.cs'->'Class4.cs'
'Interface2.cs'->'Class4.cs'
'Interface3.cs'->'Class4.cs'";

        Assert.Equal( expectedDependencies, actualDependencies );
    }
}