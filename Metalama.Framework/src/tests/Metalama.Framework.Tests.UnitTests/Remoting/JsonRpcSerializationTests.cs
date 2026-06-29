// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.Rpc.Notifications;
using Metalama.Framework.DesignTime.VisualStudio.Rpc;
using Newtonsoft.Json;
using System;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Remoting;

/// <summary>
/// Round-trip and security tests for the JSON RPC wire path (#1651 / GHSA-h26j-4vp7-x9w2). The serializer settings
/// mirror <c>BaseEndpoint.CreateRpc</c> (<see cref="TypeNameHandling.All"/> + the production
/// <see cref="JsonSerializationBinder"/>), so these tests exercise the real wire path and its per-type allow-list.
/// </summary>
public sealed class JsonRpcSerializationTests
{
    private static readonly JsonSerializerSettings _settings = CreateSettings();

    private static JsonSerializerSettings CreateSettings()
        => new()
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            SerializationBinder = new JsonSerializationBinderProvider().Binder
        };

    private static T Roundtrip<T>( T value ) => JsonConvert.DeserializeObject<T>( JsonConvert.SerializeObject( value, typeof(T), _settings ), _settings )!;

    [Fact]
    public void ProjectKey_Roundtrip()
    {
        var original = new ProjectKey( "MyAssembly", 12345UL, true );
        var result = Roundtrip( original );

        Assert.Equal( original.AssemblyName, result.AssemblyName );
        Assert.Equal( original.PreprocessorSymbolHashCode, result.PreprocessorSymbolHashCode );
    }

    [Fact]
    public void RpcEventEnvelope_PolymorphicData_Roundtrip()
    {
        // RpcEventEnvelope.Data is the abstract RpcEventData, so the concrete type name (and the nested
        // ImmutableArray<string>) are written on the wire and resolved through the allow-list on read.
        var original = new RpcEventEnvelope(
            "TestApi",
            new CompilationResultChangedEventData(
                new ProjectKey( "Test", 123UL, true ),
                false,
                ImmutableArray.Create( "file1.cs", "file2.cs" ) ) );

        var result = Roundtrip( original );

        var data = Assert.IsType<CompilationResultChangedEventData>( result.Data );
        Assert.True( data.SyntaxTreePaths.SequenceEqual( new[] { "file1.cs", "file2.cs" } ) );
    }

    [Fact]
    public void OnContractType_AllowedThroughObjectPath()
    {
        // An allow-listed type must still round-trip through the typeless ('object'-typed) path.
        object original = new ProjectKey( "MyAssembly", 12345UL, true );

        var json = JsonConvert.SerializeObject( original, typeof(object), _settings );
        var result = JsonConvert.DeserializeObject<object>( json, _settings );

        Assert.Equal( "MyAssembly", Assert.IsType<ProjectKey>( result ).AssemblyName );
    }

    [Fact]
    public void OffContractType_RejectedOnDeserialize()
    {
        // Models an attacker-chosen gadget: a type NOT on the allow-list, serialized through the object path so its
        // $type is written. Serialization is unguarded (the attacker crafts arbitrary bytes); the guard is on the
        // deserialization side, which is the untrusted boundary.
        var json = JsonConvert.SerializeObject( new OffContractGadget(), typeof(object), _settings );

        AssertRejectedByAllowList( () => JsonConvert.DeserializeObject<object>( json, _settings ) );
    }

    [Fact]
    public void OffContractEventData_RejectedInEnvelope()
    {
        // An RpcEventData subclass that is not on the allow-list must be rejected when it rides the abstract Data slot.
        var json = JsonConvert.SerializeObject( new RpcEventEnvelope( "TestApi", new OffContractEventData() ), _settings );

        AssertRejectedByAllowList( () => JsonConvert.DeserializeObject<RpcEventEnvelope>( json, _settings ) );
    }

    private static void AssertRejectedByAllowList( Func<object?> deserialize )
    {
        var exception = Record.Exception( () => deserialize() );

        Assert.NotNull( exception );

        // Newtonsoft wraps binder exceptions in JsonSerializationException; the #1651 marker survives in ToString().
        Assert.Contains( "#1651", exception!.ToString() );
    }
}

/// <summary>An <see cref="RpcEventData"/> subclass deliberately NOT on the allow-list (models an attacker-controlled event).</summary>
internal sealed class OffContractEventData : RpcEventData
{
    public override string Category => nameof(OffContractEventData);
}

/// <summary>A type deliberately NOT on the allow-list, standing in for an attacker-chosen gadget.</summary>
internal sealed class OffContractGadget
{
    public string Payload { get; set; } = "pwned";
}
