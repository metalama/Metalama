// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.DesignTime.Rpc;

namespace Metalama.Framework.DesignTime.VisualStudio.ServiceHub;

internal interface IProjectServiceProviderApi : IRpcApi
{
    Task<object> GetProjectServiceAsync( ProjectKey projectKey, Type serviceType, [UsedImplicitly] CancellationToken cancellationToken = default );
}