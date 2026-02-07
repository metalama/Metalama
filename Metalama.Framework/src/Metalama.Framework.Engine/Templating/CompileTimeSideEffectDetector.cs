// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using static Metalama.Framework.Engine.CompileTime.TemplatingScope;

namespace Metalama.Framework.Engine.Templating;

/// <summary>
/// Determines whether a compile-time expression statement has side effects that are visible outside of
/// a run-time conditional block. This is used to report LAMA0288 when a compile-time method call with
/// side effects appears inside a run-time conditional block, where the mutation of compile-time state
/// would depend on a run-time condition.
/// </summary>
/// <remarks>
/// <para>
/// The detector uses heuristics to identify compile-time side effects. A compile-time expression statement
/// is assumed to have visible side effects unless proven otherwise. The following heuristics are used:
/// </para>
/// <list type="bullet">
/// <item>An invocation on a compile-time local variable declared outside the run-time conditional block
/// (e.g., <c>stringBuilder.Append(", ")</c>).</item>
/// <item>An invocation on a compile-time field or property (e.g., <c>_items.Add("x")</c>).</item>
/// <item>A static compile-time method call (e.g., <c>CompileTimeHelper.AddItem("x")</c>).</item>
/// </list>
/// <para>
/// The following are excluded as they do not produce compile-time side effects:
/// </para>
/// <list type="bullet">
/// <item>Sub-template invocations, which generate run-time code.</item>
/// <item>Compile-time members that return run-time values (e.g., <c>IMethod.Invoke()</c>).</item>
/// <item>Invocations on compile-time locals declared inside the run-time conditional block,
/// since their state does not leak outside the block.</item>
/// </list>
/// </remarks>
internal sealed class CompileTimeSideEffectDetector
{
    private readonly SyntaxTreeAnnotationMap _syntaxTreeAnnotationMap;
    private readonly TemplateMemberClassifier _templateMemberClassifier;
    private readonly Func<SyntaxNode?, TemplatingScope> _getNodeScope;

    public CompileTimeSideEffectDetector(
        SyntaxTreeAnnotationMap syntaxTreeAnnotationMap,
        TemplateMemberClassifier templateMemberClassifier,
        Func<SyntaxNode?, TemplatingScope> getNodeScope )
    {
        this._syntaxTreeAnnotationMap = syntaxTreeAnnotationMap;
        this._templateMemberClassifier = templateMemberClassifier;
        this._getNodeScope = getNodeScope;
    }

    /// <summary>
    /// Determines whether a compile-time expression statement has side effects that are visible outside
    /// the top-level run-time conditional block.
    /// </summary>
    /// <param name="expression">The expression from an <see cref="ExpressionStatementSyntax"/> (the original,
    /// unvisited expression).</param>
    /// <param name="expressionScope">The templating scope of the visited expression.</param>
    /// <param name="runTimeConditionalBlockVariables">The set of compile-time locals declared inside
    /// the run-time conditional block, or <c>null</c> if not inside a run-time conditional block.</param>
    /// <returns><c>true</c> if the expression has compile-time side effects that would be visible
    /// outside the run-time conditional block; <c>false</c> otherwise.</returns>
    public bool HasCompileTimeSideEffect(
        ExpressionSyntax expression,
        TemplatingScope expressionScope,
        List<ILocalSymbol>? runTimeConditionalBlockVariables )
    {
        // Only consider compile-time expression statements (not those that produce run-time values).
        if ( expressionScope.GetExpressionExecutionScope() != CompileTimeOnly )
        {
            return false;
        }

        // Compile-time members returning run-time values (e.g. IMethod.Invoke()) are bridges to run-time
        // code and do not have compile-time side effects.
        if ( expressionScope.IsCompileTimeMemberReturningRunTimeValue() )
        {
            return false;
        }

        // Sub-template invocations generate run-time code and do not have compile-time side effects.
        if ( this.IsSubtemplateInvocation( expression ) )
        {
            return false;
        }

        // meta.InsertComment, meta.InsertStatement, meta.InvokeTemplate, and meta.Return are
        // compile-time methods that generate run-time code (add statements/comments to the template output).
        // They do not have compile-time side effects on user state.
        if ( this.IsCodeGeneratingMetaMethod( expression ) )
        {
            return false;
        }

        // Assignments (=, +=, -=, etc.) and increment/decrement (++, --) are already handled
        // by the existing LAMA0108 diagnostic via CheckForMutatingCompileTimeExpressionInRunTimeConditionalBlock
        // and VisitUnaryExpressionOperand. We do not report LAMA0288 for these.
        if ( expression is AssignmentExpressionSyntax or PrefixUnaryExpressionSyntax or PostfixUnaryExpressionSyntax )
        {
            return false;
        }

        // Check if this is an invocation expression statement.
        if ( expression.IsKind( SyntaxKind.InvocationExpression )
             && expression is InvocationExpressionSyntax invocation )
        {
            return this.HasInvocationSideEffect( invocation, runTimeConditionalBlockVariables );
        }

        // For non-invocation compile-time expression statements, we conservatively assume they have
        // side effects (e.g. compile-time delegates or other exotic scenarios).
        return true;
    }

    /// <summary>
    /// Determines whether a compile-time invocation has side effects visible outside the run-time
    /// conditional block.
    /// </summary>
    private bool HasInvocationSideEffect(
        InvocationExpressionSyntax invocation,
        List<ILocalSymbol>? runTimeConditionalBlockVariables )
    {
        if ( invocation.Expression.IsKind( SyntaxKind.SimpleMemberAccessExpression )
             && invocation.Expression is MemberAccessExpressionSyntax memberAccess )
        {
            // Instance method call: e.g. stringBuilder.Append(", ") or _items.Add("x").
            var receiverScope = this._getNodeScope( memberAccess.Expression ).GetExpressionExecutionScope();

            if ( receiverScope != CompileTimeOnly )
            {
                // The receiver is not compile-time, so the call does not have compile-time side effects.
                return false;
            }

            var receiverSymbol = this._syntaxTreeAnnotationMap.GetSymbol( memberAccess.Expression );

            // Method call on a compile-time local variable: check if the local was declared
            // inside the run-time conditional block.
            if ( receiverSymbol is { Kind: SymbolKind.Local } and ILocalSymbol local
                 && runTimeConditionalBlockVariables != null
                 && runTimeConditionalBlockVariables.Contains( local ) )
            {
                // The local is declared inside the run-time conditional block,
                // so the side effect is not visible outside.
                return false;
            }

            // The call is on a compile-time receiver (local declared outside, field, property, etc.)
            // and is assumed to have visible side effects.
            return true;
        }

        // Simple name call (no explicit receiver, e.g. ThrowIfReached()) or other forms.
        // Without an explicit receiver, we cannot determine the target of the side effect.
        // These calls are not flagged because the common harmful pattern is a method call on
        // a compile-time receiver (e.g. stringBuilder.Append, list.Add).
        return false;
    }

    /// <summary>
    /// Determines whether the expression is a call to a <c>meta</c> method that generates run-time code
    /// (e.g., <c>meta.InsertComment</c>, <c>meta.InsertStatement</c>, <c>meta.InvokeTemplate</c>, <c>meta.Return</c>).
    /// These methods add statements or comments to the template output and do not have compile-time side effects.
    /// </summary>
    private bool IsCodeGeneratingMetaMethod( ExpressionSyntax expression )
    {
        if ( !expression.IsKind( SyntaxKind.InvocationExpression )
             || expression is not InvocationExpressionSyntax invocation )
        {
            return false;
        }

        var metaMemberKind = this._templateMemberClassifier.GetMetaMemberKind( invocation.Expression );

        return metaMemberKind is MetaMemberKind.InsertComment
            or MetaMemberKind.InsertStatement
            or MetaMemberKind.InvokeTemplate
            or MetaMemberKind.Return;
    }

    /// <summary>
    /// Determines whether the expression is a sub-template invocation. Sub-template calls
    /// generate run-time code and do not have compile-time side effects.
    /// </summary>
    private bool IsSubtemplateInvocation( ExpressionSyntax expression )
    {
        if ( !expression.IsKind( SyntaxKind.InvocationExpression )
             || expression is not InvocationExpressionSyntax invocation )
        {
            return false;
        }

        var symbol = this._syntaxTreeAnnotationMap.GetInvocableSymbol( invocation.Expression );

        if ( symbol == null )
        {
            return false;
        }

        return this._templateMemberClassifier.SymbolClassifier.GetTemplateInfo( symbol ).CanBeReferencedAsSubtemplate;
    }
}
