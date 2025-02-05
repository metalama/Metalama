// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Code.SyntaxBuilders;

/// <summary>
/// Builds a <c>switch</c> statement.
/// </summary>
[CompileTime]
[PublicAPI]
public sealed class SwitchStatementBuilder : IStatementBuilder
{
    private readonly IExpression _expression;
    private readonly List<SwitchStatementSection> _cases = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SwitchStatementBuilder"/> class.
    /// </summary>
    /// <param name="expression">The expression in the <c>switch ( expression )</c> statement.</param>
    public SwitchStatementBuilder( IExpression expression )
    {
        this._expression = expression;
    }

    /// <summary>
    /// Add a <c>case</c> switch section. This overload accepts an <see cref="IStatementList"/>.
    /// </summary>
    /// <param name="label">The label, i.e. the value to match.</param>
    /// <param name="statements">The statements to execute.</param>
    /// <param name="appendBreak">Value indicating whether a <c>break;</c> statement should be appended to <paramref name="statements"/>. The default value is <c>true</c>.</param>
    public void AddCase( SwitchStatementLabel label, IStatementList statements, bool appendBreak = true )
    {
        this._cases.Add( new SwitchStatementSection( label, null, statements, appendBreak ) );
    }

    /// <summary>
    /// Add a <c>case</c> switch section. This overload accepts an <see cref="IStatement"/>.
    /// </summary>
    /// <param name="label">The label, i.e. the value to match.</param>
    /// <param name="statement">The statement to execute. To call a template, see <see cref="StatementFactory.FromTemplate(Metalama.Framework.Aspects.TemplateInvocation,object?)"/>.</param>
    /// <param name="appendBreak">Value indicating whether a <c>break;</c> statement should be appended to <paramref name="statement"/>. The default value is <c>true</c>.</param>
    public void AddCase( SwitchStatementLabel label, IStatement statement, bool appendBreak = true )
    {
        this._cases.Add( new SwitchStatementSection( label, null, statement.AsList(), appendBreak ) );
    }

    /// <summary>
    /// Add a <c>case</c> switch section with a <c>when</c> expression. This overload accepts an <see cref="IStatementList"/>.
    /// </summary>
    /// <param name="label">The label, i.e. the value to match.</param>
    /// <param name="when">The <c>when</c> expression.</param>
    /// <param name="statements">The statements to execute.</param>
    /// <param name="appendBreak">Value indicating whether a <c>break;</c> statement should be appended to <paramref name="statements"/>. The value is <c>true</c>.</param>
    public void AddCase( SwitchStatementLabel label, IExpression? when, IStatementList statements, bool appendBreak = true )
    {
        this._cases.Add( new SwitchStatementSection( label, when, statements, appendBreak ) );
    }

    /// <summary>
    /// Add a <c>case</c> switch section with a <c>when</c> expression.  This overload accepts an <see cref="IStatement"/>.
    /// </summary>
    /// <param name="label">The label, i.e. the value to match.</param>
    /// <param name="when">The <c>when</c> expression.</param>
    /// <param name="statement">The statement to execute. To call a template, see <see cref="StatementFactory.FromTemplate(Metalama.Framework.Aspects.TemplateInvocation,object?)"/>.</param>
    /// <param name="appendBreak">Value indicating whether a <c>break;</c> statement should be appended to <paramref name="statement"/>. The value is <c>true</c>.</param>
    public void AddCase( SwitchStatementLabel label, IExpression? when, IStatement statement, bool appendBreak = true )
    {
        this._cases.Add( new SwitchStatementSection( label, when, statement.AsList(), appendBreak ) );
    }

    /// <summary>
    /// Add a <c>default</c> switch section.  This overload accepts an <see cref="IStatementList"/>.
    /// </summary>
    /// <param name="statements">The statements to execute.</param>
    /// <param name="appendBreak">Value indicating whether a <c>break;</c> statement should be appended to <paramref name="statements"/>. The default value is <c>true</c>.</param>
    public void AddDefault( IStatementList statements, bool appendBreak = true )
    {
        this._cases.Add( new SwitchStatementSection( null, null, statements, appendBreak ) );
    }

    /// <summary>
    /// Add a <c>default</c> switch section.  This overload accepts an <see cref="IStatement"/>.
    /// </summary>
    /// <param name="statement">The statements to execute.  To call a template, see <see cref="StatementFactory.FromTemplate(Metalama.Framework.Aspects.TemplateInvocation,object?)"/>.</param>
    /// <param name="appendBreak">Value indicating whether a <c>break;</c> statement should be appended to <paramref name="statement"/>. The default value is <c>true</c>.</param>
    public void AddDefault( IStatement statement, bool appendBreak = true )
    {
        this._cases.Add( new SwitchStatementSection( null, null, statement.AsList(), appendBreak ) );
    }

    /// <summary>
    /// Gets the number of sections in the <c>switch</c>.
    /// </summary>
    public int SectionCount => this._cases.Count;

    /// <summary>
    /// Builds an <see cref="IStatement"/> from the current object.
    /// </summary>
    public IStatement ToStatement()
    {
        if ( this._cases.Count == 0 )
        {
            throw new InvalidOperationException( "The switch does not have any sections." );
        }

        return SyntaxBuilder.CurrentImplementation.CreateSwitchStatement( this._expression, this._cases.ToImmutableArray() );
    }
}