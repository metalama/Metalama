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
    public void CreateProjectShouldNotContainTestInfrastructureReferences()
    {
        // Issue #754: BaseTestRunner.CreateProject passes all references from TestProjectReferences
        // to the test compilation without filtering. In a real scenario, TestProjectReferences is
        // populated from assemblies.txt which contains ALL of @(ReferencePathWithRefAssemblies),
        // including test infrastructure assemblies (xUnit, FakeItEasy, etc.) that would not be
        // available in a real user project.

        using var testContext = this.CreateTestContext();
        var fileSystem = new TestFileSystem( testContext.ServiceProvider.Underlying );
        var directory = Path.Combine( Environment.CurrentDirectory, "tests_754" );
        fileSystem.CreateDirectory( directory );

        var serviceProvider = (GlobalServiceProvider) testContext.ServiceProvider.Global.Underlying
            .WithUntypedService( typeof(IFileSystem), fileSystem )
            .WithService( new FakeMetadataReader( directory ) );

        var metadataReferences = testContext.GetMetadataReferences().ToImmutableArray();

        // Add a reference to xUnit, simulating what assemblies.txt provides.
        // xUnit is used by the test project but should not be visible to test input files.
        var xunitRef = MetadataReference.CreateFromFile( typeof(FactAttribute).Assembly.Location );
        var allRefs = metadataReferences.Add( xunitRef );

        var testProjectReferences = new TestProjectReferences(
            allRefs,
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

        // The test compilation should NOT contain references to xUnit.
        // xUnit is needed by the test project itself but should not be visible
        // to individual test input files, as real user projects don't reference xUnit.
        Assert.DoesNotContain( referenceFileNames,
            name => name.StartsWith( "xunit", StringComparison.OrdinalIgnoreCase ) );
    }
}
#endif
