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
/// Tests for issue #754: Test compilations should not receive references to the complete test project.
/// Test compilations (individual test input files) should only receive references that a real user project
/// would have, not the full set of references from the test project (which includes xUnit, test infrastructure, etc.).
/// </summary>
public sealed class TestCompilationReferencesTests : UnitTestClass
{
    [Fact]
    public void CreateProjectReceivesOnlyProvidedReferences()
    {
        // Issue #754: Verify that BaseTestRunner.CreateProject passes through the references
        // from TestProjectReferences to the test compilation. The actual filtering of test
        // infrastructure assemblies happens in the MSBuild targets (MetalamaTestExcludedReference),
        // so CreateProject should faithfully use whatever references it receives.

        using var testContext = this.CreateTestContext();
        var fileSystem = new TestFileSystem( testContext.ServiceProvider.Underlying );
        var directory = Path.Combine( Environment.CurrentDirectory, "tests_754" );
        fileSystem.CreateDirectory( directory );

        var serviceProvider = (GlobalServiceProvider) testContext.ServiceProvider.Global.Underlying
            .WithUntypedService( typeof(IFileSystem), fileSystem )
            .WithService( new FakeMetadataReader( directory ) );

        // Use only the curated metadata references (no test infrastructure).
        var metadataReferences = testContext.GetMetadataReferences().ToImmutableArray();

        var testProjectReferences = new TestProjectReferences(
            metadataReferences,
            ImmutableArray<TargetedAssemblyReference>.Empty,
            ImmutableArray<TargetedAssemblyReference>.Empty,
            ImmutableArray<string>.Empty,
            null );

        var testRunner = new AspectTestRunner( serviceProvider, directory, testProjectReferences );
        var project = testRunner.CreateProject( testContext, new TestOptions() );

        var referenceFileNames = project.MetadataReferences
            .OfType<PortableExecutableReference>()
            .Where( r => r.FilePath != null )
            .Select( r => Path.GetFileName( r.FilePath )! )
            .ToHashSet( StringComparer.OrdinalIgnoreCase );

        // When properly filtered references are provided (as the MSBuild targets now ensure),
        // the test compilation should not contain xUnit or other test infrastructure assemblies.
        Assert.DoesNotContain( referenceFileNames,
            name => name.StartsWith( "xunit", StringComparison.OrdinalIgnoreCase ) );

        // But it should contain Metalama.Framework (user-facing API).
        Assert.Contains( referenceFileNames,
            name => name.StartsWith( "Metalama.Framework", StringComparison.OrdinalIgnoreCase ) );
    }
}
#endif
