// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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