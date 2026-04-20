// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if NET5_0_OR_GREATER
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Testing;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.AspectTesting;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.TestFramework;

/// <summary>
/// Tests for issue #754: Test compilations should not receive a reference to the test project itself.
/// </summary>
public sealed class TestCompilationReferencesTests : UnitTestClass
{
    [Fact]
    public void CreateProjectDoesNotAddExtraReferences()
    {
        // Issue #754: Verify that BaseTestRunner.CreateProject passes through only the references
        // from TestProjectReferences, without adding extra assemblies.

        using var testContext = this.CreateTestContext();
        var fileSystem = new TestFileSystem( testContext.ServiceProvider.Underlying );
        var directory = Path.Combine( Environment.CurrentDirectory, "tests_754" );
        fileSystem.CreateDirectory( directory );

        var serviceProvider = (GlobalServiceProvider) testContext.ServiceProvider.Global.Underlying
            .WithUntypedService( typeof(IFileSystem), fileSystem )
            .WithService( new FakeMetadataReader( directory ) );

        var metadataReferences = testContext.GetMetadataReferences().ToImmutableArray();

        var testProjectReferences = new TestProjectReferences(
            metadataReferences,
            ImmutableArray<TargetedAssemblyReference>.Empty,
            ImmutableArray<TargetedAssemblyReference>.Empty,
            ImmutableArray<string>.Empty,
            null );

        var testRunner = new AspectTestRunner( serviceProvider, directory, testProjectReferences );
        var project = testRunner.CreateProject( testContext, new TestOptions() );

        var projectReferenceCount = project.MetadataReferences
            .OfType<PortableExecutableReference>()
            .Count( r => r.FilePath != null );

        var inputReferenceCount = metadataReferences.Count( r => r.FilePath != null );

        // CreateProject should not add references beyond what was provided.
        Assert.Equal( inputReferenceCount, projectReferenceCount );
    }
}
#endif