// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Testing.UnitTesting;

internal sealed class TestExtensionLoader : IExtensionLoader
{
    private readonly TestContextOptions _testContextOptions;

    public TestExtensionLoader( TestContextOptions testContextOptions )
    {
        this._testContextOptions = testContextOptions;
    }

    public IEnumerable<Type> GetExtensionTypes( IProjectOptions projectOptions, CompileTimeDomain domain, ExtensionKind extensionKind )
    {
        var (extensionTypes, extensionAssemblies) = extensionKind == ExtensionKind.Default
            ? (this._testContextOptions.ExtensionTypes, this._testContextOptions.ExtensionAssemblies)
            : (this._testContextOptions.DesignTimeExtensionTypes, this._testContextOptions.DesignTimeExtensionAssemblies);

        return extensionTypes.Concat( ExtensionLoaderHelper.LoadExtensionTypes( domain, extensionKind, extensionAssemblies ) );
    }
}