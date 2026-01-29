// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Newtonsoft.Json;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Serialization;

/// <summary>
/// Base class for JSON serialization tests providing helper methods for round-trip testing.
/// </summary>
public abstract class JsonSerializationTestsBase
{
    protected ITestOutputHelper Output { get; }

    protected JsonSerializationTestsBase( ITestOutputHelper output )
    {
        this.Output = output;
    }

    /// <summary>
    /// Tests JSON serialization round-trip:
    /// 1. Serialize input object to JSON
    /// 2. Compare to expected hardcoded JSON
    /// 3. Deserialize back to object
    /// 4. Serialize again
    /// 5. Compare JSON outputs match
    /// </summary>
    protected void TestSerialization<T>( T inputObject, string expectedJson )
        where T : class
    {
        // Step 1: Serialize object to JSON
        var serializedJson = JsonConvert.SerializeObject( inputObject, Formatting.Indented );
        this.Output.WriteLine( "Serialized JSON:" );
        this.Output.WriteLine( serializedJson );

        // Step 2: Compare to expected hardcoded JSON
        Assert.Equal( NormalizeJson( expectedJson ), NormalizeJson( serializedJson ) );

        // Step 3: Deserialize back to object
        var deserialized = JsonConvert.DeserializeObject<T>( expectedJson );
        Assert.NotNull( deserialized );

        // Step 4: Serialize again
        var reserializedJson = JsonConvert.SerializeObject( deserialized, Formatting.Indented );

        // Step 5: Compare JSON outputs match (round-trip consistency)
        Assert.Equal( NormalizeJson( serializedJson ), NormalizeJson( reserializedJson ) );
    }

    /// <summary>
    /// Overload for classes with custom serialization methods (e.g., ConfigurationFile.ToJson()).
    /// </summary>
    protected void TestSerialization<T>(
        T inputObject,
        string expectedJson,
        Func<T, string> serialize,
        Func<string, T?> deserialize )
        where T : class
    {
        // Step 1: Serialize object to JSON
        var serializedJson = serialize( inputObject );
        this.Output.WriteLine( "Serialized JSON:" );
        this.Output.WriteLine( serializedJson );

        // Step 2: Compare to expected hardcoded JSON
        Assert.Equal( NormalizeJson( expectedJson ), NormalizeJson( serializedJson ) );

        // Step 3: Deserialize back to object
        var deserialized = deserialize( expectedJson );
        Assert.NotNull( deserialized );

        // Step 4: Serialize again
        var reserializedJson = serialize( deserialized );

        // Step 5: Compare JSON outputs match (round-trip consistency)
        Assert.Equal( NormalizeJson( serializedJson ), NormalizeJson( reserializedJson ) );
    }

    private static string NormalizeJson( string json )
    {
        // Normalize whitespace for comparison
        return JsonConvert.SerializeObject(
            JsonConvert.DeserializeObject( json ),
            Formatting.Indented );
    }
}
