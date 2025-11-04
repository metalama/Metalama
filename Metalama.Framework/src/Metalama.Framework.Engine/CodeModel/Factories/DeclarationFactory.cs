// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Source.ConstructedTypes;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.CompileTime.Serialization.Serializers;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using SpecialType = Metalama.Framework.Code.SpecialType;

namespace Metalama.Framework.Engine.CodeModel.Factories;

/// <summary>
/// Creates instances of <see cref="IDeclaration"/> for a given <see cref="CompilationModel"/>.
/// </summary>
[PublicAPI]
public sealed partial class DeclarationFactory : IDeclarationFactory, ISdkDeclarationFactory
{
    private readonly ConcurrentDictionary<AttributeSerializationData, DeserializedAttribute> _deserializedAttributes = new();

    private readonly INamedType?[] _specialTypes = new INamedType?[(int) SpecialType.Count];
    private readonly INamedType?[] _internalSpecialTypes = new INamedType?[(int) InternalSpecialType.Count];

    private readonly CompilationModel _compilationModel;
    private readonly CompileTimeTypeResolver _systemTypeResolver;

    internal DeclarationFactory( CompilationModel compilation )
    {
        this._compilationModel = compilation;
        this._builderCache = new Cache<DeclarationBuilderData, IDeclaration>( ReferenceEqualityComparer<DeclarationBuilderData>.Instance );
        this._symbolCache = new Cache<ISymbol, IDeclaration>( compilation.CompilationContext.SymbolComparer );
        this._typeCache = new Cache<ISymbol, IType>( compilation.CompilationContext.SymbolComparerIncludingNullability );
        this._tupleTypeCache = new Cache<TupleTypeKey, ITupleType>( EqualityComparer<TupleTypeKey>.Default );

        this._systemTypeResolver = compilation.Project.ServiceProvider.GetRequiredService<SystemTypeResolver.Provider>()
            .Get( compilation.CompilationContext );
    }

    private Compilation RoslynCompilation => this._compilationModel.RoslynCompilation;

    public INamedType GetTypeByReflectionName( string reflectionName )
    {
        var symbol = this._compilationModel.CompilationContext.ReflectionMapper.GetNamedTypeSymbolByMetadataName( reflectionName, null );

        return this.GetNamedType( symbol );
    }

    public bool TryGetTypeByReflectionName( string reflectionName, [NotNullWhen( true )] out INamedType? namedType )
    {
        var symbol = this.Compilation.GetTypeByMetadataName( reflectionName );

        if ( symbol == null )
        {
            namedType = null;

            return false;
        }
        else
        {
            namedType = this.GetNamedType( symbol );

            return true;
        }
    }

    public IType GetTypeByReflectionType( Type type ) => this.GetIType( this._compilationModel.CompilationContext.ReflectionMapper.GetTypeSymbol( type ) );

    public INamedType GetNamedTypeByReflectionType( Type type ) => (INamedType) this.GetTypeByReflectionType( type );

    public INamedType GetSpecialType( SpecialType specialType ) => this._specialTypes[(int) specialType] ??= this.GetSpecialTypeCore( specialType );

    internal INamedType GetSpecialType( InternalSpecialType specialType )
        => this._internalSpecialTypes[(int) specialType] ??= this.GetSpecialTypeCore( specialType );

    private INamedType GetSpecialTypeCore( SpecialType specialType )
    {
        var roslynSpecialType = specialType.ToRoslynSpecialType();

        if ( roslynSpecialType != Microsoft.CodeAnalysis.SpecialType.None )
        {
            return this.GetNamedType( this.RoslynCompilation.GetSpecialType( roslynSpecialType ) );
        }
        else
        {
            return
                specialType switch
                {
                    SpecialType.List_T => (INamedType) this.GetTypeByReflectionType( typeof(List<>) ),
                    SpecialType.ValueTask => (INamedType) this.GetTypeByReflectionType( typeof(ValueTask) ),
                    SpecialType.ValueTask_T => (INamedType) this.GetTypeByReflectionType( typeof(ValueTask<>) ),
                    SpecialType.ValueTuple => (INamedType) this.GetTypeByReflectionType( typeof(ValueTuple) ),
                    SpecialType.Task => (INamedType) this.GetTypeByReflectionType( typeof(Task) ),
                    SpecialType.Task_T => (INamedType) this.GetTypeByReflectionType( typeof(Task<>) ),
                    SpecialType.Type => (INamedType) this.GetTypeByReflectionType( typeof(Type) ),
                    SpecialType.IAsyncEnumerable_T => this.GetTypeByReflectionName( "System.Collections.Generic.IAsyncEnumerable`1" ),
                    SpecialType.IAsyncEnumerator_T => this.GetTypeByReflectionName( "System.Collections.Generic.IAsyncEnumerator`1" ),
                    _ => throw new ArgumentOutOfRangeException( nameof(specialType) )
                };
        }
    }

    private INamedType GetSpecialTypeCore( InternalSpecialType specialType )
        => specialType switch
        {
            InternalSpecialType.ITemplateAttribute => (INamedType) this.GetTypeByReflectionType( typeof(ITemplateAttribute) ),
            _ => throw new ArgumentOutOfRangeException( nameof(specialType) )
        };

    public IDeclaration GetDeclarationFromId( SerializableDeclarationId declarationId )
    {
        var declaration =
            declarationId.ResolveToDeclaration( this._compilationModel )
            ?? throw new InvalidOperationException(
                $"Cannot find the symbol '{declarationId}' in compilation '{this._compilationModel.RoslynCompilation.Assembly.Name}'." );

        return (IDeclaration) declaration;
    }

    public T? Translate<T>(
        T? compilationElement,
        IGenericContext? genericContext = null )
        where T : class, ICompilationElement
    {
        if ( compilationElement == null )
        {
            return null;
        }
        else if ( ReferenceEquals( compilationElement.Compilation, this._compilationModel ) )
        {
            return compilationElement;
        }
        else
        {
            return (T?) ((ICompilationElementImpl) compilationElement).Translate( this._compilationModel, genericContext, typeof(T) );
        }
    }

    public IType GetTypeFromId( SerializableTypeId serializableTypeId, IReadOnlyDictionary<string, IType>? genericArguments )
        => this._compilationModel.SerializableTypeIdResolver.ResolveId( serializableTypeId, genericArguments );

    public ITupleType CreateTupleType( IEnumerable<IType> elementTypes )
    {
        return (ITupleType) BuildValueTupleType( elementTypes.ToArray() );

        // Recursively build the ValueTuple type, handling TRest for > 7 elements
        INamedType BuildValueTupleType( in ReadOnlySpan<IType> types )
        {
            switch ( types.Length )
            {
                case 0:
                    return this.GetTypeByReflectionName( "System.ValueTuple" );

                case <= 7:
                    {
                        // For 7 or fewer elements, use ValueTuple`n directly
                        var valueTupleType = this.GetTypeByReflectionName( $"System.ValueTuple`{types.Length}" );

                        return valueTupleType.WithTypeArguments( types.ToArray() );
                    }

                default:
                    {
                        var valueTypeTypeArgs = new IType[8];

                        // For more than 7 elements, use ValueTuple`8 with TRest
                        types[..7].CopyTo( valueTypeTypeArgs );
                        valueTypeTypeArgs[7] = BuildValueTupleType( types[7..] );

                        var valueTupleType = this.GetTypeByReflectionName( "System.ValueTuple`8" );

                        return valueTupleType.WithTypeArguments( valueTypeTypeArgs );
                    }
            }
        }
    }

    public ITupleType CreateTupleType( IEnumerable<Type> elementTypes ) => this.CreateTupleType( elementTypes.Select( this.GetTypeByReflectionType ) );

    public ITupleType CreateTupleType( IEnumerable<(IType Type, string Name)> elementTypes )
    {
        var elementTypesList = elementTypes.ToReadOnlyList();
        var valueTupleType = (TupleType) this.CreateTupleType( elementTypesList.SelectAsReadOnlyList( x => x.Type ) );
        var elementNames = elementTypesList.Count > 1 ? elementTypesList.SelectAsImmutableArray( x => x.Name ) : default;

        return this.GetTupleTypeFromSymbol( valueTupleType.NamedTypeSymbol, elementNames, valueTupleType.GenericContextForSymbolMapping );
    }

    public ITupleType CreateTupleType( IEnumerable<(Type Type, string Name)> elementTypes )
        => this.CreateTupleType( elementTypes.Select( x => (this.GetTypeByReflectionType( x.Type ), x.Name) ) );

    public ITupleType CreateTupleType( IEnumerable<IParameter> elementTypes ) => this.CreateTupleType( elementTypes.Select( e => (e.Type, e.Name) ) );

    private Compilation Compilation => this._compilationModel.RoslynCompilation;

    private CompilationContext CompilationContext => this._compilationModel.CompilationContext;

    public Type GetReflectionType( ITypeSymbol typeSymbol ) => this._systemTypeResolver.GetCompileTimeType( typeSymbol, true ).AssertNotNull();

    internal DeserializedAttribute GetDeserializedAttribute( AttributeSerializationData serializationData )
        => this._deserializedAttributes.GetOrAdd(
            serializationData,
            static ( data, compilation ) => new DeserializedAttribute( data, compilation ),
            this._compilationModel );

    public void Invalidate( IRef<IDeclaration> declaration )
    {
        switch ( declaration )
        {
            case ISymbolRef symbolRef:
                this._symbolCache.Remove( symbolRef.Symbol );

                break;

            case IIntroducedRef builtDeclarationRef:
                this._builderCache.Remove( builtDeclarationRef.BuilderData );

                break;

            default:
                throw new AssertionFailedException();
        }
    }
}