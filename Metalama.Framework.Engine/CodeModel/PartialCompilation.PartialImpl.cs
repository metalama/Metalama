// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Compiler;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel
{
    public abstract partial class PartialCompilation
    {
        /// <summary>
        /// Represents a partial compilation, containing a subset of syntax trees.
        /// </summary>
        private sealed class PartialImpl : PartialCompilation
        {
            private readonly ImmutableHashSet<INamedTypeSymbol>? _types;

            private readonly ImmutableHashSet<string>? _observedSyntaxTreePaths;

            public PartialImpl(
                CompilationContext compilationContext,
                ImmutableDictionary<string, SyntaxTree> syntaxTrees,
                ImmutableHashSet<string>? observedSyntaxTreePaths,
                ImmutableHashSet<INamedTypeSymbol>? types,
                Lazy<DerivedTypeIndex> derivedTypeIndex,
                ImmutableArray<ManagedResource> resources )
                : base( compilationContext, derivedTypeIndex, resources )
            {
                this._types = types;
                this.SyntaxTrees = syntaxTrees;
                this._observedSyntaxTreePaths = observedSyntaxTreePaths;

#if DEBUG
                this.CheckTrees();
#endif
            }

            private PartialImpl(
                ImmutableDictionary<string, SyntaxTree> syntaxTrees,
                ImmutableHashSet<string>? observedSyntaxTreePaths,
                ImmutableHashSet<INamedTypeSymbol>? types,
                PartialCompilation baseCompilation,
                IReadOnlyCollection<SyntaxTreeTransformation>? modifications,
                ImmutableArray<ManagedResource> resources )
                : base( baseCompilation, modifications, resources )
            {
                this._types = types;
                this.SyntaxTrees = syntaxTrees;
                this._observedSyntaxTreePaths = observedSyntaxTreePaths;

#if DEBUG
                this.CheckTrees();
#endif
            }

#if DEBUG

            private void CheckTrees()
            {
                if ( this.SyntaxTrees.Any( t => string.IsNullOrEmpty( t.Key ) ) )
                {
                    throw new AssertionFailedException( "A syntax tree has no name." );
                }
            }
#endif

            public override ImmutableDictionary<string, SyntaxTree> SyntaxTrees { get; }

            public override ImmutableHashSet<INamedTypeSymbol> Types => this._types ?? throw new NotImplementedException();

            public override ImmutableHashSet<INamespaceSymbol> Namespaces
                => this.Types.SelectAsReadOnlyCollection( t => t.ContainingNamespace ).ToImmutableHashSet();

            internal override bool IsSyntaxTreeObserved( string syntaxTreePath )
                => this._observedSyntaxTreePaths == null || this._observedSyntaxTreePaths.Contains( syntaxTreePath );

            public override bool IsPartial => true;

            internal override bool HasObservabilityFilter => this._observedSyntaxTreePaths != null;

            internal override PartialCompilation Update(
                IReadOnlyCollection<SyntaxTreeTransformation>? transformations = null,
                ImmutableArray<ManagedResource> resources = default )
            {
                Validate( transformations );

                var syntaxTrees = this.SyntaxTrees.ToBuilder();

                if ( transformations != null )
                {
                    foreach ( var transformation in transformations )
                    {
                        if ( transformation.OldTree != null && !this.SyntaxTrees.ContainsKey( transformation.FilePath ) )
                        {
                            throw new KeyNotFoundException();
                        }

                        switch ( transformation.Kind )
                        {
                            case SyntaxTreeTransformationKind.None:
                                continue;

                            case SyntaxTreeTransformationKind.Add:
                            case SyntaxTreeTransformationKind.Replace:
                                syntaxTrees[transformation.FilePath] = transformation.NewTree.AssertNotNull();

                                break;

                            case SyntaxTreeTransformationKind.Remove:
                                syntaxTrees.Remove( transformation.FilePath );

                                break;

                            default:
                                throw new AssertionFailedException( $"Unexpected transformation kind: {transformation.Kind}." );
                        }
                    }
                }

                // TODO: when the compilation is modified, we should update the set of types and derived types.
                return new PartialImpl( syntaxTrees.ToImmutable(), this._observedSyntaxTreePaths, null, this, transformations, resources );
            }
        }
    }
}