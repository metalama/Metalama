// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.Diagnostics
{
    /// <summary>
    /// Helper methods to work with diagnostics.
    /// </summary>
    public static class DiagnosticLocationHelper
    {
        /// <summary>
        /// Gets the <see cref="Location"/> suitable to report a <see cref="Diagnostic"/> on
        /// a given <see cref="ISymbol"/> (typically the identifier).
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static Location? GetDiagnosticLocation( this ISymbol symbol ) => symbol.GetDiagnosticLocationImpl( 0 );

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private static Location? GetDiagnosticLocationImpl( this ISymbol symbol, int depth )
        {
            if ( depth > 8 )
            {
                throw new InvalidOperationException( $"Infinite recursion in getting the location for symbol '{symbol}'." );
            }

            var bestDeclaration = symbol.GetPrimarySyntaxReference();

            if ( bestDeclaration == null )
            {
                if ( symbol.ContainingSymbol != null && symbol.ContainingSymbol.Kind != SymbolKind.Namespace )
                {
                    // Implicit symbols do not have a syntax. In this case, we go to the parent declaration.
                    return symbol.ContainingSymbol?.GetDiagnosticLocationImpl( depth + 1 );
                }
                else
                {
                    // We don't walk lower than namespaces. This makes sense and works around a bug in configuration of linker fakes.
                    return null;
                }
            }
            else
            {
                return bestDeclaration.GetSyntax().GetDiagnosticLocation();
            }
        }

        public static Location? GetDiagnosticLocation( this SyntaxNode node )
        {
            switch ( node )
            {
                case null:
                    return null;

                case MethodDeclarationSyntax method:
                    return method.Identifier.GetLocation();

                case EventDeclarationSyntax @event:
                    return @event.Identifier.GetLocation();

                case PropertyDeclarationSyntax property:
                    return property.Identifier.GetLocation();

                case IndexerDeclarationSyntax indexer:
                    return indexer.ThisKeyword.GetLocation();

                case OperatorDeclarationSyntax @operator:
                    return @operator.OperatorKeyword.GetLocation();

                case ConversionOperatorDeclarationSyntax @operator:
                    return @operator.OperatorKeyword.GetLocation();

                case BaseTypeDeclarationSyntax type:
                    return type.Identifier.GetLocation();

                case ParameterSyntax parameter:
                    return parameter.Identifier.GetLocation();

                case AccessorDeclarationSyntax accessor:
                    return accessor.Keyword.GetLocation();

                case DestructorDeclarationSyntax destructor:
                    return destructor.Identifier.GetLocation();

                case ConstructorDeclarationSyntax constructor:
                    return constructor.Identifier.GetLocation();

                case TypeParameterSyntax typeParameter:
                    return typeParameter.Identifier.GetLocation();

                case VariableDeclaratorSyntax variable:
                    return variable.Identifier.GetLocation();

                case DelegateDeclarationSyntax @delegate:
                    return @delegate.Identifier.GetLocation();

                case NameEqualsSyntax nameEquals:
                    return nameEquals.Name.GetLocation();

                default:
                    return node.GetLocation();
            }
        }

        /// <summary>
        /// Gets the <see cref="Location"/> suitable to report a <see cref="Diagnostic"/> on
        /// a given <see cref="AttributeData"/>.
        /// </summary>
        internal static Location? GetDiagnosticLocation( this AttributeData attribute ) => attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();
    }
}