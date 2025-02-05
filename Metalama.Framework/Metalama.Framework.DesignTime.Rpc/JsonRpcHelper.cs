// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using StreamJsonRpc;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;

namespace Metalama.Framework.DesignTime.Rpc;

internal static class JsonRpcHelper
{
    private static bool TryResetJsonRpc()
    {
        // HACK: Per https://github.com/microsoft/vs-streamjsonrpc/issues/660,
        // StreamJsonRpc doesn't handle the case when the same assembly is in different ALCs
        // (though it doesn't happen always, it probably requires that the assemblies have the same version).
        // This happens because StreamJsonRpc reuses Reflection.Emit ModuleBuilders.
        // The workaround is to clear its internal cache of ModuleBuilders and to try again.

        var proxyGenerationType = typeof(JsonRpc).Assembly.GetType( "StreamJsonRpc.ProxyGeneration" );

        var builderLockField = proxyGenerationType?.GetField( "BuilderLock", BindingFlags.Static | BindingFlags.NonPublic );

        if ( builderLockField?.GetValue( null ) is { } builderLock )
        {
            lock ( builderLock )
            {
                var moduleBuilderCacheField = proxyGenerationType?.GetField(
                    "TransparentProxyModuleBuilderByVisibilityCheck",
                    BindingFlags.Static | BindingFlags.NonPublic );

                if ( moduleBuilderCacheField?.GetValue( null ) is List<(ImmutableHashSet<AssemblyName>, ModuleBuilder)> moduleBuilderCache )
                {
                    moduleBuilderCache.Clear();

                    return true;
                }
            }
        }

        return false;
    }

    public static T AttachSafe<T>( this JsonRpc rpc, ILogger logger ) where T : class
    {
        try
        {
            return rpc.Attach<T>();
        }
        catch ( InvalidCastException ex )
        {
            logger.Warning?.Log( $"Attempting to recover from exception: {ex.Message}" );

            if ( TryResetJsonRpc() )
            {
                return rpc.Attach<T>();
            }
            else
            {
                throw;
            }
        }
    }
}