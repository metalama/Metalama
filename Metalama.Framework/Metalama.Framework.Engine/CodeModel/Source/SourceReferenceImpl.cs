// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Source;

public sealed class SourceReferenceImpl : ISourceReferenceImpl
{
    public static SourceReferenceImpl Instance { get; } = new();

    private SourceReferenceImpl() { }

    string ISourceReferenceImpl.GetKind( in SourceReference sourceReference )
        => sourceReference.NodeOrTokenInternal switch
        {
            SyntaxNode node => node.Kind().ToString(),
            SyntaxToken token => token.Kind().ToString(),
            _ => throw new AssertionFailedException( $"{sourceReference.NodeOrTokenInternal} is not supported" )
        };

    public SourceSpan GetSourceSpan( in SourceReference sourceReference )
    {
        var (syntaxTree, span) = sourceReference.NodeOrTokenInternal switch
        {
            SyntaxNode node => (node.SyntaxTree, node.Span),
            SyntaxToken token => (token.SyntaxTree.AssertNotNull(), token.Span),
            _ => throw new AssertionFailedException( $"{sourceReference.NodeOrTokenInternal} is not supported" )
        };

        var lineSpan = syntaxTree.GetLineSpan( span );

        return new SourceSpan(
            syntaxTree.FilePath,
            syntaxTree,
            span.Start,
            span.End,
            lineSpan.StartLinePosition.Line,
            lineSpan.StartLinePosition.Character,
            lineSpan.EndLinePosition.Line,
            lineSpan.EndLinePosition.Character,
            this );
    }

    public string GetText( in SourceSpan sourceSpan )
        => ((SyntaxTree) sourceSpan.SyntaxTree).GetText().GetSubText( TextSpan.FromBounds( sourceSpan.Start, sourceSpan.End ) ).ToString();

#pragma warning disable LAMA0830 // NormalizeWhitespace is expensive.
    public string GetText( in SourceReference sourceReference, bool normalized )
        => sourceReference.NodeOrTokenInternal switch
        {
            SyntaxNode node when normalized => node.NormalizeWhitespace().ToString(),
            SyntaxNode node when !normalized => node.ToFullString(),
            SyntaxToken token when normalized => token.NormalizeWhitespace().ToString(),
            SyntaxNode token when !normalized => token.ToFullString(),
            _ => throw new AssertionFailedException( $"{sourceReference.NodeOrTokenInternal} is not supported" )
        };
#pragma warning restore LAMA0830

    public bool IsImplementationPart( in SourceReference sourceReference )
    {
        switch ( sourceReference.NodeOrTokenInternal )
        {
            case MethodDeclarationSyntax { Body: null, ExpressionBody: null } method when
                method.Modifiers.Any( SyntaxKind.PartialKeyword ):
                return false;

#if ROSLYN_4_12_0_OR_GREATER
            case PropertyDeclarationSyntax { ExpressionBody: null, AccessorList.Accessors: var accessors } property when
                accessors.All( a => a is { Body: null, ExpressionBody: null } ) &&
                property.Modifiers.Any( SyntaxKind.PartialKeyword ):
                return false;
#endif

            default:
                return true;
        }
    }
}