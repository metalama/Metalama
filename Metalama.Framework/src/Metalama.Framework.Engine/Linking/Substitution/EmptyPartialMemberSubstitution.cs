// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

internal abstract class EmptyPartialMemberSubstitution : SyntaxNodeSubstitution
{
    private readonly bool _usingSimpleInlining;
    private readonly string? _returnVariableIdentifier;

    protected EmptyPartialMemberSubstitution( CompilationContext compilationContext, bool usingSimpleInlining, string? returnVariableIdentifier )
        : base( compilationContext )
    {
        this._usingSimpleInlining = usingSimpleInlining;
        this._returnVariableIdentifier = returnVariableIdentifier;
    }

    protected SyntaxNode Substitute( SubstitutionContext substitutionContext )
    {
        var syntaxGenerator = substitutionContext.SyntaxGenerationContext.SyntaxGenerator;

        if ( this._usingSimpleInlining )
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

        if ( this._returnVariableIdentifier != null )
        {
            return syntaxGenerator.FormattedBlock(
                    SyntaxFactoryEx.AssignmentStatement(
                        IdentifierName( this._returnVariableIdentifier ),
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