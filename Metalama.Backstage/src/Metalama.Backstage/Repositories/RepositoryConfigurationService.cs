// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Metalama.Backstage.Repositories;

/// <inheritdoc cref="IRepositoryConfigurationService"/>
internal sealed class RepositoryConfigurationService : IRepositoryConfigurationService
{
    internal const string FileName = "metalama.json";

    // The result rarely changes, but it can be edited in a long-running process (the design-time analysis service),
    // so we cache by directory with a short TTL rather than for the whole process lifetime. The short-lived compiler
    // re-reads on every build anyway.
    private static readonly TimeSpan _cacheTimeToLive = TimeSpan.FromMinutes( 10 );

    private readonly IFileSystem _fileSystem;
    private readonly IJsonSerializationService _jsonSerializationService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger _logger;

    private readonly ConcurrentDictionary<string, (DateTime Timestamp, RepositoryConfigurationResult Result)> _cache =
        new( StringComparer.OrdinalIgnoreCase );

    public RepositoryConfigurationService( IServiceProvider serviceProvider )
    {
        this._fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
        this._jsonSerializationService = serviceProvider.GetRequiredBackstageService<IJsonSerializationService>();
        this._dateTimeProvider = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( "RepositoryConfiguration" );
    }

    public RepositoryConfigurationResult GetConfiguration( string startingDirectory )
    {
        if ( string.IsNullOrEmpty( startingDirectory ) )
        {
            return RepositoryConfigurationResult.Empty;
        }

        string key;

        try
        {
            key = Path.GetFullPath( startingDirectory );
        }
        catch ( ArgumentException )
        {
            // An invalid path cannot resolve a repository; behave as if no file applies.
            return RepositoryConfigurationResult.Empty;
        }

        var now = this._dateTimeProvider.UtcNow;

        if ( this._cache.TryGetValue( key, out var cached ) && now - cached.Timestamp < _cacheTimeToLive )
        {
            return cached.Result;
        }

        var result = this.Resolve( key );
        this._cache[key] = (now, result);

        return result;
    }

    private RepositoryConfigurationResult Resolve( string startingDirectory )
    {
        // Walk up from the starting directory, recording every directory that contains a metalama.json (nearest first)
        // and stopping at the repository root (the first directory that contains a .git folder or file).
        var directoriesWithFile = new List<string>();
        string? repositoryRoot = null;

        var directory = startingDirectory;

        while ( directory != null )
        {
            if ( this._fileSystem.FileExists( Path.Combine( directory, FileName ) ) )
            {
                directoriesWithFile.Add( directory );
            }

            var gitPath = Path.Combine( directory, ".git" );

            if ( this._fileSystem.DirectoryExists( gitPath ) || this._fileSystem.FileExists( gitPath ) )
            {
                repositoryRoot = directory;

                break;
            }

            directory = Path.GetDirectoryName( directory );
        }

        var warnings = ImmutableArray.CreateBuilder<RepositoryConfigurationWarning>();
        string? effectiveFile = null;

        if ( repositoryRoot != null )
        {
            foreach ( var directoryWithFile in directoriesWithFile )
            {
                if ( PathsEqual( directoryWithFile, repositoryRoot ) )
                {
                    effectiveFile = Path.Combine( repositoryRoot, FileName );
                }
                else
                {
                    var misplacedFile = Path.Combine( directoryWithFile, FileName );

                    warnings.Add(
                        new RepositoryConfigurationWarning(
                            RepositoryConfigurationWarningKind.MisplacedFile,
                            misplacedFile,
                            $"The file '{misplacedFile}' is ignored because '{FileName}' must be located at the repository root ('{repositoryRoot}')." ) );
                }
            }
        }
        else if ( directoriesWithFile.Count > 0 )
        {
            // No repository root could be confirmed (no .git anywhere up the tree). Honor the nearest file but warn.
            effectiveFile = Path.Combine( directoriesWithFile[0], FileName );

            warnings.Add(
                new RepositoryConfigurationWarning(
                    RepositoryConfigurationWarningKind.RepositoryRootNotConfirmed,
                    effectiveFile,
                    $"The repository root (a directory containing '.git') could not be located above '{startingDirectory}'. "
                    + $"The file '{effectiveFile}' is being used, but its location could not be verified to be the repository root." ) );
        }

        var configuration = new RepositoryConfiguration();

        if ( effectiveFile != null )
        {
            configuration = this.ReadFile( effectiveFile, warnings );
        }

        if ( warnings.Count > 0 )
        {
            foreach ( var warning in warnings )
            {
                this._logger.Warning?.Log( warning.Message );
            }
        }

        return new RepositoryConfigurationResult { Configuration = configuration, Warnings = warnings.ToImmutable() };
    }

    private RepositoryConfiguration ReadFile( string path, ImmutableArray<RepositoryConfigurationWarning>.Builder warnings )
    {
        string text;

        try
        {
            text = this._fileSystem.ReadAllText( path );
        }
        catch ( IOException e )
        {
            warnings.Add(
                new RepositoryConfigurationWarning(
                    RepositoryConfigurationWarningKind.MalformedFile,
                    path,
                    $"The file '{path}' is ignored because it could not be read: {e.Message}" ) );

            return new RepositoryConfiguration();
        }
        catch ( UnauthorizedAccessException e )
        {
            warnings.Add(
                new RepositoryConfigurationWarning(
                    RepositoryConfigurationWarningKind.MalformedFile,
                    path,
                    $"The file '{path}' is ignored because it could not be read: {e.Message}" ) );

            return new RepositoryConfiguration();
        }

        // A malformed or schema-incompatible file is ignored (the global default applies), but we warn so the user
        // notices that their (possibly intended opt-out) file had no effect.
        if ( this._jsonSerializationService.TryDeserialize<RepositoryConfiguration>( text, out var configuration ) )
        {
            return configuration;
        }

        warnings.Add(
            new RepositoryConfigurationWarning(
                RepositoryConfigurationWarningKind.MalformedFile,
                path,
                $"The file '{path}' is ignored because it is not valid JSON or does not match the expected schema." ) );

        return new RepositoryConfiguration();
    }

    private static bool PathsEqual( string a, string b ) => string.Equals( a, b, StringComparison.OrdinalIgnoreCase );
}
