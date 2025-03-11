// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Backstage.Extensibility
{
    public static class ServiceProviderExtensions
    {
        public static TService? GetBackstageService<TService>( this IServiceProvider serviceProvider )
            where TService : class, IBackstageService
        {
            return (TService?) serviceProvider.GetService( typeof(TService) );
        }

        public static TService GetRequiredBackstageService<TService>( this IServiceProvider serviceProvider )
            where TService : class, IBackstageService
        {
            var service = serviceProvider.GetBackstageService<TService>();

            if ( service == null )
            {
                throw new InvalidOperationException( $"There is no service of type {typeof(TService).Name}" );
            }

            return service;
        }
    }
}