// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local

#pragma warning disable IDE1006 // Inconsistent naming
#pragma warning disable SA1310

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace Metalama.Backstage.Utilities;

public class LockingProcessDetector : ILockingProcessDetector
{
    public static bool IsSupported => RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) && Environment.OSVersion.Version.Major >= 6;

    [Obfuscation( Exclude = false )]
    public IReadOnlyList<Process> GetProcessesUsingFiles( IReadOnlyList<string> filePaths )
    {
        if ( !IsSupported )
        {
            return Array.Empty<Process>();
        }

        var processes = new List<Process>();

        // Create a restart manager session
        var rv = RmStartSession( out var sessionHandle, 0, Guid.NewGuid().ToString( "N", CultureInfo.InvariantCulture ) );

        if ( rv != 0 )
        {
            return Array.Empty<Process>();
        }

        try
        {
            // Let the restart manager know what files we're interested in
            var pathStrings = filePaths.ToArray();

            rv = RmRegisterResources(
                sessionHandle,
                (uint) pathStrings.Length,
                pathStrings,
                0,
                null,
                0,
                null );

            if ( rv != 0 )
            {
                return Array.Empty<Process>();
            }

            // Ask the restart manager what other applications are using those files
            const int ERROR_MORE_DATA = 234;
            uint procInfo = 0, rebootReasons = RmRebootReasonNone;
            rv = RmGetList( sessionHandle, out var pnProcInfoNeeded, ref procInfo, null, ref rebootReasons );

            while ( rv == ERROR_MORE_DATA )
            {
                // Create an array to store the process results
                var processInfo = new RM_PROCESS_INFO[pnProcInfoNeeded];
                procInfo = (uint) processInfo.Length;

                // Get the list.  
                rv = RmGetList( sessionHandle, out pnProcInfoNeeded, ref procInfo, processInfo, ref rebootReasons );

                if ( rv == 0 )
                {
                    processes.Clear();

                    // Enumerate all of the results and add them to the list to be returned
                    for ( var i = 0; i < procInfo; i++ )
                    {
                        try
                        {
                            processes.Add( Process.GetProcessById( processInfo[i].Process.ProcessId ) );
                        }
                        catch ( ArgumentException ) { } // in case process is no longer running
                    }
                }
            }

            if ( rv != 0 )
            {
                return Array.Empty<Process>();
            }
        }
        finally
        {
            // We ignore the error here because anyway there is nothing we can do about it.
            _ = RmEndSession( sessionHandle );
        }

        return processes;
    }

    [DllImport( "rstrtmgr.dll", CharSet = CharSet.Unicode )]
    private static extern int RmStartSession( out uint pSessionHandle, int dwSessionFlags, string strSessionKey );

    [DllImport( "rstrtmgr.dll" )]
    private static extern int RmEndSession( uint pSessionHandle );

    [DllImport( "rstrtmgr.dll", CharSet = CharSet.Unicode )]
    private static extern int RmRegisterResources(
        uint pSessionHandle,
        uint nFiles,
        string[]? rgsFilenames,
        uint nApplications,
        RM_UNIQUE_PROCESS[]? rgApplications,
        uint nServices,
        string[]? rgsServiceNames );

    [DllImport( "rstrtmgr.dll" )]
    private static extern int RmGetList(
        uint dwSessionHandle,
        out uint procInfoNeeded,
        ref uint procInfo,
        [In] [Out] RM_PROCESS_INFO[]? rgAffectedApps,
        ref uint rebootReasons );

    private const int RmRebootReasonNone = 0;
    private const int CCH_RM_MAX_APP_NAME = 255;
    private const int CCH_RM_MAX_SVC_NAME = 63;

    [StructLayout( LayoutKind.Sequential )]
    private struct RM_UNIQUE_PROCESS
    {
        public int ProcessId;
        public FILETIME ProcessStartTime;
    }

    [StructLayout( LayoutKind.Sequential, CharSet = CharSet.Unicode )]
    private struct RM_PROCESS_INFO
    {
        public RM_UNIQUE_PROCESS Process;

        [MarshalAs( UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1 )]
        public string AppName;

        [MarshalAs( UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1 )]
        public string ServiceShortName;

        public RM_APP_TYPE ApplicationType;
        public uint AppStatus;
        public uint TSSessionId;

        [MarshalAs( UnmanagedType.Bool )]
        public bool Restartable;
    }

    private enum RM_APP_TYPE
    {
        RmUnknownApp = 0,
        RmMainWindow = 1,
        RmOtherWindow = 2,
        RmService = 3,
        RmExplorer = 4,
        RmConsole = 5,
        RmCritical = 1000
    }
}