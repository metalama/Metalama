// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Framework.Services;
using System;

namespace Metalama.Framework.Engine.Services;

internal abstract class CompilationServiceProvider<T> : IProjectService, IDisposable
    where T : ICompilationService
{
    protected ProjectServiceProvider ServiceProvider { get; }

    private readonly WeakCache<CompilationContext, T> _cache = new();

    protected CompilationServiceProvider( in ProjectServiceProvider serviceProvider )
    {
        this.ServiceProvider = serviceProvider;
    }

    public T Get( CompilationContext compilationContext )
        => this._cache.GetOrAdd( compilationContext, this.Create );

    protected abstract T Create( CompilationContext compilationContext );

    public void Dispose() => this._cache.Dispose();
}