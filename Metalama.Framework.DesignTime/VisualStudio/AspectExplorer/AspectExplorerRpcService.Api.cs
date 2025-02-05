// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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