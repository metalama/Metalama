// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.DesignTime.Contracts.EntryPoint;

public static class DesignTimeEntryPointConsumerExtensions
{
    public static async ValueTask<ICompilerServiceProvider?> GetServiceProviderAsync(
        this IDesignTimeEntryPointConsumer consumer,
        Version version,
        CancellationToken cancellationToken = default )
    {
        var result = new ICompilerServiceProvider?[1];
        await consumer.GetServiceProviderAsync( version, result, cancellationToken );

        return result[0];
    }
}