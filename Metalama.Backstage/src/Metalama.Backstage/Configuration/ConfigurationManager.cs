// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Serialization;
using Metalama.Backstage.Threading;
using Metalama.Backstage.Utilities;
using Metalama.Testing.Hooks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Backstage.Configuration
{
    /// <summary>
    /// Reads and writes the configuration files of the current user, and keeps an in-memory copy of the ones that
    /// have been read.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reading takes no lock at all. A writer publishes a file by substituting it in a single operation, through
    /// <see cref="IFileSystem.WriteAllTextAtomically"/>, so a reader observes either the whole previous content or
    /// the whole new one. What a reader can observe is a file that is no longer the most recent, which the
    /// <see cref="ConfigurationFile.Timestamp"/> of the value it obtains reports, and which the update path
    /// resolves by re-reading under the lock.
    /// </para>
    /// <para>
    /// Writing takes one lock per file rather than one lock for the whole directory, so that an update of one
    /// configuration file does not wait for an unrelated update of another. The lock is only ever held across one
    /// read and one write of a single file: neither the dispatch of <see cref="ConfigurationFileChanged"/> nor the
    /// processing of an external change holds it.
    /// </para>
    /// </remarks>
    internal sealed class ConfigurationManager : IConfigurationManager
    {
        /// <summary>
        /// The maximal time an update waits for the lock protecting one configuration file.
        /// </summary>
        /// <remarks>
        /// A critical section is one read and one write of a single small file, so a wait of this length already
        /// means that something is wrong. Waiting longer would not make the operation more likely to succeed, and
        /// because a failure to acquire the lock is not an error (see <see cref="TryAcquireLock"/>), a long timeout
        /// would stall the caller instead of letting it proceed with what it has.
        /// </remarks>
        private static readonly TimeSpan _lockTimeout = TimeSpan.FromSeconds( 5 );

        /// <summary>
        /// The minimal interval between two warnings about the same file failing to be locked, so that a machine on
        /// which the condition holds does not fill the log with one warning per operation.
        /// </summary>
        private static readonly TimeSpan _lockWarningPeriod = TimeSpan.FromMinutes( 1 );

        /// <summary>
        /// The source of <see cref="InstanceContext"/>.
        /// </summary>
        private static int _nextInstanceId;

        /// <summary>
        /// The file whose transformation the current thread is executing, or <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// A transformation runs while the lock protecting its file is held, so one that started a second update
        /// would make the thread hold two named locks at once, which deadlocks as soon as another thread takes the
        /// same two in the opposite order. The field is static, and therefore shared by every instance of this
        /// class in the process, because two managers over the same directory take the same locks.
        /// </remarks>
        [ThreadStatic]
        private static string? _fileBeingUpdatedByCurrentThread;

        // Stores the in-memory configuration object. Note that ConfigurationFile can be implemented in a different assembly, and that
        // there may be several copies of this assembly in the current AppDomain. Therefore, this dictionary may contain several objects
        // that represent the same file.
        private readonly ConcurrentDictionary<Type, ConfigurationFile> _instances = new();

        private readonly IDisposable? _fileSystemWatcher;
        private readonly IFileSystem _fileSystem;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IEnvironmentVariableProvider _environmentVariableProvider;
        private readonly IJsonSerializationService _jsonSerializationService;
        private readonly INamedLockService _lockService;

        /// <summary>
        /// The provider of the test synchronization points, which is never registered in production and is
        /// therefore normally <see langword="null"/>.
        /// </summary>
        private readonly ITestSynchronizationProvider? _testSynchronizationProvider;

        /// <summary>
        /// The paths of the files whose external modification has been notified but not processed yet.
        /// </summary>
        /// <remarks>
        /// The comparer is case-insensitive, like every other comparison this class makes between two paths.
        /// </remarks>
        private readonly ConcurrentDictionary<string, string> _fileChanges = new( StringComparer.OrdinalIgnoreCase );

        /// <summary>
        /// The locks protecting the individual configuration files, keyed by path and created on demand.
        /// </summary>
        private readonly Dictionary<string, INamedLock> _locks = new( StringComparer.OrdinalIgnoreCase );

        private readonly object _locksSync = new();

        /// <summary>
        /// The moment at which the failure to lock each file was last reported, which throttles the warning.
        /// </summary>
        private readonly ConcurrentDictionary<string, DateTime> _lastLockWarnings = new( StringComparer.OrdinalIgnoreCase );

        private volatile int _fileChangeProcessingTaskStatus;
        private int _isDisposed;

        public ConfigurationManager( IServiceProvider serviceProvider )
        {
            if ( !string.IsNullOrEmpty( Environment.GetEnvironmentVariable( "METALAMA_DEBUG_CONFIGURATION_MANAGER" ) ) )
            {
                DebuggerHelper.Launch();
            }

            var applicationInfo = serviceProvider.GetBackstageService<IApplicationInfoProvider>()?.CurrentApplication;
            this._fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
            this._dateTimeProvider = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
            this._environmentVariableProvider = serviceProvider.GetRequiredBackstageService<IEnvironmentVariableProvider>();
            this._jsonSerializationService = serviceProvider.GetRequiredBackstageService<IJsonSerializationService>();
            this._lockService = serviceProvider.GetRequiredBackstageService<INamedLockService>();

            // Resolved untyped, because ITestSynchronizationProvider is shared with the layers above and therefore
            // cannot derive from IBackstageService.
            this._testSynchronizationProvider = (ITestSynchronizationProvider?) serviceProvider.GetService( typeof(ITestSynchronizationProvider) );

            this.InstanceContext = string.Format( CultureInfo.InvariantCulture, "instance-{0}", Interlocked.Increment( ref _nextInstanceId ) );

            // There is a cyclic dependency between the logger factory and the configuration manager. To work around this problem, we buffer
            // the reported messages and we report them when the real logging service is available.
            this.Logger = serviceProvider.GetRequiredBackstageService<EarlyLoggerFactory>().GetLogger( "Configuration" );

            this.ApplicationDataDirectory = serviceProvider.GetRequiredBackstageService<IStandardDirectories>().ApplicationDataDirectory;

            if ( !this._fileSystem.DirectoryExists( this.ApplicationDataDirectory ) )
            {
                this._fileSystem.CreateDirectory( this.ApplicationDataDirectory );
            }

            if ( applicationInfo is { IsLongRunningProcess: true } )
            {
                this._fileSystemWatcher = this._fileSystem.WatchChanges( this.ApplicationDataDirectory, "*.json", this.OnFileChanged );
            }
        }

        /// <summary>
        /// The location of the synchronization point reached during an update, inside the lock, after the current
        /// content of the file has been read and before the new content is serialized.
        /// </summary>
        internal const string UpdateAfterReadLocation = "UpdateAfterRead";

        /// <summary>
        /// The location of the synchronization point reached during an update, inside the lock, after the new
        /// content has been serialized and before it is written.
        /// </summary>
        internal const string UpdateBeforeWriteLocation = "UpdateBeforeWrite";

        /// <summary>
        /// The location of the synchronization point reached during an update, after the file has been written and
        /// the cache updated, while the lock is still held.
        /// </summary>
        internal const string UpdateBeforeUnlockLocation = "UpdateBeforeUnlock";

        /// <summary>
        /// The location of the synchronization point reached before <see cref="ConfigurationFileChanged"/> is
        /// dispatched, which is always after every lock has been released.
        /// </summary>
        internal const string RaiseChangedBeforeInvokeLocation = "RaiseChangedBeforeInvoke";

        /// <summary>
        /// The location of the synchronization point reached at the beginning of the processing of the external
        /// changes, before the pending changes are snapshotted.
        /// </summary>
        internal const string ProcessFileChangesBeforeDrainLocation = "ProcessFileChangesBeforeDrain";

        /// <summary>
        /// The location of the synchronization point reached after one file has been taken from the pending
        /// changes and before it is reloaded, which is where a further change can land during the processing.
        /// </summary>
        internal const string ProcessFileChangesAfterDequeueLocation = "ProcessFileChangesAfterDequeue";

        /// <summary>
        /// The location of the synchronization point reached between the decision to replace the cached value and
        /// the replacement itself.
        /// </summary>
        internal const string AddToCacheBeforeSwapLocation = "AddToCacheBeforeSwap";

        /// <summary>
        /// Gets a context identifying this instance, used by the synchronization points that are not about one
        /// particular file, so that a test can pin one manager without pinning the others.
        /// </summary>
        internal string InstanceContext { get; }

        /// <summary>
        /// Composes the name of a synchronization point, following the <c>{ClassName}.{Location}:{Context}</c>
        /// convention.
        /// </summary>
        /// <param name="location">One of the <c>Location</c> constants of this class.</param>
        /// <param name="context">The path of the file concerned, or <see cref="InstanceContext"/>.</param>
        /// <returns>The name of the synchronization point.</returns>
        internal static string GetSyncPointName( string location, string context )
            => string.Format( CultureInfo.InvariantCulture, "ConfigurationManager.{0}:{1}", location, context );

        /// <summary>
        /// Reaches a synchronization point, which does nothing unless a test has armed it.
        /// </summary>
        /// <param name="location">One of the <c>Location</c> constants of this class.</param>
        /// <param name="context">The path of the file concerned, or <see cref="InstanceContext"/>.</param>
        private void SyncPoint( string location, string context )
            => this._testSynchronizationProvider?.SyncPoint( GetSyncPointName( location, context ) );

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

        /// <summary>
        /// Reloads the files whose external modification has been notified.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method takes no lock. It reads the files, which needs none because a writer publishes atomically,
        /// and it updates the cache, which is a compare-and-swap. The implementation this one replaces held the
        /// single lock of the whole directory across the entire drain, which is one of the two causes of issue
        /// 1847: a process that both writes the configuration and watches it blocked its own writers for as long as
        /// the notifications kept arriving.
        /// </para>
        /// <para>
        /// The pending state is released before the work rather than after it, both for the whole drain and for
        /// each file, so that a change notified while this method runs starts a new pass instead of being dropped.
        /// The price is that two passes can overlap, which is harmless: the cache only ever moves forward, so the
        /// later of two passes over the same file has no effect.
        /// </para>
        /// </remarks>
        private void ProcessFileChanges()
        {
            var changedValues = new List<ConfigurationFile>();

            // Released before anything that can fail, so that a failure of this pass cannot leave the flag set and
            // suppress every subsequent pass.
            this._fileChangeProcessingTaskStatus = 0;

            try
            {
                // Replaces a Thread.Sleep(100) whose only purpose was to make the sequence easier to observe in a
                // debugger, and which made this method impossible to test.
                this.SyncPoint( ProcessFileChangesBeforeDrainLocation, this.InstanceContext );

                foreach ( var fileName in this._fileChanges.Keys.ToList() )
                {
                    this._fileChanges.TryRemove( fileName, out _ );

                    this.SyncPoint( ProcessFileChangesAfterDequeueLocation, fileName );

                    this.ReloadFile( fileName, changedValues );
                }
            }
            catch ( Exception e )
            {
                this.Logger.LogException( e );
            }

            foreach ( var changedValue in changedValues )
            {
                this.RaiseConfigurationFileChanged( changedValue );
            }
        }

        /// <summary>
        /// Reloads every configuration file of the cache that is stored at a given path.
        /// </summary>
        /// <param name="fileName">The path of the file that has changed.</param>
        /// <param name="changedValues">The list to which the values whose change must be announced are added.</param>
        private void ReloadFile( string fileName, List<ConfigurationFile> changedValues )
        {
            var types = this._instances.Values
                .Where( s => string.Equals( this.GetFilePath( s.GetType() ), fileName, StringComparison.OrdinalIgnoreCase ) )
                .Select( s => s.GetType() )
                .ToList();

            foreach ( var type in types )
            {
                if ( this.TryLoadConfigurationFile( type, out var newSetting ) )
                {
                    var cachedValue = this.UpdateCacheWithoutEvent( newSetting, out var isChange );

                    if ( isChange )
                    {
                        changedValues.Add( cachedValue );
                    }
                }
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

        /// <summary>
        /// Loads a configuration file, or creates a default instance of it if it does not exist.
        /// </summary>
        /// <param name="type">The type of the configuration file.</param>
        /// <returns>The configuration file.</returns>
        private ConfigurationFile LoadOrCreate( Type type )
        {
            this.Logger.Trace?.Log( $"Loading configuration {type.Name} from file." );

            if ( this.TryLoadConfigurationFile( type, out var value ) )
            {
                return value;
            }

            var settingsObject = Activator.CreateInstance( type )
                                 ?? throw new InvalidOperationException( $"Failed to create instance of '{type.FullName}' type." );

            return (ConfigurationFile) settingsObject;
        }

        /// <inheritdoc />
        /// <remarks>
        /// This method takes no lock, which is what keeps a read off the critical path of every other operation.
        /// See the remarks of <see cref="ConfigurationManager"/> for what makes that safe.
        /// </remarks>
        public ConfigurationFile Get( Type type, bool ignoreCache = false )
        {
            if ( !ignoreCache && this._instances.TryGetValue( type, out var cachedValue ) )
            {
                return cachedValue;
            }

            var loadedValue = this.LoadOrCreate( type );

            var newCachedValue = this.UpdateCacheWithoutEvent( loadedValue, out var isChange );

            if ( isChange )
            {
                this.RaiseConfigurationFileChanged( newCachedValue );
            }

            // A caller that asked to ignore the cache receives what was on disk, even when the cache turns out to
            // hold something more recent, because that is what it asked for and what it will pass back as the
            // expected timestamp of an update.
            return ignoreCache ? loadedValue : newCachedValue;
        }

        public event Action<ConfigurationFile>? ConfigurationFileChanged;

        /// <summary>
        /// Announces a change of a configuration file to the subscribers of <see cref="ConfigurationFileChanged"/>.
        /// </summary>
        /// <param name="value">The new value of the configuration file.</param>
        /// <remarks>
        /// <para>
        /// This method must only be called once every lock has been released. A handler is free to read and to
        /// update any configuration file, including the one it is being notified about, and a lock of this class is
        /// not reentrant, so dispatching while holding one would deadlock.
        /// </para>
        /// <para>
        /// The handlers are invoked one by one rather than through the multicast delegate, because a multicast
        /// invocation stops at the first handler that throws, so one faulty subscriber would silently deprive every
        /// subsequent one of the notification.
        /// </para>
        /// <para>
        /// The guarantee a handler receives is the weak one: a notification means that the file has changed and is
        /// worth re-reading, not that <paramref name="value"/> is its current content. Dispatching outside the lock
        /// is what makes the strong guarantee unachievable, and it is the price of never holding a lock across
        /// arbitrary user code.
        /// </para>
        /// </remarks>
        private void RaiseConfigurationFileChanged( ConfigurationFile value )
        {
            var handlers = this.ConfigurationFileChanged;

            if ( handlers == null )
            {
                return;
            }

            this.SyncPoint( RaiseChangedBeforeInvokeLocation, this.GetFilePath( value.GetType() ) );

            foreach ( var handler in handlers.GetInvocationList() )
            {
                try
                {
                    ((Action<ConfigurationFile>) handler).Invoke( value );
                }
                catch ( Exception e )
                {
                    this.Logger.LogException( e, $"Error in a handler of {nameof(this.ConfigurationFileChanged)}" );
                }
            }
        }

        /// <summary>
        /// Determines whether a configuration file supersedes the one currently cached.
        /// </summary>
        /// <param name="oldValue">The value currently cached.</param>
        /// <param name="newValue">The candidate value.</param>
        /// <returns><see langword="true"/> if <paramref name="newValue"/> is the more recent of the two.</returns>
        /// <remarks>
        /// A value that has no timestamp is a default instance, created because the file did not exist or could not
        /// be read, and never supersedes a value that was actually read.
        /// </remarks>
        private static bool IsNewer( ConfigurationFile oldValue, ConfigurationFile newValue )
        {
            if ( newValue.Timestamp == null )
            {
                return false;
            }

            if ( oldValue.Timestamp == null )
            {
                return true;
            }

            return oldValue.Timestamp.Value.IsOlderThan( newValue.Timestamp.Value );
        }

        /// <summary>
        /// Stores a configuration file in the cache unless the cache already holds a more recent one, and reports
        /// whether the change must be announced.
        /// </summary>
        /// <param name="newValue">The value to store.</param>
        /// <param name="isChange">
        /// At output, whether the value that is now cached differs from the one that was cached before, other than
        /// by its version.
        /// </param>
        /// <returns>The value that is cached when this method returns, which is not necessarily <paramref name="newValue"/>.</returns>
        /// <remarks>
        /// The cache only ever moves forward. Without that, two readers that loaded two versions of the same file
        /// could store them in the reverse order, and the cache would then hold the older one indefinitely. This is
        /// what allows a read to take no lock at all, and what makes a second pass over a file that has already
        /// been reloaded a no-op.
        /// </remarks>
        private ConfigurationFile UpdateCacheWithoutEvent( ConfigurationFile newValue, out bool isChange )
        {
            var type = newValue.GetType();

            while ( true )
            {
                if ( !this._instances.TryGetValue( type, out var oldValue ) )
                {
                    if ( this._instances.TryAdd( type, newValue ) )
                    {
                        // There was nothing to change from, so the first value of a file is never announced.
                        isChange = false;

                        return newValue;
                    }

                    continue;
                }

                if ( !IsNewer( oldValue, newValue ) )
                {
                    isChange = false;

                    return oldValue;
                }

                // The comparison excludes the version, so that a file rewritten with the same content is not
                // announced as a change.
                var isStructuralChange = !this.StructurallyEquals( oldValue, newValue );

                this.SyncPoint( AddToCacheBeforeSwapLocation, this.GetFilePath( type ) );

                if ( this._instances.TryUpdate( type, newValue, oldValue ) )
                {
                    isChange = isStructuralChange;

                    return newValue;
                }
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

        /// <inheritdoc />
        public ConfigurationUpdateOutcome Update( Type type, Func<ConfigurationFile, ConfigurationFile?> transform )
        {
            var fileName = this.GetFilePath( type );

            if ( _fileBeingUpdatedByCurrentThread != null )
            {
                // Refused rather than allowed to nest, because the alternative is a deadlock between two processes
                // that is neither reproducible nor diagnosable. A transformation may read any configuration file,
                // which takes no lock, but it may not update one.
                throw new InvalidOperationException(
                    $"Cannot update '{fileName}' from within the transformation of '{_fileBeingUpdatedByCurrentThread}'. "
                    + "A transformation runs while the lock protecting its own file is held, and the locks of this class are not reentrant." );
            }

            ConfigurationFile? valueToAnnounce = null;

            try
            {
                if ( !this.TryAcquireLock( fileName, "updating it", out var releaser ) )
                {
                    return ConfigurationUpdateOutcome.LockTimeout;
                }

                using ( releaser )
                {
                    return this.UpdateWithinLock( type, fileName, transform, ref valueToAnnounce );
                }
            }
            finally
            {
                // Outside the using, and therefore outside the lock: see the remarks of
                // RaiseConfigurationFileChanged.
                if ( valueToAnnounce != null )
                {
                    this.RaiseConfigurationFileChanged( valueToAnnounce );
                }
            }
        }

        /// <summary>
        /// Performs the part of <see cref="Update"/> that runs while the lock protecting the file is held.
        /// </summary>
        /// <param name="type">The type of the configuration file.</param>
        /// <param name="fileName">The path of the configuration file.</param>
        /// <param name="transform">Produces the new content of the file from its current content.</param>
        /// <param name="valueToAnnounce">The value whose change the caller must announce once the lock is released.</param>
        /// <returns>What happened.</returns>
        private ConfigurationUpdateOutcome UpdateWithinLock(
            Type type,
            string fileName,
            Func<ConfigurationFile, ConfigurationFile?> transform,
            ref ConfigurationFile? valueToAnnounce )
        {
            this.Logger.Trace?.Log( $"Updating '{fileName}'." );

            // The file is read here rather than taken from the caller, which is what makes this a transaction:
            // there is no window between the read and the write in which another writer could intervene, so there
            // is nothing for the caller to compare against and nothing to retry.
            var currentValue = this.LoadOrCreate( type );

            var cachedCurrentValue = this.UpdateCacheWithoutEvent( currentValue, out var isCurrentValueChange );

            if ( isCurrentValueChange )
            {
                valueToAnnounce = cachedCurrentValue;
            }

            this.SyncPoint( UpdateAfterReadLocation, fileName );

            ConfigurationFile? newValue;

            // The marker covers exactly the transformation, and not the dispatch of the event that follows the
            // release of the lock: a handler is free to update whatever it likes, precisely because it holds
            // nothing when it runs.
            _fileBeingUpdatedByCurrentThread = fileName;

            try
            {
                newValue = transform( currentValue );
            }
            finally
            {
                _fileBeingUpdatedByCurrentThread = null;
            }

            if ( newValue == null )
            {
                this.Logger.Trace?.Log( $"Update of '{fileName}' declined because the file did not need to be updated." );

                return ConfigurationUpdateOutcome.Declined;
            }

            if ( currentValue.Timestamp != null && newValue.Equals( currentValue ) )
            {
                this.Logger.Trace?.Log( $"Update of '{fileName}' skipped because no change was required." );

                return ConfigurationUpdateOutcome.NoChange;
            }

            // The version is incremented from the value that was just read, and not from the value the
            // transformation produced, because the version counts the writes made to the file and must therefore
            // be a property of the file rather than of what a caller happens to return. A transformation that
            // builds a fresh instance, as resetting a configuration file to its default content does, otherwise
            // takes the version back to one, and the cache then declines the new content as older than what it
            // holds whenever the two share a modification time.
            //
            // The increment is applied to a copy rather than to the value the transformation produced, so that a
            // write that does not happen leaves nothing behind. The previous implementation incremented the
            // caller's own record before writing, so a declined attempt left the caller one version ahead of the
            // file, and the retry that followed incremented it again.
            var valueToWrite = newValue with { Version = (currentValue.Version ?? 0) + 1 };

            var json = this._jsonSerializationService.Serialize( valueToWrite, type );

            this.SyncPoint( UpdateBeforeWriteLocation, fileName );

            try
            {
                this._fileSystem.WriteAllTextAtomically( fileName, json );
            }
            catch ( Exception e )
            {
                // The write is atomic, so the previous content of the file is intact and the cache still describes
                // it. Reporting the failure is all there is to do: a configuration that could not be written must
                // not fail the operation that wanted to write it.
                this.Logger.LogException( e, $"Cannot write '{fileName}'" );

                return ConfigurationUpdateOutcome.WriteFailed;
            }

            var newLastModified = this._fileSystem.GetFileLastWriteTime( fileName );
            valueToWrite.SetFileSystemTimestamp( newLastModified );

            var cachedNewValue = this.UpdateCacheWithoutEvent( valueToWrite, out var isChange );

            if ( isChange )
            {
                valueToAnnounce = cachedNewValue;
            }

            this.Logger.Trace?.Log( $"File '{fileName}' updated. The new timestamp is '{valueToWrite.Timestamp}'." );

            this.SyncPoint( UpdateBeforeUnlockLocation, fileName );

            return ConfigurationUpdateOutcome.Updated;
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

        /// <summary>
        /// Reads a configuration file and gives the result the timestamp the file had.
        /// </summary>
        /// <param name="type">The type of the configuration file.</param>
        /// <param name="settings">At output, the configuration file that was read.</param>
        /// <returns><see langword="true"/> if the file exists and could be read.</returns>
        /// <remarks>
        /// The modification time is read before the content on purpose. A writer that substitutes the file between
        /// the two makes the result carry a timestamp older than its content, which is harmless: the value is then
        /// superseded by the next read, and an update that quotes that timestamp is declined rather than applied to
        /// the wrong base. Reading the time afterwards would produce the opposite error, in which stale content
        /// carries a current timestamp, and that one is not recoverable.
        /// </remarks>
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
        /// Gets the lock protecting one configuration file, creating it on the first use.
        /// </summary>
        /// <param name="fileName">The path of the configuration file.</param>
        /// <returns>The lock.</returns>
        /// <remarks>
        /// The lock is named after the path of the file and not after its type, because the same file is
        /// represented by a different type in each copy of the declaring assembly that is loaded, and because two
        /// distinct types can be stored in the same file.
        /// </remarks>
        private INamedLock GetLock( string fileName )
        {
            // A monitor rather than GetOrAdd, whose factory can run twice and would then create, and leak, a
            // second operating system object.
            lock ( this._locksSync )
            {
                // Checked under the same monitor as the disposal, so that a lock created here can never be added
                // to a table that Dispose has already emptied, which would leak the operating system object and
                // give the caller a lock nobody will ever dispose.
                if ( this._isDisposed != 0 )
                {
                    throw new ObjectDisposedException( nameof(ConfigurationManager) );
                }

                if ( !this._locks.TryGetValue( fileName, out var namedLock ) )
                {
                    namedLock = this._lockService.GetGlobalLock( fileName );
                    this._locks.Add( fileName, namedLock );
                }

                return namedLock;
            }
        }

        /// <summary>
        /// Acquires the lock protecting one configuration file.
        /// </summary>
        /// <param name="fileName">The path of the configuration file.</param>
        /// <param name="operation">What the lock is being acquired for, used in the warning.</param>
        /// <param name="releaser">At output, an object that releases the lock when it is disposed.</param>
        /// <returns><see langword="true"/> if the lock was acquired.</returns>
        /// <remarks>
        /// This method never throws. Failing to write a configuration file must never fail a compilation, so the
        /// caller degrades instead: an update is declined, and its caller retries or gives up. The implementation
        /// this one replaces threw a <see cref="TimeoutException"/>, which is issue 1847 as it was reported.
        /// </remarks>
        private bool TryAcquireLock( string fileName, string operation, [NotNullWhen( true )] out IDisposable? releaser )
        {
            try
            {
                if ( this.GetLock( fileName ).TryAcquire( _lockTimeout, out releaser ) )
                {
                    return true;
                }

                this.ReportLockFailure( fileName, $"Timeout while waiting {_lockTimeout.TotalSeconds} s for the lock protecting '{fileName}' before {operation}." );

                return false;
            }
            catch ( Exception e )
            {
                this.Logger.LogException( e, $"Cannot acquire the lock protecting '{fileName}' before {operation}" );

                releaser = null;

                return false;
            }
        }

        /// <summary>
        /// Reports that a file could not be locked, at most once per file and per <see cref="_lockWarningPeriod"/>.
        /// </summary>
        /// <param name="fileName">The path of the configuration file.</param>
        /// <param name="message">The message to report.</param>
        /// <remarks>
        /// On a machine where the condition holds it holds for every operation, so an unthrottled warning would
        /// produce one entry per configuration read or write of every build.
        /// </remarks>
        private void ReportLockFailure( string fileName, string message )
        {
            var now = this._dateTimeProvider.UtcNow;
            var lastWarning = this._lastLockWarnings.GetOrAdd( fileName, DateTime.MinValue );

            if ( now - lastWarning >= _lockWarningPeriod && this._lastLockWarnings.TryUpdate( fileName, now, lastWarning ) )
            {
                this.Logger.Warning?.Log( message );
            }
            else
            {
                this.Logger.Trace?.Log( message );
            }
        }

        public void Dispose()
        {
            if ( Interlocked.Exchange( ref this._isDisposed, 1 ) != 0 )
            {
                return;
            }

            // The watcher goes first, so that no further processing is started while the locks are being disposed.
            this._fileSystemWatcher?.Dispose();

            lock ( this._locksSync )
            {
                foreach ( var namedLock in this._locks.Values )
                {
                    namedLock.Dispose();
                }

                this._locks.Clear();
            }
        }
    }
}
