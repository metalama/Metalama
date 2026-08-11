// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.UserCode;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.CodeModel.References;

internal sealed class TypeIdRef<T> : DurableRef<T>
    where T : class, ICompilationElement
{
    private TypeIdRef( string id )
    {
        Invariant.Assert( SerializableTypeId.IsTypeId( id ) );

        this.Id = id;
    }

    public TypeIdRef( SerializableTypeId id ) : this( id.Id ) { }

    public override string Id { get; }

    /// <summary>
    /// Returns the type identifier wrapped in a <see cref="SerializableDeclarationId"/>.
    /// </summary>
    /// <remarks>
    /// This is the same representation that <c>SerializableDeclarationIdProvider</c> gives an array or a pointer type,
    /// whose identifier is a type identifier as well, and the resolution of an identifier already dispatches on the
    /// prefix. Throwing here instead made every caller that asks a durable reference for its identifier fail once
    /// named types started being identified this way, including <c>GetPrimarySyntaxTree</c>.
    /// </remarks>
    public override SerializableDeclarationId ToSerializableId() => new( this.Id );

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

        if ( this.GetCachedRef( compilation.RefFactory ) is { } cachedRef )
        {
            return cachedRef.GetTargetInterface( compilation, interfaceType, null, throwIfMissing );
        }

        // The resolution looks a name up through the namespaces, which BuildAspect rejects at design time because
        // design-time cache invalidation cannot track such a query. This is framework machinery and not a user code
        // model query: the identifier names a single type, and the dependency on the project that declares it is
        // already tracked through the project version. Dependency collection is therefore suppressed for the duration
        // of the lookup, exactly as SerializableDeclarationId.ResolveToDeclaration does for the same reason (issue
        // #1752). Without it, resolving a durable reference to a type throws inside BuildAspect at design time.
        using ( UserCodeExecutionContext.CurrentOrNull?.WithoutDependencyCollection() ?? default )
        {
            if ( !compilation.SerializableTypeIdResolver.TryResolveId( new SerializableTypeId( this.Id ), out var type ) )
            {
                return ReturnNullOrThrow( this.Id, throwIfMissing, compilation );
            }

            this.SetCachedRef( compilation.RefFactory, type );

            return ConvertDeclarationOrThrow( type, compilation, interfaceType );
        }
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