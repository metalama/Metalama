// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Extensions.DependencyInjection.Implementation;
using Metalama.Framework.Aspects;

namespace Metalama.Extensions.DependencyInjection;

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

    public void RegisterFramework<TFramework>( int priority = 0 )
        where TFramework : IDependencyInjectionFramework
    {
        this._options = this._options with
        {
            FrameworkRegistrations =
            this._options.FrameworkRegistrations.AddOrApplyChanges( new DependencyInjectionFrameworkRegistration( typeof(TFramework), priority ) )
        };
    }

    public void UnregisterFramework<TFramework>()
        where TFramework : IDependencyInjectionFramework
    {
        this._options = this._options with { FrameworkRegistrations = this._options.FrameworkRegistrations.Remove( typeof(TFramework) ) };
    }

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