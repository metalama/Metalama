// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SpecialType = Metalama.Framework.Code.SpecialType;

namespace Metalama.Framework.Engine.Templating;

internal sealed partial class TemplateExpansionContext
{
    internal sealed class ConfigureAwaitUserExpression : UserExpression
    {
        private readonly IUserExpression _expression;
        private readonly bool _continueOnCapturedContext;

        public ConfigureAwaitUserExpression( IUserExpression expression, bool continueOnCapturedContext )
        {
            this._expression = expression;
            this._continueOnCapturedContext = continueOnCapturedContext;
        }

        public override IType Type
        {
            get
            {
                var expressionType = (INamedType) this._expression.Type;

                return expressionType.Methods.OfExactSignature( nameof(Task.ConfigureAwait), [TypeFactory.GetType( SpecialType.Boolean )] )!.ReturnType;
            }
        }

        protected override ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null )
        {
            var generatedExpression = this._expression.ToExpressionSyntax( syntaxSerializationContext, targetType );

            // generatedExpression.ConfigureAwait(true/false)
            return InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        generatedExpression,
                        IdentifierName( nameof(Task.ConfigureAwait) ) ) )
                .AddArgumentListArguments( Argument( SyntaxFactoryEx.LiteralExpression( this._continueOnCapturedContext ) ) );
        }

        protected override string ToStringCore() => $"{this._expression}.ConfigureAwait({this._continueOnCapturedContext})";
    }
}