// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.Services;
using Xunit.Abstractions;

namespace Metalama.Testing.AspectTesting;

[UsedImplicitly]
internal class EndOfLineTestRunnerFactory : ITestRunnerFactory
#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1402 // File may only contain a single type
{
    public BaseTestRunner CreateTestRunner(
        GlobalServiceProvider serviceProvider,
        string? projectDirectory,
        TestProjectReferences references,
        ITestOutputHelper? logger )
        => new OutputFormatterAspectTestRunner( serviceProvider, projectDirectory, references, logger );
}