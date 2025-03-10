// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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