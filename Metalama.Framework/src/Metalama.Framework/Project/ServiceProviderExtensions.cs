// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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