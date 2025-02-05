// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Xunit.Abstractions;
using Xunit.Sdk;

namespace Metalama.Testing.AspectTesting
{
    internal static class MessageSinkExtensions
    {
        public static void Trace( this IMessageSink messageSink, string message ) => messageSink.OnMessage( new DiagnosticMessage( message ) );
    }
}