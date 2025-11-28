// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code.SyntaxBuilders
{
    /// <summary>
    /// Allows building run-time statements programmatically by composing a string using an underlying <see cref="System.Text.StringBuilder"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="StatementBuilder"/> provides a text-based approach to constructing C# statements programmatically. It offers
    /// the same specialized methods as <see cref="ExpressionBuilder"/> for appending syntax elements (<see cref="SyntaxBuilder.AppendLiteral"/>,
    /// <see cref="SyntaxBuilder.AppendTypeName"/>, etc.). For building multi-line code blocks, it provides methods to manage
    /// indentation and braces (<see cref="BeginBlock"/>, <see cref="EndBlock"/>, <see cref="Indent"/>, <see cref="Unindent"/>),
    /// which automatically add proper indentation after line breaks.
    /// </para>
    /// <para>
    /// A major benefit of <see cref="StatementBuilder"/> is that it can be used in compile-time methods that are not templates,
    /// providing flexibility for building statements in helper methods. After building the statement string, call
    /// <see cref="ToStatement"/> to get an <see cref="IStatement"/> object that can be inserted into template code using
    /// <see cref="meta.InsertStatement"/>.
    /// </para>
    /// <para>
    /// When using <see cref="StatementBuilder"/>, ensure that all type names are fully namespace-qualified, as you cannot assume
    /// the target code has any required <c>using</c> directives. Metalama will simplify the code and add relevant <c>using</c>
    /// directives when producing formatted output.
    /// </para>
    /// </remarks>
    /// <seealso cref="IStatement"/>
    /// <seealso cref="IStatementBuilder"/>
    /// <seealso cref="StatementFactory"/>
    /// <seealso cref="SyntaxBuilder"/>
    /// <seealso href="@run-time-statements"/>
    /// <seealso href="@templates"/>
    [CompileTime]
    [PublicAPI]
    public sealed class StatementBuilder : SyntaxBuilder, IStatementBuilder
    {
        private int _indentLevel;

        public StatementBuilder() { }

        public override void AppendVerbatim( string rawCode )
        {
            if ( this._indentLevel > 0 && this.StringBuilder[^1] is '\n' or '\r' )
            {
                this.StringBuilder.Append( ' ', this._indentLevel * 4 );
            }

            base.AppendVerbatim( rawCode );
        }

        private StatementBuilder( StatementBuilder prototype ) : base( prototype ) { }

        /// <summary>
        /// Converts the current <see cref="StatementBuilder"/> into an <see cref="IStatement"/> object, which can then
        /// be inserted into run-time code using the <see cref="meta.InsertStatement(Metalama.Framework.Code.SyntaxBuilders.IStatement)"/>
        /// method.
        /// </summary>
        /// <returns>An <see cref="IStatement"/> representing the built statement.</returns>
        public IStatement ToStatement() => StatementFactory.Parse( this.StringBuilder.ToString() );

        /// <summary>
        /// Appends a line break.
        /// </summary>
        public void AppendLine() => this.StringBuilder.AppendLine();

        /// <summary>
        /// Returns a clone of the current <see cref="StatementBuilder"/>.
        /// </summary>
        /// <returns>A new <see cref="StatementBuilder"/> with the same content.</returns>
        public StatementBuilder Clone() => new( this );

        /// <summary>
        /// Increments the indentation level.
        /// </summary>
        public void Indent()
        {
            this._indentLevel++;
        }

        /// <summary>
        /// Decrements the indentation level.
        /// </summary>
        public void Unindent() => this._indentLevel--;

        /// <summary>
        /// Begins a block (appends a <c>{</c> and increments the indentation level).
        /// </summary>
        public void BeginBlock()
        {
            this.AppendLine();
            this.AppendVerbatim( "{" );
            this.AppendLine();
            this._indentLevel++;
        }

        /// <summary>
        /// Ends a block (appends a <c>}</c> and decrements the indentation level).
        /// </summary>
        public void EndBlock()
        {
            this.AppendLine();
            this._indentLevel--;
            this.AppendVerbatim( "}" );
            this.AppendLine();
        }
    }
}