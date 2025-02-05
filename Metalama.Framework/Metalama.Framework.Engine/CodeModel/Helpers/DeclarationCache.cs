// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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