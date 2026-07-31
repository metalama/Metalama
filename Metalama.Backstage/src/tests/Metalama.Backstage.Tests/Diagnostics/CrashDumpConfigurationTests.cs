// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Tests.Serialization;
using System;
using System.Collections.Immutable;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Diagnostics;

/// <summary>
/// Regression tests for the <c>default</c> (uninitialized) <see cref="ImmutableArray{T}"/> that
/// <see cref="CrashDumpConfiguration.ExceptionTypes"/> could hold.
/// </summary>
/// <remarks>
/// See issue #1772. A <c>default(ImmutableArray&lt;T&gt;)</c> wraps a null backing array: it is not
/// <see cref="ImmutableArray{T}.Empty"/>, it throws when it is enumerated, and it throws a
/// <see cref="NullReferenceException"/> from the source-generated <c>ImmutableArrayStringSerializeHandler</c> when it
/// is serialized. A property initializer alone did not prevent it: the System.Text.Json source generator treats every
/// <c>init</c> property as a constructor parameter and assigns all of them unconditionally, so JSON without an
/// <c>exceptionTypes</c> entry overwrote the initializer with the default value.
/// </remarks>
public sealed class CrashDumpConfigurationTests : JsonSerializationTestsBase
{
    public CrashDumpConfigurationTests( ITestOutputHelper output ) : base( output ) { }

    /// <summary>
    /// Verifies that a default array assigned through an object initializer or a <c>with</c> expression is normalized.
    /// </summary>
    /// <remarks>
    /// These are the construction paths of the deserializer itself, which assigns the property unconditionally.
    /// </remarks>
    [Fact]
    public void ExceptionTypes_WhenSetToDefault_IsEmpty()
    {
        var configuration = new CrashDumpConfiguration { ExceptionTypes = default };

        Assert.False( configuration.ExceptionTypes.IsDefault );

        // This enumerates the array, which is what throws on a default instance.
        Assert.Empty( configuration.ExceptionTypes );

        var copy = configuration with { ExceptionTypes = default };

        Assert.False( copy.ExceptionTypes.IsDefault );
        Assert.Empty( copy.ExceptionTypes );
    }

    /// <summary>
    /// Verifies that a non-default value is preserved, so that the normalization does not lose the configured types.
    /// </summary>
    [Fact]
    public void ExceptionTypes_WhenSet_IsPreserved()
    {
        var configuration = new CrashDumpConfiguration { ExceptionTypes = ImmutableArray.Create( "System.InvalidOperationException" ) };

        Assert.Equal( "System.InvalidOperationException", Assert.Single( configuration.ExceptionTypes ) );
    }

    /// <summary>
    /// Verifies that JSON without an <c>exceptionTypes</c> entry is deserialized into an empty array and can be serialized back.
    /// </summary>
    /// <remarks>
    /// The reserialization matters because <c>ConfigurationManager.StructurallyEquals</c> serializes configuration
    /// objects in order to compare them, so merely reading a configuration file reaches the serializer.
    /// </remarks>
    [Theory]
    [InlineData( "{}" )]
    [InlineData( """{ "processes": { "Compiler": true } }""" )]
    public void Deserialize_WithoutExceptionTypes_YieldsEmptyArray( string json )
    {
        var configuration = JsonSerializer.Deserialize<CrashDumpConfiguration>( json, this.JsonOptions )!;

        Assert.False( configuration.ExceptionTypes.IsDefault );
        Assert.Empty( configuration.ExceptionTypes );

        var reserialized = JsonSerializer.Serialize( configuration, this.JsonOptions );

        this.Output.WriteLine( reserialized );

        Assert.Contains( "\"exceptionTypes\": []", reserialized, StringComparison.Ordinal );
    }

    /// <summary>
    /// Verifies that serializing a configuration whose <see cref="CrashDumpConfiguration.ExceptionTypes"/> is a default array succeeds.
    /// </summary>
    /// <remarks>
    /// The source-generated <c>ImmutableArrayStringSerializeHandler</c> dereferences the backing array without checking
    /// <see cref="ImmutableArray{T}.IsDefault"/>, so it throws a bare <see cref="NullReferenceException"/>.
    /// </remarks>
    [Fact]
    public void Serialize_WithDefaultExceptionTypes_DoesNotThrow()
    {
        var json = this.JsonService.Serialize( new CrashDumpConfiguration { ExceptionTypes = default }, typeof(CrashDumpConfiguration) );

        this.Output.WriteLine( json );

        Assert.Contains( "\"exceptionTypes\": []", json, StringComparison.Ordinal );
    }
}
