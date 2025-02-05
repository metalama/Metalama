// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Metalama.Framework.Engine.Templating
{
    /// <summary>
    /// Caches the <see cref="SemanticModel"/> of a syntax tree annotations (<see cref="SyntaxAnnotation"/>)
    /// so that the <see cref="SemanticModel"/> does not need to re-evaluated everything the syntax tree
    /// has changes that don't affect symbols. It also caches the <see cref="Location"/> of nodes, so that diagnostics
    /// on transformed node can be reported to their original location. A syntax tree can be annotated using <see cref="TryAnnotateTemplate"/>
    /// and the symbols can then be retrieved using <see cref="GetSymbol"/>,
    /// <see cref="GetExpressionType"/>, <see cref="GetDeclaredSymbol"/> and <see cref="GetLocation"/>.
    /// </summary>
    internal sealed partial class SyntaxTreeAnnotationMap : ILocationAnnotationMapBuilder
    {
        private const string _locationAnnotationKind = "location";
        private const string _symbolAnnotationKind = "symbol";
        private const string _declaredSymbolAnnotationKind = "declared";

        internal static readonly ImmutableList<string> AnnotationKinds = ImmutableList.Create(
            _symbolAnnotationKind,
            _declaredSymbolAnnotationKind,
            TypeAnnotationMapper.ExpressionTypeSymbolAnnotationKind,
            _locationAnnotationKind );

        private readonly List<(SyntaxTree Tree, TextSpan Span)> _indexToLocationMap = new();
        private readonly Dictionary<ISymbol, SyntaxAnnotation> _declaredSymbolToAnnotationMap = new( SymbolEqualityComparer.Default );
        private readonly Dictionary<SyntaxAnnotation, ISymbol> _annotationToDeclaredSymbolMap = new();
        private readonly Dictionary<ISymbol, SyntaxAnnotation> _symbolToAnnotationMap = new( SymbolEqualityComparer.IncludeNullability );
        private readonly Dictionary<SyntaxAnnotation, ISymbol> _annotationToSymbolMap = new();
        private readonly Dictionary<ITypeSymbol, SyntaxAnnotation> _typeToAnnotationMap = new( SymbolEqualityComparer.IncludeNullability );
        private readonly Dictionary<SyntaxAnnotation, ITypeSymbol> _annotationToTypeMap = new();

        /// <summary>
        /// Annotates a syntax tree with annotations that can later be resolved using the get methods of this class.
        /// </summary>
        public bool TryAnnotateTemplate(
            SyntaxNode root,
            SemanticModel semanticModel,
            IDiagnosticAdder diagnostics,
            CancellationToken cancellationToken,
            out SyntaxNode annotatedRoot )
        {
            var rewriter = new AnnotatingRewriter( semanticModel, this, true, diagnostics, cancellationToken );

            annotatedRoot = rewriter.Visit( root )!;

            return rewriter.Success;
        }

        // Not sure what this is used for.

        private SyntaxNodeOrToken AddLocationAnnotation( SyntaxNodeOrToken originalNode, SyntaxNodeOrToken annotatedNode )
        {
#if DEBUG
            if ( annotatedNode.HasAnnotations( _locationAnnotationKind ) )
            {
                throw new AssertionFailedException( "The node has already been annotated." );
            }
#endif

            if ( originalNode.SyntaxTree != null )
            {
                var index = this._indexToLocationMap.Count;

                this._indexToLocationMap.Add( (originalNode.SyntaxTree, originalNode.Span) );

                annotatedNode = annotatedNode.WithAdditionalAnnotations(
                    new SyntaxAnnotation( _locationAnnotationKind, index.ToString( CultureInfo.InvariantCulture ) ) );
            }

            return annotatedNode;
        }

        public SyntaxToken AddLocationAnnotation( SyntaxToken originalToken )
        {
            switch ( originalToken.Kind() )
            {
                case SyntaxKind.IdentifierToken:
                case SyntaxKind.DelegateKeyword:
                case SyntaxKind.AwaitKeyword:
                case SyntaxKind.GotoKeyword:
                case SyntaxKind.FromKeyword:
                case SyntaxKind.YieldKeyword:
                case SyntaxKind.DoKeyword:
                case SyntaxKind.EqualsGreaterThanToken:
                    return this.AddLocationAnnotation( originalToken, originalToken ).AsToken();

                default:
                    // Don't annotate punctuation because this is expensive and not useful.
                    return originalToken;
            }
        }

        public SyntaxNode AddLocationAnnotation( SyntaxNode originalNode, SyntaxNode transformedNode )
            => this.AddLocationAnnotation( (SyntaxNodeOrToken) originalNode, transformedNode ).AsNode()!;

        public Location? GetLocation( SyntaxNodeOrToken node )
        {
            // Strangely there could be sometimes be several annotations, probably due to operations on the syntax tree.
            // In this case it is better to continue than to fail.
            var annotation = node.GetAnnotations( _locationAnnotationKind ).FirstOrDefault();

            if ( annotation == null )
            {
                if ( node.Parent is { } parent )
                {
                    return this.GetLocation( parent );
                }
                else
                {
                    return null;
                }
            }
            else
            {
                var tuple = this._indexToLocationMap[int.Parse( annotation.Data!, CultureInfo.InvariantCulture )];

                return Location.Create( tuple.Tree, tuple.Span );
            }
        }

        /// <summary>
        /// Returns the result of <c>Microsoft.CodeAnalysis.SemanticModel.GetSymbolInfo</c>.
        /// </summary>
        public ISymbol? GetSymbol( SyntaxNode node )
        {
            using var enumerator = node.GetAnnotations( _symbolAnnotationKind ).GetEnumerator();

            if ( !enumerator.MoveNext() )
            {
                return null;
            }

            var annotation = enumerator.Current;

            if ( enumerator.MoveNext() )
            {
                // There is some ambiguity.
                return null;
            }

            return this._annotationToSymbolMap[annotation];
        }

        public ISymbol? GetInvocableSymbol( ExpressionSyntax node )
        {
            using var enumerator = node.GetAnnotations( _symbolAnnotationKind ).GetEnumerator();

            if ( !enumerator.MoveNext() )
            {
                return null;
            }

            var firstAnnotation = enumerator.Current;

            if ( !enumerator.MoveNext() )
            {
                // No ambiguity.
                return this._annotationToSymbolMap[firstAnnotation];
            }

            var firstSymbol = this._annotationToSymbolMap[firstAnnotation];

            // We have some ambiguity.
            // We never have ambiguities with anything else than methods, so let's end here if we don't have a method.
            if ( firstSymbol is not IMethodSymbol firstMethod )
            {
                return null;
            }

            // Get all symbols.
            var symbols = new List<IMethodSymbol> { firstMethod };

            do
            {
                symbols.Add( (IMethodSymbol) this._annotationToSymbolMap[enumerator.Current] );
            }
            while ( enumerator.MoveNext() );

            // If we have an ambiguity, it is because one of the arguments is dynamic. 
            // Take only signatures that have a dynamic argument.

            var likelySymbols = symbols.Where( m => m.Parameters.Any( p => p.Type.TypeKind == TypeKind.Dynamic ) );

            using var likelyEnumerator = likelySymbols.GetEnumerator();

            if ( !likelyEnumerator.MoveNext() )
            {
                return null;
            }

            var bestSymbol = likelyEnumerator.Current;

            if ( likelyEnumerator.MoveNext() )
            {
                // There is still some ambiguity.
                return null;
            }

            return bestSymbol;
        }

        public IEnumerable<ISymbol> GetCandidateSymbols( SyntaxNode node )
        {
            foreach ( var annotation in node.GetAnnotations( _symbolAnnotationKind ) )
            {
                yield return this._annotationToSymbolMap[annotation];
            }
        }

        /// <summary>
        /// Returns the result of <see cref="ModelExtensions.GetDeclaredSymbol"/>.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public ISymbol? GetDeclaredSymbol( SyntaxNode node )
        {
            var annotation = node.GetAnnotations( _declaredSymbolAnnotationKind ).SingleOrDefault();

            if ( annotation is not null )
            {
                return this._annotationToDeclaredSymbolMap[annotation];
            }

            return null;
        }

        /// <summary>
        /// Returns the corresponding parameter symbol of the invoked member for an argument.
        /// This takes named arguments and <see langword="params" /> parameters into account.
        /// </summary>
        /// <example>
        /// For the method <c>Add(int a, int b)</c> and the invocation <c>Add(b: 0, a: 1)</c>,
        /// calling this method on the first argument (<c>b: 0</c>) returns the second parameter (<c>b</c>).
        /// </example>
        public IParameterSymbol? GetParameterSymbol( ArgumentSyntax argument )
        {
            if ( argument.Parent?.Parent == null )
            {
                return null;
            }

            var invocationSymbol = this.GetInvocableSymbol( argument.Parent.Parent.AssertCast<ExpressionSyntax>() );

            var parameters = invocationSymbol switch
            {
                IMethodSymbol methodSymbol => methodSymbol.Parameters,
                IPropertySymbol propertySymbol => propertySymbol.Parameters,
                _ => ImmutableArray<IParameterSymbol>.Empty
            };

            if ( parameters.Length == 0 )
            {
                return null;
            }

            if ( argument.NameColon != null )
            {
                return parameters.FirstOrDefault( p => p.Name == argument.NameColon.Name.Identifier.ValueText );
            }

            var index = argument.Parent.ChildNodes().Select( ( child, i ) => (Node: child, Index: i) ).FirstOrDefault( x => x.Node == argument );

            if ( index.Node == null )
            {
                return null;
            }

            if ( index.Index < parameters.Length )
            {
                return parameters[index.Index];
            }

            var lastParameter = parameters.Last();

            if ( lastParameter.IsParams )
            {
                return lastParameter;
            }

            return null;
        }

        /// <summary>
        /// Returns the result of <c>semanticModel.GetTypeInfo(node).Type</c>.
        /// </summary>
        public ITypeSymbol? GetExpressionType( ExpressionSyntax node )
        {
            if ( node.IsKind( SyntaxKind.ElementAccessExpression ) )
            {
                // The lookup is not reliable, Roslyn does not return the proper nullability.
                var collectionType = this.GetExpressionType( ((ElementAccessExpressionSyntax) node).Expression );

                if ( collectionType is IArrayTypeSymbol arrayType )
                {
                    return arrayType.ElementType;   
                }
            }
            
            var annotation = node.GetAnnotations( TypeAnnotationMapper.ExpressionTypeSymbolAnnotationKind ).SingleOrDefault();

            if ( annotation is not null )
            {
                return this._annotationToTypeMap[annotation];
            }

            return null;
        }
    }
}