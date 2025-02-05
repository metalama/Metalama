// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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