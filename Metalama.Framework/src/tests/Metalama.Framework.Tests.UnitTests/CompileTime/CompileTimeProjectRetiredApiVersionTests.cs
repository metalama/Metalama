// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

/// <summary>
/// Tests the loading of a reference whose compile-time project manifest names a member of <see cref="RoslynApiVersion"/>
/// that was removed from the enumeration. A retired version is removed and not renumbered, so the manifest of a
/// package built by an earlier preview can contain a name that the current version does not know. See issue #2400.
/// </summary>
public sealed class CompileTimeProjectRetiredApiVersionTests : UnitTestClass
{
    /// <summary>
    /// The code of the reference. The template uses a collection expression, which requires a Roslyn version above the
    /// lowest one, so the manifest stores a <c>usedApiVersion</c> property for the template.
    /// </summary>
    private const string _referencedCode = """
                                           using Metalama.Framework.Aspects;

                                           public class MyAspect : OverrideMethodAspect
                                           {
                                               public override dynamic? OverrideMethod()
                                               {
                                                   int[] array = [1, 2];
                                                   return meta.Proceed();
                                               }
                                           }
                                           """;

    private const string _referencingCode = "/* Intentionally empty. */";

    /// <summary>
    /// Verifies that a reference whose manifest names the retired Roslyn API version <c>5.11.0</c> is reported with the
    /// <c>DependencyMustBeRecompiled</c> diagnostic, which names the reference, instead of failing with an exception
    /// thrown by the deserializer.
    /// </summary>
    [Theory]
    [InlineData( "5.11.0" )]
    [InlineData( "5.10.0" )]
    public void RetiredApiVersionReportsDependencyMustBeRecompiled( string retiredApiVersion )
    {
        using var producerContext = this.CreateTestContext();
        using var consumerContext = this.CreateTestContext();

        // Build the compile-time project of the reference and serialize it.
        var referencedCompilation = producerContext.CreateCSharpCompilation( _referencedCode, assemblyName: "ReferencedAssembly" );
        var producerRepositoryBuilder = new CompileTimeProjectRepository.Builder( producerContext.Domain, producerContext.ServiceProvider );
        DiagnosticBag producerDiagnostics = new();

        Assert.True(
            producerRepositoryBuilder.TryGetCompileTimeProjectFromCompilation(
                referencedCompilation,
                null,
                producerDiagnostics,
                false,
                producerContext.CancellationToken,
                out var referencedProject ),
            FormatDiagnostics( producerDiagnostics ) );

        Assert.NotNull( referencedProject );

        var resource = referencedProject.ToResource();

        byte[] originalBytes;

        using ( var resourceStream = resource.DataProvider.AssertNotNull()() )
        {
            using var buffer = new MemoryStream();
            resourceStream.CopyTo( buffer );
            originalBytes = buffer.ToArray();
        }

        var rewrittenBytes = RewriteManifestUsedApiVersion( originalBytes, retiredApiVersion );

        // Emit the reference with the rewritten compile-time project.
        var referencedPath = Path.Combine( consumerContext.BaseDirectory, "ReferencedAssembly.dll" );

        using ( var peStream = File.Create( referencedPath ) )
        {
            var emitResult = referencedCompilation.Emit(
                peStream,
                manifestResources:
                [
                    new ManagedResource( CompileTimeConstants.CompileTimeProjectResourceName, rewrittenBytes, true ).Resource
                ],
                cancellationToken: consumerContext.CancellationToken );

            Assert.True( emitResult.Success );
        }

        // Read the reference from a repository that has not seen the project before, so that it goes through the
        // deserialization path.
        var referencingCompilation = consumerContext.CreateCSharpCompilation(
            _referencingCode,
            additionalReferences: [MetadataReference.CreateFromFile( referencedPath )] );

        var consumerRepositoryBuilder = new CompileTimeProjectRepository.Builder( consumerContext.Domain, consumerContext.ServiceProvider );
        DiagnosticBag consumerDiagnostics = new();

        var success = consumerRepositoryBuilder.TryGetCompileTimeProjectFromCompilation(
            referencingCompilation,
            null,
            consumerDiagnostics,
            false,
            consumerContext.CancellationToken,
            out _ );

        Assert.False( success );

        var diagnostic = Assert.Single( consumerDiagnostics, d => d.Id == GeneralDiagnosticDescriptors.DependencyMustBeRecompiled.Id );
        var message = diagnostic.GetMessage( CultureInfo.InvariantCulture );

        Assert.Contains( "ReferencedAssembly", message, StringComparison.Ordinal );
    }

    private static string FormatDiagnostics( DiagnosticBag bag )
        => string.Join( "\n  ", bag.SelectAsArray( d => d.GetMessage( CultureInfo.InvariantCulture ) ) );

    /// <summary>
    /// Returns a copy of a serialized compile-time project whose manifest names the given Roslyn API version in every
    /// <c>usedApiVersion</c> property. This stands in for a project produced by an earlier Metalama version, which is
    /// not otherwise reproducible in a single build.
    /// </summary>
    private static byte[] RewriteManifestUsedApiVersion( byte[] resourceBytes, string apiVersion )
    {
        using var inputStream = new MemoryStream( resourceBytes );
        using var inputArchive = new ZipArchive( inputStream, ZipArchiveMode.Read );

        var outputStream = new MemoryStream();
        var replacementCount = 0;

        using ( var outputArchive = new ZipArchive( outputStream, ZipArchiveMode.Create, true ) )
        {
            foreach ( var inputEntry in inputArchive.Entries )
            {
                using var inputEntryStream = inputEntry.Open();
                var outputEntry = outputArchive.CreateEntry( inputEntry.FullName, CompressionLevel.Optimal );
                using var outputEntryStream = outputEntry.Open();

                if ( string.Equals( inputEntry.FullName, "manifest.json", StringComparison.Ordinal ) )
                {
                    using var manifestReader = new StreamReader( inputEntryStream, Encoding.UTF8 );
                    var manifestNode = JsonNode.Parse( manifestReader.ReadToEnd() ).AssertNotNull();

                    replacementCount += ReplaceUsedApiVersion( manifestNode, apiVersion );

                    using var manifestWriter = new StreamWriter( outputEntryStream, Encoding.UTF8 );
                    manifestWriter.Write( manifestNode.ToJsonString() );
                }
                else
                {
                    inputEntryStream.CopyTo( outputEntryStream );
                }
            }
        }

        // If the manifest does not contain the property, the test would not exercise the deserialization of a
        // retired version.
        Assert.True( replacementCount > 0, "The manifest does not contain any usedApiVersion property." );

        return outputStream.ToArray();
    }

    /// <summary>
    /// Sets every <c>usedApiVersion</c> property under the given node to the given value, and returns the number of
    /// properties that were set.
    /// </summary>
    private static int ReplaceUsedApiVersion( JsonNode node, string apiVersion )
    {
        var count = 0;

        switch ( node )
        {
            case JsonObject jsonObject:
                foreach ( var property in jsonObject.ToList() )
                {
                    if ( string.Equals( property.Key, "usedApiVersion", StringComparison.Ordinal ) )
                    {
                        jsonObject[property.Key] = apiVersion;
                        count++;
                    }
                    else if ( property.Value != null )
                    {
                        count += ReplaceUsedApiVersion( property.Value, apiVersion );
                    }
                }

                break;

            case JsonArray jsonArray:
                foreach ( var item in jsonArray )
                {
                    if ( item != null )
                    {
                        count += ReplaceUsedApiVersion( item, apiVersion );
                    }
                }

                break;
        }

        return count;
    }
}
