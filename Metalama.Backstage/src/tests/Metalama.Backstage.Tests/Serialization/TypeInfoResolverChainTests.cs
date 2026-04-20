// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Serialization;

/// <summary>
/// Tests for the TypeInfoResolverChain mechanism that allows combining multiple JsonSerializerContext instances.
/// </summary>
public sealed partial class TypeInfoResolverChainTests
{
    private readonly ITestOutputHelper _output;

    public TypeInfoResolverChainTests( ITestOutputHelper output )
    {
        this._output = output;
    }

    /// <summary>
    /// A test configuration type that is NOT registered in BackstageJsonContext.
    /// </summary>
    [ConfigurationFile( "testExternal.json" )]
    public record TestExternalConfiguration : ConfigurationFile
    {
        public string Name { get; init; } = "";

        public int Value { get; init; }
    }

    /// <summary>
    /// A test JsonSerializerContext for external types.
    /// </summary>
    [JsonSerializable( typeof(TestExternalConfiguration) )]
    internal partial class TestExternalJsonContext : JsonSerializerContext { }

    [Fact]
    public void CreateCombinedOptions_WithNoAdditionalResolvers_UsesBackstageContext()
    {
        // Arrange & Act
        var options = BackstageJsonContext.CreateCombinedOptions( writeIndented: true, additionalResolvers: [] );

        // Assert - BackstageJsonContext types should be resolvable
        var typeInfo = options.GetTypeInfo( typeof(LoggingConfiguration) );
        Assert.NotNull( typeInfo );
    }

    [Fact]
    public void CreateCombinedOptions_WithAdditionalResolver_ResolvesExternalType()
    {
        // Arrange
        var additionalResolvers = new List<IJsonTypeInfoResolver> { TestExternalJsonContext.Default };
        var options = BackstageJsonContext.CreateCombinedOptions( writeIndented: true, additionalResolvers );

        // Act - should find type from external context
        var typeInfo = options.GetTypeInfo( typeof(TestExternalConfiguration) );

        // Assert
        Assert.NotNull( typeInfo );
    }

    [Fact]
    public void CreateCombinedOptions_WithAdditionalResolver_StillResolvesBackstageTypes()
    {
        // Arrange
        var additionalResolvers = new List<IJsonTypeInfoResolver> { TestExternalJsonContext.Default };
        var options = BackstageJsonContext.CreateCombinedOptions( writeIndented: true, additionalResolvers );

        // Act - should still find BackstageJsonContext types
        var typeInfo = options.GetTypeInfo( typeof(LoggingConfiguration) );

        // Assert
        Assert.NotNull( typeInfo );
    }

    [Fact]
    public void CreateCombinedOptions_UnregisteredType_ThrowsNotSupportedException()
    {
        // Arrange - use only BackstageJsonContext, no external resolver
        var options = BackstageJsonContext.CreateCombinedOptions( writeIndented: true, additionalResolvers: [] );

        // Act & Assert - TestExternalConfiguration is NOT registered in BackstageJsonContext
        // System.Text.Json throws NotSupportedException for unregistered types
        Assert.Throws<NotSupportedException>( () => options.GetTypeInfo( typeof(TestExternalConfiguration) ) );
    }

    [Fact]
    public void SerializeAndDeserialize_WithCombinedOptions_WorksForExternalType()
    {
        // Arrange
        var additionalResolvers = new List<IJsonTypeInfoResolver> { TestExternalJsonContext.Default };
        var options = BackstageJsonContext.CreateCombinedOptions( writeIndented: true, additionalResolvers );

        var input = new TestExternalConfiguration { Name = "Test", Value = 42, Version = 1 };

        // Act
        var typeInfo = options.GetTypeInfo( typeof(TestExternalConfiguration) );
        Assert.NotNull( typeInfo );

        var json = JsonSerializer.Serialize( input, typeInfo );
        this._output.WriteLine( $"Serialized: {json}" );

        var deserialized = (TestExternalConfiguration?) JsonSerializer.Deserialize( json, typeInfo );

        // Assert
        Assert.NotNull( deserialized );
        Assert.Equal( "Test", deserialized.Name );
        Assert.Equal( 42, deserialized.Value );
        Assert.Equal( 1, deserialized.Version );
    }

    [Fact]
    public void SerializeAndDeserialize_WithCombinedOptions_WorksForBackstageType()
    {
        // Arrange
        var additionalResolvers = new List<IJsonTypeInfoResolver> { TestExternalJsonContext.Default };
        var options = BackstageJsonContext.CreateCombinedOptions( writeIndented: true, additionalResolvers );

        var input = new LoggingConfiguration
        {
            Processes = ImmutableDictionary<string, bool>.Empty
                .WithComparers( StringComparer.OrdinalIgnoreCase )
                .Add( "Test", true )
        };

        // Act
        var typeInfo = options.GetTypeInfo( typeof(LoggingConfiguration) );
        Assert.NotNull( typeInfo );

        var json = JsonSerializer.Serialize( input, typeInfo );
        this._output.WriteLine( $"Serialized: {json}" );

        var deserialized = (LoggingConfiguration?) JsonSerializer.Deserialize( json, typeInfo );

        // Assert
        Assert.NotNull( deserialized );
        Assert.True( deserialized.Processes.ContainsKey( "Test" ) );
        Assert.True( deserialized.Processes.ContainsKey( "test" ) ); // Case-insensitive
    }

    [Fact]
    public void JsonSerializationService_ThrowsForUnregisteredType_WhenNoResolver()
    {
        // Arrange - Create service with no additional resolvers
        var service = new JsonSerializationService( [] );

        // Act & Assert - should throw for unregistered type
        // System.Text.Json throws NotSupportedException before our check
        var ex = Assert.Throws<NotSupportedException>(
            () => service.Serialize( new TestExternalConfiguration { Name = "Test" }, typeof(TestExternalConfiguration) ) );

        this._output.WriteLine( $"Exception message: {ex.Message}" );
        Assert.Contains( "was not provided", ex.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void JsonSerializationService_WorksForExternalType_WhenResolverProvided()
    {
        // Arrange - Create service with external resolver
        var additionalResolvers = new List<IJsonTypeInfoResolver> { TestExternalJsonContext.Default };
        var service = new JsonSerializationService( additionalResolvers );

        var input = new TestExternalConfiguration { Name = "Test", Value = 42, Version = 1 };

        // Act
        var json = service.Serialize( input, typeof(TestExternalConfiguration) );
        this._output.WriteLine( $"Serialized: {json}" );

        var success = service.TryDeserialize<TestExternalConfiguration>( json, out var deserialized );

        // Assert
        Assert.True( success );
        Assert.NotNull( deserialized );
        Assert.Equal( "Test", deserialized.Name );
        Assert.Equal( 42, deserialized.Value );
    }
}