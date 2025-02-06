// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Records;
using System.Runtime.CompilerServices;

namespace Flashtrace.Messages;

internal static class MessageHelper
{
    public static void Write<T>( in T message, ILogRecordBuilder recordBuilder, LogRecordItem item )
        where T : IMessage
    {
        // TODO: Investigate modernizing message types (eg, to readonly struct) to avoid need for Unsafe.AsRef.
        Unsafe.AsRef( message ).Write( recordBuilder, item );
    }
}