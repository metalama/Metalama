// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.CompileTimeContracts;

public interface ITemplateSyntaxFactory
{
    ICompilation Compilation { get; }

    void AddStatement( List<StatementOrTrivia> list, StatementSyntax? statement );

    void AddStatement( List<StatementOrTrivia> list, IStatement statement );

    void AddStatement( List<StatementOrTrivia> list, IExpression expression );

    void AddStatement( List<StatementOrTrivia> list, string statement );

    void AddComments( List<StatementOrTrivia> list, params string?[]? comments );

    StatementSyntax? ToStatement( ExpressionSyntax expression );

    SyntaxList<StatementSyntax> ToStatementList( List<StatementOrTrivia> list );

    SyntaxKind Boolean( bool value );

    StatementSyntax ReturnStatement( ExpressionSyntax? returnExpression );

    StatementSyntax DynamicReturnStatement( IUserExpression returnExpression, bool awaitResult );

    StatementSyntax DynamicLocalAssignment( IdentifierNameSyntax identifier, SyntaxKind kind, IUserExpression? expression, bool awaitResult );

    StatementSyntax DynamicLocalDeclaration(
        TypeSyntax type,
        SyntaxToken identifier,
        IUserExpression? value,
        bool awaitResult );

    TypedExpressionSyntax DynamicMemberAccessExpression( IUserExpression userExpression, string member );

    SyntaxToken GetUniqueIdentifier( string hint );

    ExpressionSyntax Serialize<T>( T o );

    T AddSimplifierAnnotations<T>( T node )
        where T : SyntaxNode;

    AnonymousFunctionExpressionSyntax SimplifyAnonymousFunction<T>( T node )
        where T : AnonymousFunctionExpressionSyntax;

    ExpressionSyntax RenderInterpolatedString( InterpolatedStringExpressionSyntax interpolatedString );

    ExpressionSyntax ConditionalExpression( ExpressionSyntax condition, ExpressionSyntax whenTrue, ExpressionSyntax whenFalse );

    IUserExpression Proceed( string methodName );

    IUserExpression ConfigureAwait( IUserExpression expression, bool continueOnCapturedContext );

    ExpressionSyntax? GetDynamicSyntax( object? expression );

    TypedExpressionSyntax GetTypedExpression( IExpression expression );

    TypedExpressionSyntax RunTimeExpression( ExpressionSyntax syntax, string? type = null );

    // This is to easily work around errors when we emit a redundant call to RunTimeExpression.
    [PublicAPI]
    TypedExpressionSyntax RunTimeExpression( IUserExpression syntax, string? type = null );

    ExpressionSyntax ConvertToExpressionSyntax( object value );

    IUserExpression GetUserExpression( object expression );

    ExpressionSyntax SuppressNullableWarningExpression( ExpressionSyntax operand, string? operandTypeId = null );

    IUserExpression SuppressNullableWarningUserExpression( object operand, string? type = null );

    ExpressionSyntax ConditionalAccessExpression( ExpressionSyntax expression, ExpressionSyntax whenNotNullExpression );

    ExpressionSyntax StringLiteralExpression( string? value );

    TypeOfExpressionSyntax TypeOf( string typeOfString, Dictionary<string, TypeSyntax> substitutions );

    /// <summary>
    /// Maps a template run-time type parameter name to the correct identifier in the current expansion context.
    /// When the target method's type parameter has been renamed (e.g., by an introduction), this returns
    /// the correct name based on ordinal position mapping.
    /// </summary>
    IdentifierNameSyntax RunTimeTypeParameterIdentifier( string templateParameterName );

    InterpolationSyntax FixInterpolationSyntax( InterpolationSyntax interpolation );

    ITemplateSyntaxFactory ForLocalFunction( string returnType, Dictionary<string, IType> genericArguments, bool isAsync = false );

    BlockSyntax? InvokeTemplate( string templateName, object? templateInstanceOrType = null, object? args = null );

    BlockSyntax? InvokeTemplate( TemplateInvocation templateInvocation, object? arguments = null );

    ITemplateSyntaxFactory ForTemplate( string templateName, object? templateInstanceOrType );

    TemplateTypeArgument TemplateTypeArgument( string name, Type type );

    IExpression DefineLocalVariable( List<StatementOrTrivia> statementList, string name, IType type );

    IExpression DefineLocalVariable( List<StatementOrTrivia> statementList, string name, IType type, IExpression? expression );

    IExpression DefineLocalVariable( List<StatementOrTrivia> statementList, string name, IType type, ExpressionSyntax? expression );

    IExpression DefineLocalVariable( List<StatementOrTrivia> statementList, string name, Type type );

    IExpression DefineLocalVariable( List<StatementOrTrivia> statementList, string name, Type type, IExpression? expression );

    IExpression DefineLocalVariable( List<StatementOrTrivia> statementList, string name, Type type, ExpressionSyntax? expression );

    IExpression DefineLocalVariable( List<StatementOrTrivia> statementList, string name, ExpressionSyntax expression );

    IExpression DefineLocalVariable( List<StatementOrTrivia> statementList, string name, IExpression expression );

    string EscapeIdentifier( string name );

    SyntaxToken EscapeIdentifier( SyntaxToken token );

    ExpressionSyntax RewriteAssignmentExpression( AssignmentExpressionSyntax assignmentExpression );

    /// <summary>
    /// Gets the backing field for a property template that uses the C# 14 <c>field</c> keyword.
    /// </summary>
    ExpressionSyntax GetPropertyBackingField();

    /// <summary>
    /// Registers a preferred literal text representation for a given numeric value.
    /// When the value is later emitted as a run-time literal, the preferred text is used
    /// instead of the default representation (e.g., <c>"0xff"</c> instead of <c>"255"</c>).
    /// </summary>
    void SetPreferredLiteralText( object value, string text );
}