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
using Metalama.Framework.Engine.ReferenceGraph;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Options;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

/// <summary>
/// Design-time validators reach a consuming project through two channels, and exactly one of them must carry
/// them for any given consumer, or the consumer sees each validator more than once:
/// <list type="number">
/// <item>
/// the design-time channel, <see cref="DesignTimeProjectVersion.ReferencedExtensions"/>, which reads the producer's
/// live <c>DesignTimeAspectPipelineResult</c> and deduplicates diamond-shaped reference graphs; and
/// </item>
/// <item>
/// the manifest channel, which walks each direct reference's transitive manifest and is <em>not</em> deduplicated.
/// </item>
/// </list>
/// A consumer built against the same version of Metalama always has channel 1, because its reference carries the
/// producer's live result, so the same-version manifests must not carry validators. A consumer built against a
/// different version of Metalama has only channel 2: the producer's result is an object of the other version's
/// <c>Metalama.Framework.Engine</c> and cannot be handed across, so its manifest must carry them.
/// </summary>
/// <remarks>
/// This regressed once already (issue #1710): the same-version reference path was rerouted from the live result,
/// which filtered validators out, onto the manifest built for the cross-version consumer, which keeps them. Both
/// channels then fired, and a downstream consumer reported every cross-project reference diagnostic twice, and six
/// times across a diamond, where the undeduplicated manifest channel compounds.
/// </remarks>
public sealed class TransitiveManifestValidatorChannelTests : UnitTestClass
{
    public TransitiveManifestValidatorChannelTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

    private const string _code = """
                                 public class C { }
                                 """;

    /// <summary>
    /// Builds a pipeline result whose design-time extension collection holds the contributors returned by
    /// <paramref name="createExtensions"/>, by executing a trivial pipeline (which supplies a real
    /// <c>AspectPipelineConfiguration</c>, required by the result's non-empty invariant) and then updating it with a
    /// fabricated execution result. Going through <c>Update</c> exercises the real routing into
    /// <c>DesignTimeAspectPipelineResultExtensionCollection</c>.
    /// </summary>
    /// <param name="createExtensions">
    /// Builds the contributors, given a <see cref="SymbolDictionaryKey"/> for a type of the test compilation, which
    /// only exists once the compilation does.
    /// </param>
    private static DesignTimeAspectPipelineResult CreateResultWithExtensions(
        TestContext testContext,
        TestDesignTimeAspectPipelineFactory factory,
        Func<SymbolDictionaryKey, ITransitivePipelineContributor[]> createExtensions )
    {
        var compilation = testContext.CreateCSharpCompilation( _code );
        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var executed ) );

        // A real key for the validated declaration. The fixture never looks a validator up by symbol, but the key
        // indexes the validator dictionary that Update populates, so a default (identity-less) one would be a
        // hazard the moment anything compared keys.
        var validatedType = compilation.GetTypeByMetadataName( "C" ).AssertNotNull();
        var extensions = createExtensions( SymbolDictionaryKey.CreatePersistentKey( validatedType ) );

        var partialCompilation = PartialCompilation.CreateComplete( compilation );

        var pipelineResults = new DesignTimePipelineExecutionResult(
            partialCompilation.SyntaxTrees,
            ImmutableArray<IntroducedSyntaxTree>.Empty,
            ImmutableUserDiagnosticList.Empty,
            ImmutableArray<InheritableAspectInstance>.Empty,
            ImmutableArray<KeyValuePair<HierarchicalOptionsKey, IHierarchicalOptions>>.Empty,
            extensions.ToImmutableArray(),
            ImmutableArray<IAspectInstance>.Empty,
            ImmutableArray<ITransformationBase>.Empty,
            ImmutableDictionaryOfArray<IRef<IDeclaration>, AnnotationInstance>.Empty );

        // No project references: this fixture is about what the producer puts in its own manifests.
        var projectVersion = new DesignTimeProjectVersion(
            new TestProjectVersion( compilation ),
            ImmutableArray<DesignTimeProjectReference>.Empty,
            DesignTimeAspectPipelineStatus.Default );

        return executed.Result.Update(
            partialCompilation,
            projectVersion,
            pipelineResults,
            executed.Result.Configuration.AssertNotNull() );
    }

    /// <summary>
    /// The fast path added by #1710. The live manifest is what a same-version consumer reuses directly, so it must
    /// drop validators while keeping every other extension.
    /// </summary>
    [Fact]
    public void LiveManifest_DropsValidators_ButKeepsOtherExtensions()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        FakeContributor? validator = null;
        FakeContributor? other = null;

        var result = CreateResultWithExtensions(
            testContext,
            factory,
            key =>
            {
                validator = new FakeContributor( key, isValidator: true );
                other = new FakeContributor( key, isValidator: false );

                return [validator, other];
            } );

        // Channel 1 has the validator: this is the channel the consumer actually reads it from, so the validator
        // must not be lost, only kept out of the manifest.
        Assert.Contains( validator.AssertNotNull(), result.Extensions.Extensions );

        var manifestExtensions = result.LiveTransitiveAspectManifest.Extensions;

        Assert.DoesNotContain( validator.AssertNotNull().ManifestExtension, manifestExtensions );
        Assert.Contains( other.AssertNotNull().ManifestExtension, manifestExtensions );
    }

    /// <summary>
    /// The slow in-process path, taken when the producer's and consumer's compile-time copies differ (e.g. a
    /// multi-targeted assembly). The consumer still has channel 1, so this manifest must drop validators too.
    /// </summary>
    [Fact]
    public void InProcessSerializedManifest_DropsValidators()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var result = CreateResultWithExtensions( testContext, factory, key => [new FakeContributor( key, isValidator: true )] );

        // The gate is deliberately computed from the unfiltered collection: were it to go false here, the reference
        // would carry no live manifest either, and channel 1 would lose the validator along with the manifest.
        Assert.True( result.HasTransitiveAspectManifestContent );

        var serialized = result.SerializedTransitiveAspectManifestWithoutValidators;
        Assert.False( serialized.IsDefaultOrEmpty );

        var deserialized = TransitiveAspectsManifest.Deserialize(
            new MemoryStream( serialized.Bytes.ToArray() ),
            result.Configuration.AssertNotNull().ServiceProvider,
            "Producer" );

        Assert.Empty( deserialized.Extensions );
    }

    /// <summary>
    /// Stands in for a design-time validator, which in production comes from Metalama.Extensions.Validation and so
    /// is not available here. One class covers both roles, with <c>isValidator</c> the only difference: routing
    /// keys off <see cref="ContributorKind.IsDesignTimeValidator"/> (in conjunction with the interface, which both
    /// roles implement), so varying just the flag isolates the behaviour under test.
    /// </summary>
    private sealed class FakeContributor : ITransitivePipelineContributor, IDesignTimeValidatorExtension
    {
        public FakeContributor( SymbolDictionaryKey validatedDeclaration, bool isValidator )
        {
            this.ValidatedDeclaration = validatedDeclaration;

            this.ContributorKind = new ContributorKind<FakeContributor>( isValidator ? "FakeValidator" : "FakeExtension" )
            {
                IsDesignTimeValidator = isValidator
            };

            this.ManifestExtension = new FakeManifestExtension( this.ContributorKind );
        }

        public FakeManifestExtension ManifestExtension { get; }

        public ContributorKind ContributorKind { get; }

        // Null puts the extension in the file-path-less bucket, which Update routes into the collection all the
        // same; the fixture does not care which syntax tree it is attributed to.
        public SyntaxTree? SyntaxTree => null;

        public IDesignTimePipelineResultExtension? ToDesignTime() => this;

        public ITransitiveAspectsManifestExtension ToTransitiveAspectManifestExtension() => this.ManifestExtension;

        public ReferenceIndexerRequirements? ReferenceIndexerRequirements => null;

        public SymbolDictionaryKey ValidatedDeclaration { get; }
    }

    private sealed class FakeManifestExtension : ITransitiveAspectsManifestExtension
    {
        public FakeManifestExtension( ContributorKind contributorKind )
        {
            this.ContributorKind = contributorKind;
        }

        public ContributorKind ContributorKind { get; }
    }
}
