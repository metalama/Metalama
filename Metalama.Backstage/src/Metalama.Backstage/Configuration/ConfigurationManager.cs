// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Serialization;
using Metalama.Backstage.Utilities;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Backstage.Configuration
{
    internal sealed class ConfigurationManager : IConfigurationManager
    {
        // Stores the in-memory configuration object. Note that ConfigurationFile can be implemented in a different assembly, and that
        // there may be several copies of this assembly in the current AppDomain. Therefore, this dictionary may contain several objects
        // that represent the same file.
        private readonly ConcurrentDictionary<Type, ConfigurationFile> _instances = new();

        private readonly IDisposable? _fileSystemWatcher;
        private readonly IFileSystem _fileSystem;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IEnvironmentVariableProvider _environmentVariableProvider;
        private readonly IJsonSerializationService _jsonSerializationService;
        private readonly ConcurrentDictionary<string, string> _fileChanges = new( StringComparer.Ordinal );

        /// <summary>
        /// The default time during which an acquisition of <see cref="_mutex"/> waits before it is abandoned.
        /// </summary>
        private const int _defaultMutexTimeoutMilliseconds = 30000;

        private readonly int _mutexTimeoutMilliseconds;

        // Named semaphore to handle many instances.
        private readonly Mutex _mutex;
        private volatile int _fileChangeProcessingTaskStatus;

        /// <summary>
        /// A value indicating whether the last attempt to acquire <see cref="_mutex"/> timed out, in which case the next
        /// attempts do not wait for it. See <see cref="TryAcquireMutex"/>.
        /// </summary>
        private volatile bool _isMutexUnavailable;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationManager"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="mutexTimeoutMilliseconds">
        /// The time during which an acquisition of the global configuration mutex waits before it is abandoned. Only a
        /// test passes a value other than the default one, so that it does not have to wait for the real timeout.
        /// </param>
        public ConfigurationManager( IServiceProvider serviceProvider, int mutexTimeoutMilliseconds = _defaultMutexTimeoutMilliseconds )
        {
            this._mutexTimeoutMilliseconds = mutexTimeoutMilliseconds;

            if ( !string.IsNullOrEmpty( Environment.GetEnvironmentVariable( "METALAMA_DEBUG_CONFIGURATION_MANAGER" ) ) )
            {
                DebuggerHelper.Launch();
            }

            var applicationInfo = serviceProvider.GetBackstageService<IApplicationInfoProvider>()?.CurrentApplication;
            this._fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
            this._dateTimeProvider = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
            this._environmentVariableProvider = serviceProvider.GetRequiredBackstageService<IEnvironmentVariableProvider>();
            this._jsonSerializationService = serviceProvider.GetRequiredBackstageService<IJsonSerializationService>();

            // There is a cyclic dependency between the logger factory and the configuration manager. To work around this problem, we buffer
            // the reported messages and we report them when the real logging service is available.
            this.Logger = serviceProvider.GetRequiredBackstageService<EarlyLoggerFactory>().GetLogger( "Configuration" );

            this.ApplicationDataDirectory = serviceProvider.GetRequiredBackstageService<IStandardDirectories>().ApplicationDataDirectory;

            // We pass no logger here, we will be unable to read the log anyway if this throws an exception.
            this._mutex = MutexHelper.OpenOrCreateMutex( this.ApplicationDataDirectory, this._fileSystem.SynchronizationPrefix, null );

            if ( !this._fileSystem.DirectoryExists( this.ApplicationDataDirectory ) )
            {
                this._fileSystem.CreateDirectory( this.ApplicationDataDirectory );
            }

            if ( applicationInfo is { IsLongRunningProcess: true } )
            {
                this._fileSystemWatcher = this._fileSystem.WatchChanges( this.ApplicationDataDirectory, "*.json", this.OnFileChanged );
            }
        }

        private void OnFileChanged( FileSystemEventArgs e )
        {
            this.Logger.Trace?.Log( $"File has changed: '{e.FullPath}'." );
            var fileName = e.FullPath;

            var isAffected = this._instances.Values.Any(
                s =>
                    string.Equals( this.GetFilePath( s.GetType() ), fileName, StringComparison.OrdinalIgnoreCase ) );

            if ( isAffected &&
                 this._fileChanges.TryAdd( e.FullPath, e.FullPath ) &&
                 Interlocked.CompareExchange( ref this._fileChangeProcessingTaskStatus, 1, 0 ) == 0 )
            {
                Task.Run( this.ProcessFileChanges );
            }
        }

        private void ProcessFileChanges()
        {
            try
            {
                // To frequent avoid file locks, wait. There is another wait cycle in TryLoadSettings but not
                // waiting here is annoying for debugging.
                Thread.Sleep( 100 );

                if ( !this.TryAcquireMutex( out var mutexHandle ) )
                {
                    // The changes stay in the buffer, so that they are processed by the next notification, once the
                    // mutex has become available again.
                    this._fileChangeProcessingTaskStatus = 0;

                    return;
                }

                using ( mutexHandle )
                {
                    void Process()
                    {
                        while ( !this._fileChanges.IsEmpty )
                        {
                            foreach ( var fileName in this._fileChanges.Keys )
                            {
                                var oldSettings = this._instances.Values.Where(
                                    s =>
                                        string.Equals(
                                            this.GetFilePath( s.GetType() ),
                                            fileName,
                                            StringComparison.OrdinalIgnoreCase ) );

                                foreach ( var oldSetting in oldSettings )
                                {
                                    if ( this.TryLoadConfigurationFile( oldSetting.GetType(), out var newSetting ) &&
                                         (oldSetting.Timestamp == null || oldSetting.Timestamp.Value.IsOlderThan( newSetting.Timestamp!.Value )) )
                                    {
                                        this.AddToCache( newSetting );
                                    }
                                }

                                this._fileChanges.TryRemove( fileName, out _ );
                            }
                        }
                    }

                    Process();

                    this._fileChangeProcessingTaskStatus = 0;

                    // In case of race we don't want to lose events in the buffer.
                    // It is preferable in this case to have two concurrent tasks. They will not interfere because of the lock.
                    Process();
                }
            }
            catch ( Exception e )
            {
                // When we have an exception we may miss events in case of race.

                this.Logger.LogException( e );
                this._fileChangeProcessingTaskStatus = 0;
            }
        }

        private string ApplicationDataDirectory { get; }

        public ILogger Logger { get; }

        public string GetFilePath( string fileName ) => Path.Combine( this.ApplicationDataDirectory, fileName );

        public string GetFilePath( Type type )
        {
            var attribute = type.GetCustomAttribute<ConfigurationFileAttribute>()
                            ?? throw new InvalidOperationException(
                                $"'{nameof(ConfigurationFileAttribute)}' custom attribute not found for '{type.FullName}' type." );

            return this.GetFilePath( attribute.FileName );
        }

        private static string? GetEnvironmentVariableName( Type type )
        {
            var attribute = type.GetCustomAttribute<ConfigurationFileAttribute>()
                            ?? throw new InvalidOperationException(
                                $"'{nameof(ConfigurationFileAttribute)}' custom attribute not found for '{type.FullName}' type." );

            return attribute.EnvironmentVariableName;
        }

        public ConfigurationFile Get( Type type, bool ignoreCache = false )
        {
            // Returns null when the configuration could not be read because the global configuration mutex was
            // unavailable.
            ConfigurationFile? GetCore()
            {
                if ( !this.TryAcquireMutex( out var mutexHandle ) )
                {
                    return null;
                }

                using ( mutexHandle )
                {
                    this.Logger.Trace?.Log( $"Loading configuration {type.Name} from file." );

                    if ( this.TryLoadConfigurationFile( type, out var value ) )
                    {
                        return value;
                    }

                    return CreateDefaultInstance( type );
                }
            }

            ConfigurationFile? settings;

            if ( ignoreCache )
            {
                settings = GetCore();

                if ( settings != null )
                {
                    this.AddToCache( settings );
                }
            }
            else if ( !this._instances.TryGetValue( type, out settings ) )
            {
                settings = GetCore();

                if ( settings != null )
                {
                    settings = this._instances.GetOrAdd( type, settings );
                }
            }

            // When the mutex is unavailable, degrade to the default configuration instead of failing the operation
            // during which the configuration happens to be read. The default value is deliberately not cached, so that
            // a later read returns the real configuration once the mutex becomes available again. See issue #1847.
            return settings ?? CreateDefaultInstance( type );
        }

        private static ConfigurationFile CreateDefaultInstance( Type type )
        {
            var settingsObject = Activator.CreateInstance( type )
                                 ?? throw new InvalidOperationException( $"Failed to create instance of '{type.FullName}' type." );

            return (ConfigurationFile) settingsObject;
        }

        public event Action<ConfigurationFile>? ConfigurationFileChanged;

        private void AddToCache( ConfigurationFile settings )
        {
            var isChange = this._instances.TryGetValue( settings.GetType(), out var oldValue ) &&
                           !this.StructurallyEquals( oldValue, settings );

            // We always update the cache even if there is no structural change to make sure we have the latest version number.
            this._instances.AddOrUpdate( settings.GetType(), settings, ( _, _ ) => settings );

            // However, we only raise the event when there is a change.
            if ( isChange )
            {
                this.ConfigurationFileChanged?.Invoke( settings );
            }
        }

        private bool StructurallyEquals( ConfigurationFile a, ConfigurationFile b )
        {
            // Compare JSON representations excluding the version property
            var type = a.GetType();
            var aWithoutVersion = a with { Version = null };
            var bWithoutVersion = b with { Version = null };

            var jsonA = this._jsonSerializationService.Serialize( aWithoutVersion, type );
            var jsonB = this._jsonSerializationService.Serialize( bWithoutVersion, type );

            return jsonA.Equals( jsonB, StringComparison.Ordinal );
        }

        public bool TryUpdate( ConfigurationFile value, ConfigurationFileTimestamp? expectedTimestamp )
        {
            if ( !this.TryAcquireMutex( out var mutexHandle ) )
            {
                throw new ConfigurationMutexTimeoutException(
                    $"Cannot update '{this.GetFilePath( value.GetType() )}' because the global configuration mutex cannot be acquired." );
            }

            using ( mutexHandle )
            {
                var type = value.GetType();
                var fileName = this.GetFilePath( type );

                this.Logger.Trace?.Log( $"Trying to update '{fileName}'. Our last timestamp is '{expectedTimestamp}'." );

                // Verify (inside the global lock) that we have a fresh copy of the file.
                if ( expectedTimestamp == null )
                {
                    if ( this._fileSystem.FileExists( fileName ) )
                    {
                        this.Logger.Warning?.Log( $"Cannot update '{fileName}' because the file exists but was not expected to exist." );

                        return false;
                    }
                }
                else
                {
                    if ( !this._fileSystem.FileExists( fileName ) )
                    {
                        this.Logger.Warning?.Log( $"Cannot update '{fileName}' because the file does not exists but was expected to exist." );

                        return false;
                    }

                    var existingFile = this.Get( value.GetType(), true );

                    if ( existingFile.Timestamp!.Value != expectedTimestamp.Value )
                    {
                        this.Logger.Warning?.Log(
                            $"Cannot update '{fileName}' because the file has a different timestamp than expected: {existingFile.Timestamp} instead of {expectedTimestamp.Value}." );

                        return false;
                    }

                    // We must wait until the clock returns a different value than the current one.
                    while ( this._dateTimeProvider.UtcNow == existingFile.Timestamp.Value.ToUtcDateTime() )
                    {
                        Thread.Sleep( 1 );
                    }
                }

                if ( this._instances.TryGetValue( type, out var originalSettings ) )
                {
                    if ( expectedTimestamp.HasValue && originalSettings.Timestamp != expectedTimestamp.Value )
                    {
                        this.Logger.Warning?.Log( $"Cannot update '{fileName}' because our cached copy does not have the latest timestamp." );

                        return false;
                    }
                }

                value.IncrementVersion();
                var json = this._jsonSerializationService.Serialize( value, value.GetType() );

                RetryHelper.Retry( () => this._fileSystem.WriteAllText( fileName, json ) );

                var newLastModified = this._fileSystem.GetFileLastWriteTime( fileName );
                value.SetFileSystemTimestamp( newLastModified );
                this.AddToCache( value );

                this.Logger.Trace?.Log( $"File '{fileName}' updated. The new timestamp is '{value.Timestamp}'." );
            }

            return true;
        }

        private bool TryLoadConfigurationContent( Type type, string fileName, DateTime lastModified, [NotNullWhen( true )] out string? json )
        {
            // Try to load the json from the environment variable.
            var environmentVariableName = GetEnvironmentVariableName( type );

            if ( environmentVariableName != null )
            {
                json = this._environmentVariableProvider.GetEnvironmentVariable( environmentVariableName )!;

                if ( !string.IsNullOrWhiteSpace( json ) )
                {
                    this.Logger.Trace?.Log( $"Configuration for {type.Name} loaded from the environment variable '{environmentVariableName}'." );

                    return true;
                }
            }

            // Try to load form the file.
            if ( !this._fileSystem.FileExists( fileName ) )
            {
                this.Logger.Trace?.Log( $"The file '{fileName}' does not exist." );

                json = null;

                return false;
            }

            try
            {
                var fileNameCopy = fileName;

                this.Logger.Trace?.Log( $"Reading configuration file '{fileName}' with timestamp '{lastModified:O}'." );

                json = RetryHelper.Retry( () => this._fileSystem.ReadAllText( fileNameCopy ) );

                return true;
            }
            catch ( Exception e )
            {
                this.Logger.LogException( e, $"Error reading file '{fileName}'" );
            }

            // Could not be loaded.
            json = null;

            return false;
        }

        private bool TryLoadConfigurationFile( Type type, [NotNullWhen( true )] out ConfigurationFile? settings )
        {
            var fileName = this.GetFilePath( type );

            var lastModified = this._fileSystem.GetFileLastWriteTime( fileName );

            if ( !this.TryLoadConfigurationContent( type, fileName, lastModified, out var json ) )
            {
                settings = null;

                return false;
            }

            try
            {
                // Use the serialization service to deserialize the configuration file
                if ( !this._jsonSerializationService.TryDeserialize( json, type, out var deserializedObject ) ||
                     deserializedObject is not ConfigurationFile deserializedSettings )
                {
                    // Deserialization failed (e.g., invalid JSON). We need to return an empty instance of the
                    // configuration object, with the LastModified property properly set. If instead we return
                    // false, the caller will interpret this as if the file did not exist, and it can create
                    // an infinite loop.
                    this.Logger.Error?.Log( $"Error deserializing file '{fileName}'." );
                    settings = (ConfigurationFile) Activator.CreateInstance( type )!;
                    settings.SetFileSystemTimestamp( lastModified );

                    return true;
                }

                settings = deserializedSettings;

                if ( this.Logger.Warning != null )
                {
                    settings.Validate( message => this.Logger.Warning?.Log( $"Recoverable error in '{fileName}: {message}'" ) );
                }

                settings.SetFileSystemTimestamp( lastModified );

                return true;
            }
            catch ( Exception e )
            {
                this.Logger.LogException( e, $"Error reading file '{fileName}'" );

                // In case of error, we need to return an empty instance of the configuration object,
                // with the LastModified property properly set. If instead we return false, the caller
                // will interpret this as if the file did not exist, and it can create an infinite loop.
                settings = (ConfigurationFile) Activator.CreateInstance( type )!;
                settings.SetFileSystemTimestamp( lastModified );

                return true;
            }
        }

        /// <summary>
        /// Attempts to acquire the global configuration mutex, and returns <c>false</c> when it cannot be acquired
        /// within the configured timeout.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Failing to acquire the mutex must never fail the operation during which the configuration happens to be read
        /// or written, so this method reports the failure instead of throwing. See issue #1847: the mutex was acquired
        /// on behalf of housekeeping, such as usage telemetry, and the resulting exception aborted the compilation.
        /// </para>
        /// <para>
        /// Once an acquisition has timed out, the following ones do not wait at all until one succeeds again. The mutex
        /// is then either abandoned in a state in which it can no longer be acquired, or contended by many processes,
        /// and in both cases waiting for the full timeout on every subsequent configuration access would block the
        /// process for minutes.
        /// </para>
        /// </remarks>
        private bool TryAcquireMutex( out DisposableAction mutexHandle )
        {
            try
            {
                if ( !this._mutex.WaitOne( 0 ) )
                {
                    var isKnownUnavailable = this._isMutexUnavailable;

                    this.Logger.Trace?.Log( $"Waiting for the configuration mutex." );

                    if ( !this._mutex.WaitOne( isKnownUnavailable ? 0 : this._mutexTimeoutMilliseconds ) )
                    {
                        if ( !isKnownUnavailable )
                        {
                            this._isMutexUnavailable = true;

                            this.Logger.Error?.Log(
                                $"Cannot acquire the global configuration mutex in {this._mutexTimeoutMilliseconds} ms. The configuration is ignored until the mutex becomes available again." );
                        }

                        mutexHandle = default;

                        return false;
                    }
                }
            }
            catch ( AbandonedMutexException )
            {
                this.Logger.Trace?.Log( $"The mutex has been abandoned by another process." );
            }

            this._isMutexUnavailable = false;

            this.Logger.Trace?.Log( $"Configuration mutex acquired." );

            var stopwatch = Stopwatch.StartNew();

            mutexHandle = new DisposableAction(
                () =>
                {
                    this.Logger.Trace?.Log( $"Releasing configuration mutex. It was held for {stopwatch.ElapsedMilliseconds} ms." );

                    if ( stopwatch.ElapsedMilliseconds > 1000 )
                    {
                        this.Logger.Warning?.Log( $"The configuration mutex was held for a long time: {stopwatch.Elapsed}." );
                    }

                    this._mutex.ReleaseMutex();
                } );

            return true;
        }

        public void Dispose()
        {
            this._mutex.Dispose();
            this._fileSystemWatcher?.Dispose();
        }
    }
}