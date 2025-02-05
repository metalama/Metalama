// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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