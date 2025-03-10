// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.GenericContexts;

internal partial class SymbolGenericContext
{
    private sealed class SymbolMapper : SymbolVisitor<ISymbol>
    {
        private readonly TypeSymbolMapper _typeSymbolMapper;

        public SymbolMapper( SymbolGenericContext parent )
        {
            this._typeSymbolMapper = parent.TypeSymbolMapperInstance;
        }

        private T MapMember<T>( T symbol )
            where T : ISymbol
        {
            var containingType = this._typeSymbolMapper.Visit( symbol.ContainingType );
            var members = containingType.GetMembers( symbol.Name );

            var memberInTypeInstance = members.Single( s => symbol.OriginalDefinition.Equals( s.OriginalDefinition ) );

            if ( memberInTypeInstance.Kind == SymbolKind.Method )
            {
                var contextMethodSymbol = this._typeSymbolMapper.GenericContext._methodSymbol;

                if ( contextMethodSymbol != null )
                {
                    // We also need to create a method instance.
                    if ( memberInTypeInstance.Equals( contextMethodSymbol ) )
                    {
                        return (T) contextMethodSymbol;
                    }
                    else
                    {
                        return (T) ((IMethodSymbol) memberInTypeInstance).Construct( contextMethodSymbol.TypeArguments.ToArray() );
                    }
                }
            }

            return (T) memberInTypeInstance;
        }

        public override ISymbol DefaultVisit( ISymbol symbol ) => throw new NotSupportedException();

        public override ISymbol VisitArrayType( IArrayTypeSymbol symbol ) => this._typeSymbolMapper.Visit( symbol );

        public override ISymbol VisitAssembly( IAssemblySymbol symbol ) => symbol;

        public override ISymbol VisitDynamicType( IDynamicTypeSymbol symbol ) => this._typeSymbolMapper.Visit( symbol );

        public override ISymbol VisitEvent( IEventSymbol symbol ) => this.MapMember( symbol );

        public override ISymbol VisitField( IFieldSymbol symbol ) => this.MapMember( symbol );

        public override ISymbol VisitMethod( IMethodSymbol symbol ) => this.MapMember( symbol );

        public override ISymbol VisitModule( IModuleSymbol symbol ) => symbol;

        public override ISymbol VisitNamedType( INamedTypeSymbol symbol ) => this._typeSymbolMapper.Visit( symbol );

        public override ISymbol VisitNamespace( INamespaceSymbol symbol ) => symbol;

        public override ISymbol VisitParameter( IParameterSymbol symbol )
        {
            var parent = this.Visit( symbol.ContainingSymbol );

            return parent switch
            {
                IMethodSymbol method => method.Parameters[symbol.Ordinal],
                IPropertySymbol property => property.Parameters[symbol.Ordinal],
                _ => throw new AssertionFailedException()
            };
        }

        public override ISymbol VisitPointerType( IPointerTypeSymbol symbol ) => this._typeSymbolMapper.Visit( symbol );

        public override ISymbol VisitFunctionPointerType( IFunctionPointerTypeSymbol symbol ) => this._typeSymbolMapper.Visit( symbol );

        public override ISymbol VisitProperty( IPropertySymbol symbol ) => this.MapMember( symbol );

        public override ISymbol VisitTypeParameter( ITypeParameterSymbol symbol ) => this._typeSymbolMapper.Visit( symbol );
    }
}