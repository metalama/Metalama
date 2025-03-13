// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Options;
using System;

namespace Metalama.Extensions.DependencyInjection.Implementation;

/// <summary>
/// Represents a registration of a <see cref="IDependencyInjectionFramework"/>.
/// </summary>
[CompileTime]
public sealed class DependencyInjectionFrameworkRegistration : IIncrementalKeyedCollectionItem<Type>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyInjectionFrameworkRegistration"/> class.
    /// </summary>
    /// <param name="type">A type implementing the <see cref="IDependencyInjectionFramework"/> interface and having a default constructor.</param>
    /// <param name="priority">The priority of the aspect framework. The default priority of system-registered frameworks is 100 and 101.
    /// The default priority for user-registered frameworks is 0. 
    /// </param>
    public DependencyInjectionFrameworkRegistration( Type type, int? priority = null )
    {
        this.Type = type;
        this.Priority = priority;
    }

    Type IIncrementalKeyedCollectionItem<Type>.Key => this.Type;

    object IIncrementalObject.ApplyChanges( object options, in ApplyChangesContext context )
        => new DependencyInjectionFrameworkRegistration( this.Type, ((DependencyInjectionFrameworkRegistration) options).Priority ?? this.Priority );

    /// <summary>
    /// Gets the framework adapter type, a type implementing the <see cref="IDependencyInjectionFramework"/> interface.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Gets the priority of the framework.
    /// </summary>
    public int? Priority { get; }
}