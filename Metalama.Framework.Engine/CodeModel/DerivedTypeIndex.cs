// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel;

public sealed partial class DerivedTypeIndex
{
    // Maps a base type to direct derived types.
    private readonly ImmutableDictionaryOfArray<NamedTypeRef, NamedTypeRef> _relationships;
    private readonly ImmutableHashSet<NamedTypeRef> _processedTypes;
    private readonly CompilationContext _compilationContext;

    private DerivedTypeIndex(
        ImmutableDictionaryOfArray<NamedTypeRef, NamedTypeRef> relationships,
        ImmutableHashSet<NamedTypeRef> processedTypes,
        CompilationContext compilationContext )
    {
        this._relationships = relationships;
        this._processedTypes = processedTypes;
        this._compilationContext = compilationContext;
    }

    private bool IsContainedInCurrentCompilation( NamedTypeRef namedTypeRef )
    {
        switch ( namedTypeRef.Value )
        {
            case INamedTypeSymbol symbol:
                return this.IsCurrentCompilation( symbol.ContainingAssembly );

            case NamedTypeBuilderData:
                // Builders are always "in" the current Roslyn compilation.
                return true;

            default:
                throw new InvalidOperationException( $"Unexpected type {namedTypeRef.Value}." );
        }
    }

    private bool IsContainedInCurrentCompilation( IFullRef<INamedType> type )
    {
        switch ( type )
        {
            case ISymbolRef { Symbol: INamedTypeSymbol symbol }:
                return this.IsCurrentCompilation( symbol.ContainingAssembly );

            case IIntroducedRef { BuilderData: NamedTypeBuilderData }:
                // Builders are always "in" the current Roslyn compilation.
                return true;

            default:
                throw new InvalidOperationException( $"Unexpected type {type}." );
        }
    }

    private bool IsCurrentCompilation( IAssemblySymbol assembly )
        => this._compilationContext.SymbolComparer.Equals( this._compilationContext.Compilation.Assembly, assembly );

    internal IEnumerable<INamedType> GetDerivedTypesInCurrentCompilation( INamedType baseType, DerivedTypesOptions options )
        => options switch
        {
            DerivedTypesOptions.All => this.GetAllDerivedTypes( baseType ),
            DerivedTypesOptions.DirectOnly => this.GetDirectlyDerivedTypes( baseType ),
            DerivedTypesOptions.FirstLevelWithinCompilationOnly => this.GetFirstLevelDerivedTypes( baseType ),
            DerivedTypesOptions.IncludingExternalTypesDangerous => this.GetDerivedTypes( baseType ),
            _ => throw new ArgumentOutOfRangeException( nameof(options), $"Unexpected value '{options}'." )
        };

    internal IEnumerable<IFullRef<INamedType>> GetDerivedTypesInCurrentCompilation( IFullRef<INamedType> baseType, DerivedTypesOptions options )
        => options switch
        {
            DerivedTypesOptions.All => this.GetAllDerivedTypesCore( baseType ),
            DerivedTypesOptions.DirectOnly => this.GetDirectlyDerivedTypesCore( baseType ),
            DerivedTypesOptions.FirstLevelWithinCompilationOnly => this.GetFirstLevelDerivedTypesCore( baseType ),
            DerivedTypesOptions.IncludingExternalTypesDangerous => this.GetDerivedTypesCore( baseType ),
            _ => throw new ArgumentOutOfRangeException( nameof(options), $"Unexpected value '{options}'." )
        };

    internal IReadOnlyCollection<INamedType> GetDerivedTypes( INamedType baseType )
        => this.GetDerivedTypesCore( baseType.ToFullRef() )
            .SelectAsReadOnlyCollection( nt => nt.GetTarget( baseType.Compilation ) );

    private IReadOnlyCollection<IFullRef<INamedType>> GetDerivedTypesCore( IFullRef<INamedType> baseType )
        => this.GetRelationships( baseType )
            .SelectManyRecursiveDistinct( t => this._relationships[t], this._processedTypes.KeyComparer )
            .SelectAsReadOnlyCollection( t => t.ToRef( baseType.RefFactory ) );

    private ImmutableArray<NamedTypeRef> GetRelationships( IFullRef<INamedType> baseType ) => this._relationships[new NamedTypeRef( baseType )];

    private IEnumerable<INamedType> GetAllDerivedTypes( INamedType baseType )
        => this.GetAllDerivedTypesCore( baseType.ToFullRef() )
            .Select( nt => nt.GetTarget( baseType.Compilation ) );

    private IEnumerable<IFullRef<INamedType>> GetAllDerivedTypesCore( IFullRef<INamedType> baseType )
        => this.GetDerivedTypesCore( baseType )
            .Where( this.IsContainedInCurrentCompilation );

    private IEnumerable<INamedType> GetDirectlyDerivedTypes( INamedType baseType )
        => this.GetDirectlyDerivedTypesCore( baseType.ToFullRef() )
            .Select( nt => nt.GetTarget( baseType.Compilation ) );

    private IEnumerable<IFullRef<INamedType>> GetDirectlyDerivedTypesCore( IFullRef<INamedType> baseType )
    {
        foreach ( var namedType in this.GetRelationships( baseType ) )
        {
            if ( this.IsContainedInCurrentCompilation( namedType ) )
            {
                yield return namedType.ToRef( baseType.RefFactory );
            }
        }
    }

    private IEnumerable<INamedType> GetFirstLevelDerivedTypes( INamedType baseType )
        => this.GetFirstLevelDerivedTypesCore( baseType.ToFullRef() )
            .Select( nt => nt.GetTarget( baseType.Compilation ) );

    private IEnumerable<IFullRef<INamedType>> GetFirstLevelDerivedTypesCore( IFullRef<INamedType> baseType )
    {
        var set = new HashSet<IFullRef<INamedType>>( RefEqualityComparer<INamedType>.Default );
        GetDerivedTypesRecursive( new NamedTypeRef( baseType ) );

        return set;

        void GetDerivedTypesRecursive( NamedTypeRef parentType )
        {
            foreach ( var type in this._relationships[parentType] )
            {
                if ( this.IsContainedInCurrentCompilation( type ) )
                {
                    set.Add( type.ToRef( baseType.RefFactory ) );
                }
                else
                {
                    GetDerivedTypesRecursive( type );
                }
            }
        }
    }

    internal DerivedTypeIndex WithIntroducedInterfaces( IEnumerable<IIntroduceInterfaceTransformation> introducedInterfaces )
    {
        Builder? builder = null;

        foreach ( var transformation in introducedInterfaces )
        {
            builder ??= new Builder( this );

            var introducedInterface = transformation.InterfaceType;

            if ( !this._compilationContext.Compilation.Assembly.Equals( introducedInterface.Definition.DeclaringAssembly.GetSymbol() ) )
            {
                // The type may not have been analyzed yet.
                builder.AnalyzeType( introducedInterface );
            }

            builder.AddDerivedType( introducedInterface, transformation.TargetType );
        }

        return builder?.ToImmutable() ?? this;
    }

    internal DerivedTypeIndex WithIntroducedTypes( IEnumerable<IntroduceNamedTypeTransformation> introducedTypes )
    {
        Builder? builder = null;

        foreach ( var transformation in introducedTypes )
        {
            builder ??= new Builder( this );

            var introducedType = transformation.BuilderData;

            if ( introducedType.BaseType is { } baseType )
            {
                builder.AddDerivedType( baseType, introducedType.ToRef() );
            }

            foreach ( var implementedInterface in introducedType.ImplementedInterfaces )
            {
                builder.AddDerivedType( implementedInterface, introducedType.ToRef() );
            }
        }

        return builder?.ToImmutable() ?? this;
    }

    public void PopulateDependencies( IDependencyCollector collector )
    {
        foreach ( var baseType in this._relationships.Keys )
        {
            if ( baseType.Value is INamedTypeSymbol { OriginalDefinition.DeclaringSyntaxReferences.IsDefaultOrEmpty: false } baseTypeSymbol )
            {
                this.PopulateDependenciesCore( collector, baseTypeSymbol, baseType );
            }
        }
    }

    private void PopulateDependenciesCore( IDependencyCollector collector, INamedTypeSymbol rootType, NamedTypeRef baseType )
    {
        foreach ( var derivedType in this._relationships[baseType] )
        {
            if ( derivedType.Value is INamedTypeSymbol derivedTypeSymbol )
            {
                collector.AddDependency( rootType, derivedTypeSymbol );
                this.PopulateDependenciesCore( collector, rootType, derivedType );
            }
        }
    }

    internal DerivedTypeIndex WithAdditionalAnalyzedTypes( IEnumerable<INamedTypeSymbol> types )
    {
        Builder? builder = null;

        foreach ( var type in types )
        {
            if ( !this._processedTypes.Contains( new NamedTypeRef( type ) ) )
            {
                builder ??= new Builder( this );
                builder.AnalyzeType( type );
            }
        }

        if ( builder == null )
        {
            return this;
        }
        else
        {
            return builder.ToImmutable();
        }
    }

    internal DerivedTypeIndex WithAdditionalAnalyzedTypes( IEnumerable<INamedType> types )
    {
        Builder? builder = null;

        foreach ( var type in types )
        {
            if ( !this._processedTypes.Contains( new NamedTypeRef( type.ToFullRef() ) ) )
            {
                builder ??= new Builder( this );
                builder.AnalyzeType( type.ToRef() );
            }
        }

        if ( builder == null )
        {
            return this;
        }
        else
        {
            return builder.ToImmutable();
        }
    }
}