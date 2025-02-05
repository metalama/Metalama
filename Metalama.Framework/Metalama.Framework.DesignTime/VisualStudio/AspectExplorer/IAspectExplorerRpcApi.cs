// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.DesignTime.AspectExplorer;
using Metalama.Framework.DesignTime.Rpc;

namespace Metalama.Framework.DesignTime.VisualStudio.AspectExplorer;

internal interface IAspectExplorerRpcApi : IRpcApi
{
    /// <summary>
    /// Gets the aspect classes in a project, represented using the string form of <see cref="SerializableTypeId"/>.
    /// </summary>
    Task<IReadOnlyList<string>> GetAspectClassesAsync( ProjectKey projectKey, [UsedImplicitly] CancellationToken cancellationToken = default );

    /// <summary>
    /// Gets the aspect instances for an aspect class in a project.
    /// <paramref name="aspectClassId"/> is the string form of <see cref="SerializableTypeId"/>.
    /// </summary>
    Task<IReadOnlyList<AspectDatabaseAspectInstance>> GetAspectInstancesAsync(
        ProjectKey projectKey,
        string aspectClassAssembly,
        string aspectClassId,
        [UsedImplicitly] CancellationToken cancellationToken = default );
}