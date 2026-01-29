// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Utilities.Roslyn
{
    internal static class SyntaxHelpers
    {
        public static ParameterListSyntax WithAdditionalParameters(
            this ParameterListSyntax parameterList,
            params (TypeSyntax Type, string Name)[] additionalParameters )
        {
            var additionalParameterSyntax =
                additionalParameters.SelectAsReadOnlyList(
                    p =>
                        Parameter(
                            List<AttributeListSyntax>(),
                            TokenList(),
                            p.Type,
                            SyntaxFactoryEx.SafeIdentifier( TriviaList( ElasticSpace ), p.Name, TriviaList( ElasticSpace ) ),
                            null ) );

            return WithAdditionalParameters( parameterList, additionalParameterSyntax );
        }

        public static ParameterListSyntax WithAdditionalParameters(
            this ParameterListSyntax parameterList,
            params IReadOnlyList<ParameterSyntax> additionalParameters )
        {
            if ( parameterList.Parameters.Count > 0 && parameterList.Parameters.Last().Modifiers.Any( m => m.IsKind( SyntaxKind.ParamsKeyword ) ) )
            {
                // Insert before params.

                return parameterList
                    .WithParameters(
                        parameterList.Parameters.InsertRange(
                            parameterList.Parameters.Count - 1,
                            additionalParameters ) );
            }
            else
            {
                // Insert last.

                return parameterList
                    .WithParameters( parameterList.Parameters.AddRange( additionalParameters ) );
            }
        }

        public static BracketedParameterListSyntax WithAdditionalParameters(
            this BracketedParameterListSyntax parameterList,
            params (TypeSyntax Type, string Name)[] additionalParameters )
        {
            var additionalParameterSyntax =
                additionalParameters.SelectAsReadOnlyList(
                    p =>
                        Parameter(
                            List<AttributeListSyntax>(),
                            TokenList(),
                            p.Type,
                            SyntaxFactoryEx.SafeIdentifier( TriviaList( ElasticSpace ), p.Name, TriviaList( ElasticSpace ) ),
                            default ) );

            if ( parameterList.Parameters.Last().Modifiers.Any( m => m.IsKind( SyntaxKind.ParamsKeyword ) ) )
            {
                // Insert before params.

                return parameterList
                    .WithParameters(
                        parameterList.Parameters.InsertRange(
                            parameterList.Parameters.Count - 1,
                            additionalParameterSyntax ) );
            }
            else
            {
                // Insert last.

                return parameterList
                    .WithParameters( parameterList.Parameters.AddRange( additionalParameterSyntax ) );
            }
        }

        /// <summary>
        /// Checks if a property accessor syntax node contains the C# 14 <c>field</c> keyword expression.
        /// </summary>
        public static bool ContainsFieldExpression( AccessorDeclarationSyntax accessor )
        {
#if ROSLYN_5_0_0_OR_GREATER
            return accessor.DescendantNodesAndSelf().OfType<FieldExpressionSyntax>().Any();
#else
            return false;
#endif
        }

        /// <summary>
        /// Checks if an accessor contains an assignment to the <c>field</c> keyword.
        /// This includes simple assignments (field = value), compound assignments (field += x),
        /// increment/decrement operators (field++, --field), and passing field as out/ref argument.
        /// </summary>
        public static bool ContainsFieldAssignment( AccessorDeclarationSyntax accessor )
        {
#if ROSLYN_5_0_0_OR_GREATER
            return accessor.DescendantNodesAndSelf().Any( IsFieldAssignment );
#else
            return false;
#endif
        }

#if ROSLYN_5_0_0_OR_GREATER
        private static bool IsFieldAssignment( SyntaxNode node )
        {
            return node switch
            {
                // field = value, field += x, field -= x, etc.
                AssignmentExpressionSyntax { Left: FieldExpressionSyntax } => true,

                // ++field, --field
                PrefixUnaryExpressionSyntax { Operand: FieldExpressionSyntax } op
                    when op.IsKind( SyntaxKind.PreIncrementExpression ) || op.IsKind( SyntaxKind.PreDecrementExpression ) => true,

                // field++, field--
                PostfixUnaryExpressionSyntax { Operand: FieldExpressionSyntax } op
                    when op.IsKind( SyntaxKind.PostIncrementExpression ) || op.IsKind( SyntaxKind.PostDecrementExpression ) => true,

                // Method(out field), Method(ref field)
                ArgumentSyntax { Expression: FieldExpressionSyntax } arg
                    when arg.RefOrOutKeyword.IsKind( SyntaxKind.OutKeyword ) || arg.RefOrOutKeyword.IsKind( SyntaxKind.RefKeyword ) => true,

                _ => false
            };
        }
#endif
    }
}