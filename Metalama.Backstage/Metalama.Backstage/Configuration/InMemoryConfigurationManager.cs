// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
public class InMemoryConfigurationManager : IConfigurationManager
{
    private readonly IDateTimeProvider _timeProvider;

    private readonly Dictionary<Type, ConfigurationFile> _files = [];

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

    public void Set( ConfigurationFile file ) => this._files[file.GetType()] = file;

    public ConfigurationFile Get( Type type, bool ignoreCache = false )
    {
        if ( !this._files.TryGetValue( type, out var file ) )
        {
            file = (ConfigurationFile) Activator.CreateInstance( type )!;
        }

        return file;
    }

    public event Action<ConfigurationFile>? ConfigurationFileChanged;

    public bool TryUpdate( ConfigurationFile value, ConfigurationFileTimestamp? expectedTimestamp )
    {
        lock ( this )
        {
            var oldFile = this.Get( value.GetType() );

            if ( oldFile.Timestamp != expectedTimestamp )
            {
                return false;
            }

            value.SetFilesystemTimestamp( this._timeProvider.UtcNow );
            this._files[value.GetType()] = value;
            this.ConfigurationFileChanged?.Invoke( value );

            return true;
        }
    }
}