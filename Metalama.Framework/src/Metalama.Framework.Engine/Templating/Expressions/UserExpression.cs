// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
    /// <remarks>
    /// <para><b>Contract between <see cref="ToSyntax"/>, <see cref="GetSyntaxType"/>, and <see cref="ToTypedExpressionSyntax"/>:</b></para>
    /// <para>
    /// <see cref="ToSyntax"/> generates the <see cref="ExpressionSyntax"/> for this expression. It receives an optional
    /// <c>targetType</c> parameter that serves as a <b>hint</b> for syntax optimization (e.g., choosing between a bare method group
    /// vs. an explicit delegate creation, or between <c>default</c> vs. <c>default(T)</c>). However, <see cref="ToSyntax"/> is
    /// <b>not</b> responsible for converting the expression to <c>targetType</c> — that responsibility belongs to
    /// <see cref="TypedExpressionSyntaxImpl.Convert"/>, which is called by the consumer.
    /// </para>
    /// <para>
    /// <see cref="GetSyntaxType"/> must return the actual type of the syntax produced by <see cref="ToSyntax"/> for the same
    /// <c>targetType</c>. The base implementation returns <see cref="Type"/> (the expression's own type), which is correct
    /// for most subclasses that ignore <c>targetType</c>. Subclasses whose <see cref="ToSyntax"/> produces syntax of a
    /// different type depending on <c>targetType</c> must override <see cref="GetSyntaxType"/> to stay consistent.
    /// </para>
    /// <para>
    /// <see cref="ToTypedExpressionSyntax"/> combines both: it calls <see cref="ToSyntax"/> and <see cref="GetSyntaxType"/>
    /// with the same <c>targetType</c> to build a <see cref="TypedExpressionSyntaxImpl"/> with correct
    /// <see cref="TypedExpressionSyntaxImpl.ExpressionType"/>. This is critical because
    /// <see cref="TypedExpressionSyntaxImpl.Convert"/> uses <see cref="TypedExpressionSyntaxImpl.ExpressionType"/>
    /// to decide whether a cast is needed.
    /// </para>
    /// <para><b>Over-specification and simplification:</b></para>
    /// <para>
    /// The syntax generation pipeline intentionally produces over-specified syntax (e.g., redundant casts, fully-qualified
    /// type names, explicit <c>new DelegateType(methodGroup)</c> wrappers) to ensure correctness at generation time, when the
    /// full context is not yet known. It is the caller's responsibility to simplify the syntax afterward, once the expression
    /// is placed in its final context. This is done via the Roslyn <c>Simplifier</c> facility: nodes that may be redundant
    /// are annotated with <c>FormattingAnnotations.WithSimplifierAnnotation</c> (or the conditional
    /// <c>WithSimplifierAnnotationIfNecessary</c> extension), and the <c>CodeFormatter</c> pipeline runs the Roslyn
    /// <c>Simplifier.ReduceAsync</c> pass (plus a custom simplifier for Metalama-specific patterns such as delegate
    /// creation expressions) to remove what is unnecessary in context.
    /// </para>
    /// </remarks>
    internal abstract class UserExpression : IUserExpression
    {
        private string? _toString;

        /// <summary>
        /// Generates the <see cref="ExpressionSyntax"/> for this expression.
        /// </summary>
        /// <param name="syntaxSerializationContext">The serialization context.</param>
        /// <param name="targetType">
        /// An optional hint indicating the type the expression will be used as. Subclasses may use this to optimize
        /// the generated syntax (e.g., omitting disambiguation when the target type provides sufficient context).
        /// This method is <b>not</b> responsible for converting the expression to <paramref name="targetType"/>:
        /// the caller uses <see cref="TypedExpressionSyntaxImpl.Convert"/> for that purpose.
        /// </param>
        /// <returns>
        /// An <see cref="ExpressionSyntax"/> whose actual type must match the return value of
        /// <see cref="GetSyntaxType"/> for the same <paramref name="targetType"/>.
        /// The returned syntax may be over-specified (e.g., redundant casts or fully-qualified names);
        /// subclasses should annotate such nodes with <c>WithSimplifierAnnotationIfNecessary</c> so the
        /// downstream <c>CodeFormatter</c> pipeline can simplify them in context.
        /// </returns>
        protected abstract ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null );

        /// <summary>
        /// Gets the actual type of the syntax returned by <see cref="ToSyntax"/> for the same <paramref name="targetType"/>.
        /// </summary>
        /// <param name="targetType">The same target type that will be passed to <see cref="ToSyntax"/>.</param>
        /// <returns>
        /// The type of the expression syntax. The base implementation returns <see cref="Type"/>, which is correct for subclasses
        /// whose <see cref="ToSyntax"/> always produces syntax of <see cref="Type"/> regardless of <paramref name="targetType"/>.
        /// Subclasses that adapt their output type (e.g., producing <c>new TargetDelegateType(methodGroup)</c>) must override
        /// this method to return the matching type.
        /// </returns>
        protected virtual IType GetSyntaxType( IType? targetType ) => this.Type;

        /// <summary>
        /// Creates a <see cref="TypedExpressionSyntaxImpl"/> for the given <see cref="SyntaxGenerationContext"/>.
        /// Calls <see cref="ToSyntax"/> and <see cref="GetSyntaxType"/> with the same <paramref name="targetType"/>
        /// to ensure the <see cref="TypedExpressionSyntaxImpl.ExpressionType"/> accurately reflects the syntax.
        /// </summary>
        internal TypedExpressionSyntaxImpl ToTypedExpressionSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null )
            => new(
                this.ToSyntax( syntaxSerializationContext, targetType ),
                this.GetSyntaxType( targetType ),
                syntaxSerializationContext.CompilationModel,
                this.IsReferenceable,
                this.CanBeNull,
                this );

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
                            null,
                            null ) )
                    .NormalizeWhitespace()
                    .ToString();
        }
    }
}