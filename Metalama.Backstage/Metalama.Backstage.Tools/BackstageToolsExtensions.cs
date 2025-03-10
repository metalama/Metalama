// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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