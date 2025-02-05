// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Metalama.Framework.Engine.Utilities;

public static class AppDomainUtility
{
    /// <summary>
    /// Gets an object that can be locked when user assembly is being awaited for being unloaded. It makes sure that all arbitrary references
    /// to the <see cref="Assembly"/> are shared only within the lifetime of this lock.
    /// </summary>
    [PublicAPI]
    public static object Sync { get; } = new();

    /// <summary>
    /// Gets a list of all loaded assemblies fulfilling a given predicate while holding a lock on <see cref="Sync"/>.
    /// </summary>
    public static List<Assembly> GetLoadedAssemblies( Predicate<Assembly> predicate )
    {
        lock ( Sync )
        {
            var list = new List<Assembly>();
            list.AddRange( AppDomain.CurrentDomain.GetAssemblies().Where( a => predicate( a ) ) );

            return list;
        }
    }

    /// <summary>
    /// Determines whether the current <see cref="AppDomain"/> contains an <see cref="Assembly"/> fulfilling a given predicate,
    /// while holding a lock on <see cref="Sync"/>.
    /// </summary>
    internal static bool HasAnyLoadedAssembly( Predicate<Assembly> predicate )
    {
        lock ( Sync )
        {
            return AppDomain.CurrentDomain.GetAssemblies().Any( a => predicate( a ) );
        }
    }
}