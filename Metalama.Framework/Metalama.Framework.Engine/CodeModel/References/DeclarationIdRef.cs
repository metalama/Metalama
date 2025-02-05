// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.CodeModel.References;

/// <summary>
/// Implementation of <see cref="IDurableRef{T}"/> based on <see cref="SerializableDeclarationId"/>.
/// </summary>
internal sealed class DeclarationIdRef<T> : DurableRef<T>
    where T : class, ICompilationElement
{
    private DeclarationIdRef( string id ) : base( id ) { }

    public DeclarationIdRef( SerializableDeclarationId id ) : base( id.Id ) { }

    public override SerializableDeclarationId ToSerializableId() => new( this.Id );

    protected override ISymbol GetSymbol( CompilationContext compilationContext, bool ignoreAssemblyKey = false )
        => new SerializableDeclarationId( this.Id ).ResolveToSymbol( compilationContext );

    protected override ICompilationElement? Resolve(
        CompilationModel compilation,
        bool throwIfMissing,
        IGenericContext genericContext,
        Type interfaceType )
    {
        Invariant.Assert( genericContext.IsEmptyOrIdentity );
        
        var declaration = new SerializableDeclarationId( this.Id ).ResolveToDeclaration( compilation );

        if ( declaration == null )
        {
            return ReturnNullOrThrow( this.Id, throwIfMissing, compilation );
        }

        return ConvertDeclarationOrThrow( declaration, compilation, interfaceType );
    }

    protected override IRef<TOut> CastAsRef<TOut>() => this as IRef<TOut> ?? new DeclarationIdRef<TOut>( this.Id );

    public override IFullRef ToFullRef( RefFactory refFactory )
    {
        // TODO: BuilderData
        var symbol = new SerializableDeclarationId( this.Id ).ResolveToSymbol( refFactory.CompilationContext );

        return refFactory.FromAnySymbol( symbol );
    }
}