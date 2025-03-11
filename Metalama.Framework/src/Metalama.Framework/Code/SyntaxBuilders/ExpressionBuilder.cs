// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code.SyntaxBuilders
{
    /// <summary>
    /// Allows to build a run-time expression by composing a string thanks to an underlying <see cref="System.Text.StringBuilder"/>.
    /// Use the <see cref="ToExpression"/> method to convert the <see cref="ExpressionBuilder"/> into a compile-time representation of the expression,
    /// or the <see cref="ExpressionBuilderExtensions.ToValue(Metalama.Framework.Code.SyntaxBuilders.IExpressionBuilder)"/> methods converts it to a dynamic expression that can be used in the C# code
    /// of the template. 
    /// </summary>
    [CompileTime]
    [PublicAPI]
    public sealed class ExpressionBuilder : SyntaxBuilder, IExpressionBuilder
    {
        public ExpressionBuilder() { }

        private ExpressionBuilder( ExpressionBuilder prototype ) : base( prototype ) { }

        /// <summary>
        /// Creates a compile-time <see cref="IExpression"/> from the current <see cref="ExpressionBuilder"/>.
        /// </summary>
        public IExpression ToExpression() => ExpressionFactory.Parse( this.ToString(), this.ExpressionType, this.IsReferenceable );

        /// <summary>
        /// Returns a clone of the current <see cref="ExpressionBuilder"/>.
        /// </summary>
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