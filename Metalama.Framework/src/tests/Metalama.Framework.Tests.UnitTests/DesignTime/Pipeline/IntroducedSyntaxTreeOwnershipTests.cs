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
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

/// <summary>
/// Covers the third site named by https://github.com/metalama/Metalama/issues/1742,
/// <c>DesignTimeAspectPipelineResult.Update</c>, which throws
/// <see cref="AssertionFailedException"/> when the same introduced syntax tree name arrives twice.
/// </summary>
/// <remarks>
/// <para>
/// The site is worth separating from the path-uniqueness family, because it is not fixed by making the path index
/// tolerant. <c>Update</c> un-indexes the previous results by the <em>source path</em> they were filed under, and
/// re-indexes the new introductions by their <em>name</em>. The name comes from
/// <c>DesignTimeSyntaxTreeGenerator.GetUniqueFilenameForType</c>, which renders the target type and never the path.
/// The two keys are therefore independent, and the pass is not a transaction: when a name moves from one source path
/// to another between two runs, only the new path is un-indexed, the entry the old path left behind survives, and
/// <c>TryAdd</c> fails.
/// </para>
/// <para>
/// The name is deterministic; its ownership is not. A partial type whose primary declaration moves between files is
/// enough, and two trees sharing a path make it routine, because which of them represents the path in the
/// path-keyed result builders is not stable across compilations.
/// </para>
/// <para>
/// The test drives <c>Update</c> directly, as <see cref="SplitResultsByTreePathLookupTests"/> does, because which
/// trees the pipeline considers dirty is a function of the change graph and not something a test can dictate.
/// </para>
/// </remarks>
public sealed class IntroducedSyntaxTreeOwnershipTests : UnitTestClass
{
    public IntroducedSyntaxTreeOwnershipTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

    private const string _aspectCode = """
                                       using Metalama.Framework.Aspects;

                                       public class Aspect : TypeAspect { }
                                       """;

    private const string _firstCode = "public partial class TargetClass { }";

    private const string _secondCode = "public partial class TargetClass { }";

    private const string _introducedName = "TargetClass.cs";

    /// <summary>
    /// The same introduced name is filed first under <c>first.cs</c> and then under <c>second.cs</c>, which is what
    /// happens when the primary declaration of a partial type moves between the two files. The second update must
    /// succeed and the index must name the tree the second run produced.
    /// </summary>
    [Fact]
    public void IntroducedNameChangesOwningPath()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var compilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string> { ["aspect.cs"] = _aspectCode, ["first.cs"] = _firstCode, ["second.cs"] = _secondCode } );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var executed ) );

        var firstTree = compilation.SyntaxTrees.Single( t => t.FilePath == "first.cs" );
        var secondTree = compilation.SyntaxTrees.Single( t => t.FilePath == "second.cs" );

        var afterFirst = Update( testContext, executed.Result, compilation, firstTree );

        Assert.True( afterFirst.IntroducedSyntaxTrees.ContainsKey( _introducedName ) );

        var afterSecond = Update( testContext, afterFirst, compilation, secondTree );

        Assert.Equal( secondTree, afterSecond.IntroducedSyntaxTrees[_introducedName].SourceSyntaxTree );
    }

    /// <summary>
    /// Files a result that introduces a tree named <see cref="_introducedName"/> whose source is
    /// <paramref name="sourceTree"/>, over a partial compilation restricted to that tree, which is the state the
    /// pipeline is in when only that file is dirty.
    /// </summary>
    private static DesignTimeAspectPipelineResult Update(
        TestContext testContext,
        DesignTimeAspectPipelineResult previous,
        Compilation compilation,
        SyntaxTree sourceTree )
    {
        var partialCompilation = PartialCompilation.CreatePartial( compilation, sourceTree );

        var generatedTree = SyntaxFactory.ParseSyntaxTree(
            "public partial class TargetClass { }",
            path: _introducedName,
            options: testContext.GetCompilationParseOptions() );

        var pipelineResults = new DesignTimePipelineExecutionResult(
            partialCompilation.SyntaxTreeCollection,
            ImmutableArray.Create( new IntroducedSyntaxTree( _introducedName, sourceTree, generatedTree ) ),
            ImmutableUserDiagnosticList.Empty,
            ImmutableArray<InheritableAspectInstance>.Empty,
            ImmutableArray<KeyValuePair<HierarchicalOptionsKey, IHierarchicalOptions>>.Empty,
            ImmutableArray<ITransitivePipelineContributor>.Empty,
            ImmutableArray<IAspectInstance>.Empty,
            ImmutableArray<ITransformationBase>.Empty,
            ImmutableDictionaryOfArray<IRef<IDeclaration>, AnnotationInstance>.Empty );

        var projectVersion = new DesignTimeProjectVersion(
            new TestProjectVersion( compilation ),
            ImmutableArray<DesignTimeProjectReference>.Empty,
            DesignTimeAspectPipelineStatus.Default );

        return previous.Update( partialCompilation, projectVersion, pipelineResults, previous.Configuration.AssertNotNull() );
    }
}
