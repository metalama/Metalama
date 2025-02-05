// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.CompileTime;

internal sealed class ClassifyingCompilationContextFactory : IProjectService, IDisposable
{
    private readonly ProjectServiceProvider _serviceProvider;
    private readonly WeakCache<Compilation, ClassifyingCompilationContext> _instances = new();

    public ClassifyingCompilationContextFactory( in ProjectServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
    }

    public ClassifyingCompilationContext GetInstance( Compilation compilation ) => this._instances.GetOrAdd( compilation, this.GetInstanceCore );

    private ClassifyingCompilationContext GetInstanceCore( Compilation compilation ) => new( this._serviceProvider, compilation.GetCompilationContext() );

    public void Dispose() => this._instances.Dispose();
}