// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.DesignTime.Utilities;

/// <summary>
/// Variation of <see cref="FileSystemWatcher"/> that can handle missing parent directories.
/// </summary>
internal sealed class RecursiveFileSystemWatcher : IDisposable
{
    // Ensures that _watcher and EnableRaisingEvents are synchronized.
    private readonly object _lock = new();

    private readonly string _path;

    private readonly string _filter;

    /// <summary>
    /// Receives the exceptions thrown on the thread pool threads of <see cref="FileSystemWatcher"/>, where an exception would
    /// terminate the process. See #2317.
    /// </summary>
    private readonly Action<Exception> _onException;

    private FileSystemWatcher? _watcher;

    private RecursiveFileSystemWatcher? _parentDirectoryWatcher;

    private bool _enableRaisingEvents;

    public bool EnableRaisingEvents
    {
        get => this._enableRaisingEvents;
        set
        {
            lock ( this._lock )
            {
                this._enableRaisingEvents = value;

                if ( this._watcher != null )
                {
                    this._watcher.EnableRaisingEvents = value;
                }
            }
        }
    }

    public event FileSystemEventHandler? Changed;

    public event FileSystemEventHandler? Created;

    /// <param name="path">The directory to watch, which may not exist yet.</param>
    /// <param name="filter">The filter of the file names.</param>
    /// <param name="onException">
    /// Receives the exceptions thrown by the event handlers and by the watcher itself. They are never rethrown, because
    /// they are raised on thread pool threads.
    /// </param>
    public RecursiveFileSystemWatcher( string path, string filter, Action<Exception> onException )
    {
        this._path = path ?? throw new ArgumentNullException( nameof(path) );
        this._filter = filter;
        this._onException = onException;

        if ( Directory.Exists( path ) )
        {
            this.CreateWatcher();
        }
        else
        {
            var parentDirectory = Path.GetDirectoryName( path );

            if ( parentDirectory == null )
            {
                // Path.GetDirectoryName returns null only when the path is a root. This branch runs only when Directory.Exists
                // returned false, so the path is a root that does not exist, for instance a disconnected drive. It cannot be
                // watched, so no watcher is registered and no event is ever raised.
                return;
            }

            this._parentDirectoryWatcher = new RecursiveFileSystemWatcher( parentDirectory, Path.GetFileName( path ), onException );

            void OnParentDirectoryCreated( object? s, FileSystemEventArgs? e )
            {
                try
                {
                    if ( Directory.Exists( path ) )
                    {
                        this.CreateWatcher();
                        this._parentDirectoryWatcher?.Dispose();
                        this._parentDirectoryWatcher = null;

                        if ( this.EnableRaisingEvents )
                        {
                            // If files were created in the directory before the watcher was created, we need to raise the events for them.
                            foreach ( var file in Directory.EnumerateFileSystemEntries( path, filter ) )
                            {
                                this.Created?.Invoke( this, new FileSystemEventArgs( WatcherChangeTypes.Created, path, file ) );
                                this.Changed?.Invoke( this, new FileSystemEventArgs( WatcherChangeTypes.Changed, path, file ) );
                            }
                        }
                    }
                }
                catch ( DirectoryNotFoundException )
                {
                    // The directory may be deleted in the meantime.
                }
                catch ( Exception exception )
                {
                    // This handler runs on a thread pool thread, where an exception would terminate the process. See #2317.
                    this.ReportException( exception );
                }
            }

            this._parentDirectoryWatcher.Created += OnParentDirectoryCreated;
            this._parentDirectoryWatcher.EnableRaisingEvents = true;

            // If the parent directory was created after we checked whether it exists and before we started watching it, we need to check it again.
            OnParentDirectoryCreated( null, null );
        }
    }

    private void CreateWatcher()
    {
        var watcher = new FileSystemWatcher( this._path, this._filter );

        watcher.Changed += ( _, e ) => this.RaiseEvent( this.Changed, e );
        watcher.Created += ( _, e ) => this.RaiseEvent( this.Created, e );

        lock ( this._lock )
        {
            watcher.EnableRaisingEvents = this.EnableRaisingEvents;

            this._watcher = watcher;
        }
    }

    private void RaiseEvent( FileSystemEventHandler? handler, FileSystemEventArgs e )
    {
        // The events of FileSystemWatcher are raised on a thread pool thread, where an exception would terminate the
        // process. See #2317.
        try
        {
            handler?.Invoke( this, e );
        }
        catch ( Exception exception )
        {
            this.ReportException( exception );
        }
    }

    private void ReportException( Exception exception )
    {
        try
        {
            this._onException( exception );
        }
        catch
        {
            // The reporter must not make the process terminate either.
        }
    }

    public void Dispose()
    {
        this._watcher?.Dispose();
        this._parentDirectoryWatcher?.Dispose();

        this.Changed = null;
        this.Created = null;
    }
}