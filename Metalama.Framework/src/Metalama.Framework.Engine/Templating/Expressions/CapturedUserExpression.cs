// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.SyntaxSerialization;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Templating.Expressions;

internal sealed class CapturedUserExpression : UserExpression
{
    private readonly ICompilation _compilation;
    private readonly object? _expression;

    private CapturedUserExpression( ICompilation compilation, object? expression )
    {
        this._compilation = compilation;
        this._expression = expression;
    }

   
    

    public static IExpression Create( ICompilation compilation, object? expression )
        => expression switch
        {
            IExpression typedExpression => typedExpression,
            _ => new CapturedUserExpression( compilation, expression )
        };

    public static IExpression[] Create( ICompilation compilation, IReadOnlyList<object?>? expressions )
        => expressions switch
        {
            null => [],
            [IReadOnlyList<IExpression> expressionList] => expressionList.ToArray(),
            _ => expressions.SelectAsArray( e => Create( compilation, e ) ) 
        };

    private protected override bool? IsReferenceable
        => this._expression switch
        {
            null => false,
            TypedExpressionSyntaxImpl typedExpressionSyntaxImpl => typedExpressionSyntaxImpl.IsReferenceable,
            TypedExpressionSyntax typedExpressionSyntax => typedExpressionSyntax.IsReferenceable,
            _ => null
        };

    protected override bool CanBeNull 
        => this._expression switch
        {
            null => false,
            TypedExpressionSyntaxImpl typedExpressionSyntaxImpl => typedExpressionSyntaxImpl.CanBeNull,
            _ => this.Type.IsNullable != false
        };

    public override IType Type
        => this._expression switch
           {
               TypedExpressionSyntaxImpl { ExpressionType: { } expressionType } =>
                   this._compilation.GetCompilationModel().Factory.Translate( expressionType ),
               TypedExpressionSyntax { ExpressionType: { } expressionType }
                   => this._compilation.GetCompilationModel().Factory.Translate( expressionType ),
               ExpressionSyntax expressionSyntax => TypeAnnotationMapper.TryFindExpressionTypeFromAnnotation(
                   expressionSyntax,
                   this._compilation.GetCompilationModel(),
                   out var type )
                   ? type
                   : null,
               _ => null
           } ??
           this._compilation.Factory.GetSpecialType( SpecialType.Object );

    protected override ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null )
        => TypedExpressionSyntaxImpl.FromValue( this._expression, syntaxSerializationContext ).Syntax;
}