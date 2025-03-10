// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Caching;
using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.DesignTime.Pipeline.Diff;

internal sealed partial class ProjectVersionProvider
{
    private sealed partial class Implementation
    {
        private sealed class ChangeList
        {
            private CompilationChanges? _nonIncrementalChanges;
            private volatile WeakCache<Compilation, CompilationChanges>? _changes;
            private WeakReference<CompilationChanges>? _lastChange;

            public ProjectVersion ProjectVersion { get; }

            public CompilationChanges NonIncrementalChanges => this._nonIncrementalChanges ??= CompilationChanges.NonIncremental( this.ProjectVersion );

            public ChangeList( ProjectVersion projectVersion )
            {
                this.ProjectVersion = projectVersion;
            }

            public void Add( CompilationChanges changes )
            {
                if ( this._changes == null )
                {
                    Interlocked.CompareExchange( ref this._changes, new WeakCache<Compilation, CompilationChanges>(), null );
                }

                this._changes.TryAdd( changes.NewProjectVersion.Compilation, changes );
                this._lastChange = new WeakReference<CompilationChanges>( changes );
            }

            public bool TryGetIncrementalChanges( Compilation newCompilation, [NotNullWhen( true )] out CompilationChanges? changes )
            {
                if ( this._changes?.TryGetValue( newCompilation, out changes ) == true )
                {
                    return true;
                }
                else
                {
                    changes = null;

                    return false;
                }
            }

            public CompilationChanges? LastChanges
            {
                get
                {
                    if ( this._lastChange?.TryGetTarget( out var changes ) == true )
                    {
                        return changes;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }
    }
}