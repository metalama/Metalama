// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Caching;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Services
{
    public static class CompilationContextFactory
    {
        private static readonly WeakCache<Compilation, CompilationContext> _instances = new();

        public static CompilationContext GetCompilationContext( this Compilation compilation )
            => _instances.GetOrAdd( compilation, c => new CompilationContext( c ) );
    }
}