// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.CompileTimeContracts;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Templating.Statements;

internal sealed class SwitchStatement : IStatementImpl
{
    private readonly IExpression _expression;
    private readonly ImmutableArray<SwitchStatementSection> _sections;

    public SwitchStatement( IExpression expression, ImmutableArray<SwitchStatementSection> sections )
    {
        this._expression = expression;
        this._sections = sections;
    }

    public StatementSyntax GetSyntax( TemplateSyntaxFactoryImpl? templateSyntaxFactory )
    {
        if ( templateSyntaxFactory == null )
        {
            throw new InvalidOperationException( $"{nameof(SwitchStatementBuilder)} is not available in the current context." );
        }

        var sections = new List<SwitchSectionSyntax>();

        foreach ( var switchCase in this._sections )
        {
            SwitchLabelSyntax label;
            WhenClauseSyntax? when;

            if ( switchCase.When != null )
            {
                when = WhenClause(
                    ((IUserExpression) switchCase.When).ToTypedExpressionSyntax(
                        templateSyntaxFactory.SyntaxSerializationContext,
                        this._expression.Type.Compilation.Factory.GetSpecialType( SpecialType.Boolean ) ) );
            }
            else
            {
                when = null;
            }

            if ( switchCase.Label != null )
            {
                var tupleValues =
                    switchCase.Label.Values.SelectAsReadOnlyList( x => templateSyntaxFactory.SyntaxSerializationContext.SyntaxGenerator.TypedConstant( x ) );

                if ( tupleValues.Count == 1 )
                {
                    if ( when == null )
                    {
                        label = CaseSwitchLabel( tupleValues[0] );
                    }
                    else
                    {
                        label = CasePatternSwitchLabel(
                            ConstantPattern( tupleValues[0] ),
                            when,
                            Token( SyntaxKind.ColonToken ) );
                    }
                }
                else
                {
                    var list = SeparatedList( tupleValues.SelectAsReadOnlyCollection( x => Subpattern( ConstantPattern( x ) ) ) );

                    var recursivePattern = RecursivePattern()
                        .WithPositionalPatternClause( PositionalPatternClause( list ) );

                    label = CasePatternSwitchLabel( recursivePattern, Token( SyntaxKind.ColonToken ) ).WithWhenClause( when );
                }
            }
            else
            {
                label = DefaultSwitchLabel();
            }

            var statements = ((IStatementListImpl) switchCase.Statements).GetSyntaxes( templateSyntaxFactory ).ToMutableList();

            if ( switchCase.EndWithBreak )
            {
                statements.Add( BreakStatement() );
            }

            var section = SwitchSection( SingletonList( label ), List( statements ) );

            sections.Add( section );
        }

        var switchExpression = ((IUserExpression) this._expression).ToTypedExpressionSyntax( templateSyntaxFactory.SyntaxSerializationContext ).Syntax;

        if ( switchExpression is TupleExpressionSyntax tuple )
        {
            // Remove names
            switchExpression = tuple.WithArguments( SeparatedList( tuple.Arguments.SelectAsReadOnlyCollection( a => a.WithNameColon( null ) ) ) );
        }

        return SwitchStatement( switchExpression, List( sections ) );
    }
}