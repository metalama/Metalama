// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Metalama.Backstage.Utilities;

// ReSharper disable StringLiteralTypo
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable InconsistentNaming
#pragma warning disable SA1310, IDE1006 // Naming conventions.

internal sealed class ParentProcessSearchWindows : ParentProcessSearchBase<IntPtr>
{
    public ParentProcessSearchWindows( ILogger logger ) : base( logger ) { }
    
    [DllImport( "kernel32" )]
    private static extern IntPtr GetCurrentProcess();
    
    [StructLayout( LayoutKind.Sequential )]
    private struct PROCESS_BASIC_INFORMATION
    {
        public IntPtr Reserved1;
        public IntPtr PebBaseAddress;
        public IntPtr Reserved2_0;
        public IntPtr Reserved2_1;
        public IntPtr UniqueProcessId;
        public IntPtr InheritedFromUniqueProcessId;
    }

    [DllImport( "ntdll" )]
    private static extern int NtQueryInformationProcess(
        IntPtr processHandle,
        int processInformationClass,
        ref PROCESS_BASIC_INFORMATION processInformation,
        int processInformationLength,
        out int returnLength );

    [DllImport( "kernel32" )]
    private static extern IntPtr OpenProcess( uint dwDesiredAccess, bool bInheritHandle, int dwProcessId );

    [DllImport( "kernel32" )]
    private static extern bool CloseHandle( IntPtr hObject );

    [DllImport( "psapi", CharSet = CharSet.Unicode )]
    private static extern int GetProcessImageFileName(
        IntPtr hProcess,
        [Out] [MarshalAs( UnmanagedType.LPWStr )]
        StringBuilder lpImageFileName,
        int nSize );

    private const uint PROCESS_QUERY_INFORMATION = 0x0400;

    protected override bool IsNull( IntPtr handle ) => handle == IntPtr.Zero;

    protected override IntPtr GetCurrentProcessHandle() => GetCurrentProcess();

    protected override (string? ImageName, int CurrentProcessId, IntPtr ParentProcessHandle) GetProcessInfo( IntPtr hProcess )
    {
        string? imageName = null;
        var stringBuilder = new StringBuilder( 1024 );

        if ( GetProcessImageFileName( hProcess, stringBuilder, 1024 ) != 0 )
        {
            imageName = stringBuilder.ToString();
        }

        var pbi = default(PROCESS_BASIC_INFORMATION);
        var status = NtQueryInformationProcess( hProcess, 0, ref pbi, Marshal.SizeOf( pbi ), out _ );

        if ( status != 0 )
        {
            throw new Win32Exception( status );
        }

        int parentProcessId;
        int processId;

        try
        {
            parentProcessId = pbi.InheritedFromUniqueProcessId.ToInt32();
        }
        catch ( ArgumentException )
        {
            // not found
            parentProcessId = 0;
        }

        try
        {
            processId = pbi.UniqueProcessId.ToInt32();
        }
        catch ( ArgumentException )
        {
            // not found
            processId = 0;
        }
        
        var hParentProcess = parentProcessId != 0 ? OpenProcess( PROCESS_QUERY_INFORMATION, true, parentProcessId ) : IntPtr.Zero;

        return (imageName, processId, hParentProcess);
    }

    protected override void CloseProcessHandle( IntPtr hProcess ) => CloseHandle( hProcess );
}