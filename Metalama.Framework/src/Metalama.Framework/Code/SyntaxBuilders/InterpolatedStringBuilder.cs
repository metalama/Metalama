// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Collections.Generic;

namespace Metalama.Framework.Code.SyntaxBuilders
{
    /// <summary>
    /// Allows building run-time interpolated string expressions programmatically by adding text and expression parts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="InterpolatedStringBuilder"/> provides a convenient way to generate run-time interpolated strings when the
    /// content is determined at compile time. Instead of using <c>string.Format</c> and building separate arrays of arguments,
    /// you can use <see cref="AddText"/> to add literal text and <see cref="AddExpression(object?, int?, string?)"/> to add
    /// interpolated expressions with optional alignment and format specifiers.
    /// </para>
    /// <para>
    /// After building the string content, call <see cref="ToExpression"/> to get an <see cref="IExpression"/> representing
    /// the interpolated string. The generated code uses C# interpolated string syntax (<c>$"..."</c>).
    /// </para>
    /// </remarks>
    /// <seealso cref="IExpressionBuilder"/>
    /// <seealso cref="ExpressionFactory"/>
    /// <seealso cref="ArrayBuilder"/>
    /// <seealso href="@run-time-expressions"/>
    /// <seealso href="@templates"/>
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
        /// <param name="text">The text to add.</param>
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
        /// <param name="expression">The expression to add.</param>
        /// <param name="alignment">Optional alignment specification for the expression.</param>
        /// <param name="format">Optional format string for the expression.</param>
        public void AddExpression( dynamic? expression, int? alignment = null, string? format = null )
            => this._items.Add( new Token( (object?) expression, alignment, format ) );

        /// <summary>
        /// Adds an expression to the interpolated string.
        /// </summary>
        /// <param name="expression">The expression to add.</param>
        /// <param name="alignment">Optional alignment specification for the expression.</param>
        /// <param name="format">Optional format string for the expression.</param>
        public void AddExpression( IExpression? expression, int? alignment = null, string? format = null )
            => this._items.Add( new Token( expression, alignment, format ) );

        /// <summary>
        /// Creates a compile-time <see cref="IExpression"/> from the current <see cref="InterpolatedStringBuilder"/>.
        /// </summary>
        /// <returns>An expression representing the interpolated string.</returns>
        public IExpression ToExpression() => SyntaxBuilder.CurrentImplementation.BuildInterpolatedString( this );

        /// <summary>
        /// Returns a clone of the current <see cref="InterpolatedStringBuilder"/>.
        /// </summary>
        /// <returns>A new <see cref="InterpolatedStringBuilder"/> with the same items.</returns>
        public InterpolatedStringBuilder Clone() => new( this );
    }
}