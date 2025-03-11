// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;
using System.Collections.Concurrent;

namespace Metalama.Extensions.DependencyInjection.Implementation;

[CompileTime]
internal static class DependencyInjectionFrameworkFactory
{
    private static readonly ConcurrentDictionary<Type, IDependencyInjectionFramework> _instances = new();

    public static IDependencyInjectionFramework GetInstance( Type type )
        => _instances.GetOrAdd( type, t => (IDependencyInjectionFramework) Activator.CreateInstance( t ) );
}