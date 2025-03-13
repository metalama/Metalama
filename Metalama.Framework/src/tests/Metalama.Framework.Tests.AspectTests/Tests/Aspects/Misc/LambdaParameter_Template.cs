// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.LambdaParameter_Template;

internal class Aspect : PropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IProperty> builder )
    {
        base.BuildAspect( builder );

        builder.With( builder.Target.DeclaringType ).IntroduceMethod( nameof(PropertyBody), args: new { property = builder.Target } );
    }

    [Template]
    private string? PropertyBody( IProperty property )
    {
        var methodBody = property.GetSymbol()
            ?.DeclaringSyntaxReferences
            .Select( r => r.GetSyntax() )
            .Cast<PropertyDeclarationSyntax>()
            .Select(
                SyntaxNode? ( p ) => p.ExpressionBody ??
                                     ( p.AccessorList?.Accessors.SingleOrDefault( a => a.Keyword.IsKind( SyntaxKind.GetKeyword ) ) is var getter
                                         ? ( (SyntaxNode?)getter?.ExpressionBody ?? getter?.Body )
                                         : null ) )
            .WhereNotNull()
            .FirstOrDefault();

        return methodBody?.ToString();
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