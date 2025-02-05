using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.LambdaParameter_BuildAspect;

internal class Aspect : PropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IProperty> builder )
    {
        base.BuildAspect( builder );

        var propertyBody = builder.Target.GetSymbol()
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
            .FirstOrDefault()
            ?.ToString();

        builder.With( builder.Target.DeclaringType ).IntroduceMethod( nameof(PropertyBody), args: new { propertyBody } );
    }

    [Template]
    private string? PropertyBody( [CompileTime] string propertyBody ) => propertyBody;
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