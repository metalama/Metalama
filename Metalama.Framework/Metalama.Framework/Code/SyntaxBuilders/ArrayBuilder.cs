// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Code.SyntaxBuilders
{
    /// <summary>
    /// Compile-time object that allows to build a run-time array. Items of the array are run-time expressions.
    /// </summary>
    [CompileTime]
    public sealed class ArrayBuilder : INotNullExpressionBuilder
    {
        private readonly List<object?> _items = new();

        internal IType ItemType { get; }

        internal IReadOnlyList<object?> Items => this._items;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayBuilder"/> class where the item type is a given <see cref="IType"/>.
        /// </summary>
        public ArrayBuilder( IType itemType )
        {
            this.ItemType = itemType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayBuilder"/> class where the item type is a given <see cref="Type"/>.
        /// </summary>
        public ArrayBuilder( Type itemType ) : this( TypeFactory.GetType( itemType ) ) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayBuilder"/> class where the item type is a <see cref="object"/>.
        /// </summary>
        public ArrayBuilder() : this( TypeFactory.GetType( SpecialType.Object ) ) { }

        private ArrayBuilder( ArrayBuilder prototype )
        {
            this._items.AddRange( prototype._items );
            this.ItemType = prototype.ItemType;
        }

        /// <summary>
        /// Adds an item to the array.
        /// </summary>
        public void Add( dynamic? expression ) => this._items.Add( (object?) expression );

        /// <summary>
        /// Adds an item to the array.
        /// </summary>
        public void Add( IExpression expression ) => this._items.Add( expression );

        /// <summary>
        /// Returns a clone of the current <see cref="ArrayBuilder"/>.
        /// </summary>
        public IExpression ToExpression() => SyntaxBuilder.CurrentImplementation.BuildArray( this );

        /// <summary>
        /// Returns a clone of the current <see cref="ArrayBuilder"/>.
        /// </summary>
        public ArrayBuilder Clone() => new( this );
    }
}