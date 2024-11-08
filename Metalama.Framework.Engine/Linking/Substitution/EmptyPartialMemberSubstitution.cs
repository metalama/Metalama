// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

internal abstract class EmptyPartialMemberSubstitution( CompilationContext compilationContext, bool usingSimpleInlining, string? returnVariableIdentifier )
    : SyntaxNodeSubstitution( compilationContext )
{
    protected SyntaxNode Substitute( SubstitutionContext substitutionContext )
    {
        var syntaxGenerator = substitutionContext.SyntaxGenerationContext.SyntaxGenerator;

        if ( usingSimpleInlining )
        {
            // Uses the simple inlining, i.e. generating simple return statement without any changes for non-void methods.
            if ( this.IsVoid )
            {
                return syntaxGenerator.FormattedBlock()
                    .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
            }
            else
            {
                return syntaxGenerator.FormattedBlock(
                        ReturnStatement(
                            SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ReturnKeyword ),
                            SyntaxFactoryEx.Default,
                            Token( SyntaxKind.SemicolonToken ).WithOptionalTrailingLineFeed( substitutionContext.SyntaxGenerationContext ) ) )
                    .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
            }
        }

        if ( returnVariableIdentifier != null )
        {
            return syntaxGenerator.FormattedBlock(
                    SyntaxFactoryEx.AssignmentStatement(
                        IdentifierName( returnVariableIdentifier ),
                        SyntaxFactoryEx.Default,
                        substitutionContext.SyntaxGenerationContext ) )
                .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
        }
        else
        {
            return syntaxGenerator.FormattedBlock()
                .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
        }
    }

    protected abstract bool IsVoid { get; }
}