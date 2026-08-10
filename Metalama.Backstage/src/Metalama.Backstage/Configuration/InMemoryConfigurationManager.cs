// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using System;
using System.Collections.Generic;

namespace Metalama.Backstage.Configuration;

/// <summary>
/// An implementation of <see cref="IConfigurationManager"/> that does not store the files, but keeps them in
/// memory. This implementation is useful to build tests.
/// </summary>
/// <remarks>
/// It must behave like <see cref="ConfigurationManager"/> in everything a test can observe, so it increments the
/// version on every update, dispatches <see cref="ConfigurationFileChanged"/> outside its monitor, and guards every
/// access to its dictionary rather than only the updates.
/// </remarks>
public sealed class InMemoryConfigurationManager : IConfigurationManager
{
    private readonly IDateTimeProvider _timeProvider;

    private readonly Dictionary<Type, ConfigurationFile> _files = [];

    /// <summary>
    /// Guards <see cref="_files"/>. A private object rather than the instance itself, so that a caller holding a
    /// lock on this manager cannot interfere with it.
    /// </summary>
    private readonly object _sync = new();

    public InMemoryConfigurationManager( IServiceProvider serviceProvider, params ConfigurationFile[] files )
    {
        this.Logger = serviceProvider.GetLoggerFactory().GetLogger( "TestConfigurationManager" );
        this._timeProvider = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();

        foreach ( var file in files )
        {
            this.Set( file );
        }
    }

    public void Dispose() { }

    public ILogger Logger { get; }

    public string GetFilePath( string fileName ) => throw new NotSupportedException();

    public string GetFilePath( Type type ) => throw new NotSupportedException();

    [PublicAPI]
    public void Set( ConfigurationFile file )
    {
        lock ( this._sync )
        {
            this._files[file.GetType()] = file;
        }
    }

    public ConfigurationFile Get( Type type, bool ignoreCache = false )
    {
        lock ( this._sync )
        {
            return GetWithinLock( this._files, type );
        }
    }

    /// <summary>
    /// Returns the configuration file of a given type, or a default instance of it, while <see cref="_sync"/> is
    /// held.
    /// </summary>
    /// <param name="files">The dictionary.</param>
    /// <param name="type">The type of the configuration file.</param>
    /// <returns>The configuration file.</returns>
    private static ConfigurationFile GetWithinLock( Dictionary<Type, ConfigurationFile> files, Type type )
        => files.TryGetValue( type, out var file ) ? file : (ConfigurationFile) Activator.CreateInstance( type )!;

    public event Action<ConfigurationFile>? ConfigurationFileChanged;

    /// <inheritdoc />
    public ConfigurationUpdateOutcome Update( Type type, Func<ConfigurationFile, ConfigurationFile?> transform )
    {
        ConfigurationFile? valueToAnnounce = null;
        ConfigurationUpdateOutcome outcome;

        // The same guard as the real implementation, so that a transformation which nests an update fails here as
        // it would there. The monitor below is reentrant, so without it a nested update would silently succeed and
        // a test written against this class would not see the defect.
        ConfigurationUpdateScope.VerifyNotNested( type.Name );

        lock ( this._sync )
        {
            var currentValue = GetWithinLock( this._files, type );

            ConfigurationFile? newValue;

            using ( ConfigurationUpdateScope.Enter( type.Name ) )
            {
                newValue = transform( currentValue );
            }

            if ( newValue == null )
            {
                outcome = ConfigurationUpdateOutcome.Declined;
            }
            else if ( currentValue.Timestamp != null && newValue.Equals( currentValue ) )
            {
                outcome = ConfigurationUpdateOutcome.NoChange;
            }
            else
            {
                // Incremented from the current value and not from the one the transformation produced, for the
                // reason given in ConfigurationManager: the version counts the writes made to the file, so a
                // transformation that builds a fresh instance must not take it back to one.
                var valueToStore = newValue with { Version = (currentValue.Version ?? 0) + 1 };
                valueToStore.SetFileSystemTimestamp( this._timeProvider.UtcNow );
                this._files[type] = valueToStore;

                valueToAnnounce = valueToStore;
                outcome = ConfigurationUpdateOutcome.Updated;
            }
        }

        if ( valueToAnnounce != null )
        {
            // Dispatched one handler at a time, like the real implementation, so that a handler which throws does
            // not deprive the handlers registered after it of the notification.
            ConfigurationUpdateScope.RaiseConfigurationFileChanged( this.ConfigurationFileChanged, valueToAnnounce, this.Logger );
        }

        return outcome;
    }
}
