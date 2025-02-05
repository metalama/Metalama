// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code.SyntaxBuilders
{
    /// <summary>
    /// Allows to build a run-time statement that can be injected to run-time code using
    /// <see cref="ToStatement"/> and <see cref="meta.InsertStatement(Metalama.Framework.Code.SyntaxBuilders.IStatement)"/>.
    /// </summary>
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
        public IStatement ToStatement() => StatementFactory.Parse( this.StringBuilder.ToString() );

        /// <summary>
        /// Appends a line break.
        /// </summary>
        public void AppendLine() => this.StringBuilder.AppendLine();

        /// <summary>
        /// Returns a clone of the current <see cref="StatementBuilder"/>.
        /// </summary>
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