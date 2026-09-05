// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

/// <summary>
/// Tests the language version at which the compile-time code of a reference is parsed and compiled. The manifest of
/// a compile-time project stores the language version as an integer, so it can carry a version that the Roslyn
/// variant of the current process does not accept, and that version must be clamped instead of being passed to
/// Roslyn. See issues #1928 and #1185.
/// </summary>
public sealed class CompileTimeProjectLanguageVersionTests : UnitTestClass
{
    private const string _referencedCode = """
                                           using Metalama.Framework.Aspects;

                                           [assembly: CompileTime]

                                           public class ReferencedClass
                                           {
                                               public static int GetValue() => 42;
                                           }
                                           """;

    private const string _referencingCode = "/* Intentionally empty. */";

    /// <summary>
    /// Verifies that a reference whose manifest carries a language version above the one that the Roslyn variant of
    /// the current process accepts is read with a clamped language version and a warning that names both versions,
    /// instead of failing the compile-time build with the Roslyn error <c>CS8192</c>.
    /// </summary>
    [Fact]
    public void LanguageVersionAboveTheHostIsClampedAndReported()
    {
        // The value 1500 is C# 15. It is not a member of LanguageVersion in the Roslyn versions that Metalama
        // consumes today, which is exactly the situation the clamp addresses: the manifest is written by a Roslyn
        // variant that knows the version and read by one that does not.
        const LanguageVersion writtenLanguageVersion = (LanguageVersion) 1500;

        using var producerContext = this.CreateTestContext();
        using var consumerContext = this.CreateTestContext();

        // Build the compile-time project of the reference and serialize it.
        var referencedCompilation = producerContext.CreateCSharpCompilation( _referencedCode );
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

        var rewrittenBytes = RewriteManifestLanguageVersion( originalBytes, writtenLanguageVersion );

        // Emit the reference with the rewritten compile-time project.
        var referencedPath = Path.Combine( consumerContext.BaseDirectory, "referenced.dll" );

        using ( var peStream = File.Create( referencedPath ) )
        {
            var emitResult = referencedCompilation.Emit(
                peStream,
                manifestResources:
                [
                    new ManagedResource( CompileTimeConstants.CompileTimeProjectResourceName, rewrittenBytes, true ).Resource
                ] );

            Assert.True( emitResult.Success );
        }

        // Read the reference from a repository that has not seen the project before, so that it goes through the
        // deserialization path.
        var referencingCompilation = consumerContext.CreateCSharpCompilation(
            _referencingCode,
            additionalReferences: [MetadataReference.CreateFromFile( referencedPath )] );

        var consumerRepositoryBuilder = new CompileTimeProjectRepository.Builder( consumerContext.Domain, consumerContext.ServiceProvider );
        DiagnosticBag consumerDiagnostics = new();

        Assert.True(
            consumerRepositoryBuilder.TryGetCompileTimeProjectFromCompilation(
                referencingCompilation,
                null,
                consumerDiagnostics,
                false,
                consumerContext.CancellationToken,
                out var referencingProject ),
            FormatDiagnostics( consumerDiagnostics ) );

        Assert.NotNull( referencingProject );

        // The compile-time build must not fail, and in particular must not report the Roslyn error that names an
        // invalid language version.
        Assert.DoesNotContain( consumerDiagnostics, d => d.Severity == DiagnosticSeverity.Error );

        // The warning must name the reference, the version it requires and the version the host supports.
        var warning = Assert.Single( consumerDiagnostics, d => d.Id == "LAMA0088" );
        var message = warning.GetMessage( CultureInfo.InvariantCulture );

        Assert.Contains( referencedCompilation.AssemblyName!, message, StringComparison.Ordinal );
        Assert.Contains( "15.0", message, StringComparison.Ordinal );
    }

    private static string FormatDiagnostics( DiagnosticBag bag )
        => string.Join( "\n  ", bag.SelectAsArray( d => d.GetMessage( CultureInfo.InvariantCulture ) ) );

    /// <summary>
    /// Returns a copy of a serialized compile-time project whose manifest carries the given language version. This
    /// stands in for a project produced by a Roslyn variant that supports a language version the current one does
    /// not, which is not otherwise reproducible in a single build.
    /// </summary>
    private static byte[] RewriteManifestLanguageVersion( byte[] resourceBytes, LanguageVersion languageVersion )
    {
        using var inputStream = new MemoryStream( resourceBytes );
        using var inputArchive = new ZipArchive( inputStream, ZipArchiveMode.Read );

        var outputStream = new MemoryStream();

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
                    var manifest = CompileTimeProjectManifest.FromJson( manifestReader.ReadToEnd() );
                    manifest.LanguageVersion = languageVersion;

                    using var manifestWriter = new StreamWriter( outputEntryStream, Encoding.UTF8 );
                    manifestWriter.Write( manifest.ToJson() );
                }
                else
                {
                    inputEntryStream.CopyTo( outputEntryStream );
                }
            }
        }

        return outputStream.ToArray();
    }
}
