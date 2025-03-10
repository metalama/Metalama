// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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