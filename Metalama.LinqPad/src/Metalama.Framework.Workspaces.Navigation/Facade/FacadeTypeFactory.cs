// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Introspection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace Metalama.Framework.Workspaces.Navigation.Facade;

/// <summary>
/// Gets the <see cref="FacadeType"/> of a type, given the set of assemblies whose types are considered public.
/// </summary>
internal sealed class FacadeTypeFactory
{
    private readonly ConcurrentDictionary<Type, FacadeType> _types = new();

    public static FacadeTypeFactory Default { get; } = new();

    public ImmutableHashSet<Assembly> PublicAssemblies { get; }

    public FacadeTypeFactory( IEnumerable<Assembly>? publicAssemblies = null )
    {
        publicAssemblies ??= [];

        this.PublicAssemblies = new[]
            {
                typeof(IDeclaration).Assembly, typeof(IIntrospectionAdvice).Assembly, typeof(ICompilationSet).Assembly, typeof(Type).Assembly
            }
            .Concat( publicAssemblies )
            .ToImmutableHashSet();
    }

    public FacadeType GetFacadeType( Type type )
        => IsCollectible( type )
            ? new FacadeType( this, type )
            : this._types.GetOrAdd( type, t => new FacadeType( this, t ) );

    /// <summary>
    /// Gets a value indicating whether a type, or one of its generic arguments, comes from a collectible assembly, such as a
    /// compiled query. Such types are not cached, because the cache would keep their load context from unloading.
    /// </summary>
    internal static bool IsCollectible( Type type )
        => type.Assembly.IsCollectible
           || (type.IsGenericType && type.GetGenericArguments().Any( IsCollectible ))
           || (type.HasElementType && IsCollectible( type.GetElementType()! ));
}
