// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

internal static class FlowAnalyzer
{
    private static bool HasDefaultSection( this SwitchStatementSyntax switchStatement )
    {
        foreach ( var section in switchStatement.Sections )
        {
            foreach ( var label in section.Labels )
            {
                if ( label.Keyword.IsKind( SyntaxKind.DefaultKeyword ) )
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool NeverContinues( this SyntaxList<StatementSyntax> statements )
    {
        foreach ( var statementSyntax in statements )
        {
            if ( statementSyntax.NeverContinues() )
            {
                return true;
            }
        }

        return false;
    }

    public static bool NeverContinues( this StatementSyntax statement )
    {
        switch ( statement.Kind() )
        {
            case SyntaxKind.ReturnStatement:
                return true;

            case SyntaxKind.ThrowStatement:
                return true;

            case SyntaxKind.Block:
                {
                    var block = (BlockSyntax) statement;

                    return block.Statements.NeverContinues();
                }

            case SyntaxKind.IfStatement:
                {
                    var ifStatement = (IfStatementSyntax) statement;

                    return ifStatement.Else != null && ifStatement.Else.Statement.NeverContinues() && ifStatement.Statement.NeverContinues();
                }

            case SyntaxKind.SwitchStatement:
                {
                    var switchStatement = (SwitchStatementSyntax) statement;

                    if ( !switchStatement.HasDefaultSection() )
                    {
                        return false;
                    }

                    foreach ( var section in switchStatement.Sections )
                    {
                        if ( !section.Statements.NeverContinues() )
                        {
                            return false;
                        }
                    }

                    return true;
                }

            default:
                return false;
        }
    }
}