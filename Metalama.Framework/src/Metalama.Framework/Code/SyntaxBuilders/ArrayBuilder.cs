// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Code.SyntaxBuilders
{
    /// <summary>
    /// Allows building run-time array creation expressions programmatically by adding items one at a time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ArrayBuilder"/> provides a convenient way to generate run-time arrays when the number of elements or their
    /// values are determined at compile time. Instead of generating array initialization code as text, you can add items
    /// programmatically using <see cref="Add(dynamic)"/>, then call <see cref="ToExpression"/> to get an <see cref="IExpression"/>
    /// representing the array creation expression.
    /// </para>
    /// <para>
    /// This is particularly useful when you need to generate arrays as method arguments or in other expression contexts where
    /// a single expression is required. The generated code uses C# array initializer syntax.
    /// </para>
    /// </remarks>
    /// <seealso cref="IExpressionBuilder"/>
    /// <seealso cref="ExpressionFactory"/>
    /// <seealso cref="InterpolatedStringBuilder"/>
    /// <seealso href="@run-time-expressions"/>
    /// <seealso href="@templates"/>
    [CompileTime]
    public sealed class ArrayBuilder : INotNullExpressionBuilder
    {
        private readonly List<object?> _items = new();

        internal IType ItemType { get; }

        internal IReadOnlyList<object?> Items => this._items;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayBuilder"/> class where the item type is a given <see cref="IType"/>.
        /// </summary>
        /// <param name="itemType">The type of items in the array.</param>
        public ArrayBuilder( IType itemType )
        {
            this.ItemType = itemType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayBuilder"/> class where the item type is a given <see cref="Type"/>.
        /// </summary>
        /// <param name="itemType">The reflection type of items in the array.</param>
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
        /// <param name="expression">The expression representing the item to add.</param>
        public void Add( dynamic? expression ) => this._items.Add( (object?) expression );

        /// <summary>
        /// Adds an item to the array.
        /// </summary>
        /// <param name="expression">The expression representing the item to add.</param>
        public void Add( IExpression expression ) => this._items.Add( expression );

        /// <summary>
        /// Creates a compile-time <see cref="IExpression"/> representing the array from the current <see cref="ArrayBuilder"/>.
        /// </summary>
        /// <returns>An expression representing the array.</returns>
        public IExpression ToExpression() => SyntaxBuilder.CurrentImplementation.BuildArray( this );

        /// <summary>
        /// Returns a clone of the current <see cref="ArrayBuilder"/>.
        /// </summary>
        /// <returns>A new <see cref="ArrayBuilder"/> with the same items.</returns>
        public ArrayBuilder Clone() => new( this );
    }
}