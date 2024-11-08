// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using RoslynTypeKind = Microsoft.CodeAnalysis.TypeKind;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel;

public partial class DerivedTypeIndex
{
    internal sealed class Builder
    {
        private readonly ImmutableDictionaryOfArray<NamedTypeRef, NamedTypeRef>.Builder _relationships;
        private readonly ImmutableHashSet<NamedTypeRef>.Builder _processedTypes;
        private readonly CompilationContext _compilationContext;

        internal Builder( CompilationContext compilationContext )
        {
            this._compilationContext = compilationContext;
            this._relationships = new ImmutableDictionaryOfArray<NamedTypeRef, NamedTypeRef>.Builder();
            this._processedTypes = ImmutableHashSet.CreateBuilder<NamedTypeRef>();
        }

        internal Builder( DerivedTypeIndex immutable )
        {
            this._compilationContext = immutable._compilationContext;
            this._relationships = immutable._relationships.ToBuilder();
            this._processedTypes = immutable._processedTypes.ToBuilder();
        }

        public void AnalyzeType( IRef<INamedType> type )
        {
            switch ( type )
            {
                case ISymbolRef { Symbol: INamedTypeSymbol symbol }:
                    this.AnalyzeType( symbol );

                    break;

                case IIntroducedRef { BuilderData: NamedTypeBuilderData builder }:
                    this.AnalyzeType( builder );

                    break;

                default:
                    throw new AssertionFailedException( $"Unsupported target: {type}" );
            }
        }

        public void AnalyzeType( INamedTypeSymbol type )
        {
            if ( !this._processedTypes.Add( new NamedTypeRef( type ) ) )
            {
                return;
            }

            if ( type.BaseType != null && type.BaseType.Kind != SymbolKind.ErrorType )
            {
                var baseType = type.BaseType.OriginalDefinition;

                this._relationships.Add(
                    new NamedTypeRef( baseType ),
                    new NamedTypeRef( type ) );

                this.AnalyzeType( baseType );
            }

            foreach ( var interfaceImpl in type.Interfaces )
            {
                if ( interfaceImpl.TypeKind == RoslynTypeKind.Error )
                {
                    continue;
                }

                var interfaceType = interfaceImpl.OriginalDefinition;

                this._relationships.Add(
                    new NamedTypeRef( interfaceType ),
                    new NamedTypeRef( type ) );

                this.AnalyzeType( interfaceType );
            }

            foreach ( var nestedType in type.GetTypeMembers() )
            {
                this.AnalyzeType( nestedType );
            }
        }

        private void AnalyzeType( NamedTypeBuilderData type )
        {
            if ( !this._processedTypes.Add( new NamedTypeRef( type ) ) )
            {
                return;
            }

            if ( type.BaseType is { Definition.TypeKind: not TypeKind.Error } )
            {
                var baseType = type.BaseType.DefinitionRef;
                this._relationships.Add( new NamedTypeRef( baseType ), new NamedTypeRef( type ) );
                this.AnalyzeType( baseType );
            }

            foreach ( var interfaceImpl in type.ImplementedInterfaces )
            {
                if ( interfaceImpl is { Definition.TypeKind: TypeKind.Error } )
                {
                    continue;
                }

                var interfaceType = interfaceImpl.DefinitionRef;
                this._relationships.Add( new NamedTypeRef( interfaceType ), new NamedTypeRef( type ) );
                this.AnalyzeType( interfaceType );
            }
        }

        public void AddDerivedType( INamedTypeSymbol baseType, INamedTypeSymbol derivedType )
            => this._relationships.Add(
                new NamedTypeRef( baseType ),
                new NamedTypeRef( derivedType ) );

        public void AddDerivedType( IFullRef<INamedType> baseType, IFullRef<INamedType> derivedType )
            => this._relationships.Add( new NamedTypeRef( baseType ), new NamedTypeRef( derivedType ) );

        public DerivedTypeIndex ToImmutable()
        {
            return new DerivedTypeIndex(
                this._relationships.ToImmutable(),
                this._processedTypes.ToImmutable(),
                this._compilationContext );
        }
    }
}