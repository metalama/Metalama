// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.CodeModel.References;

/// <summary>
/// The base implementation of <see cref="IRef{T}"/> except for attributes.
/// </summary>
internal abstract class BaseRef<T> : IRefImpl, IRef<T>
    where T : class, ICompilationElement
{
    // The compilation for which the symbol (stored in Target) is valid.

    static BaseRef()
    {
        if ( !typeof(T).IsInterface )
        {
            throw new ArgumentException( "The type argument must be an interface." );
        }
    }

    public virtual RefTargetKind TargetKind => RefTargetKind.Default;

    public abstract SerializableDeclarationId ToSerializableId();
    
    public virtual SerializableDeclarationId ToSerializableId( CompilationContext compilationContext ) => this.ToSerializableId();

    protected abstract IDurableRef<T> ToDurable();

    public abstract bool IsDurable { get; }

    IRef IRefImpl.ToDurable() => this.ToDurable();

    public T GetTarget( ICompilation compilation, IGenericContext? genericContext = null )
        => (T) this.GetTargetImpl( compilation, true, genericContext, typeof(T) )!;

    [PublicAPI]
    public T? GetTargetOrNull( ICompilation compilation, IGenericContext? genericContext = null )
        => (T?) this.GetTargetImpl( compilation, false, genericContext, typeof(T) );

    ICompilationElement? IRef.GetTargetInterface( ICompilation compilation, Type? interfaceType, IGenericContext? genericContext, bool throwIfMissing )
        => this.GetTargetImpl( compilation, throwIfMissing, genericContext, interfaceType ?? typeof(T) );

    private ICompilationElement? GetTargetImpl( ICompilation compilation, bool throwIfMissing, IGenericContext? genericContext, Type interfaceType )
    {
        genericContext ??= GenericContext.Empty;
        
        using ( StackOverflowHelper.Detect() )
        {
            var compilationModel = (CompilationModel) compilation;

            return this.Resolve( compilationModel, throwIfMissing, genericContext, interfaceType );
        }
    }

    ISymbol ISdkRef.GetSymbol( Compilation compilation, bool ignoreAssemblyKey ) => this.GetSymbol( compilation.GetCompilationContext(), ignoreAssemblyKey );

    protected abstract ISymbol GetSymbol( CompilationContext compilationContext, bool ignoreAssemblyKey = false );

    protected abstract ICompilationElement? Resolve(
        CompilationModel compilation,
        bool throwIfMissing,
        IGenericContext genericContext,
        Type interfaceType );

    protected static T? ReturnNullOrThrow( string id, bool throwIfMissing, CompilationModel compilation, Exception? ex = null )
    {
        if ( throwIfMissing )
        {
            throw new SymbolNotFoundException( id, compilation.RoslynCompilation, ex );
        }
        else
        {
            return null;
        }
    }

    protected static ICompilationElement? ConvertDeclarationOrThrow(
        ICompilationElement? compilationElement,
        CompilationModel compilation,
        Type interfaceType,
        bool throwOnError = true )
    {
        if ( interfaceType == typeof(BaseRef<T>) )
        {
            return compilationElement;
        }

        var result = compilationElement switch
        {
            null => null,
            _ when interfaceType.IsInstanceOfType( compilationElement ) => compilationElement,
            IProperty property when interfaceType == typeof(IField) && property.OriginalField != null => property.OriginalField,
            IField field when interfaceType == typeof(IProperty) && field.OverridingProperty != null => (T) field.OverridingProperty,
            _ when throwOnError => throw new InvalidCastException(
                $"Cannot convert the '{compilationElement}' {compilationElement.DeclarationKind.ToDisplayString()} into a {interfaceType.Name} within the compilation '{compilation.Identity}'." ),
            _ => null
        };

        return result;
    }

    public IRef<TOut> As<TOut>()
        where TOut : class, ICompilationElement
        => this.CastAsRef<TOut>();

    protected abstract IRef<TOut> CastAsRef<TOut>()
        where TOut : class, ICompilationElement;

    // We throw an exception when the vanilla GetHashCode is used to make sure that the caller specifies a RefEqualityComparer 
    // when using IRef as a key of dictionaries.
    public override int GetHashCode() => throw new InvalidOperationException( $"Specify a {nameof(RefEqualityComparer)} or a RefComparison." );

    public bool Equals( IRef? other ) => this.Equals( other, RefComparison.Default );

    public abstract bool Equals( IRef? other, RefComparison comparison );

    public abstract int GetHashCode( RefComparison comparison );
}