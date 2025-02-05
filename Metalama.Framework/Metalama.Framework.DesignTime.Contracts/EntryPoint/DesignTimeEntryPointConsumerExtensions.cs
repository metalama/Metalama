// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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