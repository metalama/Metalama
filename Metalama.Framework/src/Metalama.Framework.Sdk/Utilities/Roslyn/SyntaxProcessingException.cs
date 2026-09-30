// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// An <see cref="Exception"/> bound to a specific syntax <see cref="Location"/>.
/// </summary>
internal sealed class SyntaxProcessingException : Exception
{
    internal SyntaxProcessingException( Exception innerException, SyntaxNode? node ) : base(
        "An exception occurred when processing a syntax tree.",
        innerException )
    {
        this.SyntaxNode = node;
    }

    public SyntaxNode? SyntaxNode { get; }

    public static bool ShouldWrapException( Exception exception, SyntaxNode? node )
        => exception is not (SyntaxProcessingException or OperationCanceledException or TaskCanceledException)
           && node?.GetLocation().SourceTree?.FilePath != null;

    // We render the message lazily to avoid a stack overflow. When the exception is thrown, the stack may be in high used. However, when the
    // exception is processed, the stack should be much lower.
    public override string Message
    {
        get
        {
            try
            {
                if ( this.SyntaxNode != null )
                {
                    // Get the node path.
                    var nodePath = "";

                    for ( var n = this.SyntaxNode; n != null; n = n.Parent )
                    {
                        if ( nodePath != "" )
                        {
                            nodePath = "/" + nodePath;
                        }

                        var identifier = n.GetType().GetProperty( "Identifier" )?.GetValue( n )?.ToString();

                        if ( identifier != null )
                        {
                            nodePath = $"{n.Kind()}[{identifier}]" + nodePath;
                        }
                        else
                        {
                            nodePath = $"{n.Kind()}" + nodePath;
                        }
                    }

                    var location = this.SyntaxNode.GetLocation();

                    return
                        $"{this.InnerException!.GetType().Name} while processing the {this.SyntaxNode.Kind()} with code `{GetNodeText( this.SyntaxNode )}` at '{nodePath}' in '{location.SourceTree?.FilePath}' {FormatLineSpan( location )}: {this.InnerException.Message}";
                }
                else
                {
                    // We should never get here because the caller should call ShouldWrapException and not create an exception of our type if the method returns false.  
                    return this.InnerException!.Message;
                }
            }
            catch
            {
                return "An exception occurred while attempting to generate a full error message.";
            }
        }
    }

    /// <summary>
    /// Returns the code of the given node, on a single line and truncated, or a description of the failure when the
    /// code cannot be rendered.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The text is built from the tokens of the node. Two tokens are separated by a single space when the source code has
    /// trivia between them. The text of a token can itself contain line breaks, for instance in a verbatim or raw string
    /// literal, so each sequence of line break characters is replaced by a single space. The text is therefore on a
    /// single line. A line break would prevent MSBuild from parsing the message correctly.
    /// </para>
    /// <para>
    /// The characters are appended one by one, and the method returns as soon as the text is longer than the maximum
    /// length, so that a large token is not copied.
    /// </para>
    /// <para>
    /// This method must not call <see cref="Microsoft.CodeAnalysis.SyntaxNodeExtensions.NormalizeWhitespace{TNode}(TNode, string, string, bool)"/>.
    /// The normalizer of Roslyn calls itself recursively for each ancestor of the node without checking the remaining
    /// stack, which causes a <see cref="StackOverflowException"/> when the node is deep in the syntax tree (issue #2083).
    /// <see cref="SyntaxNode.DescendantTokens(Func{SyntaxNode, bool}, bool)"/> is not recursive, and the method stops
    /// enumerating the tokens once it has enough text.
    /// </para>
    /// </remarks>
    private static string GetNodeText( SyntaxNode node )
    {
        const int maxLength = 40;

        try
        {
            var text = new StringBuilder();
            var previousHasTrailingTrivia = false;

            foreach ( var token in node.DescendantTokens() )
            {
                if ( text.Length > 0 && (previousHasTrailingTrivia || token.HasLeadingTrivia) )
                {
                    text.Append( ' ' );
                }

                foreach ( var c in token.Text )
                {
                    if ( c is '\r' or '\n' or '\u0085' or '\u2028' or '\u2029' )
                    {
                        // Replace a sequence of line break characters by a single space.
                        if ( text.Length > 0 && text[text.Length - 1] != ' ' )
                        {
                            text.Append( ' ' );
                        }
                    }
                    else
                    {
                        text.Append( c );
                    }

                    if ( text.Length > maxLength )
                    {
                        return text.ToString( 0, maxLength - 3 ) + "...";
                    }
                }

                previousHasTrailingTrivia = token.HasTrailingTrivia;
            }

            return text.ToString();
        }
        catch ( Exception e )
        {
            return $"<the code is not available: {e.Message}>";
        }
    }

    /// <summary>
    /// Returns the position of the given location in its file, or a description of the failure when the position
    /// cannot be computed.
    /// </summary>
    /// <remarks>
    /// Mapping a span to a line position throws when the line index of the text of the syntax tree disagrees with
    /// the content of that text, which is the state reported by issue #1858. The whole message used to be lost in
    /// that case, so the crash reports carried no information about the code that caused them.
    /// </remarks>
    private static string FormatLineSpan( Location location )
    {
        try
        {
            var lineSpan = location.GetMappedLineSpan();

            return $"({FormatLinePosition( lineSpan.StartLinePosition )}-{FormatLinePosition( lineSpan.EndLinePosition )})";
        }
        catch ( Exception e )
        {
            return $"(the position is not available: {e.Message})";
        }
    }

    private static string FormatLinePosition( in LinePosition position ) => $"{position.Line + 1},{position.Character + 1}";
}