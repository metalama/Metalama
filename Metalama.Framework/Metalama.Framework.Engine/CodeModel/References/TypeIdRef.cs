// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.CodeModel.References;

internal sealed class TypeIdRef<T> : DurableRef<T>
    where T : class, ICompilationElement
{
    private TypeIdRef( string id ) : base( id )
    {
        Invariant.Assert( SerializableTypeId.IsTypeId( id ) );
    }

    public TypeIdRef( SerializableTypeId id ) : base( id.Id ) { }

    public override SerializableDeclarationId ToSerializableId()
        => throw new NotSupportedException( "The durable reference must be first resolved to a full reference." );

    protected override ISymbol GetSymbol( CompilationContext compilationContext, bool ignoreAssemblyKey = false )
    {
        if ( !compilationContext.SerializableTypeIdResolver.TryResolveId( new SerializableTypeId( this.Id ), out var symbol ) )
        {
            throw new InvalidOperationException( $"Unable to resolve type id: {this.Id}." );
        }

        return symbol;
    }

    protected override ICompilationElement? Resolve(
        CompilationModel compilation,
        bool throwIfMissing,
        IGenericContext genericContext,
        Type interfaceType )
    {
        Invariant.Assert( genericContext.IsEmptyOrIdentity );
        
        if ( !compilation.SerializableTypeIdResolver.TryResolveId( new SerializableTypeId( this.Id ), out var symbol ) )
        {
            return ReturnNullOrThrow( this.Id, throwIfMissing, compilation );
        }

        return ConvertDeclarationOrThrow( symbol, compilation, interfaceType );
    }

    protected override IRef<TOut> CastAsRef<TOut>() => this as IRef<TOut> ?? new TypeIdRef<TOut>( this.Id );

    public override IFullRef ToFullRef( RefFactory refFactory )
    {
        if ( !refFactory.CompilationContext.SerializableTypeIdResolver.TryResolveId( new SerializableTypeId( this.Id ), out var symbol ) )
        {
            throw new InvalidOperationException( $"Unable to resolve type id: {this.Id}." );
        }

        return refFactory.FromAnySymbol( symbol );
    }
}