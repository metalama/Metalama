// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using PostSharp.Collections;
using System;

namespace PostSharp.Reflection.MethodBody
{
    /// <summary>
    /// The source code of method bodies and expression bodies is not represented in the high-level API of Metalama.
    /// If you need to access source code from an aspect, you must implement a service using Metalama SDK. 
    /// </summary>
    /// <seealso href="@roslyn-api"/>
    [Obsolete( "", true )]
    public abstract class MethodBodyVisitor
    {
        public virtual object VisitMethodBody( IMethodBody methodBody )
        {
            if ( methodBody.RootBlock != null )
            {
                this.VisitBlockExpression( methodBody.RootBlock );
            }

            return methodBody;
        }

        public virtual object VisitBlockExpression( IBlockExpression instructionBlock )
        {
            foreach ( var instruction in LinkedListExtensions.ToEnumerable( instructionBlock.Items ) )
            {
                this.VisitSyntaxElement( instruction );
            }

            return instructionBlock;
        }

        public virtual object VisitStatementExpression( IStatementExpression statementExpression )
        {
            this.VisitSyntaxElement( statementExpression.Expression );

            return statementExpression;
        }

        public virtual object VisitSyntaxElement( IMethodBodyElement syntaxElement )
        {
            switch ( syntaxElement.MethodBodyElementKind )
            {
                case MethodBodyElementKind.Add:
                case MethodBodyElementKind.AddChecked:
                case MethodBodyElementKind.And:
                case MethodBodyElementKind.ArrayIndex:
                case MethodBodyElementKind.Assign:
                case MethodBodyElementKind.Divide:
                case MethodBodyElementKind.LessThan:
                case MethodBodyElementKind.LessThanOrEqual:
                case MethodBodyElementKind.Modulo:
                case MethodBodyElementKind.Multiply:
                case MethodBodyElementKind.MultiplyChecked:
                case MethodBodyElementKind.GreaterThan:
                case MethodBodyElementKind.GreaterThanOrEqual:
                case MethodBodyElementKind.SubstractChecked:
                case MethodBodyElementKind.Substract:
                case MethodBodyElementKind.Equal:
                case MethodBodyElementKind.Different:
                case MethodBodyElementKind.Or:
                case MethodBodyElementKind.Xor:
                case MethodBodyElementKind.ShiftLeft:
                case MethodBodyElementKind.ShiftRight:
                    return this.VisitBinaryExpression( (IBinaryExpression) syntaxElement );

                case MethodBodyElementKind.ValueOf:
                    return this.VisitValueOfExpression( (IValueOfExpression) syntaxElement );

                case MethodBodyElementKind.AddressOf:
                    return this.VisitAddressOfExpression( (IAddressOfExpression) syntaxElement );

                case MethodBodyElementKind.ArrayLength:
                case MethodBodyElementKind.Cast:

                case MethodBodyElementKind.Not:
                case MethodBodyElementKind.Negate:
                case MethodBodyElementKind.SafeCast:
                case MethodBodyElementKind.Unbox:
                case MethodBodyElementKind.Throw:
                case MethodBodyElementKind.Box:
                case MethodBodyElementKind.TypedReferenceValue:
                case MethodBodyElementKind.CheckFinite:
                case MethodBodyElementKind.MakeTypedReference:
                case MethodBodyElementKind.LocalAlloc:
                case MethodBodyElementKind.TypedReferenceType:
                case MethodBodyElementKind.Return:
                case MethodBodyElementKind.Convert:
                case MethodBodyElementKind.ConvertChecked:
                    return this.VisitUnaryExpression( (IUnaryExpression) syntaxElement );

                case MethodBodyElementKind.Constant:
                    return this.VisitConstantExpression( (IConstantExpression) syntaxElement );

                case MethodBodyElementKind.Conditional:
                    return this.VisitConditionalExpression( (IConditionalExpression) syntaxElement );

                case MethodBodyElementKind.CopyBuffer:
                    return this.VisitCopyBufferExpression( (ICopyBufferExpression) syntaxElement );

                case MethodBodyElementKind.Field:
                    return this.VisitFieldExpression( (IFieldExpression) syntaxElement );

                case MethodBodyElementKind.Goto:
                    return this.VisitGotoExpression( (IGotoExpression) syntaxElement );

                case MethodBodyElementKind.InitBuffer:
                    return this.VisitInitBufferExpression( (IInitBufferExpression) syntaxElement );

                case MethodBodyElementKind.Variable:
                    return this.VisitVariableExpression( (ILocalVariableExpression) syntaxElement );

                case MethodBodyElementKind.LoadToken:
                case MethodBodyElementKind.DefaultValue:
                case MethodBodyElementKind.SizeOf:
                    return this.VisitMetadataExpression( (IMetadataExpression) syntaxElement );

                case MethodBodyElementKind.MethodCall:
                    return this.VisitMethodCallExpression( (IMethodCallExpression) syntaxElement );

                case MethodBodyElementKind.NewObject:
                    return this.VisitNewObjectExpression( (INewObjectExpression) syntaxElement );

                case MethodBodyElementKind.MethodPointer:
                    return this.VisitMethodPointerExpression( (IMethodPointerExpression) syntaxElement );

                case MethodBodyElementKind.NewArray:
                    return this.VisitNewArrayExpression( (INewArrayExpression) syntaxElement );

                case MethodBodyElementKind.Parameter:
                    return this.VisitParameterExpression( (IParameterExpression) syntaxElement );

                case MethodBodyElementKind.ArgumentList:
                case MethodBodyElementKind.This:
                case MethodBodyElementKind.Rethrow:
                    return this.VisitZeroaryExpression( (IZeroaryExpression) syntaxElement );

                case MethodBodyElementKind.Switch:
                    return this.VisitSwitchExpression( (ISwitchExpression) syntaxElement );

                case MethodBodyElementKind.Block:
                    return this.VisitBlockExpression( (IBlockExpression) syntaxElement );

                case MethodBodyElementKind.Statement:
                    return this.VisitStatementExpression( (IStatementExpression) syntaxElement );

                case MethodBodyElementKind.MethodBody:
                    return this.VisitMethodBody( (IMethodBody) syntaxElement );

                case MethodBodyElementKind.LocalVariableDefinition:
                    return this.VisitLocalVariable( (ILocalVariable) syntaxElement );

                default:
                    throw new ArgumentOutOfRangeException( nameof(syntaxElement) );
            }
        }

        public virtual object VisitAddressOfExpression( IAddressOfExpression expression )
        {
            this.VisitUnaryExpression( expression );

            return expression;
        }

        public virtual object VisitValueOfExpression( IValueOfExpression expression )
        {
            this.VisitUnaryExpression( expression );

            return expression;
        }

        public virtual object VisitLocalVariable( ILocalVariable localVariable )
        {
            return localVariable;
        }

        public virtual object VisitSwitchExpression( ISwitchExpression expression )
        {
            this.VisitSyntaxElement( expression.Condition );

            foreach ( var target in expression.Targets )
            {
                this.VisitSyntaxElement( target );
            }

            return expression;
        }

        public virtual object VisitZeroaryExpression( IZeroaryExpression expression )
        {
            return expression;
        }

        public virtual object VisitParameterExpression( IParameterExpression expression )
        {
            return expression;
        }

        public virtual object VisitNewArrayExpression( INewArrayExpression expression )
        {
            this.VisitSyntaxElement( expression.Length );

            return expression;
        }

        public virtual object VisitMethodPointerExpression( IMethodPointerExpression expression )
        {
            return expression;
        }

        public virtual object VisitNewObjectExpression( INewObjectExpression expression )
        {
            foreach ( var argument in expression.Arguments )
            {
                this.VisitSyntaxElement( argument );
            }

            return expression;
        }

        public virtual object VisitMethodCallExpression( IMethodCallExpression expression )
        {
            if ( expression.Instance != null )
            {
                this.VisitSyntaxElement( expression.Instance );
            }

            foreach ( var argument in expression.Arguments )
            {
                this.VisitSyntaxElement( argument );
            }

            return expression;
        }

        public virtual object VisitMetadataExpression( IMetadataExpression expression )
        {
            return expression;
        }

        public virtual object VisitVariableExpression( ILocalVariableExpression expression )
        {
            return expression;
        }

        public virtual object VisitInitBufferExpression( IInitBufferExpression expression )
        {
            this.VisitSyntaxElement( expression.Buffer );

            return expression;
        }

        public virtual object VisitGotoExpression( IGotoExpression expression )
        {
            return expression;
        }

        public virtual object VisitFieldExpression( IFieldExpression expression )
        {
            if ( expression.Instance != null )
            {
                this.VisitSyntaxElement( expression.Instance );
            }

            return expression;
        }

        public virtual object VisitBinaryExpression( IBinaryExpression expression )
        {
            this.VisitSyntaxElement( expression.Left );
            this.VisitSyntaxElement( expression.Right );

            return expression;
        }

        public virtual object VisitUnaryExpression( IUnaryExpression expression )
        {
            if ( expression.Value != null )
            {
                this.VisitSyntaxElement( expression.Value );
            }

            return expression;
        }

        public virtual object VisitConditionalExpression( IConditionalExpression expression )
        {
            this.VisitSyntaxElement( expression.Condition );

            if ( expression.IfTrue != null )
            {
                this.VisitSyntaxElement( expression.IfTrue );
            }

            if ( expression.IfFalse != null )
            {
                this.VisitSyntaxElement( expression.IfFalse );
            }

            return expression;
        }

        public virtual object VisitConstantExpression( IConstantExpression expression )
        {
            return expression;
        }

        public virtual object VisitCopyBufferExpression( ICopyBufferExpression expression )
        {
            this.VisitSyntaxElement( expression.Destination );
            this.VisitSyntaxElement( expression.Source );
            this.VisitSyntaxElement( expression.Length );

            return expression;
        }
    }
}