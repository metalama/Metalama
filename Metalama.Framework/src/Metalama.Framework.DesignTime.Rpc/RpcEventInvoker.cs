// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;

namespace Metalama.Framework.DesignTime.Rpc;

/// <summary>
/// Raises an event received through RPC so that an exception thrown by one subscriber neither prevents the other
/// subscribers from receiving the event nor escapes to the RPC dispatcher or to a thread pool thread. See
/// https://github.com/metalama/Metalama/issues/2317.
/// </summary>
[PublicAPI]
public static class RpcEventInvoker
{
    /// <summary>
    /// Invokes every handler of <paramref name="handlers"/> with <paramref name="argument"/>, and reports the
    /// exceptions they throw to <paramref name="logger"/> and <paramref name="exceptionHandler"/>.
    /// </summary>
    public static void InvokeEach<T>( Action<T>? handlers, T argument, ILogger logger, IRpcExceptionHandler? exceptionHandler )
    {
        if ( handlers == null )
        {
            return;
        }

        foreach ( var handler in handlers.GetInvocationList() )
        {
            try
            {
                ((Action<T>) handler).Invoke( argument );
            }
            catch ( Exception e )
            {
                Report( e, logger, exceptionHandler, isDisposing: false );
            }
        }
    }

    /// <summary>
    /// Reports an exception caught at an RPC boundary. Never throws.
    /// </summary>
    internal static void Report( Exception e, ILogger logger, IRpcExceptionHandler? exceptionHandler, bool isDisposing )
    {
        try
        {
            logger.LogException( e );
            exceptionHandler?.OnException( e, logger, isDisposing );
        }
        catch ( Exception reportingException )
        {
            logger.Error?.Log( $"Cannot report an exception: {reportingException}" );
        }
    }
}
