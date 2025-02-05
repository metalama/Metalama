// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Metalama.Backstage.Infrastructure
{
    /// <summary>
    /// Provides access to file system.
    /// </summary>
    [PublicAPI]
    public interface IFileSystem : IBackstageService
    {
        /// <summary>
        /// Gets prefix for synchronization objects (mutexes) related to objects of the current file system.
        /// Returns <see langword="null" /> for global file system.
        /// </summary>
        string? SynchronizationPrefix { get; }

        /// <summary>
        /// Returns the date and time the specified file was last written to.
        /// </summary>
        /// <param name="path">The file for which to obtain write date and time information.</param>
        /// <returns>
        /// A <see cref="DateTime" /> structure set to the date and time that the specified file was last written to.
        /// This value is expressed in local time.
        /// </returns>
        DateTime GetFileLastWriteTime( string path );

        /// <summary>
        /// Sets the date and time that the specified file was last written to.
        /// </summary>
        /// <param name="path">The file for which to set the date and time information.</param>
        /// <param name="lastWriteTime">
        /// A <see cref="T:System.DateTime" /> containing the value to set for the last write date and time of <paramref name="path" />.
        /// This value is expressed in local time.
        /// </param>
        void SetFileLastWriteTime( string path, DateTime lastWriteTime );

        /// <summary>
        /// Returns the date and time the specified file or directory was last written to.
        /// </summary>
        /// <param name="path">The file or directory for which to obtain modification date and time information.</param>
        /// <returns>
        /// A structure that is set to the date and time the specified file or directory was last written to.
        /// This value is expressed in local time.
        /// </returns>
        DateTime GetDirectoryLastWriteTime( string path );

        /// <summary>
        /// Sets the date and time a directory was last written to.
        /// </summary>
        /// <param name="path">The path of the directory.</param>
        /// <param name="lastWriteTime">The date and time the directory was last written to. This value is expressed in local time.</param>
        void SetDirectoryLastWriteTime( string path, DateTime lastWriteTime );

        /// <summary>
        /// Determines whether the specified file exists.
        /// </summary>
        /// <param name="path">The file to check.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="path"/> refers to an existing file;
        /// <c>false</c> if the file does not exist
        /// or an error occurs when trying to determine if the specified file exists.
        /// </returns>
        bool FileExists( [NotNullWhen( true )] string? path );

        /// <summary>
        /// Gets the <see cref="FileAttributes" /> of the file on the path.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>
        /// The <see cref="FileAttributes" /> of the file on the path.
        /// </returns>
        public FileAttributes GetFileAttributes( string path );

        /// <summary>
        /// Sets the specified <see cref="FileAttributes" /> of the file on the specified path.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="fileAttributes">A bitwise combination of the enumeration values.</param>
        public void SetFileAttributes( string path, FileAttributes fileAttributes );

        /// <summary>
        /// Determines whether the given path refers to an existing directory on disk.
        /// </summary>
        /// <param name="path">The directory to check.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="path"/> refers to an existing directory;
        /// <c>false</c> if the directory does not exist
        /// or an error occurs when trying to determine if the specified directory exists.
        /// </returns>
        bool DirectoryExists( [NotNullWhen( true )] string? path );

        /// <summary>
        /// Returns an array of file names in a specified path.
        /// </summary>
        /// <param name="path">
        /// The relative or absolute path to the directory to search.
        /// This string is not case-sensitive.
        /// </param>
        /// <returns>
        /// An array of the full names (including paths)
        /// of the files in the directory specified by <paramref name="path"/>.
        /// </returns>
        string[] GetFiles( string path );

        /// <summary>
        /// Returns an array of file names that match a search pattern
        /// in a specified path.
        /// </summary>
        /// <param name="path">
        /// The relative or absolute path to the directory to search.
        /// This string is not case-sensitive.
        /// </param>
        /// <param name="searchPattern">
        /// The search string to match against the names of files in path.
        /// This parameter can contain a combination of valid literal path
        /// and wild-card (* and ?) characters.
        /// It doesn't support regular expressions.
        /// </param>
        /// <returns>
        /// An array of the full names (including paths)
        /// of the files in the directory specified by <paramref name="path"/>
        /// and that match the specified <paramref name="searchPattern"/>.
        /// </returns>
        string[] GetFiles( string path, string searchPattern );

        /// <summary>
        /// Returns an array of file names that match a search pattern
        /// in a specified path, and optionally searches subdirectories.
        /// </summary>
        /// <param name="path">
        /// The relative or absolute path to the directory to search.
        /// This string is not case-sensitive.
        /// </param>
        /// <param name="searchPattern">
        /// The search string to match against the names of files in path.
        /// This parameter can contain a combination of valid literal path
        /// and wild-card (* and ?) characters.
        /// It doesn't support regular expressions.
        /// </param>
        /// <param name="searchOption">
        /// One of the enumeration values that specify whether the search operation
        /// should include only the current directory or all subdirectories.
        /// The default value is <see cref="SearchOption.TopDirectoryOnly" />.
        /// </param>
        /// <returns>
        /// An array of the full names (including paths)
        /// of the files in the directory specified by <paramref name="path"/>
        /// and that match the specified <paramref name="searchPattern"/>
        /// and <paramref name="searchOption"/>.
        /// </returns>
        string[] GetFiles( string path, string searchPattern, SearchOption searchOption );

        /// <summary>
        /// Returns an enumerable collection of file names
        /// in a specified path.
        /// </summary>
        /// <param name="path">
        /// The relative or absolute path to the directory to search.
        /// This string is not case-sensitive.
        /// </param>
        /// <returns>
        /// An enumerable collection of the full names (including paths)
        /// of the files in the directory specified by <paramref name="path"/>.
        /// </returns>
        IEnumerable<string> EnumerateFiles( string path );

        /// <summary>
        /// Returns an enumerable collection of file names that match a search pattern
        /// in a specified path.
        /// </summary>
        /// <param name="path">
        /// The relative or absolute path to the directory to search.
        /// This string is not case-sensitive.
        /// </param>
        /// <param name="searchPattern">
        /// The search string to match against the names of files in path.
        /// This parameter can contain a combination of valid literal path
        /// and wild-card (* and ?) characters.
        /// It doesn't support regular expressions.
        /// </param>
        /// <returns>
        /// An enumerable collection of the full names (including paths)
        /// of the files in the directory specified by <paramref name="path"/>
        /// and that match the specified <paramref name="searchPattern"/>.
        /// </returns>
        IEnumerable<string> EnumerateFiles( string path, string searchPattern );

        /// <summary>
        /// Returns an enumerable collection of file names that match a search pattern
        /// in a specified path, and optionally searches subdirectories.
        /// </summary>
        /// <param name="path">
        /// The relative or absolute path to the directory to search.
        /// This string is not case-sensitive.
        /// </param>
        /// <param name="searchPattern">
        /// The search string to match against the names of files in path.
        /// This parameter can contain a combination of valid literal path
        /// and wild-card (* and ?) characters.
        /// It doesn't support regular expressions.
        /// </param>
        /// <param name="searchOption">
        /// One of the enumeration values that specify whether the search operation
        /// should include only the current directory or all subdirectories.
        /// The default value is <see cref="SearchOption.TopDirectoryOnly" />.
        /// </param>
        /// <returns>
        /// An enumerable collection of the full names (including paths)
        /// of the files in the directory specified by <paramref name="path"/>
        /// and that match the specified <paramref name="searchPattern"/>
        /// and <paramref name="searchOption"/>.
        /// </returns>
        IEnumerable<string> EnumerateFiles( string path, string searchPattern, SearchOption searchOption );

        /// <summary>
        /// Returns an array of directory names in a specified path.
        /// </summary>
        /// <param name="path">
        /// The relative or absolute path to the directory to search.
        /// This string is not case-sensitive.
        /// </param>
        /// <returns>
        /// An array of the full names (including paths)
        /// of the directories in the directory specified by <paramref name="path"/>.
        /// </returns>
        string[] GetDirectories( string path );

        /// <summary>
        /// Returns an array of directory names that match a search pattern
        /// in a specified path.
        /// </summary>
        /// <param name="path">
        /// The relative or absolute path to the directory to search.
        /// This string is not case-sensitive.
        /// </param>
        /// <param name="searchPattern">
        /// The search string to match against the names of directories in path.
        /// This parameter can contain a combination of valid literal path
        /// and wild-card (* and ?) characters.
        /// It doesn't support regular expressions.
        /// </param>
        /// <returns>
        /// An array of the full names (including paths)
        /// of the directories in the directory specified by <paramref name="path"/>
        /// and that match the specified <paramref name="searchPattern"/>.
        /// </returns>
        string[] GetDirectories( string path, string searchPattern );

        /// <summary>
        /// Returns an array of directory names that match a search pattern
        /// in a specified path, and optionally searches subdirectories.
        /// </summary>
        /// <param name="path">
        /// The relative or absolute path to the directory to search.
        /// This string is not case-sensitive.
        /// </param>
        /// <param name="searchPattern">
        /// The search string to match against the names of directories in path.
        /// This parameter can contain a combination of valid literal path
        /// and wild-card (* and ?) characters.
        /// It doesn't support regular expressions.
        /// </param>
        /// <param name="searchOption">
        /// One of the enumeration values that specify whether the search operation
        /// should include only the current directory or all subdirectories.
        /// The default value is <see cref="SearchOption.TopDirectoryOnly" />.
        /// </param>
        /// <returns>
        /// An array of the full names (including paths)
        /// of the directories in the directory specified by <paramref name="path"/>
        /// and that match the specified <paramref name="searchPattern"/>
        /// and <paramref name="searchOption"/>.
        /// </returns>
        string[] GetDirectories( string path, string searchPattern, SearchOption searchOption );

        /// <summary>
        /// Returns an enumerable collection of directory names in a specified path.
        /// </summary>
        /// <param name="path">
        /// The relative or absolute path to the directory to search.
        /// This string is not case-sensitive.
        /// </param>
        /// <returns>
        /// An enumerable collection of the full names (including paths)
        /// of the directories in the directory specified by <paramref name="path"/>.
        /// </returns>
        IEnumerable<string> EnumerateDirectories( string path );

        /// <summary>
        /// Returns an enumerable collection of directory names that match a search pattern
        /// in a specified path.
        /// </summary>
        /// <param name="path">
        /// The relative or absolute path to the directory to search.
        /// This string is not case-sensitive.
        /// </param>
        /// <param name="searchPattern">
        /// The search string to match against the names of directories in path.
        /// This parameter can contain a combination of valid literal path
        /// and wild-card (* and ?) characters.
        /// It doesn't support regular expressions.
        /// </param>
        /// <returns>
        /// An enumerable collection of the full names (including paths)
        /// of the directories in the directory specified by <paramref name="path"/>
        /// and that match the specified <paramref name="searchPattern"/>.
        /// </returns>
        IEnumerable<string> EnumerateDirectories( string path, string searchPattern );

        /// <summary>
        /// Returns an enumerable collection of directory names that match a search pattern
        /// in a specified path, and optionally searches subdirectories.
        /// </summary>
        /// <param name="path">
        /// The relative or absolute path to the directory to search.
        /// This string is not case-sensitive.
        /// </param>
        /// <param name="searchPattern">
        /// The search string to match against the names of directories in path.
        /// This parameter can contain a combination of valid literal path
        /// and wild-card (* and ?) characters.
        /// It doesn't support regular expressions.
        /// </param>
        /// <param name="searchOption">
        /// One of the enumeration values that specify whether the search operation
        /// should include only the current directory or all subdirectories.
        /// The default value is <see cref="SearchOption.TopDirectoryOnly" />.
        /// </param>
        /// <returns>
        /// An enumerable collection of the full names (including paths)
        /// of the directories in the directory specified by <paramref name="path"/>
        /// and that match the specified <paramref name="searchPattern"/>
        /// and <paramref name="searchOption"/>.
        /// </returns>
        IEnumerable<string> EnumerateDirectories( string path, string searchPattern, SearchOption searchOption );

        /// <summary>
        /// Creates or overwrites a file in the specified path.
        /// </summary>
        /// <param name="path">The path and name of the file to create.</param>
        /// <returns>A <see cref="Stream"/> that provides read/write access to the file specified in <paramref name="path"/>.</returns>
        Stream CreateFile( string path );

        /// <summary>
        /// Creates or overwrites a file in the specified path, specifying a buffer size.
        /// </summary>
        /// <param name="path">The path and name of the file to create.</param>
        /// <param name="bufferSize">The number of bytes buffered for reads and writes to the file.</param>
        /// <returns>
        /// A <see cref="Stream"/> with the specified buffer size
        /// that provides read/write access to the file specified in <paramref name="path"/>.
        /// </returns>
        Stream CreateFile( string path, int bufferSize );

        /// <summary>
        /// Creates or overwrites a file in the specified path, specifying a buffer size
        /// and options that describe how to create or overwrite the file.
        /// </summary>
        /// <param name="path">The path and name of the file to create.</param>
        /// <param name="bufferSize">The number of bytes buffered for reads and writes to the file.</param>
        /// <param name="options">One of the <see cref="FileOptions"/> values that describes how to create or overwrite the file.</param>
        /// <returns>
        /// A <see cref="Stream"/> with the specified buffer size
        /// that provides specified access to the file specified in <paramref name="path"/>.
        /// </returns>
        Stream CreateFile( string path, int bufferSize, FileOptions options );

        /// <summary>
        /// Creates or opens a file for writing UTF-8 encoded text. If the file already exists, its contents are overwritten.
        /// </summary>
        /// <param name="path">The file to be opened for writing.</param>
        /// <returns>
        /// A <see cref="System.IO.StreamWriter" /> that writes to the specified file using UTF-8 encoding.
        /// </returns>
        StreamWriter CreateTextFile( string path );

        /// <summary>
        /// Creates a uniquely named, zero-byte temporary file on disk and returns the full path of that file.
        /// </summary>
        /// <returns>The full path of the temporary file.</returns>
        string GetTempFileName();

        /// <summary>
        /// Creates all directories and subdirectories in the specified path unless they already exist.
        /// </summary>
        /// <param name="path">The directory to create.</param>
        void CreateDirectory( string path );

        /// <summary>
        /// Opens an existing file.
        /// </summary>
        /// <param name="path">The file to be opened.</param>
        /// <param name="mode">
        /// A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist,
        /// and determines whether the contents of existing files are retained or overwritten.
        /// </param>
        /// <returns>A <see cref="Stream"/> on the specified path, having the specified mode.</returns>
        Stream Open( string path, FileMode mode );

        /// <summary>
        /// Opens an existing file.
        /// </summary>
        /// <param name="path">The file to be opened.</param>
        /// <param name="mode">
        /// A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist,
        /// and determines whether the contents of existing files are retained or overwritten.
        /// </param>
        /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the file.</param>
        /// <returns>
        /// A <see cref="Stream"/> on the specified path, having the specified mode
        /// with read, write, or read/write access.
        /// </returns>
        Stream Open( string path, FileMode mode, FileAccess access );

        /// <summary>
        /// Opens an existing file.
        /// </summary>
        /// <param name="path">The file to be opened.</param>
        /// <param name="mode">
        /// A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist,
        /// and determines whether the contents of existing files are retained or overwritten.
        /// </param>
        /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the file.</param>
        /// <param name="share">A <see cref="FileShare"/> value specifying the type of access other threads have to the file.</param>
        /// <returns>
        /// A <see cref="Stream"/> on the specified path, having the specified mode
        /// with read, write, or read/write access and the specified sharing option.
        /// </returns>
        Stream Open( string path, FileMode mode, FileAccess access, FileShare share );

        /// <summary>
        /// Opens an existing file.
        /// </summary>
        /// <param name="path">The file to be opened.</param>
        /// <param name="mode">
        /// A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist,
        /// and determines whether the contents of existing files are retained or overwritten.
        /// </param>
        /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the file.</param>
        /// <param name="share">A <see cref="FileShare"/> value specifying the type of access other threads have to the file.</param>
        /// <param name="bufferSize">A positive <see cref="int"/> value greater than 0 indicating the buffer size. The default buffer size is 4096.</param>
        /// <param name="options">A <see cref="FileOptions"/> value specifying advance options for creating a <see cref="FileStream"/> object.</param>
        /// <returns>
        /// A <see cref="Stream"/> on the specified path, having the specified mode
        /// with read, write, or read/write access and the specified sharing option.
        /// </returns>
        Stream Open( string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options );

        /// <summary>
        /// Opens an existing file for reading.
        /// </summary>
        /// <param name="path">The file to be opened for reading.</param>
        /// <returns>A read-only <see cref="Stream" /> on the specified path.</returns>
        Stream OpenRead( string path );

        /// <summary>
        /// Opens an existing file or creates a new file for writing.
        /// </summary>
        /// <param name="path">The file to be opened for writing.</param>
        /// <returns>An unshared writable <see cref="Stream" /> object on the specified path.</returns>
        Stream OpenWrite( string path );

        /// <summary>
        /// Opens a binary file, reads the contents of the file into a byte array, and then closes the file.
        /// </summary>
        /// <param name="path">The file to read from.</param>
        /// <returns>A <see cref="byte" /> array containing the contents of the file.</returns>
        byte[] ReadAllBytes( string path );

        /// <summary>
        /// Creates a new file, writes the specified byte array to the file, and then closes the file.
        /// If the target file already exists, it is overwritten.
        /// </summary>
        /// <param name="path">The file to write to.</param>
        /// <param name="bytes">The bytes to write to the file.</param>
        void WriteAllBytes( string path, byte[] bytes );

        /// <summary>
        /// Opens a text file, reads all text of the file, and then closes the file.
        /// </summary>
        /// <param name="path">The file to read from.</param>
        /// <returns>A string containing all lines of the file.</returns>
        string ReadAllText( string path );

        /// <summary>
        /// Creates a new file, writes the specified string to the file, and then closes the file.
        /// If the target file already exists, it is overwritten.
        /// </summary>
        /// <param name="path">The file to write to.</param>
        /// <param name="content">The string to write to the file.</param>
        void WriteAllText( string path, string? content );

        /// <summary>
        /// Creates a new file, writes the specified string to the file using the specified encoding, and then closes the file. If the target file already exists, it is overwritten.
        /// </summary>
        /// <param name="path">The file to write to.</param>
        /// <param name="contents">The string to write to the file.</param>
        /// <param name="encoding">The encoding to apply to the string.</param>
        void WriteAllText( string path, string? contents, Encoding encoding );

        /// <summary>
        /// Opens a text file, reads all lines of the file, and then closes the file.
        /// </summary>
        /// <param name="path">The file to read from.</param>
        /// <returns>A <see cref="string" /> array containing all lines of the file.</returns>
        string[] ReadAllLines( string path );

        /// <summary>
        /// Creates a new file, writes the specified string to the file, and then closes the file.
        /// If the target file already exists, it is overwritten.
        /// </summary>
        /// <param name="path">The file to write to.</param>
        /// <param name="contents">The lines to write to the file.</param>
        void WriteAllLines( string path, string[] contents );

        /// <summary>
        /// Creates a new file, writes the specified string to the file, and then closes the file.
        /// If the target file already exists, it is overwritten.
        /// </summary>
        /// <param name="path">The file to write to.</param>
        /// <param name="contents">The lines to write to the file.</param>
        void WriteAllLines( string path, IEnumerable<string> contents );

        /// <summary>
        /// Appends lines to a file, and then closes the file. If the specified file does not exist, this method creates a file, writes the specified lines to the file, and then closes the file.
        /// </summary>
        /// <param name="path">The file to append the lines to. The file is created if it doesn't already exist.</param>
        /// <param name="contents">The lines to append to the file.</param>
        void AppendAllLines( string path, IEnumerable<string> contents );

        /// <summary>
        /// Appends lines to a file by using a specified encoding, and then closes the file. If the specified file does not exist, this method creates a file, writes the specified lines to the file, and then closes the file.
        /// </summary>
        /// <param name="path">The file to append the lines to. The file is created if it doesn't already exist.</param>
        /// <param name="contents">The lines to append to the file.</param>
        /// <param name="encoding">The character encoding to use.</param>
        void AppendAllLines( string path, IEnumerable<string> contents, Encoding encoding );

        /// <summary>
        /// Opens a file, appends the specified string to the file, and then closes the file. If the file does not exist, this method creates a file, writes the specified string to the file, then closes the file.
        /// </summary>
        /// <param name="path">The file to append the specified string to.</param>
        /// <param name="contents">The string to append to the file.</param>
        void AppendAllText( string path, string? contents );

        /// <summary>
        /// Appends the specified string to the file, creating the file if it does not already exist.
        /// </summary>
        /// <param name="path">The file to append the specified string to.</param>
        /// <param name="contents">The string to append to the file.</param>
        /// <param name="encoding">The character encoding to use.</param>
        void AppendAllText( string path, string? contents, Encoding encoding );

        /// <summary>Moves a specified file to a new location, providing the option to specify a new file name.</summary>
        /// <param name="sourceFileName">The name of the file to move. Can include a relative or absolute path.</param>
        /// <param name="destFileName">The new path and name for the file.</param>
        void MoveFile( string sourceFileName, string destFileName );

        /// <summary>Deletes the specified file.</summary>
        /// <param name="path">The name of the file to be deleted. Wildcard characters are not supported.</param>
        void DeleteFile( string path );

        /// <summary>Moves a file or a directory and its contents to a new location.</summary>
        /// <param name="sourceDirName">The path of the file or directory to move.</param>
        /// <param name="destDirName">The path to the new location for <paramref name="sourceDirName" />. If <paramref name="sourceDirName" /> is a file, then <paramref name="destDirName" /> must also be a file name.</param>
        void MoveDirectory( string sourceDirName, string destDirName );

        /// <summary>Deletes the specified directory and, if indicated, any subdirectories and files in the directory.</summary>
        /// <param name="path">The name of the directory to remove.</param>
        /// <param name="recursive">
        /// <see langword="true" /> to remove directories, subdirectories, and files in <paramref name="path" />; otherwise, <see langword="false" />.</param>
        void DeleteDirectory( string path, bool recursive );

        /// <summary>
        /// Checks whether directory on <paramref name="path"/> is empty.
        /// </summary>
        /// <param name="path">The name of the directory to check.</param>
        /// <returns>Value indicating whether <paramref name="path"/> has any subdirectories or files.</returns>
        bool IsDirectoryEmpty( string path );

        /// <summary>
        /// Extracts all the files in the zip archive to a directory on the file system.
        /// </summary>
        /// <param name="sourceZipArchive">The zip archive to extract files from.</param>
        /// <param name="destinationDirectoryPath">
        /// The path to the directory to place the extracted files in. You can specify either
        /// a relative or an absolute path. A relative path is interpreted as relative to
        /// the current working directory.
        /// </param>
        void ExtractZipArchiveToDirectory( ZipArchive sourceZipArchive, string destinationDirectoryPath );

        IDisposable WatchChanges( string directory, string filter, Action<FileSystemEventArgs> callback );
    }
}