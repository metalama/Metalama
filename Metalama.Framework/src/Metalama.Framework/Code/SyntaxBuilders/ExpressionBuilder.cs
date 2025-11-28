// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code.SyntaxBuilders
{
    /// <summary>
    /// Allows to build a run-time expression programmatically by composing a string using an underlying <see cref="System.Text.StringBuilder"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ExpressionBuilder"/> provides a text-based approach to constructing complex C# expressions programmatically or
    /// dynamically. It offers specialized methods for appending different syntax elements: <see cref="SyntaxBuilder.AppendLiteral"/>
    /// for literals, <see cref="SyntaxBuilder.AppendTypeName"/> for fully-qualified type names, <see cref="SyntaxBuilder.AppendExpression"/>
    /// for existing expressions, and <see cref="SyntaxBuilder.AppendVerbatim"/> for keywords and punctuation.
    /// </para>
    /// <para>
    /// A major benefit of <see cref="ExpressionBuilder"/> is that it can be used in compile-time methods that are not templates,
    /// providing flexibility for building expressions in helper methods. After building the expression string, call
    /// <see cref="ToExpression"/> to get an <see cref="IExpression"/> object, or use <see cref="ExpressionBuilderExtensions.ToValue"/>
    /// to get a <c>dynamic</c> value that can be used directly in template code.
    /// </para>
    /// <para>
    /// When using <see cref="ExpressionBuilder"/>, ensure that all type names are fully namespace-qualified, as you cannot assume
    /// the target code has any required <c>using</c> directives. Metalama will simplify the code and add relevant <c>using</c>
    /// directives when producing formatted output.
    /// </para>
    /// </remarks>
    /// <seealso cref="IExpression"/>
    /// <seealso cref="IExpressionBuilder"/>
    /// <seealso cref="ExpressionFactory"/>
    /// <seealso cref="SyntaxBuilder"/>
    /// <seealso href="@run-time-expressions"/>
    /// <seealso href="@templates"/>
    [CompileTime]
    [PublicAPI]
    public sealed class ExpressionBuilder : SyntaxBuilder, IExpressionBuilder
    {
        public ExpressionBuilder() { }

        private ExpressionBuilder( ExpressionBuilder prototype ) : base( prototype ) { }

        /// <summary>
        /// Creates a compile-time <see cref="IExpression"/> from the current <see cref="ExpressionBuilder"/>.
        /// </summary>
        /// <returns>An <see cref="IExpression"/> representing the built expression.</returns>
        public IExpression ToExpression() => ExpressionFactory.Parse( this.ToString(), this.ExpressionType, this.IsReferenceable );

        /// <summary>
        /// Returns a clone of the current <see cref="ExpressionBuilder"/>.
        /// </summary>
        /// <returns>A new <see cref="ExpressionBuilder"/> with the same content.</returns>
        public ExpressionBuilder Clone() => new( this );

        /// <summary>
        /// Gets or sets the resulting type of the expression, if known. This value allows to generate simpler code.
        /// </summary>
        public IType? ExpressionType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the expression can be used in <c>ref</c> or <c>out</c> situations.
        /// </summary>
        public bool? IsReferenceable { get; set; }
    }
}