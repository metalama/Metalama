// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
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

        var dependentDocumentKey = DocumentKey.FromPath( "dependent.cs" );
        var masterDocumentKey = DocumentKey.FromPath( "master.cs" );
        dependencies.AddSyntaxTreeDependency( dependentDocumentKey, projectKey, masterDocumentKey, hash );

        Assert.Equal( dependentDocumentKey, dependencies.DependenciesByDependentDocumentKey[dependentDocumentKey].DependentDocumentKey );

        Assert.Equal(
            hash,
            dependencies.DependenciesByDependentDocumentKey[dependentDocumentKey]
                .DependenciesByMasterProject.Values.Single()
                .MasterDocumentKeysAndHashes[masterDocumentKey] );
    }

    [Fact]
    public void AddDuplicateSyntaxTreeDependency()
    {
        var projectKey = ProjectKeyFactory.CreateTest( "DependentAssembly" );
        var dependencies = new BaseDependencyCollector( new TestProjectVersion( projectKey ) );

        const ulong hash = 54;

        var dependentDocumentKey = DocumentKey.FromPath( "dependent.cs" );
        var masterDocumentKey = DocumentKey.FromPath( "master.cs" );
        dependencies.AddSyntaxTreeDependency( dependentDocumentKey, projectKey, masterDocumentKey, hash );
        dependencies.AddSyntaxTreeDependency( dependentDocumentKey, projectKey, masterDocumentKey, hash );

        Assert.Equal( dependentDocumentKey, dependencies.DependenciesByDependentDocumentKey[dependentDocumentKey].DependentDocumentKey );

        Assert.Equal(
            hash,
            dependencies.DependenciesByDependentDocumentKey[dependentDocumentKey]
                .DependenciesByMasterProject.Values.Single()
                .MasterDocumentKeysAndHashes[masterDocumentKey] );
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
            dependencyCollector.EnumerateSyntaxTreeDependencies().Select( x => $"'{x.MasterDocumentKey}'->'{x.DependentDocumentKey}'" ).OrderBy( x => x ) );

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
            dependencyCollector.EnumerateSyntaxTreeDependencies().Select( x => $"'{x.MasterDocumentKey}'->'{x.DependentDocumentKey}'" ).OrderBy( x => x ) );

        const string expectedDependencies = @"'Class2.cs'->'Class3.cs'
'Class2.cs'->'Class4.cs'
'Class3.cs'->'Class4.cs'
'Interface1.cs'->'Class4.cs'
'Interface2.cs'->'Class4.cs'
'Interface3.cs'->'Class4.cs'";

        Assert.Equal( expectedDependencies, actualDependencies );
    }
}