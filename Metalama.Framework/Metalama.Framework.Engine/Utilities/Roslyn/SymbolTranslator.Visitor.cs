// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Comparers;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

internal sealed partial class SymbolTranslator
{
    private sealed class Visitor : SymbolVisitor<ISymbol>
    {
        private readonly SymbolTranslator _parent;
        private readonly bool _allowMultipleCandidates;

        public Visitor( SymbolTranslator parent, bool allowMultipleCandidates )
        {
            this._parent = parent;
            this._allowMultipleCandidates = allowMultipleCandidates;
        }

        private T? Translate<T>( T symbol )
            where T : ISymbol
            => (T?) this._parent._cache.GetOrAdd( (symbol, this._allowMultipleCandidates), this.TranslateCore );

        private ISymbol? TranslateCore( (ISymbol Symbol, bool AllowMultipleCandidates) value ) => this.Visit( value.Symbol );

        public override ISymbol DefaultVisit( ISymbol symbol ) => throw new NotSupportedException( $"Cannot map a symbol of kind '{symbol.Kind}'." );

        public override ISymbol? VisitArrayType( IArrayTypeSymbol symbol )
        {
            var elementType = this.Translate( symbol.ElementType );

            if ( elementType == null )
            {
                return null;
            }

            return this._parent._targetCompilationContext.Compilation.CreateArrayTypeSymbol( elementType, symbol.Rank )
                .WithNullableAnnotation( symbol.NullableAnnotation );
        }

        public override ISymbol VisitDynamicType( IDynamicTypeSymbol symbol )
            => this._parent._targetCompilationContext.Compilation.DynamicType.WithNullableAnnotation( symbol.NullableAnnotation );

        private ISymbol? TranslateUniquelyNamedTypeMember( ISymbol symbol )
        {
            var namedType = this.Translate( symbol.ContainingType );

            if ( namedType == null )
            {
                return namedType;
            }

            return namedType.GetMembers( symbol.Name ).SingleOrDefault( m => m.Kind == symbol.Kind );
        }

        private ISymbol? TranslateNonUniquelyNamedTypeMember( ISymbol symbol )
        {
            var namedType = this.Translate( symbol.ContainingType );

            if ( namedType == null )
            {
                return null;
            }

            var candidates = namedType.GetMembers( symbol.Name )
                .Where( m => m.Kind == symbol.Kind && StructuralSymbolComparer.ContainingDeclarationOblivious.Equals( m, symbol ) )
                .ToReadOnlyList();

            switch ( candidates.Count )
            {
                case 0:
                    return null;

                case 1:
                    return candidates[0];

                default:
                    if ( this._allowMultipleCandidates )
                    {
                        return candidates[0];
                    }
                    else
                    {
                        throw new AssertionFailedException(
                            $"More than one symbol match '{symbol}': {string.Join( ", ", candidates.SelectAsReadOnlyList( x => $"'{x}'" ) )}." );
                    }
            }
        }

        public override ISymbol? VisitEvent( IEventSymbol symbol ) => this.TranslateUniquelyNamedTypeMember( symbol );

        public override ISymbol? VisitField( IFieldSymbol symbol ) => this.TranslateUniquelyNamedTypeMember( symbol );

        public override ISymbol? VisitMethod( IMethodSymbol symbol )
        {
            if ( symbol.ContainingSymbol is IMethodSymbol )
            {
                throw new NotSupportedException( "Translating a local function is not supported." );
            }

            if ( symbol.PartialImplementationPart != null )
            {
                var partialImplementation = this.Translate( symbol.PartialImplementationPart );

                return partialImplementation?.PartialDefinitionPart;
            }

            var translated = this.TranslateNonUniquelyNamedTypeMember( symbol );

            if ( translated == null )
            {
                return null;
            }

            if ( symbol.PartialDefinitionPart != null )
            {
                return ((IMethodSymbol) translated).PartialImplementationPart;
            }

            return translated;
        }

        public override ISymbol? VisitNamedType( INamedTypeSymbol symbol )
        {
            if ( symbol.IsGenericType && !symbol.IsGenericTypeDefinition() )
            {
                var constructedFrom = this.Translate( symbol.ConstructedFrom );

                if ( constructedFrom == null )
                {
                    return null;
                }

                var typeArguments = new ITypeSymbol[symbol.TypeArguments.Length];

                for ( var i = 0; i < typeArguments.Length; i++ )
                {
                    var typeArgument = this.Translate( symbol.TypeArguments[i] );

                    if ( typeArgument == null )
                    {
                        return null;
                    }

                    typeArguments[i] = typeArgument;
                }

                return constructedFrom.Construct( typeArguments ).WithNullableAnnotation( symbol.NullableAnnotation );
            }
            else
            {
                ImmutableArray<INamedTypeSymbol> types;

                if ( symbol.ContainingType != null )
                {
                    var containingType = this.Translate( symbol.ContainingType );

                    if ( containingType == null )
                    {
                        return null;
                    }

                    types = containingType.GetTypeMembers( symbol.Name, symbol.Arity );
                }
                else if ( symbol.ContainingNamespace != null )
                {
                    var ns = this.Translate( symbol.ContainingNamespace );

                    if ( ns == null )
                    {
                        return null;
                    }

                    types = ns.GetTypeMembers( symbol.Name, symbol.Arity );
                }
                else
                {
                    throw new AssertionFailedException( $"Unexpected containing declaration of type '{symbol}'." );
                }

                if ( types.IsDefaultOrEmpty )
                {
                    return null;
                }

                if ( types.Length > 1 )
                {
                    throw new AssertionFailedException( $"More than one type named '{symbol.Name}' in '{symbol.ContainingSymbol}'." );
                }

                return types[0].WithNullableAnnotation( symbol.NullableAnnotation );
            }
        }

        public override ISymbol? VisitNamespace( INamespaceSymbol symbol )
        {
            if ( symbol.IsGlobalNamespace )
            {
                switch ( symbol.NamespaceKind )
                {
                    case NamespaceKind.Assembly:
                        var assembly = this.Translate( symbol.ContainingAssembly );

                        if ( assembly == null )
                        {
                            return null;
                        }

                        return assembly.GlobalNamespace;

                    case NamespaceKind.Module:
                        var module = this.Translate( symbol.ContainingModule );

                        if ( module == null )
                        {
                            return null;
                        }

                        return module.GlobalNamespace;

                    case NamespaceKind.Compilation:
                        return this._parent._targetCompilationContext.Compilation.GlobalNamespace;

                    default:
                        throw new AssertionFailedException( $"Unexpected NamespaceKind {symbol.NamespaceKind} for '{symbol}'." );
                }
            }
            else
            {
                var ns = this.Translate( symbol.ContainingNamespace );

                if ( ns == null )
                {
                    return null;
                }

                return ns.GetNamespaceMembers().SingleOrDefault( x => x.Name == symbol.Name );
            }
        }

        public override ISymbol? VisitParameter( IParameterSymbol symbol )
        {
            var containingSymbol = this.Translate( symbol.ContainingSymbol );

            ImmutableArray<IParameterSymbol> parameters;

            switch ( containingSymbol )
            {
                case null:
                    return null;

                case IMethodSymbol method:
                    parameters = method.Parameters;

                    break;

                case IPropertySymbol property:
                    parameters = property.Parameters;

                    break;

                default:
                    throw new AssertionFailedException( $"Unexpected parent of parameter: {containingSymbol.Kind}." );
            }

            if ( parameters.Length <= symbol.Ordinal )
            {
                return null;
            }

            return parameters[symbol.Ordinal];
        }

        public override ISymbol? VisitPointerType( IPointerTypeSymbol symbol )
        {
            var pointedAtType = this.Translate( symbol.PointedAtType );

            if ( pointedAtType == null )
            {
                return null;
            }

            return this._parent._targetCompilationContext.Compilation.CreatePointerTypeSymbol( pointedAtType );
        }

        public override ISymbol? VisitProperty( IPropertySymbol symbol )
        {
#if ROSLYN_4_12_0_OR_GREATER
            if ( symbol.PartialImplementationPart != null )
            {
                var partialImplementation = this.Translate( symbol.PartialImplementationPart );

                return partialImplementation?.PartialDefinitionPart;
            }
#endif

            var translated = this.TranslateNonUniquelyNamedTypeMember( symbol );

#if ROSLYN_4_12_0_OR_GREATER
            if ( translated == null )
            {
                return null;
            }

            if ( symbol.PartialDefinitionPart != null )
            {
                return ((IPropertySymbol) translated).PartialImplementationPart;
            }
#endif

            return translated;
        }

        public override ISymbol? VisitTypeParameter( ITypeParameterSymbol symbol )
        {
            var containingSymbol = this.Translate( symbol.ContainingSymbol );

            ImmutableArray<ITypeParameterSymbol> parameters;

            switch ( containingSymbol )
            {
                case null:
                    return null;

                case IMethodSymbol method:
                    parameters = method.TypeParameters;

                    break;

                case INamedTypeSymbol namedType:
                    parameters = namedType.TypeParameters;

                    break;

                default:
                    throw new AssertionFailedException( $"Unexpected parent of type parameter: {containingSymbol.Kind}." );
            }

            if ( parameters.Length <= symbol.Ordinal )
            {
                return null;
            }

            return parameters[symbol.Ordinal].WithNullableAnnotation( symbol.NullableAnnotation );
        }

        public override ISymbol? VisitAssembly( IAssemblySymbol symbol )
        {
            // Can't use symbol.Identity here: the two assemblies can have different versions, if one of them is missing a file containing AssemblyVersion.
            return this._parent._targetCompilationContext.Compilation.GetAssembly( symbol.Name );
        }

        public override ISymbol? VisitModule( IModuleSymbol symbol )
        {
            var assembly = this.Translate( symbol.ContainingAssembly );

            if ( assembly == null )
            {
                return null;
            }

            return assembly.Modules.SingleOrDefault( m => m.Name == symbol.Name );
        }
    }
}