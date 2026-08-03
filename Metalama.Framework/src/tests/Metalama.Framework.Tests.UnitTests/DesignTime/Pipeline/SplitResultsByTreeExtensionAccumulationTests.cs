// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Options;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

/// <summary>
/// Regression test for https://github.com/metalama/Metalama/issues/1796: a transitive pipeline contributor whose
/// syntax tree is not a part of the partial compilation the pipeline ran on used to be appended to the accumulated
/// <see cref="DesignTimeAspectPipelineResult.Extensions"/> collection, which nothing could ever remove it from.
/// </summary>
/// <remarks>
/// <para>
/// The design-time pipeline runs on the dirty trees only, and a contributor is not bound to that subset: an aspect
/// can export one onto a declaration it did not itself target, such as the declaring type of a base constructor for
/// <c>PullStrategy.IntroduceParameterAndPull</c>. When that declaration is in a tree that is not dirty, the path of
/// that tree is not a key of the result builders.
/// </para>
/// <para>
/// The contributor used to go to an <c>externalValidators</c> bucket that <c>Update</c> appended to the accumulated
/// collection on every run. The only removal path, <c>UnindexOldTree</c>, reaches contributors filed under a
/// <see cref="SyntaxTreePipelineResult"/>, and a contributor in that bucket was never filed under any, so the
/// collection gained an entry per run for the lifetime of the process. Each entry also indexes itself by validated
/// declaration, so a lookup returned one copy per run and the validation work grew with the length of the session.
/// </para>
/// <para>
/// The inheritable-aspect loop of the same method had the same defect and was fixed for issue #1768 by skipping the
/// item, because the tree keeps the result of the run that did include it and overwriting that result would drop the
/// diagnostics and introductions it holds. The fix applies the same treatment to contributors, which
/// <see cref="ContributorOnTreeOutsidePartialCompilationSurvivesThroughItsOwnTree"/> checks is lossless.
/// </para>
/// <para>
/// The tests drive <c>Update</c> directly rather than through an edit-and-rerun sequence, for the reason given in
/// <see cref="SplitResultsByTreePathLookupTests"/>: which trees the pipeline considers dirty is a function of the
/// change graph and not something a test can dictate.
/// </para>
/// </remarks>
public sealed class SplitResultsByTreeExtensionAccumulationTests : UnitTestClass
{
    public SplitResultsByTreeExtensionAccumulationTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

    private const string _baseCode = "public class BaseClass { }";

    private const string _targetCode = "public class TargetClass { }";

    /// <summary>
    /// A contributor whose syntax tree is outside the partial compilation, submitted once per run, as the pipeline
    /// does when the tree that declares its target stays clean while another tree is edited.
    /// </summary>
    /// <remarks>
    /// Before the fix, the accumulated collection grew by one entry per update. The assertion is made for two
    /// different numbers of updates so that what is tested is the absence of accumulation rather than a particular
    /// count.
    /// </remarks>
    [Theory]
    [InlineData( 3 )]
    [InlineData( 12 )]
    public void ContributorOnTreeOutsidePartialCompilation_DoesNotAccumulate( int updateCount )
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var (executed, compilation) = Execute( testContext, factory );

        var baseTree = compilation.SyntaxTrees.Single( t => t.FilePath == "base.cs" );
        var targetTree = compilation.SyntaxTrees.Single( t => t.FilePath == "target.cs" );

        // The subset the pipeline ran on: target.cs is dirty, base.cs is not.
        var partialCompilation = PartialCompilation.CreatePartial( compilation, targetTree );
        Assert.DoesNotContain( "base.cs", partialCompilation.SyntaxTrees.Keys );

        var result = executed.Result;

        for ( var i = 0; i < updateCount; i++ )
        {
            result = Update( result, compilation, partialCompilation, new TestContributor( baseTree ) );
        }

        this.TestOutput.WriteLine( $"After {updateCount} updates the collection holds {result.Extensions.Extensions.Length} extension(s)." );

        Assert.Empty( result.Extensions.Extensions );
    }

    /// <summary>
    /// Verifies that skipping the contributor loses nothing, because the result of its own tree, produced by a run
    /// that did include that tree, keeps it.
    /// </summary>
    [Fact]
    public void ContributorOnTreeOutsidePartialCompilationSurvivesThroughItsOwnTree()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var (executed, compilation) = Execute( testContext, factory );

        var baseTree = compilation.SyntaxTrees.Single( t => t.FilePath == "base.cs" );
        var targetTree = compilation.SyntaxTrees.Single( t => t.FilePath == "target.cs" );
        var contributor = new TestContributor( baseTree );

        // A run that includes base.cs files the contributor under that tree.
        var wholeCompilation = PartialCompilation.CreatePartial( compilation, new[] { baseTree, targetTree } );
        var afterFullRun = Update( executed.Result, compilation, wholeCompilation, contributor );

        Assert.Contains( contributor, afterFullRun.SyntaxTreeResults["base.cs"].Extensions );
        Assert.Contains( contributor, afterFullRun.Extensions.Extensions );

        // A later run in which only target.cs is dirty must leave that entry alone, and must not add a second one.
        var partialCompilation = PartialCompilation.CreatePartial( compilation, targetTree );
        var afterPartialRun = Update( afterFullRun, compilation, partialCompilation, contributor );

        Assert.Contains( contributor, afterPartialRun.SyntaxTreeResults["base.cs"].Extensions );
        Assert.Same( contributor, Assert.Single( afterPartialRun.Extensions.Extensions ) );
    }

    /// <summary>
    /// The control case: a contributor whose syntax tree is inside the partial compilation is filed under that tree,
    /// so repeated updates replace the entry rather than adding to it.
    /// </summary>
    [Fact]
    public void ContributorOnTreeInsidePartialCompilation_IsFiledUnderItsTree()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var (executed, compilation) = Execute( testContext, factory );

        var baseTree = compilation.SyntaxTrees.Single( t => t.FilePath == "base.cs" );
        var partialCompilation = PartialCompilation.CreatePartial( compilation, baseTree );

        var result = executed.Result;

        for ( var i = 0; i < 5; i++ )
        {
            result = Update( result, compilation, partialCompilation, new TestContributor( baseTree ) );
        }

        Assert.Single( result.Extensions.Extensions );
        Assert.Single( result.SyntaxTreeResults["base.cs"].Extensions );
    }

    /// <summary>
    /// Runs a full design-time execution, which supplies the real pipeline configuration that <c>Update</c> requires.
    /// </summary>
    private static (DesignTimeAspectPipelineResultAndState Executed, Compilation Compilation) Execute(
        TestContext testContext,
        TestDesignTimeAspectPipelineFactory factory )
    {
        var compilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string> { ["base.cs"] = _baseCode, ["target.cs"] = _targetCode } );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var executed ) );

        return (executed, compilation);
    }

    /// <summary>
    /// Files an execution result that carries <paramref name="contributor"/> and nothing else into the accumulated
    /// result, over the given partial compilation.
    /// </summary>
    private static DesignTimeAspectPipelineResult Update(
        DesignTimeAspectPipelineResult result,
        Compilation compilation,
        PartialCompilation partialCompilation,
        ITransitivePipelineContributor contributor )
    {
        var pipelineResults = new DesignTimePipelineExecutionResult(
            partialCompilation.SyntaxTrees,
            ImmutableArray<IntroducedSyntaxTree>.Empty,
            ImmutableUserDiagnosticList.Empty,
            ImmutableArray<InheritableAspectInstance>.Empty,
            ImmutableArray<KeyValuePair<HierarchicalOptionsKey, IHierarchicalOptions>>.Empty,
            ImmutableArray.Create( contributor ),
            ImmutableArray<IAspectInstance>.Empty,
            ImmutableArray<ITransformationBase>.Empty,
            ImmutableDictionaryOfArray<IRef<IDeclaration>, AnnotationInstance>.Empty );

        var projectVersion = new DesignTimeProjectVersion(
            new TestProjectVersion( compilation ),
            ImmutableArray<DesignTimeProjectReference>.Empty,
            DesignTimeAspectPipelineStatus.Default );

        return result.Update( partialCompilation, projectVersion, pipelineResults, result.Configuration.AssertNotNull() );
    }

    /// <summary>
    /// A minimal transitive contributor bound to a chosen syntax tree.
    /// </summary>
    /// <remarks>
    /// The production implementation, <c>TransitiveAspectInstance</c>, is internal to the engine and is produced only
    /// by the advice that exports an aspect onto the declaring type of a base constructor, which cannot be arranged
    /// while also pinning the partial compilation to a chosen subset. Only the syntax tree of the contributor matters
    /// to the code under test, which is what this stub supplies. The approach follows
    /// <see cref="TransitiveManifestValidatorChannelTests"/>.
    /// </remarks>
    private sealed class TestContributor : ITransitivePipelineContributor, IDesignTimePipelineResultExtension
    {
        public TestContributor( SyntaxTree syntaxTree )
        {
            this.SyntaxTree = syntaxTree;
        }

        public SyntaxTree? SyntaxTree { get; }

        public ContributorKind ContributorKind => ContributorKind.TransitiveAspectInstance;

        public IDesignTimePipelineResultExtension? ToDesignTime() => this;

        public ITransitiveAspectsManifestExtension ToTransitiveAspectManifestExtension() => throw new NotSupportedException();
    }
}
