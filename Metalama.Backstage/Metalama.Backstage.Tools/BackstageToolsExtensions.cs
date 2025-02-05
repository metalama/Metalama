// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.Tools;

public static class BackstageToolsExtensions
{
    public static ServiceProviderBuilder AddTools( this ServiceProviderBuilder builder )
    {
        builder.AddService( typeof(IBackstageToolsExtractor), serviceProvider => new BackstageToolsExtractor( serviceProvider ) );

        return builder;
    }
}