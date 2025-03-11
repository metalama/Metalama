// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Engine.Utilities.UserCode;

/// <summary>
/// Wraps a <see cref="Func{TResult}"/> into a <see cref="UserCodeFunc{TResult,TPayload}"/>.
/// </summary>
internal struct UserCodeFuncAdapter<T>
{
    // Intentionally not marking the struct as readonly to avoid defensive copies when passing by ref.

    private static readonly UserCodeFunc<T, UserCodeFuncAdapter<T>> _cachedDelegate = Func;

    private static T Func( ref UserCodeFuncAdapter<T> payload ) => payload._func();

    private readonly Func<T> _func;

    public UserCodeFuncAdapter( Func<T> func )
    {
        this._func = func;
    }

#pragma warning disable CA1822
    public UserCodeFunc<T, UserCodeFuncAdapter<T>> UserCodeFunc => _cachedDelegate;
#pragma warning restore CA1822
}