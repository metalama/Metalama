// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
                            Identifier( TriviaList( ElasticSpace ), p.Name, TriviaList( ElasticSpace ) ),
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
                            Identifier( TriviaList( ElasticSpace ), p.Name, TriviaList( ElasticSpace ) ),
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
    }
}