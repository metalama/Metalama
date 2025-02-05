// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.CodeModel.UpdatableCollections;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Collections
{
    internal abstract class DeclarationCollection<TDeclaration, TRef> : IReadOnlyCollection<TDeclaration>
        where TDeclaration : class, IDeclaration
        where TRef : class, IRef<TDeclaration>
    {
        private readonly IGenericContext _genericContext;

        internal IDeclaration? ContainingDeclaration { get; }

        protected IReadOnlyList<TRef> Source { get; }

        internal CompilationModel Compilation => (CompilationModel) this.ContainingDeclaration.AssertNotNull().Compilation;

        protected DeclarationCollection( IDeclaration containingDeclaration, IReadOnlyList<TRef> source )
        {
#if DEBUG
            if ( containingDeclaration is SourceNamedTypeImpl )
            {
                throw new ArgumentOutOfRangeException( nameof(containingDeclaration) );
            }
#endif

            this._genericContext = containingDeclaration.GenericContext;
            this.Source = source;
            this.ContainingDeclaration = containingDeclaration;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeclarationCollection{TDeclaration,TRef}"/> class representing an empty list.
        /// </summary>
        protected DeclarationCollection()
        {
            this.Source = ImmutableArray<TRef>.Empty;
            this._genericContext = GenericContext.Empty;
        }

        public IEnumerator<TDeclaration> GetEnumerator()
        {
            if ( this.Source is DeclarationUpdatableCollection<TDeclaration, TRef> updatableCollection )
            {
                // We don't use the list enumeration pattern because this may lead to infinite recursions
                // if the loop body adds items during the enumeration.

                using ( StackOverflowHelper.Detect() )
                {
                    foreach ( var reference in updatableCollection )
                    {
                        yield return this.GetItem( reference );
                    }
                }
            }
            else
            {
                foreach ( var reference in this.Source )
                {
                    yield return this.GetItem( reference );
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public int Count => this.Source.Count;

        // We allow resolving references to missing declarations because the collection may be a child collection of a missing declaration,
        // for instance the parameters of a method that has been introduced into the current compilation but is not included in the current compilation.
        protected TDeclaration GetItem( TRef reference )
        {
            var declaration = reference.GetTarget( this.Compilation, genericContext: this._genericContext );

            return declaration;
        }

        protected IEnumerable<TDeclaration> GetItems( IEnumerable<IRef<TDeclaration>> references ) => references.Select( x => x.GetTarget( this.Compilation ) );

        public override string ToString()
        {
            if ( this.Source is ILazy { IsComplete: true } )
            {
                return $"{this.GetType().Name} Count={this.Count}";
            }
            else
            {
                return $"{this.GetType().Name} (unresolved)";
            }
        }
    }
}