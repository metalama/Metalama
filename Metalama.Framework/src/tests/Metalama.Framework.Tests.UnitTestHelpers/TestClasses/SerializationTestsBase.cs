// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Newtonsoft.Json;
using System.IO;

namespace Metalama.Framework.Tests.UnitTestHelpers.TestClasses;

public class SerializationTestsBase
{
    /// <summary>
    /// Round-trips <paramref name="input"/> through Newtonsoft.Json with <see cref="TypeNameHandling.All"/>.
    /// Use this for types that travel the cross-version <c>Metalama.Framework.DesignTime.Contracts</c> /
    /// <c>JsonSerializationBinder</c> path (e.g., <c>DevEnvEntryPoint</c>); use <see cref="MessagePackRoundloop{T}"/>
    /// for types that travel the same-version named-pipe RPC wire path.
    /// </summary>
    protected static T Roundloop<T>( T input )
    {
        var stringWriter = new StringWriter();
        var serializer = JsonSerializer.Create( new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All } );
        serializer.Serialize( stringWriter, input );

        return serializer.Deserialize<T>( new JsonTextReader( new StringReader( stringWriter.ToString() ) ) )!;
    }

    /// <summary>
    /// Round-trips <paramref name="input"/> through the MessagePack RPC pipeline (<see cref="MessagePackHelper"/>),
    /// matching the wire path used by <c>BaseEndpoint.CreateRpc</c>. Use this for <c>[RpcContract]</c>-annotated
    /// types that flow over the same-version named-pipe RPC.
    /// </summary>
    protected static T MessagePackRoundloop<T>( T input )
    {
        var helper = new MessagePackHelper();

        return helper.Deserialize<T>( helper.Serialize( input ) );
    }
}