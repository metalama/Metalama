// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Metalama.Framework.Engine.CodeModel.Helpers;

internal sealed class DeclarationCache : IDeclarationCache
{
    private readonly ConcurrentDictionary<MethodInfo, object> _cache = new();
    private readonly CompilationModel _compilation;

    public DeclarationCache( CompilationModel compilation )
    {
        this._compilation = compilation;
    }

    /// <remarks>The delegate passed to this method should not close over any state (e.g. if it's a lambda, it should be <see langword="static"/>).</remarks>
    public T GetOrAdd<T>( Func<ICompilation, T> func )
        where T : class
    {
        if ( !this._cache.TryGetValue( func.Method, out var value ) )
        {
#if NET6_0_OR_GREATER
            value = this._cache.GetOrAdd( func.Method, static ( _, ctx ) => ctx.func( ctx.me._compilation ), (me: this, func) );
#else
            value = this._cache.GetOrAdd( func.Method, _ => func( this._compilation ) );
#endif
        }

        return (T) value;
    }

    private T GetOrAdd<T>( Func<CompilationModel, T> func )
        where T : notnull
    {
        if ( !this._cache.TryGetValue( func.Method, out var value ) )
        {
#if NET6_0_OR_GREATER
            value = this._cache.GetOrAdd( func.Method, static ( _, ctx ) => ctx.func( ctx.me._compilation ), (me: this, func) );
#else
            value = this._cache.GetOrAdd( func.Method, _ => func( this._compilation ) );
#endif
        }

        return (T) value;
    }

    [Memo]
    public INamedType SystemObjectType => this.GetOrAdd( static c => c.Factory.GetSpecialType( SpecialType.Object ) );

    [Memo]
    public INamedType SystemVoidType => this.GetOrAdd( static c => c.Factory.GetSpecialType( SpecialType.Void ) );

    // ReSharper disable once InconsistentNaming
    [Memo]
    public INamedType ITemplateAttributeType => this.GetOrAdd( static c => c.Factory.GetSpecialType( InternalSpecialType.ITemplateAttribute ) );

    [Memo]
    public INamedType SystemStringType => this.GetOrAdd( static c => c.Factory.GetSpecialType( SpecialType.String ) );
}