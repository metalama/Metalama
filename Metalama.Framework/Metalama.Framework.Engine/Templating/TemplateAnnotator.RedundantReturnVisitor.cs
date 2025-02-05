// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Templating;

internal partial class TemplateAnnotator
{
    /// <summary>
    /// Reports an error for <c>return;</c> statements that are unnecessary, as long as the end of the method is unreachable.
    /// Also, no errors are produced if there is any unreachable code after an unnecessary <c>return;</c>.
    /// </summary>
    /// <remarks>
    /// Consider this code:
    /// <code>
    /// void M1()
    /// {
    ///     if (condition)
    ///         return;
    ///     else
    ///         return;
    /// }
    /// 
    /// void M2()
    /// {
    ///     if (condition)
    ///         return;
    /// }
    /// 
    /// void M3()
    /// {
    ///     return;
    ///     Console.WriteLine();
    /// }
    /// </code>
    /// 
    /// Method <c>M1</c> will report errors.
    /// Method <c>M2</c> will not report an error, even though the <c>return;</c> is unnecessary, because code like that can be useful in a subtemplate.
    /// Method <c>M3</c> will not report an error. Methods like that are not likely in real code and they would make the analysis more complicated.
    /// </remarks>
    private sealed class RedundantReturnVisitor : SafeSyntaxVisitor
    {
        private List<ReturnStatementSyntax>? _returnStatements = new();

        public static void ReportErrors( TemplateAnnotator templateAnnotator, StatementSyntax statement )
        {
            var visitor = new RedundantReturnVisitor();
            visitor.Visit( statement );

            if ( visitor._returnStatements != null )
            {
                foreach ( var returnStatement in visitor._returnStatements )
                {
                    templateAnnotator.ReportDiagnostic( TemplatingDiagnosticDescriptors.RedundantReturnNotAllowed, returnStatement, default );
                }
            }
        }

        public override void DefaultVisit( SyntaxNode node )
        {
            // We have found a last statement in a block that does not end execution of the method,
            // so no errors should be reported.
            this._returnStatements = null;
        }

        public override void VisitReturnStatement( ReturnStatementSyntax node )
        {
            if ( node.Expression != null )
            {
                // This should only happen for code that doesn't compile.
                return;
            }

            this._returnStatements?.Add( node );
        }

        public override void VisitThrowStatement( ThrowStatementSyntax node )
        {
            // Throw statement ends execution of the method, so nothing changes regarding reporting of errors.
        }

        private void VisitStatements( SyntaxList<StatementSyntax> statements )
        {
            if ( statements is [.., var lastStatement] )
            {
                this.Visit( lastStatement );
            }
            else
            {
                // Empty block means the end is reachable.
                this._returnStatements = null;
            }
        }

        public override void VisitBlock( BlockSyntax node )
        {
            this.VisitStatements( node.Statements );
        }

        public override void VisitIfStatement( IfStatementSyntax node )
        {
            if ( node.Else == null )
            {
                // No else means the end is reachable.
                this._returnStatements = null;
            }
            else
            {
                this.Visit( node.Statement );
                this.Visit( node.Else.Statement );
            }
        }

        public override void VisitSwitchStatement( SwitchStatementSyntax node )
        {
            var hasDefaultLabel = node.Sections.Any( section => section.Labels.OfType<DefaultSwitchLabelSyntax>().Any() );

            if ( !hasDefaultLabel )
            {
                // Assume that no default means that the switch is not exhaustive, which means the end is reachable.
                this._returnStatements = null;
            }
            else
            {
                foreach ( var section in node.Sections )
                {
                    this.VisitStatements( section.Statements );
                }
            }
        }

        public override void VisitTryStatement( TryStatementSyntax node )
        {
            this.Visit( node.Block );

            foreach ( var catchClause in node.Catches )
            {
                this.Visit( catchClause.Block );
            }
        }
    }
}