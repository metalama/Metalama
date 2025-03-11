// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Extensions.DependencyInjection.Implementation;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Extensions.DependencyInjection.ServiceLocator;

internal class EarlyServiceLocatorDependencyInjectionStrategy : DefaultDependencyInjectionStrategy, ITemplateProvider
{
    public EarlyServiceLocatorDependencyInjectionStrategy( DependencyProperties properties ) : base( properties ) { }

    protected override bool TryPullDependency(
        IAdviser<IConstructor> adviser,
        IFieldOrProperty dependencyFieldOrProperty,
        IPullStrategy pullStrategy )
    {
        adviser.WithTemplateProvider( this )
            .AddInitializer(
                nameof(this.InitializerTemplate),
                args: new { T = this.Properties.DependencyType, fieldOrProperty = dependencyFieldOrProperty } );

        return true;
    }

    [Template]
    public void InitializerTemplate<[CompileTime] T>( IFieldOrProperty fieldOrProperty )
    {
        var isRequired = this.Properties.IsRequired;

        if ( isRequired )
        {
            fieldOrProperty.Value =
                (T) ServiceProviderProvider.ServiceProvider().GetService( typeof(T) )
                ?? throw new InvalidOperationException( $"The service '{fieldOrProperty.Type}' could not be obtained from the service locator." );
        }
        else
        {
            fieldOrProperty.Value =
                (T) ServiceProviderProvider.ServiceProvider().GetService( typeof(T) );
        }
    }
}