// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Metalama.Framework.Engine.Formatting
{
    internal sealed class XmlDocumentationReader
    {
        private static readonly Regex _cleanupRegex = new( "`[0-9]+" );
        private readonly Dictionary<string, XElement> _members;
        private static readonly Regex _spaceRegex = new( "\\s+", RegexOptions.Compiled );
        private static XmlDocumentationReader? _instance;

        public static XmlDocumentationReader Instance => _instance ??= new XmlDocumentationReader();

        private XmlDocumentationReader()
        {
            var documentationPath = Path.ChangeExtension( typeof(IAspect).Assembly.Location, ".xml" );

            if ( File.Exists( documentationPath ) )
            {
                var documentationDocument = XDocument.Load( documentationPath );

                this._members = documentationDocument.Root
                                    ?.Element( "members" )
                                    ?.Elements( "member" )
                                    .Select( m => (Member: m, Name: m.Attribute( "name" )?.Value) )
                                    .Where( x => x.Name != null )
                                    .ToDictionary( x => x.Name!, x => x.Member )
                                ?? new Dictionary<string, XElement>();
            }
            else
            {
                // Coverage: ignore.
                this._members = new Dictionary<string, XElement>();
            }
        }

        public string? GetFormattedDocumentation( ISymbol symbol, CompilationContext compilationContext, string prefix = "" )
        {
            var compilation = compilationContext.Compilation;

            switch ( symbol )
            {
                case IMethodSymbol { IsOverride: true, OverriddenMethod: { } overriddenMethod }:
                    return this.GetFormattedDocumentation( overriddenMethod, compilationContext, "Overrides the " );

                case IMethodSymbol { ExplicitInterfaceImplementations.Length: > 0 } methodSymbol:
                    // TODO: Implicit implementations are not trivial.
                    return this.GetFormattedDocumentation( methodSymbol.ExplicitInterfaceImplementations.First(), compilationContext, "Implements the " );

                case IMethodSymbol methodSymbol:
                    symbol = methodSymbol.OriginalDefinition;

                    break;

                case INamedTypeSymbol namedTypeSymbol:
                    symbol = namedTypeSymbol.ConstructedFrom;

                    break;
            }

            var documentationId = symbol.GetDocumentationCommentId();

            if ( documentationId == null || !this._members.TryGetValue( documentationId, out var documentation ) )
            {
                // If the constructor is not documented, return the documentation of the type.
                if ( symbol is IMethodSymbol { MethodKind: MethodKind.Constructor } methodSymbol )
                {
                    return this.GetFormattedDocumentation( methodSymbol.ContainingType, compilationContext );
                }

                return null;
            }

            var summary = documentation.Element( "summary" );

            if ( summary == null )
            {
                return null;
            }

            StringBuilder stringBuilder = new();

            foreach ( var node in summary.Nodes() )
            {
                switch ( node )
                {
                    case XText text:
                        stringBuilder.Append( text.Value );

                        break;

                    case XElement see when see.Name == "see":

                        var cref = see.Attribute( "cref" )?.Value;

                        if ( cref == null )
                        {
                            continue;
                        }

                        var referencedSymbol = DocumentationCommentId.GetFirstSymbolForReferenceId( cref, compilation );

                        stringBuilder.Append( '\'' );

                        if ( referencedSymbol != null )
                        {
                            stringBuilder.Append( referencedSymbol.ToDisplayString( SymbolDisplayFormat.CSharpShortErrorMessageFormat ) );
                        }
                        else
                        {
                            // Somehow DocumentationCommentId.GetFirstSymbolForReferenceId does only work for types.
                            var indexOfParenthesis = cref.IndexOfOrdinal( '(' );
                            var crefWithoutParameters = indexOfParenthesis > 0 ? cref.Substring( 0, indexOfParenthesis ) : cref;
                            var parts = crefWithoutParameters.Split( '.', ':' );

                            switch ( parts[0] )
                            {
                                case "T":
                                    // Coverage: ignore (we should not get here because the resolution works well for types).
                                    stringBuilder.Append( CleanUpName( parts[^1] ) );

                                    break;

                                default:
                                    stringBuilder.Append( CleanUpName( parts[^2] ) );
                                    stringBuilder.Append( '.' );
                                    stringBuilder.Append( CleanUpName( parts[^1] ) );

                                    break;
                            }
                        }

                        stringBuilder.Append( '\'' );

                        break;

                    // ReSharper disable once StringLiteralTypo
                    case XElement paramRef when paramRef.Name == "paramref" && paramRef.Attribute( "name" )?.Value is { } paramName:

                        stringBuilder.Append( '\'' );
                        stringBuilder.Append( paramName );
                        stringBuilder.Append( '\'' );

                        break;
                }
            }

            var normalized = _spaceRegex.Replace( stringBuilder.ToString(), " " ).Trim();

            var declarationKind = symbol switch
            {
                INamedTypeSymbol namedType => namedType.TypeKind switch
                {
                    TypeKind.FunctionPointer => "function pointer",
                    TypeKind.TypeParameter => "type parameter",
                    _ => namedType.TypeKind.ToString().ToLowerInvariant()
                },
                _ => symbol.GetDeclarationKind( compilationContext ).ToDisplayString()
            };

            var prolog = $"{symbol.ToDisplayString( SymbolDisplayFormat.CSharpShortErrorMessageFormat )} {declarationKind}\n\n";

            return prefix + prolog + normalized;
        }

        private static string CleanUpName( string name ) => _cleanupRegex.Replace( name, "" );
    }
}