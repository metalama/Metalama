// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.AspectTesting;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.TemplateTests.Runner
{
    [UsedImplicitly]
    internal sealed class TemplatingTestRunnerFactory : ITestRunnerFactory
    {
        public BaseTestRunner CreateTestRunner(
            GlobalServiceProvider serviceProvider,
            string? projectDirectory,
            TestProjectReferences references,
            ITestOutputHelper? logger )
            => new TemplatingTestRunner( serviceProvider, projectDirectory, references, logger );
    }
}