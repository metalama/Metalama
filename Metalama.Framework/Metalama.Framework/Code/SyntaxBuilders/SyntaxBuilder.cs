// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Project;
using System;
using System.Text;

namespace Metalama.Framework.Code.SyntaxBuilders
{
    /// <summary>
    /// A base class for <see cref="ExpressionBuilder"/> and <see cref="StatementBuilder"/>.
    /// </summary>
    [CompileTime]
    public abstract class SyntaxBuilder
    {
        internal static ISyntaxBuilderImpl CurrentImplementation
            => MetalamaExecutionContext.CurrentInternal.SyntaxBuilder
               ?? throw new InvalidOperationException( "This service is not available in the current execution context." );

        private protected StringBuilder StringBuilder { get; }

        private protected SyntaxBuilder()
        {
            this.StringBuilder = new StringBuilder();
        }

        private protected SyntaxBuilder( SyntaxBuilder prototype )
        {
            this.StringBuilder = new StringBuilder( prototype.StringBuilder.ToString() );
        }

        /// <summary>
        /// Appends a string to the <see cref="StringBuilder"/>, without performing any modification to the input string.
        /// </summary>
        /// <param name="rawCode"></param>
        public virtual void AppendVerbatim( string rawCode ) => this.StringBuilder.Append( rawCode );

        /// <summary>
        /// Appends a literal of type <see cref="int"/> to the <see cref="StringBuilder"/>.
        /// </summary>
        public void AppendLiteral( int value ) => CurrentImplementation.AppendLiteral( value, this.StringBuilder, SpecialType.Int32, false );

        /// <summary>
        /// Appends a literal of type <see cref="uint"/> to the <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="value">The literal value.</param>
        /// <param name="stronglyTyped">A value indicating if the literal should be qualified to remove any type ambiguity, for instance
        /// if the literal can only represent an <see cref="int"/>.</param>
        public void AppendLiteral( uint value, bool stronglyTyped = false )
            => CurrentImplementation.AppendLiteral( value, this.StringBuilder, SpecialType.UInt32, stronglyTyped );

        /// <summary>
        /// Appends a literal of type <see cref="short"/> to the <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="value">The literal value.</param>
        /// <param name="stronglyTyped">A value indicating if the literal should be qualified to remove any type ambiguity, for instance
        /// if the literal can only represent an <see cref="int"/>.</param>
        public void AppendLiteral( short value, bool stronglyTyped = false )
            => CurrentImplementation.AppendLiteral( value, this.StringBuilder, SpecialType.Int16, stronglyTyped );

        /// <summary>
        /// Appends a literal of type <see cref="ushort"/> to the <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="value">The literal value.</param>
        /// <param name="stronglyTyped">A value indicating if the literal should be qualified to remove any type ambiguity, for instance
        /// if the literal can only represent an <see cref="int"/>.</param>
        public void AppendLiteral( ushort value, bool stronglyTyped = false )
            => CurrentImplementation.AppendLiteral( value, this.StringBuilder, SpecialType.UInt16, stronglyTyped );

        /// <summary>
        /// Appends a literal of type <see cref="long"/> to the <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="value">The literal value.</param>
        /// <param name="stronglyTyped">A value indicating if the literal should be qualified to remove any type ambiguity, for instance
        /// if the literal can only represent an <see cref="int"/>.</param>
        public void AppendLiteral( long value, bool stronglyTyped = false )
            => CurrentImplementation.AppendLiteral( value, this.StringBuilder, SpecialType.Int64, stronglyTyped );

        /// <summary>
        /// Appends a literal of type <see cref="ulong"/> to the <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="value">The literal value.</param>
        /// <param name="stronglyTyped">A value indicating if the literal should be qualified to remove any type ambiguity, for instance
        /// if the literal can only represent an <see cref="int"/>.</param>
        public void AppendLiteral( ulong value, bool stronglyTyped = false )
            => CurrentImplementation.AppendLiteral( value, this.StringBuilder, SpecialType.UInt64, stronglyTyped );

        /// <summary>
        /// Appends a literal of type <see cref="byte"/> to the <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="value">The literal value.</param>
        /// <param name="stronglyTyped">A value indicating if the literal should be qualified to remove any type ambiguity, for instance
        /// if the literal can only represent an <see cref="int"/>.</param>
        public void AppendLiteral( byte value, bool stronglyTyped = false )
            => CurrentImplementation.AppendLiteral( value, this.StringBuilder, SpecialType.Byte, stronglyTyped );

        /// <summary>
        /// Appends a literal of type <see cref="sbyte"/> to the <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="value">The literal value.</param>
        /// <param name="stronglyTyped">A value indicating if the literal should be qualified to remove any type ambiguity, for instance
        /// if the literal can only represent an <see cref="int"/>.</param>
        public void AppendLiteral( sbyte value, bool stronglyTyped = false )
            => CurrentImplementation.AppendLiteral( value, this.StringBuilder, SpecialType.SByte, stronglyTyped );

        /// <summary>
        /// Appends a literal of type <see cref="double"/> to the <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="value">The literal value.</param>
        /// <param name="stronglyTyped">A value indicating if the literal should be qualified to remove any type ambiguity, for instance
        /// if the literal can only represent an <see cref="int"/>.</param>
        public void AppendLiteral( double value, bool stronglyTyped = false )
            => CurrentImplementation.AppendLiteral( value, this.StringBuilder, SpecialType.Double, stronglyTyped );

        /// <summary>
        /// Appends a literal of type <see cref="float"/> to the <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="value">The literal value.</param>
        /// <param name="stronglyTyped">A value indicating if the literal should be qualified to remove any type ambiguity, for instance
        /// if the literal can only represent an <see cref="int"/>.</param>
        public void AppendLiteral( float value, bool stronglyTyped = false )
            => CurrentImplementation.AppendLiteral( value, this.StringBuilder, SpecialType.Single, stronglyTyped );

        /// <summary>
        /// Appends a literal of type <see cref="decimal"/> to the <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="value">The literal value.</param>
        /// <param name="stronglyTyped">A value indicating if the literal should be qualified to remove any type ambiguity, for instance
        /// if the literal can only represent an <see cref="int"/>.</param>
        public void AppendLiteral( decimal value, bool stronglyTyped = false )
            => CurrentImplementation.AppendLiteral( value, this.StringBuilder, SpecialType.Decimal, stronglyTyped );

        /// <summary>
        /// Appends a literal of type <see cref="string"/> to the <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="value">The literal value.</param>
        /// <param name="stronglyTyped">A value indicating if the <c>null</c> value  should be qualified as <c>(string?) null</c>.</param>
        public void AppendLiteral( string? value, bool stronglyTyped = false )
            => CurrentImplementation.AppendLiteral( value, this.StringBuilder, SpecialType.String, stronglyTyped );

        /// <summary>
        /// Appends a fully-qualified type name to the <see cref="StringBuilder"/>, where the type is given as an <see cref="IType"/>.
        /// </summary>
        public void AppendTypeName( IType type ) => CurrentImplementation.AppendTypeName( type, this.StringBuilder );

        /// <summary>
        /// Appends a fully-qualified type name to the <see cref="StringBuilder"/>, where the type is given as a reflection <see cref="Type"/>.
        /// </summary>
        public void AppendTypeName( Type type ) => CurrentImplementation.AppendTypeName( type, this.StringBuilder );

        /// <summary>
        /// Appends an expression to the <see cref="StringBuilder"/>, where the expression is given as an <see cref="IExpression"/>.
        /// </summary>
        public void AppendExpression( IExpression expression ) => CurrentImplementation.AppendExpression( expression, this.StringBuilder );

        /// <summary>
        /// Appends an expression to the <see cref="StringBuilder"/>, where the expression is given as an <see cref="IExpressionBuilder"/>.
        /// </summary>
        public void AppendExpression( IExpressionBuilder expression )
            => CurrentImplementation.AppendExpression( expression.ToExpression(), this.StringBuilder );

        /// <summary>
        /// Appends an expression to the <see cref="StringBuilder"/>, where the expression is a C# expression.
        /// </summary>
        public void AppendExpression( dynamic? expression ) => CurrentImplementation.AppendDynamic( (object?) expression, this.StringBuilder );

        public override string ToString() => this.StringBuilder.ToString();
    }
}