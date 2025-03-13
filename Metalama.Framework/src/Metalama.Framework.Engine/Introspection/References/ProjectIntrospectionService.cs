// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Framework.Introspection;

namespace Metalama.Framework.Engine.Introspection.References;

#pragma warning disable CA1001
internal sealed class ProjectIntrospectionService : IProjectIntrospectionService
#pragma warning restore CA1001
{
    private readonly ProjectServiceProvider _serviceProvider;
    private readonly WeakCache<ICompilation, ProjectReferenceGraph> _cache = new();

    public ProjectIntrospectionService( ProjectServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
    }

    public IIntrospectionReferenceGraph GetReferenceGraph( ICompilation compilation )
    {
        // Lock to prevent concurrent evaluation.
        lock ( this._cache )
        {
            return this._cache.GetOrAdd( compilation, this.GetReferenceGraphCore );
        }
    }

    private ProjectReferenceGraph GetReferenceGraphCore( ICompilation compilation ) => new( this._serviceProvider, (CompilationModel) compilation );
}