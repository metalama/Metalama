// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.Services;
#pragma warning disable CA1812 // Instantiated by reflection
using Xunit.Abstractions;

namespace Metalama.Testing.AspectTesting;

/// <summary>
/// A factory that creates <see cref="HtmlGenerationTestRunner"/> instances.
/// To use this runner in your tests, set <c>"TestRunnerFactoryType": "Metalama.Testing.AspectTesting.HtmlGenerationTestRunnerFactory"</c>
/// in your <c>metalamaTests.json</c> file.
/// </summary>
[UsedImplicitly]
internal class HtmlGenerationTestRunnerFactory : ITestRunnerFactory
{
    /// <inheritdoc />
    public BaseTestRunner CreateTestRunner(
        GlobalServiceProvider serviceProvider,
        string? projectDirectory,
        TestProjectReferences references,
        ITestOutputHelper? logger )
        => new HtmlGenerationTestRunner( serviceProvider, projectDirectory, references, logger );
}