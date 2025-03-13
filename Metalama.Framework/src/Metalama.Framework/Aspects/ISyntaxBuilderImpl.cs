// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using System;
using System.Collections.Immutable;
using System.Text;

namespace Metalama.Framework.Aspects;

[CompileTime]
internal interface ISyntaxBuilderImpl
{
    ICompilation Compilation { get; }

    IExpression Capture( object? expression );

    IExpression BuildArray( ArrayBuilder arrayBuilder );

    IExpression BuildInterpolatedString( InterpolatedStringBuilder interpolatedStringBuilder );

    IExpression ParseExpression( string code, IType? type, bool? isReferenceable );

    IStatement ParseStatement( string code );

    IStatement CreateExpressionStatement( IExpression expression );

    void AppendLiteral( object? value, StringBuilder stringBuilder, SpecialType specialType, bool stronglyTyped );

    IExpression Literal( object? value, SpecialType specialType, bool stronglyTyped );

    void AppendTypeName( IType type, StringBuilder stringBuilder );

    void AppendTypeName( Type type, StringBuilder stringBuilder );

    void AppendExpression( IExpression expression, StringBuilder stringBuilder );

    void AppendDynamic( object? expression, StringBuilder stringBuilder );

    IExpression Cast( IExpression expression, IType targetType );

    object TypedConstant( in TypedConstant typedConstant );

    IExpression ThisExpression( INamedType type );

    IExpression ToExpression( IFieldOrProperty fieldOrProperty, IExpression? instance );

    IExpression ToExpression( IParameter parameter );

    IExpression WithType( IExpression expression, IType type );

    IStatement CreateTemplateStatement( TemplateInvocation templateInvocation, object? args );

    IStatement CreateSwitchStatement( IExpression expression, ImmutableArray<SwitchStatementSection> cases );

    IStatementList UnwrapBlock( IStatement statement );

    IStatementList CreateStatementList( ImmutableArray<object> items );

    IStatement CreateBlock( IStatementList statements );

    IExpression NullExpression( IType? type );

    IExpression DefaultExpression( IType? type );
}