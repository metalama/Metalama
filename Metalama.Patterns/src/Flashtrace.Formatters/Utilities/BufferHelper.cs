// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable CommentTypo

namespace Flashtrace.Formatters.Utilities;

// TODO: Review performance-related #if's, use of PInvoke etc. For now, leaving the apparently most-vanilla branches in place.

[SuppressMessage(
    "StyleCop.CSharp.MaintainabilityRules",
    "SA1405:Debug.Assert should provide message text",
    Justification = "Ported from old code, original intent not known." )]
internal static unsafe class BufferHelper
{
#if WINDOWS_PINVOKE && UNSECURE_PINVOKE
    private static bool? runningOnWindows;

    private static bool RunningOnWindows =>
        runningOnWindows ?? (runningOnWindows =
#if RUNTIME_INFORMATION
            RuntimeInformation.IsOSPlatform( OSPlatform.Windows )).Value;
#else
            Environment.OSVersion.Platform == PlatformID.Win32NT).Value;
#endif
#endif

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static void CopyMemory( void* destination, void* source, int length )
    {
#if MEMORY_COPY
        Buffer.MemoryCopy(source, destination, length, length);
#else
        if ( sizeof(IntPtr) == 4 )
        {
            ManagedMemoryCopy32( (byte*) destination, (byte*) source, (uint) length );
        }
        else
        {
            ManagedMemoryCopy64( (byte*) destination, (byte*) source, (uint) length );
        }

#endif
    }

#if !MEMORY_COPY
    private static void ManagedMemoryCopy32( byte* dest, byte* src, uint len )
    {
        // P/Invoke into the native version when the buffers are overlapping and the copy needs to be performed backwards
        // This check can produce false positives for lengths greater than Int32.MaxInt. It is fine because we want to use PInvoke path for the large lengths anyway.
#if DEBUG
        if ( (uint) dest - (uint) src < len )
        {
            throw new ArgumentException( "Incorrect overlap of memory blocks.", nameof(dest) );
        }
#endif

        // This is portable version of memcpy. It mirrors what the hand optimized assembly versions of memcpy typically do.
        //
        // Ideally, we would just use the cpblk IL instruction here. Unfortunately, cpblk IL instruction is not as efficient as
        // possible yet and so we have this implementation here for now.

        // Note: It's important that this switch handles lengths at least up to 22.
        // See notes below near the main loop for why.

        // The switch will be very fast since it can be implemented using a jump
        // table in assembly. See http://stackoverflow.com/a/449297/4077294 for more info.

        switch ( len )
        {
            case 0:
                return;

            case 1:
                *dest = *src;

                return;

            case 2:
                *(short*) dest = *(short*) src;

                return;

            case 3:
                *(short*) dest = *(short*) src;
                *(dest + 2) = *(src + 2);

                return;

            case 4:
                *(int*) dest = *(int*) src;

                return;

            case 5:
                *(int*) dest = *(int*) src;
                *(dest + 4) = *(src + 4);

                return;

            case 6:
                *(int*) dest = *(int*) src;
                *(short*) (dest + 4) = *(short*) (src + 4);

                return;

            case 7:
                *(int*) dest = *(int*) src;
                *(short*) (dest + 4) = *(short*) (src + 4);
                *(dest + 6) = *(src + 6);

                return;

            case 8:
                *(int*) dest = *(int*) src;
                *(int*) (dest + 4) = *(int*) (src + 4);

                return;

            case 9:
                *(int*) dest = *(int*) src;
                *(int*) (dest + 4) = *(int*) (src + 4);
                *(dest + 8) = *(src + 8);

                return;

            case 10:
                *(int*) dest = *(int*) src;
                *(int*) (dest + 4) = *(int*) (src + 4);
                *(short*) (dest + 8) = *(short*) (src + 8);

                return;

            case 11:
                *(int*) dest = *(int*) src;
                *(int*) (dest + 4) = *(int*) (src + 4);
                *(short*) (dest + 8) = *(short*) (src + 8);
                *(dest + 10) = *(src + 10);

                return;

            case 12:
                *(int*) dest = *(int*) src;
                *(int*) (dest + 4) = *(int*) (src + 4);
                *(int*) (dest + 8) = *(int*) (src + 8);

                return;

            case 13:
                *(int*) dest = *(int*) src;
                *(int*) (dest + 4) = *(int*) (src + 4);
                *(int*) (dest + 8) = *(int*) (src + 8);
                *(dest + 12) = *(src + 12);

                return;

            case 14:
                *(int*) dest = *(int*) src;
                *(int*) (dest + 4) = *(int*) (src + 4);
                *(int*) (dest + 8) = *(int*) (src + 8);
                *(short*) (dest + 12) = *(short*) (src + 12);

                return;

            case 15:
                *(int*) dest = *(int*) src;
                *(int*) (dest + 4) = *(int*) (src + 4);
                *(int*) (dest + 8) = *(int*) (src + 8);
                *(short*) (dest + 12) = *(short*) (src + 12);
                *(dest + 14) = *(src + 14);

                return;

            case 16:
                *(int*) dest = *(int*) src;
                *(int*) (dest + 4) = *(int*) (src + 4);
                *(int*) (dest + 8) = *(int*) (src + 8);
                *(int*) (dest + 12) = *(int*) (src + 12);

                return;

            case 17:
                *(int*) dest = *(int*) src;
                *(int*) (dest + 4) = *(int*) (src + 4);
                *(int*) (dest + 8) = *(int*) (src + 8);
                *(int*) (dest + 12) = *(int*) (src + 12);
                *(dest + 16) = *(src + 16);

                return;

            case 18:
                *(int*) dest = *(int*) src;
                *(int*) (dest + 4) = *(int*) (src + 4);
                *(int*) (dest + 8) = *(int*) (src + 8);
                *(int*) (dest + 12) = *(int*) (src + 12);
                *(short*) (dest + 16) = *(short*) (src + 16);

                return;

            case 19:
                *(int*) dest = *(int*) src;
                *(int*) (dest + 4) = *(int*) (src + 4);
                *(int*) (dest + 8) = *(int*) (src + 8);
                *(int*) (dest + 12) = *(int*) (src + 12);
                *(short*) (dest + 16) = *(short*) (src + 16);
                *(dest + 18) = *(src + 18);

                return;

            case 20:
                *(int*) dest = *(int*) src;
                *(int*) (dest + 4) = *(int*) (src + 4);
                *(int*) (dest + 8) = *(int*) (src + 8);
                *(int*) (dest + 12) = *(int*) (src + 12);
                *(int*) (dest + 16) = *(int*) (src + 16);

                return;

            case 21:
                *(int*) dest = *(int*) src;
                *(int*) (dest + 4) = *(int*) (src + 4);
                *(int*) (dest + 8) = *(int*) (src + 8);
                *(int*) (dest + 12) = *(int*) (src + 12);
                *(int*) (dest + 16) = *(int*) (src + 16);
                *(dest + 20) = *(src + 20);

                return;

            case 22:
                *(int*) dest = *(int*) src;
                *(int*) (dest + 4) = *(int*) (src + 4);
                *(int*) (dest + 8) = *(int*) (src + 8);
                *(int*) (dest + 12) = *(int*) (src + 12);
                *(int*) (dest + 16) = *(int*) (src + 16);
                *(short*) (dest + 20) = *(short*) (src + 20);

                return;
        }

#if WINDOWS_PINVOKE && UNSECURE_PINVOKE
        if ( RunningOnWindows && len > 256 )
        {
            NativeMethods.RtlMoveMemory(dest, src, (UIntPtr)len);
            return;
        }
#endif

        uint i = 0; // byte offset at which we're copying

        if ( ((int) dest & 3) != 0 )
        {
            if ( ((int) dest & 1) != 0 )
            {
                *(dest + i) = *(src + i);
                i += 1;

                if ( ((int) dest & 2) != 0 )
                {
                    goto IntAligned;
                }
            }

            *(short*) (dest + i) = *(short*) (src + i);
            i += 2;
        }

    IntAligned:

        var end = len - 16;
        len -= i; // lower 4 bits of len represent how many bytes are left *after* the unrolled loop

        // We know due to the above switch-case that this loop will always run 1 iteration; max
        // bytes we copy before checking is 23 (7 to align the pointers, 16 for 1 iteration) so
        // the switch handles lengths 0-22.
        Debug.Assert( end >= 7 && i <= end );

        // This is separated out into a different variable, so the i + 16 addition can be
        // performed at the start of the pipeline and the loop condition does not have
        // a dependency on the writes.
        uint counter;

        do
        {
            counter = i + 16;

            // This loop looks very costly since there appear to be a bunch of temporary values
            // being created with the adds, but the jit (for x86 anyways) will convert each of
            // these to use memory addressing operands.

            // So the only cost is a bit of code size, which is made up for by the fact that
            // we save on writes to dest/src.

            *(int*) (dest + i) = *(int*) (src + i);
            *(int*) (dest + i + 4) = *(int*) (src + i + 4);
            *(int*) (dest + i + 8) = *(int*) (src + i + 8);
            *(int*) (dest + i + 12) = *(int*) (src + i + 12);

            i = counter;

            // See notes above for why this wasn't used instead
            // i += 16;
        }
        while ( counter <= end );

        if ( (len & 8) != 0 )
        {
            *(int*) (dest + i) = *(int*) (src + i);
            *(int*) (dest + i + 4) = *(int*) (src + i + 4);
            i += 8;
        }

        if ( (len & 4) != 0 )
        {
            *(int*) (dest + i) = *(int*) (src + i);
            i += 4;
        }

        if ( (len & 2) != 0 )
        {
            *(short*) (dest + i) = *(short*) (src + i);
            i += 2;
        }

        if ( (len & 1) != 0 )
        {
            *(dest + i) = *(src + i);

            // We're not using i after this, so not needed
            // i += 1;
        }
    }

    private static void ManagedMemoryCopy64( byte* dest, byte* src, ulong len )
    {
        // This is portable version of memcpy. It mirrors what the hand optimized assembly versions of memcpy typically do.
        //
        // Ideally, we would just use the cpblk IL instruction here. Unfortunately, cpblk IL instruction is not as efficient as
        // possible yet and so we have this implementation here for now.

        // Note: It's important that this switch handles lengths at least up to 22.
        // See notes below near the main loop for why.

        // The switch will be very fast since it can be implemented using a jump
        // table in assembly. See http://stackoverflow.com/a/449297/4077294 for more info.

        switch ( len )
        {
            case 0:
                return;

            case 1:
                *dest = *src;

                return;

            case 2:
                *(short*) dest = *(short*) src;

                return;

            case 3:
                *(short*) dest = *(short*) src;
                *(dest + 2) = *(src + 2);

                return;

            case 4:
                *(int*) dest = *(int*) src;

                return;

            case 5:
                *(int*) dest = *(int*) src;
                *(dest + 4) = *(src + 4);

                return;

            case 6:
                *(int*) dest = *(int*) src;
                *(short*) (dest + 4) = *(short*) (src + 4);

                return;

            case 7:
                *(int*) dest = *(int*) src;
                *(short*) (dest + 4) = *(short*) (src + 4);
                *(dest + 6) = *(src + 6);

                return;

            case 8:
                *(long*) dest = *(long*) src;

                return;

            case 9:
                *(long*) dest = *(long*) src;
                *(dest + 8) = *(src + 8);

                return;

            case 10:
                *(long*) dest = *(long*) src;
                *(short*) (dest + 8) = *(short*) (src + 8);

                return;

            case 11:
                *(long*) dest = *(long*) src;
                *(short*) (dest + 8) = *(short*) (src + 8);
                *(dest + 10) = *(src + 10);

                return;

            case 12:
                *(long*) dest = *(long*) src;
                *(int*) (dest + 8) = *(int*) (src + 8);

                return;

            case 13:
                *(long*) dest = *(long*) src;
                *(int*) (dest + 8) = *(int*) (src + 8);
                *(dest + 12) = *(src + 12);

                return;

            case 14:
                *(long*) dest = *(long*) src;
                *(int*) (dest + 8) = *(int*) (src + 8);
                *(short*) (dest + 12) = *(short*) (src + 12);

                return;

            case 15:
                *(long*) dest = *(long*) src;
                *(int*) (dest + 8) = *(int*) (src + 8);
                *(short*) (dest + 12) = *(short*) (src + 12);
                *(dest + 14) = *(src + 14);

                return;

            case 16:
                *(long*) dest = *(long*) src;
                *(long*) (dest + 8) = *(long*) (src + 8);

                return;

            case 17:
                *(long*) dest = *(long*) src;
                *(long*) (dest + 8) = *(long*) (src + 8);
                *(dest + 16) = *(src + 16);

                return;

            case 18:
                *(long*) dest = *(long*) src;
                *(long*) (dest + 8) = *(long*) (src + 8);
                *(short*) (dest + 16) = *(short*) (src + 16);

                return;

            case 19:
                *(long*) dest = *(long*) src;
                *(long*) (dest + 8) = *(long*) (src + 8);
                *(short*) (dest + 16) = *(short*) (src + 16);
                *(dest + 18) = *(src + 18);

                return;

            case 20:
                *(long*) dest = *(long*) src;
                *(long*) (dest + 8) = *(long*) (src + 8);
                *(int*) (dest + 16) = *(int*) (src + 16);

                return;

            case 21:
                *(long*) dest = *(long*) src;
                *(long*) (dest + 8) = *(long*) (src + 8);
                *(int*) (dest + 16) = *(int*) (src + 16);
                *(dest + 20) = *(src + 20);

                return;

            case 22:
                *(long*) dest = *(long*) src;
                *(long*) (dest + 8) = *(long*) (src + 8);
                *(int*) (dest + 16) = *(int*) (src + 16);
                *(short*) (dest + 20) = *(short*) (src + 20);

                return;
        }

#if WINDOWS_PINVOKE && UNSECURE_PINVOKE
        if ( RunningOnWindows && len > 256 )
        {
            NativeMethods.RtlMoveMemory(dest, src, (UIntPtr)len);
            return;
        }
#endif

        ulong i = 0; // byte offset at which we're copying

        if ( ((int) dest & 3) != 0 )
        {
            if ( ((int) dest & 1) != 0 )
            {
                *(dest + i) = *(src + i);
                i += 1;

                if ( ((int) dest & 2) != 0 )
                {
                    goto IntAligned;
                }
            }

            *(short*) (dest + i) = *(short*) (src + i);
            i += 2;
        }

    IntAligned:

        // On 64-bit IntPtr.Size == 8, so we want to advance to the next 8-aligned address. If
        // (int)dest % 8 is 0, 5, 6, or 7, we will already have advanced by 0, 3, 2, or 1
        // bytes to the next aligned address (respectively), so do nothing. On the other hand,
        // if it is 1, 2, 3, or 4 we will want to copy-and-advance another 4 bytes until
        // we're aligned.
        // The thing 1, 2, 3, and 4 have in common that the others don't is that if you
        // subtract one from them, their 3rd lsb will not be set. Hence, the below check.

        if ( (((int) dest - 1) & 4) == 0 )
        {
            *(int*) (dest + i) = *(int*) (src + i);
            i += 4;
        }

        var end = len - 16;
        len -= i; // lower 4 bits of len represent how many bytes are left *after* the unrolled loop

        // We know due to the above switch-case that this loop will always run 1 iteration; max
        // bytes we copy before checking is 23 (7 to align the pointers, 16 for 1 iteration) so
        // the switch handles lengths 0-22.
        Debug.Assert( end >= 7 && i <= end );

        // This is separated out into a different variable, so the i + 16 addition can be
        // performed at the start of the pipeline and the loop condition does not have
        // a dependency on the writes.
        ulong counter;

        do
        {
            counter = i + 16;

            // This loop looks very costly since there appear to be a bunch of temporary values
            // being created with the adds, but the jit (for x86 anyways) will convert each of
            // these to use memory addressing operands.

            // So the only cost is a bit of code size, which is made up for by the fact that
            // we save on writes to dest/src.

            *(long*) (dest + i) = *(long*) (src + i);
            *(long*) (dest + i + 8) = *(long*) (src + i + 8);

            i = counter;

            // See notes above for why this wasn't used instead
            // i += 16;
        }
        while ( counter <= end );

        if ( (len & 8) != 0 )
        {
            *(long*) (dest + i) = *(long*) (src + i);
            i += 8;
        }

        if ( (len & 4) != 0 )
        {
            *(int*) (dest + i) = *(int*) (src + i);
            i += 4;
        }

        if ( (len & 2) != 0 )
        {
            *(short*) (dest + i) = *(short*) (src + i);
            i += 2;
        }

        if ( (len & 1) != 0 )
        {
            *(dest + i) = *(src + i);

            // We're not using i after this, so not needed
            // i += 1;
        }
    }

#endif

    public static int HashMemory( byte* source, int count )
    {
        return HashMemory( source, count, count );
    }

    public static int HashMemory( IntPtr source, int count, int initialHashCode )
    {
        return HashMemory( (byte*) source, count, initialHashCode );
    }

    public static int HashMemory( byte* source, int count, int initialHashCode )
    {
        var hashCode = initialHashCode;
        var src = source;

        if ( count >= 0x10 )
        {
            do
            {
                hashCode = (hashCode << 13) + hashCode + *(int*) src;
                hashCode = (hashCode << 13) + hashCode + *(int*) (src + 4);
                hashCode = (hashCode << 13) + hashCode + *(int*) (src + 8);
                hashCode = (hashCode << 13) + hashCode + *(int*) (src + 12);

                src += 0x10;
            }
            while ( (count -= 0x10) >= 0x10 );
        }

        if ( count > 0 )
        {
            if ( (count & 8) != 0 )
            {
                hashCode = (hashCode << 13) + hashCode + *(int*) src;
                hashCode = (hashCode << 13) + hashCode + *(int*) (src + 4);
                src += 8;
            }

            if ( (count & 4) != 0 )
            {
                hashCode = (hashCode << 13) + hashCode + *(int*) src;
                src += 4;
            }

            if ( (count & 2) != 0 )
            {
                hashCode = (hashCode << 13) + hashCode + *(short*) src;
                src += 2;
            }

            if ( (count & 1) != 0 )
            {
                hashCode = (hashCode << 13) + hashCode + *src;
            }
        }

        return hashCode;
    }

    public static bool CompareMemory( byte* left, byte* right, int count )
    {
#if WINDOWS_PINVOKE && UNSECURE_PINVOKE
        if ( RunningOnWindows )
        {
            return NativeMethods.RtlCompareMemory( left, right, count ) == count;
        }
        else
#endif
        {
            return ManagedCompareMemory( left, right, count );
        }
    }

#if WINDOWS_PINVOKE && UNSECURE_PINVOKE
    private static class NativeMethods
    {
        [DllImport("ntdll.dll"), SuppressUnmanagedCodeSecurity]
        public static extern int RtlCompareMemory(byte* src1, byte* src2, int count);

        [DllImport("ntdll.dll"), SuppressUnmanagedCodeSecurity]
        public static extern void RtlMoveMemory(byte* target, byte* source, UIntPtr count);

        [DllImport( "ntdll.dll" ), SuppressUnmanagedCodeSecurity]
        public static extern void RtlZeroMemory( byte* target, int count );

    }
#endif

    private static bool ManagedCompareMemory( byte* left, byte* right, int count )
    {
        if ( left == right )
        {
            return true;
        }

        var l = left;
        var r = right;

        if ( count >= 0x10 )
        {
            do
            {
                int l1 = *(int*) r, r1 = *(int*) l;
                int l2 = *(int*) (r + 4), r2 = *(int*) (l + 4);
                int l3 = *(int*) (r + 8), r3 = *(int*) (l + 8);
                int l4 = *(int*) (r + 12), r4 = *(int*) (l + 12);

                if ( l1 != r1 || l2 != r2 || l3 != r3 || l4 != r4 )
                {
                    return false;
                }

                r += 0x10;
                l += 0x10;
            }
            while ( (count -= 0x10) >= 0x10 );
        }

        if ( count > 0 )
        {
            if ( (count & 8) != 0 )
            {
                int l1 = *(int*) r, r1 = *(int*) l;
                int l2 = *(int*) (r + 4), r2 = *(int*) (l + 4);

                if ( l1 != r1 || l2 != r2 )
                {
                    return false;
                }

                r += 8;
                l += 8;
            }

            if ( (count & 4) != 0 )
            {
                if ( *(int*) r != *(int*) l )
                {
                    return false;
                }

                r += 4;
                l += 4;
            }

            if ( (count & 2) != 0 )
            {
                if ( *(short*) r != *(short*) l )
                {
                    return false;
                }

                r += 2;
                l += 2;
            }

            if ( (count & 1) != 0 )
            {
                if ( *r != *l )
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static void ZeroMemory( byte* target, int count )
    {
#if WINDOWS_PINVOKE && UNSECURE_PINVOKE
        if (RunningOnWindows)
        {
            NativeMethods.RtlZeroMemory(target, count);
            return;
        }
#endif

        var dest = target;

        if ( count >= 0x10 )
        {
            do
            {
                *(int*) dest = 0;
                *(int*) (dest + 4) = 0;
                *(int*) (dest + 8) = 0;
                *(int*) (dest + 12) = 0;
                dest += 0x10;
            }
            while ( (count -= 0x10) >= 0x10 );
        }

        if ( count > 0 )
        {
            if ( (count & 8) != 0 )
            {
                *(int*) dest = 0;
                *(int*) (dest + 4) = 0;
                dest += 8;
            }

            if ( (count & 4) != 0 )
            {
                *(int*) dest = 0;
                dest += 4;
            }

            if ( (count & 2) != 0 )
            {
                *(short*) dest = 0;
                dest += 2;
            }

            if ( (count & 1) != 0 )
            {
                *dest = 0;
            }
        }
    }
}