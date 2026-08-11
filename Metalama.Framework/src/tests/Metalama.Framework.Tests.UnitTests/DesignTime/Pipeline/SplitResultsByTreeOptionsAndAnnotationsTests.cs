// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
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
using Metalama.Framework.Tests.UnitTestHelpers.Helpers;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

/// <summary>
/// Regression tests for https://github.com/metalama/Metalama/issues/1848: the inheritable-options loop and the
/// annotations loop of <c>SplitResultsByTree</c> read the result builders with the indexer, so an item whose syntax
/// tree is not a part of the partial compilation the pipeline ran on throws <see cref="KeyNotFoundException"/>. The
/// exception travels out of the design-time pipeline and the project loses every diagnostic and every generated
/// document.
/// </summary>
/// <remarks>
/// <para>
/// The result builders are keyed by the syntax trees of the <see cref="PartialCompilation"/> the pipeline just ran
/// on, which at design time holds the dirty trees only. Neither an inheritable option nor an annotation is bound to
/// that subset: a project fabric configures options on whatever declaration it selects, and an aspect annotates its
/// own target, both of which are ordinarily declared in a file other than the one being edited. When that file is
/// clean, its path is not a key.
/// </para>
/// <para>
/// This is the same defect as issues #1768 and #1796 on two further loops of the same method. Those were fixed by
/// skipping the item, because the tree keeps the result of the run that did include it and overwriting that result
/// would drop the diagnostics and introductions it holds. The tests below therefore assert both that the update
/// completes and that nothing is lost.
/// </para>
/// <para>
/// The tests drive <c>Update</c> directly rather than through an edit-and-rerun sequence, for the reason given in
/// <see cref="SplitResultsByTreePathLookupTests"/>: which trees the pipeline considers dirty is a function of the
/// change graph and not something a test can dictate. The inheritable options are real, produced by a real pipeline
/// execution over a project fabric of the shape reported in the issue, so only the pairing of the result with a
/// narrower partial compilation is arranged.
/// </para>
/// </remarks>
public sealed class SplitResultsByTreeOptionsAndAnnotationsTests : UnitTestClass
{
    public SplitResultsByTreeOptionsAndAnnotationsTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

    /// <summary>
    /// A project fabric that configures hierarchical options on a type declared in another file, which is the shape
    /// reported in issue #1848.
    /// </summary>
    private const string _fabricCode = """
                                       using Metalama.Framework.Fabrics;
                                       using Metalama.Framework.Options;
                                       using System.Linq;

                                       internal class Fabric : ProjectFabric
                                       {
                                           public override void AmendProject( IProjectAmender amender )
                                           {
                                               amender.Select( c => c.Types.OfName( "ExternalClass" ).Single() )
                                                   .SetOptions( c => new MyOptions { Value = "THE_VALUE" } );
                                           }
                                       }
                                       """;

    /// <summary>
    /// The file that carries the configured type, standing for <c>UsingFabricOnExternalClass.Dependency.cs</c> of
    /// the report.
    /// </summary>
    private const string _dependencyCode = "public class ExternalClass { }";

    /// <summary>
    /// The file being edited, which is the only one the partial compilation holds.
    /// </summary>
    private const string _targetCode = "public class TargetClass { }";

    /// <summary>
    /// An inheritable option is exported for a declaration of <c>dependency.cs</c>, while the pipeline ran on a
    /// partial compilation that holds <c>target.cs</c> only. Before the fix, filing the result throws
    /// <see cref="KeyNotFoundException"/>.
    /// </summary>
    [Fact]
    public void InheritableOptionsOnTreeOutsidePartialCompilation()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var (executed, compilation) = Execute( testContext, factory );

        var options = GetInheritableOptions( executed );
        Assert.NotEmpty( options );

        // The subset the pipeline ran on: target.cs is dirty, dependency.cs is not.
        var partialCompilation = CreatePartialCompilation( compilation, "target.cs" );
        Assert.False( partialCompilation.TryGetSyntaxTree( DocumentKey.FromPath( "dependency.cs" ), out _ ) );

        var updated = Update( executed.Result, compilation, partialCompilation, inheritableOptions: options );

        // The options must survive the update. They are carried by the result of dependency.cs, which the run that
        // did include that tree produced and which this update must leave alone: overwriting it with a result
        // holding the options and nothing else would drop the diagnostics and introductions of a clean file.
        var dependencyResult = updated.SyntaxTreeResults[DocumentKey.FromPath( "dependency.cs" )];
        Assert.NotEmpty( dependencyResult.InheritableOptions );

        AssertEx.EolInvariantEqual(
            DumpSyntaxTreeResult( executed.Result.SyntaxTreeResults[DocumentKey.FromPath( "dependency.cs" )] ),
            DumpSyntaxTreeResult( dependencyResult ) );
    }

    /// <summary>
    /// The control case: the configured declaration is in the partial compilation, so its options are filed under
    /// its own tree as before. Skipping them here would lose the configuration for consumers.
    /// </summary>
    [Fact]
    public void InheritableOptionsOnTreeInsidePartialCompilation()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var (executed, compilation) = Execute( testContext, factory );

        var options = GetInheritableOptions( executed );
        Assert.NotEmpty( options );

        var partialCompilation = CreatePartialCompilation( compilation, "dependency.cs" );
        Assert.True( partialCompilation.TryGetSyntaxTree( DocumentKey.FromPath( "dependency.cs" ), out _ ) );

        var updated = Update( executed.Result, compilation, partialCompilation, inheritableOptions: options );

        Assert.NotEmpty( updated.SyntaxTreeResults[DocumentKey.FromPath( "dependency.cs" )].InheritableOptions );
    }

    /// <summary>
    /// An annotation targets a declaration of <c>dependency.cs</c>, while the pipeline ran on a partial compilation
    /// that holds <c>target.cs</c> only. This is the second unguarded indexer read of the same method.
    /// </summary>
    [Fact]
    public void AnnotationOnTreeOutsidePartialCompilation()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var (executed, compilation) = Execute( testContext, factory );

        var annotations = CreateAnnotations( testContext, compilation );

        var partialCompilation = CreatePartialCompilation( compilation, "target.cs" );
        Assert.False( partialCompilation.TryGetSyntaxTree( DocumentKey.FromPath( "dependency.cs" ), out _ ) );

        var updated = Update( executed.Result, compilation, partialCompilation, annotations: annotations );

        AssertEx.EolInvariantEqual(
            DumpSyntaxTreeResult( executed.Result.SyntaxTreeResults[DocumentKey.FromPath( "dependency.cs" )] ),
            DumpSyntaxTreeResult( updated.SyntaxTreeResults[DocumentKey.FromPath( "dependency.cs" )] ) );
    }

    /// <summary>
    /// The control case: the annotated declaration is in the partial compilation, so the annotation is filed under
    /// its own tree as before.
    /// </summary>
    [Fact]
    public void AnnotationOnTreeInsidePartialCompilation()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var (executed, compilation) = Execute( testContext, factory );

        var annotations = CreateAnnotations( testContext, compilation );

        var partialCompilation = CreatePartialCompilation( compilation, "dependency.cs" );

        var updated = Update( executed.Result, compilation, partialCompilation, annotations: annotations );

        Assert.NotEmpty( updated.SyntaxTreeResults[DocumentKey.FromPath( "dependency.cs" )].Annotations );
    }

    /// <summary>
    /// Runs a full design-time execution over a project whose fabric configures options on <c>ExternalClass</c>,
    /// declared in <c>dependency.cs</c>.
    /// </summary>
    private static (DesignTimeAspectPipelineResultAndState Executed, Compilation Compilation) Execute(
        TestContext testContext,
        TestDesignTimeAspectPipelineFactory factory )
    {
        var compilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string>
            {
                ["options.cs"] = OptionsTestHelper.OptionsCode,
                ["fabric.cs"] = _fabricCode,
                ["dependency.cs"] = _dependencyCode,
                ["target.cs"] = _targetCode
            } );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var executed ) );

        return (executed, compilation);
    }

    /// <summary>
    /// Returns, in the form in which the pipeline submits them, the inheritable options that the execution exported
    /// for the declaration of <c>dependency.cs</c>.
    /// </summary>
    private static ImmutableArray<KeyValuePair<HierarchicalOptionsKey, IHierarchicalOptions>> GetInheritableOptions(
        DesignTimeAspectPipelineResultAndState executed )
        => executed.Result.SyntaxTreeResults[DocumentKey.FromPath( "dependency.cs" )]
            .InheritableOptions
            .SelectAsImmutableArray( o => new KeyValuePair<HierarchicalOptionsKey, IHierarchicalOptions>( o.Key, o.Options ) );

    /// <summary>
    /// Creates an exported annotation on <c>ExternalClass</c>, which is declared in <c>dependency.cs</c>.
    /// </summary>
    private static ImmutableDictionaryOfArray<IRef<IDeclaration>, AnnotationInstance> CreateAnnotations(
        TestContext testContext,
        Compilation compilation )
    {
        var model = testContext.CreateCompilationModel( compilation );
        var externalClass = model.Types.OfName( "ExternalClass" ).Single();

        var annotation = new AnnotationInstance( new TestAnnotation(), true, externalClass.ToRef() );

        return ImmutableDictionaryOfArray<IRef<IDeclaration>, AnnotationInstance>.Create(
            ImmutableArray.Create( annotation ),
            a => a.TargetDeclaration,
            RefEqualityComparer<IDeclaration>.Default );
    }

    /// <summary>
    /// Creates a partial compilation holding the single named syntax tree of <paramref name="compilation"/>.
    /// </summary>
    private static PartialCompilation CreatePartialCompilation( Compilation compilation, string filePath )
        => PartialCompilation.CreatePartial( compilation, compilation.SyntaxTrees.Single( t => t.FilePath == filePath ) );

    /// <summary>
    /// Files an execution result that carries the given options and annotations, and nothing else, into the
    /// accumulated result, over the given partial compilation.
    /// </summary>
    private static DesignTimeAspectPipelineResult Update(
        DesignTimeAspectPipelineResult result,
        Compilation compilation,
        PartialCompilation partialCompilation,
        ImmutableArray<KeyValuePair<HierarchicalOptionsKey, IHierarchicalOptions>> inheritableOptions = default,
        ImmutableDictionaryOfArray<IRef<IDeclaration>, AnnotationInstance>? annotations = null )
    {
        var pipelineResults = new DesignTimePipelineExecutionResult(
            partialCompilation.SyntaxTreeCollection,
            ImmutableArray<IntroducedSyntaxTree>.Empty,
            ImmutableUserDiagnosticList.Empty,
            ImmutableArray<InheritableAspectInstance>.Empty,
            inheritableOptions.IsDefault
                ? ImmutableArray<KeyValuePair<HierarchicalOptionsKey, IHierarchicalOptions>>.Empty
                : inheritableOptions,
            ImmutableArray<ITransitivePipelineContributor>.Empty,
            ImmutableArray<IAspectInstance>.Empty,
            ImmutableArray<ITransformationBase>.Empty,
            annotations ?? ImmutableDictionaryOfArray<IRef<IDeclaration>, AnnotationInstance>.Empty );

        var projectVersion = new DesignTimeProjectVersion(
            new TestProjectVersion( compilation ),
            ImmutableArray<DesignTimeProjectReference>.Empty,
            DesignTimeAspectPipelineStatus.Default );

        return result.Update( partialCompilation, projectVersion, pipelineResults, result.Configuration.AssertNotNull() );
    }

    /// <summary>
    /// Renders the parts of a <see cref="SyntaxTreePipelineResult"/> that an update of another tree must not disturb.
    /// </summary>
    private static string DumpSyntaxTreeResult( SyntaxTreePipelineResult result )
        => string.Join(
            "\n",
            $"path: {result.SyntaxTreePath}",
            $"diagnostics: {string.Join( ", ", result.Diagnostics.Select( d => d.ToString() ) )}",
            $"suppressions: {string.Join( ", ", result.Suppressions.Select( s => s.ToString() ) )}",
            $"introductions: {string.Join( ", ", result.Introductions.Select( i => i.Name ) )}",
            $"inheritable aspects: {string.Join( ", ", result.InheritableAspects.Select( a => a.ToString() ) )}",
            $"inheritable options: {string.Join( ", ", result.InheritableOptions.Select( o => $"{o.Key}={o.Options}" ) )}",
            $"annotations: {string.Join( ", ", result.Annotations.SelectMany( g => g.Select( a => a.ToString() ) ) )}" );

    /// <summary>
    /// A minimal annotation, standing in for one that an aspect exports onto its target.
    /// </summary>
    /// <remarks>
    /// Only the syntax tree of the annotated declaration matters to the code under test, and that declaration is a
    /// real one taken from the compilation. The approach follows the stub contributor of
    /// <see cref="SplitResultsByTreeExtensionAccumulationTests"/>.
    /// </remarks>
    private sealed class TestAnnotation : IAnnotation<INamedType>;
}
