// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.CompileTime.Serialization;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.Fabrics;

/// <summary>
/// Tests <c>UserCodeRetentionAnalyzer</c> against real fabrics, executed through the compile-time pipeline.
/// </summary>
/// <remarks>
/// These tests exercise the roots of the analysis, which are the objects that the fabrics leave in the pipeline
/// configuration: the queries, the contributors, the amenders and the closures of the user lambdas. A test that plants
/// the retention in a hand-built object graph cannot prove that those objects are reachable, which is why the fabrics
/// here are written as source code and compiled.
/// </remarks>
public sealed class UserCodeRetentionAnalyzerTests : UnitTestClass
{
    private const string _retentionDiagnosticId = "LAMA0085";
    private const string _summaryDiagnosticId = "LAMA0086";

    public UserCodeRetentionAnalyzerTests( ITestOutputHelper testOutputHelper ) : base( testOutputHelper ) { }

    /// <summary>
    /// The prologue that every test compiles. The aspect is applied only to the types whose name starts with
    /// <c>Target</c>, because a fabric that adds an aspect to every type of the project would also add it to the aspect
    /// class and to the fabric itself, which is not eligible.
    /// </summary>
    private const string _prologue = """
                                     using Metalama.Framework.Aspects;
                                     using Metalama.Framework.Code;
                                     using Metalama.Framework.Fabrics;
                                     using Metalama.Framework.Serialization;
                                     using System;
                                     using System.Collections.Generic;
                                     using System.Linq;

                                     internal class MyAspect : TypeAspect { }

                                     internal class TargetOne { }

                                     internal class TargetTwo { }
                                     """;

    private async Task<ImmutableArray<Diagnostic>> RunAsync( string fabricCode, bool enabled = true )
    {
        using var testContext = this.CreateTestContext( new TestContextOptions { DiagnoseMemoryLeaks = enabled } );

        var compilation = testContext.CreateCSharpCompilation( _prologue + fabricCode );
        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );
        var diagnostics = new DiagnosticBag();

        var result = await pipeline.ExecuteAsync( diagnostics.Report, null, compilation, default, testContext.CancellationToken );

        this.TestOutput.WriteLine( string.Join( "\n", diagnostics.Select( d => d.ToString() ) ) );

        Assert.True(
            result.IsSuccessful,
            "The pipeline failed: " + string.Join( ", ", diagnostics.Where( d => d.Severity == DiagnosticSeverity.Error ) ) );

        Assert.DoesNotContain( diagnostics, d => d.Severity == DiagnosticSeverity.Error );

        return diagnostics.ToImmutableArray();
    }

    private static IReadOnlyList<Diagnostic> Retentions( ImmutableArray<Diagnostic> diagnostics )
        => diagnostics.Where( d => d.Id == _retentionDiagnosticId ).ToReadOnlyList();

    [Fact]
    public async Task DeclarationCapturedByPredicate_IsReported()
    {
        // The predicate runs while the query is executed, so the list is still empty when AmendProject returns. This is
        // what the analysis would miss if it ran immediately after the fabrics.
        var diagnostics = await this.RunAsync(
            """

            internal class LeakyFabric : ProjectFabric
            {
                private readonly List<INamedType> _seen = new List<INamedType>();

                public override void AmendProject( IProjectAmender amender )
                {
                    amender.SelectTypes()
                        .Where( t => { this._seen.Add( t ); return t.Name.StartsWith( "Target" ); } )
                        .AddAspect<MyAspect>();
                }
            }
            """ );

        var retentions = Retentions( diagnostics );

        Assert.NotEmpty( retentions );
        Assert.All( retentions, d => Assert.Contains( "LeakyFabric", d.GetMessage() ) );
        Assert.Contains( retentions, d => d.GetMessage().Contains( "_seen" ) );
        Assert.Contains( diagnostics, d => d.Id == _summaryDiagnosticId );
    }

    [Fact]
    public async Task DeclarationInFabricField_IsReported()
    {
        var diagnostics = await this.RunAsync(
            """

            internal class LeakyFabric : ProjectFabric
            {
                private INamedType? _first;

                public override void AmendProject( IProjectAmender amender )
                {
                    amender.SelectTypes()
                        .Where( t => { this._first ??= t; return t.Name.StartsWith( "Target" ); } )
                        .AddAspect<MyAspect>();
                }
            }
            """ );

        var retentions = Retentions( diagnostics );

        Assert.Single( retentions );
        Assert.Contains( "LeakyFabric", retentions[0].GetMessage() );
        Assert.Contains( "_first", retentions[0].GetMessage() );
    }

    [Fact]
    public async Task DeclarationInStaticField_IsReported()
    {
        // A static field of compile-time code outlives every configuration, and is invisible to a walk that starts from
        // the contributors alone.
        var diagnostics = await this.RunAsync(
            """

            [CompileTime]
            internal static class StaticCache
            {
                public static INamedType? Type;
            }

            internal class LeakyFabric : ProjectFabric
            {
                public override void AmendProject( IProjectAmender amender )
                {
                    amender.SelectTypes()
                        .Where( t => { StaticCache.Type ??= t; return t.Name.StartsWith( "Target" ); } )
                        .AddAspect<MyAspect>();
                }
            }
            """ );

        Assert.Contains( Retentions( diagnostics ), d => d.GetMessage().Contains( "StaticCache" ) );
    }

    [Fact]
    public async Task DeclarationCapturedIndirectly_IsReported()
    {
        // The declaration is two levels deep inside the user's own objects, which proves that the walk does not stop at
        // the first user type, and that the reported type is the one that actually holds the reference.
        var diagnostics = await this.RunAsync(
            """

            [CompileTime]
            internal class Holder
            {
                public Inner Inner = new Inner();
            }

            [CompileTime]
            internal class Inner
            {
                public INamedType? Type;
            }

            internal class LeakyFabric : ProjectFabric
            {
                private readonly Holder _holder = new Holder();

                public override void AmendProject( IProjectAmender amender )
                {
                    amender.SelectTypes()
                        .Where( t => { this._holder.Inner.Type ??= t; return t.Name.StartsWith( "Target" ); } )
                        .AddAspect<MyAspect>();
                }
            }
            """ );

        var retentions = Retentions( diagnostics );

        Assert.Single( retentions );
        Assert.Contains( "Inner", retentions[0].GetMessage() );
        Assert.Contains( "_holder -> Inner -> Type", retentions[0].GetMessage() );
    }

    [Fact]
    public async Task NamespaceFabric_IsAnalysed()
    {
        var diagnostics = await this.RunAsync(
            """

            namespace Ns
            {
                internal class TargetThree { }

                internal class LeakyFabric : NamespaceFabric
                {
                    private INamedType? _first;

                    public override void AmendNamespace( INamespaceAmender amender )
                    {
                        amender.SelectTypes()
                            .Where( t => { this._first ??= t; return t.Name.StartsWith( "Target" ); } )
                            .AddAspect<MyAspect>();
                    }
                }
            }
            """ );

        Assert.Contains( Retentions( diagnostics ), d => d.GetMessage().Contains( "LeakyFabric" ) );
    }

    [Fact]
    public async Task ChainedQuery_ReportsTheCapturingLink()
    {
        // Only the first link of the chain captures. The reported path must reach the field of the fabric and must not
        // name the link that captures nothing.
        var diagnostics = await this.RunAsync(
            """

            internal class LeakyFabric : ProjectFabric
            {
                private INamedType? _first;

                public override void AmendProject( IProjectAmender amender )
                {
                    amender.SelectTypes()
                        .Where( t => { this._first ??= t; return true; } )
                        .Where( t => t.Name.StartsWith( "Target" ) )
                        .AddAspect<MyAspect>();
                }
            }
            """ );

        var retentions = Retentions( diagnostics );

        Assert.Single( retentions );
        Assert.Contains( "_first", retentions[0].GetMessage() );
    }

    [Fact]
    public async Task SameDeclarationCapturedTwice_IsReportedOnce()
    {
        var diagnostics = await this.RunAsync(
            """

            internal class LeakyFabric : ProjectFabric
            {
                private INamedType? _a;
                private INamedType? _b;

                public override void AmendProject( IProjectAmender amender )
                {
                    amender.SelectTypes()
                        .Where( t => { this._a ??= t; this._b = this._a; return t.Name.StartsWith( "Target" ); } )
                        .AddAspect<MyAspect>();
                }
            }
            """ );

        // The two fields hold the same object, and a finding is created per pinned object, not per reference to it.
        Assert.Single( Retentions( diagnostics ) );
    }

    [Fact]
    public async Task InheritableAspectWithNonSerializedDeclarationField_IsReported()
    {
        // An inheritable aspect instance is filed by the design-time pipeline under the path of its target document and
        // carried forward across every version in which that file did not change. Its target declaration is converted
        // to a durable reference, but the aspect object itself is the user's and is kept live rather than serialized,
        // so a declaration in one of its fields pins the compilation exactly as a fabric field does.
        //
        // A field that the compile-time serializer does see is already rejected by it, with a hard error, when it holds
        // a declaration; see InheritableAspectWithSerializedDeclarationField_IsRejectedBySerialization below. The gap
        // this diagnostic closes is the field the serializer skips, which is invisible to that check and is retained by
        // the design-time cache all the same.
        var diagnostics = await this.RunAsync(
            """

            [Inheritable]
            internal class LeakyInheritableAspect : TypeAspect
            {
                [NonCompileTimeSerialized]
                private INamedType? _target;

                public override void BuildAspect( IAspectBuilder<INamedType> builder )
                {
                    this._target = builder.Target;
                }
            }

            [LeakyInheritableAspect]
            internal class BaseClass { }

            internal class DerivedClass : BaseClass { }
            """ );

        Assert.Contains( Retentions( diagnostics ), d => d.GetMessage().Contains( "LeakyInheritableAspect" ) );
    }

    [Fact]
    public async Task InheritableAspectWithSerializedDeclarationField_IsRejectedBySerialization()
    {
        // Recorded so that the boundary between the two guards stays visible. The compile-time pipeline serializes the
        // externally inheritable aspects into the transitive manifest, and the serializer refuses a declaration. That
        // check is stronger than this diagnostic, because it is an error and is always on, so the diagnostic is not
        // what protects this case and must not be expected to.
        using var testContext = this.CreateTestContext( new TestContextOptions { DiagnoseMemoryLeaks = true } );

        var compilation = testContext.CreateCSharpCompilation(
            _prologue
            + """

              [Inheritable]
              internal class LeakyInheritableAspect : TypeAspect
              {
                  private INamedType? _target;

                  public override void BuildAspect( IAspectBuilder<INamedType> builder )
                  {
                      this._target = builder.Target;
                  }
              }

              [LeakyInheritableAspect]
              internal class BaseClass { }

              internal class DerivedClass : BaseClass { }
              """ );

        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );

        var exception = await Assert.ThrowsAsync<CompileTimeSerializationException>(
            async () => await pipeline.ExecuteAsync( _ => { }, null, compilation, default, testContext.CancellationToken ) );

        Assert.Contains( "is not serializable", exception.Message );
        Assert.Contains( "_target", exception.Message );
    }

    [Fact]
    public async Task InheritableAspectThatCapturesNothing_IsNotReported()
    {
        var diagnostics = await this.RunAsync(
            """

            [Inheritable]
            internal class CleanInheritableAspect : TypeAspect
            {
                private string? _name;

                public override void BuildAspect( IAspectBuilder<INamedType> builder )
                {
                    this._name = builder.Target.Name;
                }
            }

            [CleanInheritableAspect]
            internal class BaseClass { }

            internal class DerivedClass : BaseClass { }
            """ );

        Assert.Empty( Retentions( diagnostics ) );
    }

    [Fact]
    public async Task AspectDeclaredInSourceWithATemplateParameter_RetainsNothing()
    {
        // The template members of an aspect class hold the types of their template parameters, and the aspect classes
        // belong to the pipeline configuration, which outlives the compilation. For an aspect that comes from a package
        // those types are metadata symbols and pin nothing. For an aspect declared in the project, with a template
        // parameter of a type declared in the project, the symbol came from source and the retention was real, until
        // TemplateClassMemberParameter was changed to hold a durable identifier instead. See #1803.
        //
        // This is the test that reported the defect, asserting a count of one, and it is the test that now states the
        // fix. Should the parameter types go back to being symbols, it fails again.
        var diagnostics = await this.RunAsync(
            """

            internal class IntroducingAspect : TypeAspect
            {
                [Introduce]
                public void IntroducedMethod( TargetOne parameter ) { }
            }

            internal class TheFabric : ProjectFabric
            {
                public override void AmendProject( IProjectAmender amender )
                {
                    amender.SelectTypes().Where( t => t.Name == "TargetTwo" ).AddAspect<IntroducingAspect>();
                }
            }
            """ );

        Assert.Empty( Retentions( diagnostics ) );
        Assert.Equal( 0, FrameworkRetentionCount( diagnostics ) );
    }

    /// <summary>
    /// Reads the number of retentions attributed to Metalama itself from the summary diagnostic.
    /// </summary>
    /// <remarks>
    /// The findings attributed to Metalama are counted rather than reported one by one, so a test that asserts their
    /// absence has to read the summary. Asserting on it matters: a regression that starts retaining something inside the
    /// pipeline would otherwise be invisible to every test here, since none of those findings raises a diagnostic.
    /// </remarks>
    private static int FrameworkRetentionCount( ImmutableArray<Diagnostic> diagnostics )
    {
        var summary = Assert.Single( diagnostics.Where( d => d.Id == _summaryDiagnosticId ) );
        var match = Regex.Match( summary.GetMessage(), @"and (\d+) retention\(s\) in Metalama itself" );

        Assert.True( match.Success, $"Could not read the count from the summary: {summary.GetMessage()}" );

        return int.Parse( match.Groups[1].Value, CultureInfo.InvariantCulture );
    }

    [Fact]
    public async Task FabricThatCapturesNothing_IsNotReported()
    {
        // The negative control. A fabric that keeps only strings must produce no finding attributed to user code,
        // otherwise the diagnostic would fire on every project that has a fabric and would be worthless.
        var diagnostics = await this.RunAsync(
            """

            internal class CleanFabric : ProjectFabric
            {
                private readonly List<string> _names = new List<string>();

                public override void AmendProject( IProjectAmender amender )
                {
                    amender.SelectTypes()
                        .Where( t => { this._names.Add( t.Name ); return t.Name.StartsWith( "Target" ); } )
                        .AddAspect<MyAspect>();
                }
            }
            """ );

        Assert.Empty( Retentions( diagnostics ) );
        Assert.Contains( diagnostics, d => d.Id == _summaryDiagnosticId );
    }

    [Fact]
    public async Task FabricThatUsesSerializableId_IsNotReported()
    {
        // The recommended fix. A SerializableDeclarationId is backed by a string and reaches nothing, so a fabric that
        // persists its declarations this way must produce no finding.
        var diagnostics = await this.RunAsync(
            """

            internal class CleanFabric : ProjectFabric
            {
                private readonly List<SerializableDeclarationId> _ids = new List<SerializableDeclarationId>();

                public override void AmendProject( IProjectAmender amender )
                {
                    amender.SelectTypes()
                        .Where( t => { this._ids.Add( t.ToSerializableId() ); return t.Name.StartsWith( "Target" ); } )
                        .AddAspect<MyAspect>();
                }
            }
            """ );

        Assert.Empty( Retentions( diagnostics ) );
    }

    [Fact]
    public async Task FabricThatRegistersNothing_IsNotReported()
    {
        var diagnostics = await this.RunAsync(
            """

            internal class EmptyFabric : ProjectFabric
            {
                public override void AmendProject( IProjectAmender amender ) { }
            }
            """ );

        Assert.Empty( Retentions( diagnostics ) );
    }

    [Fact]
    public async Task NoFabric_ProducesNoRetention()
    {
        var diagnostics = await this.RunAsync( "" );

        Assert.Empty( Retentions( diagnostics ) );
    }

    [Fact]
    public async Task OptionDisabled_ProducesNoDiagnostic()
    {
        // The analysis is expensive, therefore its absence when the option is off is as important as its presence when
        // the option is on.
        var diagnostics = await this.RunAsync(
            """

            internal class LeakyFabric : ProjectFabric
            {
                private INamedType? _first;

                public override void AmendProject( IProjectAmender amender )
                {
                    amender.SelectTypes()
                        .Where( t => { this._first ??= t; return t.Name.StartsWith( "Target" ); } )
                        .AddAspect<MyAspect>();
                }
            }
            """,
            enabled: false );

        Assert.Empty( Retentions( diagnostics ) );
        Assert.DoesNotContain( diagnostics, d => d.Id == _summaryDiagnosticId );
    }
}
