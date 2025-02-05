// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Diagnostics;

namespace Metalama.Backstage.Diagnostics
{
    internal static class DebuggerHelper
    {
        private static readonly object _attachDebuggerSync = new();
        private static volatile bool _attachDebuggerRequested;

        public static void Launch()
        {
            lock ( _attachDebuggerSync )
            {
                if ( !_attachDebuggerRequested )
                {
                    // We try to request to attach the debugger a single time, even if the user refuses or if the debugger gets
                    // detached. It makes a better debugging experience.
                    _attachDebuggerRequested = true;

                    if ( !Debugger.IsAttached )
                    {
                        Debugger.Launch();
                    }
                }
            }
        }

        public static void Launch( DiagnosticsConfiguration configuration, ProcessKind processKind )
        {
            if ( configuration.Debugging.Processes.TryGetValue( processKind.ToString(), out var enabled ) && enabled )
            {
                Launch();
            }
        }
    }
}