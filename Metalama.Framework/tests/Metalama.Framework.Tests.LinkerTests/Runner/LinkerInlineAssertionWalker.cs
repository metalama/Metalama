// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Xunit;

namespace Metalama.Framework.Tests.LinkerTests.Runner
{
    internal sealed class LinkerInlineAssertionWalker : SafeSyntaxWalker
    {
        private static readonly Regex _assertionRegex = new( "^[\t ]*//[\t ]*ASSERT:(?<syntax>.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase );

        protected override void VisitCore( SyntaxNode? node )
        {
            if ( node == null )
            {
                base.VisitCore( node );

                return;
            }

            var trivias = node.GetLeadingTrivia();

            foreach ( var trivia in trivias )
            {
                if ( !trivia.HasStructure && TryParseAssertion( trivia.ToString(), out var assertedSyntax ) )
                {
                    var parsedAssertedTree = CSharpSyntaxTree.ParseText( assertedSyntax, options: SupportedCSharpVersions.DefaultParseOptions );
                    var parsedObservedTree = CSharpSyntaxTree.ParseText( node.ToString(), options: SupportedCSharpVersions.DefaultParseOptions );

                    Assert.Equal(
                        parsedAssertedTree.GetRoot().NormalizeWhitespace().ToString(),
                        parsedObservedTree.GetRoot().NormalizeWhitespace().ToString() );

                    return;
                }
            }

            base.VisitCore( node );
        }

        private static bool TryParseAssertion( string trivia, [NotNullWhen( true )] out string? assertedSyntax )
        {
            var match = _assertionRegex.Match( trivia );

            if ( match.Success )
            {
                assertedSyntax = match.Groups["syntax"].Value;

                return true;
            }

            assertedSyntax = null;

            return false;
        }
    }
}