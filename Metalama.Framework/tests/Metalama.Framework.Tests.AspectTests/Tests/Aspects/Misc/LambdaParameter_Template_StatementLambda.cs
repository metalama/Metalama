using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.LambdaParameter_Template_StatementLambda;

internal class Aspect : PropertyAspect
{
    [Introduce]
    private string? PropertyBody
    {
        get
        {
            {
                var methodBody = meta.Target.Property.GetSymbol()
                    ?.DeclaringSyntaxReferences
                    .Select( r => r.GetSyntax() )
                    .Cast<PropertyDeclarationSyntax>()
                    .Select(
                        SyntaxNode? ( p ) =>
                        {
                            if (p.ExpressionBody != null)
                            {
                                return p.ExpressionBody;
                            }

                            var getter = p.AccessorList?.Accessors
                                .SingleOrDefault( a => a.Keyword.IsKind( SyntaxKind.GetKeyword ) );

                            return (SyntaxNode?)getter?.ExpressionBody ?? getter?.Body;
                        } )
                    .WhereNotNull()
                    .FirstOrDefault();

                return methodBody?.ToString();
            }
        }
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    public int P
    {
        get => 42;
    }
}