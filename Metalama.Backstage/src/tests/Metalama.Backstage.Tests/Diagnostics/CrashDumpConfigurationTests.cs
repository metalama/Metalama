// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Serialization;
using Metalama.Backstage.Testing;
using System;
using System.Collections.Immutable;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Diagnostics;

/// <summary>
/// Regression tests for the <c>default</c> (uninitialized) <see cref="ImmutableArray{T}"/> that
/// <see cref="CrashDumpConfiguration.ExceptionTypes"/> could hold.
/// </summary>
/// <remarks>
/// See issue #1772. A <c>default(ImmutableArray&lt;T&gt;)</c> wraps a null backing array: it is not
/// <see cref="ImmutableArray{T}.Empty"/>, it throws an <see cref="InvalidOperationException"/> when it is enumerated,
/// and it throws a <see cref="NullReferenceException"/> from the source-generated
/// <c>ImmutableArrayStringSerializeHandler</c> when it is serialized. A property initializer alone did not prevent it:
/// the System.Text.Json source generator treats every <c>init</c> property as a constructor parameter and assigns all
/// of them unconditionally, so a <c>diagnostics.json</c> whose <c>crashDumps</c> section has no <c>exceptionTypes</c>
/// entry overwrote the initializer with the default value.
/// </remarks>
public sealed class CrashDumpConfigurationTests : TestsBase
{
    public CrashDumpConfigurationTests( ITestOutputHelper logger ) : base( logger ) { }

    private Metalama.Backstage.Configuration.ConfigurationManager CreateConfigurationManager() => new( this.ServiceProvider );

    /// <summary>
    /// Verifies that a default array explicitly assigned through an object initializer or a <c>with</c> expression is normalized.
    /// </summary>
    [Fact]
    public void ExceptionTypes_WhenSetToDefault_IsEmpty()
    {
        var configuration = new CrashDumpConfiguration { ExceptionTypes = default };

        Assert.False( configuration.ExceptionTypes.IsDefault );

        // This enumerates the array, which is what throws InvalidOperationException on a default instance.
        Assert.Empty( configuration.ExceptionTypes );

        var copy = configuration with { ExceptionTypes = default };

        Assert.False( copy.ExceptionTypes.IsDefault );
        Assert.Empty( copy.ExceptionTypes );
    }

    /// <summary>
    /// Verifies that a <c>diagnostics.json</c> without an <c>exceptionTypes</c> entry is read as an empty array and can be updated.
    /// </summary>
    /// <remarks>
    /// Such a file is what a fresh installation, an earlier version of Metalama, or a user running
    /// <c>metalama config edit diagnostics</c> leaves behind. <see cref="ConfigurationManager.Update{T}"/> compares the
    /// updated configuration with the cached one through <c>StructurallyEquals</c>, which serializes both, so merely
    /// reading and updating the file reaches the serializer.
    /// </remarks>
    [Theory]
    [InlineData( """{ "crashDumps": {} }""" )]
    [InlineData( """{ "crashDumps": { "exceptionTypes": null } }""" )]
    [InlineData( """{ "crashDumps": { "processes": { "Compiler": true } } }""" )]
    public void ReadConfigurationFile_WithoutExceptionTypes_YieldsEmptyArray( string json )
    {
        var configurationManager = this.CreateConfigurationManager();
        var filePath = configurationManager.GetFilePath( typeof(DiagnosticsConfiguration) );
        this.FileSystem.WriteAllText( filePath, json );

        var configuration = configurationManager.Get<DiagnosticsConfiguration>();

        Assert.False( configuration.CrashDumps.ExceptionTypes.IsDefault );
        Assert.Empty( configuration.CrashDumps.ExceptionTypes );

        configurationManager.Update<DiagnosticsConfiguration>(
            c => c with { Logging = new LoggingConfiguration { StopLoggingAfterHours = 4.5 } } );

        Assert.False( configurationManager.Get<DiagnosticsConfiguration>().CrashDumps.ExceptionTypes.IsDefault );
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
        var jsonSerializationService = this.ServiceProvider.GetRequiredBackstageService<IJsonSerializationService>();

        var json = jsonSerializationService.Serialize(
            new CrashDumpConfiguration { ExceptionTypes = default },
            typeof(CrashDumpConfiguration) );

        this.Logger.WriteLine( json );

        Assert.Contains( "\"exceptionTypes\": []", json, StringComparison.Ordinal );
    }
}
