// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Engine.Utilities.Caching;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Options;

// ReSharper disable once InconsistentNaming
public sealed class MSBuildProjectOptionsFactory : IDisposable, IProjectOptionsFactory
{
    private readonly TimeBasedCache<AnalyzerConfigOptions, MSBuildProjectOptions> _cache;

    public MSBuildProjectOptionsFactory() : this( null ) { }
    
    public MSBuildProjectOptionsFactory( IEnumerable<string>? relevantProperties )
    {
        this._cache = new TimeBasedCache<AnalyzerConfigOptions, MSBuildProjectOptions>(
            TimeSpan.FromMinutes( 10 ),
            new AnalyzerConfigOptionsComparer( relevantProperties ) );
    }

    public IProjectOptions GetProjectOptions(
        AnalyzerConfigOptions options,
        TransformerOptions? transformerOptions = null )
    {
        if ( transformerOptions != null )
        {
            // We have a source transformer. Caching is useless.
            return new MSBuildProjectOptions( options, transformerOptions );
        }
        else
        {
            // At design time, we should try to cache.
            return this._cache.GetOrAdd( options, o => new MSBuildProjectOptions( o ) );
        }
    }
    
    public void Dispose() => this._cache.Dispose();
}