// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline.MemoryLeaks;

/// <summary>
/// Tests the durability requirement that the design-time pipeline places on the contributors an extension produces,
/// and that no part of the extension contract states.
/// </summary>
/// <remarks>
/// <para>
/// An extension returns its contributors from <see cref="PipelineExtension.ExecuteDesignTimePipelineContributorsAsync"/>,
/// and the pipeline files each of them under the path of the syntax tree it reports, in the
/// <c>SyntaxTreePipelineResult</c> of that file. The results of the files the pipeline did not re-analyse are carried
/// forward unchanged from one version of the project to the next, and <c>SplitResultsByTree</c> goes further: when a
/// later run produces a contributor for a file that is not dirty, it discards the new instance precisely so that the
/// one produced earlier survives. A contributor therefore keeps the version of the project in which its file was last
/// analysed, which for a file the user never edits is the first version of the session.
/// </para>
/// <para>
/// This is sound only if the contributor holds nothing bound to the compilation it was produced in. The requirement
/// is real, it is what <c>SyntaxTreePipelineResult</c> claims about itself ("compilation-independent and cacheable"),
/// and nothing in <see cref="IDesignTimePipelineResultExtension"/> or
/// <see cref="ITransitivePipelineContributor.ToDesignTime"/> states it, so an extension author has no way of learning
/// it other than by measuring the memory of the analysis process.
/// </para>
/// <para>
/// The two tests below are a matched pair over the same extension, differing only in whether the reference the
/// contributor carries is made durable. That is what makes the result interpretable: a suite in which only the
/// non-durable case were present could not distinguish a genuine retention from an assertion that never holds.
/// The reference validation of Metalama.Extensions.Validation is the production extension that takes this path, and
/// the contributor below reproduces the shape of what it stores without depending on it. The missing requirement is
/// tracked by issue #1799.
/// </para>
/// </remarks>
public sealed class ExtensionContributorMemoryLeakTests : DesignTimeTestBase
{
    /// <summary>
    /// The name of the file that declares the type the contributor refers to. No test edits it, so its result, and
    /// the contributor filed under it, are produced during the first run and then survive the whole session.
    /// </summary>
    private const string _anchorFileName = "Anchor.cs";

    /// <summary>
    /// The name of the run-time file that the tests edit.
    /// </summary>
    private const string _targetFileName = "Target.cs";

    /// <summary>
    /// The name of the file that declares the aspect that registers an extension contributor.
    /// </summary>
    private const string _aspectFileName = "Aspect.cs";

    /// <summary>
    /// An aspect that registers a diagnostic source, which is a contributor of extension kind.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The design-time stage invokes an extension only when the project contributed at least one contributor of
    /// extension kind, which is how the production extensions are reached: a fabric or an aspect registers a source,
    /// and the extension turns the sources into the contributors that the pipeline then stores.
    /// </para>
    /// <para>
    /// The source is registered by an aspect rather than by a project fabric on purpose. The amender of a project
    /// fabric lives in the pipeline configuration and retains the version of the project it was built from, which is
    /// the separate defect that <see cref="FabricMemoryLeakTests"/> covers. Registering the source from an aspect
    /// keeps the query owned by the per-run aspect builder, so that the outcome of the pair below depends on the
    /// durability of the contributor's own reference and on nothing else.
    /// </para>
    /// <para>
    /// The argument of the diagnostic is the name of the method rather than the method itself, for the same reason:
    /// a diagnostic whose argument is a declaration retains the compilation through the argument, which is the
    /// separate defect that <see cref="DiagnosticArgumentMemoryLeakTests"/> covers.
    /// </para>
    /// </remarks>
    private const string _aspectCode = """
                                       using Metalama.Framework.Aspects;
                                       using Metalama.Framework.Code;
                                       using Metalama.Framework.Diagnostics;
                                       using System.Linq;

                                       public class ValidateAttribute : TypeAspect
                                       {
                                           private static readonly DiagnosticDefinition<string> _warning =
                                               new( "MY001", Severity.Warning, "Warning on {0}." );

                                           public override void BuildAspect( IAspectBuilder<INamedType> builder )
                                               => builder.Outbound.SelectMany( t => t.Methods ).ReportDiagnostic( m => _warning.WithArguments( m.Name ) );
                                       }
                                       """;

    private const string _anchorCode = """
                                       [Validate]
                                       public class Anchor
                                       {
                                           public int Value;

                                           public int GetValue() => this.Value;
                                       }
                                       """;

    public ExtensionContributorMemoryLeakTests( ITestOutputHelper logger ) : base( logger ) { }

    private static string GetTargetCode( int version )
        => $$"""
             public class Target
             {
                 public int Method() => {{version}};
             }
             """;

    private static Dictionary<string, string> CreateInitialCode()
        => new()
        {
            [_aspectFileName] = _aspectCode,
            [_anchorFileName] = _anchorCode,
            [_targetFileName] = GetTargetCode( 0 )
        };

    /// <summary>
    /// Runs an editing session on the given pipeline factory and returns a weak reference to the compilation of the
    /// first version, which is the version the contributor is produced in.
    /// </summary>
    /// <remarks>
    /// The factory is created by the caller and outlives this method, because it is the root the assertion is made
    /// against: a session whose factory had already been disposed would release everything for a reason unrelated to
    /// the property under test. Conversely, the weak reference is returned rather than asserted here, so that no local
    /// of this frame holds the compilation when the caller makes the assertion.
    /// </remarks>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private WeakReference RunEditingSession(
        TestContext testContext,
        TestDesignTimeAspectPipelineFactory factory,
        string sessionName,
        int editCount )
    {
        var simulator = new DesignTimeEditingSimulator( testContext, factory, sessionName, CreateInitialCode() );

        var initialCompilation = simulator.GetWeakReferenceToCurrentCompilation();
        simulator.Execute();

        for ( var version = 1; version <= editCount; version++ )
        {
            simulator.ApplyEdit( _targetFileName, GetTargetCode( version ) );
        }

        var contributorCount = simulator.GetPipeline().AspectPipelineResult.Extensions.Extensions.Length;
        this.TestOutput.WriteLine( $"The pipeline holds {contributorCount} contributor(s) after {editCount} edits." );

        Assert.True(
            contributorCount > 0,
            "The extension produced no contributor that survived the session, therefore the test did not exercise the "
            + "retention it was supposed to exercise." );

        return initialCompilation;
    }

    /// <summary>
    /// Creates a test context in which the given extension type is the only registered extension.
    /// </summary>
    /// <remarks>
    /// The kind of durable reference is pinned rather than inherited from the scope, because it is precisely what these
    /// tests vary and what decides their outcome. Leaving it to the default would make a change of default surface here
    /// as an unexplained retention chain rather than as its own failure.
    /// </remarks>
    private TestContext CreateTestContextWithExtension( Type extensionType, DurableRefKind durableRefKind = DurableRefKind.Serializable )
        => this.CreateTestContext(
            this.CreateDefaultTestContextOptions() with
            {
                ExtensionTypes = ImmutableArray.Create( extensionType ), DurableRefKind = durableRefKind
            } );

    /// <summary>
    /// Verifies that a contributor holding a durable reference does not retain the version of the project in which it
    /// was produced.
    /// </summary>
    /// <remarks>
    /// This is the guarantee of the pair, and
    /// <see cref="NonDurableContributor_RetainsTheCompilationItWasProducedIn"/> is the control that establishes the
    /// assertion can fail. The two sessions differ by one call to <c>ToDurable</c> and by nothing else, so passing
    /// here is attributable to the reference the contributor carries and to nothing else.
    /// </remarks>
    [Fact]
    public void DurableContributor_DoesNotRetainTheCompilationItWasProducedIn()
    {
        using var testContext = this.CreateTestContextWithExtension( typeof(DurableContributorExtension) );
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var initialCompilation = this.RunEditingSession( testContext, factory, "DurableContributor", 10 );

        MemoryLeakAssert.Collected(
            initialCompilation,
            "The compilation in which a durable contributor was produced",
            ("pipelineFactory", factory) );
    }

    /// <summary>
    /// Verifies that a contributor holding a reference obtained from the code model, without making it durable, does
    /// retain the version of the project in which it was produced.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test asserts that the retention happens, which is the opposite of what the rest of this suite asserts, and
    /// it does so for two reasons.
    /// </para>
    /// <para>
    /// It is the control that gives <see cref="DurableContributor_DoesNotRetainTheCompilationItWasProducedIn"/> its
    /// meaning. The two sessions differ by one call to <c>ToDurable</c> and by nothing else, so a suite in which the
    /// durable case passed while nothing established that the assertion can fail would be a suite whose assertion
    /// never fires.
    /// </para>
    /// <para>
    /// It also records that the framework does not, and cannot, enforce the requirement stated on
    /// <see cref="IDesignTimePipelineResultExtension"/>. What a contributor carries is opaque to the pipeline, which
    /// stores the object as the extension produced it. The requirement is therefore documented on the contract and
    /// tested here from the outside. Should enforcement ever be added, this test fails, and that failure is the signal
    /// to replace it with an assertion that the enforcement works.
    /// </para>
    /// </remarks>
    [Fact]
    public void NonDurableContributor_RetainsTheCompilationItWasProducedIn()
    {
        using var testContext = this.CreateTestContextWithExtension( typeof(NonDurableContributorExtension) );
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var initialCompilation = this.RunEditingSession( testContext, factory, "NonDurableContributor", 10 );

        MemoryLeakAssert.RetainedThrough(
            initialCompilation,
            nameof(TestContributor),
            "The compilation in which a non-durable contributor was produced",
            ("pipelineFactory", factory) );
    }

    /// <summary>
    /// Verifies that a contributor holding a durable reference produced by a batch compilation does retain the version
    /// of the project in which it was produced.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This asserts that the retention happens, and it is not a defect. During a batch compilation a durable reference
    /// holds the reference it was made from, because the one compilation of the build outlives every object the run
    /// produces and the identifier round trip would buy nothing. See issue #1811.
    /// </para>
    /// <para>
    /// It is recorded as a test rather than as a comment because the difference between the two kinds is invisible at
    /// every call site: both are <see cref="IDurableRef{T}"/>, and only what they hold differs. Should a design-time
    /// path ever be given the batch-compilation factory by accident, the contributor tests above would start failing
    /// with a retention chain, and this test is what names the cause.
    /// </para>
    /// </remarks>
    [Fact]
    public void LiveDurableContributor_RetainsTheCompilationItWasProducedIn()
    {
        using var testContext = this.CreateTestContextWithExtension( typeof(DurableContributorExtension), DurableRefKind.Live );
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var initialCompilation = this.RunEditingSession( testContext, factory, "LiveDurableContributor", 10 );

        MemoryLeakAssert.RetainedThrough(
            initialCompilation,
            nameof(TestContributor),
            "The compilation in which a durable contributor of a batch compilation was produced",
            ("pipelineFactory", factory) );
    }

    /// <summary>
    /// A test extension that produces one contributor per run, referring to a declaration of the anchor file.
    /// </summary>
    /// <remarks>
    /// The extension has no state of its own, exactly as the production extensions have none. Everything that could
    /// retain a compilation is in the contributor it returns, which is what the pipeline stores.
    /// </remarks>
    private abstract class ContributorExtension : PipelineExtension
    {
        /// <summary>
        /// Builds the reference that the contributor carries. The two implementations differ only in this method.
        /// </summary>
        protected abstract object CreatePayload( INamedType anchor );

        public override Task<ExtensionPipelineContributorsResult> ExecuteDesignTimePipelineContributorsAsync(
            AspectPipelineConfiguration pipelineConfiguration,
            IEnumerable<IPipelineContributor> contributors,
            CompilationModel initialCompilation,
            CompilationModel finalCompilation,
            CancellationToken cancellationToken )
        {
            var anchor = initialCompilation.Types.SingleOrDefault( t => t.Name == "Anchor" );

            if ( anchor == null )
            {
                // The anchor type is absent from a partial compilation that does not include its file, which is the
                // normal case for every run after the first. The contributor produced by the first run is the one
                // whose retention is under test, so there is nothing to add here.
                return Task.FromResult( ExtensionPipelineContributorsResult.Empty );
            }

            var contributor = new TestContributor( anchor.GetPrimarySyntaxTree(), this.CreatePayload( anchor ) );

            return Task.FromResult(
                new ExtensionPipelineContributorsResult(
                    ImmutableArray.Create<ITransitivePipelineContributor>( contributor ),
                    ImmutableUserDiagnosticList.Empty ) );
        }
    }

    /// <summary>
    /// Produces a contributor whose reference is durable, which is the correct behaviour.
    /// </summary>
    private sealed class DurableContributorExtension : ContributorExtension
    {
        protected override object CreatePayload( INamedType anchor ) => anchor.ToRef().ToDurable();
    }

    /// <summary>
    /// Produces a contributor whose reference is the one the code model returns, which is backed by a symbol.
    /// </summary>
    private sealed class NonDurableContributorExtension : ContributorExtension
    {
        protected override object CreatePayload( INamedType anchor ) => anchor.ToRef();
    }

    /// <summary>
    /// A minimal contributor, carrying an opaque payload whose durability is what the tests vary.
    /// </summary>
    /// <remarks>
    /// The payload is typed as <see cref="object"/> so that the contributor itself is identical in both cases and the
    /// only difference between the two sessions is the reference the extension chose to store, which is the decision
    /// an extension author actually makes.
    /// </remarks>
    private sealed class TestContributor : ITransitivePipelineContributor, IDesignTimePipelineResultExtension
    {
        private static readonly ContributorKind<TestContributor> _kind = new( nameof(TestContributor) );

#pragma warning disable IDE0052 // The field is never read: holding the payload is its entire purpose.
        private readonly object _payload;
#pragma warning restore IDE0052

        public TestContributor( SyntaxTree? syntaxTree, object payload )
        {
            this.SyntaxTree = syntaxTree;
            this._payload = payload;
        }

        public SyntaxTree? SyntaxTree { get; }

        public ContributorKind ContributorKind => _kind;

        public IDesignTimePipelineResultExtension? ToDesignTime() => this;

        public ITransitiveAspectsManifestExtension ToTransitiveAspectManifestExtension() => throw new NotSupportedException();
    }
}
