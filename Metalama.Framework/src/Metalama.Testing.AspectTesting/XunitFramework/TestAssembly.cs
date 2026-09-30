// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using Xunit.Sdk;

namespace Metalama.Testing.AspectTesting.XunitFramework;

/// <summary>
/// Describes the test assembly to xunit.
/// </summary>
internal sealed class TestAssembly : ITestAssembly
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestAssembly"/> class.
    /// </summary>
    public TestAssembly( TestFactory factory )
    {
        this.AssemblyName = factory.Assembly.FullName ?? factory.Assembly.GetName().Name ?? "";
        this.AssemblyPath = factory.Assembly.Location;
        this.ModuleVersionID = factory.Assembly.ManifestModule.ModuleVersionId;
        this.UniqueID = UniqueIDGenerator.ForAssembly( this.AssemblyPath, null );
    }

    /// <inheritdoc />
    public string AssemblyName { get; }

    /// <inheritdoc />
    public string AssemblyPath { get; }

    /// <inheritdoc />
    public string? ConfigFilePath => null;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => TestFactory.EmptyTraits;

    /// <inheritdoc />
    public string UniqueID { get; }

    /// <inheritdoc />
    public Guid ModuleVersionID { get; }
}
