// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Metalama.Backstage.Utilities;

#pragma warning disable SA1649 // File name should match first type name
internal abstract class ParentProcessSearchBase<TProcessHandle> : ParentProcessSearchBase
#pragma warning restore SA1649
{
    protected ParentProcessSearchBase( ILogger logger ) : base( logger ) { }

    public override IReadOnlyList<ProcessInfo> GetParentProcesses( ISet<string>? pivots = null )
    {
        var parents = new List<ProcessInfo>();
        var parentIds = new HashSet<int>();
        var currentProcessHandle = this.GetCurrentProcessHandle();
        var isSelf = true;

        while ( !this.IsNull( currentProcessHandle ) )
        {
            var (imageName, currentProcessId, parentProcessHandle) = this.GetProcessInfo( currentProcessHandle );

            var processInfo = new ProcessInfo( currentProcessId, imageName );

            if ( isSelf )
            {
                isSelf = false;
            }
            else
            {
                if ( !parentIds.Add( processInfo.ProcessId ) )
                {
                    // There is a loop.
                    break;
                }

                if ( parents.Count > 64 )
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Cannot have more than 64 parents. Parent processes: {0}.",
                            string.Join( ", ", parents.Select( pi => pi.ProcessId.ToString( CultureInfo.InvariantCulture ) ).ToArray() ) ) );
                }

                parents.Add( processInfo );

                if ( pivots != null && processInfo.ProcessName != null && pivots.Contains( processInfo.ProcessName ) )
                {
                    break;
                }
            }

            this.CloseProcessHandle( currentProcessHandle );

            currentProcessHandle = parentProcessHandle;
        }

        if ( !this.IsNull( currentProcessHandle ) )
        {
            this.CloseProcessHandle( currentProcessHandle );
        }

        return parents;
    }

    protected abstract bool IsNull( TProcessHandle handle );

    protected abstract TProcessHandle GetCurrentProcessHandle();

    protected abstract (string? ImageName, int CurrentProcessId, TProcessHandle ParentProcessHandle) GetProcessInfo( TProcessHandle processHandle );
    
    protected abstract void CloseProcessHandle( TProcessHandle handle );
}