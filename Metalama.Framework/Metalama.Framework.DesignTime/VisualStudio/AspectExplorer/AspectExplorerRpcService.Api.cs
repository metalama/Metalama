// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.DesignTime.AspectExplorer;
using Metalama.Framework.DesignTime.Rpc;

namespace Metalama.Framework.DesignTime.VisualStudio.AspectExplorer;

internal sealed partial class AspectExplorerRpcService
{
    private sealed class Api : IAspectExplorerRpcApi
    {
        private readonly AspectExplorerRpcService _parent;

        public Api( AspectExplorerRpcService parent )
        {
            this._parent = parent;
        }

        public Task<IReadOnlyList<string>> GetAspectClassesAsync( ProjectKey projectKey, CancellationToken cancellationToken )
        {
            return this._parent._aspectDatabase.GetAspectClassesAsync( projectKey, cancellationToken );
        }

        public Task<IReadOnlyList<AspectDatabaseAspectInstance>> GetAspectInstancesAsync(
            ProjectKey projectKey,
            string aspectClassAssembly,
            string aspectClassId,
            CancellationToken cancellationToken )
        {
            return this._parent._aspectDatabase.GetAspectInstancesAsync(
                projectKey,
                aspectClassAssembly,
                new SerializableTypeId( aspectClassId ),
                cancellationToken );
        }
    }
}