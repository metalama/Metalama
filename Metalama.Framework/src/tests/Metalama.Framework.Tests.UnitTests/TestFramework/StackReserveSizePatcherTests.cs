// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.TestFramework;

/// <summary>
/// Tests <see cref="StackReserveSizePatcher"/>, which sets the stack reserve size of the .NET Framework executable of a test
/// project and signs it again.
/// </summary>
/// <remarks>
/// The signature is compared with the one that the C# compiler produces, which is deterministic because the padding of the
/// signature scheme is. The checksum is compared with the one that Windows computes, and, on .NET Framework, the signature
/// of the modified file is verified by the runtime.
/// </remarks>
public sealed class StackReserveSizePatcherTests : UnitTestClass
{
    private const long _stackReserveSize = 4 * 1024 * 1024;

    /// <summary>
    /// Verifies that the patcher computes the same signature as the C# compiler for the same content.
    /// </summary>
    [Fact]
    public void TrySign_ProducesTheSignatureOfTheCompiler()
    {
        using var testContext = this.CreateTestContext();
        var executable = CompileSignedExecutable( testContext );

        var layout = ReadLayout( executable.Image );
        Assert.True( layout.SignatureSize > 0 );

        var resigned = (byte[]) executable.Image.Clone();
        Array.Clear( resigned, layout.SignatureOffset, layout.SignatureSize );

        Assert.True( StackReserveSizePatcher.TrySign( resigned, executable.KeyPair, executable.PublicKey, out var error ), error );
        Assert.Equal( executable.Image, resigned );
    }

    /// <summary>
    /// Verifies that the patcher sets the stack reserve size, signs the file again and updates its checksum.
    /// </summary>
    [Fact]
    public void TryPatch_SetsTheStackReserveSize_SignsAgain_AndUpdatesTheChecksum()
    {
        using var testContext = this.CreateTestContext();
        var executable = CompileSignedExecutable( testContext );
        var layout = ReadLayout( executable.Image );

        // The compiler writes a checksum when it signs, so the checksum must be updated.
        Assert.NotEqual( 0u, BitConverter.ToUInt32( executable.Image, layout.ChecksumOffset ) );

        var patched = (byte[]) executable.Image.Clone();

        Assert.True(
            StackReserveSizePatcher.TryPatch( patched, _stackReserveSize, executable.KeyPair, executable.PublicKey, out var changed, out var error ),
            error );

        Assert.True( changed );
        Assert.Equal( (uint) _stackReserveSize, BitConverter.ToUInt32( patched, layout.StackReserveOffset ) );

        Assert.False(
            patched.Skip( layout.SignatureOffset ).Take( layout.SignatureSize ).SequenceEqual( executable.Image.Skip( layout.SignatureOffset ).Take( layout.SignatureSize ) ),
            "The signature was not computed again." );

        if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
        {
            Assert.Equal( GetWindowsChecksum( executable.Image ), BitConverter.ToUInt32( executable.Image, layout.ChecksumOffset ) );
            Assert.Equal( GetWindowsChecksum( patched ), BitConverter.ToUInt32( patched, layout.ChecksumOffset ) );
        }
    }

    /// <summary>
    /// Verifies that the .NET Framework runtime accepts the signature of a patched file and rejects the signature of a
    /// file that was patched without being signed again.
    /// </summary>
    /// <remarks>
    /// The file produced by the compiler is the control. When the runtime of the machine does not accept it, the machine
    /// cannot verify strong name signatures, and the test is skipped with the error that the runtime reported. This was
    /// observed on a build agent.
    /// </remarks>
    [Fact]
    public void TryPatch_SignatureIsAcceptedByTheNetFrameworkRuntime()
    {
        Assert.SkipUnless(
            RuntimeInformation.FrameworkDescription.StartsWith( ".NET Framework", StringComparison.Ordinal ),
            "The strong name verification API is available on .NET Framework only." );

        using var testContext = this.CreateTestContext();
        var executable = CompileSignedExecutable( testContext );

        Assert.SkipUnless(
            IsSignatureValidForTheRuntime( testContext, executable.Image, "original.exe", out var controlError ),
            $"The runtime does not accept the signature produced by the compiler: {controlError}" );

        var patched = (byte[]) executable.Image.Clone();

        Assert.True(
            StackReserveSizePatcher.TryPatch( patched, _stackReserveSize, executable.KeyPair, executable.PublicKey, out _, out var error ),
            error );

        Assert.True( IsSignatureValidForTheRuntime( testContext, patched, "patched.exe", out var patchedError ), patchedError );

        // The positive result above is meaningful only if the runtime rejects a file whose signature is stale.
        var unsigned = (byte[]) executable.Image.Clone();
        Assert.True( StackReserveSizePatcher.TryPatch( unsigned, _stackReserveSize, null, null, out _, out error ), error );
        Assert.False( IsSignatureValidForTheRuntime( testContext, unsigned, "stale.exe", out _ ) );
    }

    /// <summary>
    /// Verifies that the patcher does not modify a file whose stack reserve size already has the requested value, so that an
    /// incremental build does not rewrite the file.
    /// </summary>
    [Fact]
    public void TryPatch_WhenTheValueIsAlreadySet_DoesNotChangeTheFile()
    {
        using var testContext = this.CreateTestContext();
        var executable = CompileSignedExecutable( testContext );

        var patched = (byte[]) executable.Image.Clone();
        Assert.True( StackReserveSizePatcher.TryPatch( patched, _stackReserveSize, executable.KeyPair, executable.PublicKey, out var changed, out var error ), error );
        Assert.True( changed );

        var patchedAgain = (byte[]) patched.Clone();
        Assert.True( StackReserveSizePatcher.TryPatch( patchedAgain, _stackReserveSize, executable.KeyPair, executable.PublicKey, out changed, out error ), error );

        Assert.False( changed );
        Assert.Equal( patched, patchedAgain );
    }

    /// <summary>
    /// Verifies that the patcher rejects a file that is not a portable executable file.
    /// </summary>
    [Fact]
    public void TryPatch_WhenTheFileIsNotAnExecutable_Fails()
    {
        var notAnExecutable = new byte[256];

        Assert.False( StackReserveSizePatcher.TryPatch( notAnExecutable, _stackReserveSize, null, null, out var changed, out var error ) );
        Assert.False( changed );
        Assert.NotNull( error );
    }

    private static StackReserveSizePatcher.PortableExecutableLayout ReadLayout( byte[] image )
    {
        var layout = StackReserveSizePatcher.PortableExecutableLayout.TryRead( image, out var error );
        Assert.True( layout != null, error );

        return layout;
    }

    /// <summary>
    /// Compiles a console application signed with a new strong name key.
    /// </summary>
    private static (byte[] Image, byte[] KeyPair, byte[] PublicKey) CompileSignedExecutable( MetalamaTestContext testContext )
    {
        byte[] keyPair;

        using ( var rsa = new RSACryptoServiceProvider( 2048 ) )
        {
            keyPair = rsa.ExportCspBlob( true );
        }

        Directory.CreateDirectory( testContext.BaseDirectory );
        var keyFile = Path.Combine( testContext.BaseDirectory, "key.snk" );
        File.WriteAllBytes( keyFile, keyPair );

        var compilation = CSharpCompilation.Create(
            "SignedExecutable",
            [CSharpSyntaxTree.ParseText( "class Program { static void Main() { } }" )],
            [MetadataReference.CreateFromFile( typeof(object).Assembly.Location )],
            new CSharpCompilationOptions( OutputKind.ConsoleApplication )
                .WithCryptoKeyFile( keyFile )
                .WithStrongNameProvider( new DesktopStrongNameProvider() )
                .WithDeterministic( true ) );

        using var stream = new MemoryStream();
        var result = compilation.Emit( stream );

        Assert.True( result.Success, string.Join( Environment.NewLine, result.Diagnostics ) );

        return (stream.ToArray(), keyPair, compilation.Assembly.Identity.PublicKey.ToArray());
    }

    /// <summary>
    /// Computes the checksum of an image with the <c>CheckSumMappedFile</c> function of Windows.
    /// </summary>
    private static unsafe uint GetWindowsChecksum( byte[] image )
    {
        fixed ( byte* address = image )
        {
            Assert.NotEqual( IntPtr.Zero, CheckSumMappedFile( (IntPtr) address, (uint) image.Length, out _, out var checksum ) );

            return checksum;
        }
    }

    /// <summary>
    /// Verifies the strong name signature of an image with the .NET Framework runtime, which requires a file.
    /// </summary>
    /// <param name="testContext">The test context, whose directory receives the file.</param>
    /// <param name="image">The image to verify.</param>
    /// <param name="fileName">The name of the file to write.</param>
    /// <param name="error">Set to a description of the error that the runtime reported, when the signature is rejected.</param>
    private static bool IsSignatureValidForTheRuntime( MetalamaTestContext testContext, byte[] image, string fileName, out string? error )
    {
        var path = Path.Combine( testContext.BaseDirectory, fileName );
        File.WriteAllBytes( path, image );

        if ( StrongNameSignatureVerificationEx( path, true, out var wasVerified ) )
        {
            error = null;

            return true;
        }

        error = $"StrongNameErrorInfo returned 0x{StrongNameErrorInfo():X8} for '{path}' (wasVerified: {wasVerified}).";

        return false;
    }

    [DllImport( "imagehlp.dll", SetLastError = true )]
    private static extern IntPtr CheckSumMappedFile( IntPtr baseAddress, uint fileLength, out uint headerSum, out uint checkSum );

    [DllImport( "mscoree.dll", CharSet = CharSet.Unicode )]
    [return: MarshalAs( UnmanagedType.U1 )]
    private static extern bool StrongNameSignatureVerificationEx( string filePath, [MarshalAs( UnmanagedType.U1 )] bool forceVerification, [MarshalAs( UnmanagedType.U1 )] out bool wasVerified );

    [DllImport( "mscoree.dll" )]
    private static extern int StrongNameErrorInfo();
}
