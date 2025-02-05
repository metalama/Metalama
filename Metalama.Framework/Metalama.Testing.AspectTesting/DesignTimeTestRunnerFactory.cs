// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using Xunit.Abstractions;

namespace Metalama.Testing.AspectTesting;

internal sealed class DesignTimeTestRunnerFactory : ITestRunnerFactory
{
    public BaseTestRunner CreateTestRunner(
        GlobalServiceProvider serviceProvider,
        string? projectDirectory,
        TestProjectReferences references,
        ITestOutputHelper? logger )
        => new DesignTimeTestRunner( serviceProvider, projectDirectory, references, logger );
}