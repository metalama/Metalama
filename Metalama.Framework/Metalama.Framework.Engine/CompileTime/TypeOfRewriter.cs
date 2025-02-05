// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.CompileTime;

internal sealed class TypeOfRewriter
{
    private readonly SyntaxGenerationContext _syntaxGenerationContext;
    private readonly NameSyntax _compileTimeTypeName;

    public TypeOfRewriter( SyntaxGenerationContext syntaxGenerationContext )
    {
        this._syntaxGenerationContext = syntaxGenerationContext;

        this._compileTimeTypeName = (NameSyntax)
            syntaxGenerationContext.SyntaxGenerator.TypeSyntax( syntaxGenerationContext.ReflectionMapper.GetTypeSymbol( typeof(TypeOfResolver) ) );
    }

    public ExpressionSyntax RewriteTypeOf( ITypeSymbol typeSymbol, ExpressionSyntax? substitutions = null )
    {
        if ( typeSymbol is INamedTypeSymbol { IsUnboundGenericType: true } namedType
             && namedType.TypeArguments[0].Kind == SymbolKind.ErrorType )
        {
            // We have a case like typeof(Foo<>). We need to fix it here, otherwise later processing is incorrect.

            typeSymbol = namedType.OriginalDefinition;
        }

        var memberAccess =
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                this._compileTimeTypeName,
                IdentifierName( nameof(TypeOfResolver.Resolve) ) );

        if ( substitutions == null )
        {
            var typeOfString = this._syntaxGenerationContext.SyntaxGenerator.TypeOfExpression( typeSymbol ).ToString();

            return InvocationExpression( memberAccess )
                .AddArgumentListArguments(
                    new[]
                        {
                            typeOfString,
                            typeSymbol.ContainingNamespace.GetFullName(),
                            typeSymbol.GetReflectionName(),
                            typeSymbol.GetReflectionFullName(),
                            typeSymbol.GetReflectionToStringName()
                        }
                        .SelectAsArray( s => Argument( SyntaxFactoryEx.LiteralExpression( s ) ) ) );
        }
        else
        {
            return InvocationExpression( memberAccess )
                .AddArgumentListArguments(
                    Argument( SyntaxFactoryEx.LiteralExpression( typeSymbol.GetSerializableTypeId().Id ) ),
                    Argument( substitutions ) );
        }
    }
}