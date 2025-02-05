// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Services;
using System;

namespace Metalama.Framework.Project
{
    // API in line with Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions class.

    /// <summary>
    /// Provides extensions methods to the <see cref="IServiceProvider"/> interface.
    /// </summary>
    /// <seealso cref="IGlobalService"/>
    [CompileTime]
    public static class ServiceProviderExtensions
    {
        public static T GetRequiredService<T>( this IServiceProvider<IGlobalService> serviceProvider )
            where T : class, IGlobalService
        {
            var service = (T?) serviceProvider.GetService( typeof(T) ) ?? throw new InvalidOperationException( $"Cannot get the service {typeof(T).Name}." );

            return service;
        }

        public static T GetRequiredService<T>( this IServiceProvider<IProjectService> serviceProvider )
            where T : class, IProjectService
        {
            var service = (T?) serviceProvider.GetService( typeof(T) ) ?? throw new InvalidOperationException( $"Cannot get the service {typeof(T).Name}." );

            return service;
        }
    }
}