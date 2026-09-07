// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis.CSharp;

namespace Metalama.Testing.UnitTesting;

internal sealed class TestLanguageVersionProvider : ILanguageVersionProvider
{
    private readonly IProjectOptions _projectOptions;

    public TestLanguageVersionProvider( ProjectServiceProvider serviceProvider )
    {
        this._projectOptions = serviceProvider.GetRequiredService<IProjectOptions>();
    }

    /// <summary>
    /// Gets the language version at which the compile-time compilation of a test is parsed, which is the language
    /// version of the project under test.
    /// </summary>
    /// <remarks>
    /// No ceiling is applied, unlike <see cref="LanguageVersionProvider"/>. The compile-time compilation of a test is
    /// parsed by the Roslyn that the test assembly is bound to, and no software development kit takes part, so the
    /// version that the project requests is the version the compiler receives. A test that requests the preview
    /// language version therefore covers its compile-time code as well as its run-time code. See issue #1979.
    /// </remarks>
    public LanguageVersion GetCompileTimeLanguageVersion() => this._projectOptions.LanguageVersion;
}
