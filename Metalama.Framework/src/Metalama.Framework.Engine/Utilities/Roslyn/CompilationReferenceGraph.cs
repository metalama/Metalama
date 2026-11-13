// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Utilities.Roslyn
{
    internal sealed class CompilationReferenceGraph
    {
        private static readonly WeakCache<Compilation, CompilationReferenceGraph> _instances = new( isStaticCache: true );
        private readonly Dictionary<IAssemblySymbol, (int Min, int Max)> _depth = new();
        private readonly ICompilationReferenceGraphObserver? _observer;

        public static CompilationReferenceGraph GetInstance( Compilation compilation, in ProjectServiceProvider serviceProvider )
        {
            var observer = serviceProvider.GetService<ICompilationReferenceGraphObserver>();

            return _instances.GetOrAdd( compilation, c => new CompilationReferenceGraph( c, observer ) );
        }

        private CompilationReferenceGraph( Compilation compilation, ICompilationReferenceGraphObserver? observer )
        {
            this._observer = observer;

            this.Visit(
                compilation.Assembly,
                0,
                ImmutableHashSet<IAssemblySymbol>.Empty.WithComparer( compilation.GetCompilationContext().SymbolComparer ) );
        }

        public (int Min, int Max) GetDepth( IAssemblySymbol assembly ) => this._depth[assembly];

        private void Visit( IAssemblySymbol assembly, int depth, ImmutableHashSet<IAssemblySymbol> processedAssemblies )
        {
            if ( assembly.Name.Equals( "System", StringComparison.OrdinalIgnoreCase ) ||
                 assembly.Name.Equals( "mscorlib", StringComparison.OrdinalIgnoreCase ) ||
                 assembly.Name.StartsWith( "System.", StringComparison.OrdinalIgnoreCase ) ||
                 assembly.Name.Equals( "PresentationFramework", StringComparison.OrdinalIgnoreCase ) )
            {
                // We are not interested in system assemblies, and here a cheap trick to exclude them.
                return;
            }

            this._observer?.OnAssemblyVisited( assembly, depth );

            if ( !this._depth.TryGetValue( assembly, out var depthRange ) )
            {
                depthRange = (depth, depth);
            }
            else if ( depth >= depthRange.Min && depth <= depthRange.Max )
            {
                // The references of this assembly were already walked at a depth at most this depth and at a depth at least this depth,
                // so their depth ranges already contain every depth that walking them again would give. Walking them again on each path
                // would make the analysis exponential in the number of diamonds of the graph (see #2330).
                return;
            }
            else
            {
                depthRange = (Math.Min( depthRange.Min, depth ), Math.Max( depthRange.Max, depth ));
            }

            this._depth[assembly] = depthRange;

            this._observer?.OnAssemblyExpanded( assembly, depth );

            var newProcessedAssemblies = processedAssemblies.Add( assembly );

            foreach ( var reference in assembly.Modules.SelectMany( m => m.ReferencedAssemblySymbols ) )
            {
                if ( !newProcessedAssemblies.Contains( reference ) )
                {
                    this.Visit( reference, depth + 1, newProcessedAssemblies );
                }
                else
                {
                    Logger.LoggerFactory.GetLogger( "CompilationReferenceGraph" )
                        .Error?.Log(
                            $"Circular reference found with '{reference.Identity}' referenced by '{assembly.Identity}'. "
                            + "Assemblies in the closure: " + string.Join( ", ", newProcessedAssemblies ) );
                }
            }
        }
    }
}