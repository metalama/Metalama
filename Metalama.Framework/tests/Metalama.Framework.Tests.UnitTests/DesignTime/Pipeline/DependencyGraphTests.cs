// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
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

        const string masterFilePath = "master.cs";
        const string dependentFilePath = "dependent.cs";

        dependencyCollector.AddSyntaxTreeDependency( dependentFilePath, masterCompilation, masterFilePath, hash );

        var graph = DependencyGraph.Create( dependencyCollector );

        var dependenciesByCompilation = graph.DependenciesByMasterProject.Values.Single();
        Assert.Equal( masterCompilation, dependenciesByCompilation.ProjectKey );
        var dependenciesByMasterFile = graph.DependenciesByMasterProject[masterCompilation].DependenciesByMasterFilePath.Single();
        Assert.Equal( masterFilePath, dependenciesByMasterFile.Key );
        Assert.Equal( hash, dependenciesByMasterFile.Value.DeclarationHash );
    }

    [Fact]
    public void AddTwoDependentTreesInSameCompilation()
    {
        var masterProject = ProjectKeyFactory.CreateTest( "MasterAssembly" );
        var dependencyCollector = new BaseDependencyCollector( new TestProjectVersion( masterProject ) );
        const ulong hash = 54;

        const string masterFilePath = "master.cs";
        const string dependentFilePath1 = "dependent1.cs";
        const string dependentFilePath2 = "dependent2.cs";

        dependencyCollector.AddSyntaxTreeDependency( dependentFilePath1, masterProject, masterFilePath, hash );
        dependencyCollector.AddSyntaxTreeDependency( dependentFilePath2, masterProject, masterFilePath, hash );

        var graph = DependencyGraph.Create( dependencyCollector );

        var dependenciesByProject = graph.DependenciesByMasterProject.Values.Single();
        Assert.Equal( masterProject, dependenciesByProject.ProjectKey );

        Assert.Contains(
            dependentFilePath1,
            graph.DependenciesByMasterProject[masterProject].DependenciesByMasterFilePath[masterFilePath].DependentFilePaths );

        Assert.Contains(
            dependentFilePath2,
            graph.DependenciesByMasterProject[masterProject].DependenciesByMasterFilePath[masterFilePath].DependentFilePaths );
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

        const string masterFilePath = "master.cs";
        const string dependentFilePath1 = "dependent1.cs";
        const string dependentFilePath2 = "dependent2.cs";

        dependencyCollector.AddSyntaxTreeDependency( dependentFilePath1, masterCompilation1.ProjectKey, masterFilePath, hash );
        dependencyCollector.AddSyntaxTreeDependency( dependentFilePath2, masterCompilation2.ProjectKey, masterFilePath, hash );

        var graph = DependencyGraph.Create( dependencyCollector );

        Assert.Contains(
            dependentFilePath1,
            graph.DependenciesByMasterProject[masterCompilation1.ProjectKey].DependenciesByMasterFilePath[masterFilePath].DependentFilePaths );

        Assert.Contains(
            dependentFilePath2,
            graph.DependenciesByMasterProject[masterCompilation2.ProjectKey].DependenciesByMasterFilePath[masterFilePath].DependentFilePaths );
    }

    [Fact]
    public void AddOneTreeThenRemoveDependency()
    {
        const ulong hash = 54;

        const string masterFilePath = "master.cs";
        const string dependentFilePath = "dependent.cs";

        using var testContext = this.CreateTestContext();

        var masterCompilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string>() { [masterFilePath] = "", [dependentFilePath] = "" },
            assemblyName: "MasterAssembly" );

        var dependencies1 = new BaseDependencyCollector( new TestProjectVersion( masterCompilation ) );

        dependencies1.AddSyntaxTreeDependency( dependentFilePath, masterCompilation.GetProjectKey(), masterFilePath, hash );

        var graph1 = DependencyGraph.Create( dependencies1 );

        var dependencies2 = new BaseDependencyCollector( new TestProjectVersion( masterCompilation ) );
        var graph2 = graph1.Update( dependencies2 );

        Assert.Empty( graph2.DependenciesByMasterProject );
    }

    [Fact]
    public void AddTwoDependentTreesInSameCompilationThenRemoveOne()
    {
        const ulong hash = 54;

        const string masterFilePath = "master.cs";
        const string dependentFilePath1 = "dependent1.cs";
        const string dependentFilePath2 = "dependent2.cs";

        var masterCompilation = new TestProjectVersion( "MasterAssembly" );

        // Create a 1st version of the dependent assembly with two references to master.cs.
        var dependentCompilationVersion1 = new TestProjectVersion(
            ProjectKeyFactory.CreateTest( "DependentAssembly" ),
            hashes: new Dictionary<string, ulong>() { [dependentFilePath1] = hash, [dependentFilePath2] = hash },
            referencedCompilations: new IProjectVersion[] { masterCompilation } );

        var dependencyCollector1 = new BaseDependencyCollector( dependentCompilationVersion1 );

        dependencyCollector1.AddSyntaxTreeDependency( dependentFilePath1, masterCompilation.ProjectKey, masterFilePath, hash );
        dependencyCollector1.AddSyntaxTreeDependency( dependentFilePath2, masterCompilation.ProjectKey, masterFilePath, hash );

        var graph1 = DependencyGraph.Create( dependencyCollector1 );

        // Create a 1st version of the dependent assembly with just 1 reference to master.cs.
        var dependencyCollector2 = new BaseDependencyCollector( dependentCompilationVersion1 );
        dependencyCollector2.AddSyntaxTreeDependency( dependentFilePath1, masterCompilation.ProjectKey, masterFilePath, hash );

        var graph2 = graph1
            .Update( dependencyCollector2 );

        var dependenciesByCompilation = graph2.DependenciesByMasterProject.Values.Single();
        Assert.Equal( masterCompilation.ProjectKey, dependenciesByCompilation.ProjectKey );

        Assert.Contains(
            dependentFilePath1,
            graph2.DependenciesByMasterProject[masterCompilation.ProjectKey].DependenciesByMasterFilePath[masterFilePath].DependentFilePaths );

        Assert.DoesNotContain(
            dependentFilePath2,
            graph2.DependenciesByMasterProject[masterCompilation.ProjectKey].DependenciesByMasterFilePath[masterFilePath].DependentFilePaths );
    }

    [Fact]
    public void UpdateSyntaxTreeHash()
    {
        var masterCompilation = ProjectKeyFactory.CreateTest( "MasterAssembly" );
        const ulong hash1 = 54;
        const ulong hash2 = 55;

        const string masterFilePath = "master.cs";
        const string dependentFilePath = "dependent.cs";

        var dependencies1 = new BaseDependencyCollector( new TestProjectVersion( "dummy" ) );
        dependencies1.AddSyntaxTreeDependency( dependentFilePath, masterCompilation, masterFilePath, hash1 );

        var graph1 = DependencyGraph.Create( dependencies1 );

        var dependencies2 = new BaseDependencyCollector( new TestProjectVersion( "dummy" ) );
        dependencies2.AddSyntaxTreeDependency( dependentFilePath, masterCompilation, masterFilePath, hash2 );

        var graph2 = graph1.Update( dependencies2 );

        Assert.Equal( hash2, graph2.DependenciesByMasterProject[masterCompilation].DependenciesByMasterFilePath[masterFilePath].DeclarationHash );
    }

    [Fact]
    public void RemoveDependency()
    {
        using var testContext = this.CreateTestContext();
        var masterCompilation = ProjectKeyFactory.CreateTest( "MasterAssembly" );
        const ulong hash1 = 54;

        const string masterFilePath = "master.cs";

        // We need two dependent files to make appear a bug in DependencyGraph.Builder.RemoveDependentSyntaxTree
        const string dependentFilePath1 = "dependent1.cs";
        const string dependentFilePath2 = "dependent2.cs";

        var compilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string> { [masterFilePath] = "", [dependentFilePath1] = "", [dependentFilePath2] = "" } );

        var partialCompilation = PartialCompilation.CreateComplete( compilation );

        var dependencies1 = new BaseDependencyCollector( new TestProjectVersion( "dummy" ), partialCompilation );
        dependencies1.AddSyntaxTreeDependency( dependentFilePath1, masterCompilation, masterFilePath, hash1 );
        dependencies1.AddSyntaxTreeDependency( dependentFilePath2, masterCompilation, masterFilePath, hash1 );

        var graph1 = DependencyGraph.Create( dependencies1 );

        var dependencies2 = new BaseDependencyCollector( new TestProjectVersion( "dummy" ), partialCompilation );

        _ = graph1.Update( dependencies2 );
    }
}