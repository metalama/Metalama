// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Extensibility;

/// <summary>
/// An implementation of <see cref="IExtensionLoader"/> that loads assemblies based on the <see cref="IProjectOptions"/>.
/// </summary>
internal sealed class ExtensionLoader : ExtensionLoaderBase, IExtensionLoader
{
    public ExtensionLoader( GlobalServiceProvider serviceProvider ) : base( serviceProvider ) { }

    public IEnumerable<Type> GetExtensionTypes(
        IProjectOptions projectOptions,
        CompileTimeDomain domain,
        ExtensionKind extensionKind,
        IDiagnosticAdder diagnosticAdder )
    {
        var assemblies = extensionKind == ExtensionKind.Default
            ? projectOptions.ExtensionAssemblies
            : projectOptions.DesignTimeExtensionAssemblies;

        return this.DiscoverExtensionTypes( domain, extensionKind, assemblies, projectOptions.AvoidLockingExtensionAssemblies, diagnosticAdder );
    }
}