// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.Engine;
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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

/// <summary>
/// Regression test for https://github.com/metalama/Metalama/issues/1768: <c>SplitResultsByTree</c> files each item of
/// a pipeline execution result under the file path of the syntax tree it belongs to, and the inheritable-aspect loop
/// reads that path from the result builders with the indexer. When the path is not there, the indexer throws
/// <see cref="KeyNotFoundException"/>, the exception travels out of the design-time pipeline, and the project loses
/// every diagnostic and every generated document.
/// </summary>
/// <remarks>
/// <para>
/// The path can legitimately be absent. The result builders are keyed by the syntax trees of the
/// <see cref="PartialCompilation"/> the pipeline just ran on, which is a subset of the project: at design time it
/// holds the dirty trees only. An inheritable aspect instance is not bound to that subset, because an aspect can put
/// one on a declaration it did not itself target, for instance through <c>RequireAspect</c> or through the transitive
/// instance that <c>PullStrategy.IntroduceParameterAndPull</c> exports onto the declaring type of the base
/// constructor (the producer in issue #1748, which crashed two lines above this one). When that declaration lives in
/// a tree that is not dirty, its path is not a key.
/// </para>
/// <para>
/// The neighbouring loops of the same method already treat this as ordinary: the introductions loop adds a builder
/// for the missing path with the comment "this happens when the source tree is not dirty, so it's not part of the
/// PartialCompilation", and the diagnostics, suppressions and extensions loops all probe with
/// <c>TryGetValue</c>. The inheritable-aspect loop was the only indexer read.
/// </para>
/// <para>
/// The test drives <c>Update</c> directly rather than through an edit-and-rerun sequence, because that is the only
/// way to pin the partial compilation to a chosen subset of trees: which trees the pipeline considers dirty is a
/// function of the change graph and not something a test can dictate. The aspect instance itself is real, produced
/// by a real pipeline execution, so only the pairing of the result with a narrower partial compilation is arranged.
/// This follows <see cref="TransitiveManifestValidatorChannelTests"/>, which fabricates an execution result the same
/// way.
/// </para>
/// </remarks>
public sealed class SplitResultsByTreePathLookupTests : UnitTestClass
{
    public SplitResultsByTreePathLookupTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

    private const string _aspectCode = """
                                       using Metalama.Framework.Aspects;

                                       [Inheritable]
                                       public class Aspect : TypeAspect { }
                                       """;

    private const string _baseCode = "[Aspect] public class BaseClass { }";

    private const string _targetCode = "public class TargetClass { }";

    /// <summary>
    /// An inheritable aspect instance targets a declaration in <c>base.cs</c>, while the pipeline ran on a partial
    /// compilation that holds <c>target.cs</c> only. Before the fix, filing the result throws
    /// <see cref="KeyNotFoundException"/>.
    /// </summary>
    [Fact]
    public void InheritableAspectOnTreeOutsidePartialCompilation()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var compilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string> { ["aspect.cs"] = _aspectCode, ["base.cs"] = _baseCode, ["target.cs"] = _targetCode } );

        // A full execution, which gives both a real pipeline configuration and a real inheritable aspect instance
        // targeting BaseClass, which is declared in base.cs.
        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var executed ) );

        var inheritableAspects = executed.Result.GetInheritableAspects( "Aspect" ).ToImmutableArray();
        var inheritableAspect = Assert.Single( inheritableAspects );

        // The subset the pipeline ran on: target.cs is dirty, base.cs is not.
        var targetTree = compilation.SyntaxTrees.Single( t => t.FilePath == "target.cs" );
        var partialCompilation = PartialCompilation.CreatePartial( compilation, targetTree );

        Assert.DoesNotContain( "base.cs", partialCompilation.SyntaxTrees.Keys );

        var pipelineResults = new DesignTimePipelineExecutionResult(
            partialCompilation.SyntaxTrees,
            ImmutableArray<IntroducedSyntaxTree>.Empty,
            ImmutableUserDiagnosticList.Empty,
            ImmutableArray.Create( inheritableAspect ),
            ImmutableArray<KeyValuePair<HierarchicalOptionsKey, IHierarchicalOptions>>.Empty,
            ImmutableArray<ITransitivePipelineContributor>.Empty,
            ImmutableArray<IAspectInstance>.Empty,
            ImmutableArray<ITransformationBase>.Empty,
            ImmutableDictionaryOfArray<IRef<IDeclaration>, AnnotationInstance>.Empty );

        var projectVersion = new DesignTimeProjectVersion(
            new TestProjectVersion( compilation ),
            ImmutableArray<DesignTimeProjectReference>.Empty,
            DesignTimeAspectPipelineStatus.Default );

        var updated = executed.Result.Update(
            partialCompilation,
            projectVersion,
            pipelineResults,
            executed.Result.Configuration.AssertNotNull() );

        // The aspect must survive the update: dropping it silently would make the consumer of the manifest lose the
        // inheritance, which is a different bug than the one being fixed.
        Assert.Contains( inheritableAspect, updated.GetInheritableAspects( "Aspect" ) );
    }
}
