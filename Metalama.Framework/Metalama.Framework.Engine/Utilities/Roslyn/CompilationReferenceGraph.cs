// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
        private static readonly WeakCache<Compilation, CompilationReferenceGraph> _instances = new();
        private readonly Dictionary<IAssemblySymbol, (int Min, int Max)> _depth = new();

        public static CompilationReferenceGraph GetInstance( Compilation compilation )
            => _instances.GetOrAdd( compilation, c => new CompilationReferenceGraph( c ) );

        private CompilationReferenceGraph( Compilation compilation )
        {
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

            if ( !this._depth.TryGetValue( assembly, out var depthRange ) )
            {
                depthRange = (depth, depth);
            }
            else
            {
                depthRange = (Math.Min( depthRange.Min, depth ), Math.Max( depthRange.Max, depth ));
            }

            this._depth[assembly] = depthRange;

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