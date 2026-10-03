// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.AspectTesting;
using Metalama.Testing.UnitTesting;
using SharpCrafters.Backstage.Infrastructure;
using SharpCrafters.Backstage.Testing;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace Metalama.Framework.Tests.UnitTests.TestFramework;

/// <summary>
/// Tests of the comparison of the program output (<c>.t.txt</c>) by <see cref="AspectTestRunner"/>.
/// </summary>
/// <remarks>
/// A test whose expected program output is not empty must fail when its program is not executed, for instance because the program class or
/// its main method was renamed. Otherwise the expected output silently stops being verified. The runner executes no program on .NET Framework
/// and in a test that disables the execution, so the check does not apply there.
/// </remarks>
public sealed class AspectTestRunnerProgramOutputTests : UnitTestClass
{
#if NET5_0_OR_GREATER
    private const bool _runnerExecutesPrograms = true;
#else
    private const bool _runnerExecutesPrograms = false;
#endif

    /// <summary>
    /// The test code. Its method is not named <c>Main</c>, so the runner does not find the program unless the test sets the main method.
    /// </summary>
    private const string _code = """
                                 public class Program
                                 {
                                     public static void TestMain() => System.Console.WriteLine( "hello" );
                                 }
                                 """;

    [Fact]
    public async Task NonEmptyExpectedOutput_ProgramNotExecuted_Fails()
    {
        Assert.SkipUnless( _runnerExecutesPrograms, "The runner executes no program on .NET Framework." );

        var exception = await Assert.ThrowsAsync<FailException>( () => this.RunAsync( _code, "hello" ) );

        Assert.Contains( "the program of the test was not executed", exception.Message, StringComparison.Ordinal );
        Assert.Contains( "'Main'", exception.Message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task NonEmptyExpectedOutput_ProgramExecuted_Passes()
    {
        Assert.SkipUnless( _runnerExecutesPrograms, "The runner executes no program on .NET Framework." );

        await this.RunAsync( _code, "hello", options => options.MainMethod = "TestMain" );
    }

    /// <summary>
    /// Verifies that a program that is executed and writes nothing does not match a non-empty expected output.
    /// </summary>
    [Fact]
    public async Task NonEmptyExpectedOutput_ProgramWritesNothing_Fails()
    {
        Assert.SkipUnless( _runnerExecutesPrograms, "The runner executes no program on .NET Framework." );

        const string silentCode = """
                                  public class Program
                                  {
                                      public static void TestMain() { }
                                  }
                                  """;

        await Assert.ThrowsAnyAsync<XunitException>( () => this.RunAsync( silentCode, "hello", options => options.MainMethod = "TestMain" ) );
    }

    [Fact]
    public async Task NonEmptyExpectedOutput_Net48_Skipped()
    {
        Assert.SkipWhen( _runnerExecutesPrograms, "The runner executes programs on .NET." );

        await this.RunAsync( _code, "hello" );
    }

    [Fact]
    public async Task NonEmptyExpectedOutput_DisableExecuteProgram_Skipped()
        => await this.RunAsync( _code, "hello", options => options.ExecuteProgram = false );

    /// <summary>
    /// Verifies that the check does not apply to a test whose output is not compiled, because the runner does not execute its program, even when
    /// the test sets the main method.
    /// </summary>
    [Fact]
    public async Task NonEmptyExpectedOutput_OutputCompilationDisabled_Skipped()
        => await this.RunAsync(
            _code,
            "hello",
            options =>
            {
                options.MainMethod = "TestMain";
                options.OutputCompilationDisabled = true;
            } );

    /// <summary>
    /// Runs a test file through <see cref="AspectTestRunner"/> on a virtual file system, with the given expected program output.
    /// </summary>
    private async Task RunAsync( string code, string expectedProgramOutput, Action<TestOptions>? configureOptions = null )
    {
        using var testContext = this.CreateTestContext();
        var fileSystem = new TestFileSystem( testContext.ServiceProvider.Underlying );
        var directory = Path.Combine( Environment.CurrentDirectory, "tests" );

        fileSystem.CreateDirectory( directory );
        fileSystem.WriteAllText( Path.Combine( directory, "Test.cs" ), code );
        fileSystem.WriteAllText( Path.Combine( directory, "Test.t.cs" ), code );
        fileSystem.WriteAllText( Path.Combine( directory, "Test.t.txt" ), expectedProgramOutput );

        var serviceProvider = (GlobalServiceProvider) testContext.ServiceProvider.Global.Underlying
            .WithUntypedService( typeof(IFileSystem), fileSystem )
            .WithService( new FakeMetadataReader( directory ) );

        var testProjectReferences = new TestProjectReferences(
            testContext.GetMetadataReferences().ToImmutableArray(),
            ImmutableArray<TargetedAssemblyReference>.Empty,
            ImmutableArray<TargetedAssemblyReference>.Empty,
            ImmutableArray<string>.Empty,
            null );

        var testProjectProperties = new TestProjectProperties(
            assemblyName: null,
            directory,
            directory,
            ImmutableArray<string>.Empty,
            "net10.0",
            "net10.0",
            ImmutableArray<string>.Empty );

        var testDirectoryOptionsReader = new TestDirectoryOptionsReader( serviceProvider, directory );
        var testRunner = new AspectTestRunner( serviceProvider, directory, testProjectReferences, null );
        var testInput = new TestInput.Factory( serviceProvider ).FromFile( testProjectProperties, testDirectoryOptionsReader, "Test.cs" );
        configureOptions?.Invoke( testInput.Options );
        var testContextOptions = testInput.Options.ApplyToTestContextOptions( new MetalamaTestContextOptions() );
        testInput.Options.SkipDiffTool = true;

        await testRunner.RunAndAssertAsync( testInput, testContextOptions, TestContext.Current.CancellationToken );
    }
}
