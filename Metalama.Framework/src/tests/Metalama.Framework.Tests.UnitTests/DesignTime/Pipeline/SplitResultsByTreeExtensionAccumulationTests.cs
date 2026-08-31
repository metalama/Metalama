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
        Assert.False( partialCompilation.TryGetSyntaxTree( DocumentKey.FromPath( "base.cs" ), out _ ) );

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

        Assert.Contains( contributor, afterFullRun.SyntaxTreeResults[DocumentKey.FromPath( "base.cs" )].Extensions );
        Assert.Contains( contributor, afterFullRun.Extensions.Extensions );

        // A later run in which only target.cs is dirty must leave that entry alone, and must not add a second one.
        var partialCompilation = PartialCompilation.CreatePartial( compilation, targetTree );
        var afterPartialRun = Update( afterFullRun, compilation, partialCompilation, contributor );

        Assert.Contains( contributor, afterPartialRun.SyntaxTreeResults[DocumentKey.FromPath( "base.cs" )].Extensions );
        Assert.Same( contributor, Assert.Single( afterPartialRun.Extensions.Extensions ) );
    }

    /// <summary>
    /// A contributor whose syntax tree belongs to a <em>different</em> compilation must be kept, because this project
    /// has no result keyed by that path and never will.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the case the deleted comment named "cross-project validators". A fabric in this project can validate
    /// references to declarations in a referenced project, which is a core scenario of
    /// <c>Metalama.Extensions.Architecture</c>. The syntax tree that a reference validator reports is that of the
    /// validated declaration, not of the code that declared the validator, and at design time an in-solution project
    /// reference is a <see cref="Microsoft.CodeAnalysis.CompilationReference"/>, so the symbol does have a syntax
    /// tree and that tree belongs to the other project's compilation.
    /// </para>
    /// <para>
    /// Skipping such a contributor, which is correct for a tree of this project that is merely not dirty, loses it
    /// altogether here: no <see cref="SyntaxTreePipelineResult"/> of this project is keyed by that path, so nothing
    /// carries it. In <c>Metalama.Premium</c> the observable effect is that
    /// <c>SideBySideVersionTests.TransitiveValidator</c> reports no diagnostic at all, that is, the architecture rules
    /// of a project silently stop being enforced. This test reproduces the same condition without Premium, so that
    /// this repository is self-contained.
    /// </para>
    /// </remarks>
    [Fact]
    public void ContributorOnTreeOfAnotherCompilation_IsRetained()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var (executed, compilation) = Execute( testContext, factory );

        var foreignTree = CreateForeignTree( testContext );
        var targetTree = compilation.SyntaxTrees.Single( t => t.FilePath == "target.cs" );
        var partialCompilation = PartialCompilation.CreatePartial( compilation, targetTree );

        Assert.DoesNotContain( foreignTree, compilation.SyntaxTrees );

        var contributor = new TestContributor( foreignTree );
        var updated = Update( executed.Result, compilation, partialCompilation, contributor );

        Assert.Contains( contributor, updated.Extensions.Extensions );
    }

    /// <summary>
    /// The contributor of another compilation must be kept, but exactly once, however many times the pipeline runs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Together with <see cref="ContributorOnTreeOfAnotherCompilation_IsRetained"/> this pins the whole of the
    /// required behaviour, and the two are what distinguish a correct fix from either of the two wrong ones. Appending
    /// the contributor keeps it but grows the collection by one entry per run, which is the defect of issue #1796.
    /// Skipping it bounds the collection but drops the contributor, which is the regression described above.
    /// </para>
    /// <para>
    /// A distinct instance is submitted on every update, because that is what the pipeline does: it re-materializes
    /// every reference validator on every run. In <c>Metalama.Premium</c>,
    /// <c>ValidationPipelineExtension.ExecuteDesignTimePipelineContributorsAsync</c> runs every validator source over
    /// the whole compilation, so the complete set is produced each time and replacing the previous set loses nothing.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData( 3 )]
    [InlineData( 12 )]
    public void ContributorOnTreeOfAnotherCompilation_DoesNotAccumulate( int updateCount )
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var (executed, compilation) = Execute( testContext, factory );

        var foreignTree = CreateForeignTree( testContext );
        var targetTree = compilation.SyntaxTrees.Single( t => t.FilePath == "target.cs" );
        var partialCompilation = PartialCompilation.CreatePartial( compilation, targetTree );

        var result = executed.Result;

        for ( var i = 0; i < updateCount; i++ )
        {
            result = Update( result, compilation, partialCompilation, new TestContributor( foreignTree ) );
        }

        this.TestOutput.WriteLine( $"After {updateCount} updates the collection holds {result.Extensions.Extensions.Length} extension(s)." );

        Assert.Single( result.Extensions.Extensions );
    }

    /// <summary>
    /// A run that carries no contributor at all must not discard the contributors of another compilation that earlier
    /// runs established.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The set of foreign contributors is replaced on each run rather than appended to, which is correct only for a
    /// run that actually re-produced them. A run can carry none for reasons that say nothing about whether the rules
    /// still apply: <c>DesignTimePipelineStage</c> invokes the extensions only when the run produced at least one
    /// contributor of extension kind, so a validator source registered by an aspect contributes nothing on a run in
    /// which that aspect's file is clean, and a run whose pipeline failed carries none either, because
    /// <c>PipelineState</c> reads <c>TransitiveContributors</c> from a result that is then null.
    /// </para>
    /// <para>
    /// Replacing unconditionally in that situation loses the rules of a referenced project silently, which is the
    /// regression that <see cref="ContributorOnTreeOfAnotherCompilation_IsRetained"/> exists to prevent, reached
    /// through a different door.
    /// </para>
    /// </remarks>
    [Fact]
    public void RunWithoutContributors_KeepsTheContributorsOfAnotherCompilation()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var (executed, compilation) = Execute( testContext, factory );

        var foreignTree = CreateForeignTree( testContext );
        var targetTree = compilation.SyntaxTrees.Single( t => t.FilePath == "target.cs" );
        var partialCompilation = PartialCompilation.CreatePartial( compilation, targetTree );

        var contributor = new TestContributor( foreignTree );
        var afterFirstRun = Update( executed.Result, compilation, partialCompilation, contributor );

        Assert.Contains( contributor, afterFirstRun.Extensions.Extensions );

        // The second run produces nothing, as happens when no source of extension contributors ran.
        var afterEmptyRun = Update(
            afterFirstRun,
            compilation,
            partialCompilation,
            ImmutableArray<ITransitivePipelineContributor>.Empty );

        Assert.Contains( contributor, afterEmptyRun.Extensions.Extensions );
    }

    /// <summary>
    /// Creates a syntax tree that belongs to another compilation, standing in for a declaration of a referenced
    /// project.
    /// </summary>
    private static SyntaxTree CreateForeignTree( TestContext testContext )
    {
        var referencedCompilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string> { ["referenced.cs"] = "public class ReferencedClass { }" },
            assemblyName: "ReferencedProject" );

        return referencedCompilation.SyntaxTrees.Single( t => t.FilePath == "referenced.cs" );
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
        Assert.Single( result.SyntaxTreeResults[DocumentKey.FromPath( "base.cs" )].Extensions );
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
        => Update( result, compilation, partialCompilation, ImmutableArray.Create( contributor ) );

    /// <summary>
    /// Files an execution result carrying the given contributors, which may be none, into the accumulated result.
    /// </summary>
    private static DesignTimeAspectPipelineResult Update(
        DesignTimeAspectPipelineResult result,
        Compilation compilation,
        PartialCompilation partialCompilation,
        ImmutableArray<ITransitivePipelineContributor> contributors )
    {
        var pipelineResults = new DesignTimePipelineExecutionResult(
            partialCompilation.SyntaxTreeCollection,
            ImmutableArray<IntroducedSyntaxTree>.Empty,
            ImmutableUserDiagnosticList.Empty,
            ImmutableArray<InheritableAspectInstance>.Empty,
            ImmutableArray<KeyValuePair<HierarchicalOptionsKey, IHierarchicalOptions>>.Empty,
            contributors,
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
            this.DocumentKey = syntaxTree.GetDocumentKey();
        }

        public DocumentKey DocumentKey { get; }

        public ContributorKind ContributorKind => ContributorKind.TransitiveAspectInstance;

        public IDesignTimePipelineResultExtension? ToDesignTime() => this;

        public ITransitiveAspectsManifestExtension ToTransitiveAspectManifestExtension() => throw new NotSupportedException();
    }
}
