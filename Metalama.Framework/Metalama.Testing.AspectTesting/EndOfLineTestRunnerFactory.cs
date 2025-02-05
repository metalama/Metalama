// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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