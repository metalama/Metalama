// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RefKind = Metalama.Framework.Code.RefKind;

namespace Metalama.Framework.Engine.Templating.Expressions
{
    /// <summary>
    /// Adds implementation methods to the public <see cref="IExpression"/> interface. 
    /// </summary>
    internal abstract class UserExpression : IUserExpression
    {
        private string? _toString;

        protected abstract ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null );

        /// <summary>
        /// Creates a <see cref="TypedExpressionSyntaxImpl"/> for the given <see cref="SyntaxGenerationContext"/>.
        /// </summary>
        internal TypedExpressionSyntaxImpl ToTypedExpressionSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null )
            => new(
                this.ToSyntax( syntaxSerializationContext, targetType ),
                this.Type,
                syntaxSerializationContext.CompilationModel,
                this.IsReferenceable,
                this.CanBeNull );

        public abstract IType Type { get; }

        public virtual RefKind RefKind => RefKind.None;

        bool IExpression.IsAssignable => this.IsAssignable ?? false;

        protected virtual bool? IsAssignable => null;

        private protected virtual bool? IsReferenceable => null;

        public ref object? Value => ref RefHelper.Wrap( this );

        TypedExpressionSyntax IUserExpression.ToTypedExpressionSyntax( ISyntaxGenerationContext syntaxGenerationContext, IType? targetType )
            => this.ToTypedExpressionSyntax( (SyntaxSerializationContext) syntaxGenerationContext, targetType );

        public sealed override string ToString() => this._toString ??= this.ToStringCore();

        protected virtual bool CanBeNull => true;

        protected virtual string ToStringCore()
        {
            var compilation = this.Type.GetCompilationModel();

            return
                this.ToSyntax(
                        new SyntaxSerializationContext(
                            compilation,
                            compilation.CompilationContext.GetSyntaxGenerationContext( SyntaxGenerationOptions.Formatted, isNullOblivious: false ),
                            null ) )
                    .NormalizeWhitespace()
                    .ToString();
        }
    }
}