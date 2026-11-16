// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Metalama.Mcp.Worker;

/// <summary>
/// Makes a handle that the worker inherited from the supervisor non-inheritable, so that the processes it starts do not
/// receive it.
/// </summary>
internal static partial class HandleInheritance
{
    private const uint _handleFlagInherit = 1;
    private const int _setFileDescriptorFlags = 2;
    private const int _closeOnExec = 1;

    public static void Disable( SafeHandle handle )
    {
        var success = false;
        handle.DangerousAddRef( ref success );

        try
        {
            if ( OperatingSystem.IsWindows() )
            {
                if ( !SetHandleInformation( handle.DangerousGetHandle(), _handleFlagInherit, 0 ) )
                {
                    throw new Win32Exception( Marshal.GetLastPInvokeError() );
                }
            }
            else if ( OperatingSystem.IsLinux() )
            {
                // fcntl is variadic. On Linux, variadic integer arguments are passed as fixed ones, so it can be called
                // with a fixed signature. On Apple arm64 they are passed on the stack, so macOS is not handled: there,
                // the supervisor still detects the exit of the worker and rejects responses to other requests.
                if ( SetFileDescriptorFlags( (int) handle.DangerousGetHandle(), _setFileDescriptorFlags, _closeOnExec ) == -1 )
                {
                    throw new Win32Exception( Marshal.GetLastPInvokeError() );
                }
            }
        }
        finally
        {
            if ( success )
            {
                handle.DangerousRelease();
            }
        }
    }

    [LibraryImport( "kernel32.dll", SetLastError = true )]
    [return: MarshalAs( UnmanagedType.Bool )]
    private static partial bool SetHandleInformation( IntPtr handle, uint mask, uint flags );

    [LibraryImport( "libc", EntryPoint = "fcntl", SetLastError = true )]
    private static partial int SetFileDescriptorFlags( int fileDescriptor, int command, int argument );
}
