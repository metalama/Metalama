// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime;
using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.DesignTime.VersionNeutral;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

#pragma warning disable VSTHRD200, VSTHRD003

/// <summary>
/// Tests the resumption of a paused design-time pipeline by the build touch file, <c>MetalamaBuild.touch</c>.
/// </summary>
/// <remarks>
/// <para>
/// The design-time pipeline pauses when the compile-time code of a project changes in a way that requires a build. It
/// resumes when its file system watcher reports a <see cref="WatcherChangeTypes.Changed"/>
/// event on the build touch file, which the <c>CreateMetalamaTouchFiles</c> target of <c>Metalama.Framework.targets</c>
/// writes at the beginning of every build.
/// </para>
/// <para>
/// These tests run the real design-time pipelines, with a real touch file per project and the real file system watcher.
/// Only MSBuild is simulated, by <see cref="SimulateBuild"/>, which reproduces the effect of the target on the file
/// system. The tests wait for the resumption through the status events of the pipelines, and never through a delay.
/// </para>
/// </remarks>
public sealed class BuildTouchFileTests : DesignTimeTestBase
{
    public BuildTouchFileTests( ITestOutputHelper logger ) : base( logger ) { }

    private const string _aspectCode = """
                                       using Metalama.Framework.Aspects;
                                       using Metalama.Framework.Code;
                                       using Metalama.Framework.Diagnostics;

                                       public class MyAspect : MethodAspect
                                       {
                                           private static readonly DiagnosticDefinition _description = new( "MY001", Severity.Warning, "AspectVersion=$version$" );

                                           public override void BuildAspect( IAspectBuilder<IMethod> aspectBuilder )
                                           {
                                               aspectBuilder.Diagnostics.Report( _description );
                                           }
                                       }
                                       """;

    private const string _targetCode = """
                                       class C
                                       {
                                           [MyAspect]
                                           void M() {}
                                       }
                                       """;

    /// <summary>
    /// Reproduces the effect of the <c>CreateMetalamaTouchFiles</c> target of <c>Metalama.Framework.targets</c> on the
    /// build touch file of a project.
    /// </summary>
    /// <remarks>
    /// The target runs the <c>Touch</c> task on every build that is not a design-time build. The task creates a missing
    /// file with <see cref="File.Create(string)"/>, then sets the last access time and the last write time of the file
    /// to the current time. The last step raises the <see cref="WatcherChangeTypes.Changed"/> event to which the
    /// design-time pipeline listens.
    /// </remarks>
    private static void SimulateBuild( string touchFile )
    {
        if ( !File.Exists( touchFile ) )
        {
            Directory.CreateDirectory( Path.GetDirectoryName( touchFile )! );

            using ( File.Create( touchFile ) ) { }
        }

        var now = DateTime.Now;
        File.SetLastAccessTime( touchFile, now );
        File.SetLastWriteTime( touchFile, now );
    }

    /// <summary>
    /// Returns the path of the build touch file of a project, as <c>Metalama.Framework.targets</c> defines it under the
    /// intermediate output directory of the project.
    /// </summary>
    private static string GetTouchFile( TestContext testContext, string projectName )
        => Path.Combine( testContext.BaseDirectory, projectName, "obj", "MetalamaBuild.touch" );

    /// <summary>
    /// Waits until a pipeline leaves the <see cref="DesignTimeAspectPipelineStatus.Paused"/> status.
    /// </summary>
    /// <remarks>
    /// The handler is registered before the status is read, so that a resumption that happens between the two is not
    /// missed. The event hub holds its handlers through weak references, so the caller must keep the returned object
    /// alive until the task has completed.
    /// </remarks>
    private sealed class ResumptionAwaiter
    {
        private readonly TaskCompletionSource<bool> _resumed = new( TaskCreationOptions.RunContinuationsAsynchronously );
        private readonly DesignTimeAspectPipeline _pipeline;
        private readonly Func<DesignTimePipelineStatusChangedEventArgs, Task> _handler;

        public ResumptionAwaiter( AnalysisProcessEventHub eventHub, DesignTimeAspectPipeline pipeline )
        {
            this._pipeline = pipeline;

            this._handler = args =>
            {
                if ( args.Pipeline == this._pipeline && args.IsResuming )
                {
                    this._resumed.TrySetResult( true );
                }

                return Task.CompletedTask;
            };

            eventHub.PipelineStatusChangedEvent.RegisterHandler( this._handler );

            if ( pipeline.Status != DesignTimeAspectPipelineStatus.Paused )
            {
                this._resumed.TrySetResult( true );
            }
        }

        public async Task WaitAsync( CancellationToken cancellationToken )
        {
            using ( cancellationToken.Register( () => this._resumed.TrySetCanceled( cancellationToken ) ) )
            {
                await this._resumed.Task;
            }
        }
    }

    private static ProjectOptionsWithBuildTouchFile CreateOptions( TestContext testContext, string projectName )
        => new( testContext.ProjectOptions, GetTouchFile( testContext, projectName ) );

    /// <summary>
    /// A project that references a project whose compile-time code changes, both processed by the same version of
    /// Metalama. The build updates the touch files of both projects, and both pipelines resume.
    /// </summary>
    [Fact]
    public async Task SameVersionReference_ResumesAfterBuild()
    {
        using var testContext = this.CreateTestContext( new TestContextOptions { Timeout = TimeSpan.FromSeconds( 60 ) } );

        var aspectAssemblyName = "aspect_" + RandomIdGenerator.GenerateId();
        var targetAssemblyName = "target_" + RandomIdGenerator.GenerateId();

        Compilation CreateAspectCompilation( int version )
            => testContext.CreateCSharpCompilation(
                new Dictionary<string, string> { ["Aspect.cs"] = _aspectCode.Replace( "$version$", version.ToString() ) },
                assemblyName: aspectAssemblyName );

        Compilation CreateTargetCompilation( Compilation aspectCompilation )
            => testContext.CreateCSharpCompilation(
                new Dictionary<string, string> { ["Target.cs"] = _targetCode },
                assemblyName: targetAssemblyName,
                additionalReferences: [aspectCompilation.ToMetadataReference()] );

        var aspectCompilation1 = CreateAspectCompilation( 1 );
        var targetCompilation1 = CreateTargetCompilation( aspectCompilation1 );

        var aspectOptions = CreateOptions( testContext, "Aspect" );
        var targetOptions = CreateOptions( testContext, "Target" );

        // The projects have been built once before the IDE loads them.
        SimulateBuild( aspectOptions.BuildTouchFile );
        SimulateBuild( targetOptions.BuildTouchFile );

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );
        factory.SetProjectOptions( ProjectKeyFactory.FromCompilation( aspectCompilation1 ), aspectOptions );
        factory.SetProjectOptions( ProjectKeyFactory.FromCompilation( targetCompilation1 ), targetOptions );

        var aspectPipeline = factory.CreatePipeline( aspectCompilation1 );
        var targetPipeline = factory.CreatePipeline( targetCompilation1 );

        Assert.True( factory.TryExecute( targetOptions, targetCompilation1, default, out _ ) );

        // The compile-time code changes. Both pipelines pause.
        var aspectCompilation2 = CreateAspectCompilation( 2 );
        var targetCompilation2 = CreateTargetCompilation( aspectCompilation2 );

        Assert.True( factory.TryExecute( targetOptions, targetCompilation2, default, out _ ) );
        await aspectPipeline.ProcessJobQueueWhenLockAvailableAsync();
        await targetPipeline.ProcessJobQueueWhenLockAvailableAsync();

        Assert.Equal( DesignTimeAspectPipelineStatus.Paused, aspectPipeline.Status );
        Assert.Equal( DesignTimeAspectPipelineStatus.Paused, targetPipeline.Status );

        var aspectResumption = new ResumptionAwaiter( factory.EventHub, aspectPipeline );
        var targetResumption = new ResumptionAwaiter( factory.EventHub, targetPipeline );

        // The user builds the solution. MSBuild builds the referenced project first.
        SimulateBuild( aspectOptions.BuildTouchFile );
        SimulateBuild( targetOptions.BuildTouchFile );

        await aspectResumption.WaitAsync( testContext.CancellationToken );
        await targetResumption.WaitAsync( testContext.CancellationToken );

        Assert.NotEqual( DesignTimeAspectPipelineStatus.Paused, aspectPipeline.Status );
        Assert.NotEqual( DesignTimeAspectPipelineStatus.Paused, targetPipeline.Status );
    }

    /// <summary>
    /// A project that references a project processed by another version of Metalama, whose compile-time code changes.
    /// The referencing project pauses because the referenced one is paused, but the two pipelines belong to different
    /// pipeline factories and event hubs, so the resumption of the referenced project does not reach the referencing
    /// one. The referencing project can only resume through its own touch file.
    /// </summary>
    [Fact]
    public async Task OtherVersionReference_ResumesAfterBuild()
    {
        using var testContext = this.CreateTestContext( new TestContextOptions { Timeout = TimeSpan.FromSeconds( 60 ) } );

        var workspaceProvider = new TestWorkspaceProvider( testContext.ServiceProvider );

        var entryPointManager = new DesignTimeEntryPointManager();
        var consumer = entryPointManager.GetConsumer( CurrentContractVersions.All );

        var serviceProvider = testContext.ServiceProvider.Global.Underlying.WithUntypedService( typeof(IDesignTimeEntryPointConsumer), consumer )
            .WithService( workspaceProvider );

        // Each factory creates its own event hub, as two versions of Metalama loaded in the same process do.
        using var currentVersionFactory = new TestDesignTimeAspectPipelineFactory( testContext, serviceProvider );
        var currentVersionServiceProvider = new CompilerServiceProvider( version: TestMetalamaProjectClassifier.CurrentMetalamaVersion );
        currentVersionServiceProvider.Initialize( serviceProvider.WithService( currentVersionFactory ) );
        entryPointManager.RegisterServiceProvider( currentVersionServiceProvider );

        using var otherVersionFactory = new TestDesignTimeAspectPipelineFactory( testContext, serviceProvider );
        var otherVersionServiceProvider = new CompilerServiceProvider( version: TestMetalamaProjectClassifier.OtherMetalamaVersion );
        otherVersionServiceProvider.Initialize( serviceProvider.WithService( otherVersionFactory ) );
        entryPointManager.RegisterServiceProvider( otherVersionServiceProvider );

        Assert.NotSame( currentVersionFactory.EventHub, otherVersionFactory.EventHub );

        string[] masterPreprocessorSymbols = ["METALAMA", TestMetalamaProjectClassifier.OtherMetalamaVersionPreprocessorSymbol];

        var masterProjectKey = workspaceProvider.AddOrUpdateProject(
            testContext,
            "master",
            new Dictionary<string, string> { ["master.cs"] = _aspectCode.Replace( "$version$", "1" ) },
            preprocessorSymbols: masterPreprocessorSymbols );

        var dependentProjectKey = workspaceProvider.AddOrUpdateProject(
            testContext,
            "dependent",
            new Dictionary<string, string> { ["dependent.cs"] = _targetCode },
            projectReferences: ["master"],
            preprocessorSymbols: ["METALAMA"] );

        var masterOptions = CreateOptions( testContext, "master" );
        var dependentOptions = CreateOptions( testContext, "dependent" );

        // Each version of Metalama may create a pipeline for either project, so both factories receive both options.
        foreach ( var factory in new[] { currentVersionFactory, otherVersionFactory } )
        {
            factory.SetProjectOptions( masterProjectKey, masterOptions );
            factory.SetProjectOptions( dependentProjectKey, dependentOptions );
        }

        // The projects have been built once before the IDE loads them.
        SimulateBuild( masterOptions.BuildTouchFile );
        SimulateBuild( dependentOptions.BuildTouchFile );

        var dependentCompilation1 = (await workspaceProvider.GetCompilationAsync( dependentProjectKey, testContext.CancellationToken ))!;
        var result1 = await currentVersionFactory.ExecuteAsync( dependentCompilation1, AsyncExecutionContext.Get(), testContext.CancellationToken );
        Assert.True( result1.IsSuccessful );

        Assert.True( currentVersionFactory.TryGetPipeline( dependentProjectKey, out var dependentPipeline ) );
        Assert.NotEqual( DesignTimeAspectPipelineStatus.Paused, dependentPipeline.Status );

        // The compile-time code of the master project changes.
        workspaceProvider.AddOrUpdateProject(
            testContext,
            "master",
            new Dictionary<string, string> { ["master.cs"] = _aspectCode.Replace( "$version$", "2" ) },
            preprocessorSymbols: masterPreprocessorSymbols );

        var dependentCompilation2 = (await workspaceProvider.GetCompilationAsync( dependentProjectKey, testContext.CancellationToken ))!;
        var result2 = await currentVersionFactory.ExecuteAsync( dependentCompilation2, AsyncExecutionContext.Get(), testContext.CancellationToken );
        Assert.True( result2.IsSuccessful );

        Assert.True( otherVersionFactory.TryGetPipeline( masterProjectKey, out var masterPipeline ) );

        Assert.Equal( DesignTimeAspectPipelineStatus.Paused, masterPipeline.Status );
        Assert.Equal( DesignTimeAspectPipelineStatus.Paused, dependentPipeline.Status );

        var masterResumption = new ResumptionAwaiter( otherVersionFactory.EventHub, masterPipeline );
        var dependentResumption = new ResumptionAwaiter( currentVersionFactory.EventHub, dependentPipeline );

        // The user builds the solution. MSBuild builds the referenced project first.
        SimulateBuild( masterOptions.BuildTouchFile );
        SimulateBuild( dependentOptions.BuildTouchFile );

        await masterResumption.WaitAsync( testContext.CancellationToken );

        // The resumption of the master pipeline does not reach the dependent pipeline, because the event hubs are
        // different. The dependent pipeline must resume because the build has updated its own touch file.
        await dependentResumption.WaitAsync( testContext.CancellationToken );

        Assert.NotEqual( DesignTimeAspectPipelineStatus.Paused, dependentPipeline.Status );
    }

    /// <summary>
    /// Project options that give a project its own build touch file.
    /// </summary>
    private sealed class ProjectOptionsWithBuildTouchFile : ProjectOptionsWrapper
    {
        public ProjectOptionsWithBuildTouchFile( IProjectOptions wrapped, string buildTouchFile ) : base( wrapped )
        {
            this.BuildTouchFile = buildTouchFile;
        }

        public override string BuildTouchFile { get; }
    }
}
