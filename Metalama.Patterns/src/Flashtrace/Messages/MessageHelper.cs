// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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