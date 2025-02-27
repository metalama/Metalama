// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Options;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Extensibility;

/// <summary>
/// An implementation of <see cref="IExtensionLoader"/> that loads assemblies based on the <see cref="IProjectOptions"/>.
/// </summary>
internal sealed class ExtensionLoader : IExtensionLoader
{
    public IEnumerable<Type> GetExtensionTypes( IProjectOptions projectOptions, CompileTimeDomain domain, ExtensionKind extensionKind )
    {
        var assemblies = extensionKind == ExtensionKind.Default
            ? projectOptions.ExtensionAssemblies
            : projectOptions.DesignTimeExtensionAssemblies;

        return ExtensionLoaderHelper.LoadExtensionTypes( domain, extensionKind, assemblies, projectOptions.AvoidLockingExtensionAssemblies );
    }
}