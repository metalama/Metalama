// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Extensions.DependencyInjection.Implementation;
using Metalama.Framework.Aspects;

namespace Metalama.Extensions.DependencyInjection;

/// <summary>
/// Builder for configuring dependency injection options.
/// </summary>
/// <remarks>
/// Use this builder with <see cref="DependencyInjectionExtensions.ConfigureDependencyInjection"/> to configure
/// the dependency injection framework, set default options, and register custom DI frameworks.
/// </remarks>
/// <seealso cref="DependencyInjectionExtensions"/>
/// <seealso href="@dependency-injection"/>
[CompileTime]
[PublicAPI]
public class DependencyInjectionOptionsBuilder
{
    private DependencyInjectionOptions _options = new();

    /// <summary>
    /// Sets a value indicating whether the default value for the <see cref="DependencyAttribute.IsRequired"/> property of <see cref="DependencyAttribute"/> and <see cref="IntroduceDependencyAttribute"/>.
    /// </summary>
    public bool IsRequired
    {
        set => this._options = this._options with { IsRequired = value };
    }

    /// <summary>
    /// Sets a value indicating whether the default value for the <see cref="DependencyAttribute.IsLazy"/> property of <see cref="DependencyAttribute"/> and <see cref="IntroduceDependencyAttribute"/>.
    /// </summary>
    public bool IsLazy
    {
        set => this._options = this._options with { IsLazy = value };
    }

    /// <summary>
    /// Registers a custom dependency injection framework.
    /// </summary>
    /// <typeparam name="TFramework">The type of the framework to register. Must implement <see cref="IDependencyInjectionFramework"/>.</typeparam>
    /// <param name="priority">The priority of the framework. Frameworks with higher priority are consulted first. Default is 0.</param>
    public void RegisterFramework<TFramework>( int priority = 0 )
        where TFramework : IDependencyInjectionFramework
    {
        this._options = this._options with
        {
            FrameworkRegistrations =
            this._options.FrameworkRegistrations.AddOrApplyChanges( new DependencyInjectionFrameworkRegistration( typeof(TFramework), priority ) )
        };
    }

    /// <summary>
    /// Unregisters a previously registered dependency injection framework.
    /// </summary>
    /// <typeparam name="TFramework">The type of the framework to unregister.</typeparam>
    public void UnregisterFramework<TFramework>()
        where TFramework : IDependencyInjectionFramework
    {
        this._options = this._options with { FrameworkRegistrations = this._options.FrameworkRegistrations.Remove( typeof(TFramework) ) };
    }

    /// <summary>
    /// Sets the priority of a registered dependency injection framework.
    /// </summary>
    /// <typeparam name="TFramework">The type of the framework.</typeparam>
    /// <param name="priority">The new priority value. Frameworks with higher priority are consulted first.</param>
    public void SetFrameworkPriority<TFramework>( int priority )
        where TFramework : IDependencyInjectionFramework
    {
        this._options = this._options with
        {
            FrameworkRegistrations =
            this._options.FrameworkRegistrations.AddOrApplyChanges( new DependencyInjectionFrameworkRegistration( typeof(TFramework), priority ) )
        };
    }

    /// <summary>
    /// Sets a delegate that is called when several dependency injection frameworks have been registered
    /// for the current project and many vote to handle a given dependency. The default implementation is to return
    /// the first framework in the array.
    /// </summary>
    public IDependencyInjectionFrameworkSelector? Selector
    {
        set => this._options = this._options with { Selector = value };
    }

    public DependencyInjectionOptions Build() => this._options;
}