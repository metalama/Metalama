// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// This file is not compiled into Metalama.Testing.AspectTesting. RoslynCodeTaskFactory compiles it when a .NET Framework
// test project is built (see Metalama.Testing.AspectTesting.targets), and the unit tests of Metalama compile it with the
// METALAMA_STACK_RESERVE_SIZE_TESTS symbol, which excludes the MSBuild task.

#nullable enable

using System;
using System.Security.Cryptography;
#if !METALAMA_STACK_RESERVE_SIZE_TESTS
using System.IO;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
#endif

#if !METALAMA_STACK_RESERVE_SIZE_TESTS

// ReSharper disable once CheckNamespace
#pragma warning disable CA1050 // RoslynCodeTaskFactory locates the task by its name.

/// <summary>
/// An MSBuild task that sets the <c>SizeOfStackReserve</c> field of the header of an executable, and signs the executable
/// again when it has a strong name.
/// </summary>
public sealed class MetalamaSetStackReserveSize : Task
{
    /// <summary>
    /// Gets or sets the path of the executable.
    /// </summary>
    [Required]
    public string AssemblyPath { get; set; } = "";

    /// <summary>
    /// Gets or sets the size of the stack that the executable reserves for each thread, in bytes.
    /// </summary>
    [Required]
    public long StackReserveSize { get; set; }

    /// <summary>
    /// Gets or sets the value of the <c>SignAssembly</c> property of the project.
    /// </summary>
    public bool SignAssembly { get; set; }

    /// <summary>
    /// Gets or sets the value of the <c>DelaySign</c> property of the project.
    /// </summary>
    public bool DelaySign { get; set; }

    /// <summary>
    /// Gets or sets the value of the <c>PublicSign</c> property of the project.
    /// </summary>
    public bool PublicSign { get; set; }

    /// <summary>
    /// Gets or sets the path of the strong name key file of the project, or an empty string.
    /// </summary>
    public string KeyFile { get; set; } = "";

    /// <inheritdoc />
    public override bool Execute()
    {
        byte[]? keyPair = null;
        byte[]? publicKey = null;

        // A delay-signed or public-signed assembly has no valid signature before the change, so it does not need one after.
        if ( this.SignAssembly && !this.DelaySign && !this.PublicSign )
        {
            if ( string.IsNullOrEmpty( this.KeyFile ) || !File.Exists( this.KeyFile ) )
            {
                this.Log.LogWarning(
                    "The stack reserve size of '{0}' is not changed, because the assembly has a strong name and no key file is available to sign it again.",
                    this.AssemblyPath );

                return true;
            }

            keyPair = File.ReadAllBytes( this.KeyFile );
            publicKey = AssemblyName.GetAssemblyName( this.AssemblyPath ).GetPublicKey();
        }

        var image = File.ReadAllBytes( this.AssemblyPath );

        if ( !StackReserveSizePatcher.TryPatch( image, this.StackReserveSize, keyPair, publicKey, out var changed, out var error ) )
        {
            this.Log.LogError( "Cannot change the stack reserve size of '{0}': {1}", this.AssemblyPath, error );

            return false;
        }

        // The file is written only when the value differs, so that an incremental build does not update it.
        if ( changed )
        {
            File.WriteAllBytes( this.AssemblyPath, image );
        }

        return true;
    }
}

#pragma warning restore CA1050
#endif

/// <summary>
/// Sets the <c>SizeOfStackReserve</c> field of the header of a portable executable file, signs the file again when it has
/// a strong name, and updates its checksum.
/// </summary>
/// <remarks>
/// <para>
/// The strong name signature covers the headers of the file, so changing a field invalidates it. The hash that the
/// signature covers is computed in the same way as the C# compiler and the .NET Framework runtime compute it: the bytes
/// that precede the NT headers, the NT headers with the checksum and the certificate table entry set to zero, the section
/// headers, and the raw data of each section in the order of the section table, without the signature blob.
/// </para>
/// <para>
/// The checksum does not contribute to the hash, so it is computed after the signature.
/// </para>
/// </remarks>
internal static class StackReserveSizePatcher
{
    private const int _sectionHeaderSize = 40;

    /// <summary>
    /// Sets the <c>SizeOfStackReserve</c> field of a portable executable image.
    /// </summary>
    /// <param name="image">The content of the file, modified in place.</param>
    /// <param name="stackReserveSize">The new value of the field.</param>
    /// <param name="keyPair">The strong name key pair (the content of an <c>.snk</c> file) to sign the image again with,
    /// or <c>null</c> when the image must not be signed.</param>
    /// <param name="publicKey">The public key of the assembly, which gives the hash algorithm of the signature. It is
    /// required when <paramref name="keyPair"/> is not <c>null</c>.</param>
    /// <param name="changed">Set to <c>true</c> when the image was modified.</param>
    /// <param name="error">Set to a description of the problem when the method returns <c>false</c>.</param>
    public static bool TryPatch( byte[] image, long stackReserveSize, byte[]? keyPair, byte[]? publicKey, out bool changed, out string? error )
    {
        changed = false;

        var layout = PortableExecutableLayout.TryRead( image, out error );

        if ( layout == null )
        {
            return false;
        }

        if ( layout.IsPe32Plus )
        {
            if ( ReadUInt64( image, layout.StackReserveOffset ) == (ulong) stackReserveSize )
            {
                return true;
            }

            WriteUInt64( image, layout.StackReserveOffset, (ulong) stackReserveSize );
        }
        else
        {
            if ( ReadUInt32( image, layout.StackReserveOffset ) == (uint) stackReserveSize )
            {
                return true;
            }

            WriteUInt32( image, layout.StackReserveOffset, (uint) stackReserveSize );
        }

        if ( keyPair != null && layout.SignatureSize > 0 )
        {
            if ( publicKey == null )
            {
                error = "The public key of the assembly is required to sign it.";

                return false;
            }

            if ( !TrySign( image, layout, keyPair, publicKey, out error ) )
            {
                return false;
            }
        }

        if ( ReadUInt32( image, layout.ChecksumOffset ) != 0 )
        {
            WriteUInt32( image, layout.ChecksumOffset, ComputeChecksum( image, layout.ChecksumOffset ) );
        }

        changed = true;

        return true;
    }

    /// <summary>
    /// Computes the strong name signature of an image and writes it into the image.
    /// </summary>
    public static bool TrySign( byte[] image, byte[] keyPair, byte[] publicKey, out string? error )
    {
        var layout = PortableExecutableLayout.TryRead( image, out error );

        return layout != null && TrySign( image, layout, keyPair, publicKey, out error );
    }

    private static bool TrySign( byte[] image, PortableExecutableLayout layout, byte[] keyPair, byte[] publicKey, out string? error )
    {
        if ( layout.SignatureOffset < 0 || layout.SignatureSize <= 0 )
        {
            error = "The image has no strong name signature blob.";

            return false;
        }

        var hashAlgorithm = GetSignatureHashAlgorithm( publicKey );
        var hash = ComputeStrongNameHash( image, layout, hashAlgorithm );

        RSAParameters parameters;

        using ( var cryptoServiceProvider = new RSACryptoServiceProvider() )
        {
            cryptoServiceProvider.ImportCspBlob( keyPair );
            parameters = cryptoServiceProvider.ExportParameters( true );
        }

        byte[] signature;

        using ( var rsa = RSA.Create() )
        {
            rsa.ImportParameters( parameters );
            signature = rsa.SignHash( hash, hashAlgorithm, RSASignaturePadding.Pkcs1 );
        }

        // The signature blob stores the signature in little-endian byte order.
        Array.Reverse( signature );

        if ( signature.Length != layout.SignatureSize )
        {
            error = $"The signature has {signature.Length} bytes, but the image reserves {layout.SignatureSize} bytes for it.";

            return false;
        }

        Buffer.BlockCopy( signature, 0, image, layout.SignatureOffset, signature.Length );
        error = null;

        return true;
    }

    /// <summary>
    /// Gets the hash algorithm of the strong name signature from the header of a public key blob.
    /// </summary>
    private static HashAlgorithmName GetSignatureHashAlgorithm( byte[] publicKey )
    {
        // A public key blob starts with the signature algorithm identifier and the hash algorithm identifier.
        var hashAlgorithmId = publicKey.Length >= 8 ? ReadUInt32( publicKey, 4 ) : 0;

        return hashAlgorithmId switch
        {
            0x800C => HashAlgorithmName.SHA256,
            0x800D => HashAlgorithmName.SHA384,
            0x800E => HashAlgorithmName.SHA512,
            _ => HashAlgorithmName.SHA1
        };
    }

    /// <summary>
    /// Computes the hash that the strong name signature of an image covers.
    /// </summary>
    private static byte[] ComputeStrongNameHash( byte[] image, PortableExecutableLayout layout, HashAlgorithmName hashAlgorithm )
    {
        using var hash = IncrementalHash.CreateHash( hashAlgorithm );

        // The bytes that precede the NT headers: the DOS header and the DOS stub.
        hash.AppendData( image, 0, layout.PeHeaderOffset );

        // The NT headers, with the checksum and the certificate table entry set to zero.
        var headers = new byte[layout.SectionTableOffset - layout.PeHeaderOffset];
        Buffer.BlockCopy( image, layout.PeHeaderOffset, headers, 0, headers.Length );
        Array.Clear( headers, layout.ChecksumOffset - layout.PeHeaderOffset, 4 );
        Array.Clear( headers, layout.CertificateTableEntryOffset - layout.PeHeaderOffset, 8 );
        hash.AppendData( headers );

        // The section headers.
        hash.AppendData( image, layout.SectionTableOffset, layout.SectionCount * _sectionHeaderSize );

        // The raw data of each section, without the signature blob.
        var signatureStart = layout.SignatureOffset;
        var signatureEnd = layout.SignatureOffset + layout.SignatureSize;

        for ( var i = 0; i < layout.SectionCount; i++ )
        {
            var sectionHeader = layout.SectionTableOffset + (i * _sectionHeaderSize);
            var start = (int) ReadUInt32( image, sectionHeader + 20 );
            var end = start + (int) ReadUInt32( image, sectionHeader + 16 );

            if ( signatureStart >= start && signatureEnd <= end )
            {
                hash.AppendData( image, start, signatureStart - start );
                hash.AppendData( image, signatureEnd, end - signatureEnd );
            }
            else
            {
                hash.AppendData( image, start, end - start );
            }
        }

        return hash.GetHashAndReset();
    }

    /// <summary>
    /// Computes the checksum of a portable executable image, as the <c>CheckSumMappedFile</c> function of Windows does.
    /// </summary>
    /// <param name="image">The image.</param>
    /// <param name="checksumOffset">The offset of the checksum field, which does not contribute to the checksum.</param>
    public static uint ComputeChecksum( byte[] image, int checksumOffset )
    {
        ulong sum = 0;

        for ( var i = 0; i < image.Length; i += 2 )
        {
            if ( i == checksumOffset || i == checksumOffset + 2 )
            {
                continue;
            }

            uint word = image[i];

            if ( i + 1 < image.Length )
            {
                word |= (uint) image[i + 1] << 8;
            }

            sum += word;
            sum = (sum & 0xFFFF) + (sum >> 16);
        }

        sum = (sum & 0xFFFF) + (sum >> 16);

        return (uint) (sum + (ulong) image.Length);
    }

    private static uint ReadUInt32( byte[] buffer, int offset ) => BitConverter.ToUInt32( buffer, offset );

    private static ulong ReadUInt64( byte[] buffer, int offset ) => BitConverter.ToUInt64( buffer, offset );

    private static void WriteUInt32( byte[] buffer, int offset, uint value ) => Buffer.BlockCopy( BitConverter.GetBytes( value ), 0, buffer, offset, 4 );

    private static void WriteUInt64( byte[] buffer, int offset, ulong value ) => Buffer.BlockCopy( BitConverter.GetBytes( value ), 0, buffer, offset, 8 );

    /// <summary>
    /// The offsets of the parts of a portable executable image that <see cref="StackReserveSizePatcher"/> reads or writes.
    /// </summary>
    internal sealed class PortableExecutableLayout
    {
        private PortableExecutableLayout() { }

        /// <summary>
        /// Gets the offset of the NT headers, which start with the <c>PE</c> signature.
        /// </summary>
        public int PeHeaderOffset { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the optional header has the PE32+ format.
        /// </summary>
        public bool IsPe32Plus { get; private set; }

        /// <summary>
        /// Gets the offset of the checksum field.
        /// </summary>
        public int ChecksumOffset { get; private set; }

        /// <summary>
        /// Gets the offset of the <c>SizeOfStackReserve</c> field.
        /// </summary>
        public int StackReserveOffset { get; private set; }

        /// <summary>
        /// Gets the offset of the certificate table entry of the data directories.
        /// </summary>
        public int CertificateTableEntryOffset { get; private set; }

        /// <summary>
        /// Gets the offset of the section table.
        /// </summary>
        public int SectionTableOffset { get; private set; }

        /// <summary>
        /// Gets the number of sections.
        /// </summary>
        public int SectionCount { get; private set; }

        /// <summary>
        /// Gets the offset of the strong name signature blob, or -1 when the image has none.
        /// </summary>
        public int SignatureOffset { get; private set; } = -1;

        /// <summary>
        /// Gets the size of the strong name signature blob, or zero when the image has none.
        /// </summary>
        public int SignatureSize { get; private set; }

        /// <summary>
        /// Reads the layout of an image.
        /// </summary>
        public static PortableExecutableLayout? TryRead( byte[] image, out string? error )
        {
            if ( image.Length < 0x40 || image[0] != (byte) 'M' || image[1] != (byte) 'Z' )
            {
                error = "The file is not a portable executable file.";

                return null;
            }

            var peHeaderOffset = BitConverter.ToInt32( image, 0x3C );

            if ( peHeaderOffset <= 0 || peHeaderOffset + 24 > image.Length || ReadUInt32( image, peHeaderOffset ) != 0x00004550 )
            {
                error = "The file is not a portable executable file.";

                return null;
            }

            var optionalHeaderOffset = peHeaderOffset + 24;
            var magic = BitConverter.ToUInt16( image, optionalHeaderOffset );

            if ( magic != 0x10B && magic != 0x20B )
            {
                error = "The file has an unknown optional header format.";

                return null;
            }

            var isPe32Plus = magic == 0x20B;
            var dataDirectoriesOffset = optionalHeaderOffset + (isPe32Plus ? 112 : 96);

            var layout = new PortableExecutableLayout
            {
                PeHeaderOffset = peHeaderOffset,
                IsPe32Plus = isPe32Plus,
                ChecksumOffset = optionalHeaderOffset + 64,
                StackReserveOffset = optionalHeaderOffset + 72,
                CertificateTableEntryOffset = dataDirectoriesOffset + (4 * 8),
                SectionTableOffset = optionalHeaderOffset + BitConverter.ToUInt16( image, peHeaderOffset + 20 ),
                SectionCount = BitConverter.ToUInt16( image, peHeaderOffset + 6 )
            };

            // The CLI header is the data directory entry 14. The strong name signature directory is at offset 32 of it.
            var cliHeaderOffset = layout.RvaToOffset( image, ReadUInt32( image, dataDirectoriesOffset + (14 * 8) ) );

            if ( cliHeaderOffset >= 0 )
            {
                var signatureRva = ReadUInt32( image, cliHeaderOffset + 32 );
                var signatureSize = (int) ReadUInt32( image, cliHeaderOffset + 36 );

                if ( signatureRva != 0 && signatureSize > 0 )
                {
                    layout.SignatureOffset = layout.RvaToOffset( image, signatureRva );
                    layout.SignatureSize = layout.SignatureOffset >= 0 ? signatureSize : 0;
                }
            }

            error = null;

            return layout;
        }

        /// <summary>
        /// Converts a relative virtual address to an offset in the file, or returns -1 when no section contains it.
        /// </summary>
        private int RvaToOffset( byte[] image, uint rva )
        {
            if ( rva == 0 )
            {
                return -1;
            }

            for ( var i = 0; i < this.SectionCount; i++ )
            {
                var sectionHeader = this.SectionTableOffset + (i * _sectionHeaderSize);
                var virtualSize = ReadUInt32( image, sectionHeader + 8 );
                var virtualAddress = ReadUInt32( image, sectionHeader + 12 );
                var rawDataSize = ReadUInt32( image, sectionHeader + 16 );
                var rawDataOffset = ReadUInt32( image, sectionHeader + 20 );

                if ( rva >= virtualAddress && rva < virtualAddress + Math.Max( virtualSize, rawDataSize ) )
                {
                    return (int) (rva - virtualAddress + rawDataOffset);
                }
            }

            return -1;
        }
    }
}
