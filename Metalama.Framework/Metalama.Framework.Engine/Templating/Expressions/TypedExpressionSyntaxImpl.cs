// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Diagnostics.CodeAnalysis;
using SpecialType = Metalama.Framework.Code.SpecialType;

namespace Metalama.Framework.Engine.Templating.Expressions
{
    /// <summary>
    /// Represents an <see cref="ExpressionSyntax"/> and its <see cref="IType"/>. Annotates the <see cref="ExpressionSyntax"/>
    /// with <see cref="TypeAnnotationMapper"/>.
    /// </summary>
    internal sealed class TypedExpressionSyntaxImpl : ITypedExpressionSyntaxImpl
    {
        /// <summary>
        /// Gets the expression type, or <c>null</c> if the expression is actually the <c>null</c> or <c>default</c> expression.
        /// </summary>
        public IType? ExpressionType { get; }

        /// <summary>
        /// Gets a value indicating whether it is legal to use the <c>out</c> or <c>ref</c> argument modifier with this expression.
        /// </summary>
        public bool? IsReferenceable { get; }

        public ExpressionSyntax Syntax { get; }

        public bool CanBeNull { get; }

        public ExpressionStatementSyntax ToStatement() => SyntaxFactory.ExpressionStatement( this.Syntax.RemoveParenthesis() );

        public TypedExpressionSyntaxImpl( ExpressionSyntax syntax, TypedExpressionSyntaxImpl prototype )
        {
            this.ExpressionType = prototype.ExpressionType;
            this.IsReferenceable = prototype.IsReferenceable;
            this.CanBeNull = prototype.CanBeNull;
            this.Syntax = syntax;
        }

        public IUserExpression ToUserExpression( ICompilation compilation )
        {
            var factory = compilation.GetCompilationModel().Factory;

            var type = this.ExpressionType != null
                ? factory.Translate( this.ExpressionType ).AssertNotNull()
                : factory.GetSpecialType( SpecialType.Object );

            return new SyntaxUserExpression( this.Syntax, type );
        }

        public static implicit operator TypedExpressionSyntax( TypedExpressionSyntaxImpl impl ) => new( impl );

        public static implicit operator TypedExpressionSyntaxImpl( TypedExpressionSyntax wrapper ) => (TypedExpressionSyntaxImpl) wrapper.Implementation;

        internal TypedExpressionSyntaxImpl(
            ExpressionSyntax syntax,
            IType? expressionType,
            CompilationModel compilationModel,
            bool? isReferenceable = null,
            bool? canBeNull = null )
        {
            Invariant.Assert( expressionType is not { TypeKind: TypeKind.Dynamic } );

            if ( expressionType == null )
            {
                TypeAnnotationMapper.TryFindExpressionTypeFromAnnotation( syntax, compilationModel, out expressionType );
            }
            else
            {
                syntax = TypeAnnotationMapper.AddExpressionTypeAnnotation( syntax, expressionType );
            }

            this.Syntax = syntax;
            this.ExpressionType = expressionType;
            this.IsReferenceable = isReferenceable ?? TypeAnnotationMapper.GetExpressionIsReferenceableFromAnnotation( syntax );

            // Infer nullability from the expression type if we have it.
            if ( canBeNull == null && expressionType is { IsNullable: not null } )
            {
                canBeNull = expressionType.IsNullable == true;
            }

            this.CanBeNull = canBeNull ?? true;
        }

        internal TypedExpressionSyntaxImpl(
            ExpressionSyntax syntax,
            CompilationModel compilationModel,
            bool? isReferenceable = null )
            : this( syntax, null, compilationModel, isReferenceable ) { }

        internal static ExpressionSyntax GetSyntaxFromValue( object? value, SyntaxSerializationContext serializationContext )
            => FromValue( value, serializationContext ).Syntax;

        internal static TypedExpressionSyntaxImpl FromValue( object? value, SyntaxSerializationContext serializationContext )
        {
            switch ( value )
            {
                case null:
                    return new TypedExpressionSyntaxImpl( SyntaxFactoryEx.Null, serializationContext.CompilationModel );

                case TypedExpressionSyntaxImpl runtimeExpression:
                    return runtimeExpression;

                case TypedExpressionSyntax runtimeExpression:
                    return runtimeExpression;

                case IExpression dynamicMember:
                    return dynamicMember.ToTypedExpressionSyntax( serializationContext );

                case ExpressionSyntax syntax:
                    return new TypedExpressionSyntaxImpl( syntax, serializationContext.CompilationModel );

                default:
                    var expression = SyntaxFactoryEx.LiteralExpressionOrNull( value );

                    if ( expression != null )
                    {
                        return new TypedExpressionSyntaxImpl(
                            expression,
                            serializationContext.CompilationModel.Factory.GetTypeByReflectionType( value.GetType() ),
                            serializationContext.CompilationModel );
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Cannot convert an instance of type {value.GetType().Name} to a run-time expression. If you are attempting to use a run-time expression as IExpression in compile-time code, that is not supported." );
                    }
            }
        }

        [return: NotNullIfNotNull( nameof(array) )]
        internal static TypedExpressionSyntaxImpl[]? FromValues( object?[]? array, SyntaxSerializationContext serializationContext )
        {
            switch ( array )
            {
                case null:
                    return null;

                default:
                    if ( array.Length == 0 )
                    {
                        return [];
                    }

                    var newArray = new TypedExpressionSyntaxImpl[array.Length];

                    for ( var i = 0; i < newArray.Length; i++ )
                    {
                        newArray[i] = FromValue( array[i], serializationContext );
                    }

                    return newArray;
            }
        }

        /// <summary>
        /// Converts the current <see cref="TypedExpressionSyntaxImpl"/> into an <see cref="ExpressionSyntax"/> and emits a cast
        /// if necessary.
        /// </summary>
        /// <param name="targetType">The target type, or <c>null</c> if no cast must be emitted in any case.</param>
        public TypedExpressionSyntaxImpl Convert( IType targetType, SyntaxGenerationContext generationContext )
        {
            var compilationModel = targetType.GetCompilationModel();

            if ( this.ExpressionType != null )
            {
                // If we know the type of the current expression, check if a cast is necessary.

                if ( compilationModel.Comparers.Default.IsConvertibleTo( this.ExpressionType, targetType, ConversionKind.Implicit ) )
                {
                    return new TypedExpressionSyntaxImpl( this.Syntax, targetType, compilationModel, this.IsReferenceable, this.CanBeNull );
                }
            }

            // We may need a cast. We are not sure, but we cannot do more. This could be removed later in the simplification step.
            var cast = (ExpressionSyntax) generationContext.SyntaxGenerator.CastExpression( targetType, this.Syntax );

            var expression = SyntaxFactory.ParenthesizedExpression( cast ).WithSimplifierAnnotationIfNecessary( generationContext );

            return new TypedExpressionSyntaxImpl( expression, targetType, compilationModel, this.IsReferenceable, this.CanBeNull );
        }

        public override string ToString() => this.Syntax.ToString();
    }
}