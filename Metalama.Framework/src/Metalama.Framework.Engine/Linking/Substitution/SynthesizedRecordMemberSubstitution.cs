// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

/// <summary>
/// Substitutes the declaration of a record by the body that the C# compiler would have synthesized for one of its
/// members. The compiler exposes those members as symbols only, so their primary declaration syntax is the record
/// declaration itself, and the body has to be generated from the symbol.
/// </summary>
internal sealed class SynthesizedRecordMemberSubstitution : SyntaxNodeSubstitution
{
    private readonly SyntaxNode _rootNode;
    private readonly IMethodSymbol _targetSymbol;
    private readonly bool _usingSimpleInlining;
    private readonly string? _returnVariableIdentifier;

    public SynthesizedRecordMemberSubstitution(
        CompilationContext compilationContext,
        SyntaxNode rootNode,
        IMethodSymbol targetSymbol,
        bool usingSimpleInlining,
        string? returnVariableIdentifier )
        : base( compilationContext )
    {
        this._rootNode = rootNode;
        this._targetSymbol = targetSymbol;
        this._usingSimpleInlining = usingSimpleInlining;
        this._returnVariableIdentifier = returnVariableIdentifier;
    }

    public override SyntaxNode ReplacedNode => this._rootNode;

    public override SyntaxNode Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        var (statements, result) = SynthesizedRecordMemberBodyGenerator.GenerateBody(
            this._targetSymbol,
            substitutionContext.RewritingDriver,
            substitutionContext.SyntaxGenerationContext );

        var allStatements = new List<StatementSyntax>( statements );

        if ( result != null )
        {
            if ( this._usingSimpleInlining )
            {
                allStatements.Add(
                    ReturnStatement(
                        SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ReturnKeyword ),
                        result,
                        Token( SyntaxKind.SemicolonToken ) ) );
            }
            else if ( this._returnVariableIdentifier != null )
            {
                allStatements.Add(
                    SyntaxFactoryEx.AssignmentStatement(
                        SyntaxFactoryEx.SafeIdentifierName( this._returnVariableIdentifier ),
                        result,
                        substitutionContext.SyntaxGenerationContext ) );
            }

            // When there is neither simple inlining nor a return variable, the value of the original implementation
            // is not used by the inlining context and the result expression is dropped.
        }

        return substitutionContext.SyntaxGenerationContext.SyntaxGenerator.FormattedBlock( allStatements )
            .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
    }
}
