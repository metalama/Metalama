// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using System.Collections.Generic;

[assembly: ExportExtension( typeof( ServiceFactory.Extension.MyServiceFactory ), ExtensionKinds.ServiceFactory )]

namespace ServiceFactory.Extension;

/// <summary>
/// A factory that creates instances of <see cref="Contracts.IMyService"/>.
/// </summary>
public class MyServiceFactory : IProjectServiceFactory
{
    public IEnumerable<IProjectService> CreateServices( in ProjectServiceProvider serviceProvider )
    {
        return new IProjectService[] { new MyService() };
    }
}
