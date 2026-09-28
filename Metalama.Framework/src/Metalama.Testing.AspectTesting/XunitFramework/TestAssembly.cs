// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using Xunit.Sdk;

namespace Metalama.Testing.AspectTesting.XunitFramework;

internal sealed class TestAssembly : ITestAssembly
{
    public TestAssembly( TestFactory factory )
    {
        this.AssemblyName = factory.Assembly.FullName ?? factory.Assembly.GetName().Name ?? "";
        this.AssemblyPath = factory.Assembly.Location;
        this.ModuleVersionID = factory.Assembly.ManifestModule.ModuleVersionId;
        this.UniqueID = UniqueIDGenerator.ForAssembly( this.AssemblyPath, null );
    }

    public string AssemblyName { get; }

    public string AssemblyPath { get; }

    public string? ConfigFilePath => null;

    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => TestFactory.EmptyTraits;

    public string UniqueID { get; }

    public Guid ModuleVersionID { get; }
}
