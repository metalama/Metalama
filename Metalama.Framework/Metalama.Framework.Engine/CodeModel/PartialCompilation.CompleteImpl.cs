// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.CodeModel
{
    public abstract partial class PartialCompilation
    {
        /// <summary>
        /// Represents a complete compilation, containing all syntax trees.
        /// </summary>
        private sealed class CompleteImpl : PartialCompilation
        {
            public CompleteImpl( CompilationContext compilationContext, Lazy<DerivedTypeIndex> derivedTypeIndex, ImmutableArray<ManagedResource> resources )
                : base( compilationContext, derivedTypeIndex, resources ) { }

            private CompleteImpl(
                PartialCompilation baseCompilation,
                IReadOnlyCollection<SyntaxTreeTransformation>? modifications,
                ImmutableArray<ManagedResource> resources )
                : base( baseCompilation, modifications, resources ) { }

            [Memo]
            public override ImmutableDictionary<string, SyntaxTree> SyntaxTrees => this.Compilation.GetIndexedSyntaxTrees();

            [Memo]
            public override ImmutableHashSet<INamedTypeSymbol> Types => this.Compilation.SourceModule.GetTypes().ToImmutableHashSet();

            [Memo]
            public override ImmutableHashSet<INamespaceSymbol> Namespaces
                => this.Compilation.SourceModule.GlobalNamespace.SelectManyRecursive( n => n.GetNamespaceMembers() ).ToImmutableHashSet();

            internal override bool IsSyntaxTreeObserved( string syntaxTreePath ) => true;

            public override bool IsPartial => false;

            internal override bool HasObservabilityFilter => false;

            public override PartialCompilation Update(
                IReadOnlyCollection<SyntaxTreeTransformation>? transformations = null,
                ImmutableArray<ManagedResource> resources = default )
            {
                Validate( transformations );

                return new CompleteImpl( this, transformations, resources );
            }
        }
    }
}