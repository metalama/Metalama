// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Utilities;
using Newtonsoft.Json;
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
        private readonly ConcurrentDictionary<string, string> _fileChanges = new( StringComparer.Ordinal );

        // Named semaphore to handle many instances.
        private readonly Mutex _mutex;
        private volatile int _fileChangeProcessingTaskStatus;

        public ConfigurationManager( IServiceProvider serviceProvider )
        {
            var applicationInfo = serviceProvider.GetBackstageService<IApplicationInfoProvider>()?.CurrentApplication;
            this._fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
            this._dateTimeProvider = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
            this._environmentVariableProvider = serviceProvider.GetRequiredBackstageService<IEnvironmentVariableProvider>();

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

                using ( this.WithMutex() )
                {
                    void Process()
                    {
                        while ( !this._fileChanges.IsEmpty )
                        {
                            foreach ( var fileName in this._fileChanges.Keys )
                            {
                                var oldSettings = this._instances.Values.Where(
                                    s =>
                                        string.Equals( this.GetFilePath( s.GetType() ), fileName, StringComparison.OrdinalIgnoreCase ) );

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

                this.Logger.Error?.Log( e.ToString() );
                this._fileChangeProcessingTaskStatus = 0;
            }
        }

        private string ApplicationDataDirectory { get; }

        public ILogger Logger { get; private set; }

        public string GetFilePath( string fileName ) => Path.Combine( this.ApplicationDataDirectory, fileName );

        public string GetFilePath( Type type )
        {
            var attribute = type.GetCustomAttribute<ConfigurationFileAttribute>()
                            ?? throw new InvalidOperationException(
                                $"'{nameof(ConfigurationFileAttribute)}' custom attribute not found for '{type.FullName}' type." );

            return this.GetFilePath( attribute.FileName );
        }

        private string? GetEnvironmentVariableName( Type type )
        {
            var attribute = type.GetCustomAttribute<ConfigurationFileAttribute>()
                            ?? throw new InvalidOperationException(
                                $"'{nameof(ConfigurationFileAttribute)}' custom attribute not found for '{type.FullName}' type." );

            return attribute.EnvironmentVariableName;
        }

        public ConfigurationFile Get( Type type, bool ignoreCache = false )
        {
            ConfigurationFile GetCore()
            {
                using ( this.WithMutex() )
                {
                    this.Logger.Trace?.Log( $"Loading configuration {type.Name} from file." );

                    if ( this.TryLoadConfigurationFile( type, out var value ) )
                    {
                        return value;
                    }

                    var settingsObject = Activator.CreateInstance( type )
                                         ?? throw new InvalidOperationException( $"Failed to create instance of '{type.FullName}' type." );

                    var settings = (ConfigurationFile) settingsObject;

                    return settings;
                }
            }

            ConfigurationFile settings;

            if ( ignoreCache )
            {
                settings = GetCore();
                this.AddToCache( settings );

                return settings;
            }
            else
            {
                settings = this._instances.GetOrAdd( type, _ => GetCore() );
            }

            return settings;
        }

        public event Action<ConfigurationFile>? ConfigurationFileChanged;

        private void AddToCache( ConfigurationFile settings )
        {
            var isChange = this._instances.TryGetValue( settings.GetType(), out var oldValue ) && !oldValue.StructurallyEqualsTo( settings );

            // We always update the cache even if there is no structural change to make sure we have the latest version number.
            this._instances.AddOrUpdate( settings.GetType(), settings, ( _, _ ) => settings );

            // However, we only raise the event when there is a change.
            if ( isChange )
            {
                this.ConfigurationFileChanged?.Invoke( settings );
            }
        }

        public bool TryUpdate( ConfigurationFile value, ConfigurationFileTimestamp? expectedTimestamp )
        {
            using ( this.WithMutex() )
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
                var json = value.ToJson();

                RetryHelper.Retry( () => this._fileSystem.WriteAllText( fileName, json ) );

                var newLastModified = this._fileSystem.GetFileLastWriteTime( fileName );
                value.SetFilesystemTimestamp( newLastModified );
                this.AddToCache( value );

                this.Logger.Trace?.Log( $"File '{fileName}' updated. The new timestamp is '{value.Timestamp}'." );
            }

            return true;
        }

        private bool TryLoadConfigurationContent( Type type, string fileName, DateTime lastModified, [NotNullWhen( true )] out string? json )
        {
            // Try to load the json from the environment variable.
            var environmentVariableName = this.GetEnvironmentVariableName( type );

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
                this.Logger.Error?.Log( $"Error reading file '{fileName}': " + e );
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
                var jsonSettings = new JsonSerializerSettings { TraceWriter = new JsonTraceWriter( fileName, this.Logger.WithPrefix( "Json" ) ) };

                settings = (ConfigurationFile?) JsonConvert.DeserializeObject( json, type, jsonSettings );

                if ( settings == null )
                {
                    return false;
                }

                if ( this.Logger.Warning != null )
                {
                    settings.Validate( message => this.Logger.Warning?.Log( $"Recoverable error in '{fileName}: {message}'" ) );
                }

                settings.SetFilesystemTimestamp( lastModified );

                return true;
            }
            catch ( Exception e )
            {
                this.Logger.Error?.Log( $"Error reading file '{fileName}': " + e.Message );

                // In case of error, we need to return an empty instance of the configuration object,
                // with the LastModified property properly set. If instead we return false, the caller
                // will interpret this as if the file did not exist, and it can create an infinite loop.
                settings = (ConfigurationFile) Activator.CreateInstance( type )!;
                settings.SetFilesystemTimestamp( lastModified );

                return true;
            }
        }

        private DisposableAction WithMutex()
        {
            try
            {
                if ( !this._mutex.WaitOne( 0 ) )
                {
                    this.Logger.Trace?.Log( $"Waiting for the configuration mutex." );

                    if ( !this._mutex.WaitOne( 30000 ) )
                    {
                        throw new TimeoutException( "Cannot acquire the global configuration mutex in 30s." );
                    }
                }
            }
            catch ( AbandonedMutexException )
            {
                this.Logger.Trace?.Log( $"The mutex has been abandoned by another process." );
            }

            this.Logger.Trace?.Log( $"Configuration mutex acquired." );

            var stopwatch = Stopwatch.StartNew();

            return new DisposableAction(
                () =>
                {
                    this.Logger.Trace?.Log( $"Releasing configuration mutex. It was held for {stopwatch.ElapsedMilliseconds} ms." );

                    if ( stopwatch.ElapsedMilliseconds > 1000 )
                    {
                        this.Logger.Warning?.Log( $"The configuration mutex was held for a long time: {stopwatch.Elapsed}." );
                    }

                    this._mutex.ReleaseMutex();
                } );
        }

        public void Dispose()
        {
            this._mutex.Dispose();
            this._fileSystemWatcher?.Dispose();
        }
    }
}