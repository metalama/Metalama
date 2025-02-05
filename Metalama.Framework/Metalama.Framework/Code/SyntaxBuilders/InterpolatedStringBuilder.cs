// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System.Collections.Generic;

namespace Metalama.Framework.Code.SyntaxBuilders
{
    /// <summary>
    /// Compile-time object that allows to build a run-time interpolated string.
    /// </summary>
    [CompileTime]
    public sealed partial class InterpolatedStringBuilder : INotNullExpressionBuilder
    {
        private readonly List<object?> _items = new();

        internal IReadOnlyList<object?> Items => this._items;

        /// <summary>
        /// Gets the number of items that have been added to the current builder.
        /// </summary>
        public int ItemCount => this._items.Count;

        public InterpolatedStringBuilder() { }

        private InterpolatedStringBuilder( InterpolatedStringBuilder prototype )
        {
            this._items.AddRange( prototype._items );
        }

        /// <summary>
        /// Adds a fixed text to the interpolated string.
        /// </summary>
        public void AddText( string? text )
        {
            if ( !string.IsNullOrEmpty( text ) )
            {
                this._items.Add( text );
            }
        }

        /// <summary>
        /// Adds an expression to the interpolated string.
        /// </summary>
        public void AddExpression( dynamic? expression, int? alignment = null, string? format = null )
            => this._items.Add( new Token( (object?) expression, alignment, format ) );

        /// <summary>
        /// Adds an expression to the interpolated string.
        /// </summary>
        public void AddExpression( IExpression? expression, int? alignment = null, string? format = null )
            => this._items.Add( new Token( expression, alignment, format ) );

        /// <summary>
        /// Creates a compile-time <see cref="IExpression"/> from the current <see cref="ExpressionBuilder"/>.
        /// </summary>
        public IExpression ToExpression() => SyntaxBuilder.CurrentImplementation.BuildInterpolatedString( this );

        public InterpolatedStringBuilder Clone() => new( this );
    }
}