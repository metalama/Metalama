// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CompileTime;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.Templating
{
    internal sealed partial class TemplateAnnotator
    {
        private sealed class ScopeContext
        {
            private readonly TemplatingScope _preferredScope;

            public static ScopeContext Default
                => new( TemplatingScope.RunTimeOrCompileTime, false, null, null, TemplatingScope.RunTimeOrCompileTime, null, false, null );

            public TemplatingScope CurrentBreakOrContinueScope { get; }

            /// <summary>
            /// Gets a value indicating whether the current expression is obliged to be compile-time-only.
            /// </summary>
            public bool ForceCompileTimeOnlyExpression => this._preferredScope == TemplatingScope.CompileTimeOnly;

            public bool PreferRunTimeExpression => this._preferredScope == TemplatingScope.RunTimeOnly;

            /// <summary>
            /// Gets a value indicating whether the current node is guarded by a conditional statement where the condition is a run-time-only
            /// expression.
            /// </summary>
            [MemberNotNullWhen( true, nameof(IsRunTimeConditionalBlockReason), nameof(RunTimeConditionalBlockVariables) )]
            public bool IsRunTimeConditionalBlock { get; }

            public string? IsRunTimeConditionalBlockReason { get; }

            /// <summary>
            /// Gets compile-time local variables declared directly within the current run-time conditional block.
            /// </summary>
            public List<ILocalSymbol>? RunTimeConditionalBlockVariables { get; }

            public string? PreferredScopeReason { get; }

            public bool IsDynamicTypingForbidden { get; }

            private string? ForbidDynamicTypingReason { get; }

            private ScopeContext(
                TemplatingScope currentBreakOrContinueScope,
                bool isRunTimeConditionalBlock,
                string? isRunTimeConditionalBlockReason,
                List<ILocalSymbol>? runTimeConditionalBlockVariables,
                TemplatingScope preferredScope,
                string? preferredScopeReason,
                bool isDynamicTypingForbidden,
                string? forbidDynamicTypingReason )
            {
                this.CurrentBreakOrContinueScope = currentBreakOrContinueScope;
                this.IsRunTimeConditionalBlock = isRunTimeConditionalBlock;
                this.IsRunTimeConditionalBlockReason = isRunTimeConditionalBlockReason;
                this.RunTimeConditionalBlockVariables = runTimeConditionalBlockVariables;
                this._preferredScope = preferredScope;
                this.PreferredScopeReason = preferredScopeReason;
                this.IsDynamicTypingForbidden = isDynamicTypingForbidden;
                this.ForbidDynamicTypingReason = forbidDynamicTypingReason;
            }

            /// <summary>
            /// Enters an expression branch that must be compile-time because the parent must be
            /// compile-time.
            /// </summary>
            /// <returns>A cookie to dispose at the end.</returns>
            public ScopeContext CompileTimeOnly( string reason )
                => new(
                    this.CurrentBreakOrContinueScope,
                    this.IsRunTimeConditionalBlock,
                    this.IsRunTimeConditionalBlockReason,
                    this.RunTimeConditionalBlockVariables,
                    TemplatingScope.CompileTimeOnly,
                    reason,
                    this.IsDynamicTypingForbidden,
                    this.ForbidDynamicTypingReason );

            public ScopeContext RunTimeOrCompileTime( string reason )
                => new(
                    this.CurrentBreakOrContinueScope,
                    this.IsRunTimeConditionalBlock,
                    this.IsRunTimeConditionalBlockReason,
                    this.RunTimeConditionalBlockVariables,
                    TemplatingScope.RunTimeOrCompileTime,
                    reason,
                    this.IsDynamicTypingForbidden,
                    this.ForbidDynamicTypingReason );

            public ScopeContext RunTimePreferred( string reason )
                => new(
                    this.CurrentBreakOrContinueScope,
                    this.IsRunTimeConditionalBlock,
                    this.IsRunTimeConditionalBlockReason,
                    this.RunTimeConditionalBlockVariables,
                    TemplatingScope.RunTimeOnly,
                    reason,
                    this.IsDynamicTypingForbidden,
                    this.ForbidDynamicTypingReason );

            /// <summary>
            /// Enters a branch of the syntax tree whose execution depends on a run-time-only condition.
            /// Compile-time local variables can be modified within such a branch only if they are declared directly within that branch.
            /// </summary>
            public ScopeContext RunTimeConditional( string reason )
                => new(
                    this.CurrentBreakOrContinueScope,
                    isRunTimeConditionalBlock: true,
                    isRunTimeConditionalBlockReason: reason,
                    runTimeConditionalBlockVariables: new List<ILocalSymbol>(),
                    this._preferredScope,
                    this.PreferredScopeReason,
                    this.IsDynamicTypingForbidden,
                    this.ForbidDynamicTypingReason );

            public ScopeContext BreakOrContinue( TemplatingScope scope, string reason )
                => new(
                    scope,
                    scope == TemplatingScope.RunTimeOnly || this.IsRunTimeConditionalBlock,
                    scope == TemplatingScope.RunTimeOnly ? reason : this.IsRunTimeConditionalBlockReason,
                    scope == TemplatingScope.RunTimeOnly ? new List<ILocalSymbol>() : this.RunTimeConditionalBlockVariables,
                    this._preferredScope,
                    this.PreferredScopeReason,
                    this.IsDynamicTypingForbidden,
                    this.ForbidDynamicTypingReason );

            public ScopeContext ForbidDynamic( string reason )
                => new(
                    this.CurrentBreakOrContinueScope,
                    this.IsRunTimeConditionalBlock,
                    this.IsRunTimeConditionalBlockReason,
                    this.RunTimeConditionalBlockVariables,
                    this._preferredScope,
                    this.PreferredScopeReason,
                    true,
                    reason );
        }
    }
}