// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.DesignTime.Pipeline.Dependencies;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

public sealed class DependencyGraphTests : DesignTimeTestBase
{
    [Fact]
    public void AddOneTree()
    {
        var masterCompilation = ProjectKeyFactory.CreateTest( "MasterAssembly" );
        var dependencyCollector = new BaseDependencyCollector( new TestProjectVersion( masterCompilation ) );

        const ulong hash = 54;

        var masterDocumentKey = DocumentKey.FromPath( "master.cs" );
        var dependentDocumentKey = DocumentKey.FromPath( "dependent.cs" );

        dependencyCollector.AddSyntaxTreeDependency( dependentDocumentKey, masterCompilation, masterDocumentKey, hash );

        var graph = DependencyGraph.Create( dependencyCollector );

        var dependenciesByCompilation = graph.DependenciesByMasterProject.Values.Single();
        Assert.Equal( masterCompilation, dependenciesByCompilation.ProjectKey );
        var dependenciesByMasterFile = graph.DependenciesByMasterProject[masterCompilation].DependenciesByMasterDocumentKey.Single();
        Assert.Equal( masterDocumentKey, dependenciesByMasterFile.Key );
        Assert.Equal( hash, dependenciesByMasterFile.Value.DeclarationHash );
    }

    [Fact]
    public void AddTwoDependentTreesInSameCompilation()
    {
        var masterProject = ProjectKeyFactory.CreateTest( "MasterAssembly" );
        var dependencyCollector = new BaseDependencyCollector( new TestProjectVersion( masterProject ) );
        const ulong hash = 54;

        var masterDocumentKey = DocumentKey.FromPath( "master.cs" );
        var dependentDocumentKey1 = DocumentKey.FromPath( "dependent1.cs" );
        var dependentDocumentKey2 = DocumentKey.FromPath( "dependent2.cs" );

        dependencyCollector.AddSyntaxTreeDependency( dependentDocumentKey1, masterProject, masterDocumentKey, hash );
        dependencyCollector.AddSyntaxTreeDependency( dependentDocumentKey2, masterProject, masterDocumentKey, hash );

        var graph = DependencyGraph.Create( dependencyCollector );

        var dependenciesByProject = graph.DependenciesByMasterProject.Values.Single();
        Assert.Equal( masterProject, dependenciesByProject.ProjectKey );

        Assert.Contains(
            dependentDocumentKey1,
            graph.DependenciesByMasterProject[masterProject].DependenciesByMasterDocumentKey[masterDocumentKey].DependentDocumentKeys );

        Assert.Contains(
            dependentDocumentKey2,
            graph.DependenciesByMasterProject[masterProject].DependenciesByMasterDocumentKey[masterDocumentKey].DependentDocumentKeys );
    }

    [Fact]
    public void AddTwoDependentTreesInDifferentCompilation()
    {
        var masterCompilation1 = new TestProjectVersion( "MasterAssembly1" );
        var masterCompilation2 = new TestProjectVersion( "MasterAssembly2" );

        var dependentCompilation = new TestProjectVersion(
            "Dependent",
            referencedCompilations: new IProjectVersion[] { masterCompilation1, masterCompilation2 } );

        var dependencyCollector = new BaseDependencyCollector( dependentCompilation );

        const ulong hash = 54;

        var masterDocumentKey = DocumentKey.FromPath( "master.cs" );
        var dependentDocumentKey1 = DocumentKey.FromPath( "dependent1.cs" );
        var dependentDocumentKey2 = DocumentKey.FromPath( "dependent2.cs" );

        dependencyCollector.AddSyntaxTreeDependency( dependentDocumentKey1, masterCompilation1.ProjectKey, masterDocumentKey, hash );
        dependencyCollector.AddSyntaxTreeDependency( dependentDocumentKey2, masterCompilation2.ProjectKey, masterDocumentKey, hash );

        var graph = DependencyGraph.Create( dependencyCollector );

        Assert.Contains(
            dependentDocumentKey1,
            graph.DependenciesByMasterProject[masterCompilation1.ProjectKey].DependenciesByMasterDocumentKey[masterDocumentKey].DependentDocumentKeys );

        Assert.Contains(
            dependentDocumentKey2,
            graph.DependenciesByMasterProject[masterCompilation2.ProjectKey].DependenciesByMasterDocumentKey[masterDocumentKey].DependentDocumentKeys );
    }

    [Fact]
    public void AddOneTreeThenRemoveDependency()
    {
        const ulong hash = 54;

        var masterDocumentKey = DocumentKey.FromPath( "master.cs" );
        var dependentDocumentKey = DocumentKey.FromPath( "dependent.cs" );

        using var testContext = this.CreateTestContext();

        var masterCompilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string>() { [masterDocumentKey.Path] = "", [dependentDocumentKey.Path] = "" },
            assemblyName: "MasterAssembly" );

        var dependencies1 = new BaseDependencyCollector( new TestProjectVersion( masterCompilation ) );

        dependencies1.AddSyntaxTreeDependency( dependentDocumentKey, masterCompilation.GetProjectKey(), masterDocumentKey, hash );

        var graph1 = DependencyGraph.Create( dependencies1 );

        var dependencies2 = new BaseDependencyCollector( new TestProjectVersion( masterCompilation ) );
        var graph2 = graph1.Update( dependencies2 );

        Assert.Empty( graph2.DependenciesByMasterProject );
    }

    [Fact]
    public void AddTwoDependentTreesInSameCompilationThenRemoveOne()
    {
        const ulong hash = 54;

        var masterDocumentKey = DocumentKey.FromPath( "master.cs" );
        var dependentDocumentKey1 = DocumentKey.FromPath( "dependent1.cs" );
        var dependentDocumentKey2 = DocumentKey.FromPath( "dependent2.cs" );

        var masterCompilation = new TestProjectVersion( "MasterAssembly" );

        // Create a 1st version of the dependent assembly with two references to master.cs.
        var dependentCompilationVersion1 = new TestProjectVersion(
            ProjectKeyFactory.CreateTest( "DependentAssembly" ),
            hashes: new Dictionary<DocumentKey, ulong>() { [dependentDocumentKey1] = hash, [dependentDocumentKey2] = hash },
            referencedCompilations: new IProjectVersion[] { masterCompilation } );

        var dependencyCollector1 = new BaseDependencyCollector( dependentCompilationVersion1 );

        dependencyCollector1.AddSyntaxTreeDependency( dependentDocumentKey1, masterCompilation.ProjectKey, masterDocumentKey, hash );
        dependencyCollector1.AddSyntaxTreeDependency( dependentDocumentKey2, masterCompilation.ProjectKey, masterDocumentKey, hash );

        var graph1 = DependencyGraph.Create( dependencyCollector1 );

        // Create a 1st version of the dependent assembly with just 1 reference to master.cs.
        var dependencyCollector2 = new BaseDependencyCollector( dependentCompilationVersion1 );
        dependencyCollector2.AddSyntaxTreeDependency( dependentDocumentKey1, masterCompilation.ProjectKey, masterDocumentKey, hash );

        var graph2 = graph1
            .Update( dependencyCollector2 );

        var dependenciesByCompilation = graph2.DependenciesByMasterProject.Values.Single();
        Assert.Equal( masterCompilation.ProjectKey, dependenciesByCompilation.ProjectKey );

        Assert.Contains(
            dependentDocumentKey1,
            graph2.DependenciesByMasterProject[masterCompilation.ProjectKey].DependenciesByMasterDocumentKey[masterDocumentKey].DependentDocumentKeys );

        Assert.DoesNotContain(
            dependentDocumentKey2,
            graph2.DependenciesByMasterProject[masterCompilation.ProjectKey].DependenciesByMasterDocumentKey[masterDocumentKey].DependentDocumentKeys );
    }

    [Fact]
    public void UpdateSyntaxTreeHash()
    {
        var masterCompilation = ProjectKeyFactory.CreateTest( "MasterAssembly" );
        const ulong hash1 = 54;
        const ulong hash2 = 55;

        var masterDocumentKey = DocumentKey.FromPath( "master.cs" );
        var dependentDocumentKey = DocumentKey.FromPath( "dependent.cs" );

        var dependencies1 = new BaseDependencyCollector( new TestProjectVersion( "dummy" ) );
        dependencies1.AddSyntaxTreeDependency( dependentDocumentKey, masterCompilation, masterDocumentKey, hash1 );

        var graph1 = DependencyGraph.Create( dependencies1 );

        var dependencies2 = new BaseDependencyCollector( new TestProjectVersion( "dummy" ) );
        dependencies2.AddSyntaxTreeDependency( dependentDocumentKey, masterCompilation, masterDocumentKey, hash2 );

        var graph2 = graph1.Update( dependencies2 );

        Assert.Equal( hash2, graph2.DependenciesByMasterProject[masterCompilation].DependenciesByMasterDocumentKey[masterDocumentKey].DeclarationHash );
    }

    [Fact]
    public void RemoveDependency()
    {
        using var testContext = this.CreateTestContext();
        var masterCompilation = ProjectKeyFactory.CreateTest( "MasterAssembly" );
        const ulong hash1 = 54;

        var masterDocumentKey = DocumentKey.FromPath( "master.cs" );

        // We need two dependent files to make appear a bug in DependencyGraph.Builder.RemoveDependentSyntaxTree
        var dependentDocumentKey1 = DocumentKey.FromPath( "dependent1.cs" );
        var dependentDocumentKey2 = DocumentKey.FromPath( "dependent2.cs" );

        var compilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string> { [masterDocumentKey.Path] = "", [dependentDocumentKey1.Path] = "", [dependentDocumentKey2.Path] = "" } );

        var partialCompilation = PartialCompilation.CreateComplete( compilation );

        var dependencies1 = new BaseDependencyCollector( new TestProjectVersion( "dummy" ), partialCompilation );
        dependencies1.AddSyntaxTreeDependency( dependentDocumentKey1, masterCompilation, masterDocumentKey, hash1 );
        dependencies1.AddSyntaxTreeDependency( dependentDocumentKey2, masterCompilation, masterDocumentKey, hash1 );

        var graph1 = DependencyGraph.Create( dependencies1 );

        var dependencies2 = new BaseDependencyCollector( new TestProjectVersion( "dummy" ), partialCompilation );

        _ = graph1.Update( dependencies2 );
    }
}