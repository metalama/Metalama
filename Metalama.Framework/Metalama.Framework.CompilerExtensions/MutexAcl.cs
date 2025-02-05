// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using JetBrains.Annotations;
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

// ReSharper disable TooWideLocalVariableScope
// ReSharper disable InconsistentNaming
// ReSharper disable MemberHidesStaticFromOuterClass

namespace Metalama.Framework.Threading;

// This code is mostly taken from https://github.com/dotnet/runtime/blob/770df102/src/libraries/System.Threading.AccessControl/src/System/Threading/MutexAcl.cs.
// The main difference is that Create takes mutexSecurity as an SDDL string instead of a MutexSecurity object.
internal static class MutexAcl
{
    // This SDDL form is created by creating the same MutexSecurity as used by MutexHelper
    // (with .AddAccessRule(new(new SecurityIdentifier(WorldSid, null), Synchronize | Modify, Allow))
    // and then calling GetSecurityDescriptorSddlForm(All).
    public static string? AllowUsingMutexToEveryone => RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ? "D:(A;;0x100001;;;WD)" : null;

    /// <summary>Gets or creates <see cref="Mutex" /> instance, allowing a <paramref name="mutexSecurity" /> to be optionally specified to set it during the mutex creation.</summary>
    /// <param name="initiallyOwned"><see langword="true" /> to give the calling thread initial ownership of the named system mutex if the named system mutex is created as a result of this call; otherwise, <see langword="false" />.</param>
    /// <param name="name">The optional name of the system mutex. If this argument is set to <see langword="null" /> or <see cref="string.Empty" />, a local mutex is created.</param>
    /// <param name="mutexSecurity">The optional mutex access control security to apply.</param>
    /// <returns>An object that represents a system mutex, if named, or a local mutex, if nameless.</returns>
    /// <exception cref="ArgumentException">.NET Framework only: The length of the name exceeds the maximum limit.</exception>
    /// <exception cref="WaitHandleCannotBeOpenedException">A mutex handle with system-wide <paramref name="name" /> cannot be created. A mutex handle of a different type might have the same name.</exception>
    public static unsafe Mutex Create( bool initiallyOwned, string? name, string? mutexSecurity )
    {
        if ( mutexSecurity == null )
        {
            return new Mutex( initiallyOwned, name, out _ );
        }

        var mutexFlags = initiallyOwned ? Interop.Kernel32.CREATE_MUTEX_INITIAL_OWNER : 0;

        fixed ( byte* pSecurityDescriptor = BinaryFormFromSddlForm( mutexSecurity ) )
        {
            var secAttrs = new Interop.Kernel32.SECURITY_ATTRIBUTES
            {
                nLength = (uint) sizeof(Interop.Kernel32.SECURITY_ATTRIBUTES), lpSecurityDescriptor = pSecurityDescriptor
            };

            var handle = Interop.Kernel32.CreateMutexEx(
                (IntPtr) (&secAttrs),
                name,
                mutexFlags,
                (uint) MutexRights.FullControl ); // Equivalent to MUTEX_ALL_ACCESS 

            var errorCode = Marshal.GetLastWin32Error();

            if ( handle.IsInvalid )
            {
                handle.SetHandleAsInvalid();

                switch ( errorCode )
                {
                    case Interop.Errors.ERROR_FILENAME_EXCED_RANGE:
                        throw new ArgumentException( "Mutex name too long", nameof(name) );

                    case Interop.Errors.ERROR_INVALID_HANDLE:
                        throw new WaitHandleCannotBeOpenedException( $"Mutex {name} cannot be opened" );

                    default:
                        throw Marshal.GetExceptionForHR( errorCode ) ?? throw new InvalidOperationException( "Handle was invalid, but there was no error" );
                }
            }

            return CreateAndReplaceHandle( handle );
        }
    }

    private static Mutex CreateAndReplaceHandle( SafeWaitHandle replacementHandle )
    {
        // The value of initiallyOwned should not matter since we are replacing the
        // handle with one from an existing Mutex, and disposing the old one
        // We should only make sure that it is a valid value
        var mutex = new Mutex( initiallyOwned: default );

        var old = mutex.SafeWaitHandle;
        mutex.SafeWaitHandle = replacementHandle;
        old.Dispose();

        return mutex;
    }

    private static byte[] BinaryFormFromSddlForm( string sddlForm )
    {
        if ( sddlForm == null )
        {
            throw new ArgumentNullException( nameof(sddlForm) );
        }

        int error;
        var byteArray = IntPtr.Zero;
        uint byteArraySize = 0;
        byte[]? binaryForm;

        try
        {
            if ( !Interop.Advapi32.ConvertStringSdToSd(
                    sddlForm,
                    SecurityDescriptorRevision,
                    out byteArray,
                    ref byteArraySize ) )
            {
                error = Marshal.GetLastWin32Error();

                switch ( error )
                {
                    case Interop.Errors.ERROR_INVALID_PARAMETER or Interop.Errors.ERROR_INVALID_ACL or Interop.Errors.ERROR_INVALID_SECURITY_DESCR
                        or Interop.Errors.ERROR_UNKNOWN_REVISION:
                        throw new ArgumentException(
                            "Invalid SD SDDL form",
                            nameof(sddlForm) );

                    case Interop.Errors.ERROR_NOT_ENOUGH_MEMORY:
#pragma warning disable CA2201 // Do not raise reserved exception types
                        throw new OutOfMemoryException();
#pragma warning restore CA2201

                    case Interop.Errors.ERROR_INVALID_SID:
                        throw new ArgumentException(
                            "Invalid SID in SDDL string",
                            nameof(sddlForm) );

                    default:
                        {
                            if ( error != Interop.Errors.ERROR_SUCCESS )
                            {
                                Debug.Fail( $"Unexpected error out of Win32.ConvertStringSdToSd: {error}" );

                                throw new Win32Exception( error, $"Unexpected error 0x{error:x8}" );
                            }

                            break;
                        }
                }
            }

            binaryForm = new byte[byteArraySize];

            // Extract the data from the returned pointer

            Marshal.Copy( byteArray, binaryForm, 0, (int) byteArraySize );
        }
        finally
        {
            // Now is a good time to get rid of the returned pointer
            if ( byteArray != IntPtr.Zero )
            {
                Marshal.FreeHGlobal( byteArray );
            }
        }

        return binaryForm;
    }

    private static byte SecurityDescriptorRevision => 1;

    // Derive this list of values from winnt.h and MSDN docs:
    // https://learn.microsoft.com/windows/desktop/sync/synchronization-object-security-and-access-rights

    // In order to call ReleaseMutex, you must have an ACL granting you
    // MUTEX_MODIFY_STATE rights (0x0001).  The other interesting value
    // in a Mutex's ACL is MUTEX_ALL_ACCESS (0x1F0001).
    // You need SYNCHRONIZE to be able to open a handle to a mutex.
    [Flags]
    private enum MutexRights
    {
        FullControl = 0x1F0001
    }

#pragma warning disable SA1307, SA1310, SA1402

    internal static class Interop
    {
        internal static class Advapi32
        {
            [DllImport(
                Libraries.Advapi32,
                EntryPoint = "ConvertStringSecurityDescriptorToSecurityDescriptorW",
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true,
                ExactSpelling = true,
                CharSet = CharSet.Unicode )]
            internal static extern bool ConvertStringSdToSd(
                string stringSd,
                /* DWORD */
                uint stringSdRevision,
                out IntPtr resultSd,
                ref uint resultSdLength );
        }

        internal static class Kernel32
        {
            internal const uint CREATE_MUTEX_INITIAL_OWNER = 0x1;

            [DllImport( Libraries.Kernel32, EntryPoint = "CreateMutexExW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true )]
            internal static extern SafeWaitHandle CreateMutexEx( IntPtr lpMutexAttributes, string? name, uint flags, uint desiredAccess );

            [StructLayout( LayoutKind.Sequential )]
            internal struct SECURITY_ATTRIBUTES
            {
                internal uint nLength;
                internal unsafe void* lpSecurityDescriptor;
                internal BOOL bInheritHandle;
            }
        }

        private static class Libraries
        {
            internal const string Advapi32 = "advapi32.dll";
            internal const string Kernel32 = "kernel32.dll";
        }

        /// <summary>
        /// Blittable version of Windows BOOL type. It is convenient in situations where
        /// manual marshalling is required, or to avoid overhead of regular bool marshalling.
        /// </summary>
        /// <remarks>
        /// Some Windows APIs return arbitrary integer values although the return type is defined
        /// as BOOL. It is best to never compare BOOL to TRUE. Always use bResult != BOOL.FALSE
        /// or bResult == BOOL.FALSE .
        /// </remarks>
        [PublicAPI]
        internal enum BOOL
        {
            FALSE = 0,
            TRUE = 1
        }

        // As defined in winerror.h and https://learn.microsoft.com/windows/win32/debug/system-error-codes
        internal static class Errors
        {
            internal const int ERROR_SUCCESS = 0x0;
            internal const int ERROR_INVALID_HANDLE = 0x6;
            internal const int ERROR_NOT_ENOUGH_MEMORY = 0x8;
            internal const int ERROR_INVALID_PARAMETER = 0x57;
            internal const int ERROR_FILENAME_EXCED_RANGE = 0xCE;
            internal const int ERROR_UNKNOWN_REVISION = 0x519;
            internal const int ERROR_INVALID_ACL = 0x538;
            internal const int ERROR_INVALID_SECURITY_DESCR = 0x53A;
            internal const int ERROR_INVALID_SID = 0x539;
        }
    }
}