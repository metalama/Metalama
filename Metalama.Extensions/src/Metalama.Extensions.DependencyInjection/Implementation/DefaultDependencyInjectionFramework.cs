// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using System;

namespace Metalama.Extensions.DependencyInjection.Implementation;

/// <summary>
/// The default implementation of <see cref="IDependencyInjectionFramework"/>. It pulls dependencies from all constructors and use <see cref="Func{TResult}"/>
/// to accept lazy dependencies.
/// </summary>
public class DefaultDependencyInjectionFramework : IDependencyInjectionFramework
{
    /// <inheritdoc />
    public virtual bool CanHandleDependency( DependencyProperties properties, in ScopedDiagnosticSink diagnostics ) => !properties.IsStatic;

    /// <inheritdoc />
    public IntroduceDependencyResult IntroduceDependency(
        DependencyProperties properties,
        IAdviser<INamedType> adviser )
    {
        return this.GetStrategy( properties ).IntroduceDependency( adviser );
    }

    /// <inheritdoc />
    public bool TryImplementDependency( DependencyProperties properties, IAdviser<IFieldOrProperty> adviser )
    {
        return this.GetStrategy( properties ).TryImplementDependency( adviser );
    }

    /// <summary>
    /// Gets an instance of the <see cref="DefaultDependencyInjectionStrategy"/> class for a given context.
    /// </summary>
    protected virtual DefaultDependencyInjectionStrategy GetStrategy( DependencyProperties properties )
        => properties.IsLazy
            ? new LazyDependencyInjectionStrategy( properties )
            : new DefaultDependencyInjectionStrategy( properties );
}