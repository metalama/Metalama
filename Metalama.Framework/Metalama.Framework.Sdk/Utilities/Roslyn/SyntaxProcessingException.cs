// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
                    // Get the node text. We need to remove CR and LF otherwise it is not well parsed by MSBuild.
                    var nodeText = this.SyntaxNode.NormalizeWhitespace().ToString().Replace( "\r\n", " " ).Replace( "\n", " " ).Replace( "\n", " " );

                    if ( nodeText.Length > 40 )
                    {
                        nodeText = nodeText.Substring( 0, 37 ) + "...";
                    }

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
                        $"{this.InnerException!.GetType().Name} while processing the {this.SyntaxNode.Kind()} with code `{nodeText}` at '{nodePath}' in '{location.SourceTree?.FilePath}' ({FormatLinePosition( location.GetMappedLineSpan().StartLinePosition )}-{FormatLinePosition( location.GetMappedLineSpan().EndLinePosition )}): {this.InnerException.Message}";
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

    private static string FormatLinePosition( in LinePosition position ) => $"{position.Line + 1},{position.Character + 1}";
}