// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
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
    private static string GetNodeText( SyntaxNode node )
    {
        try
        {
            // We need to remove CR and LF otherwise the text is not well parsed by MSBuild.
            var nodeText = node.NormalizeWhitespace().ToString().Replace( "\r\n", " " ).Replace( "\n", " " );

            if ( nodeText.Length > 40 )
            {
                nodeText = nodeText.Substring( 0, 37 ) + "...";
            }

            return nodeText;
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