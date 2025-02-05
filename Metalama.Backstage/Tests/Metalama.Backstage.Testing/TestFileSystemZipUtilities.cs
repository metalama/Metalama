// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.IO;
using System.IO.Compression;

namespace Metalama.Backstage.Testing;

// https://github.com/Testably/Testably.Abstractions/blob/main/Source/Testably.Abstractions.Compression/Internal/ZipUtilities.cs
internal static class TestFileSystemZipUtilities
{
    internal static void ExtractRelativeToDirectory(
        TestFileSystem fileSystem,
        ZipArchiveEntry sourceEntry,
        string destinationDirectoryName,
        bool overwrite )
    {
        var fileDestinationPath =
            Path.Combine(
                destinationDirectoryName,
                sourceEntry.FullName.TrimStart(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar ) );

        var directoryPath =
            Path.GetDirectoryName( fileDestinationPath );

        if ( directoryPath != null &&
             !fileSystem.DirectoryExists( directoryPath ) )
        {
            fileSystem.CreateDirectory( directoryPath );
        }

        if ( sourceEntry.FullName.EndsWith( "/", StringComparison.Ordinal ) )
        {
            if ( sourceEntry.Length != 0 )
            {
                throw new IOException( "Zip entry name ends in directory separator character but contains data." );
            }

            fileSystem.CreateDirectory( fileDestinationPath );
        }
        else
        {
            ExtractToFile( fileSystem, sourceEntry, fileDestinationPath, overwrite );
        }
    }

    internal static void ExtractToDirectory(
        TestFileSystem fileSystem,
        ZipArchive sourceZipArchive,
        string destinationDirectoryPath,
        bool overwriteFiles = false )
    {
        foreach ( var entry in sourceZipArchive.Entries )
        {
            ExtractRelativeToDirectory( fileSystem, entry, destinationDirectoryPath, overwriteFiles );
        }
    }

    internal static void ExtractToFile(
        TestFileSystem fileSystem,
        ZipArchiveEntry sourceEntry,
        string destinationFileName,
        bool overwrite )
    {
        var mode = overwrite ? FileMode.Create : FileMode.CreateNew;

        using ( var fileStream = fileSystem.Open( destinationFileName, mode, FileAccess.Write, FileShare.None ) )
        {
            using ( var entryStream = sourceEntry.Open() )
            {
                entryStream.CopyTo( fileStream );
            }
        }

        fileSystem.SetFileLastWriteTime( destinationFileName, sourceEntry.LastWriteTime.DateTime );
    }
}