// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Testing.AspectTesting;
using System.Collections.Immutable;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.TestFramework;

internal sealed class FakeMetadataReader : ITestAssemblyMetadataReader
{
    private readonly string _projectDirectory;

    public FakeMetadataReader( string projectDirectory )
    {
        this._projectDirectory = projectDirectory;
    }

    public TestAssemblyMetadata GetMetadata( IAssemblyInfo assembly )
        => new(
            this._projectDirectory,
            this._projectDirectory,
            ImmutableArray<string>.Empty,
            "net6.0",
            false,
            ImmutableArray<TestAssemblyReference>.Empty,
            ImmutableArray<TestAssemblyReference>.Empty,
            ImmutableArray<TestAssemblyReference>.Empty,
            ImmutableArray<string>.Empty, 
            null,
            ImmutableArray<string>.Empty );
}