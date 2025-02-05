// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using System;
using System.Linq;

namespace Metalama.Backstage.Licensing
{
    internal static class ApplicationInfoLicenseExtensions
    {
        public static bool IsPreviewLicenseEligible( this IComponentInfo component )
            => (component.IsPrerelease ?? false) && component.BuildDate.HasValue && component.Company == "PostSharp Technologies";

        // ReSharper disable once UnusedMember.Global
        public static bool IsPreviewLicenseEligible( this IApplicationInfo application )
            => ((IComponentInfo) application).IsPreviewLicenseEligible() || application.Components.Any( c => c.IsPreviewLicenseEligible() );

        public static IComponentInfo GetLatestComponentMadeByPostSharp( this IApplicationInfo application )
        {
            IComponentInfo latestComponentLicensedByBuildDate = application;

            foreach ( var component in application.Components )
            {
                if ( component.Company != "PostSharp Technologies" )
                {
                    continue;
                }

                if ( !component.BuildDate.HasValue )
                {
                    throw new InvalidOperationException( $"Application component '{component.Name}' is missing build date information." );
                }

                // If a component is built the same day as the application, we prefer the component.
                if ( latestComponentLicensedByBuildDate.BuildDate <= component.BuildDate )
                {
                    latestComponentLicensedByBuildDate = component;
                }
            }

            return latestComponentLicensedByBuildDate;
        }
    }
}