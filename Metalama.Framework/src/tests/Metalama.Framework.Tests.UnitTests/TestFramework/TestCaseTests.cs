// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Metalama.Testing.AspectTesting;
using Metalama.Testing.AspectTesting.XunitFramework;
using Metalama.Testing.UnitTesting;
using SharpCrafters.Backstage.Infrastructure;
using SharpCrafters.Backstage.Testing;
using System;
using System.Collections.Immutable;
using System.IO;
using Xunit;
using Xunit.Sdk;

namespace Metalama.Framework.Tests.UnitTests.TestFramework;

/// <summary>
/// Tests the <see cref="TestCase"/> of the aspect test framework and its <see cref="TestFactory"/>.
/// </summary>
public sealed class TestCaseTests : UnitTestClass
{
    /// <summary>
    /// Verifies that a test case survives the serialization by which VSTest passes a selection of tests to the executor,
    /// and that its deserialization reuses the <see cref="TestFactory"/> of the assembly instead of creating a service
    /// provider.
    /// </summary>
    /// <remarks>
    /// <see cref="TestFactory.GetInstance(GlobalServiceProvider, System.Reflection.Assembly)"/> caches the factory per
    /// assembly for the lifetime of the process. This is the only test that registers a factory for the assembly of the
    /// unit tests.
    /// </remarks>
    [Fact]
    public void SerializationRoundTrip_ReusesTheFactoryOfTheAssembly()
    {
        using var testContext = this.CreateTestContext();
        var (serviceProvider, directory, fileSystem) = CreateServices( testContext );
        fileSystem.CreateDirectory( Path.Combine( directory, "A" ) );
        fileSystem.WriteAllText( Path.Combine( directory, "A", "T1.cs" ), "/* Empty */" );

        var assembly = typeof(TestCaseTests).Assembly;
        var factory = TestFactory.GetInstance( serviceProvider, assembly );

        Assert.Same(
            factory,
            TestFactory.GetInstance( () => throw new InvalidOperationException( "The service provider must not be created again." ), assembly ) );

        var testCase = new TestCase( factory, Path.Combine( "A", "T1.cs" ) );

        var serialized = SerializationHelper.Instance.Serialize( testCase );
        var deserialized = Assert.IsType<TestCase>( SerializationHelper.Instance.Deserialize( serialized ) );

        Assert.Equal( testCase.RelativePath, deserialized.RelativePath );
        Assert.Equal( testCase.UniqueID, deserialized.UniqueID );
        Assert.Same( testCase.TestMethod, deserialized.TestMethod );
        Assert.Equal( Path.Combine( directory, "A", "T1.cs" ), deserialized.FullPath );
    }

    /// <summary>
    /// Verifies that <see cref="TestCase.SkipReason"/> reads the test file once, because xunit reads the property several
    /// times per test.
    /// </summary>
    [Fact]
    public void SkipReason_IsReadOnce()
    {
        using var testContext = this.CreateTestContext();
        var (serviceProvider, directory, fileSystem) = CreateServices( testContext );
        var path = Path.Combine( directory, "Skipped.cs" );
        fileSystem.WriteAllText( path, "#if TEST_OPTIONS\n// @Skipped(The reason)\n#endif" );

        var factory = new TestFactory(
            serviceProvider,
            CreateProjectProperties( directory ),
            new TestDirectoryOptionsReader( serviceProvider, directory ),
            typeof(TestCaseTests).Assembly );

        var testCase = new TestCase( factory, "Skipped.cs" );

        Assert.Equal( "The reason", testCase.SkipReason );

        // A second read of the file would find no skip reason.
        fileSystem.WriteAllText( path, "/* Empty */" );

        Assert.Equal( "The reason", testCase.SkipReason );
    }

    /// <summary>
    /// Verifies that <see cref="TestCase.SkipReason"/> is <c>null</c> for a test that is not skipped, as the contract of
    /// xunit requires.
    /// </summary>
    [Fact]
    public void SkipReason_WhenNotSkipped_IsNull()
    {
        using var testContext = this.CreateTestContext();
        var (serviceProvider, directory, fileSystem) = CreateServices( testContext );
        fileSystem.WriteAllText( Path.Combine( directory, "NotSkipped.cs" ), "/* Empty */" );

        var factory = new TestFactory(
            serviceProvider,
            CreateProjectProperties( directory ),
            new TestDirectoryOptionsReader( serviceProvider, directory ),
            typeof(TestCaseTests).Assembly );

        var testCase = new TestCase( factory, "NotSkipped.cs" );

        Assert.Null( testCase.SkipReason );
    }

    private static (GlobalServiceProvider ServiceProvider, string Directory, TestFileSystem FileSystem) CreateServices( MetalamaTestContext testContext )
    {
        var fileSystem = new TestFileSystem( testContext.ServiceProvider.Underlying );
        var directory = Path.Combine( Environment.CurrentDirectory, "tests" );
        fileSystem.CreateDirectory( directory );

        var serviceProvider = (GlobalServiceProvider) testContext.ServiceProvider.Global.Underlying
            .WithUntypedService( typeof(IFileSystem), fileSystem )
            .WithService( new FakeMetadataReader( directory ) );

        return (serviceProvider, directory, fileSystem);
    }

    private static TestProjectProperties CreateProjectProperties( string directory )
        => new(
            assemblyName: null,
            directory,
            directory,
            ImmutableArray<string>.Empty,
            "net10.0",
            "net10.0",
            ImmutableArray<string>.Empty );
}
