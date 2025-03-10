// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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