// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Introspection;
using Metalama.Framework.Workspaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Metalama.Introspection.Navigation.Facade;

/// <summary>
/// Gets the <see cref="FacadeType"/> of a type, given the set of assemblies whose types are considered public.
/// </summary>
internal sealed class FacadeTypeFactory
{
    private readonly ConcurrentDictionary<Type, FacadeType> _types = new();

    // A weak table does not keep a collectible type alive, so it does not keep the load context of a query from unloading.
    private readonly ConditionalWeakTable<Type, FacadeType> _collectibleTypes = new();

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
            ? this._collectibleTypes.GetValue( type, t => new FacadeType( this, t ) )
            : this._types.GetOrAdd( type, t => new FacadeType( this, t ) );

    /// <summary>
    /// Gets a value indicating whether a type, or one of its generic arguments, comes from a collectible assembly, such as a
    /// compiled query. Such types are cached in weak tables only, so that the cache does not keep their load context from
    /// unloading.
    /// </summary>
    internal static bool IsCollectible( Type type )
        => type.Assembly.IsCollectible
           || (type.IsGenericType && type.GetGenericArguments().Any( IsCollectible ))
           || (type.HasElementType && IsCollectible( type.GetElementType()! ));
}
