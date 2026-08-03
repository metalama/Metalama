// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
                this.SyntaxTreesByPath = syntaxTrees;
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
                this.SyntaxTreesByPath = syntaxTrees;
                this._observedSyntaxTreePaths = observedSyntaxTreePaths;

#if DEBUG
                this.CheckTrees();
#endif
            }

#if DEBUG

            private void CheckTrees()
            {
                if ( this.SyntaxTreesByPath.Any( t => string.IsNullOrEmpty( t.Key ) ) )
                {
                    throw new AssertionFailedException( "A syntax tree has no name." );
                }
            }
#endif

            /// <summary>
            /// Gets the syntax trees of the subset indexed by path. This is the storage of the class, and the source of
            /// both <see cref="SyntaxTreeCollection"/> and <see cref="TryGetSyntaxTree"/>. It is an
            /// <see cref="ImmutableDictionary{TKey,TValue}"/>, unlike the index of a complete compilation, because
            /// <see cref="Update"/> derives a new subset from it and the structural sharing is what makes that cheap.
            /// </summary>
            private ImmutableDictionary<string, SyntaxTree> SyntaxTreesByPath { get; }

            [Obsolete( "Use SyntaxTreeCollection to enumerate the syntax trees, or TryGetSyntaxTree to find one by its DocumentKey." )]
            public override ImmutableDictionary<string, SyntaxTree> SyntaxTrees => this.SyntaxTreesByPath;

            /// <remarks>
            /// Materialized under <see cref="MemoAttribute"/> rather than adapted, because the pipeline enumerates this
            /// collection once per tree and an array is the cheapest thing to walk. One instance exists per
            /// <see cref="PartialImpl"/>, and instances are created only by <see cref="Update"/>.
            /// </remarks>
            [Memo]
            public override IReadOnlyCollection<SyntaxTree> SyntaxTreeCollection => this.SyntaxTreesByPath.Values.ToImmutableArray();

            public override bool TryGetSyntaxTree( DocumentKey documentKey, [NotNullWhen( true )] out SyntaxTree? syntaxTree )
                => this.SyntaxTreesByPath.TryGetValue( documentKey.Path, out syntaxTree );

            public override ImmutableHashSet<INamedTypeSymbol> Types => this._types ?? throw new NotImplementedException();

            public override ImmutableHashSet<INamespaceSymbol> Namespaces
                => this.Types.SelectAsReadOnlyCollection( t => t.ContainingNamespace ).ToImmutableHashSet();

            internal override bool IsSyntaxTreeObserved( string syntaxTreePath )
                => this._observedSyntaxTreePaths == null || this._observedSyntaxTreePaths.Contains( syntaxTreePath );

            public override bool IsPartial => true;

            internal override bool HasObservabilityFilter => this._observedSyntaxTreePaths != null;

            public override PartialCompilation Update(
                IReadOnlyCollection<SyntaxTreeTransformation>? transformations = null,
                ImmutableArray<ManagedResource> resources = default )
            {
                Validate( transformations );

                var syntaxTrees = this.SyntaxTreesByPath.ToBuilder();

                if ( transformations != null )
                {
                    foreach ( var transformation in transformations )
                    {
                        // Matched by identity and not merely by path: the transformation names the tree it applies to,
                        // and Compilation.ReplaceSyntaxTree resolves it by identity, so a transformation naming a tree
                        // that this partial compilation does not hold has to be rejected here. Left to Roslyn it fails
                        // with a message that names neither the path nor the caller.
                        if ( transformation.OldTree != null
                             && (!this.SyntaxTreesByPath.TryGetValue( transformation.FilePath, out var existingTree )
                                 || existingTree != transformation.OldTree) )
                        {
                            throw new KeyNotFoundException(
                                $"The partial compilation does not contain the syntax tree '{transformation.FilePath}' that the transformation replaces." );
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