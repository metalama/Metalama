// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// Computes the size of the stack that remains available to the current thread before
/// <see cref="RuntimeHelpers.EnsureSufficientExecutionStack"/> throws an <see cref="InsufficientExecutionStackException"/>.
/// </summary>
/// <remarks>
/// <para>
/// .NET has no public API that returns the bounds of the stack of the current thread. This class queries them from the
/// operating system: <c>GetCurrentThreadStackLimits</c> on Windows, <c>pthread_getattr_np</c> on Linux, and
/// <c>pthread_get_stackaddr_np</c> on macOS. The stack grows towards lower addresses on all these platforms.
/// </para>
/// <para>
/// <see cref="RuntimeHelpers.EnsureSufficientExecutionStack"/> throws when the remaining stack is smaller than a margin
/// that depends on the runtime. On .NET Framework, the margin is half of the size of the stack of the thread. On .NET Core
/// and .NET, the margin is 128 KB in a 64-bit process and 64 KB in a 32-bit process. This class computes the lowest address
/// that the runtime accepts, and <see cref="TryGetAvailableStackSize"/> returns the difference between the address of a
/// local variable and this address.
/// </para>
/// <para>
/// The bounds of the stack of a thread do not change, so they are queried once per thread and stored in a
/// <see cref="ThreadStaticAttribute"/> field. A <see cref="ThreadStaticAttribute"/> field is used instead of a
/// <see cref="System.Threading.ThreadLocal{T}"/> because <see cref="RecursionGuard"/> reads it for every visited node.
/// </para>
/// <para>
/// When the operating system is not supported, or when the query fails, <see cref="TryGetAvailableStackSize"/> returns
/// <c>false</c>. It never throws an exception.
/// </para>
/// </remarks>
internal static class StackLimits
{
    /// <summary>
    /// The value of <see cref="_limitAddress"/> when the bounds of the stack of the current thread cannot be determined.
    /// </summary>
    private const ulong _notAvailable = ulong.MaxValue;

    /// <summary>
    /// The size of the buffer that holds a <c>pthread_attr_t</c> structure on Linux. It is larger than the structure on all
    /// supported architectures (56 bytes on x64, 64 bytes on arm64).
    /// </summary>
    private const int _pthreadAttrBufferSize = 256;

    /// <summary>
    /// Indicates whether the current runtime is .NET Framework, whose margin is half of the size of the stack.
    /// </summary>
    private static readonly bool _isNetFramework =
        RuntimeInformation.FrameworkDescription.StartsWith( ".NET Framework", StringComparison.OrdinalIgnoreCase );

    /// <summary>
    /// The lowest address of the stack of the current thread that <see cref="RuntimeHelpers.EnsureSufficientExecutionStack"/>
    /// accepts, or <c>0</c> when it has not been computed yet on this thread, or <see cref="_notAvailable"/> when it cannot
    /// be determined.
    /// </summary>
    [ThreadStatic]
    private static ulong _limitAddress;

    /// <summary>
    /// Gets the number of bytes of stack that the current thread can use below the frame of the caller before
    /// <see cref="RuntimeHelpers.EnsureSufficientExecutionStack"/> throws an <see cref="InsufficientExecutionStackException"/>.
    /// </summary>
    /// <param name="available">The number of available bytes. It is negative when the stack is already below the limit
    /// of the runtime, and it is <c>0</c> when the method returns <c>false</c>.</param>
    /// <returns><c>true</c> when the bounds of the stack of the current thread are known, otherwise <c>false</c>.</returns>
    public static unsafe bool TryGetAvailableStackSize( out long available )
    {
        var limitAddress = _limitAddress;

        if ( limitAddress == 0 )
        {
            limitAddress = _limitAddress = ComputeLimitAddress();
        }

        if ( limitAddress == _notAvailable )
        {
            available = 0;

            return false;
        }

        byte local = 0;
        var currentAddress = (ulong) &local;

        available = (long) (currentAddress - limitAddress);

        return true;
    }

    /// <summary>
    /// Computes the lowest address of the stack of the current thread that
    /// <see cref="RuntimeHelpers.EnsureSufficientExecutionStack"/> accepts.
    /// </summary>
    /// <returns>The address, or <see cref="_notAvailable"/> when it cannot be determined.</returns>
    private static ulong ComputeLimitAddress()
    {
        if ( !TryQueryBounds( out var lowestAddress, out var size ) || lowestAddress == 0 || size == 0 )
        {
            return _notAvailable;
        }

        var margin = _isNetFramework ? size / 2 : (ulong) (IntPtr.Size == 8 ? 128 * 1024 : 64 * 1024);

        return lowestAddress + margin;
    }

    /// <summary>
    /// Queries the lowest address and the size of the stack of the current thread from the operating system.
    /// </summary>
    private static bool TryQueryBounds( out ulong lowestAddress, out ulong size )
    {
        try
        {
            if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
            {
                Windows.GetBounds( out lowestAddress, out size );

                return true;
            }
            else if ( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) )
            {
                return Linux.TryGetBounds( out lowestAddress, out size );
            }
            else if ( RuntimeInformation.IsOSPlatform( OSPlatform.OSX ) )
            {
                MacOS.GetBounds( out lowestAddress, out size );

                return true;
            }
        }
        catch ( Exception e ) when ( e is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException ) { }

        lowestAddress = 0;
        size = 0;

        return false;
    }

    /// <summary>
    /// Queries the bounds of the stack on Windows.
    /// </summary>
    private static class Windows
    {
        /// <summary>
        /// Gets the lowest address and the size of the stack of the current thread.
        /// </summary>
        public static void GetBounds( out ulong lowestAddress, out ulong size )
        {
            GetCurrentThreadStackLimits( out var lowLimit, out var highLimit );

            lowestAddress = lowLimit.ToUInt64();
            size = highLimit.ToUInt64() - lowestAddress;
        }

        /// <summary>
        /// Retrieves the boundaries of the stack of the current thread. This function is available on Windows 8 and later.
        /// </summary>
        [DllImport( "kernel32.dll" )]
        private static extern void GetCurrentThreadStackLimits( out UIntPtr lowLimit, out UIntPtr highLimit );
    }

    /// <summary>
    /// Queries the bounds of the stack on Linux.
    /// </summary>
    /// <remarks>
    /// The <c>pthread</c> functions are exported by <c>libc.so.6</c> since glibc 2.34. Before that version, some of them
    /// are exported only by <c>libpthread.so.0</c>. On musl, they are exported by the C library, whose name differs between
    /// distributions but which the runtime resolves from the name <c>libc</c>. The libraries are therefore tried in that order.
    /// </remarks>
    private static class Linux
    {
        /// <summary>
        /// Gets the lowest address and the size of the stack of the current thread.
        /// </summary>
        /// <returns><c>true</c> when the bounds were determined, otherwise <c>false</c>.</returns>
        public static bool TryGetBounds( out ulong lowestAddress, out ulong size )
        {
            try
            {
                return GlibcLibC.TryGetBounds( out lowestAddress, out size );
            }
            catch ( Exception e ) when ( e is DllNotFoundException or EntryPointNotFoundException ) { }

            try
            {
                return GlibcLibPthread.TryGetBounds( out lowestAddress, out size );
            }
            catch ( Exception e ) when ( e is DllNotFoundException or EntryPointNotFoundException ) { }

            return MuslLibC.TryGetBounds( out lowestAddress, out size );
        }

        /// <summary>
        /// Gets the bounds of the stack of the current thread with <c>pthread_getattr_np</c> and <c>pthread_attr_getstack</c>,
        /// using the given imported functions.
        /// </summary>
        private static unsafe bool TryGetBounds(
            Func<UIntPtr> pthreadSelf,
            PthreadGetAttr pthreadGetAttr,
            PthreadAttrGetStack pthreadAttrGetStack,
            PthreadAttrDestroy pthreadAttrDestroy,
            out ulong lowestAddress,
            out ulong size )
        {
            lowestAddress = 0;
            size = 0;

            var attr = stackalloc byte[_pthreadAttrBufferSize];

            if ( pthreadGetAttr( pthreadSelf(), attr ) != 0 )
            {
                return false;
            }

            try
            {
                if ( pthreadAttrGetStack( attr, out var stackAddress, out var stackSize ) != 0 )
                {
                    return false;
                }

                lowestAddress = stackAddress.ToUInt64();
                size = stackSize.ToUInt64();

                return true;
            }
            finally
            {
                _ = pthreadAttrDestroy( attr );
            }
        }

        private unsafe delegate int PthreadGetAttr( UIntPtr thread, byte* attr );

        private unsafe delegate int PthreadAttrGetStack( byte* attr, out UIntPtr stackAddress, out UIntPtr stackSize );

        private unsafe delegate int PthreadAttrDestroy( byte* attr );

        /// <summary>
        /// Imports the <c>pthread</c> functions from <c>libc.so.6</c> (glibc 2.34 and later).
        /// </summary>
        private static unsafe class GlibcLibC
        {
            private const string _library = "libc.so.6";

            public static bool TryGetBounds( out ulong lowestAddress, out ulong size )
                => Linux.TryGetBounds( pthread_self, pthread_getattr_np, pthread_attr_getstack, pthread_attr_destroy, out lowestAddress, out size );

            [DllImport( _library )]
            private static extern UIntPtr pthread_self();

            [DllImport( _library )]
            private static extern int pthread_getattr_np( UIntPtr thread, byte* attr );

            [DllImport( _library )]
            private static extern int pthread_attr_getstack( byte* attr, out UIntPtr stackAddress, out UIntPtr stackSize );

            [DllImport( _library )]
            private static extern int pthread_attr_destroy( byte* attr );
        }

        /// <summary>
        /// Imports the <c>pthread</c> functions from <c>libpthread.so.0</c> (glibc before 2.34).
        /// </summary>
        private static unsafe class GlibcLibPthread
        {
            private const string _library = "libpthread.so.0";

            public static bool TryGetBounds( out ulong lowestAddress, out ulong size )
                => Linux.TryGetBounds( pthread_self, pthread_getattr_np, pthread_attr_getstack, pthread_attr_destroy, out lowestAddress, out size );

            [DllImport( _library )]
            private static extern UIntPtr pthread_self();

            [DllImport( _library )]
            private static extern int pthread_getattr_np( UIntPtr thread, byte* attr );

            [DllImport( _library )]
            private static extern int pthread_attr_getstack( byte* attr, out UIntPtr stackAddress, out UIntPtr stackSize );

            [DllImport( _library )]
            private static extern int pthread_attr_destroy( byte* attr );
        }

        /// <summary>
        /// Imports the <c>pthread</c> functions from the C library of musl.
        /// </summary>
        private static unsafe class MuslLibC
        {
            private const string _library = "libc";

            public static bool TryGetBounds( out ulong lowestAddress, out ulong size )
                => Linux.TryGetBounds( pthread_self, pthread_getattr_np, pthread_attr_getstack, pthread_attr_destroy, out lowestAddress, out size );

            [DllImport( _library )]
            private static extern UIntPtr pthread_self();

            [DllImport( _library )]
            private static extern int pthread_getattr_np( UIntPtr thread, byte* attr );

            [DllImport( _library )]
            private static extern int pthread_attr_getstack( byte* attr, out UIntPtr stackAddress, out UIntPtr stackSize );

            [DllImport( _library )]
            private static extern int pthread_attr_destroy( byte* attr );
        }
    }

    /// <summary>
    /// Queries the bounds of the stack on macOS.
    /// </summary>
    private static class MacOS
    {
        private const string _library = "/usr/lib/libSystem.dylib";

        /// <summary>
        /// Gets the lowest address and the size of the stack of the current thread.
        /// </summary>
        /// <remarks>
        /// <c>pthread_get_stackaddr_np</c> returns the highest address of the stack, so the lowest address is the highest
        /// address minus the size of the stack.
        /// </remarks>
        public static void GetBounds( out ulong lowestAddress, out ulong size )
        {
            var thread = pthread_self();
            var highestAddress = pthread_get_stackaddr_np( thread ).ToUInt64();
            size = pthread_get_stacksize_np( thread ).ToUInt64();
            lowestAddress = highestAddress > size ? highestAddress - size : 0;
        }

        [DllImport( _library )]
        private static extern UIntPtr pthread_self();

        [DllImport( _library )]
        private static extern UIntPtr pthread_get_stackaddr_np( UIntPtr thread );

        [DllImport( _library )]
        private static extern UIntPtr pthread_get_stacksize_np( UIntPtr thread );
    }
}
