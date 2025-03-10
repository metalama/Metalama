// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;
using System;
using System.Reflection;

namespace Metalama.Framework.Code;

/// <summary>
/// A service that caches declarations of the current compilation. It is typically used to cache often-used declarations
/// across aspect instances.
/// </summary>
[CompileTime]
[InternalImplement]
public interface IDeclarationCache
{
    /// <summary>
    /// Gets an item from the cache or computes it and add its.
    /// </summary>
    /// <param name="func">The delegate that computes the item. The cache key is the <see cref="MethodInfo"/> of this delegate.
    /// It is essential that the implementation of this delegate is "static", i.e. has no reference to anything else than the <see cref="ICompilation"/>
    /// it is provided with. </param>
    /// <typeparam name="T">The kind of cached item.</typeparam>
    T GetOrAdd<T>( Func<ICompilation, T> func )
        where T : class;
}